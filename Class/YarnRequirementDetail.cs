using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnRequirementDetail
    {
        public Int64 YarnRequirementDetailID { get; set; }
        public DateTime DateandTime { get; set; }
        public string DateandTimes { get; set; }
        public int UserAccountId { get; set; }
        public int YarnRequirementID { get; set; }
        public int YarnMaterialID { get; set; }
        public string YarnMaterial { get; set; }
        public string Denier { get; set; }
        public string HSNCode { get; set; }
        public int UnitId { get; set; }
        public string Unit { get; set; }
        public int GSTSLABId { get; set; }
        public string GSTSLABName { get; set; }


        public int PartyID { get; set; }
        public int YarnColorID { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
<<<<<<< HEAD

=======
        public int companyId { get; set; }
>>>>>>> origin/master
        public string CompanyName { get; set; }
        public string CompanyColourCode { get; set; }
        public int NoofBoxes { get; set; }


        public List<PartyMaster> listPartyMaster { get; set; }
    }
}