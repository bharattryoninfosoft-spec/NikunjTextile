using System;

namespace NikunjTextile.Class
{
    public class ProcessItemInput
    {
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public long SaleOrderID { get; set; }
        public long SaleOrderDetailID { get; set; }
        public long SaleOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public string ColourMatchingName { get; set; }
        public long ColourMatchingID { get; set; }
        public int MachineNumber { get; set; }
        public decimal PCS { get; set; }
        public decimal ShortPCS { get; set; }
        public bool IsClosed { get; set; }

        public ProcessItemInput()
        {
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            ColourMatchingID = 0;
            MachineNumber = 1;
            ShortPCS = 0;
            IsClosed = false;
        }
    }
}

