using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class GstSlabMaster
    {
        public int GSTSLABId { get; set; }
        public DateTime date { get; set; }

        public string dates { get; set; }
        public string CompanyName { get; set; }
        public string GSTSLABName { get; set; }
        public int CompanyId { get; set; }
        public decimal GST { get; set; }
        public decimal IGST { get; set; }
        public decimal CESS { get; set; }
        public int UserAccountId { get; set; }
    }
}