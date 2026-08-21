using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class JobberInwardMasterModel
    {
        public long JobberInwardId { get; set; }
        public string DateAndTime { get; set; }
        public string JobberId { get; set; }   
        public long PartyId { get; set; }
        public long CompanyId { get; set; }
        public long UserAccountId { get; set; }
        public long JobWorkOutwardId { get; set; }
        public string ChallanNo { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal PendingWeight { get; set; }
        public decimal POPendingWeight { get; set; }
        public int GodownID { get; set; }
        public string PhotoOfInward { get; set; }
        public byte IsComplete { get; set; }
        public int FinancialYearID { get; set; }
        public List<JobberInwardDetailModel> Detail { get; set; }
    }

    public class JobberInwardDetailModel
    {
        public string BoxNo { get; set; }

        public decimal NetWeight { get; set; }

        public string BarcodeNo { get; set; }

        public int GodownLocationID { get; set; }

        public int YarnInterchangeID { get; set; }

        public int IsScanStutas { get; set; }
    }
}