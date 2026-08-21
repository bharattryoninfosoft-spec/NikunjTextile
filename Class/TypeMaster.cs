using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class TypeMaster
    {
        public int TypeID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public string Type { get; set; }
        public int CompanyId { get; set; }
        public int IsActive { get; set; }
        public int Position { get; set; }
        public string CompanyName { get; set; }
        public int UserAccountId { get; set; }
        public string UserAccountName { get; set; }
        public string ProductSrNo { get; set; }
        public string searchTyre { get; set; }
        public string startFrom { get; set; }
        public int UnitId { get; set; }
    }
}