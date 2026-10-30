using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Web;

namespace NikunjTextile.Class
{
    public class ProductionOrderRequest
    {
        public long ProductionOrderID { get; set; }
        public string ProductionOrderNo { get; set; }
        public string ProductionOrderDate { get; set; }

        public long? JobWorkerID { get; set; }
        public string JobWorkerName { get; set; }
        public decimal Rpm { get; set; }
        public decimal PanaRepeat { get; set; }
        public decimal PanaWidth { get; set; }
        public decimal NoOfMachines { get; set; }
        public decimal JobRate { get; set; }
        public string Traders { get; set; }

        public decimal TotalPcs { get; set; }
        public decimal TotalProductionPcs { get; set; }
        public decimal TotalPendingPcs { get; set; }
        public decimal TotalAmount { get; set; }

        // These are accepted from the client, but SaveProductionOrder
        // always prefers the logged-in/default database values.
        public long? CompanyId { get; set; }
        public long? UserAccountId { get; set; }

        public List<ProductionOrderDetailRequest> listProductionOrderDetail { get; set; }
    }

    public class ProductionOrderDetailRequest
    {
        public long ProductionOrderDetailID { get; set; }

        public long? SalesOrderID { get; set; }
        public long? SalesOrderDetailID { get; set; }
        public long? SubDetailID { get; set; }

        public string SalesOrderNo { get; set; }
        public string SlipNo { get; set; }
        public string Barcode { get; set; }

        public string OrderDate { get; set; }

        public long? PartyID { get; set; }
        public string PartyName { get; set; }

        public long? DesignEntryFormID { get; set; }
        public string DesignNo { get; set; }

        public long? ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }

        public string WarpQuality { get; set; }
        public string WeftQuality { get; set; }

        public decimal PCS { get; set; }
        public decimal ProductionPCS { get; set; }
        public decimal PendingPCS { get; set; }

        public decimal SalesRate { get; set; }
        public decimal JobRate { get; set; }
        public decimal Amount { get; set; }

        public decimal Rpm { get; set; }
        public decimal PanaRepeat { get; set; }
        public decimal PanaWidth { get; set; }
        public decimal NoOfMachines { get; set; }

