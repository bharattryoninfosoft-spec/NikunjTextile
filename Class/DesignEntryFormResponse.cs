using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class DesignEntryFormResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public int IsJobWork { get; set; }
        public List<DesignEntryForm> listDesignEntryForm { get; set; }
        public List<DesignerMaster> listDesignerMaster { get; set; }
        public List<DesignSelectionModel> DesignSelectionModel { get; set; }
        public List<MaterialMaster> listMaterialMaster { get; set; }
        public List<SketcherMaster> listSketcherMaster { get; set; }
        public List<TypeMaster> listTypeMaster { get; set; }
        public List<WarpQualityMaster> listWarpQualityMaster { get; set; }
        public List<YarnColorMaster> listYarnColorMaster { get; set; }
        public List<YarnMaterialMaster> listYarnMaterialMaster { get; set; }
        public List<YarnQualityMaster> listYarnQualityMaster { get; set; }
        public List<DesignCategoryMaster> listDesignCategoryMaster { get; set; }
        public List<DesignSpecificationMaster> listDesignSpecificationMaster { get; set; }
        public List<YarnRequirementMaster> listYarnRequirementMaster { get; set; }
        public List<GstSlabMaster> listGstSlabMaster { get; set; }
        public List<YarnPOMaster> listYarnPOMaster { get; set; }
        public List<LocationMaster> listLocationMaster { get; set; }
        public List<GodownMaster> listGodownMaster { get; set; }
        public List<YarnInwardMaster> listYarnInwardMaster { get; set; }


    }
}