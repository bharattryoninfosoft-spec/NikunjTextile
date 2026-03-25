using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class BrokerMaster
    {
        public int? BrokerId { get; set; }
        public DateTime DateAndTime { get; set; }
        public string BrokerCode { get; set; }
        public string BrokerName { get; set; }
        public string FirmName { get; set; }
        public string Email { get; set; }
        public string MobileNo { get; set; }
        public string Address { get; set; }
        public string CityName { get; set; }
        public string PANCard { get; set; }
        public string GSTNo { get; set; }
        public decimal CommissionRate { get; set; }
        public bool IsActive { get; set; }
        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int CompanyId { get; set; }
    }
}