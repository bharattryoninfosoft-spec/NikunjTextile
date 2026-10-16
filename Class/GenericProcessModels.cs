using System;
using System.Collections.Generic;

namespace NikunjTextile.Class
{
    #region COMMON RESPONSE WRAPPERS
    public class GenericApiResponse
    {
        public bool Status { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TransactionNo { get; set; }
        public string ReceiveChallanNo { get; set; }
        public string IssueBackChallanNo { get; set; }
        public string JobberReceiveChallanNo { get; set; }
        public object Data { get; set; }

        public GenericApiResponse()
        {
            Status = true;
            Success = true;
            Message = string.Empty;
            TransactionNo = string.Empty;
            ReceiveChallanNo = string.Empty;
            IssueBackChallanNo = string.Empty;
            JobberReceiveChallanNo = string.Empty;
        }

        public GenericApiResponse(bool status, string message)
        {
            Status = status;
            Success = status;
            Message = message ?? string.Empty;
            TransactionNo = string.Empty;
            ReceiveChallanNo = string.Empty;
            IssueBackChallanNo = string.Empty;
            JobberReceiveChallanNo = string.Empty;
        }
    }
    #endregion

    #region DROPDOWN DTOs
    public class ProcessDropdownItem
    {
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public string ProcessCode { get; set; }

        public ProcessDropdownItem()
        {
            ProcessName = string.Empty;
            ProcessCode = string.Empty;
        }
    }

    public class JobberDropdownItem
    {
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }

        public JobberDropdownItem()
        {
            JobberName = string.Empty;
            ProcessorName = string.Empty;
        }
    }

    public class DesignDropdownItem
    {
        public string DesignNo { get; set; }

        public DesignDropdownItem()
        {
            DesignNo = string.Empty;
        }
    }

    public class SlipDropdownItem
    {
        public string SlipNo { get; set; }

        public SlipDropdownItem()
        {
            SlipNo = string.Empty;
        }
    }

    public class MachineDropdownItem
    {
        public int MachineNumber { get; set; }

        public MachineDropdownItem() { }
    }

    public class BarcodeDropdownItem
    {
        public string Barcode { get; set; }

        public BarcodeDropdownItem()
        {
            Barcode = string.Empty;
        }
    }

    public class GenericProcessDropdownResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<ProcessDropdownItem> Processes { get; set; }
        public List<JobberDropdownItem> Jobbers { get; set; }
        public List<JobberDropdownItem> Processors { get; set; }
        public List<DesignDropdownItem> Designs { get; set; }
        public List<SlipDropdownItem> Slips { get; set; }
        public List<MachineDropdownItem> Machines { get; set; }
        public List<BarcodeDropdownItem> Barcodes { get; set; }

