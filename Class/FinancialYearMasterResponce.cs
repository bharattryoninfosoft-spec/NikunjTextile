using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class FinancialYearMasterResponce
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<FinancialYearMaster> listFinancialYearMaster { get; set; }
    }
}