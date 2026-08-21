using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class UserAccountMaster
    {
        public int UserAccountId { get; set; }
        public DateTime UserAccountDateAndTime { get; set; }
        public string UserAccountDateAndTimes { get; set; }
        public string UserAccountName { get; set; }
        public string UserRole { get; set; }
        public string UserAccountMobileNo { get; set; }
        public string UserAccountEmail { get; set; }
        public string UserAccountProfile { get; set; }
        public string UserAccountPassword { get; set; }
        public string AllowLogin { get; set; }



        public string CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string CompState { get; set; }
        public string FinancialYearID { get; set; }
    }
}