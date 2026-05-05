using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnPOMaster
    {
        public int YarnPOID { get; set; }
        public int YarnMaterialID { get; set; }
        public int YarnPODetailID { get; set; }
        public DateTime DateandTime { get; set; }
        public string YarnMaterial { get; set; }
        public string CompanyCode { get; set; }
        public string DateandTimes { get; set; }
        public Int64 UserAccountId { get; set; }
        public Int64 CompanyId { get; set; }
        public Int32 FinancialYearID { get; set; }
        public DateTime PODate { get; set; }
        public string PODates { get; set; }
        public Int32 PONo { get; set; }
        public Int64 SupplierPartyID { get; set; }
        public Int64 BillToPartyID { get; set; }
        public int IsSameAsSupplier { get; set; }
        public Int64 CompanyPartyID { get; set; }
        public int IsSameAsCompany { get; set; }
        public Int64 ShippedToPartyID { get; set; }
        public Int64 GodownID { get; set; }
        public string GodownTitle { get; set; }
        public string GodownAddress { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string DeliveryTime { get; set; }
        public string PaymentCondition { get; set; }
        public string NotesRemarks { get; set; }
        public string DeleteYarnDetailsArray { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal PendingWeight { get; set; }
        public string SupplierPartyName { get; set; }
        public string SupplierMobileNo { get; set; }
        public string SupplierAlterMobileNo { get; set; }
        public string SupplierEmail { get; set; }
        public string SupplierBillingAddress { get; set; }
        public string SupplierShippingAddress { get; set; }
        public string SupplierGSTIN { get; set; }
        public string SupplierPanNo { get; set; }
        public string SupplierState { get; set; }
        public string SupplierCity { get; set; }
        public string SupplierPincode { get; set; }
        public string SupplierBankHolderName { get; set; }
        public string SupplierBankAccountNo { get; set; }
        public string SupplierBankIFSCCode { get; set; }
        public string SupplierBankName { get; set; }
        public string SupplierBankBranch { get; set; }
        public string CompanyPartyName { get; set; }
        public string CompanyMobileNo { get; set; }
        public string CompanyAlterMobileNo { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyBillingAddress { get; set; }
        public string CompanyShippingAddress { get; set; }
        public string CompanyGSTIN { get; set; }
        public string CompanyPanNo { get; set; }
        public string CompanyState { get; set; }
        public string CompanyCity { get; set; }
        public string CompanyPincode { get; set; }
        public string CompanyBankHolderName { get; set; }
        public string CompanyBankAccountNo { get; set; }
        public string CompanyBankIFSCCode { get; set; }
        public string CompanyBankName { get; set; }
        public string CompanyBankBranch { get; set; }
        public string BillToPartyName { get; set; }
        public string BillToMobileNo { get; set; }
        public string BillToAlterMobileNo { get; set; }
        public string BillToEmail { get; set; }
        public string BillToBillingAddress { get; set; }
        public string BillToShippingAddress { get; set; }
        public string BillToGSTIN { get; set; }
        public string BillToPanNo { get; set; }
        public string BillToState { get; set; }
        public string BillToCity { get; set; }
        public string BillToPincode { get; set; }
        public string BillToBankHolderName { get; set; }
        public string BillToBankAccountNo { get; set; }
        public string BillToBankIFSCCode { get; set; }
        public string BillToBankName { get; set; }
        public string BillToBankBranch { get; set; }
        public string ShippedToPartyName { get; set; }
        public string ShippedToMobileNo { get; set; }
        public string ShippedToAlterMobileNo { get; set; }
        public string ShippedToEmail { get; set; }
        public string ShippedToBillingAddress { get; set; }
        public string ShippedToShippingAddress { get; set; }
        public string ShippedToGSTIN { get; set; }
        public string ShippedToPanNo { get; set; }
        public string ShippedToState { get; set; }
        public string ShippedToCity { get; set; }
        public string ShippedToPincode { get; set; }
        public string ShippedToBankHolderName { get; set; }
        public string ShippedToBankAccountNo { get; set; }
        public string ShippedToBankIFSCCode { get; set; }
        public string ShippedToBankName { get; set; }
        public string ShippedToBankBranch { get; set; }
        public string YarnPOArray { get; set; }
        public string SearchRequirementNo { get; set; }
        public string startFrom { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public int YarnColorID { get; set; }        
        public decimal Qty { get; set; }
        public decimal PendingQty { get; set; }
        public decimal Rate { get; set; }
        public int EditableOrNot { get; set; }
        public bool IsComplete { get; set; } 
        public List<YarnPODetails> listYarnPODetails { get; set; }
    }
}