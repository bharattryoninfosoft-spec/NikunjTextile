using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnColorMaster
    {
        public int YarnColorID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public int CompanyId { get; set; }
        public int IsActive { get; set; }
        public int Position { get; set; }
        public string CompanyName { get; set; }
        public int UserAccountId { get; set; }
        public string UserAccountName { get; set; }
        public string ProductSrNo { get; set; }
        public string SearchYarnColor { get; set; }
        public string startFrom { get; set; }

    }
}