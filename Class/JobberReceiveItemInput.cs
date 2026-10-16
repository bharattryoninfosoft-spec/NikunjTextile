using System;

namespace NikunjTextile.Class
{
    public class JobberReceiveItemInput
    {
        public long IssueBackDetailID { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public decimal ReceivePCS { get; set; }
        public long IssueBackTransactionID { get; set; }
        public string Remark { get; set; }
    }
}
