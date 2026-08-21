using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class DesignFormWarpData
    {
        public Int64 DesignEntryWarpID { get; set; }
        public Int64 DesignEntryFormID { get; set; }
        public Int64 YarnQualityMaterialID { get; set; }
        public int YarnQualityID { get; set; }
        public string YarnQuality { get; set; }
        public int YarnColorID { get; set; }
        public string YarnColor { get; set; }
        public string YarnMaterial { get; set; }
        public int Tar { get; set; }
    }
}