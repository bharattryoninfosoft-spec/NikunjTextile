using iText.Forms.Xfdf;
using iText.Kernel.Geom;
using iText.Layout.Element;
using iTextSharp.text;
using iTextSharp.text.pdf;
using NikunjTextile.Class;
using NikunjTextile.Class.API;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Windows.Forms;

namespace NikunjTextile
{
    [WebService(Namespace = "http://tempuri.org/")]
    [ScriptService]  

    public class WebServiceMoblieAPI : WebService
    {
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void UserLoginAPI(string Username, string Password)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

            using (SqlConnection con = new SqlConnection(cs))
            {
                MD5 md5Hasher = MD5.Create();
                byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(Password));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                Password = sBuilder.ToString().ToUpper();
                SqlCommand cmd = new SqlCommand(@"SELECT U.*, C.CompanyId,C.CompanyName,F.FinancialYearID, F.FinancialYear   FROM UserAccountMaster U
                                                    LEFT JOIN CompanyMaster C ON C.CompanyId = U.CompanyId
                                                    CROSS JOIN
                                                    (
                                                        SELECT TOP 1 FinancialYearID, FinancialYear FROM FinancialYearMaster  WHERE IsDefault = 1
                                                    ) F
                                                    WHERE U.UserAccountMobileNo = @username  AND U.UserAccountPassword = @password", con);
                cmd.Parameters.AddWithValue("@username", Username);
                cmd.Parameters.AddWithValue("@password", Password);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                JavaScriptSerializer js = new JavaScriptSerializer();
                object result;
                if (rdr.Read())
                {
                    string allowLogin = Convert.ToString(rdr["AllowLogin"]);
                    if (allowLogin == "1")
                    {
                        result = new
                        {
                            success = true,
                            message = "Login Successfully",
                            data = new
                            {
                                UserAccountId = Convert.ToString(rdr["UserAccountId"]),
                                UserAccountMobileNo = Convert.ToString(rdr["UserAccountMobileNo"]),
                                UserAccountName = Convert.ToString(rdr["UserAccountName"]),
                                UserRole = Convert.ToString(rdr["UserRole"]),
                                UserAccountEmail = Convert.ToString(rdr["UserAccountEmail"]),
                                UserAccountProfile = Convert.ToString(rdr["UserAccountProfile"]),
                                AllowLogin = allowLogin,
                                CompanyId = Convert.ToInt32(rdr["CompanyId"]),
                                CompanyName = Convert.ToString(rdr["CompanyName"]),
                                FinancialYearID = Convert.ToInt32(rdr["FinancialYearID"]),
                                FinancialYear = Convert.ToString(rdr["FinancialYear"])
                            }
                        };
                    }
                    else
                    {
                        result = new
                        {
                            success = false,
                            message = "This user is not allowed to login",
                            data = (object)null
                        };
                    }
                }
                else
                {
                    result = new
                    {
                        success = false,
                        message = "Invalid Username or Password",
                        data = (object)null
                    };
                }
                Context.Response.Write(js.Serialize(result));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetDisplayYarnoutwardScanMasterAPI(string Status, int UserId, string SearchRequirementNo = "", int pageNumber = 1, int pageSize = 30)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            JavaScriptSerializer js = new JavaScriptSerializer();

            List<ListYarnOutwardMasterAPI> listUser = new List<ListYarnOutwardMasterAPI>();
            PaginationResponse<ListYarnOutwardMasterAPI> response = new PaginationResponse<ListYarnOutwardMasterAPI>();

            string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

            int scanStatus = Status == "Pending" ? 0 : 1;

            int startRow = ((pageNumber - 1) * pageSize) + 1;
            int endRow = pageNumber * pageSize;

            string searchQuery = "";

            if (!string.IsNullOrEmpty(SearchRequirementNo))
            {
                searchQuery = " AND (YOM.OutwardListNo LIKE '%" + SearchRequirementNo + "%' " +
                              " OR PM.PartyName LIKE '%" + SearchRequirementNo + "%' " +
                              " OR PM.MobileNo LIKE '%" + SearchRequirementNo + "%')";
            }

            int totalRecords = 0;

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.Text;

                cmd.CommandText = @"
        SELECT * FROM (
            SELECT ROW_NUMBER() OVER (ORDER BY YOM.YarnOutwardID DESC) row_num,
            YOM.YarnOutwardID,
            YOM.OutwardListNo,
            YOM.DateAndTime,
            PM.PartyName,
            PM.MobileNo,
            ISNULL(YD.TotalNoOfBox,0) AS TotalNoOfBox
            FROM YarnOutwardMaster YOM
            LEFT JOIN PartyMaster PM ON YOM.PartyId = PM.PartyId
            LEFT JOIN (
                SELECT YarnOutwardID, SUM(NoOfBox) AS TotalNoOfBox
                FROM YarnOutwardDetail
                GROUP BY YarnOutwardID
            ) YD ON YOM.YarnOutwardID = YD.YarnOutwardID
            WHERE YOM.IsScanStutas = @ScanStatus
            AND YOM.FinancialYearID = (SELECT FinancialYearID FROM FinancialYearMaster WHERE IsDefault = 1)
            AND YOM.CompanyId = (SELECT CompanyId FROM CompanyMaster WHERE is_default = 1)
            AND YOM.GodownManagerUserAccountId=@GodownManagerUserAccountId
            " + searchQuery + @"
        ) A
        WHERE row_num BETWEEN @StartRow AND @EndRow
        ORDER BY YarnOutwardID DESC";

                cmd.Parameters.AddWithValue("@ScanStatus", scanStatus);
                cmd.Parameters.AddWithValue("@StartRow", startRow);
                cmd.Parameters.AddWithValue("@EndRow", endRow);
                cmd.Parameters.AddWithValue("@GodownManagerUserAccountId", UserId);

                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    ListYarnOutwardMasterAPI condition = new ListYarnOutwardMasterAPI();

                    condition.YarnOutwardID = Convert.ToInt32(rdr["YarnOutwardID"]);
                    condition.OutwardListNo = Convert.ToInt32(rdr["OutwardListNo"]);
                    condition.DateAndTime = Convert.ToDateTime(rdr["DateAndTime"]);
                    condition.DateAndTimes = Convert.ToDateTime(rdr["DateAndTime"]).ToString("dd-MM-yyyy");
                    condition.PartyName = rdr["PartyName"].ToString().ToUpper();
                    condition.MobileNo = rdr["MobileNo"].ToString();
                    condition.BoxNo = rdr["TotalNoOfBox"].ToString();
                    listUser.Add(condition);
                }

                rdr.Close();

                SqlCommand countCmd = new SqlCommand();
                countCmd.Connection = con;

                countCmd.CommandText = @"SELECT COUNT(*) 
                                 FROM YarnOutwardMaster YOM
                                 LEFT JOIN PartyMaster PM ON YOM.PartyId = PM.PartyId
                                 WHERE YOM.IsScanStutas = @ScanStatus " + searchQuery;

                countCmd.Parameters.AddWithValue("@ScanStatus", scanStatus);

                totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());

