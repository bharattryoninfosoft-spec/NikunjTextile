using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class LocationMaster
    {
        public int LocationID { get; set; }
        public DateTime DateAndTime { get; set; }
        public string DateAndTimes { get; set; }
        public string LocationTitle { get; set; }
        public string LocationAddress { get; set; }
        public int IsActive { get; set; }
        public int CompanyId { get; set; }
        public int UserAccountId { get; set; }
    }
}