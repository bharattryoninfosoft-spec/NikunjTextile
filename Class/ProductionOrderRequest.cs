using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ProductionOrderDeleteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long ProductionOrderID { get; set; }
    }
}