                con.Close();
            }

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            response.Code = 200;
            response.Message = "Success";
            response.TotalRecords = totalRecords;
            response.TotalPages = totalPages;
            response.Data = listUser;

            Context.Response.Write(js.Serialize(response));
            Context.Response.Flush();
            Context.Response.SuppressContent = true;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetYarnOutwardScanAPI(int YarnOutwardID = 0)
        {
            try
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                YarnOutwardScanData data = new YarnOutwardScanData();
                List<YarnOutwardMasterAPI> yarnOutwards = new List<YarnOutwardMasterAPI>();

                int ChallanNo = 0;
                string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                // Get ChallanNo
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.Text;

                    cmd.CommandText = @"SELECT ISNULL(MAX(ChallanNo),0) + 1 AS ChallanNo FROM YARNOUTWARDScanMaster";

                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();

                    if (rdr.Read())
                    {
                        ChallanNo = Convert.ToInt32(rdr["ChallanNo"]);
                    }
                }

                // Get Box Data
                using (SqlConnection con = new SqlConnection(cs))
                {
                    try
                    {
                        SqlCommand cmd = new SqlCommand();
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;

                        cmd.CommandText = @"SELECT                            
                            Pm.PartyId,
                            PM.PartyName,
							PM1.PartyId AS YarnCompanyId,
                            PM1.PartyName AS YarnCompany,
                            Pm.BillingAddress AS Address,
                            YMM.YarnMaterialID AS YarnMaterialID,
                            YMM.YarnMaterial AS YarnMaterial,
                            YCM.YarnColorID AS YarnColorID,
                            YCM.YarnColor AS YarnColour,
                            YCM.YarnColorCode AS Code,
		                    GLM.GodownLocationID,
                            GLM.LocationTitle AS Location,
                            N.BoxNo
                            FROM YarnOutwardDetail YOD
                            INNER JOIN YarnOutwardMaster YM 
                                ON YM.YarnOutwardID = YOD.YarnOutwardID
                            LEFT JOIN PartyMaster PM 
                                ON YM.PartyId = PM.PartyId
                            LEFT JOIN PartyMaster PM1 ON YOD.BillToPartyID = PM1.PartyId
                            LEFT JOIN YarnMaterialMaster YMM  
                                ON YMM.YarnMaterialID = YOD.YarnMaterialID
                            LEFT JOIN YarnColorMaster YCM  
                                ON YCM.YarnColorID = YOD.YarnColorID
                            LEFT JOIN GodownLocationMaster GLM  
                                ON GLM.GodownLocationID = YOD.GodownLocationID
                            CROSS APPLY
                            (
                                SELECT TOP (YOD.NoOfBox)
                                ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS BoxNo
                                FROM master..spt_values
                            ) N
            WHERE YOD.YarnOutwardID = @YarnOutwardID";

                        cmd.Parameters.AddWithValue("@YarnOutwardID", YarnOutwardID);

                        con.Open();
                        SqlDataReader rdr = cmd.ExecuteReader();

                        while (rdr.Read())
                        {
                            YarnOutwardMasterAPI yarnOutward = new YarnOutwardMasterAPI();

                            yarnOutward.PartyId = Convert.ToInt32(rdr["PartyId"]);
                            yarnOutward.PartyName = (rdr["PartyName"].ToString());
                            yarnOutward.CompanyId = Convert.ToInt32(rdr["YarnCompanyId"].ToString());
                            yarnOutward.CompanyName = rdr["YarnCompany"].ToString();
                            yarnOutward.Address = rdr["Address"].ToString();
                            yarnOutward.YarnMaterialID = Convert.ToInt32(rdr["YarnMaterialID"].ToString());
                            yarnOutward.YarnMaterial = rdr["YarnMaterial"].ToString();
                            yarnOutward.YarnColorID = Convert.ToInt32(rdr["YarnColorID"].ToString());
                            yarnOutward.YarnColor = rdr["YarnColour"].ToString();
                            yarnOutward.YarnColorCode = rdr["Code"].ToString();
                            yarnOutward.GodownLocationID = Convert.ToInt32(rdr["GodownLocationID"].ToString());
                            yarnOutward.LocationTitle = rdr["Location"].ToString();
                            yarnOutward.BoxNo = (rdr["BoxNo"].ToString());

                            yarnOutwards.Add(yarnOutward);
                        }
                        data.ChallanNo = ChallanNo;
                        data.Items = yarnOutwards;
                        ApiResponse<YarnOutwardScanData> response = new ApiResponse<YarnOutwardScanData>()
                        {
                            Code = 200,
                            Message = "Success",
                            Data = data
                        };
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        js.MaxJsonLength = Int32.MaxValue;
                        Context.Response.Write(js.Serialize(response));
                        Context.Response.Flush();
                        Context.Response.SuppressContent = true;
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }
                    catch (Exception ex)
                    {
                        ApiResponse<YarnOutwardScanData> response = new ApiResponse<YarnOutwardScanData>()
                        {
                            Code = 500,
                            Message = ex.Message
                        };
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        js.MaxJsonLength = Int32.MaxValue;
                        Context.Response.Write(js.Serialize(response));
                        Context.Response.Flush();
                        Context.Response.SuppressContent = true;
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }
                }
            }
            catch (Exception ex)
            {
                ApiResponse<YarnOutwardScanData> response = new ApiResponse<YarnOutwardScanData>()
                {
                    Code = 500,
                    Message = ex.Message
                };
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetBoxWeightAPI(string BoxNo, int YarnMaterial, int YarnColour, int GodownLocationID, int YarnCompany)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

            YarnInwardDetail yarnOutward = new YarnInwardDetail();
            ApiResponse<YarnInwardDetail> response = new ApiResponse<YarnInwardDetail>();

            JavaScriptSerializer js = new JavaScriptSerializer();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(@"
                           IF EXISTS (
                                SELECT 1 
                                FROM YARNOUTWARDScanMasterDetails YOSD
                                LEFT JOIN YarnInwardDetail YID ON YOSD.YarnInwardDetailID = YID.YarnInwardDetailID
                                LEFT JOIN YarnInwardMaster YM ON YM.YarnInwardID = YID.YarnInwardID
                                LEFT JOIN YarnPOMaster YPM ON YPM.YarnPOID = YM.YarnPOID
                                LEFT JOIN PartyMaster PM ON PM.PartyId = YPM.BillToPartyID
                                WHERE YOSD.BarcodeNo = @BarcodeNo 
                                  AND YOSD.YarnMaterial = @YarnMaterial 
                                  AND YOSD.YarnColour = @YarnColour 
                                  AND YOSD.GodownLocationID = @GodownLocationID
                                  AND YOSD.YarnCompany = @YarnCompany
                            )
                            BEGIN
                                SELECT 
                                    'EXIST' AS Status,
                                    NULL AS YarnInwardDetailID,
                                    NULL AS BoxNo,
                                    NULL AS NetWeight,
                                    NULL AS BarcodeNo,
                                    NULL AS GodownLocationID,
                                    NULL AS YarnInwardID
                            END
                            ELSE
                            BEGIN
                                SELECT 
                                    'OK' AS Status,
                                    YID.YarnInwardDetailID,
                                    YID.BoxNo,
                                    YID.NetWeight,
                                    YID.BarcodeNo,
                                    GLM.GodownLocationID,
                                    GLM.LocationTitle,
                                    YID.YarnInwardID,
                                    YMM.YarnMaterialID,
                                    YMM.YarnMaterial,
                                    YCM.YarnColorID,
                                    YCM.YarnColor,
                                    YCM.YarnColorCode,
                                    PM.PartyId,
                                    PM.PartyName
                                FROM YarnInwardDetail YID
                                LEFT JOIN YarnInwardMaster YM ON YM.YarnInwardID = YID.YarnInwardID
                                LEFT JOIN YarnPOMaster YPM ON YPM.YarnPOID = YM.YarnPOID
                                LEFT JOIN PartyMaster PM ON PM.PartyId = YPM.BillToPartyID
                                LEFT JOIN YarnMaterialMaster YMM ON YMM.YarnMaterialID = YM.YarnMaterialID
                                LEFT JOIN YarnColorMaster YCM ON YCM.YarnColorID = YM.YarnColorID
                                LEFT JOIN GodownLocationMaster GLM ON GLM.GodownLocationID = YID.YarnInterchangeID
                                WHERE YID.BarcodeNo = @BarcodeNo
                                  AND YMM.YarnMaterialID = @YarnMaterial
                                  AND YCM.YarnColorID = @YarnColour
                                  AND YID.GodownLocationID = @GodownLocationID
                                  AND YPM.BillToPartyID = @YarnCompany
                            END
                        ", con);

                cmd.Parameters.Add("@BarcodeNo", SqlDbType.VarChar).Value = BoxNo;
                cmd.Parameters.Add("@YarnMaterial", SqlDbType.Int).Value = YarnMaterial;
                cmd.Parameters.Add("@YarnColour", SqlDbType.Int).Value = YarnColour;
                cmd.Parameters.Add("@GodownLocationID", SqlDbType.Int).Value = GodownLocationID;
                cmd.Parameters.Add("@YarnCompany", SqlDbType.Int).Value = YarnCompany;

                con.Open();

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        if (rdr["Status"].ToString() == "EXIST")
                        {
                            response.Code = 410;
                            response.Message = $"This Box {BoxNo} already scanned.";
                            response.Data = null;
                        }
                        else
                        {
                            yarnOutward.YarnInwardDetailID = rdr["YarnInwardDetailID"] != DBNull.Value ? Convert.ToInt32(rdr["YarnInwardDetailID"]) : 0;
                            yarnOutward.BoxNo = rdr["BoxNo"]?.ToString();
                            yarnOutward.NetWeight = rdr["NetWeight"] != DBNull.Value ? Convert.ToDecimal(rdr["NetWeight"]) : 0;
                            yarnOutward.BarcodeNo = rdr["BarcodeNo"]?.ToString();
                            yarnOutward.GodownLocationID = rdr["GodownLocationID"] != DBNull.Value ? Convert.ToInt32(rdr["GodownLocationID"]) : 0;
                            yarnOutward.YarnInwardID = rdr["YarnInwardID"] != DBNull.Value ? Convert.ToInt32(rdr["YarnInwardID"]) : 0;
                            response.Code = 200;
                            response.Message = "Success";
                            response.Data = yarnOutward;
                        }
                    }
                    else
                    {
                        response.Code = 404;
                        response.Message = "Box not found.";
                        response.Data = null;
                    }
                }
            }

            js.MaxJsonLength = Int32.MaxValue;
            Context.Response.Write(js.Serialize(response));
            Context.Response.Flush();
            Context.Response.SuppressContent = true;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveYarnoutwardScanMasterAPI(YarnOutwardScanMasterRequestAPI model)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            ApiResponse response = new ApiResponse();

            JavaScriptSerializer js = new JavaScriptSerializer();
            string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
            SqlTransaction tran = null;

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();
                    tran = con.BeginTransaction();

                    int YarnOutwardID = Convert.ToInt32(model.YarnOutwardID);

                    int YarnOutwardScanID = model.YarnOutwardScanID;

                    // INSERT MASTER
                    if (YarnOutwardScanID == 0)
                    {
                        SqlCommand cmdInsert = new SqlCommand(@"
                INSERT INTO YARNOUTWARDScanMaster
                (DateAndTime,YarnOutwardID,ChallanNo,ChallanDate,TotalBox,TotalWeight,UserAccountId,FinancialYearID,CompanyId)
                VALUES
                (GETDATE(),@YarnOutwardID,@ChallanNo,@ChallanDate,@TotalBox,@TotalWeight,@UserAccountId,
                (SELECT FinancialYearID FROM FinancialYearMaster WHERE IsDefault = 1),
                (SELECT CompanyId FROM CompanyMaster WHERE is_default = 1))
                SELECT SCOPE_IDENTITY()", con, tran);

                        cmdInsert.Parameters.AddWithValue("@YarnOutwardID", YarnOutwardID);
                        cmdInsert.Parameters.AddWithValue("@ChallanNo", model.ChallanNo ?? "");
                        cmdInsert.Parameters.AddWithValue("@ChallanDate", (object)model.ChallanDate ?? DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@TotalBox", model.TotalBox);
                        cmdInsert.Parameters.AddWithValue("@TotalWeight", model.TotalWeight);
                        cmdInsert.Parameters.AddWithValue("@UserAccountId", model.UserId);

                        YarnOutwardScanID = Convert.ToInt32(cmdInsert.ExecuteScalar());
                    }
                    else
                    {
                        SqlCommand cmdUpdate = new SqlCommand(@"
                UPDATE YARNOUTWARDScanMaster
                SET ChallanNo=@ChallanNo,ChallanDate=@ChallanDate,TotalBox=@TotalBox,TotalWeight=@TotalWeight
                WHERE YarnOutwardScanID=@YarnOutwardScanID", con, tran);

                        cmdUpdate.Parameters.AddWithValue("@YarnOutwardScanID", YarnOutwardScanID);
                        cmdUpdate.Parameters.AddWithValue("@ChallanNo", model.ChallanNo ?? "");
                        cmdUpdate.Parameters.AddWithValue("@ChallanDate", (object)model.ChallanDate ?? DBNull.Value);
                        cmdUpdate.Parameters.AddWithValue("@TotalBox", model.TotalBox);
                        cmdUpdate.Parameters.AddWithValue("@TotalWeight", model.TotalWeight);

                        cmdUpdate.ExecuteNonQuery();

                        SqlCommand cmdDelete = new SqlCommand(
                            "DELETE FROM YARNOUTWARDScanMasterDetails WHERE YarnOutwardScanID=@YarnOutwardScanID",
                            con, tran);

                        cmdDelete.Parameters.AddWithValue("@YarnOutwardScanID", YarnOutwardScanID);
                        cmdDelete.ExecuteNonQuery();
                    }

                    // INSERT DETAILS
                    foreach (var item in model.Details)
                    {
                        SqlCommand cmdDetail = new SqlCommand(@"
                 INSERT INTO YARNOUTWARDScanMasterDetails
                (DateAndTime,YarnOutwardScanID,YarnMaterial,YarnColour,GodownLocationID,PartyID,
                 BarcodeNo,NetWeight,BoxNo,YarnInwardDetailID,UserAccountId,YarnCompany)
                VALUES
                (GETDATE(),@YarnOutwardScanID,@YarnMaterial,@YarnColour,@GodownLocationID,@PartyID,
                 @BarcodeNo,@NetWeight,@BoxNo,@YarnInwardDetailID,@UserAccountId,@YarnCompany)", con, tran);

                        cmdDetail.Parameters.AddWithValue("@YarnOutwardScanID", YarnOutwardScanID);
                        cmdDetail.Parameters.AddWithValue("@YarnMaterial", item.YarnMaterialID);
                        cmdDetail.Parameters.AddWithValue("@YarnColour", item.YarnColorID);
                        cmdDetail.Parameters.AddWithValue("@GodownLocationID", item.GodownLocationID);
                        cmdDetail.Parameters.AddWithValue("@PartyID", model.PartyId);
                        cmdDetail.Parameters.AddWithValue("@BarcodeNo", item.BarcodeNo);
                        cmdDetail.Parameters.AddWithValue("@NetWeight", item.NetWeight);
                        cmdDetail.Parameters.AddWithValue("@BoxNo", item.BoxNo);
                        cmdDetail.Parameters.AddWithValue("@YarnInwardDetailID", item.YarnInwardDetailID);
                        cmdDetail.Parameters.AddWithValue("@UserAccountId", model.UserId);
                        cmdDetail.Parameters.AddWithValue("@YarnCompany", item.YarnCompany);
                        cmdDetail.ExecuteNonQuery();
                        SqlCommand cmdYarnInwardDetail = new SqlCommand(@"
                        UPDATE YarnInwardDetail  SET IsScanStutas = 1 , IsScanUpdateDateTime=GetDate() WHERE YarnInwardDetailID = @YarnInwardDetailID", con, tran);
                        cmdYarnInwardDetail.Parameters.AddWithValue("@YarnInwardDetailID", item.YarnInwardDetailID);
                        cmdYarnInwardDetail.ExecuteNonQuery();
                    }

                    SqlCommand cmdUpdateMaster = new SqlCommand(@"
            UPDATE YARNOUTWARDMaster  SET IsScanStutas = 1 WHERE YarnOutwardID = @YarnOutwardID", con, tran);

                    cmdUpdateMaster.Parameters.AddWithValue("@YarnOutwardID", YarnOutwardID);
                    cmdUpdateMaster.ExecuteNonQuery();

                    tran.Commit();

                    response.Code = 200;
                    response.Message = "Yarn Outward saved successfully.";
                }
            }
            catch (Exception ex)
            {
                if (tran != null)
                    tran.Rollback();
                response.Code = 500;
                response.Message = ex.Message;
            }
            js.MaxJsonLength = Int32.MaxValue;
            Context.Response.Write(js.Serialize(response));
            Context.Response.Flush();
            Context.Response.SuppressContent = true;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }



        #region Sale Order Entry API
        [WebMethod]
        public void GetSaleOrderListAPI(int page = 1, int pageSize = 10)
        {
            try
            {
                string conStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conStr))
                {
                    con.Open();

                    int offset = (page - 1) * pageSize;

                    string query = @"
                SELECT 
                    SO.SaleOrderID,
                    SO.OrderNo,
                    SO.OrderDate,
                    PM.PartyName,
                    BM.BrokerName,
                    TM.TransportName,
                    SO.TotalQty,
                    SO.InvoiceAmount
                FROM SaleOrder SO
                LEFT JOIN PartyMaster PM ON SO.PartyId = PM.PartyId
                LEFT JOIN BrokerMaster BM ON SO.BrokerID = BM.BrokerCode
                LEFT JOIN TransportMaster TM ON SO.TransportID = TM.TransportCode
                ORDER BY CAST(SO.OrderNo AS INT) DESC, SO.OrderDate DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        List<SaleOrderVM> list = new List<SaleOrderVM>();

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                list.Add(new SaleOrderVM
                                {
                                    SaleOrderID = Convert.ToInt32(rdr["SaleOrderID"]),
                                    OrderNo = rdr["OrderNo"].ToString(),
                                    OrderDate = Convert.ToDateTime(rdr["OrderDate"]).ToString("dd-MM-yyyy"),
                                    PartyName = rdr["PartyName"] == DBNull.Value ? "" : rdr["PartyName"].ToString(),
                                    BrokerName = rdr["BrokerName"] == DBNull.Value ? "" : rdr["BrokerName"].ToString(),
                                    TransportName = rdr["TransportName"] == DBNull.Value ? "" : rdr["TransportName"].ToString(),
                                    TotalQty = rdr["TotalQty"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["TotalQty"]),
                                    InvoiceAmount = rdr["InvoiceAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["InvoiceAmount"])
                                });
                            }
                        }

                        int totalRecords;
                        using (SqlCommand countCmd = new SqlCommand("SELECT COUNT(*) FROM SaleOrder", con))
                        {
                            totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                        }

                        var response = new
                        {
                            Code = 200,
                            success = true,
                            Data = list,
                            totalRecords = totalRecords,
                            currentPage = page,
                            pageSize = pageSize
                        };

                        JavaScriptSerializer js = new JavaScriptSerializer();
                        js.MaxJsonLength = int.MaxValue;

                        Context.Response.Clear();
                        Context.Response.ContentType = "application/json";
                        Context.Response.Write(js.Serialize(response));
                        Context.Response.Flush();
                        Context.Response.SuppressContent = true;
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Code = 400,
                    success = false,
                    message = ex.Message
                };

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = int.MaxValue;

                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                Context.Response.Write(js.Serialize(errorResponse));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
        [WebMethod]
        public void GetDesignEntryDataAPI(int DesignColorMatchingFormID)
        {
            try
            {
                List<DesignModel> designList = new List<DesignModel>();
                List<TypeModel> typeList = new List<TypeModel>();
                List<UnitModel> unitList = new List<UnitModel>();
                List<ColorGroupModel> colorList = new List<ColorGroupModel>();
                HashSet<int> typeSet = new HashSet<int>();
                HashSet<int> unitSet = new HashSet<int>();
                HashSet<int> colorSet = new HashSet<int>();
                string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"SELECT DCMD.DesignColorMatchingDetailsID,DCMD.MatchingNo,DCMD.MatchingID,DCMD.WarpMatchingID,DCMD.FeederMatchingID,
                DCMD.ColorMatchingPhoto,DEFM.DesignEntryFormID,DCMFM.DesignColorMatchingFormID,DEFM.DesignNo,DEFM.DesignerCode,DEFM.TypeID, DEFM.PhotoOfDesign,
                TM.Type,ISNULL(UM.UnitCode, '') AS UnitCode,ISNULL(UM.UnitId, 0) AS UnitId,ISNULL(DCMD.ColorGroupID, 0) AS ColorGroupID,
                ISNULL(CGM.ColorGroup, '') AS ColorGroup,ISNULL(DEFM.SaleRate, 0) AS SaleRate  
                    FROM DesignEntryFormMaster DEFM
            INNER JOIN DesignColorMatchingFormMaster DCMFM ON DCMFM.DesignEntryFormID = DEFM.DesignEntryFormID
            LEFT JOIN TypeMaster TM ON TM.TypeID = DEFM.TypeID
            LEFT JOIN UnitMaster UM ON UM.UnitId = TM.UnitId
            LEFT JOIN DesignColorMatchingDetails DCMD ON DCMD.DesignColorMatchingFormID = DCMFM.DesignColorMatchingFormID
            LEFT JOIN ColorGroupMaster CGM ON CGM.ColorGroupID = DCMD.ColorGroupID
                where DCMFM.DesignColorMatchingFormID = "+ DesignColorMatchingFormID + " ORDER BY DEFM.DesignEntryFormID DESC";

                    con.Open();

                    SqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        int designId = Convert.ToInt32(rdr["DesignEntryFormID"]);

                        // FIND EXISTING DESIGN
                        var design = designList
                            .FirstOrDefault(d => d.DesignEntryFormID == designId);

                        if (design == null)
                        {
                            design = new DesignModel
                            {
                                DesignEntryFormID = designId,

                                DesignColorMatchingFormID =
                                    rdr["DesignColorMatchingFormID"] != DBNull.Value
                                    ? Convert.ToInt32(rdr["DesignColorMatchingFormID"])
                                    : 0,

                                DesignNo = rdr["DesignNo"].ToString().ToUpper(),

                                DesignerCode = rdr["DesignerCode"]
                                    .ToString().ToUpper(),

                                TypeID = rdr["TypeID"] != DBNull.Value
                                    ? Convert.ToInt32(rdr["TypeID"])
                                    : 0,

                                Type = rdr["Type"].ToString().ToUpper(),

                                UnitId = rdr["UnitId"] != DBNull.Value
                                    ? Convert.ToInt32(rdr["UnitId"])
                                    : 0,

                                UnitCode = rdr["UnitCode"]
                                    .ToString().ToUpper(),

                                SaleRate = rdr["SaleRate"] != DBNull.Value
                                    ? Convert.ToDecimal(rdr["SaleRate"])
                                    : 0,

                                ColorGroups = new List<ColorGroupModel>(),

                                DesignColorMatchingDetails =
                                    new List<DesignColorMatchingDetailsModel>()
                            };

                            designList.Add(design);
                        }

                        // ADD COLOR GROUPS
                        if (rdr["ColorGroupID"] != DBNull.Value)
                        {
                            int colorId = Convert.ToInt32(rdr["ColorGroupID"]);

                            if (!design.ColorGroups
                                .Any(c => c.ColorGroupID == colorId))
                            {
                                design.ColorGroups.Add(new ColorGroupModel
                                {
                                    ColorGroupID = colorId,
                                    ColorGroup = rdr["ColorGroup"].ToString()
                                });
                            }

                            // GLOBAL COLOR LIST
                            if (!colorSet.Contains(colorId))
                            {
                                colorSet.Add(colorId);

                                colorList.Add(new ColorGroupModel
                                {
                                    ColorGroupID = colorId,
                                    ColorGroup = rdr["ColorGroup"].ToString()
                                });
                            }
                        }

                        // DESIGN COLOR MATCHING DETAILS
                        if (rdr["DesignColorMatchingDetailsID"] != DBNull.Value)
                        {
                            int detailsId =
                                Convert.ToInt32(
                                    rdr["DesignColorMatchingDetailsID"]);

                            if (!design.DesignColorMatchingDetails
                                .Any(x => x.DesignColorMatchingDetailsID == detailsId))
                            {
                                design.DesignColorMatchingDetails.Add(
                                    new DesignColorMatchingDetailsModel
                                    {
                                        DesignColorMatchingDetailsID = detailsId,
                                        MatchingNo =rdr["MatchingNo"] != DBNull.Value? Convert.ToInt32(rdr["MatchingNo"]): 0,
                                        MatchingID =rdr["MatchingID"].ToString(),
                                        WarpMatchingID =rdr["WarpMatchingID"].ToString(),
                                        FeederMatchingID =rdr["FeederMatchingID"].ToString(),
                                        ColorMatchingPhoto =rdr["ColorMatchingPhoto"].ToString(),
                                        ColorGroupID =rdr["ColorGroupID"] != DBNull.Value ? Convert.ToInt32(rdr["ColorGroupID"]) : 0,
                                    });
                            }
                        }

                        // TYPE LIST
                        int typeId = rdr["TypeID"] != DBNull.Value
                            ? Convert.ToInt32(rdr["TypeID"])
                            : 0;

                        if (!typeSet.Contains(typeId))
                        {
                            typeSet.Add(typeId);

                            typeList.Add(new TypeModel
                            {
                                TypeID = typeId,
                                Type = rdr["Type"].ToString().ToUpper()
                            });
                        }

                        // UNIT LIST
                        int unitId = rdr["UnitId"] != DBNull.Value
                            ? Convert.ToInt32(rdr["UnitId"])
                            : 0;

                        if (!unitSet.Contains(unitId))
                        {
                            unitSet.Add(unitId);

                            unitList.Add(new UnitModel
                            {
                                UnitId = unitId,
                                UnitCode = rdr["UnitCode"]
                                    .ToString().ToUpper()
                            });
                        }
                    }

                    rdr.Close();
                    con.Close();
                }

                // FINAL RESPONSE
                DesignEntryDataResponse DataResponse =
                    new DesignEntryDataResponse
                    {
                        Designs = designList,
                        Types = typeList,
                        Units = unitList,
                        ColorGroups = colorList
                    };

                var response = new
                {
                    Code = 200,
                    success = true,
                    Data = DataResponse
                };

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;

                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;

                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Code = 400,
                    success = false,
                    message = ex.Message
                };

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;

                Context.Response.Write(js.Serialize(errorResponse));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;

                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
        [WebMethod]
        public void GetDesignDataMasterAPI()
        {
            try
            {
                List<DesignMasterModel> designList =
                    new List<DesignMasterModel>();

                string cs = ConfigurationManager
                    .ConnectionStrings["sqlconnstr"]
                    .ConnectionString;

                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand();

                    cmd.Connection = con;
                    cmd.CommandType = CommandType.Text;

                    cmd.CommandText = @"SELECT DEFM.DesignEntryFormID,DCMFM.DesignColorMatchingFormID,
                            DEFM.DesignNo,
                            DEFM.DesignerCode,
                            DEFM.BarcodeNo
                        FROM DesignEntryFormMaster DEFM
                        INNER JOIN DesignColorMatchingFormMaster DCMFM 
                            ON DCMFM.DesignEntryFormID = DEFM.DesignEntryFormID
                        ORDER BY DEFM.DesignEntryFormID DESC";

                    con.Open();

                    SqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        int designId = Convert.ToInt32(rdr["DesignEntryFormID"]);

                        var design = designList.FirstOrDefault(d => d.DesignEntryFormID == designId);

                        if (design == null)
                        {
                            design = new DesignMasterModel
                            {
                                DesignEntryFormID = designId,
                                DesignColorMatchingDetailsID = rdr["DesignColorMatchingFormID"] != DBNull.Value ? Convert.ToInt32(rdr["DesignColorMatchingFormID"]): 0,
                                DesignNo = rdr["DesignNo"].ToString().ToUpper(),
                                DesignerCode = rdr["DesignerCode"].ToString().ToUpper(),
                                BarcodeNo = rdr["BarcodeNo"].ToString()
                            };
                            designList.Add(design);
                        }
                    }
                    rdr.Close();
                    con.Close();
                }
                var response = new
                {
                    Code = 200,
                    success = true,
                    Data = designList
                };

                JavaScriptSerializer js = new JavaScriptSerializer();

                js.MaxJsonLength = Int32.MaxValue;

                Context.Response.Write(js.Serialize(response));

                Context.Response.Flush();
                Context.Response.SuppressContent = true;

                HttpContext.Current.ApplicationInstance
                    .CompleteRequest();
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Code = 400,
                    success = false,
                    message = ex.Message
                };

                JavaScriptSerializer js =
                    new JavaScriptSerializer();

                js.MaxJsonLength = Int32.MaxValue;

                Context.Response.Write(js.Serialize(errorResponse));

                Context.Response.Flush();
                Context.Response.SuppressContent = true;

                HttpContext.Current.ApplicationInstance
                    .CompleteRequest();
            }
        }
        [WebMethod]
        public void GetDefaultDataLoadAPI()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                List<object> partyList = new List<object>();
                List<object> brokerList = new List<object>();
                List<object> transportList = new List<object>();
                List<object> additionalRemark = new List<object>();
                List<object> gstdata = new List<object>();

                int nextOrderNo = 0;
                DateTime serverDate = DateTime.Now;

                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();

                    // ✅ Party List
                    using (SqlCommand cmd = new SqlCommand(@"SELECT PartyId,PartyName,GSTIN,BillingAddress FROM PartyMaster ORDER BY PartyId DESC", con))
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            partyList.Add(new
                            {
                                PartyId = dr["PartyId"],
                                PartyName = dr["PartyName"].ToString(),
                                GSTIN = dr["GSTIN"].ToString(),
                                BillingAddress = dr["BillingAddress"].ToString()
                            });
                        }
                        dr.Close();
                    }

                    // ✅ Broker List
                    using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 10 BrokerId,BrokerCode,BrokerName,MobileNo FROM BrokerMaster ORDER BY BrokerId DESC", con))
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            brokerList.Add(new
                            {
                                BrokerId = dr["BrokerId"],
                                BrokerCode = dr["BrokerCode"].ToString(),
                                BrokerName = dr["BrokerName"].ToString(),
                                MobileNo = dr["MobileNo"].ToString()
                            });
                        }
                        dr.Close();
                    }

                    // ✅ Transport List
                    using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 10 TransportId,TransportCode,TransportName,Address,CityName FROM TransportMaster ORDER BY TransportCode DESC", con))
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            transportList.Add(new
                            {
                                TransportId = dr["TransportId"],
                                TransportCode = dr["TransportCode"],
                                TransportName = dr["TransportName"].ToString(),
                                Address = dr["Address"].ToString(),
                                CityName = dr["CityName"].ToString()
                            });
                        }
                        dr.Close();
                    }

                    // ✅ Server Date
                    using (SqlCommand cmd = new SqlCommand("SELECT GETDATE()", con))
                    {
                        serverDate = (DateTime)cmd.ExecuteScalar();
                    }

                    // ✅ Next Order No
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(CAST(OrderNo AS INT)), 0) + 1 FROM SaleOrder", con))
                    {
                        nextOrderNo = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                                

                    using (SqlCommand cmd = new SqlCommand(@"SELECT DISTINCT AdditionalRemark  FROM SaleOrder  WHERE ISNULL(AdditionalRemark,'') <> ''  ORDER BY AdditionalRemark", con))
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            additionalRemark.Add(new
                            {
                                AdditionalRemark = dr["AdditionalRemark"].ToString()
                            });
                        }
                        dr.Close();
                    }

                    using (SqlCommand cmd = new SqlCommand(@"select * from GSTSLABMaster  order by GSTSLABName", con))
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            gstdata.Add(new
                            {
                                GSTSLABId = dr["GSTSLABId"],
                                GSTSLABName = dr["GSTSLABName"].ToString(),
                                GST = dr["GST"],
                                IGST = dr["IGST"],

                            });
                        }
                    }                       
                }

                var response = new
                {
                    Code = 200,
                    success = true,
                    Data = new
                    {
                        PartyList = partyList,
                        BrokerList = brokerList,
                        TransportList = transportList,
                        AdditionalRemark= additionalRemark,
                        GstData= gstdata,
                        ServerDate = serverDate.ToString("dd-MM-yyyy"),
                        NextOrderNo = nextOrderNo
                    }
                };

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;

                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Code = 400,
                    success = false,
                    message = ex.Message
                };

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;
                Context.Response.Write(js.Serialize(errorResponse));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
        [WebMethod]
        public void GetDefaultDataSearchAPI(string partySearch = "", string brokerSearch = "", string transportSearch = "")
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                List<object> partyList = new List<object>();
                List<object> brokerList = new List<object>();
                List<object> transportList = new List<object>();

                int nextOrderNo = 0;
                DateTime serverDate;

                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();

                    if (!string.IsNullOrWhiteSpace(partySearch))
                    {
                        // ✅ Party Filter
                        using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 20 PartyId, PartyName, GSTIN, BillingAddress FROM PartyMaster 
                                                            WHERE (@partySearch = '' OR PartyName LIKE '%' + @partySearch + '%')
                                                            ORDER BY PartyName ASC", con))
                        {
                            cmd.Parameters.AddWithValue("@partySearch", partySearch ?? "");

                            SqlDataReader dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                partyList.Add(new
                                {
                                    PartyId = dr["PartyId"],
                                    PartyName = dr["PartyName"].ToString(),
                                    GSTIN = dr["GSTIN"].ToString(),
                                    BillingAddress = dr["BillingAddress"].ToString()
                                });
                            }
                            dr.Close();
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(brokerSearch))
                    {

                        // ✅ Broker Filter
                        using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 10 BrokerId, BrokerCode, BrokerName, MobileNo FROM BrokerMaster 
                                                WHERE (@brokerSearch = '' OR BrokerName LIKE '%' + @brokerSearch + '%' ) ORDER BY BrokerName ASC", con))
                        {
                            cmd.Parameters.AddWithValue("@brokerSearch", brokerSearch ?? "");

                            SqlDataReader dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                brokerList.Add(new
                                {
                                    BrokerId = dr["BrokerId"],
                                    BrokerCode = dr["BrokerCode"].ToString(),
                                    BrokerName = dr["BrokerName"].ToString(),
                                    MobileNo = dr["MobileNo"].ToString()
                                });
                            }
                            dr.Close();
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(transportSearch))
                    {
                        // ✅ Transport Filter
                        using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 10 TransportCode, TransportName, Address, CityName FROM TransportMaster 
                                                            WHERE (@transportSearch = '' OR TransportName LIKE '%' + @transportSearch + '%')
                                                    ORDER BY TransportName ASC ", con))
                        {
                            cmd.Parameters.AddWithValue("@transportSearch", transportSearch ?? "");

                            SqlDataReader dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                transportList.Add(new
                                {
                                    TransportCode = dr["TransportCode"],
                                    TransportName = dr["TransportName"].ToString(),
                                    Address = dr["Address"].ToString(),
                                    CityName = dr["CityName"].ToString()
                                });
                            }
                            dr.Close();
                        }
                    }

                    // ✅ Server Date
                    using (SqlCommand cmd = new SqlCommand("SELECT GETDATE()", con))
                    {
                        serverDate = (DateTime)cmd.ExecuteScalar();
                    }

                    // ✅ Next Order No
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(OrderNo),0) + 1 FROM SaleOrder", con))
                    {
                        nextOrderNo = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }

                var response = new
                {
                    Code = 200,
                    success = true,
                    Data = new
                    {
                        PartyList = partyList,
                        BrokerList = brokerList,
                        TransportList = transportList,
                        ServerDate = serverDate,
                        NextOrderNo = nextOrderNo
                    }
                };

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;

                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Code = 400,
                    success = false,
                    message = ex.Message
                };

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;

                Context.Response.Write(js.Serialize(errorResponse));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
        [WebMethod]
        private string GetNextOrderNo(SqlConnection con, SqlTransaction tran)
        {
            using (SqlCommand cmd = new SqlCommand(@"
        SELECT ISNULL(MAX(TRY_CAST(OrderNo AS INT)),0) + 1
        FROM SaleOrder WITH (UPDLOCK, HOLDLOCK)", con, tran))
            {
                return Convert.ToString(cmd.ExecuteScalar());
            }
        }

        [WebMethod]
        public ApiResponse<int> SaveSaleOrderAPI(SaleOrderModel model)
        {
            if (model == null)
            {
                return new ApiResponse<int>
                {
                    Code = 400,
                    success = false,
                    Message = "Invalid data"
                };
            }

            try
            {
                using (SqlConnection con = new SqlConnection(
                    ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString))
                {
                    con.Open();

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            int saleOrderId = 0;

                            using (SqlCommand cmd = new SqlCommand())
                            {
                                cmd.Connection = con;
                                cmd.Transaction = trans;

                                // INSERT
                                if (model.SaleOrderID == 0)
                                {
                                    model.OrderNo = GetNextOrderNo(con, trans);

                                    cmd.CommandText = @"
                            INSERT INTO SaleOrder
                            (
                                DateAndTime,
                                OrderNo,
                                OrderDate,
                                PartyID,
                                BrokerID,
                                MarkupPercent,
                                TransportID,
                                TotalQty,
                                TotalAmount,
                                DiscountPercent,
                                DiscountAmount,
                                GSTPercent,
                                GSTAmount,
                                InvoiceAmount,
                                Remark,
                                UserAccountId,
                                FinancialYearID,
                                CompanyId,
                                DiscountType,
                                AdditionalRemark,
                                AdditionalValue,
                                RoundOff
                            )
                            VALUES
                            (
                                GETDATE(),
                                @OrderNo,
                                @OrderDate,
                                @PartyID,
                                @BrokerID,
                                @MarkupPercent,
                                @TransportID,
                                @TotalQty,
                                @TotalAmount,
                                @DiscountPercent,
                                @DiscountAmount,
                                @GSTPercent,
                                @GSTAmount,
                                @InvoiceAmount,
                                @Remark,
                                @UserAccountId,
                                @FinancialYearID,
                                @CompanyId,
                                @DiscountType,
                                @AdditionalRemark,
                                @AdditionalValue,
                                @RoundOff
                            );

                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                                    AddParams(cmd, model);

                                    saleOrderId = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                                else
                                {
                                    saleOrderId = model.SaleOrderID;

                                    // Existing OrderNo Fetch
                                    using (SqlCommand orderCmd = new SqlCommand(
                                        "SELECT OrderNo FROM SaleOrder WHERE SaleOrderID=@Id",
                                        con, trans))
                                    {
                                        orderCmd.Parameters.AddWithValue("@Id", saleOrderId);
                                        model.OrderNo = Convert.ToString(orderCmd.ExecuteScalar());
                                    }

                                    cmd.CommandText = @"
                            UPDATE SaleOrder
                            SET
                                OrderDate=@OrderDate,
                                PartyID=@PartyID,
                                BrokerID=@BrokerID,
                                MarkupPercent=@MarkupPercent,
                                TransportID=@TransportID,
                                TotalQty=@TotalQty,
                                TotalAmount=@TotalAmount,
                                DiscountPercent=@DiscountPercent,
                                DiscountAmount=@DiscountAmount,
                                GSTPercent=@GSTPercent,
                                GSTAmount=@GSTAmount,
                                InvoiceAmount=@InvoiceAmount,
                                Remark=@Remark,
                                DiscountType=@DiscountType,
                                AdditionalRemark=@AdditionalRemark,
                                AdditionalValue=@AdditionalValue,
                                RoundOff=@RoundOff
                            WHERE SaleOrderID=@SaleOrderID";

                                    AddParams(cmd, model);

                                    cmd.Parameters.Add("@SaleOrderID", SqlDbType.Int)
                                        .Value = saleOrderId;

                                    cmd.ExecuteNonQuery();

                                    // Delete Old Details
                                    using (SqlCommand delSub = new SqlCommand(@"
                                DELETE FROM SaleOrderSubDetails
                                WHERE DetailID IN
                                (
                                    SELECT DetailID
                                    FROM SaleOrderDetails
                                    WHERE SaleOrderID=@SaleOrderID
                                )", con, trans))
                                    {
                                        delSub.Parameters.AddWithValue("@SaleOrderID", saleOrderId);
                                        delSub.ExecuteNonQuery();
                                    }

                                    using (SqlCommand delDet = new SqlCommand(@"
                                DELETE FROM SaleOrderDetails
                                WHERE SaleOrderID=@SaleOrderID", con, trans))
                                    {
                                        delDet.Parameters.AddWithValue("@SaleOrderID", saleOrderId);
                                        delDet.ExecuteNonQuery();
                                    }
                                }
                            }

                            // DETAILS
                            if (model.Details != null && model.Details.Count > 0)
                            {
                                foreach (var d in model.Details)
                                {
                                    int detailId;

                                    using (SqlCommand cmdDetail = new SqlCommand(@"
                                INSERT INTO SaleOrderDetails
                                (
                                    SaleOrderID,
                                    DesignID,
                                    DesignNo,
                                    ItemType,
                                    NoOfColours,
                                    Qty,
                                    Unit,
                                    Rate,
                                    Amount,
                                    DesignColorMatchingID,
                                    BrokerRate
                                )
                                VALUES
                                (
                                    @SaleOrderID,
                                    @DesignID,
                                    @DesignNo,
                                    @ItemType,
                                    @NoOfColours,
                                    @Qty,
                                    @Unit,
                                    @Rate,
                                    @Amount,
                                    @DesignColorMatchingID,
                                    @BrokerRate
                                );

                                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                                        con, trans))
                                    {
                                        cmdDetail.Parameters.AddWithValue("@SaleOrderID", saleOrderId);
                                        cmdDetail.Parameters.AddWithValue("@DesignID", (object)d.DesignID ?? DBNull.Value);
                                        cmdDetail.Parameters.AddWithValue("@DesignNo", (object)d.DesignNo ?? DBNull.Value);
                                        cmdDetail.Parameters.AddWithValue("@ItemType", (object)d.ItemType ?? DBNull.Value);
                                        cmdDetail.Parameters.AddWithValue("@NoOfColours", d.NoOfColours);
                                        cmdDetail.Parameters.AddWithValue("@Qty", d.Qty);
                                        cmdDetail.Parameters.AddWithValue("@Unit", (object)d.Unit ?? DBNull.Value);
                                        cmdDetail.Parameters.AddWithValue("@Rate", d.Rate);
                                        cmdDetail.Parameters.AddWithValue("@Amount", d.Amount);
                                        cmdDetail.Parameters.AddWithValue("@DesignColorMatchingID", d.DesignColorMatchingID);
                                        cmdDetail.Parameters.AddWithValue("@BrokerRate", d.BrokerRate);

                                        detailId = Convert.ToInt32(cmdDetail.ExecuteScalar());
                                    }

                                    if (d.SubDetails != null)
                                    {
                                        foreach (var s in d.SubDetails)
                                        {
                                            using (SqlCommand cmdSub = new SqlCommand(@"
                                        INSERT INTO SaleOrderSubDetails
                                        (
                                            DetailID,
                                            ColourID,
                                            ColourName,
                                            Qty,
                                            Unit,
                                            Remark
                                        )
                                        VALUES
                                        (
                                            @DetailID,
                                            @ColourID,
                                            @ColourName,
                                            @Qty,
                                            @Unit,
                                            @Remark
                                        )", con, trans))
                                            {
                                                cmdSub.Parameters.AddWithValue("@DetailID", detailId);
                                                cmdSub.Parameters.AddWithValue("@ColourID", s.ColourID);
                                                cmdSub.Parameters.AddWithValue("@ColourName", (object)s.ColourName ?? DBNull.Value);
                                                cmdSub.Parameters.AddWithValue("@Qty", s.Qty);
                                                cmdSub.Parameters.AddWithValue("@Unit", (object)s.Unit ?? DBNull.Value);
                                                cmdSub.Parameters.AddWithValue("@Remark", (object)s.Remark ?? DBNull.Value);

                                                cmdSub.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                }
                            }

                            trans.Commit();

                          var response = new ApiResponse<int>
                            {
                                Code = 200,
                                success = true,
                                Data = saleOrderId,
                                Message = "Saved Successfully"
                            };
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            js.MaxJsonLength = Int32.MaxValue;
                            Context.Response.Write(js.Serialize(response));
                            Context.Response.Flush();
                            Context.Response.SuppressContent = true;
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return new ApiResponse<int>();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();

                            var response = new ApiResponse<int>
                            {
                                Code = 400,
                                success = false,
                                Message = ex.Message
                            };
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            js.MaxJsonLength = Int32.MaxValue;
                            Context.Response.Write(js.Serialize(response));
                            Context.Response.Flush();
                            Context.Response.SuppressContent = true;
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return new ApiResponse<int>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>
                {
                    Code = 400,
                    success = false,
                    Message = ex.Message
                };
            }
        }
        private void AddParams(SqlCommand cmd, SaleOrderModel model)
        {
            cmd.Parameters.Add("@OrderNo", SqlDbType.NVarChar).Value = model.OrderNo ?? "";
            cmd.Parameters.Add("@OrderDate", SqlDbType.DateTime).Value = model.OrderDate;
            cmd.Parameters.Add("@PartyID", SqlDbType.Int).Value = (model.PartyID);
            cmd.Parameters.Add("@BrokerID", SqlDbType.NVarChar).Value = (model.BrokerID);
            cmd.Parameters.Add("@TransportID", SqlDbType.NVarChar).Value = (model.TransportID);
            cmd.Parameters.Add("@MarkupPercent", SqlDbType.Decimal).Value = (model.MarkupPercent);
            cmd.Parameters.Add("@TotalQty", SqlDbType.Decimal).Value = (model.TotalQty);
            cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal).Value = (model.TotalAmount);
            cmd.Parameters.Add("@DiscountPercent", SqlDbType.Decimal).Value = (model.DiscountPercent);
            cmd.Parameters.Add("@DiscountAmount", SqlDbType.Decimal).Value = (model.DiscountAmount);
            cmd.Parameters.Add("@GSTPercent", SqlDbType.Decimal).Value = (model.GSTPercent);
            cmd.Parameters.Add("@GSTAmount", SqlDbType.Decimal).Value = (model.GSTAmount);
            cmd.Parameters.Add("@InvoiceAmount", SqlDbType.Decimal).Value = (model.InvoiceAmount);
            cmd.Parameters.Add("@Remark", SqlDbType.NVarChar).Value = model.Remark ?? "";
            cmd.Parameters.Add("@UserAccountId", SqlDbType.Int).Value = model.UserAccountId;
            cmd.Parameters.Add("@FinancialYearID", SqlDbType.Int).Value = model.FinancialYearID;
            cmd.Parameters.Add("@CompanyId", SqlDbType.Int).Value = model.CompanyId;
            cmd.Parameters.Add("@DiscountType", SqlDbType.NVarChar).Value = model.DiscountType;
            cmd.Parameters.Add("@AdditionalRemark", SqlDbType.NVarChar).Value = model.AdditionalRemark;
            cmd.Parameters.Add("@AdditionalValue", SqlDbType.NVarChar).Value = model.AdditionalValue;
            cmd.Parameters.Add("@RoundOff", SqlDbType.Decimal).Value = model.RoundOff;
        }
        [WebMethod]
        public ApiResponse<SaleOrderVM> GetSaleOrderByIdAPI(int SaleOrderID)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;
            try
            {

                string conStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conStr))
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT SO.SaleOrderID,SO.OrderNo,CONVERT(VARCHAR(10), SO.OrderDate, 23) AS OrderDate,SO.PartyId,PM.PartyName,SO.BrokerID,SO.TransportID,SO.MarkupPercent,
                        SO.TotalQty,SO.TotalAmount,So.DiscountType,SO.AdditionalRemark,SO.AdditionalValue,SO.DiscountPercent,SO.DiscountAmount,SO.GSTPercent,SO.GSTAmount,SO.InvoiceAmount,SO.Remark,SO.UserAccountId,SO.FinancialYearID,
                        SO.CompanyId,D.DetailID,D.SaleOrderID,D.DesignID,D.DesignNo,D.ItemType,TM.Type,D.NoOfColours,D.Qty AS DetailQty,D.Unit,D.Rate,D.Amount,SD.SubDetailID,D.DesignColorMatchingID,
                        SD.DetailID,SD.ColourID,SD.ColourName,SD.Qty AS SubQty,SD.Unit AS SubUnit,SD.Remark as SubRemark,D.BrokerRate,SO.RoundOff,    
                        ISNULL(DCMD.DesignColorMatchingDetailsID, 0) AS ColorDetailsID,
                        ISNULL(DCMD.ColorGroupID, 0) AS ColorGroupID,
                        ISNULL(CGM.ColorGroup, '') AS ColorGroup,
	                    DEFM.DesignEntryFormID, DCMFM.DesignColorMatchingFormID,DEFM.DesignNo,DEFM.DesignerCode,DEFM.TypeID,TM.Type,ISNULL(UM.UnitCode, '') AS UnitCode,
                        ISNULL(UM.UnitId, 0) AS UnitId,ISNULL(DCMD.DesignColorMatchingDetailsID, 0) AS ColorDetailsID,
                        ISNULL(DCMD.ColorGroupID, 0) AS ColorGroupID,ISNULL(CGM.ColorGroup, '') AS ColorGroup,
	                    ISNULL(DEFM.SaleRate, 0) AS SaleRate 
                    FROM SaleOrder SO
                    LEFT JOIN PartyMaster PM ON SO.PartyId = PM.PartyId
                    LEFT JOIN SaleOrderDetails D ON SO.SaleOrderID = D.SaleOrderID
                    LEFT JOIN TypeMaster TM ON TM.TypeID = D.ItemType
                    LEFT JOIN SaleOrderSubDetails SD ON D.DetailID = SD.DetailID
                    LEFT JOIN UnitMaster UM ON UM.UnitId = TM.UnitId
                    LEFT JOIN DesignColorMatchingDetails DCMD ON SD.ColourID = DCMD.DesignColorMatchingDetailsID
                    LEFT JOIN DesignColorMatchingFormMaster DCMFM ON DCMFM.DesignColorMatchingFormID = DCMD.DesignColorMatchingFormID
                    LEFT JOIN DesignEntryFormMaster DEFM ON DEFM.DesignEntryFormID= DCMFM.DesignEntryFormID
                    LEFT JOIN ColorGroupMaster CGM ON CGM.ColorGroupID = DCMD.ColorGroupID
                    WHERE SO.SaleOrderID = @ID ORDER BY D.DetailID, SD.SubDetailID
                    ", con))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = SaleOrderID; // ✅ FIXED (no AddWithValue)

                    con.Open();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        SaleOrderVM order = null;

                        while (rdr.Read())
                        {
                            // ✅ MASTER
                            if (order == null)
                            {
                                order = new SaleOrderVM
                                {
                                    SaleOrderID = SafeInt(rdr["SaleOrderID"]),
                                    OrderNo = rdr["OrderNo"]?.ToString(),
                                    OrderDate = Convert.ToDateTime(rdr["OrderDate"]).ToString("dd-MM-yyyy"),
                                    PartyID = SafeInt(rdr["PartyId"]),
                                    PartyName = rdr["PartyName"]?.ToString(),
                                    BrokerID = (rdr["BrokerID"].ToString()),
                                    TransportID = rdr["TransportID"]?.ToString(),
                                    MarkupPercent = SafeDecimal(rdr["MarkupPercent"]),
                                    TotalQty = SafeDecimal(rdr["TotalQty"]),
                                    TotalAmount = SafeDecimal(rdr["TotalAmount"]),
                                    DiscountPercent = SafeDecimal(rdr["DiscountPercent"]),
                                    DiscountAmount = SafeDecimal(rdr["DiscountAmount"]),
                                    GSTPercent = SafeDecimal(rdr["GSTPercent"]),
                                    GSTAmount = SafeDecimal(rdr["GSTAmount"]),
                                    InvoiceAmount = SafeDecimal(rdr["InvoiceAmount"]),
                                    Remark = rdr["Remark"]?.ToString(),
                                    ColourID = SafeInt(rdr["ColourID"]),
                                    ColourName = rdr["ColourName"]?.ToString(),
                                    Qty = SafeDecimal(rdr["SubQty"]),
                                    Unit = rdr["SubUnit"]?.ToString(),
                                    SaleRate = SafeDecimal(rdr["SaleRate"]),
                                    DesignNo = (rdr["DesignEntryFormID"]?.ToString() + "|" + rdr["DesignColorMatchingFormID"]?.ToString()),
                                    DesignName = rdr["DesignNo"]?.ToString(),
                                    ColorGroupId = SafeInt(rdr["ColorGroupID"]),
                                    ColorGroup = rdr["ColorGroup"]?.ToString(),
                                    ColorDetailsID = SafeInt(rdr["ColorDetailsID"]?.ToString()),
                                    UnitId = SafeInt(rdr["UnitId"]?.ToString()),
                                    TypeID = SafeInt(rdr["TypeID"]?.ToString()),
                                    AdditionalValue = SafeDecimal(rdr["AdditionalValue"]),
                                    AdditionalRemark = (rdr["AdditionalRemark"].ToString()),
                                    DiscountType = (rdr["AdditionalValue"].ToString()),
                                    RoundOff = SafeDecimal(rdr["RoundOff"]),
                                    Details = new List<SaleOrderDetailVM>()
                                };
                            }

                            // ✅ DETAIL
                            if (rdr["DetailID"] != DBNull.Value)
                            {
                                int detailId = SafeInt(rdr["DetailID"]);

                                var detail = order.Details
                                    .FirstOrDefault(x => x.DetailID == detailId);

                                if (detail == null)
                                {
                                    detail = new SaleOrderDetailVM
                                    {
                                        DetailID = detailId,
                                        colorId = SafeInt(rdr["ColourID"]?.ToString()),
                                        ColorGroupId = SafeInt(rdr["ColorGroupId"]),
                                        ColorGroup = (rdr["ColorGroup"].ToString()),
                                        colorName = (rdr["ColourName"].ToString()),
                                        ItemType = SafeInt(rdr["ItemType"]),
                                        Type = rdr["Type"]?.ToString(),
                                        NoOfColours = SafeInt(rdr["NoOfColours"]),
                                        Qty = SafeDecimal(rdr["DetailQty"]),
                                        Unit = rdr["Unit"]?.ToString(),
                                        TotalQty = SafeDecimal(rdr["TotalQty"]),
                                        Rate = SafeDecimal(rdr["Rate"]),
                                        Amount = SafeDecimal(rdr["Amount"]),
                                        SaleRate = SafeDecimal(rdr["SaleRate"]),
                                        DesignId = (rdr["DesignID"]?.ToString()),
                                        DesignNo = (rdr["DesignNo"]?.ToString()),
                                        DesignColorMatchingID = SafeInt (rdr["DesignColorMatchingID"]),
                                        DesignName = rdr["DesignNo"]?.ToString(),
                                        ColorDetailsID = SafeInt(rdr["ColorDetailsID"]?.ToString()),
                                        UnitId = SafeInt(rdr["UnitId"]?.ToString()),
                                        TypeID = SafeInt(rdr["TypeID"]?.ToString()),
                                        MarkupPercent = SafeDecimal(rdr["MarkupPercent"]),
                                        ColourID = SafeInt(rdr["ColourID"]),
                                        ColourName = rdr["ColourName"]?.ToString(),
                                        BrokerRate = SafeDecimal(rdr["BrokerRate"]),
                                        SubDetails = new List<SaleOrderSubVM>()
                                    };

                                    order.Details.Add(detail);
                                }

                                // ✅ SUB DETAIL
                                if (rdr["SubDetailID"] != DBNull.Value)
                                {
                                    int subId = SafeInt(rdr["SubDetailID"]);

                                    if (!detail.SubDetails.Any(x => x.SubDetailID == subId)) // ✅ prevent duplicate
                                    {
                                        detail.SubDetails.Add(new SaleOrderSubVM
                                        {
                                            SubDetailID = subId,
                                            ColourID = SafeInt(rdr["ColourID"]),
                                            ColourName = rdr["ColourName"]?.ToString(),
                                            Qty = SafeDecimal(rdr["SubQty"]),
                                            Unit = rdr["SubUnit"]?.ToString(),
                                            SaleRate = SafeDecimal(rdr["SaleRate"]),
                                            DesignNo = (rdr["DesignEntryFormID"]?.ToString() + "|" + rdr["DesignColorMatchingFormID"]?.ToString()),
                                            DesignName = rdr["DesignNo"]?.ToString(),
                                            ColorGroupId = SafeInt(rdr["ColorGroupID"]),
                                            ColorGroup = rdr["ColorGroup"]?.ToString(),
                                            Remark = rdr["SubRemark"]?.ToString(),
                                            ColorDetailsID = SafeInt(rdr["ColorDetailsID"]?.ToString()),
                                            UnitId = SafeInt(rdr["UnitId"]?.ToString()),
                                            TypeID = SafeInt(rdr["TypeID"]?.ToString()),
                                            MarkupPercent = SafeDecimal(rdr["MarkupPercent"]),
                                        });
                                    }
                                }
                            }
                        }
                        if(order != null)
                        {
                            var response = new ApiResponse<SaleOrderVM>
                            {
                                Code = 200,
                                success = true,
                                Message = "",
                                Data = order ?? new SaleOrderVM { Details = new List<SaleOrderDetailVM>() }
                            };
                            Context.Response.Write(js.Serialize(response));
                            Context.Response.Flush();
                            Context.Response.SuppressContent = true;
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return new ApiResponse<SaleOrderVM>();
                        }
                        else
                        {
                            var response = new ApiResponse<SaleOrderVM>
                            {
                                Code = 400,
                                success = false,
                                Data=null,
                                Message="No Data Found"
                            };
                            Context.Response.Write(js.Serialize(response));
                            Context.Response.Flush();
                            Context.Response.SuppressContent = true;
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return new ApiResponse<SaleOrderVM>();                         
                        }
                     
                    }
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<SaleOrderVM>
                {
                    success = false,
                    Message = ex.Message
                };
            }
        }
        public decimal SafeDecimal(object val)
        {
            decimal result;
            return val == DBNull.Value || !decimal.TryParse(val.ToString(), out result)
                ? 0
                : result;
        }
        public int SafeInt(object val)
        {
            int result;
            return val == DBNull.Value || !int.TryParse(val.ToString(), out result)
                ? 0
                : result;
        }
        [WebMethod]       
        public ApiResponse<object> SaveTransport(TransportMaster obj)
        {
            string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;
            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // ✅ Fix TransportId null issue
                    int transportId = obj.TransportId <= 0 ? 0 : obj.TransportId;

                    // ✅ Duplicate mobile check
                    using (SqlCommand checkCmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM TransportMaster 
                WHERE MobileNo = @MobileNo AND TransportId <> @TransportId", con))
                    {
                        checkCmd.Parameters.Add("@MobileNo", SqlDbType.VarChar).Value = obj.MobileNo ?? "";
                        checkCmd.Parameters.Add("@TransportId", SqlDbType.Int).Value = transportId;

                        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                        {
                            return new ApiResponse<object>
                            {
                                success = false,
                                Message = "Mobile number already exists"
                            };
                        }
                    }

                    // ✅ Generate Code (safe)
                    string code = "";
                    if (transportId == 0)
                    {
                        using (SqlCommand cmdCode = new SqlCommand(@"
                    SELECT 'TR' + RIGHT('000' + CAST(ISNULL(MAX(CAST(SUBSTRING(TransportCode,3,LEN(TransportCode)) AS INT)),0) + 1 AS VARCHAR),3)
                    FROM TransportMaster", con))
                        {
                            code = cmdCode.ExecuteScalar()?.ToString();
                        }
                    }

                    if (transportId == 0)
                    {
                        // ✅ INSERT
                        using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO TransportMaster
                    (TransportCode,TransportName,FirmName,ContactPerson,Email,MobileNo,AlternateMobileNo,Address,CityName,PANCard,GSTNo,VehicleType,IsActive,UserAccountId,FinancialYearID,CompanyId,DateAndTime)
                    VALUES
                    (@Code,@Name,@Firm,@Contact,@Email,@Mobile,@AltMobile,@Address,@City,@PAN,@GST,@Vehicle,@Active,@UserId,@FY,@Comp,GETDATE())", con))
                        {
                            cmd.Parameters.Add("@Code", SqlDbType.VarChar).Value = code;
                            cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = obj.TransportName ?? "";
                            cmd.Parameters.Add("@Firm", SqlDbType.NVarChar).Value = obj.FirmName ?? "";
                            cmd.Parameters.Add("@Contact", SqlDbType.NVarChar).Value = obj.ContactPerson ?? "";
                            cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = obj.Email ?? "";
                            cmd.Parameters.Add("@Mobile", SqlDbType.VarChar).Value = obj.MobileNo ?? "";
                            cmd.Parameters.Add("@AltMobile", SqlDbType.VarChar).Value = obj.AlternateMobileNo ?? "";
                            cmd.Parameters.Add("@Address", SqlDbType.NVarChar).Value = obj.Address ?? "";
                            cmd.Parameters.Add("@City", SqlDbType.VarChar).Value = obj.CityName ?? "";
                            cmd.Parameters.Add("@PAN", SqlDbType.VarChar).Value = obj.PANCard ?? "";
                            cmd.Parameters.Add("@GST", SqlDbType.VarChar).Value = obj.GSTNo ?? "";
                            cmd.Parameters.Add("@Vehicle", SqlDbType.NVarChar).Value = obj.VehicleType ?? "";
                            cmd.Parameters.Add("@Active", SqlDbType.Bit).Value = obj.IsActive;
                            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = obj.UserAccountId;
                            cmd.Parameters.Add("@FY", SqlDbType.Int).Value = obj.FinancialYearID;
                            cmd.Parameters.Add("@Comp", SqlDbType.Int).Value = obj.CompanyId;

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // ✅ UPDATE
                        using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE TransportMaster SET 
                        TransportName=@Name,
                        FirmName=@Firm,
                        ContactPerson=@Contact,
                        Email=@Email,
                        MobileNo=@Mobile,
                        AlternateMobileNo=@AltMobile,
                        Address=@Address,
                        CityName=@City,
                        PANCard=@PAN,
                        GSTNo=@GST,
                        VehicleType=@Vehicle,
                        IsActive=@Active
                    WHERE TransportId=@Id", con))
                        {
                            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = transportId;
                            cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = obj.TransportName ?? "";
                            cmd.Parameters.Add("@Firm", SqlDbType.NVarChar).Value = obj.FirmName ?? "";
                            cmd.Parameters.Add("@Contact", SqlDbType.NVarChar).Value = obj.ContactPerson ?? "";
                            cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = obj.Email ?? "";
                            cmd.Parameters.Add("@Mobile", SqlDbType.VarChar).Value = obj.MobileNo ?? "";
                            cmd.Parameters.Add("@AltMobile", SqlDbType.VarChar).Value = obj.AlternateMobileNo ?? "";
                            cmd.Parameters.Add("@Address", SqlDbType.NVarChar).Value = obj.Address ?? "";
                            cmd.Parameters.Add("@City", SqlDbType.VarChar).Value = obj.CityName ?? "";
                            cmd.Parameters.Add("@PAN", SqlDbType.VarChar).Value = obj.PANCard ?? "";
                            cmd.Parameters.Add("@GST", SqlDbType.VarChar).Value = obj.GSTNo ?? "";
                            cmd.Parameters.Add("@Vehicle", SqlDbType.NVarChar).Value = obj.VehicleType ?? "";
                            cmd.Parameters.Add("@Active", SqlDbType.Bit).Value = obj.IsActive;

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                var response = new ApiResponse<object>
                {
                    Code = 200,
                    success = true,
                    Message = "Saved successfully",                      
                };
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<object>();              
            }
            catch (Exception ex)
            {
                var response = new ApiResponse<object>
                {
                    Code = 400,
                    success = true,
                    Message = ex.Message
                };
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<object>();
        
            }
        }
        [WebMethod]
        public ApiResponse<object> SaleOrderReport(List<int> saleOrderID)
        {
            try
            {

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;
                string folderPath = HttpContext.Current.Server.MapPath("~/GeneratedPDF/");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = "SaleOrder_" +
                                  DateTime.Now.ToString("yyyyMMddHHmmss") +
                                  ".pdf";

                string fullPath = System.IO.Path.Combine(folderPath, fileName);

                List<byte[]> pdfBytesList = new List<byte[]>();

                using (SqlConnection con = new SqlConnection(
                    ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString))
                {
                    con.Open();

                    foreach (int orderId in saleOrderID)
                    {
                        DataSet ds = new DataSet();

                        using (SqlDataAdapter daMaster =
                            new SqlDataAdapter("SPR_GetSaleOrder", con))
                        {
                            daMaster.SelectCommand.CommandType =
                                CommandType.StoredProcedure;

                            daMaster.SelectCommand.Parameters.AddWithValue(
                                "@SaleOrderID", orderId);

                            daMaster.Fill(ds, "SaleOrderMaster");
                        }

                        using (SqlDataAdapter daDetail =
                            new SqlDataAdapter("SPR_GetSaleOrderDetails", con))
                        {
                            daDetail.SelectCommand.CommandType =
                                CommandType.StoredProcedure;

                            daDetail.SelectCommand.Parameters.AddWithValue(
                                "@SaleOrderID", orderId);

                            daDetail.Fill(ds, "SaleOrderDetails");
                        }

                        if (!ds.Tables.Contains("SaleOrderMaster") ||
                            ds.Tables["SaleOrderMaster"].Rows.Count == 0)
                        {
                            continue;
                        }

                        if (!ds.Tables.Contains("SaleOrderDetails") ||
                            ds.Tables["SaleOrderDetails"].Rows.Count == 0)
                        {
                            continue;
                        }

                        Microsoft.Reporting.WebForms.LocalReport report = new Microsoft.Reporting.WebForms.LocalReport();

                        string reportPath =
                            HttpContext.Current.Server.MapPath(
                                "~/Reports/SaleOrderBill.rdlc");

                        if (!File.Exists(reportPath))
                        {
                            throw new Exception(
                                "RDLC file not found : " + reportPath);
                        }

                        report.ReportPath = reportPath;

                        report.DataSources.Clear();

                        report.DataSources.Add(
                            new Microsoft.Reporting.WebForms.ReportDataSource(
                                "SaleOrderMaster",
                                ds.Tables["SaleOrderMaster"]));

                        report.DataSources.Add(
                            new Microsoft.Reporting.WebForms.ReportDataSource(
                                "SaleOrderDetails",
                                ds.Tables["SaleOrderDetails"]));

                        report.Refresh();

                        string mimeType;
                        string encoding;
                        string extension;
                        string[] streamids;
                        Microsoft.Reporting.WebForms.Warning[] warnings;

                        byte[] bytes = report.Render(
                            "PDF",
                            null,
                            out mimeType,
                            out encoding,
                            out extension,
                            out streamids,
                            out warnings);

                        if (bytes != null && bytes.Length > 0)
                        {
                            pdfBytesList.Add(bytes);
                        }
                    }
                }

                if (pdfBytesList.Count == 0)
                {
                  var NoPDFgenerated = new ApiResponse<object>
                     {
                   
                        Code = 400,
                        success = false,
                        Message = "No PDF generated.",
                        Data = null
                    };
                    Context.Response.Write(js.Serialize(NoPDFgenerated));
                    Context.Response.Flush();
                    Context.Response.SuppressContent = true;
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                    return new ApiResponse<object>();
                }
            
                // Merge All PDFs
                using (Document document = new Document(iTextSharp.text.PageSize.A4))
                using (FileStream stream = new FileStream(fullPath, FileMode.Create))
                {
                    PdfCopy copy = new PdfCopy(document, stream);

                    document.Open();

                    foreach (byte[] pdfBytes in pdfBytesList)
                    {
                        using (PdfReader reader = new PdfReader(pdfBytes))
                        {
                            copy.AddDocument(reader);
                        }
                    }

                    document.Close();
                }

                string filePath =
                    HttpContext.Current.Request.Url
                    .GetLeftPart(UriPartial.Authority)
                    + "/GeneratedPDF/" + fileName;

                var response = new ApiResponse<object>
                {
                    Code = 200,
                    success = true,
                    Message = "PDF Generated Successfully",
                    Data = new
                    {
                        FilePath = filePath
                    }
                };
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<object>();
            }
            catch (Exception ex)
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;
                var response = new ApiResponse<object>
                {
                    Code = 500,
                    success = false,
                    Message = ex.ToString(),
                    Data = null
                };
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<object>();
            }
        }
        #endregion
    }
}
