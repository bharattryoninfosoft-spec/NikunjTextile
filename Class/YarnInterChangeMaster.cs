using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnInterChangeMaster
    {

        public DateTime DateAndTime { get; set; }
        public string DateAndTimes { get; set; }
        public int YarnInwardID { get; set; }
        public int YarnInterchangeID { get; set; }
        public int YarnPOID { get; set; }
        public int YarnMaterialID { get; set; }
        public int YarnInwardDetailID { get; set; }
        public int YarnColorID { get; set; }
        public int GodownID { get; set; }
        public string GodownTitle { get; set; }
        public string GodownAddress { get; set; }
        public int GodownLocationID { get; set; }
        public int YarnPODetailID { get; set; }
        public decimal NetWeight { get; set; }
        public string PartyName { get; set; }
        public string YarnMaterial { get; set; }
        public string YarnColorCode { get; set; }
        public string YarnColor { get; set; }
        public string BoxNo { get; set; }
        public string CompanyCode { get; set; }
        public string LocationTitle { get; set; }
        public string MOVEBOXORNOT { get; set; }

    }
}