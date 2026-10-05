using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class DesignEntryForm
    {
        public string DesignEntryFormArray { get; set; }
        public string tempPhotoOfDesign { get; set; }
        public string tempPhotoOfSketch { get; set; }

        public Int64 DesignEntryFormID { get; set; }
        public Int64 DesignColorMatchingFormID { get; set; }
        public Int64 DesignColorMatchingFormIDs { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public Int32 UserAccountId { get; set; }
        public string UserAccountName { get; set; }
        public Int32 CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int FinancialYearID { get; set; }
        public string PhotoOfDesign { get; set; }
        public string PhotoOfSketch { get; set; }
        public int WarpQualityID { get; set; }
        public string WarpQuality { get; set; }
        public DateTime DesignDate { get; set; }
        public string DesignDates { get; set; }
        public int DesignerID { get; set; }
        public int SketcherID { get; set; }
        public string Sketcher { get; set; }
        public string DesignerCode { get; set; }
        public string DesignNo { get; set; }

        public string spanExstingDesignerCode { get; set; }
        public string ExstingDesignNo { get; set; }

        public string DesignerName { get; set; }
        public string Remark { get; set; }
<<<<<<< HEAD
        public int PickOnLoom { get; set; }
=======
        public string PickOnLoom { get; set; }
        public string HSNCode { get; set; }
>>>>>>> origin/master
        public decimal TotalCard { get; set; }
        public decimal AveragePic { get; set; }
        public string ReedOnLoom { get; set; }
        public int TypeID { get; set; }
        public string Type { get; set; }
        public int MaterialID { get; set; }
        public int DesignCategoryID { get; set; }
        public string DesignerCodeDesignerCode { get; set; }
        public string Material { get; set; }
        public string DesignCategory { get; set; }
        public string DesignCut { get; set; }
        public string SearchDesignNo { get; set; }
        public string startFrom { get; set; }
        public decimal SaleRate { get; set; }
        public string BarcodeNo { get; set; }
        public List<DesignEntryWeftData> listDesignEntryWeftData { get; set; }
        public List<DesignFormWarpData> listDesignFormWarpData { get; set; }
        public string UnitCode { get; set; }
        public int UnitId { get; set; }
<<<<<<< HEAD


=======
>>>>>>> origin/master
    }
    public class DesignSelectionModel
    {
        public int DesignEntryFormID { get; set; }
        public Int64 DesignColorMatchingFormIDs { get; set; }
        public int DesignColorMatchingFormID { get; set; }
        public string DesignNo { get; set; }
        public string DesignerCode { get; set; }
        public int TypeID { get; set; }
        public string Type { get; set; }
        public string UnitCode { get; set; }
        public int UnitId { get; set; }
        public int ColorGroupID { get; set; }
        public string ColorGroup { get; set; }
    }
    public class DesignEntryDataFormResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<DesignModel> Designs { get; set; }
        public List<TypeModel> Types { get; set; }
        public List<UnitModel> Units { get; set; }
        public List<ColorGroupModel> ColorGroups { get; set; }
    }
    public class DesignEntryDataResponse
    {
        public List<DesignModel> Designs { get; set; }
        public List<TypeModel> Types { get; set; }
        public List<UnitModel> Units { get; set; }
        public List<ColorGroupModel> ColorGroups { get; set; }
    }

    public class TypeModel
    {
        public int TypeID { get; set; }
        public string Type { get; set; }
    }

    public class UnitModel
    {
        public int UnitId { get; set; }
        public string UnitCode { get; set; }
    }
    public class DesignColorMatchingDetailsModel
    {
        public int DesignColorMatchingDetailsID { get; set; }
        public int MatchingNo { get; set; }
        public string MatchingID { get; set; }
        public string WarpMatchingID { get; set; }
        public string FeederMatchingID { get; set; }
        public string ColorMatchingPhoto { get; set; }
        public int ColorGroupID { get; set; }
    }
    public class ColorGroupModel
    {
        public int ColorGroupID { get; set; }
        public string ColorGroup { get; set; }
    }
    public class DesignModel
    {
        public int DesignEntryFormID { get; set; }
        public int DesignColorMatchingFormID { get; set; }

        public string DesignNo { get; set; }
        public string DesignerCode { get; set; }

        public int TypeID { get; set; }
        public string Type { get; set; }

        public int UnitId { get; set; }
        public string UnitCode { get; set; }

        public decimal SaleRate { get; set; }
        public string BarcodeNo { get; set; }

        // ✅ MULTI VALUE SUPPORT
        public List<DesignColorMatchingDetailsModel> DesignColorMatchingDetails { get; set; }
        public List<ColorGroupModel> ColorGroups { get; set; }
    }
    public class DesignMasterModel
    {
        public int DesignEntryFormID { get; set; }
        public int DesignColorMatchingDetailsID { get; set; }
        public string DesignNo { get; set; }
        public string DesignerCode { get; set; }
        public string BarcodeNo { get; set; }
    }
}