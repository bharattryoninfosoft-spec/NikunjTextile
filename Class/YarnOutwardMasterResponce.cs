using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnOutwardMasterResponce
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public int YarnOutwardID { get; set; }
        public int GodownID { get; set; }
        public List<YarnOutwardMaster> listYarnOutwardMaster { get; set; }
        public List<PartyMaster> listPartyMaster { get; set; }
        public List<PartyMaster> listCompanyMaster { get; set; }
        public List<YarnMaterialMaster> listYarnMaterialMaster { get; set; }
        public List<YarnColorMaster> listYarnColorMaster { get; set; }
        public List<YarnInwardMaster> listYarnInwardMaster { get; set; }
        public List<UserAccountMaster> listUserAccountMaster { get; set; }
        public List<GodownMaster> listGodownMaster { get; set; }
        public List<TotalInwardStockArray> listTotalInwardStockArray { get; set; }
        public List<TotalOutwardStockArray> listTotalOutwardStockArray { get; set; }
        public List<TotalExistingOutwardStockArray> listTotalExistingOutwardStockArray { get; set; }
        public List<YarnOutwardMaster> YarnOutwardcondition { get; set; }

    }
}