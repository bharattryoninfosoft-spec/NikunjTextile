using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class JobberMasterClass
    {
        public int JobberMasterID { get; set; }

        public string JobberName { get; set; }

        public string Rpm { get; set; }

        public string PanaRepeat { get; set; }

        public string PanaWidth { get; set; }

        public string NoOfMachines { get; set; }
    }
    public class ProductionDesignFormWarp
    {
        public int DesignEntryWarpID { get; set; }

        public int YarnQualityMaterialID { get; set; }

        public int Tar { get; set; }

        public string YarnMaterial { get; set; }

        public string Color { get; set; }

        public string WarpPattern { get; set; }
    }

}