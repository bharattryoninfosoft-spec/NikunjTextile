using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnInterChangeResponce
    {
        public int Code { get; set; }
        public string Message { get; set; }

        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public int YarnPODetailIDCompanyCode { get; set; }
        public int GodownID { get; set; }
        public int GodownLocationID { get; set; }
        public int YarnPOID { get; set; }
        public int YarnInwardDetailID { get; set; }
        public string YarnInwardDetailIDs { get; set; }

        public List<PartyMaster> listPartyMaster { get; set; }
        public List<YarnColorMaster> listYarnColorMaster { get; set; }
        public List<YarnMaterialMaster> listYarnMaterialMaster { get; set; }
        public List<YarnPODetails> listYarnPODetails { get; set; }
        public List<YarnInwardDetail> listYarnInwardDetail { get; set; }
        public List<YarnPOMaster> listYarnPOMaster { get; set; }
        public List<YarnInterChangeMaster> listYarnInterChangeMaster { get; set; }
    }
}