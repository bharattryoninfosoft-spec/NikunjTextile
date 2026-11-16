using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;

namespace NikunjTextile.Class
{
    public class SaleOrderModel
    {
        public int SaleOrderID { get; set; }
        public DateTime DateAndTime { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public int PartyID { get; set; }
        public string BrokerID { get; set; }
        public decimal MarkupPercent { get; set; }
        public string TransportID { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string Remark { get; set; }
        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int CompanyId { get; set; }
        public string DiscountType { get; set; }
        public  string AdditionalRemark { get; set; }
        public decimal AdditionalValue { get; set; }

        public List<SaleOrderDetailModel> Details { get; set; }
        public List<SaleOrderAdditionalChargeModel> AdditionalCharges { get; set; }
    }
    public class SaleOrderDetailModel
    {
        public int DetailID { get; set; }
        public int SaleOrderID { get; set; }
        public string DesignID { get; set; }
        public int DesignColorMatchingID { get; set; }
        public string DesignNo { get; set; }
        public string ItemType { get; set; }
        public int NoOfColours { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public string DetailsRemark { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal BrokerRate { get; set; }
        public List<SaleOrderSubDetailModel> SubDetails { get; set; }
    }
    public class SaleOrderSubDetailModel
    {
        public int SubDetailID { get; set; }
        public int DetailID { get; set; }
        public int ColourID { get; set; }
        public string ColourName { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public string Remark { get; set; }
    }
    public class SaleOrderVM
    {
        public int SaleOrderID { get; set; }
        public DateTime DateAndTime { get; set; }
        public string OrderNo { get; set; }
        public string OrderDate { get; set; }
        public string OrderDates { get; set; }
        public int PartyID { get; set; }
        public string BrokerID { get; set; }
        public decimal MarkupPercent { get; set; }
        public string TransportID { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string Remark { get; set; }
        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int CompanyId { get; set; }
        public string PartyName { get; set; }
        public string BrokerName { get; set; }
        public string TransportName { get; set; }
        public int SubDetailID { get; set; }
        public int DetailID { get; set; }
        public int ColourID { get; set; }
        public string ColourName { get; set; }
        public decimal Qty { get; set; }
        public decimal SaleRate { get; set; }
        public string Unit { get; set; }
        public string DesignNo { get; set; }
        public string DesignName { get; set; }
        public string ColorGroup { get; set; }
        public int ColorGroupId { get; set; }
        public int ColorDetailsID { get; set; }
        public int UnitId { get; set; }
        public int TypeID { get; set; }
        public decimal RoundOff { get; set; }
        public string DetailsRemark { get; set; }
        public string DiscountType  { get; set; }
        public string AdditionalRemark { get; set; }
        public decimal AdditionalValue { get; set; }
        public List<SaleOrderDetailVM> Details { get; set; } = new List<SaleOrderDetailVM>();
        public List<SaleOrderAdditionalChargeModel> AdditionalCharges { get; set; }
        public int isEdite { get; set; }
    }

    public class SaleOrderDetailVM
    {
        public int DetailID { get; set; }
        public int SaleOrderID { get; set; }    
        public string DesignNo { get; set; }
        public int ItemType { get; set; }
        public int NoOfColours { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public decimal TotalQty { get; set; }
        public decimal BrokerRate { get; set; }
        public string DetailsRemark { get; set; }
        public string designName { get; set; }
        public string ColorGroup { get; set; }
        public int DesignColorMatchingID { get; set; }
        public int colorGroupId { get; set; }
        public int colorGroupName { get; set; }
        public int colorId { get; set; }
        public string colorName { get; set; }
        public string DesignId { get; set; }
        public string DesignName { get; set; }
        public int ColorGroupId { get; set; }
        public int SubDetailID { get; set; }
        public int ColourID { get; set; }
        public string ColourName { get; set; }
        public string ImageUrl { get; set; }
        public decimal SaleRate { get; set; }
        public decimal MarkupPercent { get; set; }
        public int ColorDetailsID { get; set; }
        public int UnitId { get; set; }
        public int TypeID { get; set; }
        public List<SaleOrderSubVM> SubDetails { get; set; } = new List<SaleOrderSubVM>();
      
    }

    public class SaleOrderSubVM
    {
        public int SubDetailID { get; set; }
        public int DetailID { get; set; }
        public int ColourID { get; set; }
        public string ColourName { get; set; }
        public decimal Qty { get; set; }
        public decimal SaleRate { get; set; }
        public decimal MarkupPercent { get; set; }
        public string Unit { get; set; }
        public string DesignNo { get; set; }
        public string DesignName { get; set; }
        public string ColorGroup { get; set; }
        public int ColorGroupId { get; set; }
        public int ColorDetailsID { get; set; }
        public int UnitId { get; set; }
        public int TypeID { get; set; }
        public string Remark { get; set; }
        
    }
    public class ColorMatchingDetailModel
    {
        public int DesignColorMatchingFormID { get; set; }
        public int DesignColorMatchingDetailsID { get; set; }
        public int ColorGroupID { get; set; }
        public string WarpMatchingID { get; set; }
        public string FeederMatchingID { get; set; }
    }

    public class SaleOrderResponse
    {
        public bool success { get; set; }
        public List<SaleOrderVM> Data { get; set; }
        public int totalRecords { get; set; }
        public int currentPage { get; set; }
        public int pageSize { get; set; }
    }

    public class SaleOrderReport
    {
        public int SaleOrderID { get; set; }
        public DateTime DateAndTime { get; set; }
        public int OrderNo { get; set; }
        public DateTime OrderDate { get; set; }

        public int PartyID { get; set; }
        public string PartyName { get; set; }
        public string BillingAddress { get; set; }
        public string PartyMobileNo { get; set; }
        public string PartyGSTIN { get; set; }
        public string PartyState { get; set; }
        public string PartyCity { get; set; }
        public string PartyPincode { get; set; }

        public string BrokerCode { get; set; }
        public string BrokerName { get; set; }

        public string TransportID { get; set; }
        public string TransportName { get; set; }

        public decimal MarkupPercent { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }

        public decimal GSTPercent { get; set; }
        public decimal GSTAmount { get; set; }

        public decimal InvoiceAmount { get; set; }
        public decimal RoundOff { get; set; }

        public string Remark { get; set; }

        public string AdditionalRemark { get; set; }
        public decimal AdditionalValue { get; set; }

        public string DiscountType { get; set; }

        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int CompanyId { get; set; }

        public string CompanyName { get; set; }
        public string BusinessAddress { get; set; }
        public string CompanyGSTIN { get; set; }
        public string CompanyMobileNo { get; set; }
        public string CompanyState { get; set; }
        public string Email { get; set; }
    }
    public class SaleOrderDetailsReport
    {
        public int DetailID { get; set; }
        public int SaleOrderID { get; set; }

        public int DesignID { get; set; }
        public string DesignNo { get; set; }

        public int ItemType { get; set; }
        public string TypeName { get; set; }

        public int NoOfColours { get; set; }
        public string Colors { get; set; }

        public decimal Qty { get; set; }
        public string Unit { get; set; }

        public decimal Rate { get; set; }
        public decimal Amount { get; set; }

        public int DesignColorMatchingID { get; set; }

        public decimal BrokerRate { get; set; }
    }
    public class SaleOrderReportModel
    {
        public SaleOrderReport Master { get; set; }

        public List<SaleOrderDetailsReport> Details { get; set; }
    }
    public class SaleOrderAdditionalChargeModel
    {
        public int AdditionalChargeID { get; set; }
        public string ChargeRemark { get; set; }
        public decimal ChargeValue { get; set; }
        public decimal TotalCalculatedAmount { get; set; }
    }

    public static class SaleOrderLockHelper
    {
        public static void AcquireOrderLock(SqlConnection con, SqlTransaction tran, long saleOrderId, int timeoutMs = 10000)
        {
            if (saleOrderId <= 0) return;
            string resourceName = "SaleOrder_Lock_" + saleOrderId;
            using (SqlCommand cmdLock = new SqlCommand("sp_getapplock", con, tran))
            {
                cmdLock.CommandType = CommandType.StoredProcedure;
                cmdLock.Parameters.AddWithValue("@Resource", resourceName);
                cmdLock.Parameters.AddWithValue("@LockMode", "Exclusive");
                cmdLock.Parameters.AddWithValue("@LockOwner", "Transaction");
                cmdLock.Parameters.AddWithValue("@LockTimeout", timeoutMs);

                SqlParameter returnParam = new SqlParameter();
                returnParam.Direction = ParameterDirection.ReturnValue;
                cmdLock.Parameters.Add(returnParam);

                cmdLock.ExecuteNonQuery();

                int result = Convert.ToInt32(returnParam.Value);
                if (result < 0)
                {
                    throw new InvalidOperationException(string.Format("Failed to acquire application lock for Sale Order {0} (Result: {1}). The order may be in use by another transaction.", saleOrderId, result));
                }
            }
        }

        public static void AcquireOrderLocks(SqlConnection con, SqlTransaction tran, IEnumerable<long> saleOrderIds, int timeoutMs = 10000)
        {
            if (saleOrderIds == null) return;
            var sorted = saleOrderIds.Where(id => id > 0).Distinct().OrderBy(id => id).ToList();
            foreach (var id in sorted)
            {
                AcquireOrderLock(con, tran, id, timeoutMs);
            }
        }

        public static long ResolveSaleOrderId(SqlConnection con, SqlTransaction tran, long saleOrderId, long detailId = 0, long subDetailId = 0)
        {
            if (saleOrderId > 0) return saleOrderId;

            if (detailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 SaleOrderID FROM dbo.SaleOrderDetails WITH (NOLOCK) WHERE DetailID = @ID", con, tran))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.BigInt).Value = detailId;
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt64(val);
                }
            }

            if (subDetailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 D.SaleOrderID FROM dbo.SaleOrderSubDetails SD WITH (NOLOCK) INNER JOIN dbo.SaleOrderDetails D WITH (NOLOCK) ON SD.DetailID = D.DetailID WHERE SD.SubDetailID = @ID", con, tran))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.BigInt).Value = subDetailId;
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt64(val);
                }
            }

            return 0;
        }

        public static void ValidateOrderAndDetailActive(SqlConnection con, SqlTransaction tran, long saleOrderId, long saleOrderDetailId = 0, long saleOrderSubDetailId = 0)
        {
            if (saleOrderId <= 0 && saleOrderDetailId <= 0 && saleOrderSubDetailId <= 0) return;

            if (saleOrderId <= 0)
            {
                saleOrderId = ResolveSaleOrderId(con, tran, saleOrderId, saleOrderDetailId, saleOrderSubDetailId);
            }

            if (saleOrderId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT SaleOrderID FROM dbo.SaleOrder WITH (UPDLOCK, HOLDLOCK) WHERE SaleOrderID = @OrderID", con, tran))
                {
                    cmd.Parameters.Add("@OrderID", SqlDbType.Int).Value = (int)saleOrderId;
                    object val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value)
                    {
                        throw new InvalidOperationException(string.Format("Sale Order {0} does not exist or has been deleted.", saleOrderId));
                    }
                }
            }

            if (saleOrderDetailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT SaleOrderID FROM dbo.SaleOrderDetails WITH (UPDLOCK, HOLDLOCK) WHERE DetailID = @DetailID", con, tran))
                {
                    cmd.Parameters.Add("@DetailID", SqlDbType.Int).Value = (int)saleOrderDetailId;
                    object val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value)
                    {
                        throw new InvalidOperationException(string.Format("Sale Order Detail {0} does not exist or has been deleted.", saleOrderDetailId));
                    }
                    long actualOrderId = Convert.ToInt64(val);
                    if (saleOrderId > 0 && actualOrderId > 0 && actualOrderId != saleOrderId)
                    {
                        throw new InvalidOperationException(string.Format("Sale Order Detail {0} does not belong to Sale Order {1} (belongs to {2}).", saleOrderDetailId, saleOrderId, actualOrderId));
                    }
                }
            }

            if (saleOrderSubDetailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT SD.DetailID, D.SaleOrderID FROM dbo.SaleOrderSubDetails SD WITH (UPDLOCK, HOLDLOCK) INNER JOIN dbo.SaleOrderDetails D WITH (UPDLOCK, HOLDLOCK) ON SD.DetailID = D.DetailID WHERE SD.SubDetailID = @SubDetailID", con, tran))
                {
                    cmd.Parameters.Add("@SubDetailID", SqlDbType.Int).Value = (int)saleOrderSubDetailId;
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                        {
                            throw new InvalidOperationException(string.Format("Sale Order SubDetail {0} does not exist or has been deleted.", saleOrderSubDetailId));
                        }
                        long actualDetailId = dr["DetailID"] != DBNull.Value ? Convert.ToInt64(dr["DetailID"]) : 0;
                        long actualOrderId = dr["SaleOrderID"] != DBNull.Value ? Convert.ToInt64(dr["SaleOrderID"]) : 0;

                        if (saleOrderDetailId > 0 && actualDetailId > 0 && actualDetailId != saleOrderDetailId)
                        {
                            throw new InvalidOperationException(string.Format("Sale Order SubDetail {0} does not belong to Detail {1} (belongs to {2}).", saleOrderSubDetailId, saleOrderDetailId, actualDetailId));
                        }

                        if (saleOrderId > 0 && actualOrderId > 0 && actualOrderId != saleOrderId)
                        {
                            throw new InvalidOperationException(string.Format("Sale Order SubDetail {0} does not belong to Sale Order {1} (belongs to {2}).", saleOrderSubDetailId, saleOrderId, actualOrderId));
                        }
                    }
                }
            }
        }
    }
}