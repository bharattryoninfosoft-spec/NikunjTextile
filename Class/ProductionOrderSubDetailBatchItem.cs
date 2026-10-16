using System;

namespace NikunjTextile.Class
{
    public class ProductionOrderSubDetailBatchItem
    {
        public long productionOrderID { get; set; }
        public long productionOrderDetailID { get; set; }
        public string productionDate { get; set; }
        public int machineNumber { get; set; }
        public decimal pcs { get; set; }
    }
}
