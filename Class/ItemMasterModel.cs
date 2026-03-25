using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class ItemMasterModel
    {
        public int ItemId { get; set; }

        public DateTime? DateAndTime { get; set; }

        public string ItemCode { get; set; }

        public string ItemName { get; set; }

        public byte UnitId { get; set; }

        public string HSNCode { get; set; }

        public long GSTSLABId { get; set; }

        public decimal PurchaseRate { get; set; }

        public decimal SaleRate { get; set; }

        public decimal OpeningStock { get; set; }

        public decimal MinStockLevel { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public int UserAccountId { get; set; }

        public int FinancialYearID { get; set; }

        public int CompanyId { get; set; }
        public string BarcodeNo { get; set; }
    }
}