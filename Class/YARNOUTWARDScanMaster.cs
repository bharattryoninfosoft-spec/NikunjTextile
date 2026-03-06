using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class YarnOutwardScanMasterModel
    {
        public string YarnOutwardID { get; set; }
        public int PartyId { get; set; }
        public string ChallanNo { get; set; }
        public string ChallanDate { get; set; }
        public int TotalBox { get; set; }
        public decimal TotalWeight { get; set; }
        public List<YarnOutwardScanDetailModel> Details { get; set; }
    }

    public class YarnOutwardScanDetailModel
    {
        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public int GodownLocationID { get; set; }
        public string BoxNo { get; set; }
        public decimal NetWeight { get; set; }
        public int YarnInwardDetailID { get; set; }
    }
}