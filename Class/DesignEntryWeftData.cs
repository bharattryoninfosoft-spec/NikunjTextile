using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class DesignEntryWeftData
    {
        public Int64 DesignEntryWeftID { get; set; }
        public Int64 DesignEntryFormID { get; set; }
        public decimal Card { get; set; }
        public decimal Pick { get; set; }
        public int YarnMaterialID { get; set; }
        public int DesignSpecificationID { get; set; }
        public string YarnMaterial { get; set; }
        public string DesignSpecification { get; set; }
    }
}