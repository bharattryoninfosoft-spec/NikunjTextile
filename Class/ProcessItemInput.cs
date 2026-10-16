using System;

namespace NikunjTextile.Class
{
    public class ProcessItemInput
    {
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public decimal PCS { get; set; }

        public ProcessItemInput()
        {
            SlipNo = string.Empty;
        }
    }
}

