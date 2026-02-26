using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnInwardDetail
    {
        public int YarnInwardDetailID { get; set; }
        public DateTime DateAndTime { get; set; }
        public string DateAndTimes { get; set; }
        public string BoxNo { get; set; }
        public string BarcodeNo { get; set; }
        public decimal NetWeight { get; set; }
        public int GodownLocationID { get; set; }
        public string LocationTitle { get; set; }
        public int YarnInwardID { get; set; }
    }
}