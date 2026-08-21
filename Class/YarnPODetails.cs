using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnPODetails
    {
        public Int64 YarnPODetailID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public Int64 UserAccountId { get; set; }
        public Int64 YarnPOID { get; set; }
        public int YarnMaterialID { get; set; }
        public string YarnMaterial { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public int YarnColorID { get; set; }
        public string CompanyCode { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public Int64 GSTSLABId { get; set; }
        public string GSTSLABName { get; set; }
        public decimal Amount { get; set; }

    }
}