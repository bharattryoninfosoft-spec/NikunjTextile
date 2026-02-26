using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnRequirementMaster
    {
        public int YarnRequirementID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public int UserAccountId { get; set; }
        public int CompanyId { get; set; }
        public int FinancialYearID { get; set; }
        public DateTime RequirementDate { get; set; }
        public string RequirementDates { get; set; }
        public int RequirementNo { get; set; }
        public string SearchRequirementNo { get; set; }
        public string startFrom { get; set; }
        public int PartyId { get; set; } 
        public string YarnRequirementArray { get; set; }
        public string DeleteYarnDetailsArray { get; set; }


        public string PartyName { get; set; }
        public string MobileNo { get; set; }
        public string AlterMobileNo { get; set; }
        public string Email { get; set; }
        public string BillingAddress { get; set; }
        public string ShippingAddress { get; set; }
        public string GSTIN { get; set; }
        public string PanNo { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Pincode { get; set; }
        public string BankHolderName { get; set; }
        public string BankAccountNo { get; set; }
        public string BankIFSCCode { get; set; }
        public string BankName { get; set; }
        public string BankBranch { get; set; }
        public string CompanyColourCode { get; set; }
        public string CompanyName { get; set; }



        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        
        public List<YarnRequirementDetail> listYarnRequirementDetail { get; set; }

    }
}