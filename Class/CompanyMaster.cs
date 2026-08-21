using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class CompanyMaster
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string MobileNo { get; set; }
        public string AlterMobileNo { get; set; }
        public string Email { get; set; }
        public string BusinessAddress { get; set; }
        public string GSTIN { get; set; }


        public string State { get; set; }
        public string StateTitle { get; set; }
        public string BusinessType { get; set; }
        public string BusinessCategory { get; set; }
        public string BusinessDescription { get; set; }

        public string BankName { get; set; }
        public string ACNo { get; set; }
        public string IFSC { get; set; }
        public string CompanyLogo { get; set; }

        public string Sign { get; set; }
        public string is_default { get; set; }
        public string BarcodeTitle { get; set; }
        public string SetCreditPercentage { get; set; }
        public string SupportNumber { get; set; }
    }
}