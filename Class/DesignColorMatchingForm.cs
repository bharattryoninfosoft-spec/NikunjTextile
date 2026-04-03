using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class DesignColorMatchingForm
    {
        public Int64 DesignColorMatchingFormID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public int UserAccountId { get; set; }
        public int CompanyId { get; set; }
        public int FinancialYearID { get; set; }
        public Int64 DesignEntryFormID { get; set; }
        public string DesignEntryFormIDs { get; set; }
        public int TotalWarp { get; set; }
        public int TotalWeft { get; set; }
        public string ColorArray { get; set; }
        public string DesignColorMatchingArray { get; set; }
        public string ColorFormArray { get; set; }
        public string DeleteColorArray { get; set; }
        public string MatchingArray { get; set; }
        public string PhotoOfDesign { get; set; }
        public int DesignerID { get; set; }
        public string DesignNo { get; set; }
        public int PickOnLoom { get; set; }
        public double TotalCard { get; set; }
        public string DesignerCode { get; set; }
        public string ReedOnLoom { get; set; }
        public string WarpQuality { get; set; }
        public string MatchingID { get; set; }
        public string WarpMatchingID { get; set; }
        public string FeederMatchingID { get; set; }
        public string SearchDesignNo { get; set; }
        public string DesignCategory { get; set; }
        public string WarpCheckBox { get; set; }
        public string WeftCheckBox { get; set; }
        public string startFrom { get; set; }
        public decimal SaleRate { get; set; }
        public List<DesignEntryWeftData> listDesignEntryWeftData { get; set; }
        public List<DesignFormWarpData> listDesignFormWarpData { get; set; }
        public List<DesignColorMatchingDetails> listDesignColorMatchingDetails { get; set; }

    }
}