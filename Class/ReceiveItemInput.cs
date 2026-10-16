using System;

namespace NikunjTextile.Class
{
    public class ReceiveItemInput
    {
        public long IssueDetailID { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public decimal IssuePCS { get; set; }
        public decimal ReceivePCS { get; set; }
        public decimal ShortPCS { get; set; }
        public string Remark { get; set; }

        public ReceiveItemInput()
        {
            SlipNo = string.Empty;
            Remark = string.Empty;
        }
    }
}

