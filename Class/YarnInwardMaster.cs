using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnInwardMaster
    {

        public int YarnInwardID { get; set; }
        public DateTime DateAndTime { get; set; }
        public string DateAndTimes { get; set; }
        public DateTime YarnInwardDate { get; set; }
        public string YarnInwardDates { get; set; }
        public int ChallanNo { get; set; }
        public DateTime ChallanDate { get; set; }
        public string ChallanDates { get; set; }
        public string PartyChallanNo { get; set; }
        public int YarnPOID { get; set; }
        public int YarnPoNo { get; set; }
        public string ShippedToName { get; set; }
        public string SupplierName { get; set; }
        public string BillToPartyName { get; set; }
        public string ShippedtoAddress { get; set; }
        public string MobileNo { get; set; }
        public string CompanyName { get; set; }
        public int YarnMaterialID { get; set; }
        public string YarnMaterial { get; set; }
        public string YarnColor { get; set; }
        public string YarnPONo { get; set; }
        public string ColourCode { get; set; }
        public int YarnColorID { get; set; }
        public int YarnPODetailIDCompanyCode { get; set; }
        public string LotNo { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal PendingWeight { get; set; }
        public decimal POPendingWeight { get; set; }
        public int GodownID { get; set; }
        public int IsComplete { get; set; }
        public string YarnGodownTitle { get; set; }
        public string GODOWNCHANGE { get; set; }
        public string GodownTitle { get; set; }
        public string GodownAddress { get; set; }
        public int CompanyId { get; set; }
        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public string YarnInwardArray { get; set; }
        public string SearchRequirementNo { get; set; }
        public string startFrom { get; set; }
        public string PhotoOfInward { get; set; }
        public string isUploadPhoto { get; set; }
        public string DeleteYarnInwardDetail { get; set; }
        public int PartyId { get; set; }


        public string YarnColorCode { get; set; }
        public string BoxNo { get; set; }
        public string BarcodeNo { get; set; }
        public decimal NetWeight { get; set; }
        public Int32 YarnInwardDetailID { get; set; }
        public Int32 TotalBarcode { get; set; }

        public List<YarnInwardDetail> listYarnInwardDetail { get; set; }

    }
}