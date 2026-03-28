using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string Remark { get; set; }
        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int CompanyId { get; set; }

        public List<SaleOrderDetailModel> Details { get; set; }
    }
    public class SaleOrderDetailModel
    {
        public int DetailID { get; set; }
        public int SaleOrderID { get; set; }
        public string DesignID { get; set; }
        public string DesignNo { get; set; }
        public string ItemType { get; set; }
        public int NoOfColours { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
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
    }
    public class SaleOrderVM
    {
        public int SaleOrderID { get; set; }
        public string OrderNo { get; set; }
        public string OrderDate { get; set; }
        public decimal TotalQty { get; set; }
        public decimal InvoiceAmount { get; set; }

        public List<SaleOrderDetailVM> Details { get; set; } = new List<SaleOrderDetailVM>();
    }

    public class SaleOrderDetailVM
    {
        public int DetailID { get; set; }
        public string DesignNo { get; set; }
        public string ItemType { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }

        public List<SaleOrderSubVM> SubDetails { get; set; } = new List<SaleOrderSubVM>();
    }

    public class SaleOrderSubVM
    {
        public int ColourID { get; set; }
        public string ColourName { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
    }
}