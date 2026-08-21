using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class GodownManager
    {
        public int GodownManagerID { get; set; }
        public DateTime DateAndTime { get; set; }
        public string DateandTimes { get; set; }
        public string GodownManagerName { get; set; }
        public string MobileNo { get; set; }
        public string tempMobileNo { get; set; }
        public string AlterMobileNo { get; set; }
        public string Email { get; set; }
        public string ManagerAddress { get; set; }
        public int IsActiveManager { get; set; }
        public int UserAccountId { get; set; }
        public string startFrom { get; set; }
    }
}