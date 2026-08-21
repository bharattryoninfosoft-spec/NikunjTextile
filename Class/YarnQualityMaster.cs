using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnQualityMaster
    {
        public int YarnQualityID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public string YarnQuality { get; set; }
        public int CompanyId { get; set; }
        public int IsActive { get; set; }
        public int Position { get; set; }
        public string CompanyName { get; set; }
        public int UserAccountId { get; set; }
        public string UserAccountName { get; set; }
        public string ProductSrNo { get; set; }

    }
}