        public GenericProcessDropdownResponse()
        {
            Status = true;
            Message = string.Empty;
            Processes = new List<ProcessDropdownItem>();
            Jobbers = new List<JobberDropdownItem>();
            Processors = new List<JobberDropdownItem>();
            Designs = new List<DesignDropdownItem>();
            Slips = new List<SlipDropdownItem>();
            Machines = new List<MachineDropdownItem>();
            Barcodes = new List<BarcodeDropdownItem>();
        }
    }
    #endregion

    #region JOBBER TO PROCESS DTOs
    public class JobberToProcessItemDto
    {
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string ProductionOrderNo { get; set; }
        public string OrderDate { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public string PartyName { get; set; }
        public string SlipNo { get; set; }
        public string Barcode { get; set; }
        public int MachineNumber { get; set; }
        public string DesignNo { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public decimal ProductionPCS { get; set; }
        public decimal JobberStockPCS { get; set; }
        public decimal AlreadyIssuedPCS { get; set; }
        public decimal IssuedPCS { get; set; }
        public decimal ReceivedBackPCS { get; set; }
        public decimal PendingIssuePCS { get; set; }
        public decimal IssuePCS { get; set; }
        public string Status { get; set; }
        public string PhotoPath { get; set; }
        public string SONumber { get; set; }
        public long SaleOrderID { get; set; }

        public JobberToProcessItemDto()
        {
            ProductionOrderNo = string.Empty;
            OrderDate = string.Empty;
            JobberName = string.Empty;
            PartyName = string.Empty;
            SlipNo = string.Empty;
            Barcode = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            Status = "PENDING";
            PhotoPath = string.Empty;
            SONumber = string.Empty;
        }
    }

    public class JobberToProcessListDto
    {
        public long ProcessTransactionID { get; set; }
        public string ProcessTransactionNo { get; set; }
        public string TransactionDate { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public string TransactionType { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }
        public string JobType { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalPCS { get; set; }
        public decimal TotalIssuePCS { get; set; }
        public decimal TotalReceivePCS { get; set; }
        public decimal TotalShortPCS { get; set; }
        public decimal PendingPCS { get; set; }
        public string StatusText { get; set; }
        public string CreatedDateTime { get; set; }

        public JobberToProcessListDto()
        {
            ProcessTransactionNo = string.Empty;
            TransactionDate = string.Empty;
            ProcessName = string.Empty;
            TransactionType = "ISSUE";
            JobberName = string.Empty;
            ProcessorName = string.Empty;
            JobType = string.Empty;
            StatusText = string.Empty;
            CreatedDateTime = string.Empty;
        }
    }

    public class JobberToProcessMasterHeaderDto
    {
        public long ProcessTransactionID { get; set; }
        public string ProcessTransactionNo { get; set; }
        public string TransactionDateStr { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }
        public string JobType { get; set; }
        public string Remark { get; set; }

        public JobberToProcessMasterHeaderDto()
        {
            ProcessTransactionNo = string.Empty;
            TransactionDateStr = string.Empty;
            TransactionDate = string.Empty;
            TransactionType = "ISSUE";
            ProcessName = string.Empty;
            JobberName = string.Empty;
            ProcessorName = string.Empty;
            JobType = string.Empty;
            Remark = string.Empty;
        }
    }

    public class JobberToProcessDetailItemDto
    {
        public long ProcessTransactionDetailID { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public int MachineNumber { get; set; }
        public decimal ProductionPCS { get; set; }
        public decimal PCS { get; set; }
        public decimal JobberStockPCS { get; set; }
        public decimal PendingIssuePCS { get; set; }
        public decimal IssuePCS { get; set; }
        public decimal ReceivePCS { get; set; }
        public decimal ShortPCS { get; set; }
        public decimal PendingPCS { get; set; }
        public decimal AllottedPcs { get; set; }
        public string PhotoPath { get; set; }

        public JobberToProcessDetailItemDto()
        {
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            PhotoPath = string.Empty;
        }
    }

    public class JobberToProcessDetailResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public JobberToProcessMasterHeaderDto Master { get; set; }
        public List<JobberToProcessDetailItemDto> Details { get; set; }

        public JobberToProcessDetailResponse()
        {
            Status = true;
            Message = string.Empty;
            Master = new JobberToProcessMasterHeaderDto();
            Details = new List<JobberToProcessDetailItemDto>();
        }
    }
    #endregion

    #region HISTORY & TIMELINE DTOs
    public class ProcessHistoryItemDto
    {
        public long ProcessHistoryID { get; set; }
        public long ProcessTransactionDetailID { get; set; }
        public string DateStr { get; set; }
        public string TransactionType { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long FromPartyID { get; set; }
        public string FromPartyName { get; set; }
        public long ToPartyID { get; set; }
        public string ToPartyName { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SONumber { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public decimal Qty { get; set; }
        public decimal ShortQty { get; set; }
        public int MachineNumber { get; set; }
        public string Remark { get; set; }
        public long ReferenceTransactionID { get; set; }
        public string TransactionNo { get; set; }

        public ProcessHistoryItemDto()
        {
            DateStr = string.Empty;
            TransactionType = string.Empty;
            ProcessName = string.Empty;
            FromPartyName = string.Empty;
            ToPartyName = string.Empty;
            SONumber = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            Remark = string.Empty;
            TransactionNo = string.Empty;
        }
    }

    public class ProcessTransactionHistoryResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<ProcessHistoryItemDto> History { get; set; }

        public ProcessTransactionHistoryResponse()
        {
            Status = true;
            Message = string.Empty;
            History = new List<ProcessHistoryItemDto>();
        }
    }

    public class TimelineEventDto
    {
        public int StepNo { get; set; }
        public string Stage { get; set; }
        public string Action { get; set; }
        public string DateStr { get; set; }
        public string PartyName { get; set; }
        public string Description { get; set; }
        public decimal Qty { get; set; }
        public decimal ShortQty { get; set; }
        public string Status { get; set; }
        public string BadgeClass { get; set; }

        public TimelineEventDto()
        {
            Stage = string.Empty;
            Action = string.Empty;
            DateStr = string.Empty;
            PartyName = string.Empty;
            Description = string.Empty;
            Status = string.Empty;
            BadgeClass = string.Empty;
        }
    }

    public class SlipMetaDto
    {
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderID { get; set; }
        public string SalesOrderNo { get; set; }
        public string DesignNo { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public decimal ProductionPCS { get; set; }
        public string SlipNo { get; set; }
        public int MachineNumber { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }

        public SlipMetaDto()
        {
            SalesOrderNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            SlipNo = string.Empty;
            JobberName = string.Empty;
        }
    }

    public class SlipStockSummaryDto
    {
        public decimal OriginalProductionPCS { get; set; }
        public decimal JobberCurrentStockPCS { get; set; }
        public decimal PendingProcessReceivePCS { get; set; }
        public decimal ProcessStockPCS { get; set; }
        public decimal PendingJobberReceivePCS { get; set; }
        public decimal TotalShortPCS { get; set; }
        public decimal TotalIssuedToProcessPCS { get; set; }
        public decimal TotalReceivedFromProcessPCS { get; set; }
        public decimal TotalIssuedBackPCS { get; set; }
        public decimal TotalJobberReceivedPCS { get; set; }

        public SlipStockSummaryDto() { }
    }

    public class SlipTimelineAndStockResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public SlipMetaDto Meta { get; set; }
        public List<ProcessHistoryItemDto> Timeline { get; set; }
        public SlipStockSummaryDto StockSummary { get; set; }

        public SlipTimelineAndStockResponse()
        {
            Status = true;
            Message = string.Empty;
            Meta = new SlipMetaDto();
            Timeline = new List<ProcessHistoryItemDto>();
            StockSummary = new SlipStockSummaryDto();
        }
    }

    public class TimelineStockSummaryResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public string SONumber { get; set; }
        public decimal ProductionPCS { get; set; }
        public string CurrentStatus { get; set; }
        public decimal JobberStockPCS { get; set; }
        public decimal InProcessPCS { get; set; }
        public decimal ProcessStockPCS { get; set; }
        public decimal CompletedPCS { get; set; }
        public decimal ShortPCS { get; set; }
        public string CurrentHolder { get; set; }
        public List<TimelineEventDto> Timeline { get; set; }

        public TimelineStockSummaryResponse()
        {
            Status = true;
            Message = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            SONumber = string.Empty;
            CurrentStatus = string.Empty;
            CurrentHolder = string.Empty;
            Timeline = new List<TimelineEventDto>();
        }
    }
    #endregion

    #region PROCESS RECEIVE DTOs
    public class IssueChallanForReceiveDto
    {
        public long ProcessTransactionID { get; set; }
        public string ProcessTransactionNo { get; set; }
        public string TransactionDate { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }
        public string JobType { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public decimal TotalIssuePCS { get; set; }
        public decimal TotalReceivePCS { get; set; }
        public decimal TotalShortPCS { get; set; }
        public decimal PendingReceivePCS { get; set; }

        public IssueChallanForReceiveDto()
        {
            ProcessTransactionNo = string.Empty;
            TransactionDate = string.Empty;
            ProcessName = string.Empty;
            JobberName = string.Empty;
            ProcessorName = string.Empty;
            JobType = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
        }
    }

    public class IssueChallanDetailForReceiveItemDto
    {
        public long IssueDetailID { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public int MachineNumber { get; set; }
        public decimal IssuePCS { get; set; }
        public decimal AlreadyReceivePCS { get; set; }
        public decimal PendingReceivePCS { get; set; }
        public decimal ReceivePCS { get; set; }
        public decimal ShortPCS { get; set; }
        public string JobType { get; set; }
        public string PhotoPath { get; set; }

        public IssueChallanDetailForReceiveItemDto()
        {
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            JobType = string.Empty;
            PhotoPath = string.Empty;
        }
    }

    public class IssueChallanDetailForReceiveResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public JobberToProcessMasterHeaderDto Master { get; set; }
        public string NextReceiveNo { get; set; }
        public List<IssueChallanDetailForReceiveItemDto> Details { get; set; }

        public IssueChallanDetailForReceiveResponse()
        {
            Status = true;
            Message = string.Empty;
            Master = new JobberToProcessMasterHeaderDto();
            NextReceiveNo = string.Empty;
            Details = new List<IssueChallanDetailForReceiveItemDto>();
        }
    }
    #endregion

    #region PROCESS STOCK & ISSUE BACK DTOs
    public class ProcessStockItemDto
    {
        public int SrNo { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public int MachineNumber { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public string SONumber { get; set; }
        public string JobType { get; set; }
        public decimal ReceivedPCS { get; set; }
        public decimal IssuedBackPCS { get; set; }
        public decimal AvailablePCS { get; set; }
        public decimal ProcessStockPCS { get; set; }
        public string Status { get; set; }

        public ProcessStockItemDto()
        {
            ProcessName = string.Empty;
            ProcessorName = string.Empty;
            JobberName = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            SONumber = string.Empty;
            JobType = string.Empty;
            Status = "PROCESS STOCK";
        }
    }

    public class ProcessStockForIssueBackItemDto
    {
        public int SrNo { get; set; }
        public long ReceiveDetailID { get; set; }
        public long OriginalReceiveTransactionID { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public int MachineNumber { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public string SONumber { get; set; }
        public string JobType { get; set; }
        public decimal ReceivedPCS { get; set; }
        public decimal IssuedBackPCS { get; set; }
        public decimal AvailablePCS { get; set; }
        public decimal IssueBackPCS { get; set; }

        public ProcessStockForIssueBackItemDto()
        {
            ProcessName = string.Empty;
            ProcessorName = string.Empty;
            JobberName = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            SONumber = string.Empty;
            JobType = string.Empty;
        }
    }

    public class ProcessIssueBackListDto
    {
        public long ProcessTransactionID { get; set; }
        public string ProcessTransactionNo { get; set; }
        public string TransactionDate { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public string JobType { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public decimal TotalIssuedBackPCS { get; set; }
        public long ReferenceTransactionID { get; set; }

        public ProcessIssueBackListDto()
        {
            ProcessTransactionNo = string.Empty;
            TransactionDate = string.Empty;
            ProcessName = string.Empty;
            ProcessorName = string.Empty;
            JobberName = string.Empty;
            JobType = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
        }
    }

    public class ProcessIssueBackDetailItemDto
    {
        public long ProcessTransactionDetailID { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public int MachineNumber { get; set; }
        public decimal IssuePCS { get; set; }
        public decimal IssueBackPCS { get; set; }
        public string JobType { get; set; }
        public string PhotoPath { get; set; }

        public ProcessIssueBackDetailItemDto()
        {
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            JobType = string.Empty;
            PhotoPath = string.Empty;
        }
    }

    public class ProcessIssueBackDetailResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public JobberToProcessMasterHeaderDto Master { get; set; }
        public List<ProcessIssueBackDetailItemDto> Details { get; set; }

        public ProcessIssueBackDetailResponse()
        {
            Status = true;
            Message = string.Empty;
            Master = new JobberToProcessMasterHeaderDto();
            Details = new List<ProcessIssueBackDetailItemDto>();
        }
    }
    #endregion

    #region JOBBER RECEIVE DTOs
    public class JobberReceiveItemDto
    {
        public long DetailID { get; set; }
        public long IssueBackDetailID { get; set; }
        public long OriginalIssueBackTransactionID { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public int MachineNumber { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public string SONumber { get; set; }
        public string JobType { get; set; }
        public decimal IssueBackPCS { get; set; }
        public decimal AlreadyJobberReceivePCS { get; set; }
        public decimal AlreadyReceivePCS { get; set; }
        public decimal PendingJobberReceivePCS { get; set; }
        public decimal PendingReceivePCS { get; set; }
        public decimal ReceivePCS { get; set; }
        public decimal ShortPCS { get; set; }
        public string PhotoPath { get; set; }

        public JobberReceiveItemDto()
        {
            ProcessName = string.Empty;
            ProcessorName = string.Empty;
            JobberName = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            SONumber = string.Empty;
            JobType = string.Empty;
            PhotoPath = string.Empty;
        }
    }

    public class JobberReceiveChallanListItemDto
    {
        public long ProcessTransactionID { get; set; }
        public string ProcessTransactionNo { get; set; }
        public string TransactionDate { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long ProcessorID { get; set; }
        public string ProcessorName { get; set; }
        public long JobberID { get; set; }
        public string JobberName { get; set; }
        public string JobType { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public decimal TotalIssueBackPCS { get; set; }
        public decimal TotalJobberReceivePCS { get; set; }
        public decimal PendingJobberReceivePCS { get; set; }
        public decimal TotalReceivePCS { get; set; }
        public decimal TotalReceivedPCS { get; set; }
        public decimal TotalShortPCS { get; set; }
        public long ReferenceTransactionID { get; set; }
        public string Status { get; set; }

        public JobberReceiveChallanListItemDto()
        {
            ProcessTransactionNo = string.Empty;
            TransactionDate = string.Empty;
            ProcessName = string.Empty;
            ProcessorName = string.Empty;
            JobberName = string.Empty;
            JobType = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            Status = string.Empty;
        }
    }

    public class JobberReceiveDetailResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public JobberToProcessMasterHeaderDto Master { get; set; }
        public string NextJobberReceiveNo { get; set; }
        public string NextReceiveNo { get; set; }
        public List<JobberReceiveItemDto> Details { get; set; }

        public JobberReceiveDetailResponse()
        {
            Status = true;
            Message = string.Empty;
            Master = new JobberToProcessMasterHeaderDto();
            NextJobberReceiveNo = string.Empty;
            NextReceiveNo = string.Empty;
            Details = new List<JobberReceiveItemDto>();
        }
    }
    #endregion

    #region CHALLAN REPORT DTOs
    public class ChallanReportMasterDto
    {
        public long ProcessTransactionID { get; set; }
        public string ProcessTransactionNo { get; set; }
        public string TransactionType { get; set; }
        public string TransactionDateStr { get; set; }
        public string TransactionDate { get; set; }
        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        public long FromPartyID { get; set; }
        public string FromPartyName { get; set; }
        public long ToPartyID { get; set; }
        public string ToPartyName { get; set; }
        public long ReferenceTransactionID { get; set; }
        public string ReferenceTransactionNo { get; set; }
        public string ReferenceTransactionDateStr { get; set; }
        public string Remark { get; set; }

        public ChallanReportMasterDto()
        {
            ProcessTransactionNo = string.Empty;
            TransactionType = string.Empty;
            TransactionDateStr = string.Empty;
            TransactionDate = string.Empty;
            ProcessName = string.Empty;
            FromPartyName = string.Empty;
            ToPartyName = string.Empty;
            ReferenceTransactionNo = string.Empty;
            ReferenceTransactionDateStr = string.Empty;
            Remark = string.Empty;
        }
    }

    public class ChallanReportDetailDto
    {
        public long ProcessTransactionDetailID { get; set; }
        public long ProductionOrderID { get; set; }
        public long ProductionOrderDetailID { get; set; }
        public long ProductionOrderSubDetailID { get; set; }
        public string SONumber { get; set; }
        public string SlipNo { get; set; }
        public string DesignNo { get; set; }
        public long ColourMatchingID { get; set; }
        public string ColourMatchingName { get; set; }
        public int MachineNumber { get; set; }
        public decimal PCS { get; set; }
        public decimal IssuePCS { get; set; }
        public decimal ReceivePCS { get; set; }
        public decimal ShortPCS { get; set; }
        public decimal PendingPCS { get; set; }
        public string Remark { get; set; }

        public ChallanReportDetailDto()
        {
            SONumber = string.Empty;
            SlipNo = string.Empty;
            DesignNo = string.Empty;
            ColourMatchingName = string.Empty;
            Remark = string.Empty;
        }
    }

    public class ChallanCompanyDto
    {
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string GSTNo { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public ChallanCompanyDto()
        {
            CompanyName = "NIKUNJ TEXTILE ERP";
            Address = "Ring Road, Surat, Gujarat - 395002";
            GSTNo = "24AAAAA0000A1Z5";
            Phone = "+91 98765 43210";
            Email = "info@nikunjtextile.com";
        }
    }

    public class ProcessChallanReportDataResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public ChallanReportMasterDto Master { get; set; }
        public List<ChallanReportDetailDto> Details { get; set; }
        public ChallanCompanyDto Company { get; set; }

        public ProcessChallanReportDataResponse()
        {
            Status = true;
            Message = string.Empty;
            Master = new ChallanReportMasterDto();
            Details = new List<ChallanReportDetailDto>();
            Company = new ChallanCompanyDto();
        }
    }
    #endregion
}
