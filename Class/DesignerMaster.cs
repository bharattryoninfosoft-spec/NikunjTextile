using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class DesignerMaster
    {
        public int DesignerID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public string DesignerCode { get; set; }
        public string DesignerName { get; set; }
        public string Address { get; set; }
        public string BankName { get; set; }
        public string BankAcNo { get; set; }
        public string IFSCCode { get; set; }
        public string AadharUpload { get; set; }
        public int CompanyId { get; set; }
        public int IsActive { get; set; }
        public int Position { get; set; }
        public string CompanyName { get; set; }
        public int UserAccountId { get; set; }
        public string UserAccountName { get; set; }
        public string ProductSrNo { get; set; }
        public string searchDesigner { get; set; }
        public string startFrom { get; set; }
    }
}