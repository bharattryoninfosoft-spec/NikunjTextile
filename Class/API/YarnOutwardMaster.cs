using System;
using System.Collections.Generic;

namespace NikunjTextile.Class.API
{
    // Common API Response
    public class ApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public bool success { get; set; }
    }

    // Generic API Response
    public class ApiResponse<T> : ApiResponse
    {
        public T Data { get; set; }
    }

    // Pagination Response
    public class PaginationResponse<T> : ApiResponse
    {
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public List<T> Data { get; set; }
    }

    // Yarn Outward List Model
    public class ListYarnOutwardMasterAPI
    {
        public int YarnOutwardID { get; set; }
        public int OutwardListNo { get; set; }
        public DateTime DateAndTime { get; set; }
        public string DateAndTimes { get; set; }
        public string PartyName { get; set; }
        public string MobileNo { get; set; }
        public string BoxNo { get; set; }

        // Search / Pagination
        public string SearchRequirementNo { get; set; }
        public int StartFrom { get; set; }
    }

    // Yarn Outward Scan Item
    public class YarnOutwardMasterAPI
    {
        public int PartyId { get; set; }
        public string PartyName { get; set; }
        public string Address { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int YarnMaterialID { get; set; }
        public string YarnMaterial { get; set; }

        public int YarnColorID { get; set; }
        public string YarnColor { get; set; }
        public string YarnColorCode { get; set; }

        public int GodownLocationID { get; set; }
        public string LocationTitle { get; set; }

        public string BoxNo { get; set; }
    }

    // Scan Response Data
    public class YarnOutwardScanData
    {
        public int ChallanNo { get; set; }
        public List<YarnOutwardMasterAPI> Items { get; set; }
    }

    // Response Helper
    public static class ApiResponseHelper
    {
        public static ApiResponse<T> Success<T>(T data)
        {
            return new ApiResponse<T>
            {
                Code = 200,
                Message = "Success",
                Data = data
            };
        }

        public static ApiResponse Error(string message)
        {
            return new ApiResponse
            {
                Code = 500,
                Message = message
            };
        }
    }
    public class YarnOutwardScanMasterRequestAPI
    {
        public string YarnOutwardID { get; set; }
        public int YarnOutwardScanID { get; set; }
        public string ChallanNo { get; set; }
        public DateTime? ChallanDate { get; set; }
        public int TotalBox { get; set; }
        public decimal TotalWeight { get; set; }
        public int PartyId { get; set; }
        public int UserId { get; set; }
        public List<YarnOutwardScanDetailRequestAPI> Details { get; set; }
    }

    public class YarnOutwardScanDetailRequestAPI
    {
        public int YarnMaterialID { get; set; }
        public int YarnColorID { get; set; }
        public int GodownLocationID { get; set; }
        public string BoxNo { get; set; }
        public string BarcodeNo { get; set; }
        public decimal NetWeight { get; set; }
        public int YarnInwardDetailID { get; set; }
        public int YarnCompany { get; set; }
    }
}
