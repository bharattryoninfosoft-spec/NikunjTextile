using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class CommanResponse
    {
        public int id { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string PODate { get; set; }
    }
    public class UpdateBoxWeightResponse
    {
        public int id { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string PODate { get; set; }
        public int YarnInwardDetailID { get; set; }
        public string BoxNo { get; set; }
        public decimal NetWeight { get; set; }
        public string BarcodeNo { get; set; }
        public int GodownLocationID { get; set; }
        public int YarnInwardID { get; set; }
    }

}