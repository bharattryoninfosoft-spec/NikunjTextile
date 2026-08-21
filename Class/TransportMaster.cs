using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class TransportMaster
    {
        public int TransportId { get; set; }
        public string TransportCode { get; set; }
        public string TransportName { get; set; }
        public string FirmName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string MobileNo { get; set; }
        public string AlternateMobileNo { get; set; }
        public string Address { get; set; }
        public string CityName { get; set; }
        public string PANCard { get; set; }
        public string GSTNo { get; set; }
        public string VehicleType { get; set; }
        public bool IsActive { get; set; }

        public int UserAccountId { get; set; }
        public int FinancialYearID { get; set; }
        public int CompanyId { get; set; }
    }
}