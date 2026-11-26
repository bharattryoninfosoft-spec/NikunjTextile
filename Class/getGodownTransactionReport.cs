using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NikunjTextile.Class
{
    public class GodownInwardTransactionRow
    {
        public int SrNo { get; set; }
        public string TransactionDate { get; set; }
        public string InwardNo { get; set; }
        public string PONo { get; set; }
        public string PartyChallanNo { get; set; }
        public string SupplierName { get; set; }
        public string YarnCompanyName { get; set; }
        public string YarnMaterial { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public string BoxNo { get; set; }
        public string BarcodeNo { get; set; }
        public decimal NetWeight { get; set; }
        public string Location { get; set; }
        public string GodownName { get; set; }
        public string ColorSwatchHex { get; set; }
    }

    public class GodownOutwardTransactionRow
    {
        public int SrNo { get; set; }
        public string TransactionDate { get; set; }
        public string OutwardNo { get; set; }
        public string ChallanNo { get; set; }
        public string PartyChallanNo { get; set; }
        public string JobberCreditorName { get; set; }
        public string YarnCompanyName { get; set; }
        public string YarnMaterial { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }
        public string BoxNo { get; set; }
        public string BarcodeNo { get; set; }
        public decimal NetWeight { get; set; }
        public string GodownAddress { get; set; }
        public string GodownName { get; set; }
        public string ColorSwatchHex { get; set; }
    }

    public class GodownTransactionReportResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<GodownInwardTransactionRow> InwardList { get; set; }
        public List<GodownOutwardTransactionRow> OutwardList { get; set; }
    }
}
