using System;

namespace NikunjTextile.Class
{
    public class IssueBackItemInput
    {
        public long ReceiveDetailID { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public decimal IssueBackPCS { get; set; }
        public long OriginalReceiveTransactionID { get; set; }
        public string Remark { get; set; }
    }
}
