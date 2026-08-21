using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class FinancialYearMaster
    {
        public int FinancialYearID { get; set; }
        public int FinancialYearIDs { get; set; }
        public string FinancialYear { get; set; }
        public string AssessmentYear { get; set; }
        public DateTime StartDate { get; set; }
        public string StartDates { get; set; }
        public DateTime EndDate { get; set; }
        public string EndDates { get; set; }
        public int IsDefault { get; set; }

        public string IsDefaultCss { get; set; }
        public string IsDefaults { get; set; }
    }
}