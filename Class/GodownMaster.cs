using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class GodownMaster
    {
        public int GodownID { get; set; }
        public DateTime DateAndTime { get; set; }
        public string DateAndTimes { get; set; }
        public string GodownTitle { get; set; }
        public string GodownAddress { get; set; }
        public string GodownArray { get; set; }
        public string DeleteGodownArray { get; set; }
        public int IsDefault { get; set; }
        public int IsActive { get; set; }
        public int IsManual { get; set; }
        public int CompanyId { get; set; }
        public int UserAccountId { get; set; }

        public int GodownLocationID { get; set; }
        public string LocationTitle { get; set; }
        public string BoxNo { get; set; }
        public string NetWeight { get; set; }
        public string searchGodown { get; set; }
        public string startFrom { get; set; }
        public string GSTIN { get; set; }

        public List<GodownLocationMaster> listGodownLocationMaster { get; set; }

    }
}