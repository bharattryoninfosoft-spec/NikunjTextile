using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnOutwardScanMasterModel
    {
        public string YarnOutwardID { get; set; }
        public int PartyId { get; set; }
        public string ChallanNo { get; set; }
        public string ChallanDate { get; set; }
        public int TotalBox { get; set; }
        public decimal TotalWeight { get; set; }
        public List<YarnOutwardScanDetailModel> Details { get; set; }
    }

    public class YarnOutwardScanDetailModel
    {
        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public int GodownLocationID { get; set; }
        public string BoxNo { get; set; }
        public decimal NetWeight { get; set; }
        public int YarnInwardDetailID { get; set; }
        public int YarnCompany { get; set; }
    }

    public class ChallanAllocationResult
    {
        public int NextChallanNumber { get; set; }
        public string ChallanNo { get; set; }
        public int CompanyId { get; set; }
        public int FinancialYearID { get; set; }
    }

    public static class YarnScanChallanHelper
    {
        /// <summary>
        /// Resolves exactly one default CompanyId and FinancialYearID.
        /// Throws an InvalidOperationException if zero or multiple defaults exist.
        /// </summary>
        public static (int companyId, int financialYearId) ResolveDefaultScope(SqlConnection con, SqlTransaction tran)
        {
            if (con == null) throw new ArgumentNullException(nameof(con));
            if (tran == null) throw new ArgumentNullException(nameof(tran));

            int companyId;
            int financialYearId;

            // 1. Resolve single default CompanyId
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT CompanyId 
                FROM dbo.CompanyMaster WITH (UPDLOCK, HOLDLOCK)
                WHERE is_default = 1", con, tran))
            {
                List<int> companies = new List<int>();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        companies.Add(Convert.ToInt32(rdr["CompanyId"]));
                    }
                }

                if (companies.Count == 0)
                {
                    throw new InvalidOperationException("No default Company found in CompanyMaster (is_default = 1).");
                }
                if (companies.Count > 1)
                {
                    throw new InvalidOperationException("Multiple default Companies found in CompanyMaster (is_default = 1).");
                }
                companyId = companies[0];
            }

            // 2. Resolve single default FinancialYearID
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT FinancialYearID 
                FROM dbo.FinancialYearMaster WITH (UPDLOCK, HOLDLOCK)
                WHERE IsDefault = 1", con, tran))
            {
                List<int> years = new List<int>();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        years.Add(Convert.ToInt32(rdr["FinancialYearID"]));
                    }
                }

                if (years.Count == 0)
                {
                    throw new InvalidOperationException("No default FinancialYear found in FinancialYearMaster (IsDefault = 1).");
                }
                if (years.Count > 1)
                {
                    throw new InvalidOperationException("Multiple default FinancialYears found in FinancialYearMaster (IsDefault = 1).");
                }
                financialYearId = years[0];
            }

            return (companyId, financialYearId);
        }

        /// <summary>
        /// Atomically allocates the next sequential ChallanNo using SQL Server sp_getapplock
        /// scoped by CompanyId and FinancialYearID.
        /// Guaranteed to be transaction-bound, concurrency-safe, and never produce duplicates.
        /// </summary>
        public static ChallanAllocationResult AllocateNextChallanNo(
              SqlConnection con,
              SqlTransaction tran,
              int? explicitCompanyId = null,
              int? explicitFinancialYearId = null,
              int timeoutMs = 15000)
        {
            if (con == null) throw new ArgumentNullException(nameof(con));
            if (tran == null) throw new ArgumentNullException(nameof(tran));

            int companyId;
            int financialYearId;

            if (explicitCompanyId.HasValue && explicitCompanyId.Value > 0 &&
                explicitFinancialYearId.HasValue && explicitFinancialYearId.Value > 0)
            {
                companyId = explicitCompanyId.Value;
                financialYearId = explicitFinancialYearId.Value;
            }
            else
            {
                var defaults = ResolveDefaultScope(con, tran);
                companyId = defaults.companyId;
                financialYearId = defaults.financialYearId;
            }

            // Acquire exclusive application lock scoped to CompanyId + FinancialYearID
            string lockResource = $"YarnOutwardScan_ChallanNo_{companyId}_{financialYearId}";
            using (SqlCommand cmdLock = new SqlCommand(@"
                DECLARE @res INT;
                EXEC @res = sp_getapplock 
                    @Resource = @Resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = @TimeoutMs;
                SELECT @res;", con, tran))
            {
                cmdLock.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value = lockResource;
                cmdLock.Parameters.Add("@TimeoutMs", SqlDbType.Int).Value = timeoutMs;

                object lockObj = cmdLock.ExecuteScalar();
                int lockResult = (lockObj != null && lockObj != DBNull.Value) ? Convert.ToInt32(lockObj) : -999;

                if (lockResult < 0)
                {
                    string reason;
                    switch (lockResult)
                    {
                        case -1: reason = "Lock request timed out."; break;
                        case -2: reason = "Lock request was canceled."; break;
                        case -3: reason = "Lock request was chosen as deadlock victim."; break;
                        default: reason = $"sp_getapplock returned error code {lockResult}."; break;
                    }
                    throw new InvalidOperationException($"Failed to acquire exclusive ChallanNo lock for resource '{lockResource}': {reason}");
                }
            }

            // Calculate MAX(TRY_CONVERT(INT, ChallanNo)) + 1 after acquiring the exclusive lock
            // Aggregate query without GROUP BY guarantees exactly 1 row is returned even if table is empty
            using (SqlCommand cmdNext = new SqlCommand(@"
                SELECT ISNULL(MAX(TRY_CONVERT(INT, ChallanNo)), 0) + 1 AS NextChallanNo
                FROM dbo.YARNOUTWARDScanMaster
                WHERE CompanyId = @CompanyId AND FinancialYearID = @FinancialYearID;", con, tran))
            {
                cmdNext.Parameters.Add("@CompanyId", SqlDbType.Int).Value = companyId;
                cmdNext.Parameters.Add("@FinancialYearID", SqlDbType.Int).Value = financialYearId;

                object nextValObj = cmdNext.ExecuteScalar();
                int nextVal = (nextValObj != null && nextValObj != DBNull.Value) ? Convert.ToInt32(nextValObj) : 1;
                if (nextVal <= 0) nextVal = 1;

                return new ChallanAllocationResult
                {
                    CompanyId = companyId,
                    FinancialYearID = financialYearId,
                    NextChallanNumber = nextVal,
                    ChallanNo = nextVal.ToString()
                };
            }
        }

        /// <summary>
        /// Reads the non-blocking preview next ChallanNo for display in UI.
        /// </summary>
        public static int GetPreviewNextChallanNo(SqlConnection con, SqlTransaction tran = null)
        {
            if (con == null) throw new ArgumentNullException(nameof(con));

            using (SqlCommand cmd = new SqlCommand(@"
                SELECT ISNULL(MAX(TRY_CONVERT(INT, ChallanNo)), 0) + 1 AS ChallanNo
                FROM dbo.YARNOUTWARDScanMaster WITH (NOLOCK)
                WHERE CompanyId = (SELECT TOP 1 CompanyId FROM dbo.CompanyMaster WHERE is_default = 1)
                  AND FinancialYearID = (SELECT TOP 1 FinancialYearID FROM dbo.FinancialYearMaster WHERE IsDefault = 1);", con, tran))
            {
                object valObj = cmd.ExecuteScalar();
                int val = (valObj != null && valObj != DBNull.Value) ? Convert.ToInt32(valObj) : 1;
                return val > 0 ? val : 1;
            }
        }
    }
}
