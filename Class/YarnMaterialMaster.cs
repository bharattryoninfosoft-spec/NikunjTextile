using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnMaterialMaster
    {
        public int YarnMaterialID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public string YarnMaterial { get; set; }
        public string SearchYarnMaterial { get; set; }
        public string Denier { get; set; }
        public string HSNCode { get; set; }
        public int UnitId { get; set; }
        public string Unit { get; set; }
        public int GSTSLABId { get; set; }
        public string GSTSLABName { get; set; }
        public int CompanyId { get; set; }
        public int IsActive { get; set; }
        public int Position { get; set; }
        public string CompanyName { get; set; }
        public int UserAccountId { get; set; }
        public string UserAccountName { get; set; }
        public string ProductSrNo { get; set; }
        public string startFrom { get; set; }
    }
}