        public decimal RepPCS { get; set; }
        public decimal RepRate { get; set; }
        public decimal RepAmt { get; set; }
        public int JobberAccept { get; set; }
    }

    public class ProductionOrderSaveResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long ProductionOrderID { get; set; }
        public string ProductionOrderNo { get; set; }
        public int InsertedDetails { get; set; }
        public int UpdatedDetails { get; set; }
        public int DeletedDetails { get; set; }
    }
    public class ProductionOrderNoResponse
    {
        public bool Success { get; set; }
        public long ProductionOrderID { get; set; }
        public string ProductionOrderNo { get; set; }
    }
    public class ProductionOrderListItem
    {
        public long ProductionOrderID { get; set; }
        public string ProductionOrderNo { get; set; }
        public DateTime ProductionOrderDate { get; set; }

        public long? JobWorkerID { get; set; }
        public string JobWorkerName { get; set; }

        public decimal Rpm { get; set; }
        public decimal PanaRepeat { get; set; }
        public decimal PanaWidth { get; set; }
        public decimal NoOfMachines { get; set; }
        public decimal JobRate { get; set; }
        public string Traders { get; set; }

        public decimal TotalPcs { get; set; }
        public decimal TotalProductionPcs { get; set; }
        public decimal TotalPendingPcs { get; set; }
        public decimal TotalAmount { get; set; }

        public int Status { get; set; }

        public long? CompanyId { get; set; }
        public long? UserAccountId { get; set; }

        public int DetailCount { get; set; }
    }
    public class ProductionOrderDetailListItem
    {
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderID { get; set; }

        public long? SalesOrderID { get; set; }
        public long? SalesOrderDetailID { get; set; }
        public long? SubDetailID { get; set; }

        public string SalesOrderNo { get; set; }
        public string SlipNo { get; set; }
        public string Barcode { get; set; }

        public string OrderDate { get; set; }

        public long? PartyID { get; set; }
        public string PartyName { get; set; }

        public long? DesignEntryFormID { get; set; }
        public string DesignNo { get; set; }

        public long? ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }

        public string WarpQuality { get; set; }
        public string WeftQuality { get; set; }

        public decimal PCS { get; set; }
        public decimal ProductionPCS { get; set; }
        public decimal PendingPCS { get; set; }

        public decimal SalesRate { get; set; }
        public decimal JobRate { get; set; }
        public decimal Amount { get; set; }

        public decimal Rpm { get; set; }
        public decimal PanaRepeat { get; set; }
        public decimal PanaWidth { get; set; }
        public decimal NoOfMachines { get; set; }

        public decimal RepPCS { get; set; }
        public decimal RepRate { get; set; }
        public decimal RepAmt { get; set; }
        public int JobberAccept { get; set; }

        public long? CompanyId { get; set; }
        public long? UserAccountId { get; set; }
    }
    public class ProductionOrderByIDResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public ProductionOrderListItem Master { get; set; }
        public List<ProductionOrderDetailListItem> Details { get; set; }
    }
    public static class ProductionOrderLockHelper
    {
        public static void AcquireOrderLock(SqlConnection con, SqlTransaction tran, long productionOrderId, int timeoutMs = 10000)
        {
            if (productionOrderId <= 0) return;
            string resourceName = "ProductionOrder_Lock_" + productionOrderId;
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
                    throw new InvalidOperationException(string.Format("Failed to acquire application lock for Production Order {0} (Result: {1}). The order may be in use by another transaction.", productionOrderId, result));
                }
            }
        }

        public static void AcquireOrderLocks(SqlConnection con, SqlTransaction tran, IEnumerable<long> productionOrderIds, int timeoutMs = 10000)
        {
            if (productionOrderIds == null) return;
            var sorted = productionOrderIds.Where(id => id > 0).Distinct().OrderBy(id => id).ToList();
            foreach (var id in sorted)
            {
                AcquireOrderLock(con, tran, id, timeoutMs);
            }
        }

        public static long ResolveProductionOrderId(SqlConnection con, SqlTransaction tran, long orderId, long detailId = 0, long subDetailId = 0, long processDetailId = 0, string slipNo = null)
        {
            if (orderId > 0) return orderId;

            if (detailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ProductionOrderID FROM dbo.ProductionOrderDetail WITH (NOLOCK) WHERE ProductionOrderDetailID = @ID", con, tran))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.BigInt).Value = detailId;
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt64(val);
                }
            }

            if (subDetailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ProductionOrderID FROM dbo.ProductionOrderSubDetail WITH (NOLOCK) WHERE ProductionOrderSubDetailID = @ID", con, tran))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.BigInt).Value = subDetailId;
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt64(val);
                }
            }

            if (processDetailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ProductionOrderID FROM dbo.ProcessTransactionDetail WITH (NOLOCK) WHERE ProcessTransactionDetailID = @ID", con, tran))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.BigInt).Value = processDetailId;
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt64(val);
                }
            }

            if (!string.IsNullOrWhiteSpace(slipNo))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ProductionOrderID FROM dbo.ProductionOrderDetail WITH (NOLOCK) WHERE SlipNo = @SlipNo", con, tran))
                {
                    cmd.Parameters.Add("@SlipNo", SqlDbType.VarChar, 100).Value = slipNo.Trim();
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt64(val);
                }
            }

            return 0;
        }

        public static void ValidateOrderAndDetailActive(SqlConnection con, SqlTransaction tran, long productionOrderId, long productionOrderDetailId = 0, long productionOrderSubDetailId = 0, string slipNo = null)
        {
            if (productionOrderId <= 0 && productionOrderDetailId <= 0 && productionOrderSubDetailId <= 0 && string.IsNullOrWhiteSpace(slipNo)) return;

            if (productionOrderId <= 0)
            {
                productionOrderId = ResolveProductionOrderId(con, tran, productionOrderId, productionOrderDetailId, productionOrderSubDetailId, 0, slipNo);
            }

            if (productionOrderId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT IsActive FROM dbo.ProductionOrderMaster WITH (UPDLOCK, HOLDLOCK) WHERE ProductionOrderID = @OrderID", con, tran))
                {
                    cmd.Parameters.Add("@OrderID", SqlDbType.BigInt).Value = productionOrderId;
                    object val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value)
                    {
                        throw new InvalidOperationException(string.Format("Production Order {0} does not exist.", productionOrderId));
                    }
                    if (!Convert.ToBoolean(val))
                    {
                        throw new InvalidOperationException(string.Format("Production Order {0} is inactive or has been deleted. Downstream operation cannot proceed.", productionOrderId));
                    }
                }
            }

            if (productionOrderDetailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT IsActive FROM dbo.ProductionOrderDetail WITH (UPDLOCK, HOLDLOCK) WHERE ProductionOrderDetailID = @DetailID", con, tran))
                {
                    cmd.Parameters.Add("@DetailID", SqlDbType.BigInt).Value = productionOrderDetailId;
                    object val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value)
                    {
                        throw new InvalidOperationException(string.Format("Production Order Detail {0} does not exist.", productionOrderDetailId));
                    }
                    if (!Convert.ToBoolean(val))
                    {
                        throw new InvalidOperationException(string.Format("Production Order Detail {0} is inactive or has been deleted. Downstream operation cannot proceed.", productionOrderDetailId));
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(slipNo))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 IsActive FROM dbo.ProductionOrderDetail WITH (UPDLOCK, HOLDLOCK) WHERE SlipNo = @SlipNo", con, tran))
                {
                    cmd.Parameters.Add("@SlipNo", SqlDbType.VarChar, 100).Value = slipNo.Trim();
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value && !Convert.ToBoolean(val))
                    {
                        throw new InvalidOperationException(string.Format("Production Order Detail for Slip '{0}' is inactive or has been deleted. Downstream operation cannot proceed.", slipNo.Trim()));
                    }
                }
            }

            if (productionOrderSubDetailId > 0)
            {
                using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(IsActive, 1) FROM dbo.ProductionOrderSubDetail WITH (UPDLOCK, HOLDLOCK) WHERE ProductionOrderSubDetailID = @SubDetailID", con, tran))
                {
                    cmd.Parameters.Add("@SubDetailID", SqlDbType.BigInt).Value = productionOrderSubDetailId;
                    object val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value)
                    {
                        throw new InvalidOperationException(string.Format("Production Order SubDetail {0} does not exist.", productionOrderSubDetailId));
                    }
                    if (!Convert.ToBoolean(val))
                    {
                        throw new InvalidOperationException(string.Format("Production Order SubDetail {0} is inactive or has been deleted. Downstream operation cannot proceed.", productionOrderSubDetailId));
                    }
                }
            }
        }

        public static void CoordinateAndValidate(SqlConnection con, SqlTransaction tran, IEnumerable<ProcessItemInput> items)
        {
            if (items == null) return;
            var list = items.ToList();
            var orderIds = new HashSet<long>();
            foreach (var item in list)
            {
                long resolvedOrderId = ResolveProductionOrderId(con, tran, item.ProductionOrderID, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, 0, item.SlipNo);
                if (resolvedOrderId > 0) orderIds.Add(resolvedOrderId);
            }

            AcquireOrderLocks(con, tran, orderIds);

            foreach (var item in list)
            {
                long resolvedOrderId = ResolveProductionOrderId(con, tran, item.ProductionOrderID, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, 0, item.SlipNo);
                ValidateOrderAndDetailActive(con, tran, resolvedOrderId, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.SlipNo);
            }
        }

        public static void CoordinateAndValidate(SqlConnection con, SqlTransaction tran, IEnumerable<ReceiveItemInput> items)
        {
            if (items == null) return;
            var list = items.ToList();
            var orderIds = new HashSet<long>();
            foreach (var item in list)
            {
                long resolvedOrderId = ResolveProductionOrderId(con, tran, item.ProductionOrderID, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.IssueDetailID, item.SlipNo);
                if (resolvedOrderId > 0) orderIds.Add(resolvedOrderId);
            }

            AcquireOrderLocks(con, tran, orderIds);

            foreach (var item in list)
            {
                long resolvedOrderId = ResolveProductionOrderId(con, tran, item.ProductionOrderID, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.IssueDetailID, item.SlipNo);
                ValidateOrderAndDetailActive(con, tran, resolvedOrderId, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.SlipNo);
            }
        }

        public static void CoordinateAndValidate(SqlConnection con, SqlTransaction tran, IEnumerable<IssueBackItemInput> items)
        {
            if (items == null) return;
            var list = items.ToList();
            var orderIds = new HashSet<long>();
            foreach (var item in list)
            {
                long resolvedOrderId = ResolveProductionOrderId(con, tran, item.ProductionOrderID, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.ReceiveDetailID, item.SlipNo);
                if (resolvedOrderId > 0) orderIds.Add(resolvedOrderId);
            }

            AcquireOrderLocks(con, tran, orderIds);

            foreach (var item in list)
            {
                long resolvedOrderId = ResolveProductionOrderId(con, tran, item.ProductionOrderID, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.ReceiveDetailID, item.SlipNo);
                ValidateOrderAndDetailActive(con, tran, resolvedOrderId, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.SlipNo);
            }
        }

        public static void CoordinateAndValidate(SqlConnection con, SqlTransaction tran, IEnumerable<JobberReceiveItemInput> items)
        {
            if (items == null) return;
            var list = items.ToList();
            var orderIds = new HashSet<long>();
            foreach (var item in list)
            {
                long resolvedOrderId = ResolveProductionOrderId(con, tran, item.ProductionOrderID, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.IssueBackDetailID, item.SlipNo);
                if (resolvedOrderId > 0) orderIds.Add(resolvedOrderId);
            }

            AcquireOrderLocks(con, tran, orderIds);

            foreach (var item in list)
            {
                long resolvedOrderId = ResolveProductionOrderId(con, tran, item.ProductionOrderID, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.IssueBackDetailID, item.SlipNo);
                ValidateOrderAndDetailActive(con, tran, resolvedOrderId, item.ProductionOrderDetailID, item.ProductionOrderSubDetailID, item.SlipNo);
            }
        }
    }

    public class ProductionOrderDeleteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long ProductionOrderID { get; set; }
    }
}