using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnOutwardMaster
    {
        public int YarnOutwardScanID { get; set; }
        public int YarnOutwardID { get; set; }
        public DateTime DateAndTime { get; set; }
        public DateTime ChallanDate { get; set; }
        public string DateAndTimes { get; set; }
        public string OutwardListNo { get; set; }
        public DateTime OutwardListDate { get; set; }
        public string OutwardListDates { get; set; }
        public string siftYarnInwardDetailID { get; set; }
        public int GodownManagerUserAccountId { get; set; }
        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int CompanyId { get; set; }
        public string SearchRequirementNo { get; set; }
        public string UserAccountName { get; set; }
        public string PartyName { get; set; }
        public string MobileNo { get; set; }
        public string UserAccountMobileNo { get; set; }
        public string startFrom { get; set; }
        public string SundryParty { get; set; }


        public int Stock { get; set; }
        public int PartyId { get; set; }
        public int GodownID { get; set; }
        public int BillToPartyID { get; set; }
        public string CompanyName { get; set; }
        public int YarnMaterialID { get; set; }
        public string YarnMaterial { get; set; }
        public int YarnColorID { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public int NoOfBox { get; set; }
        



        public int YarnInwardDetailID { get; set; }
        
        public string BoxNo { get; set; }
        public decimal NetWeight { get; set; }
        public string BarcodeNo { get; set; }
        public int GodownLocationID { get; set; }
        public int YarnInwardID { get; set; }
        public string GodownTitle { get; set; }
        public string LocationTitle { get; set; }


        public string YarnInwardDetailIDss { get; set; }
        public string GodownInputBoxss { get; set; }
        public string SiftGodownLocationId { get; set; }
        public string siftGodownInputBox { get; set; }
        public string Address { get; set; }
        public int ChallanNo { get; set; }
         
        public string GeneratedLink { get; set; }

        public string updatesiftGodownInputBox { get; set; }
        //public string strBillToPartyID { get; set; }
        //public string strYarnMaterialID { get; set; }
        //public string strYarnColorID { get; set; }
        //public string strInpuNoOfBox { get; set; }


        public List<YarnOutwardDetail> listYarnOutwardDetail { get; set; }
    }
    public class YarnOutwardMasterModel
    {
        public int YarnOutwardID { get; set; }
        public DateTime DateAndTime { get; set; }
        public int GodownID { get; set; }
        public int PartyId { get; set; }
        public int OutwardListNo { get; set; }
        public DateTime OutwardListDate { get; set; } 
        public int GodownManagerUserAccountId { get; set; }
        public Boolean IsScanStutas { get; set; }
        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int SundryPartyId { get; set; }
        public int CompanyId { get; set; }
    }

    public class YarnOutwardDetailModel
    {
        public int YarnOutwardDetailID { get; set; }
        public DateTime DateAndTime { get; set; }
        public int YarnOutwardID { get; set; }
        public int BillToPartyID { get; set; }
        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public string YarnColorCode { get; set; }
        public int GodownLocationID { get; set; }
        public int NoOfBox { get; set; }
        public int UserAccountId { get; set; }
        public int YarnRequirementDetailID { get; set; }
        public DateTime YarnRequirementDateTime { get; set; }
        public int YarnCompanyID { get; set; }


    }
    public class YarnOutwardEditResponse
    {
        public YarnOutwardMasterModel Master { get; set; }
        public List<YarnOutwardDetailModel> Detail { get; set; }
    }
    public class UpdateYarnOutwardMasterModel
    {
        public int YarnOutwardID { get; set; }
        public DateTime DateAndTime { get; set; }
        public int GodownID { get; set; }
        public int PartyId { get; set; }
        public int OutwardListNo { get; set; }
        public string OutwardListDate { get; set; }
        public int GodownManagerUserAccountId { get; set; }
        public Boolean IsScanStutas { get; set; }
        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string YarnMaterial { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public string LocationTitle { get; set; }
        public int Stock { get; set; }
        public int YarnOutwardDetailID { get; set; }
        public int BillToPartyID { get; set; }
        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public int GodownLocationID { get; set; }
        public int NoOfBox { get; set; }
        public int YarnRequirementDetailID { get; set; }
        public DateTime YarnRequirementDateTime { get; set; }
    }
    public class JobberOutwardMasterDTO
    {
        public int PartyId { get; set; }
        public string OutwardListNo { get; set; }
        public string OutwardListDate { get; set; }
        public decimal TotalWeight { get; set; } // Added this property to match front-end payload
        public int YarnPOID { get; set; }        
        public string JobberOutwardEncryptedID { get; set; }
        public int JobberOutwardID { get; set; }
        public List<JobberOutwardDetailDTO> Details { get; set; }
    }

    public class JobberOutwardDetailDTO
    {
        public int GodownID { get; set; }
        public int JobberInwardDetailID { get; set; }
        public int JobberInwardID { get; set; }
        public int BillToPartyId { get; set; }
        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public int NoOfBox { get; set; }
        public decimal NetWeight { get; set; }
        public string BarcodeNo { get; set; }
        public string BoxNo { get; set; }         
        public int YarnPOID { get; set; }
        public int CompanyPartyId{get;set;}
        public string DetailsJson { get; set; }
        public string JobberOutwardEncryptedID { get; set; }
    }

    public class ServiceResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    public class PartyDetailsDTO
    {
        public int PartyId { get; set; }
        public string PartyName { get; set; }
        public string BillingAddress { get; set; }
    }
    public class JobberOutwardMasterModel
    {
        public int JobberOutwardID { get; set; }
        public string JobberOutwardEncryptedID { get; set; }
        public int PartyId { get; set; }
        public string OutwardListNo { get; set; }
        public string OutwardListDate { get; set; }
        public decimal TotalWeight { get; set; }
        public List<JobberOutwardDetailModel> Details { get; set; }
    }


    public class JobberOutwardDetailModel
    {
        public int JobberOutwardDetailID { get; set; }

        public int GodownID { get; set; }
        public int JobberInwardDetailID { get; set; }
        public int JobberInwardID { get; set; }
        public int BillToPartyId { get; set; }
        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public int NoOfBox { get; set; }
        public decimal NetWeight { get; set; }
        public string BarcodeNo { get; set; }
        public string BoxNo { get; set; }
        public int YarnPOID { get; set; }
    }
}