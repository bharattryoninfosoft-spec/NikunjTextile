using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class TotalInwardStockArray
    {
        public int BillToPartyID { get; set; }
        public string CompanyName { get; set; }
        public int YarnMaterialID { get; set; }
        public string YarnMaterial { get; set; }
        public int YarnColorID { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public int Stock { get; set; }
        public int GodownID { get; set; }
    }
}