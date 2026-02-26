using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnOutwardDetail
    {
        public int YarnOutwardDetailID { get; set; }
        public int YarnOutwardID { get; set; }
        public int YarnInwardDetailID { get; set; }
        public int BillToPartyID { get; set; }
        public int YarnInwardID { get; set; }
        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public DateTime DateAndTime { get; set; }
        public string DateAndTimes { get; set; }
        public string YarnMaterial { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public string CompanyName { get; set; }
        public string CompanyMobileNo { get; set; }
        public string BoxNo { get; set; }
        public string BarcodeNo { get; set; }
        public string GodownTitle { get; set; }
        public string GodownAddress { get; set; }
        public string LocationTitle { get; set; }
        public decimal NetWeight { get; set; }
        public decimal NoOfBox { get; set; }
        public int GodownLocationID { get; set; }

    }
}