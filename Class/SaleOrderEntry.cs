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
}