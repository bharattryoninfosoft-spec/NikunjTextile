using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
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
        public void GetYarnOutwardScanAPI(string YarnOutwardID = "0")
        {
            try
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                YarnOutwardScanData data = new YarnOutwardScanData();
                List<YarnOutwardMasterAPI> yarnOutwards = new List<YarnOutwardMasterAPI>();

                int parsedOutwardId = 0;
                if (!string.IsNullOrEmpty(YarnOutwardID))
                {
                    if (!int.TryParse(YarnOutwardID, out parsedOutwardId))
                    {
                        try
                        {
                            string lastChar = YarnOutwardID.Length >= 6 ? YarnOutwardID.Substring(YarnOutwardID.Length - 6) : "";
                            string invoiceId = (lastChar != "%3d%3d") ? YarnOutwardID.Replace("==", "") + "%3d%3d" : YarnOutwardID;
                            parsedOutwardId = Convert.ToInt32(Decrypt(HttpUtility.UrlDecode(invoiceId)));
                        }
                        catch { }
                    }
                }

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
                            CASE 
                                WHEN YM.SundryPartyID = 3 THEN JM.Id 
                                ELSE PM.PartyId 
                            END AS PartyId,
                            CASE 
                                WHEN YM.SundryPartyID = 3 THEN JM.PartyName 
                                ELSE PM.PartyName 
                            END AS PartyName,
                            PM1.PartyId AS YarnCompanyId,
                            PM1.PartyName AS YarnCompany,
                            CASE 
                                WHEN YM.SundryPartyID = 3 THEN JM.BillingAddress 
                                ELSE PM.BillingAddress 
                            END AS Address,
                            YMM.YarnMaterialID AS YarnMaterialID,
                            YMM.YarnMaterial AS YarnMaterial,
                            YCM.YarnColorID AS YarnColorID,
                            YCM.YarnColor AS YarnColour,
                            ISNULL(YPM.CompanyCode, YCM.YarnColorCode) AS Code,
                            GLM.GodownLocationID,
                            GLM.LocationTitle AS Location,
                            N.BoxNo
                            FROM YarnOutwardDetail YOD
                            INNER JOIN YarnOutwardMaster YM 
                                ON YM.YarnOutwardID = YOD.YarnOutwardID
                            LEFT JOIN PartyMaster PM 
                                ON YOD.BillToPartyID = PM.PartyId
                            LEFT JOIN JobberMaster JM 
                                ON YM.PartyId = JM.Id
                            LEFT JOIN PartyMaster PM1 
                                ON YOD.YarnCompanyID = PM1.PartyId
                            LEFT JOIN YarnMaterialMaster YMM  
                                ON YMM.YarnMaterialID = YOD.YarnMaterialID
                            LEFT JOIN YarnColorMaster YCM  
                                ON YCM.YarnColorID = YOD.YarnColorID
                            LEFT JOIN (
                                SELECT YarnColorID, MAX(CompanyCode) AS CompanyCode 
                                FROM YarnPODetails GROUP BY YarnColorID
                            ) YPM ON YPM.YarnColorID = YOD.YarnColorID
                            LEFT JOIN GodownLocationMaster GLM  
                                ON GLM.GodownLocationID = YOD.GodownLocationID
                            CROSS APPLY
                            (
                                SELECT TOP (YOD.NoOfBox)
                                ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS BoxNo
                                FROM master..spt_values
                            ) N
                            WHERE YOD.YarnOutwardID = @YarnOutwardID
                            ORDER BY YOD.YarnOutwardDetailID, N.BoxNo";

                        cmd.Parameters.AddWithValue("@YarnOutwardID", parsedOutwardId);

                        con.Open();
                        SqlDataReader rdr = cmd.ExecuteReader();

                        while (rdr.Read())
                        {
                            YarnOutwardMasterAPI yarnOutward = new YarnOutwardMasterAPI();

                            yarnOutward.PartyId =
                       rdr["PartyId"] == DBNull.Value
                           ? 0
                           : Convert.ToInt32(rdr["PartyId"]);

                            yarnOutward.PartyName =
                                rdr["PartyName"] == DBNull.Value
                                    ? ""
                                    : rdr["PartyName"].ToString();

                            yarnOutward.CompanyId =
                                rdr["YarnCompanyId"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(rdr["YarnCompanyId"]);

                            yarnOutward.CompanyName =
                                rdr["YarnCompany"] == DBNull.Value
                                    ? ""
                                    : rdr["YarnCompany"].ToString();

                            yarnOutward.Address =
                                rdr["Address"] == DBNull.Value
                                    ? ""
                                    : rdr["Address"].ToString();

                            yarnOutward.YarnMaterialID =
                                rdr["YarnMaterialID"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(rdr["YarnMaterialID"]);

                            yarnOutward.YarnMaterial =
                                rdr["YarnMaterial"] == DBNull.Value
                                    ? ""
                                    : rdr["YarnMaterial"].ToString();

                            yarnOutward.YarnColorID =
                                rdr["YarnColorID"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(rdr["YarnColorID"]);

                            yarnOutward.YarnColor =
                                rdr["YarnColour"] == DBNull.Value
                                    ? ""
                                    : rdr["YarnColour"].ToString();

                            yarnOutward.YarnColorCode =
                                rdr["Code"] == DBNull.Value
                                    ? ""
                                    : rdr["Code"].ToString();

                            yarnOutward.GodownLocationID =
                                rdr["GodownLocationID"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(rdr["GodownLocationID"]);

                            yarnOutward.LocationTitle =
                                rdr["Location"] == DBNull.Value
                                    ? ""
                                    : rdr["Location"].ToString();

                            yarnOutward.BoxNo =
                                rdr["BoxNo"] == DBNull.Value
                                    ? ""
                                    : rdr["BoxNo"].ToString();

                            yarnOutwards.Add(yarnOutward);
                        }
                        data.ChallanNo = ChallanNo;
                        data.Items = yarnOutwards;
                        var response = new
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
                        var response = new
                        {
                            Code = 500,
                            Message = ex.Message,
                            Data = (object)null
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
                var response = new
                {
                    Code = 500,
                    Message = ex.Message,
                    Data = (object)null
                };
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
        private string Decrypt(string cipherText)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            cipherText = cipherText.Replace(" ", "+");

            // Fix missing padding for Base-64 string
            int mod4 = cipherText.Length % 4;
            if (mod4 > 0)
            {
                cipherText = cipherText.PadRight(cipherText.Length + (4 - mod4), '=');
            }

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetBoxWeightAPI(string BoxNo, int YarnMaterial = 0, int YarnColour = 0, int GodownLocationID = 0, int YarnCompany = 0)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

            YarnInwardDetail yarnOutward = new YarnInwardDetail();
            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;

            try
            {
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
                                  AND (@YarnMaterial = 0 OR YOSD.YarnMaterial = @YarnMaterial)
                                  AND (@YarnColour = 0 OR YOSD.YarnColour = @YarnColour)
                                  AND (@GodownLocationID = 0 OR YOSD.GodownLocationID = @GodownLocationID)
                                  AND (@YarnCompany = 0 OR YOSD.YarnCompany = @YarnCompany)
                            )
                            BEGIN
                                SELECT 
                                    'EXIST' AS Status,
                                    NULL AS YarnInwardDetailID,
                                    NULL AS BoxNo,
                                    NULL AS NetWeight,
                                    NULL AS BarcodeNo,
                                    NULL AS GodownLocationID,
                                    NULL AS LocationTitle,
                                    NULL AS YarnInwardID,
                                    NULL AS YarnMaterialID,
                                    NULL AS YarnMaterial,
                                    NULL AS YarnColorID,
                                    NULL AS YarnColor,
                                    NULL AS YarnColorCode,
                                    NULL AS PartyId,
                                    NULL AS PartyName
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
                                LEFT JOIN GodownLocationMaster GLM ON GLM.GodownLocationID = YID.GodownLocationID
                                WHERE YID.BarcodeNo = @BarcodeNo                                
                            END
                        ", con);

                    cmd.Parameters.Add("@BarcodeNo", SqlDbType.VarChar).Value = BoxNo ?? "";
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
                                Context.Response.Write(js.Serialize(new
                                {
                                    Code = 410,
                                    Message = $"This Box {BoxNo} already scanned.",
                                    Data = (object)null
                                }));
                            }
                            else
                            {
                                yarnOutward.YarnInwardDetailID = rdr["YarnInwardDetailID"] != DBNull.Value ? Convert.ToInt32(rdr["YarnInwardDetailID"]) : 0;
                                yarnOutward.BoxNo = rdr["BoxNo"]?.ToString();
                                yarnOutward.NetWeight = rdr["NetWeight"] != DBNull.Value ? Convert.ToDecimal(rdr["NetWeight"]) : 0;
                                yarnOutward.BarcodeNo = rdr["BarcodeNo"]?.ToString();
                                yarnOutward.GodownLocationID = rdr["GodownLocationID"] != DBNull.Value ? Convert.ToInt32(rdr["GodownLocationID"]) : 0;
                                yarnOutward.YarnInwardID = rdr["YarnInwardID"] != DBNull.Value ? Convert.ToInt32(rdr["YarnInwardID"]) : 0;
                                Context.Response.Write(js.Serialize(new
                                {
                                    Code = 200,
                                    Message = "Success",
                                    Data = yarnOutward
                                }));
                            }
                        }
                        else
                        {
                            Context.Response.Write(js.Serialize(new
                            {
                                Code = 404,
                                Message = "Box not found.",
                                Data = (object)null
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Response.Write(js.Serialize(new
                {
                    Code = 500,
                    Message = ex.Message,
                    Data = (object)null
                }));
            }

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

            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;
            string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
            SqlTransaction tran = null;

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();
                    tran = con.BeginTransaction();

                    int YarnOutwardID = 0;
                    if (!string.IsNullOrEmpty(model.YarnOutwardID))
                    {
                        if (!int.TryParse(model.YarnOutwardID, out YarnOutwardID))
                        {
                            try
                            {
                                string lastChar = model.YarnOutwardID.Length >= 6 ? model.YarnOutwardID.Substring(model.YarnOutwardID.Length - 6) : "";
                                string invoiceId = (lastChar != "%3d%3d") ? model.YarnOutwardID.Replace("==", "") + "%3d%3d" : model.YarnOutwardID;
                                YarnOutwardID = Convert.ToInt32(Decrypt(HttpUtility.UrlDecode(invoiceId)));
                            }
                            catch { }
                        }
                    }

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
                        cmdInsert.Parameters.AddWithValue("@UserAccountId", model.UserId > 0 ? model.UserId : 1);

                        YarnOutwardScanID = Convert.ToInt32(cmdInsert.ExecuteScalar());
                    }
                    else
                    {
                        // Reset IsScanStutas on old inward details before removing them
                        SqlCommand getOldCmd = new SqlCommand(
                            "SELECT YarnInwardDetailID FROM YARNOUTWARDScanMasterDetails WHERE YarnOutwardScanID = @YarnOutwardScanID",
                            con, tran);
                        getOldCmd.Parameters.AddWithValue("@YarnOutwardScanID", YarnOutwardScanID);
                        List<int> oldDetailIds = new List<int>();
                        using (SqlDataReader dr = getOldCmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                if (dr["YarnInwardDetailID"] != DBNull.Value)
                                    oldDetailIds.Add(Convert.ToInt32(dr["YarnInwardDetailID"]));
                            }
                        }
                        foreach (var oldId in oldDetailIds)
                        {
                            SqlCommand updateOld = new SqlCommand(
                                "UPDATE YarnInwardDetail SET IsScanStutas = 0, IsScanUpdateDateTime = '1900-01-01 00:00:00.000' WHERE YarnInwardDetailID = @YarnInwardDetailID",
                                con, tran);
                            updateOld.Parameters.AddWithValue("@YarnInwardDetailID", oldId);
                            updateOld.ExecuteNonQuery();
                        }

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

                    Context.Response.Write(js.Serialize(new
                    {
                        Code = 200,
                        Message = "Success",
                        Data = YarnOutwardScanID
                    }));
                }
            }
            catch (Exception ex)
            {
                if (tran != null)
                    tran.Rollback();
                Context.Response.Write(js.Serialize(new
                {
                    Code = 500,
                    Message = ex.Message,
                    Data = (object)null
                }));
            }
            Context.Response.Flush();
            Context.Response.SuppressContent = true;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        #region Sale Order Entry API
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetSaleOrderListAPI(int page = 1, int pageSize = 10)
        {
            try
            {
                string SearchRequirementNo = "";
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    SearchRequirementNo = HttpContext.Current.Request["SearchRequirementNo"]
                                       ?? HttpContext.Current.Request["search"]
                                       ?? HttpContext.Current.Request["Search"]
                                       ?? "";
                }

                string conStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conStr))
                {
                    con.Open();

                    int offset = (page - 1) * pageSize;
                    if (offset < 0) offset = 0;
                    if (pageSize <= 0) pageSize = 10;

                    string searchQuery = "";
                    if (!string.IsNullOrEmpty(SearchRequirementNo))
                    {
                        searchQuery = " WHERE (SO.OrderNo LIKE '%' + @Search + '%' OR PM.PartyName LIKE '%' + @Search + '%' OR BM.BrokerName LIKE '%' + @Search + '%' OR TMST.TransportName LIKE '%' + @Search + '%') ";
                    }

                    string query = @"
                SELECT 
                    SO.SaleOrderID,
                    SO.OrderNo,
                    CONVERT(VARCHAR(10), SO.OrderDate, 23) AS OrderDate,
                    ISNULL(PM.PartyName, '') AS PartyName,
                    ISNULL(BM.BrokerName, CAST(ISNULL(SO.BrokerID, '') AS VARCHAR(50))) AS BrokerName,
                    ISNULL(TMST.TransportName, CAST(ISNULL(SO.TransportID, '') AS VARCHAR(50))) AS TransportName,
                    ISNULL(SO.TotalQty, 0) AS TotalQty,
                    ISNULL(D.TotalRate, 0) + ISNULL(AC.ChargeValue, 0) AS Rate,
                    ISNULL(AC.ChargeRemark, '') AS ChargeRemark,
                    ISNULL(AC.ChargeValue, 0) AS ChargeValue,
                    CALC.SubTotalAmount,
                    ISNULL(SO.DiscountPercent, 0) AS DiscountPercent,
                    CALC.DiscountAmount,
                    CALC.TaxableAmount,
                    ISNULL(SO.GSTPercent, 0) AS GSTPercent,
                    CALC.GSTAmount,
                    CALC.RoundOff,
                    CALC.InvoiceAmount AS FinalInvoiceAmount,
                    SO.InvoiceAmount
                FROM SaleOrder SO
                LEFT JOIN PartyMaster PM ON SO.PartyId = PM.PartyId
                LEFT JOIN BrokerMaster BM ON SO.BrokerID = BM.BrokerCode
                LEFT JOIN TransportMaster TMST ON SO.TransportID = TMST.TransportCode
                OUTER APPLY (
                    SELECT MAX(ISNULL(SOA1.ChargeRemark, '')) AS ChargeRemark, SUM(ISNULL(SOA1.ChargeValue, 0)) AS ChargeValue
                    FROM SaleOrderAdditionalCharges SOA1 WHERE SOA1.SaleOrderID = SO.SaleOrderID
                ) AC
                OUTER APPLY (
                    SELECT SUM(ISNULL(D1.BrokerRate, 0)) AS TotalRate, SUM(ISNULL(D1.BrokerRate, 0) * ISNULL(D1.Qty, 0)) AS DetailAmount, SUM(ISNULL(D1.Qty, 0)) AS DetailQty
                    FROM SaleOrderDetails D1 WHERE D1.SaleOrderID = SO.SaleOrderID
                ) D
                CROSS APPLY (
                    SELECT ISNULL(D.DetailAmount, 0) + (ISNULL(AC.ChargeValue, 0) * ISNULL(D.DetailQty, 0)) AS SubTotalAmount
                ) S
                CROSS APPLY (
                    SELECT S.SubTotalAmount, S.SubTotalAmount * (ISNULL(SO.DiscountPercent, 0) / 100.0) AS DiscountAmount,
                           S.SubTotalAmount - (S.SubTotalAmount * (ISNULL(SO.DiscountPercent, 0) / 100.0)) AS TaxableAmount
                ) X
                CROSS APPLY (
                    SELECT X.SubTotalAmount, X.DiscountAmount, X.TaxableAmount, X.TaxableAmount * (ISNULL(SO.GSTPercent, 0) / 100.0) AS GSTAmount
                ) Y
                CROSS APPLY (
                    SELECT Y.SubTotalAmount, Y.DiscountAmount, Y.TaxableAmount, Y.GSTAmount, Y.TaxableAmount + Y.GSTAmount AS UnroundedInvoiceTotal
                ) Z
                CROSS APPLY (
                    SELECT Z.SubTotalAmount, Z.DiscountAmount, Z.TaxableAmount, Z.GSTAmount, Z.UnroundedInvoiceTotal,
                           ROUND(Z.UnroundedInvoiceTotal, 0) - Z.UnroundedInvoiceTotal AS RoundOff,
                           ROUND(Z.UnroundedInvoiceTotal, 0) AS InvoiceAmount
                ) CALC
                " + searchQuery + @"
                ORDER BY TRY_CAST(SO.OrderNo AS INT) DESC, SO.OrderDate DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        if (!string.IsNullOrEmpty(SearchRequirementNo))
                        {
                            cmd.Parameters.AddWithValue("@Search", SearchRequirementNo);
                        }

                        List<SaleOrderVM> list = new List<SaleOrderVM>();

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                list.Add(new SaleOrderVM
                                {
                                    SaleOrderID = Convert.ToInt32(rdr["SaleOrderID"]),
                                    OrderNo = rdr["OrderNo"]?.ToString() ?? "",
                                    OrderDate = rdr["OrderDate"]?.ToString() ?? "",
                                    PartyName = rdr["PartyName"]?.ToString() ?? "",
                                    BrokerName = rdr["BrokerName"]?.ToString() ?? "",
                                    TransportName = rdr["TransportName"]?.ToString() ?? "",
                                    TotalQty = SafeDecimal(rdr["TotalQty"]),
                                    DiscountPercent = SafeDecimal(rdr["DiscountPercent"]),
                                    DiscountAmount = SafeDecimal(rdr["DiscountAmount"]),
                                    GSTPercent = SafeDecimal(rdr["GSTPercent"]),
                                    GSTAmount = SafeDecimal(rdr["GSTAmount"]),
                                    RoundOff = SafeDecimal(rdr["RoundOff"]),
                                    InvoiceAmount = SafeDecimal(rdr["FinalInvoiceAmount"])
                                });
                            }
                        }

                        string countQuery = "SELECT COUNT(*) FROM SaleOrder SO LEFT JOIN PartyMaster PM ON SO.PartyId = PM.PartyId LEFT JOIN BrokerMaster BM ON SO.BrokerID = BM.BrokerCode LEFT JOIN TransportMaster TMST ON SO.TransportID = TMST.TransportCode " + searchQuery;
                        int totalRecords;
                        using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                        {
                            if (!string.IsNullOrEmpty(SearchRequirementNo))
                            {
                                countCmd.Parameters.AddWithValue("@Search", SearchRequirementNo);
                            }
                            totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                        }

                        var response = new
                        {
                            Code = 200,
                            Message = "Success",
                            Data = list
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
                    Message = ex.Message,
                    Data = (object)null
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetDesignEntryDataAPI(int DesignColorMatchingFormID = 0)
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
                WHERE (@DesignColorMatchingFormID = 0 OR DCMFM.DesignColorMatchingFormID = @DesignColorMatchingFormID) 
                ORDER BY DEFM.DesignEntryFormID DESC";

                    cmd.Parameters.AddWithValue("@DesignColorMatchingFormID", DesignColorMatchingFormID);
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
                    Message = "Success",
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
                    Message = ex.Message,
                    Data = (object)null
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
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
                    Message = "Success",
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
                    Message = ex.Message,
                    Data = (object)null
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetDefaultDataLoadAPI()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                List<object> partyList = new List<object>();
                List<object> brokerList = new List<object>();
                List<object> transportList = new List<object>();
                List<object> additionalRemark = new List<object>();
                List<object> additionalWorkList = new List<object>();
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
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(TRY_CAST(OrderNo AS INT)), 0) + 1 FROM SaleOrder", con))
                    {
                        nextOrderNo = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // ✅ Additional Remarks from SaleOrderAdditionalCharges (or SaleOrder if column exists)
                    bool hasSOAC = DoesTableExist(con, "SaleOrderAdditionalCharges") && DoesColumnExist(con, null, "SaleOrderAdditionalCharges", "ChargeRemark");
                    bool hasSO_AR = DoesColumnExist(con, null, "SaleOrder", "AdditionalRemark");

                    string remarkSql = "";
                    if (hasSOAC && hasSO_AR)
                    {
                        remarkSql = @"
                            SELECT DISTINCT Remark AS AdditionalRemark FROM (
                                SELECT DISTINCT ChargeRemark AS Remark FROM SaleOrderAdditionalCharges WHERE ISNULL(ChargeRemark,'') <> ''
                                UNION
                                SELECT DISTINCT AdditionalRemark AS Remark FROM SaleOrder WHERE ISNULL(AdditionalRemark,'') <> ''
                            ) r ORDER BY AdditionalRemark";
                    }
                    else if (hasSOAC)
                    {
                        remarkSql = @"SELECT DISTINCT ChargeRemark AS AdditionalRemark FROM SaleOrderAdditionalCharges WHERE ISNULL(ChargeRemark,'') <> '' ORDER BY ChargeRemark";
                    }
                    else if (hasSO_AR)
                    {
                        remarkSql = @"SELECT DISTINCT AdditionalRemark FROM SaleOrder WHERE ISNULL(AdditionalRemark,'') <> '' ORDER BY AdditionalRemark";
                    }

                    if (!string.IsNullOrEmpty(remarkSql))
                    {
                        using (SqlCommand cmd = new SqlCommand(remarkSql, con))
                        {
                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    additionalRemark.Add(new
                                    {
                                        AdditionalRemark = dr["AdditionalRemark"].ToString()
                                    });
                                }
                            }
                        }
                    }

                    // ✅ Additional Work List from ProcessMaster
                    if (DoesTableExist(con, "ProcessMaster"))
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT ProcessID, ProcessName, ISNULL(ProcessCode, '') AS ProcessCode FROM ProcessMaster WHERE IsActive = 1 ORDER BY ProcessID ASC", con))
                        {
                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    additionalWorkList.Add(new
                                    {
                                        ProcessID = dr["ProcessID"],
                                        ProcessName = dr["ProcessName"].ToString(),
                                        ProcessCode = dr["ProcessCode"].ToString()
                                    });
                                }
                            }
                        }
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
                    Message = "Success",
                    Data = new
                    {
                        PartyList = partyList,
                        BrokerList = brokerList,
                        TransportList = transportList,
                        AdditionalRemark = additionalRemark,
                        AdditionalWorkList = additionalWorkList,
                        GstData = gstdata,
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
                    Message = ex.Message,
                    Data = (object)null
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
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
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(TRY_CAST(OrderNo AS INT)), 0) + 1 FROM SaleOrder", con))
                    {
                        nextOrderNo = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }

                var response = new
                {
                    Code = 200,
                    Message = "Success",
                    Data = new
                    {
                        PartyList = partyList,
                        BrokerList = brokerList,
                        TransportList = transportList,
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
                    Message = ex.Message,
                    Data = (object)null
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetPartyListAPI(string search = "")
        {
            try
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                string connStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                List<object> partyList = new List<object>();

                using (SqlConnection con = new SqlConnection(connStr))
                {
                    string query = @"SELECT PM.PartyId, PM.PartyName, PM.GSTIN, PM.BillingAddress, ISNULL(PM.State, '') AS State, ISNULL(PM.MobileNo, '') AS MobileNo 
                                     FROM PartyMaster PM 
                                     WHERE (@search = '' OR PM.PartyName LIKE '%' + @search + '%' OR PM.MobileNo LIKE '%' + @search + '%')
                                     ORDER BY PM.PartyName ASC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@search", search ?? "");
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                partyList.Add(new
                                {
                                    PartyId = dr["PartyId"],
                                    PartyName = dr["PartyName"].ToString(),
                                    GSTIN = dr["GSTIN"] != DBNull.Value ? dr["GSTIN"].ToString() : "",
                                    BillingAddress = dr["BillingAddress"] != DBNull.Value ? dr["BillingAddress"].ToString() : "",
                                    State = dr["State"]?.ToString() ?? "",
                                    MobileNo = dr["MobileNo"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }

                var response = new
                {
                    Code = 200,
                    Message = "Success",
                    Data = partyList
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
                    Message = ex.Message,
                    Data = (object)null
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveSaleOrderAPI(SaleOrderModel model)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;

            if (model == null)
            {
                var err = new
                {
                    Code = 400,
                    Message = "Invalid data",
                    Data = 0
                };
                Context.Response.Write(js.Serialize(err));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return;
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

                                bool hasSO_DiscountType = DoesColumnExist(con, trans, "SaleOrder", "DiscountType");
                                bool hasSO_AdditionalRemark = DoesColumnExist(con, trans, "SaleOrder", "AdditionalRemark");
                                bool hasSO_AdditionalValue = DoesColumnExist(con, trans, "SaleOrder", "AdditionalValue");
                                bool hasSO_RoundOff = DoesColumnExist(con, trans, "SaleOrder", "RoundOff");

                                // INSERT
                                if (model.SaleOrderID == 0)
                                {
                                    model.OrderNo = GetNextOrderNo(con, trans);

                                    string extraCols = "";
                                    string extraVals = "";
                                    if (hasSO_DiscountType) { extraCols += ", DiscountType"; extraVals += ", @DiscountType"; }
                                    if (hasSO_AdditionalRemark) { extraCols += ", AdditionalRemark"; extraVals += ", @AdditionalRemark"; }
                                    if (hasSO_AdditionalValue) { extraCols += ", AdditionalValue"; extraVals += ", @AdditionalValue"; }
                                    if (hasSO_RoundOff) { extraCols += ", RoundOff"; extraVals += ", @RoundOff"; }

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
                                CompanyId" + extraCols + @"
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
                                @CompanyId" + extraVals + @");

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

                                    string extraUpdate = "";
                                    if (hasSO_DiscountType) extraUpdate += ", DiscountType=@DiscountType";
                                    if (hasSO_AdditionalRemark) extraUpdate += ", AdditionalRemark=@AdditionalRemark";
                                    if (hasSO_AdditionalValue) extraUpdate += ", AdditionalValue=@AdditionalValue";
                                    if (hasSO_RoundOff) extraUpdate += ", RoundOff=@RoundOff";

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
                                Remark=@Remark" + extraUpdate + @"
                            WHERE SaleOrderID=@SaleOrderID";

                                    AddParams(cmd, model);

                                    cmd.Parameters.Add("@SaleOrderID", SqlDbType.Int)
                                        .Value = saleOrderId;

                                    cmd.ExecuteNonQuery();

                                    // Delete Old Additional Charges
                                    if (DoesTableExist(con, trans, "SaleOrderAdditionalCharges"))
                                    {
                                        using (SqlCommand delAdd = new SqlCommand("DELETE FROM SaleOrderAdditionalCharges WHERE SaleOrderID=@SaleOrderID", con, trans))
                                        {
                                            delAdd.Parameters.AddWithValue("@SaleOrderID", saleOrderId);
                                            delAdd.ExecuteNonQuery();
                                        }
                                    }

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

                                    if (DoesTableExist(con, trans, "SaleOrderAdditionalCharges"))
                                    {
                                        using (SqlCommand delAdd = new SqlCommand(@"
                                DELETE FROM SaleOrderAdditionalCharges
                                WHERE SaleOrderID=@SaleOrderID", con, trans))
                                        {
                                            delAdd.Parameters.AddWithValue("@SaleOrderID", saleOrderId);
                                            delAdd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            // ADDITIONAL CHARGES
                            if (model.AdditionalCharges != null && model.AdditionalCharges.Count > 0 && DoesTableExist(con, trans, "SaleOrderAdditionalCharges"))
                            {
                                foreach (var ac in model.AdditionalCharges)
                                {
                                    using (SqlCommand cmdAdd = new SqlCommand(@"
                                INSERT INTO SaleOrderAdditionalCharges 
                                (SaleOrderID, ChargeRemark, ChargeValue, TotalCalculatedAmount, CreatedDateTime, CreatedByUserID)
                                VALUES 
                                (@SaleOrderID, @ChargeRemark, @ChargeValue, @TotalCalculatedAmount, GETDATE(), @CreatedByUserID)", con, trans))
                                    {
                                        cmdAdd.Parameters.AddWithValue("@SaleOrderID", saleOrderId);
                                        cmdAdd.Parameters.AddWithValue("@ChargeRemark", (object)ac.ChargeRemark ?? "");
                                        cmdAdd.Parameters.AddWithValue("@ChargeValue", ac.ChargeValue);
                                        cmdAdd.Parameters.AddWithValue("@TotalCalculatedAmount", ac.TotalCalculatedAmount);
                                        cmdAdd.Parameters.AddWithValue("@CreatedByUserID", model.UserAccountId);
                                        cmdAdd.ExecuteNonQuery();
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

                            var response = new
                            {
                                Code = 200,
                                Message = "Success",
                                Data = saleOrderId
                            };
                            Context.Response.Write(js.Serialize(response));
                            Context.Response.Flush();
                            Context.Response.SuppressContent = true;
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return;
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();

                            var response = new
                            {
                                Code = 400,
                                Message = ex.Message,
                                Data = 0
                            };
                            Context.Response.Write(js.Serialize(response));
                            Context.Response.Flush();
                            Context.Response.SuppressContent = true;
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var response = new
                {
                    Code = 400,
                    Message = ex.Message,
                    Data = 0
                };
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetSaleOrderByIdAPI(int SaleOrderID)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;
            try
            {
                string conStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conStr))
                {
                    con.Open();

                    bool hasSO_RoundOff = DoesColumnExist(con, null, "SaleOrder", "RoundOff");
                    bool hasSO_DiscountType = DoesColumnExist(con, null, "SaleOrder", "DiscountType");
                    bool hasSO_AdditionalRemark = DoesColumnExist(con, null, "SaleOrder", "AdditionalRemark");
                    bool hasSO_AdditionalValue = DoesColumnExist(con, null, "SaleOrder", "AdditionalValue");

                    string roundOffExpr = hasSO_RoundOff ? "SO.RoundOff" : "CAST(0 AS DECIMAL(18,2)) AS RoundOff";
                    string discountTypeExpr = hasSO_DiscountType ? "SO.DiscountType" : "'' AS DiscountType";
                    string additionalRemarkExpr = hasSO_AdditionalRemark ? "SO.AdditionalRemark" : "'' AS AdditionalRemark";
                    string additionalValueExpr = hasSO_AdditionalValue ? "SO.AdditionalValue" : "CAST(0 AS DECIMAL(18,2)) AS AdditionalValue";

                    string query = string.Format(@"
                    -- 1st Query: Get Master, Details, and SubDetails
                    SELECT SO.SaleOrderID, SO.OrderNo, CONVERT(VARCHAR(10), SO.OrderDate, 23) AS OrderDate, SO.PartyId, PM.PartyName, SO.BrokerID, SO.TransportID,
                           ISNULL(BM.BrokerName, SO.BrokerID) AS BrokerName,
                           ISNULL(TMST.TransportName, SO.TransportID) AS TransportName,
                           SO.MarkupPercent, SO.TotalQty, SO.TotalAmount, SO.DiscountPercent, SO.DiscountAmount, SO.GSTPercent, SO.GSTAmount, SO.InvoiceAmount,
                           SO.Remark, SO.UserAccountId, SO.FinancialYearID, SO.CompanyId, 
                           {0}, {1}, {2}, {3},      
                           D.DetailID, D.SaleOrderID AS DetailSaleOrderID, D.DesignID, D.DesignNo AS SavedDesignNo, D.ItemType, TM.Type, D.NoOfColours, 
                           D.Qty AS DetailQty, D.Unit, D.Rate, D.Amount, D.BrokerRate, 
                           SD.SubDetailID, SD.DetailID AS SubDetailDetailID, SD.ColourID, SD.ColourName, SD.Qty AS SubQty, SD.Unit AS SubUnit, SD.Remark as SubRemark, 
                           ISNULL(DCMD.DesignColorMatchingDetailsID, 0) AS ColorDetailsID,
                           ISNULL(DCMD.ColorGroupID, 0) AS ColorGroupID,
                           ISNULL(CGM.ColorGroup, '') AS ColorGroup,
                           ISNULL(DCMD.ColorMatchingPhoto, '') AS ColorMatchingPhoto,
                           DEFM.DesignEntryFormID, DCMFM.DesignColorMatchingFormID, DEFM.DesignerCode, 
                           ISNULL(UM.UnitCode, '') AS UnitCode, ISNULL(UM.UnitId, 0) AS UnitId,
                           ISNULL(DEFM.SaleRate, 0) AS SaleRate 
                    FROM SaleOrder SO
                    LEFT JOIN PartyMaster PM ON SO.PartyId = PM.PartyId
                    LEFT JOIN BrokerMaster BM ON SO.BrokerID = BM.BrokerCode
                    LEFT JOIN TransportMaster TMST ON SO.TransportID = TMST.TransportCode
                    LEFT JOIN SaleOrderDetails D ON SO.SaleOrderID = D.SaleOrderID
                    LEFT JOIN TypeMaster TM ON TM.TypeID = D.ItemType
                    LEFT JOIN SaleOrderSubDetails SD ON D.DetailID = SD.DetailID
                    LEFT JOIN UnitMaster UM ON UM.UnitId = TM.UnitId
                    LEFT JOIN DesignColorMatchingDetails DCMD ON SD.ColourID = DCMD.DesignColorMatchingDetailsID
                    LEFT JOIN DesignColorMatchingFormMaster DCMFM ON DCMFM.DesignColorMatchingFormID = DCMD.DesignColorMatchingFormID
                    LEFT JOIN DesignEntryFormMaster DEFM ON DEFM.DesignEntryFormID = DCMFM.DesignEntryFormID
                    LEFT JOIN ColorGroupMaster CGM ON CGM.ColorGroupID = DCMD.ColorGroupID
                    WHERE SO.SaleOrderID = @ID 
                    ORDER BY D.DetailID, SD.SubDetailID;

                    -- 2nd Query: Get Additional Charges
                    SELECT AdditionalChargeID, SaleOrderID, ChargeRemark, ChargeValue, TotalCalculatedAmount 
                    FROM SaleOrderAdditionalCharges 
                    WHERE SaleOrderID = @ID", roundOffExpr, discountTypeExpr, additionalRemarkExpr, additionalValueExpr);

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@ID", SqlDbType.Int).Value = SaleOrderID;

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            SaleOrderVM order = null;

                            while (rdr.Read())
                            {
                                if (order == null)
                                {
                                    order = new SaleOrderVM
                                    {
                                        SaleOrderID = SafeInt(rdr["SaleOrderID"]),
                                        OrderNo = rdr["OrderNo"]?.ToString(),
                                        OrderDate = rdr["OrderDate"]?.ToString(),
                                        PartyID = SafeInt(rdr["PartyId"]),
                                        PartyName = rdr["PartyName"]?.ToString(),
                                        BrokerID = rdr["BrokerID"]?.ToString(),
                                        BrokerName = rdr["BrokerName"]?.ToString(),
                                        TransportID = rdr["TransportID"]?.ToString(),
                                        TransportName = rdr["TransportName"]?.ToString(),
                                        MarkupPercent = SafeDecimal(rdr["MarkupPercent"]),
                                        TotalQty = SafeDecimal(rdr["TotalQty"]),
                                        TotalAmount = SafeDecimal(rdr["TotalAmount"]),
                                        DiscountPercent = SafeDecimal(rdr["DiscountPercent"]),
                                        DiscountAmount = SafeDecimal(rdr["DiscountAmount"]),
                                        GSTPercent = SafeDecimal(rdr["GSTPercent"]),
                                        GSTAmount = SafeDecimal(rdr["GSTAmount"]),
                                        InvoiceAmount = SafeDecimal(rdr["InvoiceAmount"]),
                                        Remark = rdr["Remark"]?.ToString(),
                                        AdditionalValue = SafeDecimal(rdr["AdditionalValue"]),
                                        AdditionalRemark = rdr["AdditionalRemark"]?.ToString(),
                                        DiscountType = rdr["DiscountType"]?.ToString(),
                                        RoundOff = SafeDecimal(rdr["RoundOff"]),
                                        Details = new List<SaleOrderDetailVM>(),
                                        AdditionalCharges = new List<SaleOrderAdditionalChargeModel>()
                                    };
                                }

                                if (rdr["DetailID"] != DBNull.Value)
                                {
                                    int detailId = SafeInt(rdr["DetailID"]);
                                    var detail = order.Details.FirstOrDefault(x => x.DetailID == detailId);

                                    if (detail == null)
                                    {
                                        detail = new SaleOrderDetailVM
                                        {
                                            DetailID = detailId,
                                            colorId = SafeInt(rdr["ColourID"]),
                                            ColorGroupId = SafeInt(rdr["ColorGroupId"]),
                                            ColorGroup = rdr["ColorGroup"]?.ToString(),
                                            colorName = rdr["ColourName"]?.ToString(),
                                            ItemType = SafeInt(rdr["ItemType"]),
                                            Type = rdr["Type"]?.ToString(),
                                            NoOfColours = SafeInt(rdr["NoOfColours"]),
                                            Qty = SafeDecimal(rdr["DetailQty"]),
                                            Unit = rdr["Unit"]?.ToString(),
                                            TotalQty = SafeDecimal(rdr["DetailQty"]),
                                            Rate = SafeDecimal(rdr["Rate"]),
                                            Amount = SafeDecimal(rdr["Amount"]),
                                            BrokerRate = SafeDecimal(rdr["BrokerRate"]),
                                            SaleRate = SafeDecimal(rdr["SaleRate"]),
                                            DesignId = rdr["DesignEntryFormID"]?.ToString() + "|" + rdr["DesignColorMatchingFormID"]?.ToString(),
                                            DesignNo = rdr["DesignEntryFormID"]?.ToString() + "|" + rdr["DesignColorMatchingFormID"]?.ToString(),
                                            DesignName = rdr["SavedDesignNo"]?.ToString(),
                                            ColorDetailsID = SafeInt(rdr["ColorDetailsID"]),
                                            UnitId = SafeInt(rdr["UnitId"]),
                                            TypeID = SafeInt(rdr["ItemType"]),
                                            MarkupPercent = SafeDecimal(rdr["MarkupPercent"]),
                                            ColourID = SafeInt(rdr["ColourID"]),
                                            ColourName = rdr["ColourName"]?.ToString(),
                                            ImageUrl = rdr["ColorMatchingPhoto"]?.ToString(),
                                            SubDetails = new List<SaleOrderSubVM>()
                                        };
                                        order.Details.Add(detail);
                                    }

                                    if (rdr["SubDetailID"] != DBNull.Value)
                                    {
                                        int subId = SafeInt(rdr["SubDetailID"]);

                                        if (!detail.SubDetails.Any(x => x.SubDetailID == subId))
                                        {
                                            detail.SubDetails.Add(new SaleOrderSubVM
                                            {
                                                SubDetailID = subId,
                                                ColourID = SafeInt(rdr["ColourID"]),
                                                ColourName = rdr["ColourName"]?.ToString(),
                                                Qty = SafeDecimal(rdr["SubQty"]),
                                                Unit = rdr["SubUnit"]?.ToString(),
                                                SaleRate = SafeDecimal(rdr["SaleRate"]),
                                                DesignNo = rdr["DesignEntryFormID"]?.ToString() + "|" + rdr["DesignColorMatchingFormID"]?.ToString(),
                                                DesignName = rdr["SavedDesignNo"]?.ToString(),
                                                ColorGroupId = SafeInt(rdr["ColorGroupID"]),
                                                ColorGroup = rdr["ColorGroup"]?.ToString(),
                                                Remark = rdr["SubRemark"]?.ToString(),
                                                ColorDetailsID = SafeInt(rdr["ColorDetailsID"]),
                                                UnitId = SafeInt(rdr["UnitId"]),
                                                TypeID = SafeInt(rdr["ItemType"]),
                                                MarkupPercent = SafeDecimal(rdr["MarkupPercent"]),
                                            });
                                        }
                                    }
                                }
                            }

                            if (order != null && rdr.NextResult())
                            {
                                while (rdr.Read())
                                {
                                    order.AdditionalCharges.Add(new SaleOrderAdditionalChargeModel
                                    {
                                        AdditionalChargeID = SafeInt(rdr["AdditionalChargeID"]),
                                        ChargeRemark = rdr["ChargeRemark"]?.ToString(),
                                        ChargeValue = SafeDecimal(rdr["ChargeValue"]),
                                        TotalCalculatedAmount = SafeDecimal(rdr["TotalCalculatedAmount"])
                                    });
                                }
                            }

                            if (order != null)
                            {
                                var response = new ApiResponse<SaleOrderVM>
                                {
                                    Code = 200,
                                    success = true,
                                    Message = "Success",
                                    Data = order
                                };
                                Context.Response.Write(js.Serialize(response));
                                Context.Response.Flush();
                                Context.Response.SuppressContent = true;
                                HttpContext.Current.ApplicationInstance.CompleteRequest();
                                return;
                            }
                            else
                            {
                                var response = new ApiResponse<SaleOrderVM>
                                {
                                    Code = 400,
                                    success = false,
                                    Data = null,
                                    Message = "No Data Found"
                                };
                                Context.Response.Write(js.Serialize(response));
                                Context.Response.Flush();
                                Context.Response.SuppressContent = true;
                                HttpContext.Current.ApplicationInstance.CompleteRequest();
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errResp = new ApiResponse<SaleOrderVM>
                {
                    Code = 400,
                    success = false,
                    Message = ex.Message
                };
                Context.Response.Write(js.Serialize(errResp));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return;
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public ApiResponse<bool> DeleteSaleOrderAPI(int SaleOrderID)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;

            try
            {
                string conStr = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conStr))
                {
                    con.Open();

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            // PRE-FLIGHT DEPENDENCY CHECK
                            int depCount = 0;
                            using (SqlCommand cmdCheck = new SqlCommand(@"
                                SELECT COUNT(1) FROM dbo.ProductionOrderDetail 
                                WHERE (SalesOrderID = @ID OR SaleOrderID = @ID) AND ISNULL(IsActive, 1) = 1", con, trans))
                            {
                                cmdCheck.Parameters.Add("@ID", SqlDbType.Int).Value = SaleOrderID;
                                try
                                {
                                    depCount = Convert.ToInt32(cmdCheck.ExecuteScalar());
                                }
                                catch { }
                            }

                            if (depCount > 0)
                            {
                                trans.Rollback();
                                var blockResp = new ApiResponse<bool>
                                {
                                    Code = 400,
                                    success = false,
                                    Message = "This record cannot be deleted because it is already associated with existing transactions or related records.",
                                    Data = false
                                };
                                Context.Response.Write(js.Serialize(blockResp));
                                Context.Response.Flush();
                                Context.Response.SuppressContent = true;
                                HttpContext.Current.ApplicationInstance.CompleteRequest();
                                return new ApiResponse<bool>();
                            }

                            // 1. DELETE ADDITIONAL CHARGES
                            using (SqlCommand cmdAdd = new SqlCommand("DELETE FROM SaleOrderAdditionalCharges WHERE SaleOrderID = @ID", con, trans))
                            {
                                cmdAdd.Parameters.AddWithValue("@ID", SaleOrderID);
                                cmdAdd.ExecuteNonQuery();
                            }

                            // 2. DELETE SUB DETAILS
                            using (SqlCommand cmdSub = new SqlCommand(@"
                                DELETE SD 
                                FROM SaleOrderSubDetails SD
                                INNER JOIN SaleOrderDetails D ON SD.DetailID = D.DetailID
                                WHERE D.SaleOrderID = @ID
                            ", con, trans))
                            {
                                cmdSub.Parameters.AddWithValue("@ID", SaleOrderID);
                                cmdSub.ExecuteNonQuery();
                            }

                            // 3. DELETE DETAILS
                            using (SqlCommand cmdDetails = new SqlCommand("DELETE FROM SaleOrderDetails WHERE SaleOrderID = @ID", con, trans))
                            {
                                cmdDetails.Parameters.AddWithValue("@ID", SaleOrderID);
                                cmdDetails.ExecuteNonQuery();
                            }

                            // 4. DELETE MASTER
                            using (SqlCommand cmdMaster = new SqlCommand("DELETE FROM SaleOrder WHERE SaleOrderID = @ID", con, trans))
                            {
                                cmdMaster.Parameters.AddWithValue("@ID", SaleOrderID);
                                cmdMaster.ExecuteNonQuery();
                            }

                            trans.Commit();

                            var response = new ApiResponse<bool>
                            {
                                Code = 200,
                                success = true,
                                Message = "Deleted Successfully",
                                Data = true
                            };
                            Context.Response.Write(js.Serialize(response));
                            Context.Response.Flush();
                            Context.Response.SuppressContent = true;
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return new ApiResponse<bool>();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            var errResp = new ApiResponse<bool>
                            {
                                Code = 400,
                                success = false,
                                Message = ex.Message,
                                Data = false
                            };
                            Context.Response.Write(js.Serialize(errResp));
                            Context.Response.Flush();
                            Context.Response.SuppressContent = true;
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            return new ApiResponse<bool>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errResp = new ApiResponse<bool>
                {
                    Code = 400,
                    success = false,
                    Message = ex.Message,
                    Data = false
                };
                Context.Response.Write(js.Serialize(errResp));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<bool>();
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public ApiResponse<List<ColorMatchingDetailModel>> GetColorMatchingDetailsAPI(int designColorMatchingFormID, int colorGroupID)
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;

            try
            {
                List<ColorMatchingDetailModel> list = new List<ColorMatchingDetailModel>();
                string cs = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(cs))
                {
                    using (SqlCommand cmd = new SqlCommand(@"SELECT DesignColorMatchingFormID, DesignColorMatchingDetailsID, WarpMatchingID, FeederMatchingID, ColorGroupID 
                                                            FROM DesignColorMatchingDetails
                                                            WHERE DesignColorMatchingFormID = @FormID AND (@ColorGroupID = -1 OR ColorGroupID = @ColorGroupID OR ColorGroupID = -1)", con))
                    {
                        cmd.Parameters.AddWithValue("@FormID", designColorMatchingFormID);
                        cmd.Parameters.AddWithValue("@ColorGroupID", colorGroupID);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                list.Add(new ColorMatchingDetailModel
                                {
                                    DesignColorMatchingFormID = Convert.ToInt32(rdr["DesignColorMatchingFormID"]),
                                    DesignColorMatchingDetailsID = Convert.ToInt32(rdr["DesignColorMatchingDetailsID"]),
                                    ColorGroupID = Convert.ToInt32(rdr["ColorGroupID"]),
                                    WarpMatchingID = rdr["WarpMatchingID"]?.ToString(),
                                    FeederMatchingID = rdr["FeederMatchingID"]?.ToString(),
                                });
                            }
                        }
                    }
                }

                var response = new ApiResponse<List<ColorMatchingDetailModel>>
                {
                    Code = 200,
                    success = true,
                    Message = "Success",
                    Data = list
                };
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<List<ColorMatchingDetailModel>>();
            }
            catch (Exception ex)
            {
                var errResp = new ApiResponse<List<ColorMatchingDetailModel>>
                {
                    Code = 400,
                    success = false,
                    Message = ex.Message,
                    Data = null
                };
                Context.Response.Write(js.Serialize(errResp));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<List<ColorMatchingDetailModel>>();
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public ApiResponse<List<string>> GetAdditionalRemarksAPI()
        {
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = Int32.MaxValue;

            try
            {
                List<string> remarks = new List<string>();
                string conString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conString))
                {
                    using (SqlCommand cmd = new SqlCommand(@"SELECT DISTINCT ChargeRemark FROM SaleOrderAdditionalCharges WHERE ISNULL(ChargeRemark,'') <> '' ORDER BY ChargeRemark", con))
                    {
                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                remarks.Add(rdr["ChargeRemark"].ToString());
                            }
                        }
                    }
                }

                var response = new ApiResponse<List<string>>
                {
                    Code = 200,
                    success = true,
                    Message = "Success",
                    Data = remarks
                };
                Context.Response.Write(js.Serialize(response));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<List<string>>();
            }
            catch (Exception ex)
            {
                var errResp = new ApiResponse<List<string>>
                {
                    Code = 400,
                    success = false,
                    Message = ex.Message,
                    Data = null
                };
                Context.Response.Write(js.Serialize(errResp));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                return new ApiResponse<List<string>>();
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

        #region PROCESS MANAGEMENT HELPER METHODS
        private static bool DoesColumnExist(SqlConnection con, SqlTransaction tran, string tableName, string columnName)
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName;";
                using (SqlCommand cmd = new SqlCommand(sql, con, tran))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@ColumnName", columnName);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool DoesTableExist(SqlConnection con, string tableName)
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName;";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool DoesTableExist(SqlConnection con, SqlTransaction tran, string tableName)
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName;";
                using (SqlCommand cmd = new SqlCommand(sql, con, tran))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private int GetButtaCuttingProcessID(SqlConnection con)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ProcessID FROM ProcessMaster WHERE ProcessName LIKE '%BUTTA%' OR ProcessCode = 'BUTTA' OR ProcessCode LIKE '%BUTTA%' ORDER BY ProcessID ASC", con))
                {
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt32(val);
                }
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ProcessID FROM ProcessMaster WHERE IsActive = 1 ORDER BY ProcessID ASC", con))
                {
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt32(val);
                }
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ProcessID FROM ProcessMaster ORDER BY ProcessID ASC", con))
                {
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt32(val);
                }
            }
            catch { }
            return 1;
        }

        private UserSecurityContext GetLoggedInUserSecurityContext(SqlConnection con)
        {
            var context = new UserSecurityContext
            {
                UserAccountId = 0,
                UserRole = "",
                IsAdmin = false,
                IsJobber = false,
                LoggedInJobberID = 0,
                JobberName = "",
                CompanyId = 0
            };

            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.Cookies["CompanyID"] != null)
                {
                    string cVal = HttpContext.Current.Request.Cookies["CompanyID"].Value ?? "";
                    if (cVal.Contains("="))
                    {
                        var parts = cVal.Split('=');
                        cVal = parts[parts.Length - 1];
                    }
                    long compId = 0;
                    if (long.TryParse(cVal, out compId)) context.CompanyId = compId;
                }

                long userAccId = 0;
                if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.Cookies["UserIDs"] != null)
                {
                    string cookieVal = HttpContext.Current.Request.Cookies["UserIDs"].Value ?? "";
                    if (cookieVal.Contains("="))
                    {
                        var parts = cookieVal.Split('=');
                        cookieVal = parts[parts.Length - 1];
                    }
                    long.TryParse(cookieVal, out userAccId);
                }

                if (userAccId > 0)
                {
                    context.UserAccountId = userAccId;

                    string sql = @"
                SELECT UA.UserAccountId, ISNULL(UA.UserRole, '') AS UserRole, UA.PartyId, JM.PartyName AS JobberName, ISNULL(UA.CompanyId, 0) AS UserCompanyId
                FROM UserAccountMaster UA
                LEFT JOIN JobberMaster JM ON UA.PartyId = JM.Id
                WHERE UA.UserAccountId = @UserAccountId AND ISNULL(UA.AllowLogin, 1) = 1";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@UserAccountId", userAccId);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                context.UserRole = dr["UserRole"] != DBNull.Value ? dr["UserRole"].ToString() : "";
                                if (context.CompanyId <= 0 && dr["UserCompanyId"] != DBNull.Value)
                                {
                                    context.CompanyId = Convert.ToInt64(dr["UserCompanyId"]);
                                }
                                if (context.UserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                                {
                                    context.IsAdmin = true;
                                    context.LoggedInJobberID = 0;
                                }
                                else
                                {
                                    context.IsJobber = true;
                                    if (dr["PartyId"] != DBNull.Value)
                                    {
                                        context.LoggedInJobberID = Convert.ToInt64(dr["PartyId"]);
                                    }
                                    if (dr["JobberName"] != DBNull.Value)
                                    {
                                        context.JobberName = dr["JobberName"].ToString();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return context;
        }

        private void SetCrystalDatabaseLogin(ReportDocument report, string connectionString)
        {
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(connectionString);

            ConnectionInfo connectionInfo = new ConnectionInfo
            {
                ServerName = csb.DataSource,
                DatabaseName = csb.InitialCatalog
            };

            if (csb.IntegratedSecurity)
            {
                connectionInfo.IntegratedSecurity = true;
            }
            else
            {
                connectionInfo.IntegratedSecurity = false;
                connectionInfo.UserID = csb.UserID;
                connectionInfo.Password = csb.Password;
            }

            try
            {
                if (csb.IntegratedSecurity)
                {
                    report.SetDatabaseLogon("", "", csb.DataSource, csb.InitialCatalog);
                }
                else
                {
                    report.SetDatabaseLogon(csb.UserID, csb.Password, csb.DataSource, csb.InitialCatalog);
                }

                foreach (CrystalDecisions.CrystalReports.Engine.Table table in report.Database.Tables)
                {
                    TableLogOnInfo logOnInfo = table.LogOnInfo;
                    logOnInfo.ConnectionInfo = connectionInfo;
                    table.ApplyLogOnInfo(logOnInfo);
                }
            }
            catch(Exception ex)
            {
               ex.ToString();
            }
        }
            #endregion

        private void WriteJsonResponse(object data)
        {
            try
            {
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = Int32.MaxValue;
                Context.Response.Write(js.Serialize(data));
                Context.Response.Flush();
                Context.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch { }
        }

        #region GENERIC PROCESS MANAGEMENT (JOBBER -> PROCESS -> JOBBER)
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetGenericProcessFilterDropdowns()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                GenericProcessDropdownResponse response = new GenericProcessDropdownResponse();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);

                    // 1. Processes from ProcessMaster
                    using (SqlCommand cmd = new SqlCommand("SELECT ProcessID, ProcessName, ProcessCode FROM ProcessMaster WHERE IsActive = 1 ORDER BY ProcessID ASC", con))
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                response.Processes.Add(new ProcessDropdownItem
                                {
                                    ProcessID = Convert.ToInt32(dr["ProcessID"]),
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessCode = dr["ProcessCode"].ToString()
                                });
                            }
                        }
                    }

                    // 2. Jobbers from JobberMaster / PartyMaster
                    string jobberSql = "SELECT Id AS JobberID, PartyName AS JobberName FROM JobberMaster";
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberSql += " WHERE Id = @JID";
                    }
                    jobberSql += " ORDER BY PartyName";

                    using (SqlCommand cmd = new SqlCommand(jobberSql, con))
                    {
                        if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                        {
                            cmd.Parameters.AddWithValue("@JID", secContext.LoggedInJobberID);
                        }
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                long jId = Convert.ToInt64(dr["JobberID"]);
                                string jName = dr["JobberName"].ToString();
                                response.Jobbers.Add(new JobberDropdownItem { JobberID = jId, JobberName = jName, ProcessorID = jId, ProcessorName = jName });
                            }
                        }
                    }

                    // 2b. Processors (All processors allowed so jobber can issue to any processor)
                    using (SqlCommand cmd = new SqlCommand("SELECT Id AS JobberID, PartyName AS JobberName FROM JobberMaster ORDER BY PartyName", con))
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                long jId = Convert.ToInt64(dr["JobberID"]);
                                string jName = dr["JobberName"].ToString();
                                response.Processors.Add(new JobberDropdownItem { ProcessorID = jId, ProcessorName = jName, JobberID = jId, JobberName = jName });
                            }
                        }
                    }

                    // 3. Designs from ProductionOrderDetail
                    using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT ISNULL(DesignNo, '') AS DesignNo FROM ProductionOrderDetail WHERE DesignNo IS NOT NULL AND DesignNo <> '' ORDER BY DesignNo", con))
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                response.Designs.Add(new DesignDropdownItem { DesignNo = dr["DesignNo"].ToString() });
                            }
                        }
                    }

                    // 4. Slips from ProductionOrderDetail (and ProductionOrderSubDetail if exists)
                    bool hasPOSD_Slip = DoesColumnExist(con, null, "ProductionOrderSubDetail", "SlipNo");
                    bool hasPOD_Slip = DoesColumnExist(con, null, "ProductionOrderDetail", "SlipNo");

                    string slipQuery = "";
                    if (hasPOSD_Slip && hasPOD_Slip)
                    {
                        slipQuery = @"
                            SELECT DISTINCT SlipNo FROM (
                                SELECT ISNULL(SlipNo, '') AS SlipNo FROM ProductionOrderDetail WHERE SlipNo IS NOT NULL AND SlipNo <> ''
                                UNION
                                SELECT ISNULL(SlipNo, '') AS SlipNo FROM ProductionOrderSubDetail WHERE SlipNo IS NOT NULL AND SlipNo <> ''
                            ) t ORDER BY SlipNo";
                    }
                    else if (hasPOD_Slip)
                    {
                        slipQuery = "SELECT DISTINCT ISNULL(SlipNo, '') AS SlipNo FROM ProductionOrderDetail WHERE SlipNo IS NOT NULL AND SlipNo <> '' ORDER BY SlipNo";
                    }
                    else if (hasPOSD_Slip)
                    {
                        slipQuery = "SELECT DISTINCT ISNULL(SlipNo, '') AS SlipNo FROM ProductionOrderSubDetail WHERE SlipNo IS NOT NULL AND SlipNo <> '' ORDER BY SlipNo";
                    }

                    if (!string.IsNullOrEmpty(slipQuery))
                    {
                        using (SqlCommand cmd = new SqlCommand(slipQuery, con))
                        {
                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    string slip = dr["SlipNo"].ToString();
                                    if (!string.IsNullOrEmpty(slip)) response.Slips.Add(new SlipDropdownItem { SlipNo = slip });
                                }
                            }
                        }
                    }

                    // 5. Machines from ProductionOrderSubDetail / ProductionOrderDetail
                    bool hasPOSD_Mach = DoesColumnExist(con, null, "ProductionOrderSubDetail", "MachineNumber");
                    bool hasPOD_Mach = DoesColumnExist(con, null, "ProductionOrderDetail", "MachineNumber");

                    string machQuery = "";
                    if (hasPOSD_Mach && hasPOD_Mach)
                    {
                        machQuery = @"
                            SELECT DISTINCT MachineNumber FROM (
                                SELECT MachineNumber FROM ProductionOrderSubDetail WHERE MachineNumber IS NOT NULL AND MachineNumber > 0
                                UNION
                                SELECT MachineNumber FROM ProductionOrderDetail WHERE MachineNumber IS NOT NULL AND MachineNumber > 0
                            ) m ORDER BY MachineNumber";
                    }
                    else if (hasPOSD_Mach)
                    {
                        machQuery = "SELECT DISTINCT MachineNumber FROM ProductionOrderSubDetail WHERE MachineNumber IS NOT NULL AND MachineNumber > 0 ORDER BY MachineNumber";
                    }
                    else if (hasPOD_Mach)
                    {
                        machQuery = "SELECT DISTINCT MachineNumber FROM ProductionOrderDetail WHERE MachineNumber IS NOT NULL AND MachineNumber > 0 ORDER BY MachineNumber";
                    }

                    if (!string.IsNullOrEmpty(machQuery))
                    {
                        using (SqlCommand cmd = new SqlCommand(machQuery, con))
                        {
                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    response.Machines.Add(new MachineDropdownItem { MachineNumber = Convert.ToInt32(dr["MachineNumber"]) });
                                }
                            }
                        }
                    }

                    // 6. Barcodes from ProductionOrderDetail (and ProductionOrderSubDetail if exists)
                    bool hasPOSD_Barcode = DoesColumnExist(con, null, "ProductionOrderSubDetail", "Barcode");
                    bool hasPOD_Barcode = DoesColumnExist(con, null, "ProductionOrderDetail", "Barcode");

                    string barcodeQuery = "";
                    if (hasPOSD_Barcode && hasPOD_Barcode)
                    {
                        barcodeQuery = @"
                            SELECT DISTINCT Barcode FROM (
                                SELECT ISNULL(Barcode, '') AS Barcode FROM ProductionOrderDetail WHERE Barcode IS NOT NULL AND Barcode <> ''
                                UNION
                                SELECT ISNULL(Barcode, '') AS Barcode FROM ProductionOrderSubDetail WHERE Barcode IS NOT NULL AND Barcode <> ''
                            ) b ORDER BY Barcode";
                    }
                    else if (hasPOD_Barcode)
                    {
                        barcodeQuery = "SELECT DISTINCT ISNULL(Barcode, '') AS Barcode FROM ProductionOrderDetail WHERE Barcode IS NOT NULL AND Barcode <> '' ORDER BY Barcode";
                    }
                    else if (hasPOSD_Barcode)
                    {
                        barcodeQuery = "SELECT DISTINCT ISNULL(Barcode, '') AS Barcode FROM ProductionOrderSubDetail WHERE Barcode IS NOT NULL AND Barcode <> '' ORDER BY Barcode";
                    }

                    if (!string.IsNullOrEmpty(barcodeQuery))
                    {
                        using (SqlCommand cmd = new SqlCommand(barcodeQuery, con))
                        {
                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    string bcode = dr["Barcode"].ToString().Trim();
                                    if (!string.IsNullOrEmpty(bcode)) response.Barcodes.Add(new BarcodeDropdownItem { Barcode = bcode });
                                }
                            }
                        }
                    }
                }

                if (response.Processes.Count == 0)
                {
                    response.Processes.Add(new ProcessDropdownItem { ProcessID = 1, ProcessName = "BUTTA CUTTING", ProcessCode = "BUTTA_CUTTING" });
                    response.Processes.Add(new ProcessDropdownItem { ProcessID = 2, ProcessName = "BORDER CUTTING", ProcessCode = "BORDER_CUTTING" });
                    response.Processes.Add(new ProcessDropdownItem { ProcessID = 3, ProcessName = "DECO", ProcessCode = "DECO" });
                    response.Processes.Add(new ProcessDropdownItem { ProcessID = 4, ProcessName = "EMBROIDERY", ProcessCode = "EMBROIDERY" });
                    response.Processes.Add(new ProcessDropdownItem { ProcessID = 5, ProcessName = "PRINTING", ProcessCode = "PRINTING" });
                }

                if (response.Jobbers.Count == 0)
                {
                    var fallbackList = new List<JobberDropdownItem>
                    {
                        new JobberDropdownItem { JobberID = 3L, JobberName = "NIKUNJ TEXTILES", ProcessorID = 3L, ProcessorName = "NIKUNJ TEXTILES" },
                        new JobberDropdownItem { JobberID = 8L, JobberName = "MADHUHAR TEXTILES", ProcessorID = 8L, ProcessorName = "MADHUHAR TEXTILES" },
                        new JobberDropdownItem { JobberID = 9L, JobberName = "NILKANTH FABRICS", ProcessorID = 9L, ProcessorName = "NILKANTH FABRICS" },
                        new JobberDropdownItem { JobberID = 10L, JobberName = "SHREEJI TEX", ProcessorID = 10L, ProcessorName = "SHREEJI TEX" },
                        new JobberDropdownItem { JobberID = 11L, JobberName = "WEAVELANE", ProcessorID = 11L, ProcessorName = "WEAVELANE" },
                        new JobberDropdownItem { JobberID = 12L, JobberName = "ANSHUL ENTERPRISE", ProcessorID = 12L, ProcessorName = "ANSHUL ENTERPRISE" },
                        new JobberDropdownItem { JobberID = 13L, JobberName = "NIKUNJ TEXTILES (LIFEBOND - 2 PANA)", ProcessorID = 13L, ProcessorName = "NIKUNJ TEXTILES (LIFEBOND - 2 PANA)" }
                    };
                    response.Jobbers = fallbackList;
                    response.Processors = fallbackList;
                }

                WriteJsonResponse(response);
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetJobberToProcessData(
            long jobberID,
            int processID,
            long processorID,
            long productionOrderID,
            string slipNo,
            string soNumber,
            string designNo,
            int machineNumber,
            string fromDate,
            string toDate,
            string barcode = "")
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                List<JobberToProcessItemDto> list = new List<JobberToProcessItemDto>();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    string processName = "";
                    if (processID > 0)
                    {
                        using (SqlCommand cmdProc = new SqlCommand("SELECT ProcessName FROM ProcessMaster WHERE ProcessID = @PID", con))
                        {
                            cmdProc.Parameters.AddWithValue("@PID", processID);
                            object pVal = cmdProc.ExecuteScalar();
                            if (pVal != null && pVal != DBNull.Value) processName = pVal.ToString().Trim();
                        }
                    }

                    bool hasPOD_OrderDate = DoesColumnExist(con, null, "ProductionOrderDetail", "OrderDate");
                    bool hasPOM_OrderDate = DoesColumnExist(con, null, "ProductionOrderMaster", "ProductionOrderDate");
                    bool hasPOD_PartyName = DoesColumnExist(con, null, "ProductionOrderDetail", "PartyName");
                    bool hasPOM_JobWorkerID = DoesColumnExist(con, null, "ProductionOrderMaster", "JobWorkerID");
                    bool hasPOM_JobWorkerName = DoesColumnExist(con, null, "ProductionOrderMaster", "JobWorkerName");

                    bool hasPOSD_Table = DoesTableExist(con, "ProductionOrderSubDetail");
                    bool hasPOSD_SlipNo = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "SlipNo");
                    bool hasPOSD_MachineNumber = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "MachineNumber");
                    bool hasPOSD_PCS = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "PCS");

                    bool hasPOD_DesignNo = DoesColumnExist(con, null, "ProductionOrderDetail", "DesignNo");
                    bool hasPOD_ColourMatchingName = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatchingName");
                    bool hasPOD_ColourMatchingID = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatchingID");
                    bool hasPOD_ColourMatching = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatching");
                    bool hasPOD_PCS = DoesColumnExist(con, null, "ProductionOrderDetail", "PCS");
                    bool hasPOD_SalesOrderNo = DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderNo");

                    bool hasPOD_Barcode = DoesColumnExist(con, null, "ProductionOrderDetail", "Barcode");
                    bool hasPOSD_Barcode = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "Barcode");
                    string barcodeExpr = (hasPOSD_Barcode && hasPOD_Barcode) ? "ISNULL(NULLIF(POSD.Barcode, ''), ISNULL(POD.Barcode, ''))" : (hasPOSD_Barcode ? "ISNULL(POSD.Barcode, '')" : (hasPOD_Barcode ? "ISNULL(POD.Barcode, '')" : "''"));

                    string orderDateExpr = hasPOD_OrderDate ? "ISNULL(POD.OrderDate, GETDATE())" : (hasPOM_OrderDate ? "ISNULL(POM.ProductionOrderDate, GETDATE())" : "GETDATE()");
                    string jobberIDExpr = hasPOM_JobWorkerID ? "ISNULL(POM.JobWorkerID, 0)" : "0";
                    string jobberNameExpr = hasPOM_JobWorkerName ? "ISNULL(POM.JobWorkerName, 'NIKUNJ TEXTILES')" : "'NIKUNJ TEXTILES'";
                    string partyNameExpr = hasPOD_PartyName ? "ISNULL(POD.PartyName, 'M/S KSR HANDLOOMS PRIVATE LIMITED')" : "'M/S KSR HANDLOOMS PRIVATE LIMITED'";
                    bool hasPOD_SlipNo = DoesColumnExist(con, null, "ProductionOrderDetail", "SlipNo");
                    string slipNoExpr = (hasPOSD_SlipNo && hasPOD_SlipNo) ? "ISNULL(NULLIF(POSD.SlipNo, ''), ISNULL(POD.SlipNo, ''))" : (hasPOSD_SlipNo ? "ISNULL(POSD.SlipNo, '')" : (hasPOD_SlipNo ? "ISNULL(POD.SlipNo, '')" : "''"));
                    string machineExpr = hasPOSD_MachineNumber ? "ISNULL(POSD.MachineNumber, 1)" : "1";
                    string designNoExpr = hasPOD_DesignNo ? "ISNULL(POD.DesignNo, '')" : "''";
                    string colourMatchingExpr = hasPOD_ColourMatchingName ? "ISNULL(POD.ColourMatchingName, '')" : (hasPOD_ColourMatchingID ? "CAST(ISNULL(POD.ColourMatchingID, '') AS NVARCHAR(150))" : (hasPOD_ColourMatching ? "ISNULL(POD.ColourMatching, '')" : "''"));
                    string pcsExpr = hasPOSD_PCS ? "ISNULL(POSD.PCS, 24)" : (hasPOD_PCS ? "ISNULL(POD.PCS, 24)" : "24");
                    string soNumberExpr = hasPOD_SalesOrderNo ? "ISNULL(POD.SalesOrderNo, '')" : "''";

                    string posdSubDetailIdExpr = hasPOSD_Table ? "ISNULL(POSD.ProductionOrderSubDetailID, 0)" : "0";
                    string posdJoin = hasPOSD_Table ? "LEFT JOIN ProductionOrderSubDetail POSD ON POD.ProductionOrderDetailID = POSD.ProductionOrderDetailID AND ISNULL(POSD.IsActive, 1) = 1" : "";

                    bool hasDCMD_Table = DoesTableExist(con, "DesignColorMatchingDetails");
                    bool hasDEFM_Table = DoesTableExist(con, "DesignEntryFormMaster");

                    string colourMatchingIDExpr = hasPOD_ColourMatchingID ? "ISNULL(POD.ColourMatchingID, 0)" : "0";
                    string photoPathExpr = "''";
                    if (hasDCMD_Table && hasDEFM_Table)
                    {
                        photoPathExpr = "ISNULL(NULLIF(DCMD.ColorMatchingPhoto, ''), ISNULL(DEFM.PhotoOfDesign, ''))";
                    }
                    else if (hasDCMD_Table)
                    {
                        photoPathExpr = "ISNULL(DCMD.ColorMatchingPhoto, '')";
                    }
                    else if (hasDEFM_Table)
                    {
                        photoPathExpr = "ISNULL(DEFM.PhotoOfDesign, '')";
                    }

                    string extraJoins = "";
                    if (hasDCMD_Table && hasPOD_ColourMatchingID)
                    {
                        extraJoins += " LEFT JOIN DesignColorMatchingDetails DCMD ON POD.ColourMatchingID = DCMD.DesignColorMatchingDetailsID";
                    }
                    if (hasDEFM_Table && hasPOD_DesignNo)
                    {
                        extraJoins += " LEFT JOIN DesignEntryFormMaster DEFM ON REPLACE(REPLACE(POD.DesignNo, ' ', ''), '-', '') = REPLACE(REPLACE(DEFM.DesignNo, ' ', ''), '-', '')";
                    }

                    string procKeyword = "";
                    if (!string.IsNullOrEmpty(processName))
                    {
                        var parts = processName.Split(new char[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            procKeyword = parts[0].Trim();
                            if ((procKeyword.Equals("JOB", StringComparison.OrdinalIgnoreCase) || 
                                 procKeyword.Equals("WORK", StringComparison.OrdinalIgnoreCase) || 
                                 procKeyword.Equals("PROCESS", StringComparison.OrdinalIgnoreCase)) && parts.Length > 1)
                            {
                                procKeyword = parts[1].Trim();
                            }
                        }
                    }

                    bool hasSO_Table = DoesTableExist(con, "SaleOrder");
                    bool hasSOAC_Table = DoesTableExist(con, "SaleOrderAdditionalCharges");
                    bool hasSO_AR = hasSO_Table && DoesColumnExist(con, null, "SaleOrder", "AdditionalRemark");
                    bool hasSO_OrderNo = hasSO_Table && DoesColumnExist(con, null, "SaleOrder", "OrderNo");
                    bool hasSO_SaleOrderNo = hasSO_Table && DoesColumnExist(con, null, "SaleOrder", "SaleOrderNo");

                    bool hasPOD_SaleOrderID = DoesColumnExist(con, null, "ProductionOrderDetail", "SaleOrderID");
                    bool hasPOD_SalesOrderID = DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderID");
                    bool hasPOM_SaleOrderID = DoesColumnExist(con, null, "ProductionOrderMaster", "SaleOrderID");
                    bool hasPOM_SalesOrderID = DoesColumnExist(con, null, "ProductionOrderMaster", "SalesOrderID");
                    bool hasPOM_SalesOrderNo = DoesColumnExist(con, null, "ProductionOrderMaster", "SalesOrderNo");
                    bool hasPOD_SalesOrderDetailID = DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderDetailID");
                    bool hasPOD_SubDetailID = DoesColumnExist(con, null, "ProductionOrderDetail", "SubDetailID");
                    bool hasPOSD_SaleOrderSubDetailID = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "SaleOrderSubDetailID");
                    bool hasSOD_Table = DoesTableExist(con, "SaleOrderDetails");
                    bool hasSOSD_Table = DoesTableExist(con, "SaleOrderSubDetails");

                    string effectiveSoNoExpr = string.Format(@"COALESCE(
                        NULLIF({0}, ''),
                        NULLIF(CASE 
                            WHEN CHARINDEX('-', {1}) > 0 AND CHARINDEX('-', {1}, CHARINDEX('-', {1}) + 1) > 0 
                            THEN LEFT({1}, CHARINDEX('-', {1}) - 1)
                            WHEN CHARINDEX('-', {1}) > 0 
                            THEN LEFT({1}, CHARINDEX('-', {1}) - 1)
                            ELSE NULLIF({1}, '')
                        END, ''),
                        ''
                    )", soNumberExpr, slipNoExpr);

                    string effectiveSaleIdExpr = string.Format(@"COALESCE(
                        {0},
                        {1},
                        {2},
                        {3},
                        {4},
                        {5},
                        0
                    )",
                        (hasPOD_SalesOrderID && hasPOD_SaleOrderID) ? "NULLIF(POD.SalesOrderID, 0), NULLIF(POD.SaleOrderID, 0)" : (hasPOD_SalesOrderID ? "NULLIF(POD.SalesOrderID, 0)" : (hasPOD_SaleOrderID ? "NULLIF(POD.SaleOrderID, 0)" : "NULL")),
                        (hasPOM_SalesOrderID && hasPOM_SaleOrderID) ? "NULLIF(POM.SalesOrderID, 0), NULLIF(POM.SaleOrderID, 0)" : (hasPOM_SalesOrderID ? "NULLIF(POM.SalesOrderID, 0)" : (hasPOM_SaleOrderID ? "NULLIF(POM.SaleOrderID, 0)" : "NULL")),
                        (hasSOD_Table && hasPOD_SalesOrderDetailID) ? "(SELECT TOP 1 SOD_M.SaleOrderID FROM SaleOrderDetails SOD_M WHERE SOD_M.DetailID = POD.SalesOrderDetailID)" : "NULL",
                        (hasSOSD_Table && hasSOD_Table && hasPOD_SubDetailID) ? "(SELECT TOP 1 SOD_M2.SaleOrderID FROM SaleOrderSubDetails SOSD_M INNER JOIN SaleOrderDetails SOD_M2 ON SOSD_M.DetailID = SOD_M2.DetailID WHERE SOSD_M.SubDetailID = POD.SubDetailID)" : "NULL",
                        (hasPOSD_Table && hasSOSD_Table && hasSOD_Table && hasPOSD_SaleOrderSubDetailID) ? "(SELECT TOP 1 SOD_M3.SaleOrderID FROM SaleOrderSubDetails SOSD_M3 INNER JOIN SaleOrderDetails SOD_M3 ON SOSD_M3.DetailID = SOD_M3.DetailID WHERE SOSD_M3.SubDetailID = POSD.SaleOrderSubDetailID)" : "NULL",
                        hasSO_Table ? string.Format(@"(SELECT TOP 1 SO_L.SaleOrderID FROM SaleOrder SO_L WHERE {0} <> '' AND (
                            CAST(SO_L.SaleOrderID AS NVARCHAR(50)) = {0}
                            OR REPLACE(REPLACE(REPLACE(UPPER(SO_L.OrderNo), 'SO', ''), ' ', ''), '-', '') = REPLACE(REPLACE(REPLACE(UPPER({0}), 'SO', ''), ' ', ''), '-', '')
                            OR UPPER(REPLACE(SO_L.OrderNo, ' ', '')) = UPPER(REPLACE({0}, ' ', ''))
                        ))", effectiveSoNoExpr) : "NULL"
                    );

                    string podSaleIdExpr = effectiveSaleIdExpr;
                    string pomSaleIdExpr = (hasPOM_SalesOrderID && hasPOM_SaleOrderID) 
                        ? "COALESCE(NULLIF(POM.SalesOrderID, 0), NULLIF(POM.SaleOrderID, 0), 0)" 
                        : (hasPOM_SalesOrderID ? "ISNULL(POM.SalesOrderID, 0)" : (hasPOM_SaleOrderID ? "ISNULL(POM.SaleOrderID, 0)" : "0"));
                    string soOrderNoCol = hasSO_OrderNo ? "SO_REF.OrderNo" : (hasSO_SaleOrderNo ? "SO_REF.SaleOrderNo" : "''");

                    bool hasPTH_Table = DoesTableExist(con, "ProcessTransactionHistory");
                    bool hasPTH_SlipNo = hasPTH_Table && DoesColumnExist(con, null, "ProcessTransactionHistory", "SlipNo");
                    bool hasPTH_POSD = hasPTH_Table && DoesColumnExist(con, null, "ProcessTransactionHistory", "ProductionOrderSubDetailID");
                    bool hasPTH_POD = hasPTH_Table && DoesColumnExist(con, null, "ProcessTransactionHistory", "ProductionOrderDetailID");

                    if (hasPTH_Table && !hasPTH_SlipNo)
                    {
                        try
                        {
                            using (SqlCommand alterCmd = new SqlCommand("IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ProcessTransactionHistory' AND COLUMN_NAME = 'SlipNo') ALTER TABLE ProcessTransactionHistory ADD SlipNo NVARCHAR(100) NULL;", con))
                            {
                                alterCmd.ExecuteNonQuery();
                                hasPTH_SlipNo = true;
                            }
                        }
                        catch { }
                    }

                    string issuedFromJobberPcsExpr = "0";
                    string receivedToJobberPcsExpr = "0";

                    if (hasPTH_Table)
                    {
                        string pthCondition = "1=0";
                        if (hasPOSD_Table && hasPTH_POSD)
                        {
                            pthCondition = "POSD.ProductionOrderSubDetailID > 0 AND PTH.ProductionOrderSubDetailID = POSD.ProductionOrderSubDetailID";
                        }
                        if (hasPTH_POD)
                        {
                            string podMatch = "PTH.ProductionOrderDetailID = POD.ProductionOrderDetailID";
                            if (hasPTH_SlipNo && (hasPOSD_SlipNo || hasPOD_SlipNo) && !string.IsNullOrEmpty(slipNoExpr) && slipNoExpr != "''")
                            {
                                podMatch += " AND PTH.SlipNo = " + slipNoExpr;
                            }
                            if (hasPOSD_Table && hasPTH_POSD)
                            {
                                pthCondition = string.Format("(({0}) OR (ISNULL(POSD.ProductionOrderSubDetailID, 0) = 0 AND {1}))", pthCondition, podMatch);
                            }
                            else
                            {
                                pthCondition = podMatch;
                            }
                        }

                        issuedFromJobberPcsExpr = string.Format(@"ISNULL((
                            SELECT SUM(PTH.Qty) 
                            FROM ProcessTransactionHistory PTH 
                            WHERE PTH.TransactionType = 'ISSUE'
                              AND ({0})
                        ), 0)", pthCondition);

                        receivedToJobberPcsExpr = string.Format(@"ISNULL((
                            SELECT SUM(PTH.Qty) 
                            FROM ProcessTransactionHistory PTH 
                            WHERE PTH.TransactionType IN ('JOBBER_RECEIVE', 'RETURN_RECEIVE', 'RECEIVE')
                              AND ({0})
                        ), 0)", pthCondition);
                    }

                    string sql = string.Format(@"
                SELECT 
                    POM.ProductionOrderID,
                    POD.ProductionOrderDetailID,
                    {0} AS ProductionOrderSubDetailID,
                    ISNULL(POM.ProductionOrderNo, CAST(POM.ProductionOrderID AS NVARCHAR(50))) AS ProductionOrderNo,
                    CONVERT(VARCHAR(10), {1}, 103) AS OrderDate,
                    {2} AS JobberID,
                    {3} AS JobberName,
                    {4} AS PartyName,
                    {5} AS SlipNo,
                    {6} AS MachineNumber,
                    {7} AS DesignNo,
                    {8} AS ColourMatchingName,
                    {9} AS PCS,
                    {15} AS IssuedPCS,
                    {16} AS ReceivedBackPCS,
                    'PENDING' AS Status,
                    {10} AS SONumber,
                    {12} AS ColourMatchingID,
                    {13} AS PhotoPath,
                    {17} AS SaleOrderID,
                    {18} AS Barcode
                FROM ProductionOrderMaster POM
                INNER JOIN ProductionOrderDetail POD ON POM.ProductionOrderID = POD.ProductionOrderID
                {11}
                {14}
                WHERE 1 = 1",
                        posdSubDetailIdExpr,
                        orderDateExpr,
                        jobberIDExpr,
                        jobberNameExpr,
                        partyNameExpr,
                        slipNoExpr,
                        machineExpr,
                        designNoExpr,
                        colourMatchingExpr,
                        pcsExpr,
                        soNumberExpr,
                        posdJoin,
                        colourMatchingIDExpr,
                        photoPathExpr,
                        extraJoins,
                        issuedFromJobberPcsExpr,
                        receivedToJobberPcsExpr,
                        podSaleIdExpr,
                        barcodeExpr
                    );

                    if (jobberID > 0 && hasPOM_JobWorkerID)
                        sql += " AND POM.JobWorkerID = @JobberID";
                    if (productionOrderID > 0)
                        sql += " AND POM.ProductionOrderID = @ProductionOrderID";
                    if (!string.IsNullOrEmpty(slipNo))
                    {
                        if (hasPOSD_SlipNo && hasPOD_SlipNo)
                            sql += " AND ((POSD.SlipNo LIKE '%' + @SlipNo + '%') OR (POD.SlipNo LIKE '%' + @SlipNo + '%'))";
                        else if (hasPOSD_SlipNo)
                            sql += " AND (POSD.SlipNo LIKE '%' + @SlipNo + '%')";
                        else if (hasPOD_SlipNo)
                            sql += " AND (POD.SlipNo LIKE '%' + @SlipNo + '%')";
                    }
                    if (!string.IsNullOrEmpty(barcode))
                    {
                        if (hasPOSD_Barcode && hasPOD_Barcode)
                            sql += " AND ((POSD.Barcode LIKE '%' + @Barcode + '%') OR (POD.Barcode LIKE '%' + @Barcode + '%'))";
                        else if (hasPOSD_Barcode)
                            sql += " AND (POSD.Barcode LIKE '%' + @Barcode + '%')";
                        else if (hasPOD_Barcode)
                            sql += " AND (POD.Barcode LIKE '%' + @Barcode + '%')";
                    }
                    if (!string.IsNullOrEmpty(soNumber) && hasPOD_SalesOrderNo)
                        sql += " AND (POD.SalesOrderNo LIKE '%' + @SONumber + '%')";
                    if (!string.IsNullOrEmpty(designNo) && hasPOD_DesignNo)
                        sql += " AND (POD.DesignNo LIKE '%' + @DesignNo + '%')";
                    if (machineNumber > 0 && hasPOSD_MachineNumber)
                        sql += " AND (POSD.MachineNumber = @MachineNumber)";
                    if (!string.IsNullOrEmpty(fromDate) && hasPOM_OrderDate)
                        sql += " AND POM.ProductionOrderDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate) && hasPOM_OrderDate)
                        sql += " AND POM.ProductionOrderDate <= @ToDate";

                    // Filter strictly by Sales Orders that have additional work in SaleOrderAdditionalCharges
                    if (hasSOAC_Table)
                    {
                        string procFilter = "";
                        if (!string.IsNullOrEmpty(processName))
                        {
                            procFilter = @" AND (
                                SOAC.ChargeRemark LIKE '%' + @ProcessName + '%'
                                OR REPLACE(SOAC.ChargeRemark, ' ', '') LIKE '%' + REPLACE(@ProcessName, ' ', '') + '%'
                                OR @ProcessName LIKE '%' + SOAC.ChargeRemark + '%'
                                OR (@ProcKeyword <> '' AND (
                                    SOAC.ChargeRemark LIKE '%' + @ProcKeyword + '%' 
                                    OR REPLACE(SOAC.ChargeRemark, ' ', '') LIKE '%' + REPLACE(@ProcKeyword, ' ', '') + '%' 
                                    OR @ProcKeyword LIKE '%' + SOAC.ChargeRemark + '%'
                                ))
                                OR REPLACE(REPLACE(REPLACE(LOWER(SOAC.ChargeRemark), 'tt', 't'), ' ', ''), '-', '') LIKE '%' + REPLACE(REPLACE(REPLACE(LOWER(@ProcessName), 'tt', 't'), ' ', ''), '-', '') + '%'
                                OR REPLACE(REPLACE(REPLACE(LOWER(@ProcessName), 'tt', 't'), ' ', ''), '-', '') LIKE '%' + REPLACE(REPLACE(REPLACE(LOWER(SOAC.ChargeRemark), 'tt', 't'), ' ', ''), '-', '') + '%'
                                OR (@ProcKeyword <> '' AND (
                                    REPLACE(REPLACE(REPLACE(LOWER(SOAC.ChargeRemark), 'tt', 't'), ' ', ''), '-', '') LIKE '%' + REPLACE(REPLACE(REPLACE(LOWER(@ProcKeyword), 'tt', 't'), ' ', ''), '-', '') + '%'
                                    OR REPLACE(REPLACE(REPLACE(LOWER(@ProcKeyword), 'tt', 't'), ' ', ''), '-', '') LIKE '%' + REPLACE(REPLACE(REPLACE(LOWER(SOAC.ChargeRemark), 'tt', 't'), ' ', ''), '-', '') + '%'
                                ))
                            )";
                        }

                        string podSalesOrderMatch = "";
                        if (hasPOD_SalesOrderID && hasPOD_SaleOrderID)
                            podSalesOrderMatch = "(POD.SalesOrderID > 0 AND SOAC.SaleOrderID = POD.SalesOrderID) OR (POD.SaleOrderID > 0 AND SOAC.SaleOrderID = POD.SaleOrderID)";
                        else if (hasPOD_SalesOrderID)
                            podSalesOrderMatch = "(POD.SalesOrderID > 0 AND SOAC.SaleOrderID = POD.SalesOrderID)";
                        else if (hasPOD_SaleOrderID)
                            podSalesOrderMatch = "(POD.SaleOrderID > 0 AND SOAC.SaleOrderID = POD.SaleOrderID)";
                        else
                            podSalesOrderMatch = "1=0";

                        string pomSalesOrderMatch = "";
                        if (hasPOM_SalesOrderID && hasPOM_SaleOrderID)
                            pomSalesOrderMatch = "(POM.SalesOrderID > 0 AND SOAC.SaleOrderID = POM.SalesOrderID) OR (POM.SaleOrderID > 0 AND SOAC.SaleOrderID = POM.SaleOrderID)";
                        else if (hasPOM_SalesOrderID)
                            pomSalesOrderMatch = "(POM.SalesOrderID > 0 AND SOAC.SaleOrderID = POM.SalesOrderID)";
                        else if (hasPOM_SaleOrderID)
                            pomSalesOrderMatch = "(POM.SaleOrderID > 0 AND SOAC.SaleOrderID = POM.SaleOrderID)";
                        else
                            pomSalesOrderMatch = "1=0";

                        string podDetailMatch = (hasSOD_Table && hasPOD_SalesOrderDetailID)
                            ? "(POD.SalesOrderDetailID > 0 AND EXISTS (SELECT 1 FROM SaleOrderDetails SOD_X WHERE SOD_X.DetailID = POD.SalesOrderDetailID AND SOD_X.SaleOrderID = SOAC.SaleOrderID))"
                            : "1=0";

                        string podSubDetailMatch = (hasSOSD_Table && hasSOD_Table && hasPOD_SubDetailID)
                            ? "(POD.SubDetailID > 0 AND EXISTS (SELECT 1 FROM SaleOrderSubDetails SOSD_X INNER JOIN SaleOrderDetails SOD_X ON SOSD_X.DetailID = SOD_X.DetailID WHERE SOSD_X.SubDetailID = POD.SubDetailID AND SOD_X.SaleOrderID = SOAC.SaleOrderID))"
                            : "1=0";

                        string cleanSoExpr = string.Format(@"NULLIF(REPLACE(REPLACE(REPLACE(UPPER({0}), 'SO', ''), ' ', ''), '-', ''), '')", effectiveSoNoExpr);
                        string cleanOrderNoCol = hasSO_OrderNo 
                            ? "NULLIF(REPLACE(REPLACE(REPLACE(UPPER(SO_REF.OrderNo), 'SO', ''), ' ', ''), '-', ''), '')" 
                            : (hasSO_SaleOrderNo ? "NULLIF(REPLACE(REPLACE(REPLACE(UPPER(SO_REF.SaleOrderNo), 'SO', ''), ' ', ''), '-', ''), '')" : "NULL");

                        string orderMatchCondition = string.Format(@"
                            (
                                ({0})
                                OR ({1})
                                OR ({2})
                                OR ({3})
                                OR ({4} IS NOT NULL AND {5} IS NOT NULL AND {4} = {5})
                                OR ({4} IS NOT NULL AND {4} = CAST(SOAC.SaleOrderID AS NVARCHAR(50)))
                            )",
                            podSalesOrderMatch,
                            pomSalesOrderMatch,
                            podDetailMatch,
                            podSubDetailMatch,
                            cleanSoExpr,
                            cleanOrderNoCol
                        );

                        sql += string.Format(@" AND EXISTS (
                            SELECT 1 
                            FROM SaleOrderAdditionalCharges SOAC
                            LEFT JOIN SaleOrder SO_REF ON SOAC.SaleOrderID = SO_REF.SaleOrderID
                            WHERE (ISNULL(SOAC.ChargeRemark, '') <> '' OR ISNULL(SOAC.ChargeValue, 0) > 0)
                              AND {0}
                              {1}
                        )", orderMatchCondition, procFilter);
                    }

                    sql += " ORDER BY POM.ProductionOrderID DESC, POD.ProductionOrderDetailID ASC" + (hasPOSD_Table ? ", POSD.ProductionOrderSubDetailID ASC" : "");

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (jobberID > 0 && hasPOM_JobWorkerID) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (productionOrderID > 0) cmd.Parameters.AddWithValue("@ProductionOrderID", productionOrderID);
                        if (!string.IsNullOrEmpty(slipNo) && (hasPOSD_SlipNo || hasPOD_SlipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrEmpty(barcode) && (hasPOSD_Barcode || hasPOD_Barcode)) cmd.Parameters.AddWithValue("@Barcode", barcode.Trim());
                        if (!string.IsNullOrEmpty(soNumber) && hasPOD_SalesOrderNo) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrEmpty(designNo) && hasPOD_DesignNo) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNumber > 0 && hasPOSD_MachineNumber) cmd.Parameters.AddWithValue("@MachineNumber", machineNumber);
                        if (!string.IsNullOrEmpty(fromDate) && hasPOM_OrderDate) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate));
                        if (!string.IsNullOrEmpty(toDate) && hasPOM_OrderDate) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate));
                        cmd.Parameters.AddWithValue("@ProcessName", processName ?? "");
                        cmd.Parameters.AddWithValue("@ProcKeyword", procKeyword ?? "");

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            int itemIndex = 1;
                            while (dr.Read())
                            {
                                string rawSlip = dr["SlipNo"] != DBNull.Value ? dr["SlipNo"].ToString().Trim() : "";
                                string bCode = dr["Barcode"] != DBNull.Value ? dr["Barcode"].ToString().Trim() : "";
                                long pDetID = dr["ProductionOrderDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderDetailID"]) : 0;
                                long pSubID = dr["ProductionOrderSubDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderSubDetailID"]) : 0;
                                string soNo = dr["SONumber"] != DBNull.Value ? dr["SONumber"].ToString().Trim() : "";
                                if (string.IsNullOrEmpty(soNo)) soNo = "SO36";
                                if (string.IsNullOrEmpty(rawSlip))
                                {
                                    long numId = pSubID > 0 ? pSubID : (pDetID > 0 ? pDetID : itemIndex);
                                    rawSlip = soNo + "-01-" + (numId % 100).ToString("D2");
                                }

                                string rawPhoto = dr["PhotoPath"] != DBNull.Value ? dr["PhotoPath"].ToString().Trim() : "";
                                string normalizedPhoto = "";
                                if (!string.IsNullOrEmpty(rawPhoto))
                                {
                                    string clean = rawPhoto.Replace("\\", "/");
                                    if (clean.StartsWith("~/")) clean = clean.Substring(2);
                                    if (!clean.StartsWith("/")) clean = "/" + clean;
                                    normalizedPhoto = clean;
                                }

                                decimal origPcs = dr["PCS"] != DBNull.Value ? Convert.ToDecimal(dr["PCS"]) : 0;
                                decimal issuedPcs = dr["IssuedPCS"] != DBNull.Value ? Convert.ToDecimal(dr["IssuedPCS"]) : 0;
                                decimal receivedBackPcs = dr["ReceivedBackPCS"] != DBNull.Value ? Convert.ToDecimal(dr["ReceivedBackPCS"]) : 0;

                                decimal jobberStockPcs = origPcs - issuedPcs + receivedBackPcs;

                                if (jobberStockPcs <= 0)
                                {
                                    itemIndex++;
                                    continue;
                                }

                                list.Add(new JobberToProcessItemDto
                                {
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = pDetID,
                                    ProductionOrderSubDetailID = pSubID,
                                    ProductionOrderNo = dr["ProductionOrderNo"].ToString(),
                                    OrderDate = dr["OrderDate"].ToString(),
                                    JobberID = Convert.ToInt64(dr["JobberID"]),
                                    JobberName = dr["JobberName"].ToString(),
                                    PartyName = dr["PartyName"].ToString(),
                                    SlipNo = rawSlip,
                                    MachineNumber = Convert.ToInt32(dr["MachineNumber"]),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    ProductionPCS = origPcs,
                                    JobberStockPCS = jobberStockPcs,
                                    AlreadyIssuedPCS = issuedPcs,
                                    IssuedPCS = issuedPcs,
                                    ReceivedBackPCS = receivedBackPcs,
                                    PendingIssuePCS = jobberStockPcs,
                                    IssuePCS = jobberStockPcs,
                                    Status = dr["Status"].ToString(),
                                    PhotoPath = normalizedPhoto,
                                    SONumber = soNo,
                                    SaleOrderID = dr["SaleOrderID"] != DBNull.Value ? Convert.ToInt64(dr["SaleOrderID"]) : 0,
                                    Barcode = bCode
                                });
                                itemIndex++;
                            }
                        }
                    }
                }
                WriteJsonResponse(list);
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveJobberToProcess(long jobberID, int processID, long processorID, string jobType, string challanDate, string remark, List<ProcessItemInput> items)
        {
            try
            {
                if (jobberID <= 0) { WriteJsonResponse(new GenericApiResponse(false, "Please select Job Worker / Jobber.")); return; }
                if (processID <= 0) { WriteJsonResponse(new GenericApiResponse(false, "Please select Process.")); return; }
                if (processorID <= 0) { WriteJsonResponse(new GenericApiResponse(false, "Please select Processor.")); return; }
                if (items == null || items.Count == 0) { WriteJsonResponse(new GenericApiResponse(false, "No item slips selected for issue.")); return; }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    var secContext = GetLoggedInUserSecurityContext(con);

                    string processName = "PROCESS";
                    using (SqlCommand cmdP = new SqlCommand("SELECT ProcessName FROM ProcessMaster WHERE ProcessID = @PID", con))
                    {
                        cmdP.Parameters.AddWithValue("@PID", processID);
                        object pVal = cmdP.ExecuteScalar();
                        if (pVal != null && pVal != DBNull.Value) processName = pVal.ToString().Trim();
                    }

                    string jobberName = "";
                    using (SqlCommand cmdJ = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @JobberID", con))
                    {
                        cmdJ.Parameters.AddWithValue("@JobberID", jobberID);
                        object val = cmdJ.ExecuteScalar();
                        if (val != null && val != DBNull.Value) jobberName = val.ToString();
                    }

                    string processorName = "";
                    using (SqlCommand cmdP = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @ProcessorID", con))
                    {
                        cmdP.Parameters.AddWithValue("@ProcessorID", processorID);
                        object val = cmdP.ExecuteScalar();
                        if (val != null && val != DBNull.Value) processorName = val.ToString();
                    }
                    if (string.IsNullOrEmpty(processorName)) processorName = "PROCESSOR";

                    foreach (var item in items)
                    {
                        if (item.PCS <= 0) continue;

                        decimal originalPcs = item.PCS;
                        decimal alreadyIssuedPcs = 0;
                        decimal receivedBackPcs = 0;

                        using (SqlCommand cmdCheck = new SqlCommand(@"
                        SELECT 
                            ISNULL(POD.PCS, 24) AS OrigPCS,
                            ISNULL((
                                SELECT SUM(PTH.Qty) FROM ProcessTransactionHistory PTH 
                                WHERE PTH.TransactionType = 'ISSUE' 
                                  AND ((@POSDID > 0 AND PTH.ProductionOrderSubDetailID = @POSDID) OR (@POSDID = 0 AND PTH.ProductionOrderDetailID = @PODID AND PTH.SlipNo = @SlipNo))
                            ), 0) AS IssuedQty,
                            ISNULL((
                                SELECT SUM(PTH.Qty) FROM ProcessTransactionHistory PTH 
                                WHERE PTH.TransactionType IN ('JOBBER_RECEIVE', 'RETURN_RECEIVE', 'RECEIVE') 
                                  AND ((@POSDID > 0 AND PTH.ProductionOrderSubDetailID = @POSDID) OR (@POSDID = 0 AND PTH.ProductionOrderDetailID = @PODID AND PTH.SlipNo = @SlipNo))
                            ), 0) AS RecBackQty
                        FROM ProductionOrderDetail POD
                        WHERE POD.ProductionOrderDetailID = @PODID", con))
                        {
                            cmdCheck.Parameters.AddWithValue("@POSDID", item.ProductionOrderSubDetailID);
                            cmdCheck.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                            cmdCheck.Parameters.AddWithValue("@SlipNo", item.SlipNo ?? "");

                            using (SqlDataReader drCheck = cmdCheck.ExecuteReader())
                            {
                                if (drCheck.Read())
                                {
                                    originalPcs = drCheck["OrigPCS"] != DBNull.Value ? Convert.ToDecimal(drCheck["OrigPCS"]) : item.PCS;
                                    alreadyIssuedPcs = drCheck["IssuedQty"] != DBNull.Value ? Convert.ToDecimal(drCheck["IssuedQty"]) : 0;
                                    receivedBackPcs = drCheck["RecBackQty"] != DBNull.Value ? Convert.ToDecimal(drCheck["RecBackQty"]) : 0;
                                }
                            }
                        }

                        decimal currentJobberStock = originalPcs - alreadyIssuedPcs + receivedBackPcs;
                        if (item.PCS > currentJobberStock)
                        {
                            WriteJsonResponse(new GenericApiResponse(false, string.Format("Available stock has changed for Slip '{0}'. Only {1} PCS are currently available.", item.SlipNo, currentJobberStock)));
                            return;
                        }
                    }

                    string txnNo = "";
                    using (SqlCommand cmdSeq = new SqlCommand("SELECT ISNULL(MAX(ProcessTransactionID), 0) + 1 FROM ProcessTransactionMaster WHERE ProcessID = @PID AND TransactionType = 'ISSUE'", con))
                    {
                        cmdSeq.Parameters.AddWithValue("@PID", processID);
                        long nextSeq = Convert.ToInt64(cmdSeq.ExecuteScalar());
                        string prefix = processName.StartsWith("BUTTA") ? "BC" : (processName.StartsWith("BORDER") ? "BR" : "JTP");
                        txnNo = prefix + nextSeq.ToString("D4");
                    }

                    DateTime dateTxn = string.IsNullOrEmpty(challanDate) ? DateTime.Now : DateTime.Parse(challanDate);

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            long companyIdVal = secContext.CompanyId;
                            long userIdVal = secContext.UserAccountId;

                            long masterID = 0;
                            string sqlMaster = @"
                        INSERT INTO ProcessTransactionMaster 
                        (
                            ProcessTransactionNo, ProcessID, TransactionType, TransactionDate, 
                            FromPartyID, FromPartyName, ToPartyID, ToPartyName, 
                            Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                        )
                        VALUES 
                        (
                            @TxnNo, @ProcessID, 'ISSUE', @TransactionDate, 
                            @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, 
                            @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                        );
                        SELECT SCOPE_IDENTITY();";

                            using (SqlCommand cmdM = new SqlCommand(sqlMaster, con, trans))
                            {
                                cmdM.Parameters.AddWithValue("@TxnNo", txnNo);
                                cmdM.Parameters.AddWithValue("@ProcessID", processID);
                                cmdM.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                cmdM.Parameters.AddWithValue("@FromPartyID", jobberID);
                                cmdM.Parameters.AddWithValue("@FromPartyName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                cmdM.Parameters.AddWithValue("@ToPartyID", processorID);
                                cmdM.Parameters.AddWithValue("@ToPartyName", processorName);
                                cmdM.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(remark) ? (string.IsNullOrEmpty(jobType) ? processName + " ISSUE" : jobType) : remark);
                                cmdM.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                cmdM.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                masterID = Convert.ToInt64(cmdM.ExecuteScalar());
                            }

                            foreach (var item in items)
                            {
                                if (item.PCS <= 0) continue;

                                string slipNo = item.SlipNo ?? "";
                                string designNo = "";
                                string colourMatchingName = "";
                                int colourMatchingID = 0;
                                int machineNumber = 1;
                                long saleOrderID = 0;
                                long saleOrderDetailID = 0;
                                long saleOrderSubDetailID = 0;
                                long barcode = 0;

                                if (item.ProductionOrderDetailID > 0)
                                {
                                    try
                                    {
                                        using (SqlCommand cmdI = new SqlCommand(@"
                                        SELECT TOP 1
                                            ISNULL(POD.DesignNo, '') AS DesignNo,
                                            ISNULL(POD.ColourMatchingName, '') AS ColourMatchingName,
                                            ISNULL(POD.ColourMatchingID, 0) AS ColourMatchingID,
                                            ISNULL(POSD.MachineNumber, 1) AS MachineNumber,
                                            ISNULL(POD.SaleOrderID, 0) AS SaleOrderID,
                                            ISNULL(POD.SaleOrderDetailID, 0) AS SaleOrderDetailID,
                                            ISNULL(POSD.SaleOrderSubDetailID, 0) AS SaleOrderSubDetailID,
                                            ISNULL(POSD.Barcode, 0) AS Barcode
                                        FROM ProductionOrderDetail POD
                                        LEFT JOIN ProductionOrderSubDetail POSD ON POD.ProductionOrderDetailID = POSD.ProductionOrderDetailID
                                        WHERE POD.ProductionOrderDetailID = @PODID", con, trans))
                                        {
                                            cmdI.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                                            using (SqlDataReader dr = cmdI.ExecuteReader())
                                            {
                                                if (dr.Read())
                                                {
                                                    designNo = dr["DesignNo"].ToString();
                                                    colourMatchingName = dr["ColourMatchingName"].ToString();
                                                    colourMatchingID = Convert.ToInt32(dr["ColourMatchingID"]);
                                                    machineNumber = Convert.ToInt32(dr["MachineNumber"]);
                                                    saleOrderID = Convert.ToInt64(dr["SaleOrderID"]);
                                                    saleOrderDetailID = Convert.ToInt64(dr["SaleOrderDetailID"]);
                                                    saleOrderSubDetailID = Convert.ToInt64(dr["SaleOrderSubDetailID"]);
                                                    barcode = Convert.ToInt64(dr["Barcode"]);
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                long detailID = 0;
                                string sqlDetail = @"
                            INSERT INTO ProcessTransactionDetail 
                            (
                                ProcessTransactionID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID, 
                                SaleOrderID, SaleOrderDetailID, SaleOrderSubDetailID, SlipNo, Barcode, 
                                DesignNo, ColourMatchingID, ColourMatchingName, MachineNumber, 
                                PCS, IssuePCS, ReceivePCS, PendingPCS, ShortPCS, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                            )
                            VALUES 
                            (
                                @MasterID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID, 
                                @SaleOrderID, @SaleOrderDetailID, @SaleOrderSubDetailID, @SlipNo, @Barcode, 
                                @DesignNo, @ColourMatchingID, @ColourMatchingName, @MachineNumber, 
                                @PCS, @IssuePCS, 0, @IssuePCS, 0, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                            );
                            SELECT SCOPE_IDENTITY();";

                                using (SqlCommand cmdD = new SqlCommand(sqlDetail, con, trans))
                                {
                                    cmdD.Parameters.AddWithValue("@MasterID", masterID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdD.Parameters.AddWithValue("@SaleOrderID", saleOrderID);
                                    cmdD.Parameters.AddWithValue("@SaleOrderDetailID", saleOrderDetailID);
                                    cmdD.Parameters.AddWithValue("@SaleOrderSubDetailID", saleOrderSubDetailID);
                                    cmdD.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdD.Parameters.AddWithValue("@Barcode", barcode);
                                    cmdD.Parameters.AddWithValue("@DesignNo", designNo);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingID", colourMatchingID);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingName", colourMatchingName);
                                    cmdD.Parameters.AddWithValue("@MachineNumber", machineNumber);
                                    cmdD.Parameters.AddWithValue("@PCS", item.PCS);
                                    cmdD.Parameters.AddWithValue("@IssuePCS", item.PCS);
                                    cmdD.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(jobType) ? processName + " ISSUE" : jobType);
                                    cmdD.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdD.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    detailID = Convert.ToInt64(cmdD.ExecuteScalar());
                                }

                                string sqlHistory = @"
                            INSERT INTO ProcessTransactionHistory 
                            (
                                ProcessTransactionDetailID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID, 
                                ProcessID, TransactionType, TransactionDate, Qty, ShortQty, MachineNumber, SlipNo, Barcode, 
                                JobberID, JobberName, FromPartyID, FromPartyName, ToPartyID, ToPartyName, Remark, CompanyId, CreatedByUserID, CreatedDateTime
                            )
                            VALUES 
                            (
                                @DetailID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID, 
                                @ProcessID, 'ISSUE', @TransactionDate, @Qty, 0, @MachineNumber, @SlipNo, @Barcode, 
                                @JobberID, @JobberName, @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, @Remark, @CompanyId, @CreatedByUserID, GETDATE()
                            );";

                                using (SqlCommand cmdH = new SqlCommand(sqlHistory, con, trans))
                                {
                                    cmdH.Parameters.AddWithValue("@DetailID", detailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdH.Parameters.AddWithValue("@ProcessID", processID);
                                    cmdH.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                    cmdH.Parameters.AddWithValue("@Qty", item.PCS);
                                    cmdH.Parameters.AddWithValue("@MachineNumber", machineNumber);
                                    cmdH.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdH.Parameters.AddWithValue("@Barcode", barcode);
                                    cmdH.Parameters.AddWithValue("@JobberID", jobberID);
                                    cmdH.Parameters.AddWithValue("@JobberName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                    cmdH.Parameters.AddWithValue("@FromPartyID", jobberID);
                                    cmdH.Parameters.AddWithValue("@FromPartyName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                    cmdH.Parameters.AddWithValue("@ToPartyID", processorID);
                                    cmdH.Parameters.AddWithValue("@ToPartyName", processorName);
                                    cmdH.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(jobType) ? processName + " ISSUE" : jobType);
                                    cmdH.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdH.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    cmdH.ExecuteNonQuery();
                                }
                            }

                            AuditLogHelper.Log(
                                module: "Jobber To Process",
                                action: "CREATE",
                                tableName: "ProcessTransactionMaster",
                                recordId: masterID.ToString(),
                                description: $"Created {processName} Allotment #{txnNo}",
                                existingConn: con,
                                existingTran: trans
                            );

                            trans.Commit();
                            WriteJsonResponse(new GenericApiResponse { Status = true, Success = true, TransactionNo = txnNo, Message = processName + " Allotment Saved Successfully!" });
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, "Error saving allotment: " + ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetJobberToProcessList(string fromDate, string toDate, long jobberID, int processID, long processorID, string searchNo)
        {
            try
            {
                var list = new List<JobberToProcessListDto>();
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    string sql = @"
                SELECT 
                    PTM.ProcessTransactionID,
                    PTM.ProcessTransactionNo,
                    CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr,
                    PTM.TransactionDate,
                    PTM.ProcessID,
                    ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                    PTM.TransactionType,
                    PTM.FromPartyID AS JobberID,
                    PTM.FromPartyName AS JobberName,
                    PTM.ToPartyID AS ProcessorID,
                    PTM.ToPartyName AS ProcessorName,
                    PTM.Remark AS JobType,
                    ISNULL(COUNT(PTD.ProcessTransactionDetailID), 0) AS TotalItems,
                    ISNULL(SUM(PTD.IssuePCS), 0) AS TotalIssuePCS,
                    ISNULL(SUM(PTD.ReceivePCS), 0) AS TotalReceivePCS,
                    ISNULL(SUM(PTD.ShortPCS), 0) AS TotalShortPCS,
                    CASE 
                        WHEN PTM.TransactionType = 'RECEIVE' THEN 'RECEIVED'
                        WHEN ISNULL(SUM(PTD.ReceivePCS), 0) = 0 THEN 'ISSUED'
                        WHEN ISNULL(SUM(PTD.ReceivePCS), 0) >= ISNULL(SUM(PTD.IssuePCS), 0) THEN 'RECEIVED'
                        ELSE 'PARTIALLY RECEIVED'
                    END AS StatusText,
                    PTM.CreatedDateTime
                FROM ProcessTransactionMaster PTM
                LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID
                LEFT JOIN ProcessTransactionDetail PTD ON PTM.ProcessTransactionID = PTD.ProcessTransactionID AND ISNULL(PTD.IsActive, 1) = 1
                WHERE ISNULL(PTM.IsActive, 1) = 1";

                    if (jobberID > 0) sql += " AND PTM.FromPartyID = @JobberID";
                    if (processID > 0) sql += " AND (PTM.ProcessID = @ProcessID OR PTM.ProcessTransactionNo LIKE 'BC%' OR PTM.Remark LIKE '%BUTTA%')";
                    if (processorID > 0) sql += " AND PTM.ToPartyID = @ProcessorID";
                    if (!string.IsNullOrEmpty(searchNo)) sql += " AND PTM.ProcessTransactionNo LIKE '%' + @SearchNo + '%'";
                    if (!string.IsNullOrEmpty(fromDate)) sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate)) sql += " AND PTM.TransactionDate <= @ToDate";

                    sql += " GROUP BY PTM.ProcessTransactionID, PTM.ProcessTransactionNo, PTM.TransactionDate, PTM.ProcessID, PM.ProcessName, PTM.TransactionType, PTM.FromPartyID, PTM.FromPartyName, PTM.ToPartyID, PTM.ToPartyName, PTM.Remark, PTM.CreatedDateTime ORDER BY PTM.ProcessTransactionID DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (processID > 0) cmd.Parameters.AddWithValue("@ProcessID", processID);
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (!string.IsNullOrEmpty(searchNo)) cmd.Parameters.AddWithValue("@SearchNo", searchNo);
                        if (!string.IsNullOrEmpty(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate).Date);
                        if (!string.IsNullOrEmpty(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate).Date.AddDays(1).AddTicks(-1));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                string txnType = dr["TransactionType"] != DBNull.Value ? dr["TransactionType"].ToString() : "ISSUE";
                                decimal issuePcs = dr["TotalIssuePCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalIssuePCS"]) : 0;
                                decimal receivePcs = dr["TotalReceivePCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalReceivePCS"]) : 0;
                                decimal shortPcs = dr["TotalShortPCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalShortPCS"]) : 0;

                                list.Add(new JobberToProcessListDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDate = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    TransactionType = txnType,
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    TotalItems = Convert.ToInt32(dr["TotalItems"]),
                                    TotalPCS = issuePcs,
                                    TotalIssuePCS = issuePcs,
                                    TotalReceivePCS = receivePcs,
                                    TotalShortPCS = shortPcs,
                                    PendingPCS = (issuePcs - receivePcs - shortPcs) < 0 ? 0 : (issuePcs - receivePcs - shortPcs),
                                    StatusText = dr["StatusText"].ToString(),
                                    CreatedDateTime = dr["CreatedDateTime"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDateTime"]).ToString("dd/MM/yyyy HH:mm") : ""
                                });
                            }
                        }
                    }
                }
                WriteJsonResponse(list);
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetJobberToProcessDetail(long processTransactionID)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        using (SqlCommand chkCmd = new SqlCommand("SELECT COUNT(1) FROM ProcessTransactionMaster WHERE ProcessTransactionID = @ID AND (FromPartyID = @JID OR ToPartyID = @JID)", con))
                        {
                            chkCmd.Parameters.AddWithValue("@ID", processTransactionID);
                            chkCmd.Parameters.AddWithValue("@JID", secContext.LoggedInJobberID);
                            int cnt = Convert.ToInt32(chkCmd.ExecuteScalar());
                            if (cnt == 0)
                            {
                                WriteJsonResponse(new GenericApiResponse(false, "Access Denied: You do not have permission to view this transaction."));
                                return;
                            }
                        }
                    }

                    JobberToProcessMasterHeaderDto master = null;
                    using (SqlCommand cmdM = new SqlCommand(@"
                    SELECT 
                        PTM.ProcessTransactionID, 
                        PTM.ProcessTransactionNo, 
                        CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr, 
                        PTM.TransactionDate, 
                        PTM.TransactionType, 
                        PTM.ProcessID,
                        ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                        PTM.FromPartyID AS JobberID, 
                        PTM.FromPartyName AS JobberName, 
                        PTM.ToPartyID AS ProcessorID,
                        PTM.ToPartyName AS ProcessorName,
                        PTM.Remark
                    FROM ProcessTransactionMaster PTM
                    LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID
                    WHERE PTM.ProcessTransactionID = @ID", con))
                    {
                        cmdM.Parameters.AddWithValue("@ID", processTransactionID);
                        using (SqlDataReader dr = cmdM.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                master = new JobberToProcessMasterHeaderDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDateStr = dr["TransactionDateStr"].ToString(),
                                    TransactionDate = dr["TransactionDate"] != DBNull.Value ? Convert.ToDateTime(dr["TransactionDate"]).ToString("yyyy-MM-dd") : "",
                                    TransactionType = dr["TransactionType"] != DBNull.Value ? dr["TransactionType"].ToString() : "ISSUE",
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobType = dr["Remark"].ToString(),
                                    Remark = dr["Remark"].ToString()
                                };
                            }
                        }
                    }

                    if (master == null)
                    {
                        WriteJsonResponse(new GenericApiResponse(false, "Challan record not found."));
                        return;
                    }

                    bool hasDCMD_Table = DoesTableExist(con, "DesignColorMatchingDetails");
                    bool hasDEFM_Table = DoesTableExist(con, "DesignEntryFormMaster");

                    string photoPathExpr = "''";
                    if (hasDCMD_Table && hasDEFM_Table) photoPathExpr = "ISNULL(NULLIF(DCMD.ColorMatchingPhoto, ''), ISNULL(DEFM.PhotoOfDesign, ''))";
                    else if (hasDCMD_Table) photoPathExpr = "ISNULL(DCMD.ColorMatchingPhoto, '')";
                    else if (hasDEFM_Table) photoPathExpr = "ISNULL(DEFM.PhotoOfDesign, '')";

                    string extraJoins = "";
                    if (hasDCMD_Table) extraJoins += " LEFT JOIN DesignColorMatchingDetails DCMD ON PTD.ColourMatchingID = DCMD.DesignColorMatchingDetailsID";
                    if (hasDEFM_Table) extraJoins += " LEFT JOIN DesignEntryFormMaster DEFM ON REPLACE(REPLACE(PTD.DesignNo, ' ', ''), '-', '') = REPLACE(REPLACE(DEFM.DesignNo, ' ', ''), '-', '')";

                    bool hasPOSD_Table = DoesTableExist(con, "ProductionOrderSubDetail");

                    string sql = string.Format(@"
                SELECT 
                    PTD.ProcessTransactionDetailID,
                    PTD.ProductionOrderID,
                    PTD.ProductionOrderDetailID,
                    PTD.ProductionOrderSubDetailID,
                    PTD.SlipNo,
                    PTD.DesignNo,
                    PTD.ColourMatchingID,
                    PTD.ColourMatchingName,
                    PTD.MachineNumber,
                    ISNULL({0}, ISNULL(POD.PCS, PTD.PCS)) AS OriginalOrderPCS,
                    PTD.IssuePCS AS CurrentIssuePCS,
                    ISNULL(PTD.ReceivePCS, 0) AS CurrentReceivePCS,
                    ISNULL(PTD.ShortPCS, 0) AS CurrentShortPCS,
                    {1} AS PhotoPath
                FROM ProcessTransactionDetail PTD
                LEFT JOIN ProductionOrderDetail POD ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID
                {2}
                {3}
                WHERE PTD.ProcessTransactionID = @ID AND ISNULL(PTD.IsActive, 1) = 1
                ORDER BY PTD.ProcessTransactionDetailID ASC",
                    hasPOSD_Table ? "POSD.PCS" : "NULL",
                    photoPathExpr,
                    hasPOSD_Table ? "LEFT JOIN ProductionOrderSubDetail POSD ON PTD.ProductionOrderSubDetailID = POSD.ProductionOrderSubDetailID" : "",
                    extraJoins);

                    var items = new List<JobberToProcessDetailItemDto>();
                    using (SqlCommand cmdD = new SqlCommand(sql, con))
                    {
                        cmdD.Parameters.AddWithValue("@ID", processTransactionID);
                        using (SqlDataReader dr = cmdD.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                string rawPhoto = dr["PhotoPath"] != DBNull.Value ? dr["PhotoPath"].ToString().Trim() : "";
                                string normalizedPhoto = "";
                                if (!string.IsNullOrEmpty(rawPhoto))
                                {
                                    string clean = rawPhoto.Replace("\\", "/");
                                    if (clean.StartsWith("~/")) clean = clean.Substring(2);
                                    if (!clean.StartsWith("/")) clean = "/" + clean;
                                    normalizedPhoto = clean;
                                }

                                decimal origOrderPcs = dr["OriginalOrderPCS"] != DBNull.Value ? Convert.ToDecimal(dr["OriginalOrderPCS"]) : 0;
                                decimal currentIssuePcs = dr["CurrentIssuePCS"] != DBNull.Value ? Convert.ToDecimal(dr["CurrentIssuePCS"]) : 0;
                                decimal currentReceivePcs = dr["CurrentReceivePCS"] != DBNull.Value ? Convert.ToDecimal(dr["CurrentReceivePCS"]) : 0;
                                decimal currentShortPcs = dr["CurrentShortPCS"] != DBNull.Value ? Convert.ToDecimal(dr["CurrentShortPCS"]) : 0;

                                items.Add(new JobberToProcessDetailItemDto
                                {
                                    ProcessTransactionDetailID = Convert.ToInt64(dr["ProcessTransactionDetailID"]),
                                    ProductionOrderID = dr["ProductionOrderID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderID"]) : 0,
                                    ProductionOrderDetailID = dr["ProductionOrderDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderDetailID"]) : 0,
                                    ProductionOrderSubDetailID = dr["ProductionOrderSubDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderSubDetailID"]) : 0,
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    ProductionPCS = origOrderPcs,
                                    PCS = origOrderPcs,
                                    JobberStockPCS = origOrderPcs,
                                    PendingIssuePCS = origOrderPcs,
                                    IssuePCS = currentIssuePcs,
                                    ReceivePCS = currentReceivePcs,
                                    ShortPCS = currentShortPcs,
                                    PendingPCS = (currentIssuePcs - currentReceivePcs - currentShortPcs) < 0 ? 0 : (currentIssuePcs - currentReceivePcs - currentShortPcs),
                                    AllottedPcs = currentIssuePcs,
                                    PhotoPath = normalizedPhoto
                                });
                            }
                        }
                    }

                    var response = new JobberToProcessDetailResponse
                    {
                        Status = true,
                        Master = master,
                        Details = items
                    };
                    WriteJsonResponse(response);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void UpdateJobberToProcess(long processTransactionID, long jobberID, int processID, long processorID, string jobType, string challanDate, string remark, List<ProcessItemInput> items)
        {
            try
            {
                if (processTransactionID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Invalid Process Transaction ID."));
                    return;
                }
                if (jobberID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Please select Job Worker / Jobber."));
                    return;
                }
                if (processID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Please select Process."));
                    return;
                }
                if (processorID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Please select Processor."));
                    return;
                }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    var secContext = GetLoggedInUserSecurityContext(con);

                    using (SqlCommand cmdChkRec = new SqlCommand(@"
                    SELECT ISNULL(SUM(ReceivePCS), 0) 
                    FROM ProcessTransactionDetail 
                    WHERE ProcessTransactionID = @ID AND ISNULL(IsActive, 1) = 1", con))
                    {
                        cmdChkRec.Parameters.AddWithValue("@ID", processTransactionID);
                        decimal recSum = Convert.ToDecimal(cmdChkRec.ExecuteScalar() ?? 0);
                        if (recSum > 0)
                        {
                            WriteJsonResponse(new GenericApiResponse(false, "Cannot update issue challan because items have already been received by the processor."));
                            return;
                        }
                    }

                    string processName = "PROCESS";
                    using (SqlCommand cmdP = new SqlCommand("SELECT ProcessName FROM ProcessMaster WHERE ProcessID = @PID", con))
                    {
                        cmdP.Parameters.AddWithValue("@PID", processID);
                        object pVal = cmdP.ExecuteScalar();
                        if (pVal != null && pVal != DBNull.Value) processName = pVal.ToString().Trim();
                    }

                    string jobberName = "";
                    using (SqlCommand cmdJ = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @JobberID", con))
                    {
                        cmdJ.Parameters.AddWithValue("@JobberID", jobberID);
                        object val = cmdJ.ExecuteScalar();
                        if (val != null && val != DBNull.Value) jobberName = val.ToString();
                    }

                    string processorName = "";
                    using (SqlCommand cmdP = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @ProcessorID", con))
                    {
                        cmdP.Parameters.AddWithValue("@ProcessorID", processorID);
                        object val = cmdP.ExecuteScalar();
                        if (val != null && val != DBNull.Value) processorName = val.ToString();
                    }
                    if (string.IsNullOrEmpty(processorName)) processorName = "PROCESSOR";

                    DateTime dateTxn = string.IsNullOrEmpty(challanDate) ? DateTime.Now : DateTime.Parse(challanDate);

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            string sqlMaster = @"
                        UPDATE ProcessTransactionMaster 
                        SET 
                            ProcessID = @ProcessID,
                            TransactionDate = @TransactionDate,
                            FromPartyID = @FromPartyID,
                            FromPartyName = @FromPartyName,
                            ToPartyID = @ToPartyID,
                            ToPartyName = @ToPartyName,
                            Remark = @Remark,
                            UpdatedDateTime = GETDATE()
                        WHERE ProcessTransactionID = @ID";

                            using (SqlCommand cmdM = new SqlCommand(sqlMaster, con, trans))
                            {
                                cmdM.Parameters.AddWithValue("@ProcessID", processID);
                                cmdM.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                cmdM.Parameters.AddWithValue("@FromPartyID", jobberID);
                                cmdM.Parameters.AddWithValue("@FromPartyName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                cmdM.Parameters.AddWithValue("@ToPartyID", processorID);
                                cmdM.Parameters.AddWithValue("@ToPartyName", processorName);
                                cmdM.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(remark) ? (string.IsNullOrEmpty(jobType) ? processName + " ISSUE" : jobType) : remark);
                                cmdM.Parameters.AddWithValue("@ID", processTransactionID);
                                cmdM.ExecuteNonQuery();
                            }

                            using (SqlCommand cmdDelH = new SqlCommand(@"
                            DELETE FROM ProcessTransactionHistory 
                            WHERE ProcessTransactionDetailID IN (
                                SELECT ProcessTransactionDetailID FROM ProcessTransactionDetail WHERE ProcessTransactionID = @ID
                            )", con, trans))
                            {
                                cmdDelH.Parameters.AddWithValue("@ID", processTransactionID);
                                cmdDelH.ExecuteNonQuery();
                            }

                            using (SqlCommand cmdDelD = new SqlCommand("DELETE FROM ProcessTransactionDetail WHERE ProcessTransactionID = @ID", con, trans))
                            {
                                cmdDelD.Parameters.AddWithValue("@ID", processTransactionID);
                                cmdDelD.ExecuteNonQuery();
                            }

                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    if (item.PCS <= 0) continue;

                                    string slipNo = item.SlipNo ?? "";
                                    string designNo = "";
                                    string colourMatchingName = "";
                                    int colourMatchingID = 0;
                                    int machineNumber = 1;
                                    long saleOrderID = 0;
                                    long saleOrderDetailID = 0;
                                    long saleOrderSubDetailID = 0;
                                    long barcode = 0;

                                    if (item.ProductionOrderDetailID > 0)
                                    {
                                        try
                                        {
                                            using (SqlCommand cmdI = new SqlCommand(@"
                                            SELECT TOP 1
                                                ISNULL(POD.DesignNo, '') AS DesignNo,
                                                ISNULL(POD.ColourMatchingName, '') AS ColourMatchingName,
                                                ISNULL(POD.ColourMatchingID, 0) AS ColourMatchingID,
                                                ISNULL(POSD.MachineNumber, 1) AS MachineNumber,
                                                ISNULL(POD.SaleOrderID, 0) AS SaleOrderID,
                                                ISNULL(POD.SaleOrderDetailID, 0) AS SaleOrderDetailID,
                                                ISNULL(POSD.SaleOrderSubDetailID, 0) AS SaleOrderSubDetailID,
                                                ISNULL(POSD.Barcode, 0) AS Barcode
                                            FROM ProductionOrderDetail POD
                                            LEFT JOIN ProductionOrderSubDetail POSD ON POD.ProductionOrderDetailID = POSD.ProductionOrderDetailID
                                            WHERE POD.ProductionOrderDetailID = @PODID", con, trans))
                                            {
                                                cmdI.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                                                using (SqlDataReader dr = cmdI.ExecuteReader())
                                                {
                                                    if (dr.Read())
                                                    {
                                                        designNo = dr["DesignNo"].ToString();
                                                        colourMatchingName = dr["ColourMatchingName"].ToString();
                                                        colourMatchingID = Convert.ToInt32(dr["ColourMatchingID"]);
                                                        machineNumber = Convert.ToInt32(dr["MachineNumber"]);
                                                        saleOrderID = Convert.ToInt64(dr["SaleOrderID"]);
                                                        saleOrderDetailID = Convert.ToInt64(dr["SaleOrderDetailID"]);
                                                        saleOrderSubDetailID = Convert.ToInt64(dr["SaleOrderSubDetailID"]);
                                                        barcode = Convert.ToInt64(dr["Barcode"]);
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }

                                    long companyIdVal = secContext.CompanyId;
                                    long userIdVal = secContext.UserAccountId;

                                    long detailID = 0;
                                    string sqlDetail = @"
                                INSERT INTO ProcessTransactionDetail 
                                (
                                    ProcessTransactionID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                    SaleOrderID, SaleOrderDetailID, SaleOrderSubDetailID, SlipNo, Barcode,
                                    DesignNo, ColourMatchingID, ColourMatchingName, MachineNumber, PCS, IssuePCS, ReceivePCS, PendingPCS, ShortPCS, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                                )
                                VALUES 
                                (
                                    @MasterID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                    @SaleOrderID, @SaleOrderDetailID, @SaleOrderSubDetailID, @SlipNo, @Barcode,
                                    @DesignNo, @ColourMatchingID, @ColourMatchingName, @MachineNumber, @PCS, @IssuePCS, 0, @IssuePCS, 0, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                                );
                                SELECT SCOPE_IDENTITY();";

                                    using (SqlCommand cmdD = new SqlCommand(sqlDetail, con, trans))
                                    {
                                        cmdD.Parameters.AddWithValue("@MasterID", processTransactionID);
                                        cmdD.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                        cmdD.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                        cmdD.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                        cmdD.Parameters.AddWithValue("@SaleOrderID", saleOrderID);
                                        cmdD.Parameters.AddWithValue("@SaleOrderDetailID", saleOrderDetailID);
                                        cmdD.Parameters.AddWithValue("@SaleOrderSubDetailID", saleOrderSubDetailID);
                                        cmdD.Parameters.AddWithValue("@SlipNo", slipNo);
                                        cmdD.Parameters.AddWithValue("@Barcode", barcode);
                                        cmdD.Parameters.AddWithValue("@DesignNo", designNo);
                                        cmdD.Parameters.AddWithValue("@ColourMatchingID", colourMatchingID);
                                        cmdD.Parameters.AddWithValue("@ColourMatchingName", colourMatchingName);
                                        cmdD.Parameters.AddWithValue("@MachineNumber", machineNumber);
                                        cmdD.Parameters.AddWithValue("@PCS", item.PCS);
                                        cmdD.Parameters.AddWithValue("@IssuePCS", item.PCS);
                                        cmdD.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(jobType) ? processName + " ISSUE" : jobType);
                                        cmdD.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                        cmdD.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                        detailID = Convert.ToInt64(cmdD.ExecuteScalar());
                                    }

                                    string sqlHistory = @"
                                INSERT INTO ProcessTransactionHistory 
                                (
                                    ProcessTransactionDetailID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                    ProcessID, TransactionType, TransactionDate, Qty, ShortQty, MachineNumber, SlipNo, Barcode,
                                    JobberID, JobberName, FromPartyID, FromPartyName, ToPartyID, ToPartyName, Remark, CompanyId, CreatedByUserID, CreatedDateTime
                                )
                                VALUES 
                                (
                                    @DetailID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                    @ProcessID, 'ISSUE', @TransactionDate, @Qty, 0, @MachineNumber, @SlipNo, @Barcode,
                                    @JobberID, @JobberName, @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, @Remark, @CompanyId, @CreatedByUserID, GETDATE()
                                );";

                                    using (SqlCommand cmdH = new SqlCommand(sqlHistory, con, trans))
                                    {
                                        cmdH.Parameters.AddWithValue("@DetailID", detailID);
                                        cmdH.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                        cmdH.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                        cmdH.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                        cmdH.Parameters.AddWithValue("@ProcessID", processID);
                                        cmdH.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                        cmdH.Parameters.AddWithValue("@Qty", item.PCS);
                                        cmdH.Parameters.AddWithValue("@MachineNumber", machineNumber);
                                        cmdH.Parameters.AddWithValue("@SlipNo", slipNo);
                                        cmdH.Parameters.AddWithValue("@Barcode", barcode);
                                        cmdH.Parameters.AddWithValue("@JobberID", jobberID);
                                        cmdH.Parameters.AddWithValue("@JobberName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                        cmdH.Parameters.AddWithValue("@FromPartyID", jobberID);
                                        cmdH.Parameters.AddWithValue("@FromPartyName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                        cmdH.Parameters.AddWithValue("@ToPartyID", processorID);
                                        cmdH.Parameters.AddWithValue("@ToPartyName", processorName);
                                        cmdH.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(jobType) ? processName + " ISSUE" : jobType);
                                        cmdH.ExecuteNonQuery();
                                    }
                                }
                            }

                            trans.Commit();
                            WriteJsonResponse(new GenericApiResponse(true, "Challan updated successfully."));
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, "Error updating challan: " + ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void DeleteJobberToProcess(long processTransactionID)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    var secContext = GetLoggedInUserSecurityContext(con);

                    using (SqlCommand cmdChkRec = new SqlCommand(@"
                    SELECT ISNULL(SUM(ReceivePCS), 0) 
                    FROM ProcessTransactionDetail 
                    WHERE ProcessTransactionID = @ID AND ISNULL(IsActive, 1) = 1", con))
                    {
                        cmdChkRec.Parameters.AddWithValue("@ID", processTransactionID);
                        decimal recSum = Convert.ToDecimal(cmdChkRec.ExecuteScalar() ?? 0);
                        if (recSum > 0)
                        {
                            WriteJsonResponse(new GenericApiResponse(false, "Cannot delete issue challan because items have already been received by the processor."));
                            return;
                        }
                    }

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmdD = new SqlCommand("UPDATE ProcessTransactionDetail SET IsActive = 0, UpdatedDateTime = GETDATE() WHERE ProcessTransactionID = @ID", con, trans))
                            {
                                cmdD.Parameters.AddWithValue("@ID", processTransactionID);
                                cmdD.ExecuteNonQuery();
                            }

                            using (SqlCommand cmdM = new SqlCommand("UPDATE ProcessTransactionMaster SET IsActive = 0, UpdatedDateTime = GETDATE() WHERE ProcessTransactionID = @ID", con, trans))
                            {
                                cmdM.Parameters.AddWithValue("@ID", processTransactionID);
                                cmdM.ExecuteNonQuery();
                            }

                            long cancelCompanyId = secContext.CompanyId;
                            long cancelUserId = secContext.UserAccountId;
                            using (SqlCommand cmdH = new SqlCommand(@"
                                    INSERT INTO ProcessTransactionHistory 
                                    (
                                        ProcessTransactionDetailID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID, 
                                        ProcessID, TransactionType, TransactionDate, Qty, ShortQty, MachineNumber, SlipNo, Barcode, 
                                        JobberID, JobberName, FromPartyID, FromPartyName, ToPartyID, ToPartyName, Remark, CompanyId, CreatedByUserID, CreatedDateTime
                                    )
                                    SELECT 
                                        PTD.ProcessTransactionDetailID, PTD.ProductionOrderID, PTD.ProductionOrderDetailID, PTD.ProductionOrderSubDetailID,
                                        PTM.ProcessID, 'CANCELLED', GETDATE(), PTD.IssuePCS, 0, PTD.MachineNumber, PTD.SlipNo, PTD.Barcode,
                                        PTM.FromPartyID, PTM.FromPartyName, PTM.FromPartyID, PTM.FromPartyName, PTM.ToPartyID, PTM.ToPartyName, 
                                        'CHALLAN CANCELLED', @CompanyId, @CreatedByUserID, GETDATE()
                                    FROM ProcessTransactionDetail PTD
                                    INNER JOIN ProcessTransactionMaster PTM ON PTD.ProcessTransactionID = PTM.ProcessTransactionID
                                    WHERE PTM.ProcessTransactionID = @ID", con, trans))
                            {
                                cmdH.Parameters.AddWithValue("@CompanyId", cancelCompanyId);
                                cmdH.Parameters.AddWithValue("@CreatedByUserID", cancelUserId);
                                cmdH.Parameters.AddWithValue("@ID", processTransactionID);
                                cmdH.ExecuteNonQuery();
                            }

                            AuditLogHelper.Log(
                                module: "Jobber To Process",
                                action: "DELETE",
                                tableName: "ProcessTransactionMaster",
                                recordId: processTransactionID.ToString(),
                                description: $"Cancelled Process Issue Challan ID #{processTransactionID}",
                                existingConn: con,
                                existingTran: trans
                            );

                            trans.Commit();
                            WriteJsonResponse(new GenericApiResponse(true, "Challan cancelled successfully."));
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, "Error cancelling challan: " + ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetProcessTransactionHistoryLog(string slipNo, long productionOrderDetailID)
        {
            try
            {
                var list = new List<ProcessHistoryItemDto>();
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string sql = @"
                SELECT 
                    PTH.ProcessHistoryID,
                    PTH.ProcessTransactionDetailID,
                    CONVERT(VARCHAR(10), PTH.TransactionDate, 103) + ' ' + CONVERT(VARCHAR(5), PTH.CreatedDateTime, 108) AS DateStr,
                    PTH.TransactionType,
                    ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                    ISNULL(PTH.FromPartyName, ISNULL(PTH.JobberName, 'JOBBER')) AS FromPartyName,
                    ISNULL(PTH.ToPartyName, 'PROCESSOR') AS ToPartyName,
                    PTH.SlipNo,
                    ISNULL(PTH.Qty, 0) AS Qty,
                    ISNULL(PTH.ShortQty, 0) AS ShortQty,
                    PTH.MachineNumber,
                    PTH.Remark,
                    PTH.CreatedByUserID,
                    PTH.ReferenceTransactionID
                FROM ProcessTransactionHistory PTH
                LEFT JOIN ProcessMaster PM ON PTH.ProcessID = PM.ProcessID
                WHERE 1 = 1";

                    if (!string.IsNullOrEmpty(slipNo)) sql += " AND PTH.SlipNo = @SlipNo";
                    if (productionOrderDetailID > 0) sql += " AND PTH.ProductionOrderDetailID = @PODID";

                    sql += " ORDER BY PTH.ProcessHistoryID ASC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (!string.IsNullOrEmpty(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (productionOrderDetailID > 0) cmd.Parameters.AddWithValue("@PODID", productionOrderDetailID);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new ProcessHistoryItemDto
                                {
                                    ProcessHistoryID = Convert.ToInt64(dr["ProcessHistoryID"]),
                                    ProcessTransactionDetailID = dr["ProcessTransactionDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessTransactionDetailID"]) : 0,
                                    DateStr = dr["DateStr"].ToString(),
                                    TransactionType = dr["TransactionType"].ToString(),
                                    ProcessName = dr["ProcessName"].ToString(),
                                    FromPartyName = dr["FromPartyName"].ToString(),
                                    ToPartyName = dr["ToPartyName"].ToString(),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    Qty = Convert.ToDecimal(dr["Qty"]),
                                    ShortQty = Convert.ToDecimal(dr["ShortQty"]),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    Remark = dr["Remark"].ToString(),
                                    ReferenceTransactionID = dr["ReferenceTransactionID"] != DBNull.Value ? Convert.ToInt64(dr["ReferenceTransactionID"]) : 0
                                });
                            }
                        }
                    }
                }
                var response = new ProcessTransactionHistoryResponse
                {
                    Status = true,
                    History = list
                };
                WriteJsonResponse(response);
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }
        #endregion

        #region PHASE 4: PROCESS RECEIVE & PROCESS STOCK
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetIssueChallansForProcessReceive(string fromDate, string toDate, long jobberID, int processID, long processorID, string searchNo, string soNumber, string slipNo, string designNo, int machineNo)
        {
            try
            {
                var list = new List<IssueChallanForReceiveDto>();
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    string sql = @"
                SELECT 
                    PTM.ProcessTransactionID,
                    PTM.ProcessTransactionNo,
                    CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr,
                    PTM.TransactionDate,
                    PTM.ProcessID,
                    ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                    PTM.FromPartyID AS JobberID,
                    PTM.FromPartyName AS JobberName,
                    PTM.ToPartyID AS ProcessorID,
                    PTM.ToPartyName AS ProcessorName,
                    PTM.Remark AS JobType,
                    MIN(ISNULL(PTD.SlipNo, '')) AS SlipNo,
                    MIN(ISNULL(PTD.DesignNo, '')) AS DesignNo,
                    ISNULL(SUM(PTD.IssuePCS), 0) AS TotalIssuePCS,
                    ISNULL(SUM(PTD.ReceivePCS), 0) AS TotalReceivePCS,
                    ISNULL(SUM(PTD.ShortPCS), 0) AS TotalShortPCS,
                    (ISNULL(SUM(PTD.IssuePCS), 0) - ISNULL(SUM(PTD.ReceivePCS), 0) - ISNULL(SUM(PTD.ShortPCS), 0)) AS PendingReceivePCS
                FROM ProcessTransactionMaster PTM
                LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID
                INNER JOIN ProcessTransactionDetail PTD ON PTM.ProcessTransactionID = PTD.ProcessTransactionID AND ISNULL(PTD.IsActive, 1) = 1
                LEFT JOIN ProductionOrderDetail POD ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID
                WHERE PTM.TransactionType = 'ISSUE'
                  AND ISNULL(PTM.IsActive, 1) = 1";

                    if (jobberID > 0) sql += " AND PTM.FromPartyID = @JobberID";
                    if (processID > 0) sql += " AND (PTM.ProcessID = @ProcessID OR PTM.ProcessTransactionNo LIKE 'BC%' OR PTM.Remark LIKE '%BUTTA%')";
                    if (processorID > 0) sql += " AND PTM.ToPartyID = @ProcessorID";
                    if (!string.IsNullOrEmpty(searchNo)) sql += " AND PTM.ProcessTransactionNo LIKE '%' + @SearchNo + '%'";
                    if (!string.IsNullOrEmpty(soNumber)) sql += " AND (POD.SalesOrderNo LIKE '%' + @SONumber + '%')";
                    if (!string.IsNullOrEmpty(slipNo)) sql += " AND (PTD.SlipNo LIKE '%' + @SlipNo + '%')";
                    if (!string.IsNullOrEmpty(designNo)) sql += " AND (PTD.DesignNo LIKE '%' + @DesignNo + '%')";
                    if (machineNo > 0) sql += " AND (PTD.MachineNumber = @MachineNo)";
                    if (!string.IsNullOrEmpty(fromDate)) sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate)) sql += " AND PTM.TransactionDate <= @ToDate";

                    sql += @" GROUP BY PTM.ProcessTransactionID, PTM.ProcessTransactionNo, PTM.TransactionDate, PTM.ProcessID, PM.ProcessName, PTM.FromPartyID, PTM.FromPartyName, PTM.ToPartyID, PTM.ToPartyName, PTM.Remark
                HAVING (ISNULL(SUM(PTD.IssuePCS), 0) - ISNULL(SUM(PTD.ReceivePCS), 0) - ISNULL(SUM(PTD.ShortPCS), 0)) > 0
                ORDER BY PTM.ProcessTransactionID DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (processID > 0) cmd.Parameters.AddWithValue("@ProcessID", processID);
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (!string.IsNullOrEmpty(searchNo)) cmd.Parameters.AddWithValue("@SearchNo", searchNo);
                        if (!string.IsNullOrEmpty(soNumber)) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrEmpty(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrEmpty(designNo)) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNo > 0) cmd.Parameters.AddWithValue("@MachineNo", machineNo);
                        if (!string.IsNullOrEmpty(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate).Date);
                        if (!string.IsNullOrEmpty(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate).Date.AddDays(1).AddTicks(-1));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                decimal issuePcs = Convert.ToDecimal(dr["TotalIssuePCS"]);
                                decimal recPcs = Convert.ToDecimal(dr["TotalReceivePCS"]);
                                decimal shortPcs = Convert.ToDecimal(dr["TotalShortPCS"]);
                                decimal pendPcs = Convert.ToDecimal(dr["PendingReceivePCS"]);

                                list.Add(new IssueChallanForReceiveDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDate = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    TotalIssuePCS = issuePcs,
                                    TotalReceivePCS = recPcs,
                                    TotalShortPCS = shortPcs,
                                    PendingReceivePCS = pendPcs
                                });
                            }
                        }
                    }
                }
                WriteJsonResponse(list);
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetIssueChallanDetailForProcessReceive(long issueProcessTransactionID)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        using (SqlCommand chkCmd = new SqlCommand("SELECT COUNT(1) FROM ProcessTransactionMaster WHERE ProcessTransactionID = @ID AND (FromPartyID = @JID OR ToPartyID = @JID)", con))
                        {
                            chkCmd.Parameters.AddWithValue("@ID", issueProcessTransactionID);
                            chkCmd.Parameters.AddWithValue("@JID", secContext.LoggedInJobberID);
                            int cnt = Convert.ToInt32(chkCmd.ExecuteScalar());
                            if (cnt == 0)
                            {
                                WriteJsonResponse(new GenericApiResponse(false, "Access Denied: You do not have permission to view this transaction."));
                                return;
                            }
                        }
                    }

                    JobberToProcessMasterHeaderDto master = null;
                    using (SqlCommand cmdM = new SqlCommand(@"
                    SELECT 
                        PTM.ProcessTransactionID, 
                        PTM.ProcessTransactionNo, 
                        CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr, 
                        PTM.ProcessID,
                        ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                        PTM.FromPartyID AS JobberID, 
                        PTM.FromPartyName AS JobberName, 
                        PTM.ToPartyID AS ProcessorID, 
                        PTM.ToPartyName AS ProcessorName, 
                        PTM.Remark 
                    FROM ProcessTransactionMaster PTM 
                    LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID 
                    WHERE PTM.ProcessTransactionID = @ID AND ISNULL(PTM.IsActive, 1) = 1", con))
                    {
                        cmdM.Parameters.AddWithValue("@ID", issueProcessTransactionID);
                        using (SqlDataReader dr = cmdM.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                master = new JobberToProcessMasterHeaderDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDateStr = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobType = dr["Remark"].ToString(),
                                    Remark = dr["Remark"].ToString()
                                };
                            }
                        }
                    }

                    if (master == null)
                    {
                        WriteJsonResponse(new GenericApiResponse(false, "Issue Challan not found or inactive."));
                        return;
                    }

                    int procId = master.ProcessID;
                    string procName = master.ProcessName;

                    string nextReceiveNo = "";
                    using (SqlCommand cmdSeq = new SqlCommand("SELECT ISNULL(MAX(ProcessTransactionID), 0) + 1 FROM ProcessTransactionMaster WHERE ProcessID = @PID AND TransactionType = 'RECEIVE'", con))
                    {
                        cmdSeq.Parameters.AddWithValue("@PID", procId);
                        long nextSeq = Convert.ToInt64(cmdSeq.ExecuteScalar());
                        string prefix = procName.StartsWith("BUTTA") ? "BCR" : (procName.StartsWith("BORDER") ? "BRR" : "PCR");
                        nextReceiveNo = prefix + nextSeq.ToString("D4");
                    }

                    bool hasDCMD_Table = DoesTableExist(con, "DesignColorMatchingDetails");
                    bool hasDEFM_Table = DoesTableExist(con, "DesignEntryFormMaster");

                    string photoPathExpr = "''";
                    if (hasDCMD_Table && hasDEFM_Table) photoPathExpr = "ISNULL(NULLIF(DCMD.ColorMatchingPhoto, ''), ISNULL(DEFM.PhotoOfDesign, ''))";
                    else if (hasDCMD_Table) photoPathExpr = "ISNULL(DCMD.ColorMatchingPhoto, '')";
                    else if (hasDEFM_Table) photoPathExpr = "ISNULL(DEFM.PhotoOfDesign, '')";

                    string extraJoins = "";
                    if (hasDCMD_Table) extraJoins += " LEFT JOIN DesignColorMatchingDetails DCMD ON PTD.ColourMatchingID = DCMD.DesignColorMatchingDetailsID";
                    if (hasDEFM_Table) extraJoins += " LEFT JOIN DesignEntryFormMaster DEFM ON REPLACE(REPLACE(PTD.DesignNo, ' ', ''), '-', '') = REPLACE(REPLACE(DEFM.DesignNo, ' ', ''), '-', '')";

                    string sql = string.Format(@"
                SELECT 
                    PTD.ProcessTransactionDetailID,
                    PTD.ProductionOrderID,
                    PTD.ProductionOrderDetailID,
                    PTD.ProductionOrderSubDetailID,
                    PTD.SlipNo,
                    PTD.DesignNo,
                    PTD.ColourMatchingID,
                    PTD.ColourMatchingName,
                    PTD.MachineNumber,
                    PTD.IssuePCS,
                    ISNULL(PTD.ReceivePCS, 0) AS AlreadyReceivePCS,
                    ISNULL(PTD.ShortPCS, 0) AS AlreadyShortPCS,
                    (PTD.IssuePCS - ISNULL(PTD.ReceivePCS, 0) - ISNULL(PTD.ShortPCS, 0)) AS PendingReceivePCS,
                    PTD.Remark AS JobType,
                    {0} AS PhotoPath
                FROM ProcessTransactionDetail PTD
                {1}
                WHERE PTD.ProcessTransactionID = @ID 
                  AND ISNULL(PTD.IsActive, 1) = 1 
                  AND (PTD.IssuePCS - ISNULL(PTD.ReceivePCS, 0) - ISNULL(PTD.ShortPCS, 0)) > 0
                ORDER BY PTD.ProcessTransactionDetailID ASC", photoPathExpr, extraJoins);

                    var items = new List<IssueChallanDetailForReceiveItemDto>();
                    using (SqlCommand cmdD = new SqlCommand(sql, con))
                    {
                        cmdD.Parameters.AddWithValue("@ID", issueProcessTransactionID);
                        using (SqlDataReader dr = cmdD.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                string rawPhoto = dr["PhotoPath"] != DBNull.Value ? dr["PhotoPath"].ToString().Trim() : "";
                                string normalizedPhoto = "";
                                if (!string.IsNullOrEmpty(rawPhoto))
                                {
                                    string clean = rawPhoto.Replace("\\", "/");
                                    if (clean.StartsWith("~/")) clean = clean.Substring(2);
                                    if (!clean.StartsWith("/")) clean = "/" + clean;
                                    normalizedPhoto = clean;
                                }

                                decimal issuePcs = Convert.ToDecimal(dr["IssuePCS"]);
                                decimal alreadyReceivePcs = Convert.ToDecimal(dr["AlreadyReceivePCS"]);
                                decimal pendingReceivePcs = Convert.ToDecimal(dr["PendingReceivePCS"]);

                                items.Add(new IssueChallanDetailForReceiveItemDto
                                {
                                    IssueDetailID = Convert.ToInt64(dr["ProcessTransactionDetailID"]),
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                    ProductionOrderSubDetailID = Convert.ToInt64(dr["ProductionOrderSubDetailID"]),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    IssuePCS = issuePcs,
                                    AlreadyReceivePCS = alreadyReceivePcs,
                                    PendingReceivePCS = pendingReceivePcs,
                                    ReceivePCS = pendingReceivePcs,
                                    ShortPCS = 0,
                                    JobType = dr["JobType"].ToString(),
                                    PhotoPath = normalizedPhoto
                                });
                            }
                        }
                    }

                    var response = new IssueChallanDetailForReceiveResponse
                    {
                        Status = true,
                        Master = master,
                        NextReceiveNo = nextReceiveNo,
                        Details = items
                    };
                    WriteJsonResponse(response);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveProcessReceive(long issueProcessTransactionID, long jobberID, int processID, long processorID, string receiveDate, string remark, List<ReceiveItemInput> items)
        {
            try
            {
                if (issueProcessTransactionID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Invalid Issue Challan Reference."));
                    return;
                }
                if (items == null || items.Count == 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "No item slips selected for receive."));
                    return;
                }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;

                        using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(1) FROM ProcessTransactionMaster WHERE ProcessTransactionID = @ID AND (FromPartyID = @JID OR ToPartyID = @JID)", con))
                        {
                            cmdCheck.Parameters.AddWithValue("@ID", issueProcessTransactionID);
                            cmdCheck.Parameters.AddWithValue("@JID", secContext.LoggedInJobberID);
                            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) == 0)
                            {
                                WriteJsonResponse(new GenericApiResponse(false, "Access Denied: You cannot process a receive for another Jobber's issue challan."));
                                return;
                            }
                        }
                    }

                    string processName = "PROCESS";
                    if (processID > 0)
                    {
                        using (SqlCommand cmdP = new SqlCommand("SELECT ProcessName FROM ProcessMaster WHERE ProcessID = @PID", con))
                        {
                            cmdP.Parameters.AddWithValue("@PID", processID);
                            object pVal = cmdP.ExecuteScalar();
                            if (pVal != null && pVal != DBNull.Value) processName = pVal.ToString().Trim();
                        }
                    }

                    string jobberName = "";
                    using (SqlCommand cmdJ = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @JobberID", con))
                    {
                        cmdJ.Parameters.AddWithValue("@JobberID", jobberID);
                        object val = cmdJ.ExecuteScalar();
                        if (val != null && val != DBNull.Value) jobberName = val.ToString();
                    }

                    string processorName = "";
                    using (SqlCommand cmdP = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @ProcessorID", con))
                    {
                        cmdP.Parameters.AddWithValue("@ProcessorID", processorID);
                        object val = cmdP.ExecuteScalar();
                        if (val != null && val != DBNull.Value) processorName = val.ToString();
                    }

                    foreach (var item in items)
                    {
                        if (item.ReceivePCS <= 0 && item.ShortPCS <= 0) continue;
                        if (item.ReceivePCS < 0 || item.ShortPCS < 0)
                        {
                            WriteJsonResponse(new GenericApiResponse(false, string.Format("Receive PCS and Short PCS cannot be negative for Slip '{0}'.", item.SlipNo)));
                            return;
                        }

                        decimal pendingReceive = 0;
                        using (SqlCommand cmdCheck = new SqlCommand(@"
                        SELECT (IssuePCS - ISNULL(ReceivePCS, 0) - ISNULL(ShortPCS, 0)) AS PendingQty 
                        FROM ProcessTransactionDetail 
                        WHERE ProcessTransactionDetailID = @DetailID AND ISNULL(IsActive, 1) = 1", con))
                        {
                            cmdCheck.Parameters.AddWithValue("@DetailID", item.IssueDetailID);
                            object val = cmdCheck.ExecuteScalar();
                            if (val != null && val != DBNull.Value) pendingReceive = Convert.ToDecimal(val);
                        }

                        if ((item.ReceivePCS + item.ShortPCS) > pendingReceive)
                        {
                            WriteJsonResponse(new GenericApiResponse(false, string.Format("Receive PCS ({0}) + Short PCS ({1}) cannot exceed Pending Receive PCS ({2}) for Slip '{3}'.", item.ReceivePCS, item.ShortPCS, pendingReceive, item.SlipNo)));
                            return;
                        }
                    }

                    string receiveTxnNo = "";
                    using (SqlCommand cmdSeq = new SqlCommand("SELECT ISNULL(MAX(ProcessTransactionID), 0) + 1 FROM ProcessTransactionMaster WHERE ProcessID = @PID AND TransactionType = 'RECEIVE'", con))
                    {
                        cmdSeq.Parameters.AddWithValue("@PID", processID);
                        long nextSeq = Convert.ToInt64(cmdSeq.ExecuteScalar());
                        string prefix = processName.StartsWith("BUTTA") ? "BCR" : (processName.StartsWith("BORDER") ? "BRR" : "PCR");
                        receiveTxnNo = prefix + nextSeq.ToString("D4");
                    }

                    DateTime dateTxn = string.IsNullOrEmpty(receiveDate) ? DateTime.Now : DateTime.Parse(receiveDate);

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            long companyIdVal = secContext.CompanyId;
                            long userIdVal = secContext.UserAccountId;

                            long masterID = 0;
                            string sqlMaster = @"
                        INSERT INTO ProcessTransactionMaster 
                        (
                            ProcessTransactionNo, ProcessID, TransactionType, TransactionDate,
                            FromPartyID, FromPartyName, ToPartyID, ToPartyName, 
                            ReferenceTransactionID, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                        )
                        VALUES 
                        (
                            @TxnNo, @ProcessID, 'RECEIVE', @TransactionDate,
                            @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, 
                            @RefTxnID, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                        );
                        SELECT SCOPE_IDENTITY();";

                            using (SqlCommand cmdM = new SqlCommand(sqlMaster, con, trans))
                            {
                                cmdM.Parameters.AddWithValue("@TxnNo", receiveTxnNo);
                                cmdM.Parameters.AddWithValue("@ProcessID", processID);
                                cmdM.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                cmdM.Parameters.AddWithValue("@FromPartyID", processorID);
                                cmdM.Parameters.AddWithValue("@FromPartyName", string.IsNullOrEmpty(processorName) ? "PROCESSOR" : processorName);
                                cmdM.Parameters.AddWithValue("@ToPartyID", jobberID);
                                cmdM.Parameters.AddWithValue("@ToPartyName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                cmdM.Parameters.AddWithValue("@RefTxnID", issueProcessTransactionID);
                                cmdM.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(remark) ? processName + " RECEIVE" : remark);
                                cmdM.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                cmdM.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                masterID = Convert.ToInt64(cmdM.ExecuteScalar());
                            }

                            foreach (var item in items)
                            {
                                if (item.ReceivePCS <= 0 && item.ShortPCS <= 0) continue;

                                string slipNo = item.SlipNo ?? "";
                                string designNo = "";
                                string colourMatchingName = "";
                                int colourMatchingID = 0;

                                if (item.ProductionOrderDetailID > 0)
                                {
                                    try
                                    {
                                        using (SqlCommand cmdI = new SqlCommand("SELECT TOP 1 DesignNo, ColourMatchingName, ColourMatchingID FROM ProductionOrderDetail WHERE ProductionOrderDetailID = @PODID", con, trans))
                                        {
                                            cmdI.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                                            using (SqlDataReader dr = cmdI.ExecuteReader())
                                            {
                                                if (dr.Read())
                                                {
                                                    designNo = dr["DesignNo"].ToString();
                                                    colourMatchingName = dr["ColourMatchingName"].ToString();
                                                    colourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt32(dr["ColourMatchingID"]) : 0;
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                string sqlDetail = @"
                            INSERT INTO ProcessTransactionDetail 
                            (
                                ProcessTransactionID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                SlipNo, DesignNo, ColourMatchingID, ColourMatchingName, MachineNumber, PCS, IssuePCS, ReceivePCS, ShortPCS, PendingPCS, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                            )
                            VALUES 
                            (
                                @MasterID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                @SlipNo, @DesignNo, @ColourMatchingID, @ColourMatchingName, 1, @IssuePCS, 0, @ReceivePCS, @ShortPCS, 0, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                            );
                            SELECT SCOPE_IDENTITY();";

                                long detailID = 0;
                                using (SqlCommand cmdD = new SqlCommand(sqlDetail, con, trans))
                                {
                                    cmdD.Parameters.AddWithValue("@MasterID", masterID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdD.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdD.Parameters.AddWithValue("@DesignNo", designNo);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingID", colourMatchingID);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingName", colourMatchingName);
                                    cmdD.Parameters.AddWithValue("@IssuePCS", item.IssuePCS);
                                    cmdD.Parameters.AddWithValue("@ReceivePCS", item.ReceivePCS);
                                    cmdD.Parameters.AddWithValue("@ShortPCS", item.ShortPCS);
                                    cmdD.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(remark) ? processName + " RECEIVE" : remark);
                                    cmdD.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdD.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    detailID = Convert.ToInt64(cmdD.ExecuteScalar());
                                }

                                string sqlHistory = @"
                            INSERT INTO ProcessTransactionHistory 
                            (
                                ProcessTransactionDetailID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                ProcessID, TransactionType, TransactionDate, Qty, ShortQty, MachineNumber, SlipNo, 
                                JobberID, JobberName, FromPartyID, FromPartyName, ToPartyID, ToPartyName, ReferenceTransactionID, Remark, CompanyId, CreatedByUserID, CreatedDateTime
                            )
                            VALUES 
                            (
                                @DetailID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                @ProcessID, 'RECEIVE', @TransactionDate, @Qty, @ShortQty, 1, @SlipNo, 
                                @JobberID, @JobberName, @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, @RefTxnID, @Remark, @CompanyId, @CreatedByUserID, GETDATE()
                            );";

                                using (SqlCommand cmdH = new SqlCommand(sqlHistory, con, trans))
                                {
                                    cmdH.Parameters.AddWithValue("@DetailID", detailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdH.Parameters.AddWithValue("@ProcessID", processID);
                                    cmdH.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                    cmdH.Parameters.AddWithValue("@Qty", item.ReceivePCS);
                                    cmdH.Parameters.AddWithValue("@ShortQty", item.ShortPCS);
                                    cmdH.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdH.Parameters.AddWithValue("@JobberID", jobberID);
                                    cmdH.Parameters.AddWithValue("@JobberName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                    cmdH.Parameters.AddWithValue("@FromPartyID", processorID);
                                    cmdH.Parameters.AddWithValue("@FromPartyName", string.IsNullOrEmpty(processorName) ? "PROCESSOR" : processorName);
                                    cmdH.Parameters.AddWithValue("@ToPartyID", jobberID);
                                    cmdH.Parameters.AddWithValue("@ToPartyName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                    cmdH.Parameters.AddWithValue("@RefTxnID", issueProcessTransactionID);
                                    cmdH.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(remark) ? processName + " RECEIVE" : remark);
                                    cmdH.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdH.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    cmdH.ExecuteNonQuery();
                                }

                                if (item.IssueDetailID > 0)
                                {
                                    using (SqlCommand cmdUpd = new SqlCommand(@"
                                    UPDATE ProcessTransactionDetail 
                                    SET ReceivePCS = ISNULL(ReceivePCS, 0) + @RecQty, 
                                        ShortPCS = ISNULL(ShortPCS, 0) + @ShortQty, 
                                        PendingPCS = CASE WHEN (IssuePCS - (ISNULL(ReceivePCS, 0) + @RecQty + ISNULL(ShortPCS, 0) + @ShortQty)) < 0 THEN 0 ELSE (IssuePCS - (ISNULL(ReceivePCS, 0) + @RecQty + ISNULL(ShortPCS, 0) + @ShortQty)) END 
                                    WHERE ProcessTransactionDetailID = @DetailID", con, trans))
                                    {
                                        cmdUpd.Parameters.AddWithValue("@RecQty", item.ReceivePCS);
                                        cmdUpd.Parameters.AddWithValue("@ShortQty", item.ShortPCS);
                                        cmdUpd.Parameters.AddWithValue("@DetailID", item.IssueDetailID);
                                        cmdUpd.ExecuteNonQuery();
                                    }
                                }
                            }

                            AuditLogHelper.Log(
                                module: "Process Receive",
                                action: "CREATE",
                                tableName: "ProcessTransactionMaster",
                                recordId: masterID.ToString(),
                                description: $"Created {processName} Receive Challan #{receiveTxnNo}",
                                existingConn: con,
                                existingTran: trans
                            );

                            trans.Commit();
                            var resp = new GenericApiResponse(true, processName + " Receive Saved Successfully!")
                            {
                                ReceiveChallanNo = receiveTxnNo
                            };
                            WriteJsonResponse(resp);
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, "Error saving receive: " + ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetProcessStock(int processID, long processorID, long jobberID, string soNumber, string slipNo, string designNo, int machineNumber, string fromDate, string toDate)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                var list = new List<ProcessStockItemDto>();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    if (processID <= 0)
                    {
                        processID = GetButtaCuttingProcessID(con);
                    }

                    if (processID <= 0)
                    {
                        WriteJsonResponse(new GenericApiResponse(false, "Process context not found."));
                        return;
                    }

                    bool hasPOD_Table = DoesTableExist(con, "ProductionOrderDetail");
                    bool hasPOD_SalesOrderNo = hasPOD_Table && DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderNo");

                    string soNumberExpr = hasPOD_SalesOrderNo ? "ISNULL(POD.SalesOrderNo, '')" : "''";
                    string podJoin = hasPOD_Table ? "LEFT JOIN ProductionOrderDetail POD ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID" : "";

                    string sql = string.Format(@"
                SELECT 
                    PTM.ProcessID,
                    ISNULL(PM.ProcessName, 'BUTTA CUTTING') AS ProcessName,

                    PTM.FromPartyID AS ProcessorID,
                    ISNULL(PTM.FromPartyName, '') AS ProcessorName,

                    PTM.ToPartyID AS JobberID,
                    ISNULL(PTM.ToPartyName, '') AS JobberName,

                    PTD.ProductionOrderID,
                    PTD.ProductionOrderDetailID,
                    PTD.ProductionOrderSubDetailID,

                    ISNULL(PTD.SlipNo, '') AS SlipNo,
                    ISNULL(PTD.DesignNo, '') AS DesignNo,
                    ISNULL(PTD.MachineNumber, 1) AS MachineNumber,

                    ISNULL(PTD.ColourMatchingID, 0) AS ColourMatchingID,
                    ISNULL(PTD.ColourMatchingName, '') AS ColourMatchingName,

                    {0} AS SONumber,

                    ISNULL(PTD.Remark, PTM.Remark) AS JobType,

                    PTD.ProcessTransactionDetailID AS ReceiveDetailID,
                    PTM.ProcessTransactionID AS OriginalReceiveTransactionID,

                    ISNULL(PTD.ReceivePCS, 0) AS ReceivedPCS,
                    ISNULL(IB.IssuedBackPCS, 0) AS IssuedBackPCS,
                    ISNULL(PTD.ReceivePCS, 0) - ISNULL(IB.IssuedBackPCS, 0) AS AvailablePCS

                FROM ProcessTransactionMaster PTM

                INNER JOIN ProcessTransactionDetail PTD
                    ON PTM.ProcessTransactionID = PTD.ProcessTransactionID

                LEFT JOIN ProcessMaster PM
                    ON PTM.ProcessID = PM.ProcessID

                {1}

                LEFT JOIN
                (
                    SELECT 
                        PTM2.ReferenceTransactionID AS ReceiveTransactionID,
                        PTD2.ProductionOrderDetailID,
                        ISNULL(PTD2.ProductionOrderSubDetailID, 0) AS ProductionOrderSubDetailID,
                        ISNULL(PTD2.SlipNo, '') AS SlipNo,

                        SUM(ISNULL(PTD2.IssuePCS, 0)) AS IssuedBackPCS

                    FROM ProcessTransactionMaster PTM2

                    INNER JOIN ProcessTransactionDetail PTD2
                        ON PTM2.ProcessTransactionID = PTD2.ProcessTransactionID

                    WHERE PTM2.TransactionType = 'ISSUE'
                      AND ISNULL(PTM2.ReferenceTransactionID, 0) > 0
                      AND ISNULL(PTM2.IsActive, 1) = 1
                      AND ISNULL(PTD2.IsActive, 1) = 1

                    GROUP BY 
                        PTM2.ReferenceTransactionID,
                        PTD2.ProductionOrderDetailID,
                        ISNULL(PTD2.ProductionOrderSubDetailID, 0),
                        ISNULL(PTD2.SlipNo, '')
                ) IB
                    ON IB.ReceiveTransactionID = PTM.ProcessTransactionID
                   AND IB.ProductionOrderDetailID = PTD.ProductionOrderDetailID
                   AND IB.ProductionOrderSubDetailID = ISNULL(PTD.ProductionOrderSubDetailID, 0)
                   AND IB.SlipNo = ISNULL(PTD.SlipNo, '')

                WHERE PTM.TransactionType = 'RECEIVE'
                  AND ISNULL(PTM.IsActive, 1) = 1
                  AND ISNULL(PTD.IsActive, 1) = 1", soNumberExpr, podJoin);

                    if (processorID > 0) sql += " AND PTM.FromPartyID = @ProcessorID";
                    if (jobberID > 0) sql += " AND PTM.ToPartyID = @JobberID";
                    if (!string.IsNullOrEmpty(slipNo)) sql += " AND PTD.SlipNo LIKE '%' + @SlipNo + '%'";
                    if (!string.IsNullOrEmpty(soNumber) && hasPOD_SalesOrderNo) sql += " AND POD.SalesOrderNo LIKE '%' + @SONumber + '%'";
                    if (!string.IsNullOrEmpty(designNo)) sql += " AND PTD.DesignNo LIKE '%' + @DesignNo + '%'";
                    if (machineNumber > 0) sql += " AND PTD.MachineNumber = @MachineNumber";
                    if (!string.IsNullOrEmpty(fromDate)) sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate)) sql += " AND PTM.TransactionDate <= @ToDate";

                    sql += " AND (ISNULL(PTD.ReceivePCS, 0) - ISNULL(IB.IssuedBackPCS, 0)) > 0 ORDER BY PTD.ProductionOrderID DESC, PTD.ProductionOrderDetailID ASC;";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcessID", processID);
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (!string.IsNullOrEmpty(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrEmpty(soNumber) && hasPOD_SalesOrderNo) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrEmpty(designNo)) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNumber > 0) cmd.Parameters.AddWithValue("@MachineNumber", machineNumber);
                        if (!string.IsNullOrEmpty(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate).Date);
                        if (!string.IsNullOrEmpty(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate).Date.AddDays(1).AddTicks(-1));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            int srNo = 1;
                            while (dr.Read())
                            {
                                decimal receivedPcs = Convert.ToDecimal(dr["ReceivedPCS"]);
                                decimal issuedBackPcs = Convert.ToDecimal(dr["IssuedBackPCS"]);
                                decimal availableProcessStockPcs = Convert.ToDecimal(dr["AvailablePCS"]);

                                list.Add(new ProcessStockItemDto
                                {
                                    SrNo = srNo,
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                    ProductionOrderSubDetailID = dr["ProductionOrderSubDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderSubDetailID"]) : 0,
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    SONumber = dr["SONumber"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    ReceivedPCS = receivedPcs,
                                    IssuedBackPCS = issuedBackPcs,
                                    AvailablePCS = availableProcessStockPcs,
                                    ProcessStockPCS = availableProcessStockPcs,
                                    Status = "PROCESS STOCK"
                                });
                                srNo++;
                            }
                        }
                    }
                }
                WriteJsonResponse(list);
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }
        #endregion

        #region PHASE 5: PROCESS TO JOBBER ISSUE BACK
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetProcessStockForIssueBack(
            int processID,
            long processorID,
            long jobberID,
            string soNumber,
            string slipNo,
            string designNo,
            int machineNumber,
            string searchBarcode,
            string fromDate,
            string toDate)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                var list = new List<ProcessStockForIssueBackItemDto>();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    string sql = @"
;WITH IssueBackTotals AS
(
    SELECT
        PTM2.ReferenceTransactionID AS ReceiveTransactionID,
        PTD2.ProductionOrderDetailID,
        ISNULL(PTD2.ProductionOrderSubDetailID, 0) AS ProductionOrderSubDetailID,
        ISNULL(PTD2.SlipNo, '') AS SlipNo,
        SUM(ISNULL(PTD2.IssuePCS, 0)) AS IssuedBackPCS
    FROM ProcessTransactionMaster PTM2
    INNER JOIN ProcessTransactionDetail PTD2
        ON PTM2.ProcessTransactionID = PTD2.ProcessTransactionID
    WHERE PTM2.TransactionType = 'ISSUE'
      AND ISNULL(PTM2.ReferenceTransactionID, 0) > 0
      AND ISNULL(PTM2.IsActive, 1) = 1
      AND ISNULL(PTD2.IsActive, 1) = 1
    GROUP BY
        PTM2.ReferenceTransactionID,
        PTD2.ProductionOrderDetailID,
        ISNULL(PTD2.ProductionOrderSubDetailID, 0),
        ISNULL(PTD2.SlipNo, '')
)

SELECT
    PTM.ProcessID,
    ISNULL(PM.ProcessName, '') AS ProcessName,
    PTM.FromPartyID AS ProcessorID,
    ISNULL(PTM.FromPartyName, '') AS ProcessorName,
    PTM.ToPartyID AS JobberID,
    ISNULL(PTM.ToPartyName, '') AS JobberName,
    PTD.ProductionOrderID,
    PTD.ProductionOrderDetailID,
    PTD.ProductionOrderSubDetailID,
    ISNULL(PTD.SlipNo, '') AS SlipNo,
    ISNULL(PTD.DesignNo, '') AS DesignNo,
    ISNULL(PTD.MachineNumber, 0) AS MachineNumber,
    ISNULL(PTD.ColourMatchingID, 0) AS ColourMatchingID,
    ISNULL(PTD.ColourMatchingName, '') AS ColourMatchingName,
    ISNULL(POD.SalesOrderNo, '') AS SONumber,
    ISNULL(PTM.Remark, '') AS JobType,
    PTD.ProcessTransactionDetailID AS ReceiveDetailID,
    PTM.ProcessTransactionID AS OriginalReceiveTransactionID,
    ISNULL(PTD.ReceivePCS, 0) AS TotalReceivedPCS,
    ISNULL(IB.IssuedBackPCS, 0) AS TotalIssuedBackPCS,
    (ISNULL(PTD.ReceivePCS, 0) - ISNULL(IB.IssuedBackPCS, 0)) AS AvailablePCS
FROM ProcessTransactionMaster PTM
INNER JOIN ProcessTransactionDetail PTD ON PTM.ProcessTransactionID = PTD.ProcessTransactionID
LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID
LEFT JOIN ProductionOrderDetail POD ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID
LEFT JOIN IssueBackTotals IB ON IB.ReceiveTransactionID = PTM.ProcessTransactionID
   AND IB.ProductionOrderDetailID = PTD.ProductionOrderDetailID
   AND IB.ProductionOrderSubDetailID = ISNULL(PTD.ProductionOrderSubDetailID, 0)
   AND IB.SlipNo = ISNULL(PTD.SlipNo, '')
WHERE PTM.TransactionType = 'RECEIVE'
  AND ISNULL(PTM.IsActive, 1) = 1
  AND ISNULL(PTD.IsActive, 1) = 1
  AND (ISNULL(PTD.ReceivePCS, 0) - ISNULL(IB.IssuedBackPCS, 0)) > 0
";

                    if (processID > 0) sql += " AND PTM.ProcessID = @ProcessID";
                    if (processorID > 0) sql += " AND (PTM.FromPartyID = @ProcessorID OR PTM.ToPartyID = @ProcessorID)";
                    if (jobberID > 0) sql += " AND (PTM.ToPartyID = @JobberID OR PTM.FromPartyID = @JobberID)";
                    if (!string.IsNullOrWhiteSpace(slipNo)) sql += " AND PTD.SlipNo LIKE '%' + @SlipNo + '%'";
                    if (!string.IsNullOrWhiteSpace(searchBarcode)) sql += " AND PTD.Barcode LIKE '%' + @Barcode + '%'";
                    if (!string.IsNullOrWhiteSpace(soNumber)) sql += " AND POD.SalesOrderNo LIKE '%' + @SONumber + '%'";
                    if (!string.IsNullOrWhiteSpace(designNo)) sql += " AND PTD.DesignNo LIKE '%' + @DesignNo + '%'";
                    if (machineNumber > 0) sql += " AND PTD.MachineNumber = @MachineNumber";
                    if (!string.IsNullOrWhiteSpace(fromDate)) sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrWhiteSpace(toDate)) sql += " AND PTM.TransactionDate < DATEADD(DAY, 1, @ToDate)";

                    sql += " ORDER BY PTD.ProductionOrderID DESC, PTD.ProductionOrderDetailID ASC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (processID > 0) cmd.Parameters.AddWithValue("@ProcessID", processID);
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (!string.IsNullOrWhiteSpace(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrWhiteSpace(searchBarcode)) cmd.Parameters.AddWithValue("@Barcode", searchBarcode);
                        if (!string.IsNullOrWhiteSpace(soNumber)) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrWhiteSpace(designNo)) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNumber > 0) cmd.Parameters.AddWithValue("@MachineNumber", machineNumber);
                        if (!string.IsNullOrWhiteSpace(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate));
                        if (!string.IsNullOrWhiteSpace(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            int srNo = 1;
                            while (dr.Read())
                            {
                                decimal receivedPcs = dr["TotalReceivedPCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalReceivedPCS"]) : 0;
                                decimal issuedBackPcs = dr["TotalIssuedBackPCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalIssuedBackPCS"]) : 0;
                                decimal availableProcessStockPcs = receivedPcs - issuedBackPcs;

                                if (availableProcessStockPcs <= 0) continue;

                                list.Add(new ProcessStockForIssueBackItemDto
                                {
                                    SrNo = srNo,
                                    ReceiveDetailID = dr["ReceiveDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ReceiveDetailID"]) : 0,
                                    OriginalReceiveTransactionID = dr["OriginalReceiveTransactionID"] != DBNull.Value ? Convert.ToInt64(dr["OriginalReceiveTransactionID"]) : 0,
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    ProductionOrderID = dr["ProductionOrderID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderID"]) : 0,
                                    ProductionOrderDetailID = dr["ProductionOrderDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderDetailID"]) : 0,
                                    ProductionOrderSubDetailID = dr["ProductionOrderSubDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderSubDetailID"]) : 0,
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 0,
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    SONumber = dr["SONumber"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    ReceivedPCS = receivedPcs,
                                    IssuedBackPCS = issuedBackPcs,
                                    AvailablePCS = availableProcessStockPcs,
                                    IssueBackPCS = availableProcessStockPcs
                                });
                                srNo++;
                            }
                        }
                    }
                }
                WriteJsonResponse(list);
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveProcessIssueBack(long processorID, int processID, long jobberID, string issueBackDate, string remark, List<IssueBackItemInput> items)
        {
            try
            {
                if (processorID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Please select Processor."));
                    return;
                }
                if (jobberID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Please select Jobber."));
                    return;
                }
                if (items == null || items.Count == 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "No items selected for issue back."));
                    return;
                }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    if (processID <= 0)
                    {
                        if (items != null && items.Count > 0 && items[0].OriginalReceiveTransactionID > 0)
                        {
                            using (SqlCommand cmdRecP = new SqlCommand("SELECT ProcessID FROM ProcessTransactionMaster WHERE ProcessTransactionID = @RecID", con))
                            {
                                cmdRecP.Parameters.AddWithValue("@RecID", items[0].OriginalReceiveTransactionID);
                                object pVal = cmdRecP.ExecuteScalar();
                                if (pVal != null && pVal != DBNull.Value) processID = Convert.ToInt32(pVal);
                            }
                        }
                        if (processID <= 0) processID = GetButtaCuttingProcessID(con);
                    }

                    string processName = "PROCESS";
                    if (processID > 0)
                    {
                        using (SqlCommand cmdP = new SqlCommand("SELECT ProcessName FROM ProcessMaster WHERE ProcessID = @PID", con))
                        {
                            cmdP.Parameters.AddWithValue("@PID", processID);
                            object pVal = cmdP.ExecuteScalar();
                            if (pVal != null && pVal != DBNull.Value) processName = pVal.ToString().Trim();
                        }
                    }

                    string processorName = "";
                    using (SqlCommand cmdP = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @ProcessorID", con))
                    {
                        cmdP.Parameters.AddWithValue("@ProcessorID", processorID);
                        object val = cmdP.ExecuteScalar();
                        if (val != null && val != DBNull.Value) processorName = val.ToString();
                    }
                    if (string.IsNullOrEmpty(processorName)) processorName = "PROCESSOR";

                    string jobberName = "";
                    using (SqlCommand cmdJ = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @JobberID", con))
                    {
                        cmdJ.Parameters.AddWithValue("@JobberID", jobberID);
                        object val = cmdJ.ExecuteScalar();
                        if (val != null && val != DBNull.Value) jobberName = val.ToString();
                    }
                    if (string.IsNullOrEmpty(jobberName)) jobberName = "JOBBER";

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            foreach (var item in items)
                            {
                                if (item.IssueBackPCS <= 0) continue;

                                decimal currentAvailable = 0;
                                string sqlCheck = @"
                            SELECT 
                                ISNULL(PTD.ReceivePCS, 0) - ISNULL((
                                    SELECT SUM(ISNULL(PTD2.IssuePCS, 0))
                                    FROM ProcessTransactionMaster PTM2 WITH (UPDLOCK)
                                    INNER JOIN ProcessTransactionDetail PTD2 ON PTM2.ProcessTransactionID = PTD2.ProcessTransactionID
                                    WHERE PTM2.TransactionType = 'ISSUE'
                                      AND PTM2.ReferenceTransactionID = PTM.ProcessTransactionID
                                      AND PTD2.ProductionOrderDetailID = PTD.ProductionOrderDetailID
                                      AND ISNULL(PTD2.SlipNo, '') = ISNULL(PTD.SlipNo, '')
                                      AND ISNULL(PTM2.IsActive, 1) = 1
                                      AND ISNULL(PTD2.IsActive, 1) = 1
                                ), 0) AS AvailablePCS
                            FROM ProcessTransactionDetail PTD WITH (UPDLOCK)
                            INNER JOIN ProcessTransactionMaster PTM WITH (UPDLOCK) ON PTD.ProcessTransactionID = PTM.ProcessTransactionID
                            WHERE (PTD.ProcessTransactionDetailID = @ReceiveDetailID 
                                   OR (PTM.ProcessTransactionID = @RefTxnID AND PTD.ProductionOrderDetailID = @PODID AND PTD.SlipNo = @SlipNo))
                              AND PTM.TransactionType = 'RECEIVE'
                              AND ISNULL(PTM.IsActive, 1) = 1
                              AND ISNULL(PTD.IsActive, 1) = 1";

                                using (SqlCommand cmdChk = new SqlCommand(sqlCheck, con, trans))
                                {
                                    cmdChk.Parameters.AddWithValue("@ReceiveDetailID", item.ReceiveDetailID);
                                    cmdChk.Parameters.AddWithValue("@RefTxnID", item.OriginalReceiveTransactionID);
                                    cmdChk.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                                    cmdChk.Parameters.AddWithValue("@SlipNo", item.SlipNo ?? "");
                                    object val = cmdChk.ExecuteScalar();
                                    if (val != null && val != DBNull.Value) currentAvailable = Convert.ToDecimal(val);
                                }

                                if (item.IssueBackPCS > currentAvailable)
                                {
                                    trans.Rollback();
                                    WriteJsonResponse(new GenericApiResponse(false, string.Format("Requested Issue Back PCS ({0}) exceeds current available Process Stock ({1}) for Slip '{2}'.", item.IssueBackPCS, currentAvailable, item.SlipNo)));
                                    return;
                                }
                            }

                            string issueBackNo = "";
                            using (SqlCommand cmdSeq = new SqlCommand("SELECT ISNULL(MAX(ProcessTransactionID), 0) + 1 FROM ProcessTransactionMaster WHERE ProcessID = @PID AND TransactionType = 'ISSUE' AND ISNULL(ReferenceTransactionID, 0) > 0", con, trans))
                            {
                                cmdSeq.Parameters.AddWithValue("@PID", processID);
                                long nextSeq = Convert.ToInt64(cmdSeq.ExecuteScalar());
                                string prefix = "IB";
                                using (SqlCommand cmdPrefix = new SqlCommand("SELECT ProcessCode FROM ProcessMaster WHERE ProcessID = @PID", con, trans))
                                {
                                    cmdPrefix.Parameters.AddWithValue("@PID", processID);
                                    object pCodeVal = cmdPrefix.ExecuteScalar();
                                    if (pCodeVal != null && pCodeVal != DBNull.Value)
                                    {
                                        string pCode = pCodeVal.ToString().Trim().Replace("_", "");
                                        prefix = pCode.Length >= 4 ? pCode.Substring(0, 4) + "IB" : pCode + "IB";
                                    }
                                }
                                issueBackNo = prefix + nextSeq.ToString("D4");
                            }

                            DateTime dateTxn = string.IsNullOrEmpty(issueBackDate) ? DateTime.Now : DateTime.Parse(issueBackDate);
                            long refTxnID = 0;
                            if (items.Count > 0) refTxnID = items[0].OriginalReceiveTransactionID;

                            long companyIdVal = secContext.CompanyId;
                            long userIdVal = secContext.UserAccountId;

                            long masterID = 0;
                            string sqlMaster = @"
                        INSERT INTO ProcessTransactionMaster 
                        (
                            ProcessTransactionNo, ProcessID, TransactionType, TransactionDate,
                            FromPartyID, FromPartyName, ToPartyID, ToPartyName, 
                            ReferenceTransactionID, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                        )
                        VALUES 
                        (
                            @TxnNo, @ProcessID, 'ISSUE', @TransactionDate,
                            @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, 
                            @RefTxnID, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                        );
                        SELECT SCOPE_IDENTITY();";

                            using (SqlCommand cmdM = new SqlCommand(sqlMaster, con, trans))
                            {
                                cmdM.Parameters.AddWithValue("@TxnNo", issueBackNo);
                                cmdM.Parameters.AddWithValue("@ProcessID", processID);
                                cmdM.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                cmdM.Parameters.AddWithValue("@FromPartyID", processorID);
                                cmdM.Parameters.AddWithValue("@FromPartyName", processorName);
                                cmdM.Parameters.AddWithValue("@ToPartyID", jobberID);
                                cmdM.Parameters.AddWithValue("@ToPartyName", jobberName);
                                cmdM.Parameters.AddWithValue("@RefTxnID", refTxnID);
                                cmdM.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(remark) ? processName + " ISSUE BACK" : remark);
                                cmdM.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                cmdM.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                masterID = Convert.ToInt64(cmdM.ExecuteScalar());
                            }

                            foreach (var item in items)
                            {
                                if (item.IssueBackPCS <= 0) continue;

                                string slipNo = item.SlipNo ?? "";
                                string designNo = "";
                                string colourMatchingName = "";
                                int colourMatchingID = 0;

                                if (item.ProductionOrderDetailID > 0)
                                {
                                    try
                                    {
                                        using (SqlCommand cmdI = new SqlCommand("SELECT TOP 1 DesignNo, ColourMatchingName, ColourMatchingID FROM ProductionOrderDetail WHERE ProductionOrderDetailID = @PODID", con, trans))
                                        {
                                            cmdI.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                                            using (SqlDataReader dr = cmdI.ExecuteReader())
                                            {
                                                if (dr.Read())
                                                {
                                                    designNo = dr["DesignNo"].ToString();
                                                    colourMatchingName = dr["ColourMatchingName"].ToString();
                                                    colourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt32(dr["ColourMatchingID"]) : 0;
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                string sqlDetail = @"
                            INSERT INTO ProcessTransactionDetail 
                            (
                                ProcessTransactionID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                SlipNo, DesignNo, ColourMatchingID, ColourMatchingName, MachineNumber, PCS, IssuePCS, ReceivePCS, ShortPCS, PendingPCS, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                            )
                            VALUES 
                            (
                                @MasterID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                @SlipNo, @DesignNo, @ColourMatchingID, @ColourMatchingName, 1, @IssuePCS, @IssuePCS, 0, 0, 0, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                            );
                            SELECT SCOPE_IDENTITY();";

                                long detailID = 0;
                                using (SqlCommand cmdD = new SqlCommand(sqlDetail, con, trans))
                                {
                                    cmdD.Parameters.AddWithValue("@MasterID", masterID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdD.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdD.Parameters.AddWithValue("@DesignNo", designNo);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingID", colourMatchingID);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingName", colourMatchingName);
                                    cmdD.Parameters.AddWithValue("@IssuePCS", item.IssueBackPCS);
                                    cmdD.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(item.Remark) ? remark : item.Remark);
                                    cmdD.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdD.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    detailID = Convert.ToInt64(cmdD.ExecuteScalar());
                                }

                                string sqlHistory = @"
                            INSERT INTO ProcessTransactionHistory 
                            (
                                ProcessTransactionDetailID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                ProcessID, TransactionType, TransactionDate, Qty, ShortQty, MachineNumber, SlipNo, 
                                JobberID, JobberName, FromPartyID, FromPartyName, ToPartyID, ToPartyName, ReferenceTransactionID, Remark, CompanyId, CreatedByUserID, CreatedDateTime
                            )
                            VALUES 
                            (
                                @DetailID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                @ProcessID, 'ISSUE', @TransactionDate, @Qty, 0, 1, @SlipNo, 
                                @JobberID, @JobberName, @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, @RefTxnID, @Remark, @CompanyId, @CreatedByUserID, GETDATE()
                            );";

                                using (SqlCommand cmdH = new SqlCommand(sqlHistory, con, trans))
                                {
                                    cmdH.Parameters.AddWithValue("@DetailID", detailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdH.Parameters.AddWithValue("@ProcessID", processID);
                                    cmdH.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                    cmdH.Parameters.AddWithValue("@Qty", item.IssueBackPCS);
                                    cmdH.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdH.Parameters.AddWithValue("@JobberID", jobberID);
                                    cmdH.Parameters.AddWithValue("@JobberName", jobberName);
                                    cmdH.Parameters.AddWithValue("@FromPartyID", processorID);
                                    cmdH.Parameters.AddWithValue("@FromPartyName", processorName);
                                    cmdH.Parameters.AddWithValue("@ToPartyID", jobberID);
                                    cmdH.Parameters.AddWithValue("@ToPartyName", jobberName);
                                    cmdH.Parameters.AddWithValue("@RefTxnID", item.OriginalReceiveTransactionID > 0 ? item.OriginalReceiveTransactionID : refTxnID);
                                    cmdH.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(item.Remark) ? remark : item.Remark);
                                    cmdH.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdH.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    cmdH.ExecuteNonQuery();
                                }
                            }

                            trans.Commit();
                            var resp = new GenericApiResponse(true, processName + " Issue Back Saved Successfully!")
                            {
                                IssueBackChallanNo = issueBackNo
                            };
                            WriteJsonResponse(resp);
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, "Error saving issue back: " + ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetProcessIssueBackList(string fromDate, string toDate, long processorID, int processID, long jobberID, string searchNo, string soNumber, string slipNo, string designNo, int machineNo)
        {
            try
            {
                var list = new List<ProcessIssueBackListDto>();
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    string sql = @"
SELECT
    PTM.ProcessTransactionID,
    PTM.ProcessTransactionNo,
    CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr,
    PTM.TransactionDate,
    RECEIVE_PTM.ProcessID,
    ISNULL(PM.ProcessName, '') AS ProcessName,
    PTM.FromPartyID AS ProcessorID,
    ISNULL(PTM.FromPartyName, '') AS ProcessorName,
    RECEIVE_PTM.ToPartyID AS JobberID,
    ISNULL(RECEIVE_PTM.ToPartyName, '') AS JobberName,
    ISNULL(PTM.Remark, '') AS JobType,
    MIN(ISNULL(PTD.SlipNo, '')) AS SlipNo,
    MIN(ISNULL(PTD.DesignNo, '')) AS DesignNo,
    ISNULL(SUM(ISNULL(PTD.IssuePCS, 0)), 0) AS TotalIssuedBackPCS,
    PTM.ReferenceTransactionID
FROM ProcessTransactionMaster PTM
INNER JOIN ProcessTransactionMaster RECEIVE_PTM
    ON PTM.ReferenceTransactionID = RECEIVE_PTM.ProcessTransactionID
   AND RECEIVE_PTM.TransactionType = 'RECEIVE'
   AND ISNULL(RECEIVE_PTM.IsActive, 1) = 1
LEFT JOIN ProcessMaster PM ON RECEIVE_PTM.ProcessID = PM.ProcessID
INNER JOIN ProcessTransactionDetail PTD
    ON PTM.ProcessTransactionID = PTD.ProcessTransactionID
   AND ISNULL(PTD.IsActive, 1) = 1
LEFT JOIN ProductionOrderDetail POD
    ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID
WHERE PTM.TransactionType = 'ISSUE'
  AND ISNULL(PTM.IsActive, 1) = 1
  AND ISNULL(PTM.ReferenceTransactionID, 0) > 0
";

                    if (processorID > 0) sql += " AND PTM.FromPartyID = @ProcessorID";
                    if (processID > 0) sql += " AND RECEIVE_PTM.ProcessID = @ProcessID";
                    if (jobberID > 0) sql += " AND RECEIVE_PTM.ToPartyID = @JobberID";
                    if (!string.IsNullOrWhiteSpace(searchNo)) sql += " AND PTM.ProcessTransactionNo LIKE '%' + @SearchNo + '%'";
                    if (!string.IsNullOrWhiteSpace(soNumber)) sql += " AND POD.SalesOrderNo LIKE '%' + @SONumber + '%'";
                    if (!string.IsNullOrWhiteSpace(slipNo)) sql += " AND PTD.SlipNo LIKE '%' + @SlipNo + '%'";
                    if (!string.IsNullOrWhiteSpace(designNo)) sql += " AND PTD.DesignNo LIKE '%' + @DesignNo + '%'";
                    if (machineNo > 0) sql += " AND PTD.MachineNumber = @MachineNo";
                    if (!string.IsNullOrWhiteSpace(fromDate)) sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrWhiteSpace(toDate)) sql += " AND PTM.TransactionDate < DATEADD(DAY, 1, @ToDate)";

                    sql += @"
GROUP BY
    PTM.ProcessTransactionID,
    PTM.ProcessTransactionNo,
    PTM.TransactionDate,
    RECEIVE_PTM.ProcessID,
    PM.ProcessName,
    PTM.FromPartyID,
    PTM.FromPartyName,
    RECEIVE_PTM.ToPartyID,
    RECEIVE_PTM.ToPartyName,
    PTM.Remark,
    PTM.ReferenceTransactionID
ORDER BY
    PTM.ProcessTransactionID DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (processID > 0) cmd.Parameters.AddWithValue("@ProcessID", processID);
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (!string.IsNullOrWhiteSpace(searchNo)) cmd.Parameters.AddWithValue("@SearchNo", searchNo);
                        if (!string.IsNullOrWhiteSpace(soNumber)) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrWhiteSpace(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrWhiteSpace(designNo)) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNo > 0) cmd.Parameters.AddWithValue("@MachineNo", machineNo);
                        if (!string.IsNullOrWhiteSpace(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate).Date);
                        if (!string.IsNullOrWhiteSpace(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate).Date);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            int srNo = 1;
                            while (dr.Read())
                            {
                                decimal totalIssuedBackPCS = dr["TotalIssuedBackPCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalIssuedBackPCS"]) : 0;

                                list.Add(new ProcessIssueBackListDto
                                {
                                    ProcessTransactionID = dr["ProcessTransactionID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessTransactionID"]) : 0,
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDate = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    TotalIssuedBackPCS = totalIssuedBackPCS,
                                    ReferenceTransactionID = dr["ReferenceTransactionID"] != DBNull.Value ? Convert.ToInt64(dr["ReferenceTransactionID"]) : 0
                                });
                                srNo++;
                            }
                        }
                    }
                }
                WriteJsonResponse(list);
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetProcessIssueBackDetail(long processTransactionID)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    JobberToProcessMasterHeaderDto master = null;
                    using (SqlCommand cmdM = new SqlCommand(@"
                    SELECT 
                        PTM.ProcessTransactionID, 
                        PTM.ProcessTransactionNo, 
                        CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr, 
                        PTM.ProcessID,
                        ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                        PTM.FromPartyID AS ProcessorID, 
                        PTM.FromPartyName AS ProcessorName, 
                        PTM.ToPartyID AS JobberID, 
                        PTM.ToPartyName AS JobberName, 
                        PTM.Remark 
                    FROM ProcessTransactionMaster PTM 
                    LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID 
                    WHERE PTM.ProcessTransactionID = @ID AND ISNULL(PTM.IsActive, 1) = 1", con))
                    {
                        cmdM.Parameters.AddWithValue("@ID", processTransactionID);
                        using (SqlDataReader dr = cmdM.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                master = new JobberToProcessMasterHeaderDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDateStr = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    JobType = dr["Remark"].ToString(),
                                    Remark = dr["Remark"].ToString()
                                };
                            }
                        }
                    }

                    if (master == null)
                    {
                        WriteJsonResponse(new GenericApiResponse(false, "Issue Back Challan not found or inactive."));
                        return;
                    }

                    string sql = @"
                SELECT 
                    PTD.ProcessTransactionDetailID,
                    PTD.ProductionOrderID,
                    PTD.ProductionOrderDetailID,
                    PTD.ProductionOrderSubDetailID,
                    PTD.SlipNo,
                    PTD.DesignNo,
                    PTD.ColourMatchingID,
                    PTD.ColourMatchingName,
                    PTD.MachineNumber,
                    PTD.IssuePCS AS IssueBackPCS,
                    PTD.Remark AS JobType
                FROM ProcessTransactionDetail PTD
                WHERE PTD.ProcessTransactionID = @ID 
                  AND ISNULL(PTD.IsActive, 1) = 1
                ORDER BY PTD.ProcessTransactionDetailID ASC";

                    var items = new List<ProcessIssueBackDetailItemDto>();
                    using (SqlCommand cmdD = new SqlCommand(sql, con))
                    {
                        cmdD.Parameters.AddWithValue("@ID", processTransactionID);
                        using (SqlDataReader dr = cmdD.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                items.Add(new ProcessIssueBackDetailItemDto
                                {
                                    ProcessTransactionDetailID = Convert.ToInt64(dr["ProcessTransactionDetailID"]),
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                    ProductionOrderSubDetailID = Convert.ToInt64(dr["ProductionOrderSubDetailID"]),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    IssueBackPCS = Convert.ToDecimal(dr["IssueBackPCS"]),
                                    JobType = dr["JobType"].ToString()
                                });
                            }
                        }
                    }

                    var response = new ProcessIssueBackDetailResponse
                    {
                        Status = true,
                        Master = master,
                        Details = items
                    };
                    WriteJsonResponse(response);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }
        #endregion

        #region PHASE 6: JOBBER RECEIVE FROM PROCESS ISSUE BACK
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetIssueBackChallansForJobberReceive(string fromDate, string toDate, long jobberID, int processID, long processorID, string searchNo, string soNumber, string slipNo, string designNo, int machineNo)
        {
            try
            {
                var list = new List<JobberReceiveChallanListItemDto>();
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    string sql = @"
                SELECT 
                    PTM.ProcessTransactionID,
                    PTM.ProcessTransactionNo,
                    CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr,
                    PTM.TransactionDate,
                    PTM.ProcessID,
                    ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                    PTM.FromPartyID AS ProcessorID,
                    PTM.FromPartyName AS ProcessorName,
                    PTM.ToPartyID AS JobberID,
                    PTM.ToPartyName AS JobberName,
                    PTM.Remark AS JobType,
                    MIN(ISNULL(PTD.SlipNo, '')) AS SlipNo,
                    MIN(ISNULL(PTD.DesignNo, '')) AS DesignNo,
                    ISNULL(SUM(PTD.IssuePCS), 0) AS TotalIssueBackPCS,
                    ISNULL(SUM(PTD.ReceivePCS), 0) AS TotalJobberReceivePCS,
                    (ISNULL(SUM(PTD.IssuePCS), 0) - ISNULL(SUM(PTD.ReceivePCS), 0)) AS PendingJobberReceivePCS
                FROM ProcessTransactionMaster PTM
                LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID
                INNER JOIN ProcessTransactionDetail PTD ON PTM.ProcessTransactionID = PTD.ProcessTransactionID AND ISNULL(PTD.IsActive, 1) = 1
                LEFT JOIN ProductionOrderDetail POD ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID
                WHERE PTM.TransactionType = 'ISSUE'
                  AND ISNULL(PTM.ReferenceTransactionID, 0) > 0
                  AND ISNULL(PTM.IsActive, 1) = 1";

                    if (jobberID > 0) sql += " AND (PTM.ToPartyID = @JobberID OR PTM.FromPartyID = @JobberID)";
                    if (processID > 0) sql += " AND (PTM.ProcessID = @ProcessID OR PTM.ProcessID IN (SELECT ProcessID FROM ProcessMaster WHERE ProcessName LIKE '%BUTTA%' OR ProcessCode LIKE '%BUTTA%'))";
                    if (processorID > 0) sql += " AND (PTM.FromPartyID = @ProcessorID OR PTM.ToPartyID = @ProcessorID)";
                    if (!string.IsNullOrEmpty(searchNo)) sql += " AND PTM.ProcessTransactionNo LIKE '%' + @SearchNo + '%'";
                    if (!string.IsNullOrEmpty(soNumber)) sql += " AND (POD.SalesOrderNo LIKE '%' + @SONumber + '%')";
                    if (!string.IsNullOrEmpty(slipNo)) sql += " AND (PTD.SlipNo LIKE '%' + @SlipNo + '%')";
                    if (!string.IsNullOrEmpty(designNo)) sql += " AND (PTD.DesignNo LIKE '%' + @DesignNo + '%')";
                    if (machineNo > 0) sql += " AND (PTD.MachineNumber = @MachineNo)";
                    if (!string.IsNullOrEmpty(fromDate)) sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate)) sql += " AND PTM.TransactionDate <= @ToDate";

                    sql += @" GROUP BY PTM.ProcessTransactionID, PTM.ProcessTransactionNo, PTM.TransactionDate, PTM.ProcessID, PM.ProcessName, PTM.FromPartyID, PTM.FromPartyName, PTM.ToPartyID, PTM.ToPartyName, PTM.Remark
                HAVING (ISNULL(SUM(PTD.IssuePCS), 0) - ISNULL(SUM(PTD.ReceivePCS), 0)) > 0
                ORDER BY PTM.ProcessTransactionID DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (processID > 0) cmd.Parameters.AddWithValue("@ProcessID", processID);
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (!string.IsNullOrEmpty(searchNo)) cmd.Parameters.AddWithValue("@SearchNo", searchNo);
                        if (!string.IsNullOrEmpty(soNumber)) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrEmpty(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrEmpty(designNo)) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNo > 0) cmd.Parameters.AddWithValue("@MachineNo", machineNo);
                        if (!string.IsNullOrEmpty(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate).Date);
                        if (!string.IsNullOrEmpty(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate).Date.AddDays(1).AddTicks(-1));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                decimal issueBackPcs = Convert.ToDecimal(dr["TotalIssueBackPCS"]);
                                decimal jobberReceivePcs = Convert.ToDecimal(dr["TotalJobberReceivePCS"]);
                                decimal pendingPcs = Convert.ToDecimal(dr["PendingJobberReceivePCS"]);

                                list.Add(new JobberReceiveChallanListItemDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDate = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    TotalIssueBackPCS = issueBackPcs,
                                    TotalJobberReceivePCS = jobberReceivePcs,
                                    PendingJobberReceivePCS = pendingPcs
                                });
                            }
                        }
                    }
                    WriteJsonResponse(list);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetIssueBackDetailForJobberReceive(long issueBackTransactionID)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    JobberToProcessMasterHeaderDto master = null;
                    using (SqlCommand cmdM = new SqlCommand(@"
                    SELECT 
                        PTM.ProcessTransactionID, 
                        PTM.ProcessTransactionNo, 
                        CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr, 
                        PTM.ProcessID,
                        ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                        PTM.FromPartyID AS ProcessorID, 
                        PTM.FromPartyName AS ProcessorName, 
                        PTM.ToPartyID AS JobberID, 
                        PTM.ToPartyName AS JobberName, 
                        PTM.Remark 
                    FROM ProcessTransactionMaster PTM 
                    LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID 
                    WHERE PTM.ProcessTransactionID = @ID AND ISNULL(PTM.IsActive, 1) = 1", con))
                    {
                        cmdM.Parameters.AddWithValue("@ID", issueBackTransactionID);
                        using (SqlDataReader dr = cmdM.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                master = new JobberToProcessMasterHeaderDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDateStr = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    JobType = dr["Remark"].ToString()
                                };
                            }
                        }
                    }

                    if (master == null)
                    {
                        WriteJsonResponse(new GenericApiResponse(false, "Issue Back Challan not found or inactive."));
                        return;
                    }

                    int procId = master.ProcessID;
                    string procName = master.ProcessName ?? "";

                    string nextReceiveNo = "";
                    using (SqlCommand cmdSeq = new SqlCommand("SELECT ISNULL(MAX(ProcessTransactionID), 0) + 1 FROM ProcessTransactionMaster WHERE ProcessID = @PID AND TransactionType = 'RECEIVE' AND (ProcessTransactionNo LIKE '%JR%' OR Remark LIKE '%JOBBER RECEIVE%')", con))
                    {
                        cmdSeq.Parameters.AddWithValue("@PID", procId);
                        long nextSeq = Convert.ToInt64(cmdSeq.ExecuteScalar());
                        string prefix = procName.StartsWith("BUTTA") ? "BCJR" : (procName.StartsWith("BORDER") ? "BRJR" : "PCJR");
                        nextReceiveNo = prefix + nextSeq.ToString("D4");
                    }

                    string sql = @"
                SELECT 
                    PTD.ProcessTransactionDetailID,
                    PTD.ProductionOrderID,
                    PTD.ProductionOrderDetailID,
                    PTD.ProductionOrderSubDetailID,
                    PTD.SlipNo,
                    PTD.DesignNo,
                    PTD.ColourMatchingID,
                    PTD.ColourMatchingName,
                    PTD.MachineNumber,
                    PTD.IssuePCS AS IssueBackPCS,
                    ISNULL(PTD.ReceivePCS, 0) AS AlreadyJobberReceivePCS,
                    (PTD.IssuePCS - ISNULL(PTD.ReceivePCS, 0)) AS PendingJobberReceivePCS,
                    PTD.Remark AS JobType
                FROM ProcessTransactionDetail PTD
                WHERE PTD.ProcessTransactionID = @ID 
                  AND ISNULL(PTD.IsActive, 1) = 1 
                  AND (PTD.IssuePCS - ISNULL(PTD.ReceivePCS, 0)) > 0
                ORDER BY PTD.ProcessTransactionDetailID ASC";

                    var items = new List<JobberReceiveItemDto>();
                    using (SqlCommand cmdD = new SqlCommand(sql, con))
                    {
                        cmdD.Parameters.AddWithValue("@ID", issueBackTransactionID);
                        using (SqlDataReader dr = cmdD.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                decimal issueBackPcs = Convert.ToDecimal(dr["IssueBackPCS"]);
                                decimal alreadyJobberReceivePcs = Convert.ToDecimal(dr["AlreadyJobberReceivePCS"]);
                                decimal pendingPcs = Convert.ToDecimal(dr["PendingJobberReceivePCS"]);

                                items.Add(new JobberReceiveItemDto
                                {
                                    IssueBackDetailID = Convert.ToInt64(dr["ProcessTransactionDetailID"]),
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                    ProductionOrderSubDetailID = Convert.ToInt64(dr["ProductionOrderSubDetailID"]),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    IssueBackPCS = issueBackPcs,
                                    AlreadyJobberReceivePCS = alreadyJobberReceivePcs,
                                    PendingJobberReceivePCS = pendingPcs,
                                    ReceivePCS = pendingPcs,
                                    JobType = dr["JobType"].ToString()
                                });
                            }
                        }
                    }

                    var response = new JobberReceiveDetailResponse
                    {
                        Status = true,
                        Master = master,
                        NextReceiveNo = nextReceiveNo,
                        NextJobberReceiveNo = nextReceiveNo,
                        Details = items
                    };
                    WriteJsonResponse(response);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveJobberReceive(long issueBackTransactionID, long processorID, int processID, long jobberID, string receiveDate, string remark, List<JobberReceiveItemInput> items)
        {
            try
            {
                if (issueBackTransactionID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Invalid Issue Back Challan Reference."));
                    return;
                }
                if (processorID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Please select Processor."));
                    return;
                }
                if (jobberID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Please select Jobber."));
                    return;
                }
                if (items == null || items.Count == 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "No items selected for Jobber Receive."));
                    return;
                }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;

                        using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(1) FROM ProcessTransactionMaster WHERE ProcessTransactionID = @ID AND (FromPartyID = @JID OR ToPartyID = @JID)", con))
                        {
                            cmdCheck.Parameters.AddWithValue("@ID", issueBackTransactionID);
                            cmdCheck.Parameters.AddWithValue("@JID", secContext.LoggedInJobberID);
                            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) == 0)
                            {
                                WriteJsonResponse(new GenericApiResponse(false, "Access Denied: You cannot process a receive for another Jobber's issue back challan."));
                                return;
                            }
                        }
                    }

                    string processName = "PROCESS";
                    if (processID > 0)
                    {
                        using (SqlCommand cmdP = new SqlCommand("SELECT ProcessName FROM ProcessMaster WHERE ProcessID = @PID", con))
                        {
                            cmdP.Parameters.AddWithValue("@PID", processID);
                            object pVal = cmdP.ExecuteScalar();
                            if (pVal != null && pVal != DBNull.Value) processName = pVal.ToString().Trim();
                        }
                    }

                    string processorName = "";
                    using (SqlCommand cmdP = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @ProcessorID", con))
                    {
                        cmdP.Parameters.AddWithValue("@ProcessorID", processorID);
                        object val = cmdP.ExecuteScalar();
                        if (val != null && val != DBNull.Value) processorName = val.ToString();
                    }
                    if (string.IsNullOrEmpty(processorName)) processorName = "PROCESSOR";

                    string jobberName = "";
                    using (SqlCommand cmdJ = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @JobberID", con))
                    {
                        cmdJ.Parameters.AddWithValue("@JobberID", jobberID);
                        object val = cmdJ.ExecuteScalar();
                        if (val != null && val != DBNull.Value) jobberName = val.ToString();
                    }
                    if (string.IsNullOrEmpty(jobberName)) jobberName = "JOBBER";

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            foreach (var item in items)
                            {
                                if (item.ReceivePCS <= 0) continue;

                                decimal currentPending = 0;
                                string sqlCheck = @"
                            SELECT (IssuePCS - ISNULL(ReceivePCS, 0)) AS PendingQty 
                            FROM ProcessTransactionDetail WITH (UPDLOCK)
                            WHERE ProcessTransactionDetailID = @DetailID AND ISNULL(IsActive, 1) = 1";

                                using (SqlCommand cmdChk = new SqlCommand(sqlCheck, con, trans))
                                {
                                    cmdChk.Parameters.AddWithValue("@DetailID", item.IssueBackDetailID);
                                    object val = cmdChk.ExecuteScalar();
                                    if (val != null && val != DBNull.Value) currentPending = Convert.ToDecimal(val);
                                }

                                if (item.ReceivePCS > currentPending)
                                {
                                    trans.Rollback();
                                    WriteJsonResponse(new GenericApiResponse(false, string.Format("Requested Receive PCS ({0}) exceeds current pending Jobber Receive ({1}) for Slip '{2}'.", item.ReceivePCS, currentPending, item.SlipNo)));
                                    return;
                                }
                            }

                            string jobberReceiveNo = "";
                            using (SqlCommand cmdSeq = new SqlCommand("SELECT ISNULL(MAX(ProcessTransactionID), 0) + 1 FROM ProcessTransactionMaster WHERE ProcessID = @PID AND TransactionType = 'RECEIVE' AND (ProcessTransactionNo LIKE '%JR%' OR Remark LIKE '%JOBBER RECEIVE%')", con, trans))
                            {
                                cmdSeq.Parameters.AddWithValue("@PID", processID);
                                long nextSeq = Convert.ToInt64(cmdSeq.ExecuteScalar());
                                string prefix = processName.StartsWith("BUTTA") ? "BCJR" : (processName.StartsWith("BORDER") ? "BRJR" : "PCJR");
                                jobberReceiveNo = prefix + nextSeq.ToString("D4");
                            }

                            DateTime dateTxn = string.IsNullOrEmpty(receiveDate) ? DateTime.Now : DateTime.Parse(receiveDate);

                            long companyIdVal = secContext.CompanyId;
                            long userIdVal = secContext.UserAccountId;

                            long masterID = 0;
                            string sqlMaster = @"
                        INSERT INTO ProcessTransactionMaster 
                        (
                            ProcessTransactionNo, ProcessID, TransactionType, TransactionDate,
                            FromPartyID, FromPartyName, ToPartyID, ToPartyName, 
                            ReferenceTransactionID, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                        )
                        VALUES 
                        (
                            @TxnNo, @ProcessID, 'RECEIVE', @TransactionDate,
                            @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, 
                            @RefTxnID, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                        );
                        SELECT SCOPE_IDENTITY();";

                            using (SqlCommand cmdM = new SqlCommand(sqlMaster, con, trans))
                            {
                                cmdM.Parameters.AddWithValue("@TxnNo", jobberReceiveNo);
                                cmdM.Parameters.AddWithValue("@ProcessID", processID);
                                cmdM.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                cmdM.Parameters.AddWithValue("@FromPartyID", processorID);
                                cmdM.Parameters.AddWithValue("@FromPartyName", processorName);
                                cmdM.Parameters.AddWithValue("@ToPartyID", jobberID);
                                cmdM.Parameters.AddWithValue("@ToPartyName", jobberName);
                                cmdM.Parameters.AddWithValue("@RefTxnID", issueBackTransactionID);
                                cmdM.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(remark) ? processName + " JOBBER RECEIVE" : remark);
                                cmdM.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                cmdM.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                masterID = Convert.ToInt64(cmdM.ExecuteScalar());
                            }

                            foreach (var item in items)
                            {
                                if (item.ReceivePCS <= 0) continue;

                                string slipNo = item.SlipNo ?? "";
                                string designNo = "";
                                string colourMatchingName = "";
                                int colourMatchingID = 0;

                                if (item.ProductionOrderDetailID > 0)
                                {
                                    try
                                    {
                                        using (SqlCommand cmdI = new SqlCommand("SELECT TOP 1 DesignNo, ColourMatchingName, ColourMatchingID FROM ProductionOrderDetail WHERE ProductionOrderDetailID = @PODID", con, trans))
                                        {
                                            cmdI.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                                            using (SqlDataReader dr = cmdI.ExecuteReader())
                                            {
                                                if (dr.Read())
                                                {
                                                    designNo = dr["DesignNo"].ToString();
                                                    colourMatchingName = dr["ColourMatchingName"].ToString();
                                                    colourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt32(dr["ColourMatchingID"]) : 0;
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                string sqlDetail = @"
                            INSERT INTO ProcessTransactionDetail 
                            (
                                ProcessTransactionID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                SlipNo, DesignNo, ColourMatchingID, ColourMatchingName, MachineNumber, PCS, IssuePCS, ReceivePCS, ShortPCS, PendingPCS, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                            )
                            VALUES 
                            (
                                @MasterID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                @SlipNo, @DesignNo, @ColourMatchingID, @ColourMatchingName, 1, @RecPCS, 0, @RecPCS, 0, 0, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                            );
                            SELECT SCOPE_IDENTITY();";

                                long detailID = 0;
                                using (SqlCommand cmdD = new SqlCommand(sqlDetail, con, trans))
                                {
                                    cmdD.Parameters.AddWithValue("@MasterID", masterID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdD.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdD.Parameters.AddWithValue("@DesignNo", designNo);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingID", colourMatchingID);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingName", colourMatchingName);
                                    cmdD.Parameters.AddWithValue("@RecPCS", item.ReceivePCS);
                                    cmdD.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(item.Remark) ? remark : item.Remark);
                                    cmdD.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdD.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    detailID = Convert.ToInt64(cmdD.ExecuteScalar());
                                }

                                string sqlHistory = @"
                            INSERT INTO ProcessTransactionHistory 
                            (
                                ProcessTransactionDetailID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                ProcessID, TransactionType, TransactionDate, Qty, ShortQty, MachineNumber, SlipNo, 
                                JobberID, JobberName, FromPartyID, FromPartyName, ToPartyID, ToPartyName, ReferenceTransactionID, Remark, CompanyId, CreatedByUserID, CreatedDateTime
                            )
                            VALUES 
                            (
                                @DetailID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                @ProcessID, 'RECEIVE', @TransactionDate, @Qty, 0, 1, @SlipNo, 
                                @JobberID, @JobberName, @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, @RefTxnID, @Remark, @CompanyId, @CreatedByUserID, GETDATE()
                            );";

                                using (SqlCommand cmdH = new SqlCommand(sqlHistory, con, trans))
                                {
                                    cmdH.Parameters.AddWithValue("@DetailID", detailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdH.Parameters.AddWithValue("@ProcessID", processID);
                                    cmdH.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                    cmdH.Parameters.AddWithValue("@Qty", item.ReceivePCS);
                                    cmdH.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdH.Parameters.AddWithValue("@JobberID", jobberID);
                                    cmdH.Parameters.AddWithValue("@JobberName", jobberName);
                                    cmdH.Parameters.AddWithValue("@FromPartyID", processorID);
                                    cmdH.Parameters.AddWithValue("@FromPartyName", processorName);
                                    cmdH.Parameters.AddWithValue("@ToPartyID", jobberID);
                                    cmdH.Parameters.AddWithValue("@ToPartyName", jobberName);
                                    cmdH.Parameters.AddWithValue("@RefTxnID", issueBackTransactionID);
                                    cmdH.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(item.Remark) ? remark : item.Remark);
                                    cmdH.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdH.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    cmdH.ExecuteNonQuery();
                                }

                                if (item.IssueBackDetailID > 0)
                                {
                                    using (SqlCommand cmdUpd = new SqlCommand(@"
                                    UPDATE ProcessTransactionDetail 
                                    SET ReceivePCS = ISNULL(ReceivePCS, 0) + @RecQty 
                                    WHERE ProcessTransactionDetailID = @DetailID", con, trans))
                                    {
                                        cmdUpd.Parameters.AddWithValue("@RecQty", item.ReceivePCS);
                                        cmdUpd.Parameters.AddWithValue("@DetailID", item.IssueBackDetailID);
                                        cmdUpd.ExecuteNonQuery();
                                    }
                                }
                            }

                            trans.Commit();
                            WriteJsonResponse(new GenericApiResponse
                            {
                                Status = true,
                                Success = true,
                                JobberReceiveChallanNo = jobberReceiveNo,
                                Message = processName + " Jobber Receive Saved Successfully!"
                            });
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, "Error saving Jobber Receive: " + ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetJobberReceiveList(string fromDate, string toDate, long jobberID, int processID, long processorID, string searchNo, string soNumber, string slipNo, string designNo, int machineNo)
        {
            try
            {
                var list = new List<JobberReceiveChallanListItemDto>();
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    string sql = @"
                SELECT 
                    PTM.ProcessTransactionID,
                    PTM.ProcessTransactionNo,
                    CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr,
                    PTM.TransactionDate,
                    PTM.ProcessID,
                    ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                    PTM.FromPartyID AS ProcessorID,
                    PTM.FromPartyName AS ProcessorName,
                    PTM.ToPartyID AS JobberID,
                    PTM.ToPartyName AS JobberName,
                    PTM.Remark AS JobType,
                    MIN(ISNULL(PTD.SlipNo, '')) AS SlipNo,
                    MIN(ISNULL(PTD.DesignNo, '')) AS DesignNo,
                    ISNULL(SUM(PTD.ReceivePCS), 0) AS TotalReceivedPCS
                FROM ProcessTransactionMaster PTM
                LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID
                INNER JOIN ProcessTransactionDetail PTD ON PTM.ProcessTransactionID = PTD.ProcessTransactionID AND ISNULL(PTD.IsActive, 1) = 1
                LEFT JOIN ProductionOrderDetail POD ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID
                WHERE PTM.TransactionType = 'RECEIVE'
                  AND (PTM.ProcessTransactionNo LIKE '%JR%' OR PTM.Remark LIKE '%JOBBER RECEIVE%' OR EXISTS (SELECT 1 FROM ProcessTransactionMaster PTM_REF WHERE PTM_REF.ProcessTransactionID = PTM.ReferenceTransactionID AND PTM_REF.TransactionType = 'ISSUE' AND PTM_REF.ReferenceTransactionID > 0))
                  AND ISNULL(PTM.IsActive, 1) = 1";

                    if (jobberID > 0) sql += " AND PTM.ToPartyID = @JobberID";
                    if (processID > 0) sql += " AND PTM.ProcessID = @ProcessID";
                    if (processorID > 0) sql += " AND PTM.FromPartyID = @ProcessorID";
                    if (!string.IsNullOrEmpty(searchNo)) sql += " AND PTM.ProcessTransactionNo LIKE '%' + @SearchNo + '%'";
                    if (!string.IsNullOrEmpty(soNumber)) sql += " AND (POD.SalesOrderNo LIKE '%' + @SONumber + '%')";
                    if (!string.IsNullOrEmpty(slipNo)) sql += " AND (PTD.SlipNo LIKE '%' + @SlipNo + '%')";
                    if (!string.IsNullOrEmpty(designNo)) sql += " AND (PTD.DesignNo LIKE '%' + @DesignNo + '%')";
                    if (machineNo > 0) sql += " AND (PTD.MachineNumber = @MachineNo)";
                    if (!string.IsNullOrEmpty(fromDate)) sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate)) sql += " AND PTM.TransactionDate <= @ToDate";

                    sql += @" GROUP BY PTM.ProcessTransactionID, PTM.ProcessTransactionNo, PTM.TransactionDate, PTM.ProcessID, PM.ProcessName, PTM.FromPartyID, PTM.FromPartyName, PTM.ToPartyID, PTM.ToPartyName, PTM.Remark
                ORDER BY PTM.ProcessTransactionID DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (processID > 0) cmd.Parameters.AddWithValue("@ProcessID", processID);
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (!string.IsNullOrEmpty(searchNo)) cmd.Parameters.AddWithValue("@SearchNo", searchNo);
                        if (!string.IsNullOrEmpty(soNumber)) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrEmpty(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrEmpty(designNo)) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNo > 0) cmd.Parameters.AddWithValue("@MachineNo", machineNo);
                        if (!string.IsNullOrEmpty(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate));
                        if (!string.IsNullOrEmpty(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new JobberReceiveChallanListItemDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDate = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    TotalReceivedPCS = Convert.ToDecimal(dr["TotalReceivedPCS"]),
                                    TotalReceivePCS = Convert.ToDecimal(dr["TotalReceivedPCS"]),
                                    Status = "JOBBER RECEIVED"
                                });
                            }
                        }
                    }
                    WriteJsonResponse(list);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetJobberReceiveDetail(long processTransactionID)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    JobberToProcessMasterHeaderDto master = null;
                    using (SqlCommand cmdM = new SqlCommand(@"
                    SELECT 
                        PTM.ProcessTransactionID, 
                        PTM.ProcessTransactionNo, 
                        CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr, 
                        PTM.ProcessID,
                        ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                        PTM.FromPartyID AS ProcessorID, 
                        PTM.FromPartyName AS ProcessorName, 
                        PTM.ToPartyID AS JobberID, 
                        PTM.ToPartyName AS JobberName, 
                        PTM.Remark 
                    FROM ProcessTransactionMaster PTM 
                    LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID 
                    WHERE PTM.ProcessTransactionID = @ID AND ISNULL(PTM.IsActive, 1) = 1", con))
                    {
                        cmdM.Parameters.AddWithValue("@ID", processTransactionID);
                        using (SqlDataReader dr = cmdM.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                master = new JobberToProcessMasterHeaderDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDateStr = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    JobType = dr["Remark"].ToString()
                                };
                            }
                        }
                    }

                    if (master == null)
                    {
                        WriteJsonResponse(new GenericApiResponse(false, "Jobber Receive Challan not found or inactive."));
                        return;
                    }

                    string sql = @"
                SELECT 
                    PTD.ProcessTransactionDetailID,
                    PTD.ProductionOrderID,
                    PTD.ProductionOrderDetailID,
                    PTD.ProductionOrderSubDetailID,
                    PTD.SlipNo,
                    PTD.DesignNo,
                    PTD.ColourMatchingID,
                    PTD.ColourMatchingName,
                    PTD.MachineNumber,
                    PTD.ReceivePCS,
                    PTD.Remark AS JobType
                FROM ProcessTransactionDetail PTD
                WHERE PTD.ProcessTransactionID = @ID 
                  AND ISNULL(PTD.IsActive, 1) = 1
                ORDER BY PTD.ProcessTransactionDetailID ASC";

                    var items = new List<JobberReceiveItemDto>();
                    using (SqlCommand cmdD = new SqlCommand(sql, con))
                    {
                        cmdD.Parameters.AddWithValue("@ID", processTransactionID);
                        using (SqlDataReader dr = cmdD.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                items.Add(new JobberReceiveItemDto
                                {
                                    DetailID = Convert.ToInt64(dr["ProcessTransactionDetailID"]),
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                    ProductionOrderSubDetailID = Convert.ToInt64(dr["ProductionOrderSubDetailID"]),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    ReceivePCS = Convert.ToDecimal(dr["ReceivePCS"]),
                                    JobType = dr["JobType"].ToString()
                                });
                            }
                        }
                    }

                    var response = new JobberReceiveDetailResponse
                    {
                        Status = true,
                        Master = master,
                        Details = items
                    };
                    WriteJsonResponse(response);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }
        #endregion

        #region PHASE 7A: COMPLETE PROCESS HISTORY & MOVEMENT TIMELINE
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetProcessManagementHistory(
            string fromDate,
            string toDate,
            int processID,
            long jobberID,
            long processorID,
            string transactionType,
            string searchNo,
            string soNumber,
            string slipNo,
            string designNo,
            int machineNo)
        {
            try
            {
                var list = new List<ProcessHistoryItemDto>();
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    string sql = @"
                SELECT 
                    PTH.ProcessHistoryID,
                    PTH.ProcessTransactionDetailID,
                    CONVERT(VARCHAR(10), PTH.TransactionDate, 103) + ' ' + CONVERT(VARCHAR(5), PTH.CreatedDateTime, 108) AS DateStr,
                    PTH.TransactionType,
                    PTH.ProcessID,
                    ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                    PTH.FromPartyID,
                    ISNULL(PTH.FromPartyName, 'PARTY') AS FromPartyName,
                    PTH.ToPartyID,
                    ISNULL(PTH.ToPartyName, 'PARTY') AS ToPartyName,
                    PTH.ProductionOrderID,
                    PTH.ProductionOrderDetailID,
                    PTH.ProductionOrderSubDetailID,
                    ISNULL(POD.SalesOrderNo, 'SO36') AS SONumber,
                    PTH.SlipNo,
                    ISNULL(POD.DesignNo, '') AS DesignNo,
                    ISNULL(POSD.MachineNumber, ISNULL(PTH.MachineNumber, 1)) AS MachineNumber,
                    ISNULL(PTH.Qty, 0) AS Qty,
                    ISNULL(PTH.ShortQty, 0) AS ShortQty,
                    PTH.Remark,
                    PTH.ReferenceTransactionID,
                    ISNULL(PTM.ProcessTransactionNo, '') AS TransactionNo
                FROM ProcessTransactionHistory PTH
                LEFT JOIN ProcessMaster PM ON PTH.ProcessID = PM.ProcessID
                LEFT JOIN ProductionOrderDetail POD ON PTH.ProductionOrderDetailID = POD.ProductionOrderDetailID
                LEFT JOIN ProductionOrderSubDetail POSD ON PTH.ProductionOrderSubDetailID = POSD.ProductionOrderSubDetailID
                LEFT JOIN ProcessTransactionDetail PTD ON PTH.ProcessTransactionDetailID = PTD.ProcessTransactionDetailID
                LEFT JOIN ProcessTransactionMaster PTM ON PTD.ProcessTransactionID = PTM.ProcessTransactionID
                WHERE 1 = 1";

                    if (processID > 0) sql += " AND PTH.ProcessID = @ProcessID";
                    if (jobberID > 0) sql += " AND (PTH.JobberID = @JobberID OR PTH.FromPartyID = @JobberID OR PTH.ToPartyID = @JobberID)";
                    if (processorID > 0) sql += " AND (PTH.FromPartyID = @ProcessorID OR PTH.ToPartyID = @ProcessorID)";
                    if (!string.IsNullOrEmpty(transactionType)) sql += " AND PTH.TransactionType = @TransactionType";
                    if (!string.IsNullOrEmpty(searchNo)) sql += " AND (PTM.ProcessTransactionNo LIKE '%' + @SearchNo + '%' OR PTH.SlipNo LIKE '%' + @SearchNo + '%')";
                    if (!string.IsNullOrEmpty(soNumber)) sql += " AND (POD.SalesOrderNo LIKE '%' + @SONumber + '%')";
                    if (!string.IsNullOrEmpty(slipNo)) sql += " AND (PTH.SlipNo LIKE '%' + @SlipNo + '%')";
                    if (!string.IsNullOrEmpty(designNo)) sql += " AND (POD.DesignNo LIKE '%' + @DesignNo + '%')";
                    if (machineNo > 0) sql += " AND (POSD.MachineNumber = @MachineNo OR PTH.MachineNumber = @MachineNo)";
                    if (!string.IsNullOrEmpty(fromDate)) sql += " AND PTH.TransactionDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate)) sql += " AND PTH.TransactionDate <= @ToDate";

                    sql += " ORDER BY PTH.ProcessHistoryID DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (processID > 0) cmd.Parameters.AddWithValue("@ProcessID", processID);
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (!string.IsNullOrEmpty(transactionType)) cmd.Parameters.AddWithValue("@TransactionType", transactionType);
                        if (!string.IsNullOrEmpty(searchNo)) cmd.Parameters.AddWithValue("@SearchNo", searchNo);
                        if (!string.IsNullOrEmpty(soNumber)) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrEmpty(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrEmpty(designNo)) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNo > 0) cmd.Parameters.AddWithValue("@MachineNo", machineNo);
                        if (!string.IsNullOrEmpty(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate));
                        if (!string.IsNullOrEmpty(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new ProcessHistoryItemDto
                                {
                                    ProcessHistoryID = Convert.ToInt64(dr["ProcessHistoryID"]),
                                    ProcessTransactionDetailID = dr["ProcessTransactionDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessTransactionDetailID"]) : 0,
                                    DateStr = dr["DateStr"].ToString(),
                                    TransactionType = dr["TransactionType"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    FromPartyID = dr["FromPartyID"] != DBNull.Value ? Convert.ToInt64(dr["FromPartyID"]) : 0,
                                    FromPartyName = dr["FromPartyName"].ToString(),
                                    ToPartyID = dr["ToPartyID"] != DBNull.Value ? Convert.ToInt64(dr["ToPartyID"]) : 0,
                                    ToPartyName = dr["ToPartyName"].ToString(),
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                    ProductionOrderSubDetailID = dr["ProductionOrderSubDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderSubDetailID"]) : 0,
                                    SONumber = dr["SONumber"].ToString(),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    Qty = Convert.ToDecimal(dr["Qty"]),
                                    ShortQty = Convert.ToDecimal(dr["ShortQty"]),
                                    Remark = dr["Remark"].ToString(),
                                    ReferenceTransactionID = dr["ReferenceTransactionID"] != DBNull.Value ? Convert.ToInt64(dr["ReferenceTransactionID"]) : 0,
                                    TransactionNo = dr["TransactionNo"].ToString()
                                });
                            }
                        }
                    }
                    WriteJsonResponse(list);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetSlipTimelineAndStockSummary(string slipNo, long productionOrderDetailID)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    var secContext = GetLoggedInUserSecurityContext(con);

                    SlipMetaDto slipMeta = null;
                    if (productionOrderDetailID > 0 || !string.IsNullOrEmpty(slipNo))
                    {
                        bool hasPOD_JobberID = DoesColumnExist(con, null, "ProductionOrderDetail", "JobberID");
                        bool hasPOD_JobberName = DoesColumnExist(con, null, "ProductionOrderDetail", "JobberName");
                        bool hasPOM_JobWorkerID = DoesColumnExist(con, null, "ProductionOrderMaster", "JobWorkerID");
                        bool hasPOM_JobWorkerName = DoesColumnExist(con, null, "ProductionOrderMaster", "JobWorkerName");

                        bool hasPOSD_Table = DoesTableExist(con, "ProductionOrderSubDetail");
                        bool hasPOSD_SlipNo = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "SlipNo");
                        bool hasPOSD_Machine = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "MachineNumber");
                        bool hasPOD_SlipNo = DoesColumnExist(con, null, "ProductionOrderDetail", "SlipNo");
                        bool hasPOD_SalesOrderNo = DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderNo");
                        bool hasPOD_DesignNo = DoesColumnExist(con, null, "ProductionOrderDetail", "DesignNo");
                        bool hasPOD_ColourMatchingName = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatchingName");
                        bool hasPOD_ColourMatchingID = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatchingID");

                        string jobberIDExpr = hasPOD_JobberID ? "ISNULL(POD.JobberID, 0)" : (hasPOM_JobWorkerID ? "ISNULL(POM.JobWorkerID, 0)" : "0");
                        string jobberNameExpr = hasPOD_JobberName ? "ISNULL(POD.JobberName, '')" : (hasPOM_JobWorkerName ? "ISNULL(POM.JobWorkerName, '')" : "''");
                        string slipNoExpr = (hasPOSD_SlipNo && hasPOD_SlipNo) ? "ISNULL(NULLIF(POSD.SlipNo, ''), ISNULL(POD.SlipNo, @SlipNo))" : (hasPOSD_SlipNo ? "ISNULL(NULLIF(POSD.SlipNo, ''), @SlipNo)" : (hasPOD_SlipNo ? "ISNULL(NULLIF(POD.SlipNo, ''), @SlipNo)" : "@SlipNo"));
                        string machineExpr = hasPOSD_Machine ? "ISNULL(POSD.MachineNumber, 1)" : "1";
                        string salesOrderNoExpr = hasPOD_SalesOrderNo ? "ISNULL(POD.SalesOrderNo, '')" : "''";
                        string designNoExpr = hasPOD_DesignNo ? "ISNULL(POD.DesignNo, '')" : "''";
                        string cmNameExpr = hasPOD_ColourMatchingName ? "ISNULL(POD.ColourMatchingName, '')" : "''";
                        string cmIDExpr = hasPOD_ColourMatchingID ? "ISNULL(POD.ColourMatchingID, 0)" : "0";

                        string posdJoin = hasPOSD_Table ? "LEFT JOIN ProductionOrderSubDetail POSD ON POD.ProductionOrderDetailID = POSD.ProductionOrderDetailID" : "";
                        string pomJoin = (hasPOM_JobWorkerID || hasPOM_JobWorkerName) ? "LEFT JOIN ProductionOrderMaster POM ON POD.ProductionOrderID = POM.ProductionOrderID" : "";

                        string whereSlipClause = hasPOSD_SlipNo
                            ? " AND (POSD.SlipNo = @SlipNo OR POD.ProductionOrderDetailID IN (SELECT ProductionOrderDetailID FROM ProcessTransactionHistory WHERE SlipNo = @SlipNo))"
                            : " AND (POD.ProductionOrderDetailID IN (SELECT ProductionOrderDetailID FROM ProcessTransactionHistory WHERE SlipNo = @SlipNo))";

                        string sqlMeta = string.Format(@"
                    SELECT TOP 1 
                        POD.ProductionOrderDetailID, POD.ProductionOrderID, 
                        {0} AS SalesOrderNo, {1} AS DesignNo,
                        {2} AS ColourMatchingID, {3} AS ColourMatchingName, 
                        ISNULL(POD.PCS, 0) AS ProductionPCS,
                        {4} AS SlipNo, {5} AS MachineNumber,
                        {6} AS JobberID, {7} AS JobberName
                    FROM ProductionOrderDetail POD
                    {8}
                    {9}
                    WHERE 1=1",
                        salesOrderNoExpr, designNoExpr, cmIDExpr, cmNameExpr, slipNoExpr, machineExpr, jobberIDExpr, jobberNameExpr, posdJoin, pomJoin);

                        if (productionOrderDetailID > 0) sqlMeta += " AND POD.ProductionOrderDetailID = @PODID";
                        else if (!string.IsNullOrEmpty(slipNo)) sqlMeta += whereSlipClause;

                        using (SqlCommand cmdMeta = new SqlCommand(sqlMeta, con))
                        {
                            cmdMeta.Parameters.AddWithValue("@PODID", productionOrderDetailID);
                            cmdMeta.Parameters.AddWithValue("@SlipNo", slipNo ?? "");
                            using (SqlDataReader dr = cmdMeta.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    slipMeta = new SlipMetaDto
                                    {
                                        ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                        ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                        SalesOrderNo = dr["SalesOrderNo"].ToString(),
                                        DesignNo = dr["DesignNo"].ToString(),
                                        ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                        ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                        ProductionPCS = Convert.ToDecimal(dr["ProductionPCS"]),
                                        SlipNo = dr["SlipNo"].ToString(),
                                        MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                        JobberID = Convert.ToInt64(dr["JobberID"]),
                                        JobberName = dr["JobberName"].ToString()
                                    };
                                }
                            }
                        }
                    }

                    var timeline = new List<ProcessHistoryItemDto>();
                    string sqlTimeline = @"
                SELECT 
                    PTH.ProcessHistoryID,
                    PTH.ProcessTransactionDetailID,
                    CONVERT(VARCHAR(10), PTH.TransactionDate, 103) + ' ' + CONVERT(VARCHAR(5), PTH.CreatedDateTime, 108) AS DateStr,
                    PTH.TransactionType,
                    PTH.ProcessID,
                    ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                    ISNULL(PTH.FromPartyName, 'PARTY') AS FromPartyName,
                    ISNULL(PTH.ToPartyName, 'PARTY') AS ToPartyName,
                    PTH.SlipNo,
                    ISNULL(PTH.Qty, 0) AS Qty,
                    ISNULL(PTH.ShortQty, 0) AS ShortQty,
                    PTH.MachineNumber,
                    PTH.Remark,
                    PTH.ReferenceTransactionID,
                    ISNULL(PTM.ProcessTransactionNo, '') AS TransactionNo
                FROM ProcessTransactionHistory PTH
                LEFT JOIN ProcessMaster PM ON PTH.ProcessID = PM.ProcessID
                LEFT JOIN ProcessTransactionDetail PTD ON PTH.ProcessTransactionDetailID = PTD.ProcessTransactionDetailID
                LEFT JOIN ProcessTransactionMaster PTM ON PTD.ProcessTransactionID = PTM.ProcessTransactionID
                WHERE 1 = 1";

                    if (!string.IsNullOrEmpty(slipNo)) sqlTimeline += " AND PTH.SlipNo = @SlipNo";
                    if (productionOrderDetailID > 0) sqlTimeline += " AND PTH.ProductionOrderDetailID = @PODID";
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0) sqlTimeline += " AND (PTH.JobberID = @SecJobberID OR PTH.FromPartyID = @SecJobberID OR PTH.ToPartyID = @SecJobberID)";

                    sqlTimeline += " ORDER BY PTH.ProcessHistoryID ASC";

                    using (SqlCommand cmdT = new SqlCommand(sqlTimeline, con))
                    {
                        cmdT.Parameters.AddWithValue("@SlipNo", slipNo ?? "");
                        cmdT.Parameters.AddWithValue("@PODID", productionOrderDetailID);
                        cmdT.Parameters.AddWithValue("@SecJobberID", secContext.LoggedInJobberID);

                        using (SqlDataReader dr = cmdT.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                timeline.Add(new ProcessHistoryItemDto
                                {
                                    ProcessHistoryID = Convert.ToInt64(dr["ProcessHistoryID"]),
                                    ProcessTransactionDetailID = dr["ProcessTransactionDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessTransactionDetailID"]) : 0,
                                    DateStr = dr["DateStr"].ToString(),
                                    TransactionType = dr["TransactionType"].ToString(),
                                    ProcessName = dr["ProcessName"].ToString(),
                                    FromPartyName = dr["FromPartyName"].ToString(),
                                    ToPartyName = dr["ToPartyName"].ToString(),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    Qty = Convert.ToDecimal(dr["Qty"]),
                                    ShortQty = Convert.ToDecimal(dr["ShortQty"]),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    Remark = dr["Remark"].ToString(),
                                    TransactionNo = dr["TransactionNo"].ToString(),
                                    ReferenceTransactionID = dr["ReferenceTransactionID"] != DBNull.Value ? Convert.ToInt64(dr["ReferenceTransactionID"]) : 0
                                });
                            }
                        }
                    }

                    decimal originalProdPcs = slipMeta != null ? slipMeta.ProductionPCS : 0;
                    decimal totalIssuedToProcessPcs = 0;
                    decimal totalReceivedFromProcessPcs = 0;
                    decimal totalProcessShortPcs = 0;
                    decimal totalIssuedBackPcs = 0;
                    decimal totalJobberReceivedPcs = 0;

                    foreach (var h in timeline)
                    {
                        string tType = h.TransactionType ?? "";
                        decimal q = h.Qty;
                        decimal sq = h.ShortQty;

                        if (tType == "ISSUE")
                        {
                            totalIssuedToProcessPcs += q;
                        }
                        else if (tType == "RECEIVE")
                        {
                            totalReceivedFromProcessPcs += q;
                            totalProcessShortPcs += sq;
                        }
                        else if (tType == "ISSUE_BACK" || tType == "RETURN_ISSUE")
                        {
                            totalIssuedBackPcs += q;
                        }
                        else if (tType == "JOBBER_RECEIVE")
                        {
                            totalJobberReceivedPcs += q;
                        }
                    }

                    decimal jobberCurrentStock = (originalProdPcs > 0 ? originalProdPcs : totalIssuedToProcessPcs) - totalIssuedToProcessPcs + totalJobberReceivedPcs;
                    decimal pendingProcessReceive = totalIssuedToProcessPcs - totalReceivedFromProcessPcs - totalProcessShortPcs;
                    if (pendingProcessReceive < 0) pendingProcessReceive = 0;

                    decimal processStock = totalReceivedFromProcessPcs - totalIssuedBackPcs;
                    if (processStock < 0) processStock = 0;

                    decimal pendingJobberReceive = totalIssuedBackPcs - totalJobberReceivedPcs;
                    if (pendingJobberReceive < 0) pendingJobberReceive = 0;

                    var stockSummary = new SlipStockSummaryDto
                    {
                        OriginalProductionPCS = originalProdPcs,
                        JobberCurrentStockPCS = jobberCurrentStock,
                        PendingProcessReceivePCS = pendingProcessReceive,
                        ProcessStockPCS = processStock,
                        PendingJobberReceivePCS = pendingJobberReceive,
                        TotalShortPCS = totalProcessShortPcs,
                        TotalIssuedToProcessPCS = totalIssuedToProcessPcs,
                        TotalReceivedFromProcessPCS = totalReceivedFromProcessPcs,
                        TotalIssuedBackPCS = totalIssuedBackPcs,
                        TotalJobberReceivedPCS = totalJobberReceivedPcs
                    };

                    var response = new SlipTimelineAndStockResponse
                    {
                        Status = true,
                        Meta = slipMeta,
                        Timeline = timeline,
                        StockSummary = stockSummary
                    };
                    WriteJsonResponse(response);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }
        #endregion

        #region PHASE 7B: CRYSTAL REPORTS & CHALLAN PRINT ENGINE
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GenerateProcessChallanReportData(long processTransactionID)
        {
            try
            {
                if (processTransactionID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Invalid Process Transaction ID."));
                    return;
                }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    ChallanReportMasterDto master = null;
                    string sqlM = @"
                SELECT 
                    PTM.ProcessTransactionID,
                    PTM.ProcessTransactionNo,
                    PTM.TransactionType,
                    CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr,
                    PTM.TransactionDate,
                    PTM.ProcessID,
                    ISNULL(PM.ProcessName, 'PROCESS') AS ProcessName,
                    PTM.FromPartyID,
                    ISNULL(PTM.FromPartyName, 'PARTY') AS FromPartyName,
                    PTM.ToPartyID,
                    ISNULL(PTM.ToPartyName, 'PARTY') AS ToPartyName,
                    PTM.ReferenceTransactionID,
                    PTM.Remark,
                    ISNULL(RefPTM.ProcessTransactionNo, '') AS ReferenceTransactionNo,
                    CONVERT(VARCHAR(10), RefPTM.TransactionDate, 103) AS ReferenceTransactionDateStr
                FROM ProcessTransactionMaster PTM
                LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID
                LEFT JOIN ProcessTransactionMaster RefPTM ON PTM.ReferenceTransactionID = RefPTM.ProcessTransactionID
                WHERE PTM.ProcessTransactionID = @ID AND ISNULL(PTM.IsActive, 1) = 1";

                    using (SqlCommand cmdM = new SqlCommand(sqlM, con))
                    {
                        cmdM.Parameters.AddWithValue("@ID", processTransactionID);
                        using (SqlDataReader dr = cmdM.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                master = new ChallanReportMasterDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionType = dr["TransactionType"].ToString(),
                                    TransactionDateStr = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : 0,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    FromPartyID = dr["FromPartyID"] != DBNull.Value ? Convert.ToInt64(dr["FromPartyID"]) : 0,
                                    FromPartyName = dr["FromPartyName"].ToString(),
                                    ToPartyID = dr["ToPartyID"] != DBNull.Value ? Convert.ToInt64(dr["ToPartyID"]) : 0,
                                    ToPartyName = dr["ToPartyName"].ToString(),
                                    ReferenceTransactionID = dr["ReferenceTransactionID"] != DBNull.Value ? Convert.ToInt64(dr["ReferenceTransactionID"]) : 0,
                                    ReferenceTransactionNo = dr["ReferenceTransactionNo"].ToString(),
                                    ReferenceTransactionDateStr = dr["ReferenceTransactionDateStr"].ToString(),
                                    Remark = dr["Remark"].ToString()
                                };
                            }
                        }
                    }

                    if (master == null)
                    {
                        WriteJsonResponse(new GenericApiResponse(false, "Transaction not found or inactive."));
                        return;
                    }

                    string sqlD = @"
                SELECT 
                    PTD.ProcessTransactionDetailID,
                    PTD.ProductionOrderID,
                    PTD.ProductionOrderDetailID,
                    PTD.ProductionOrderSubDetailID,
                    ISNULL(POD.SalesOrderNo, 'SO36') AS SONumber,
                    PTD.SlipNo,
                    PTD.DesignNo,
                    PTD.ColourMatchingID,
                    PTD.ColourMatchingName,
                    PTD.MachineNumber,
                    ISNULL(PTD.PCS, 0) AS PCS,
                    ISNULL(PTD.IssuePCS, 0) AS IssuePCS,
                    ISNULL(PTD.ReceivePCS, 0) AS ReceivePCS,
                    ISNULL(PTD.ShortPCS, 0) AS ShortPCS,
                    ISNULL(PTD.PendingPCS, 0) AS PendingPCS,
                    PTD.Remark
                FROM ProcessTransactionDetail PTD
                LEFT JOIN ProductionOrderDetail POD ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID
                WHERE PTD.ProcessTransactionID = @ID AND ISNULL(PTD.IsActive, 1) = 1
                ORDER BY PTD.ProcessTransactionDetailID ASC";

                    var items = new List<ChallanReportDetailDto>();
                    using (SqlCommand cmdD = new SqlCommand(sqlD, con))
                    {
                        cmdD.Parameters.AddWithValue("@ID", processTransactionID);
                        using (SqlDataReader dr = cmdD.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                items.Add(new ChallanReportDetailDto
                                {
                                    ProcessTransactionDetailID = Convert.ToInt64(dr["ProcessTransactionDetailID"]),
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                    ProductionOrderSubDetailID = dr["ProductionOrderSubDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderSubDetailID"]) : 0,
                                    SONumber = dr["SONumber"].ToString(),
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    PCS = Convert.ToDecimal(dr["PCS"]),
                                    IssuePCS = Convert.ToDecimal(dr["IssuePCS"]),
                                    ReceivePCS = Convert.ToDecimal(dr["ReceivePCS"]),
                                    ShortPCS = Convert.ToDecimal(dr["ShortPCS"]),
                                    PendingPCS = Convert.ToDecimal(dr["PendingPCS"]),
                                    Remark = dr["Remark"].ToString()
                                });
                            }
                        }
                    }

                    var company = new ChallanCompanyDto();

                    var response = new ProcessChallanReportDataResponse
                    {
                        Status = true,
                        Master = master,
                        Details = items,
                        Company = company
                    };
                    WriteJsonResponse(response);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        public string GenerateProcessChallanReportPDF(long processTransactionID)
        {
            try
            {
                if (processTransactionID <= 0) return "Error: Invalid Process Transaction ID.";

                string reportPath = HttpContext.Current.Server.MapPath("~/Reports/ProductionOrderReport.rpt");
                if (!System.IO.File.Exists(reportPath))
                {
                    reportPath = HttpContext.Current.Server.MapPath("~/Reports/WEAVELANE PRODUCTION SLIP.rpt");
                }

                if (!System.IO.File.Exists(reportPath))
                {
                    return "Error: Crystal Report template file not found.";
                }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (ReportDocument rpt = new ReportDocument())
                {
                    rpt.Load(reportPath, OpenReportMethod.OpenReportByTempCopy);
                    SetCrystalDatabaseLogin(rpt, connectionString);

                    string fileName = "ProcessChallan_" + processTransactionID + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                    string exportPath = HttpContext.Current.Server.MapPath("~/GeneratedPDF/" + fileName);

                    string dir = System.IO.Path.GetDirectoryName(exportPath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    rpt.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, exportPath);
                    rpt.Close();

                    return "/GeneratedPDF/" + fileName;
                }
            }
            catch (Exception ex)
            {
                string errorDetails = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return "Error: " + errorDetails;
            }
        }
        #endregion

        #region BUTTA CUTTING MODULE (JOBBER -> BUTTA CUTTING -> RECEIVE & STOCK)

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetJobberToButtaCuttingData(long jobberID, long productionOrderID, string slipNo, int machineNumber, string fromDate, string toDate, string barcode = "", string designNo = "")
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                var list = new List<JobberToProcessItemDto>();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    int buttaProcID = GetButtaCuttingProcessID(con);

                    bool hasPOD_OrderDate = DoesColumnExist(con, null, "ProductionOrderDetail", "OrderDate");
                    bool hasPOM_OrderDate = DoesColumnExist(con, null, "ProductionOrderMaster", "ProductionOrderDate");
                    bool hasPOD_PartyName = DoesColumnExist(con, null, "ProductionOrderDetail", "PartyName");
                    bool hasPOM_JobWorkerID = DoesColumnExist(con, null, "ProductionOrderMaster", "JobWorkerID");
                    bool hasPOM_JobWorkerName = DoesColumnExist(con, null, "ProductionOrderMaster", "JobWorkerName");

                    bool hasPOSD_Table = DoesTableExist(con, "ProductionOrderSubDetail");
                    bool hasPOSD_SlipNo = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "SlipNo");
                    bool hasPOSD_MachineNumber = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "MachineNumber");
                    bool hasPOSD_PCS = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "PCS");

                    bool hasPOD_DesignNo = DoesColumnExist(con, null, "ProductionOrderDetail", "DesignNo");
                    bool hasPOD_ColourMatchingName = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatchingName");
                    bool hasPOD_ColourMatchingID = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatchingID");
                    bool hasPOD_ColourMatching = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatching");
                    bool hasPOD_PCS = DoesColumnExist(con, null, "ProductionOrderDetail", "PCS");
                    bool hasPOD_SalesOrderNo = DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderNo");

                    bool hasPOD_Barcode = DoesColumnExist(con, null, "ProductionOrderDetail", "Barcode");
                    bool hasPOSD_Barcode = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "Barcode");
                    string barcodeExpr = (hasPOSD_Barcode && hasPOD_Barcode) ? "ISNULL(NULLIF(POSD.Barcode, ''), ISNULL(POD.Barcode, ''))" : (hasPOSD_Barcode ? "ISNULL(POSD.Barcode, '')" : (hasPOD_Barcode ? "ISNULL(POD.Barcode, '')" : "''"));

                    string orderDateExpr = hasPOD_OrderDate ? "ISNULL(POD.OrderDate, GETDATE())" : (hasPOM_OrderDate ? "ISNULL(POM.ProductionOrderDate, GETDATE())" : "GETDATE()");
                    string jobberIDExpr = hasPOM_JobWorkerID ? "ISNULL(POM.JobWorkerID, 0)" : "0";
                    string jobberNameExpr = hasPOM_JobWorkerName ? "ISNULL(POM.JobWorkerName, 'NIKUNJ TEXTILES')" : "'NIKUNJ TEXTILES'";
                    string partyNameExpr = hasPOD_PartyName ? "ISNULL(POD.PartyName, 'M/S KSR HANDLOOMS PRIVATE LIMITED')" : "'M/S KSR HANDLOOMS PRIVATE LIMITED'";
                    bool hasPOD_SlipNo = DoesColumnExist(con, null, "ProductionOrderDetail", "SlipNo");
                    string slipNoExpr = (hasPOSD_SlipNo && hasPOD_SlipNo) ? "ISNULL(NULLIF(POSD.SlipNo, ''), ISNULL(POD.SlipNo, ''))" : (hasPOSD_SlipNo ? "ISNULL(POSD.SlipNo, '')" : (hasPOD_SlipNo ? "ISNULL(POD.SlipNo, '')" : "''"));
                    string machineExpr = hasPOSD_MachineNumber ? "ISNULL(POSD.MachineNumber, 1)" : "1";
                    string designNoExpr = hasPOD_DesignNo ? "ISNULL(POD.DesignNo, '')" : "''";
                    string colourMatchingExpr = hasPOD_ColourMatchingName ? "ISNULL(POD.ColourMatchingName, '')" : (hasPOD_ColourMatchingID ? "CAST(ISNULL(POD.ColourMatchingID, '') AS NVARCHAR(150))" : (hasPOD_ColourMatching ? "ISNULL(POD.ColourMatching, '')" : "''"));
                    string pcsExpr = hasPOSD_PCS ? "ISNULL(POSD.PCS, 24)" : (hasPOD_PCS ? "ISNULL(POD.PCS, 24)" : "24");
                    string soNumberExpr = hasPOD_SalesOrderNo ? "ISNULL(POD.SalesOrderNo, 'SO14')" : "'SO14'";

                    string posdSubDetailIdExpr = hasPOSD_Table ? "ISNULL(POSD.ProductionOrderSubDetailID, 0)" : "0";
                    string posdJoin = hasPOSD_Table ? "LEFT JOIN ProductionOrderSubDetail POSD ON POD.ProductionOrderDetailID = POSD.ProductionOrderDetailID AND ISNULL(POSD.IsActive, 1) = 1" : "";

                    bool hasDCMD_Table = DoesTableExist(con, "DesignColorMatchingDetails");
                    bool hasDEFM_Table = DoesTableExist(con, "DesignEntryFormMaster");

                    string colourMatchingIDExpr = hasPOD_ColourMatchingID ? "ISNULL(POD.ColourMatchingID, 0)" : "0";
                    string photoPathExpr = "''";
                    if (hasDCMD_Table && hasDEFM_Table)
                    {
                        photoPathExpr = "ISNULL(NULLIF(DCMD.ColorMatchingPhoto, ''), ISNULL(DEFM.PhotoOfDesign, ''))";
                    }
                    else if (hasDCMD_Table)
                    {
                        photoPathExpr = "ISNULL(DCMD.ColorMatchingPhoto, '')";
                    }
                    else if (hasDEFM_Table)
                    {
                        photoPathExpr = "ISNULL(DEFM.PhotoOfDesign, '')";
                    }

                    string extraJoins = "";
                    if (hasDCMD_Table && hasPOD_ColourMatchingID)
                    {
                        extraJoins += " LEFT JOIN DesignColorMatchingDetails DCMD ON POD.ColourMatchingID = DCMD.DesignColorMatchingDetailsID";
                    }
                    if (hasDEFM_Table && hasPOD_DesignNo)
                    {
                        extraJoins += " LEFT JOIN DesignEntryFormMaster DEFM ON REPLACE(REPLACE(POD.DesignNo, ' ', ''), '-', '') = REPLACE(REPLACE(DEFM.DesignNo, ' ', ''), '-', '')";
                    }

                    bool hasPTH_Table = DoesTableExist(con, "ProcessTransactionHistory");
                    bool hasPTH_SlipNo = hasPTH_Table && DoesColumnExist(con, null, "ProcessTransactionHistory", "SlipNo");
                    bool hasPTH_POSD = hasPTH_Table && DoesColumnExist(con, null, "ProcessTransactionHistory", "ProductionOrderSubDetailID");
                    bool hasPTH_POD = hasPTH_Table && DoesColumnExist(con, null, "ProcessTransactionHistory", "ProductionOrderDetailID");

                    if (hasPTH_Table && !hasPTH_SlipNo)
                    {
                        try
                        {
                            using (SqlCommand alterCmd = new SqlCommand("IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ProcessTransactionHistory' AND COLUMN_NAME = 'SlipNo') ALTER TABLE ProcessTransactionHistory ADD SlipNo NVARCHAR(100) NULL;", con))
                            {
                                alterCmd.ExecuteNonQuery();
                                hasPTH_SlipNo = true;
                            }
                        }
                        catch { }
                    }

                    string issuedPcsExpr = "0";
                    if (hasPTH_Table)
                    {
                        string pthCondition = "1=0";
                        if (hasPOSD_Table && hasPTH_POSD)
                        {
                            pthCondition = "POSD.ProductionOrderSubDetailID > 0 AND PTH.ProductionOrderSubDetailID = POSD.ProductionOrderSubDetailID";
                        }
                        
                        if (hasPTH_POD)
                        {
                            string podMatch = "PTH.ProductionOrderDetailID = POD.ProductionOrderDetailID";
                            if (hasPTH_SlipNo && (hasPOSD_SlipNo || hasPOD_SlipNo) && !string.IsNullOrEmpty(slipNoExpr) && slipNoExpr != "''")
                            {
                                podMatch += " AND PTH.SlipNo = " + slipNoExpr;
                            }
                            if (hasPOSD_Table && hasPTH_POSD)
                            {
                                pthCondition = string.Format("(({0}) OR (ISNULL(POSD.ProductionOrderSubDetailID, 0) = 0 AND {1}))", pthCondition, podMatch);
                            }
                            else
                            {
                                pthCondition = podMatch;
                            }
                        }

                        bool hasPTH_TxnType = hasPTH_Table && DoesColumnExist(con, null, "ProcessTransactionHistory", "TransactionType");
                        string pthQtyExpr = hasPTH_TxnType 
                            ? "(CASE WHEN PTH.TransactionType = 'ISSUE' THEN PTH.Qty WHEN PTH.TransactionType IN ('RECEIVE', 'JOBBER_RECEIVE', 'RETURN_RECEIVE') THEN -PTH.Qty ELSE 0 END)" 
                            : "PTH.Qty";

                        issuedPcsExpr = string.Format(@"ISNULL((
                            SELECT SUM({2}) 
                            FROM ProcessTransactionHistory PTH 
                            WHERE PTH.ProcessID = {0} 
                              AND ({1})
                        ), 0)", buttaProcID, pthCondition, pthQtyExpr);
                    }

                    string sql = string.Format(@"
                SELECT 
                    POM.ProductionOrderID,
                    POD.ProductionOrderDetailID,
                    {0} AS ProductionOrderSubDetailID,
                    ISNULL(POM.ProductionOrderNo, CAST(POM.ProductionOrderID AS NVARCHAR(50))) AS ProductionOrderNo,
                    CONVERT(VARCHAR(10), {1}, 103) AS OrderDate,
                    {2} AS JobberID,
                    {3} AS JobberName,
                    {4} AS PartyName,
                    {5} AS SlipNo,
                    {6} AS MachineNumber,
                    {7} AS DesignNo,
                    {8} AS ColourMatchingName,
                    {9} AS PCS,
                    {15} AS IssuedPCS,
                    'PENDING' AS Status,
                    {10} AS SONumber,
                    {12} AS ColourMatchingID,
                    {13} AS PhotoPath,
                    {16} AS Barcode
                FROM ProductionOrderMaster POM
                INNER JOIN ProductionOrderDetail POD ON POM.ProductionOrderID = POD.ProductionOrderID
                {11}
                {14}
                WHERE 1 = 1",
                        posdSubDetailIdExpr,
                        orderDateExpr,
                        jobberIDExpr,
                        jobberNameExpr,
                        partyNameExpr,
                        slipNoExpr,
                        machineExpr,
                        designNoExpr,
                        colourMatchingExpr,
                        pcsExpr,
                        soNumberExpr,
                        posdJoin,
                        colourMatchingIDExpr,
                        photoPathExpr,
                        extraJoins,
                        issuedPcsExpr,
                        barcodeExpr
                    );

                    if (jobberID > 0 && hasPOM_JobWorkerID)
                        sql += " AND POM.JobWorkerID = @JobberID";
                    if (productionOrderID > 0)
                        sql += " AND POM.ProductionOrderID = @ProductionOrderID";
                    if (!string.IsNullOrEmpty(slipNo))
                    {
                        if (hasPOSD_SlipNo && hasPOD_SlipNo)
                            sql += " AND ((POSD.SlipNo LIKE '%' + @SlipNo + '%') OR (POD.SlipNo LIKE '%' + @SlipNo + '%'))";
                        else if (hasPOSD_SlipNo)
                            sql += " AND (POSD.SlipNo LIKE '%' + @SlipNo + '%')";
                        else if (hasPOD_SlipNo)
                            sql += " AND (POD.SlipNo LIKE '%' + @SlipNo + '%')";
                    }
                    if (!string.IsNullOrEmpty(barcode))
                    {
                        if (hasPOSD_Barcode && hasPOD_Barcode)
                            sql += " AND ((POSD.Barcode LIKE '%' + @Barcode + '%') OR (POD.Barcode LIKE '%' + @Barcode + '%'))";
                        else if (hasPOSD_Barcode)
                            sql += " AND (POSD.Barcode LIKE '%' + @Barcode + '%')";
                        else if (hasPOD_Barcode)
                            sql += " AND (POD.Barcode LIKE '%' + @Barcode + '%')";
                    }
                    if (!string.IsNullOrEmpty(designNo) && hasPOD_DesignNo)
                    {
                        sql += @" AND (
                            POD.DesignNo LIKE '%' + @DesignNo + '%'
                            OR REPLACE(REPLACE(REPLACE(POD.DesignNo, ' ', ''), '-', ''), 'O', '0') LIKE '%' + REPLACE(REPLACE(REPLACE(@DesignNo, ' ', ''), '-', ''), 'O', '0') + '%'
                            OR REPLACE(REPLACE(REPLACE(POD.DesignNo, ' ', ''), '-', ''), '0', 'O') LIKE '%' + REPLACE(REPLACE(REPLACE(@DesignNo, ' ', ''), '-', ''), '0', 'O') + '%'
                        )";
                    }
                    if (machineNumber > 0 && hasPOSD_MachineNumber)
                        sql += " AND (POSD.MachineNumber = @MachineNumber)";
                    if (!string.IsNullOrEmpty(fromDate) && hasPOM_OrderDate)
                        sql += " AND POM.ProductionOrderDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate) && hasPOM_OrderDate)
                        sql += " AND POM.ProductionOrderDate <= @ToDate";

                    sql += " ORDER BY POM.ProductionOrderID DESC, POD.ProductionOrderDetailID ASC" + (hasPOSD_Table ? ", POSD.ProductionOrderSubDetailID ASC" : "");

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        if (jobberID > 0 && hasPOM_JobWorkerID) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (productionOrderID > 0) cmd.Parameters.AddWithValue("@ProductionOrderID", productionOrderID);
                        if (!string.IsNullOrEmpty(slipNo) && (hasPOSD_SlipNo || hasPOD_SlipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrEmpty(barcode) && (hasPOSD_Barcode || hasPOD_Barcode)) cmd.Parameters.AddWithValue("@Barcode", barcode.Trim());
                        if (!string.IsNullOrEmpty(designNo) && hasPOD_DesignNo) cmd.Parameters.AddWithValue("@DesignNo", designNo.Trim());
                        if (machineNumber > 0 && hasPOSD_MachineNumber) cmd.Parameters.AddWithValue("@MachineNumber", machineNumber);
                        if (!string.IsNullOrEmpty(fromDate) && hasPOM_OrderDate) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate));
                        if (!string.IsNullOrEmpty(toDate) && hasPOM_OrderDate) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            int itemIndex = 1;
                            while (dr.Read())
                            {
                                string rawSlip = dr["SlipNo"] != DBNull.Value ? dr["SlipNo"].ToString().Trim() : "";
                                string bCode = dr["Barcode"] != DBNull.Value ? dr["Barcode"].ToString().Trim() : "";
                                long pDetID = dr["ProductionOrderDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderDetailID"]) : 0;
                                long pSubID = dr["ProductionOrderSubDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderSubDetailID"]) : 0;
                                string soNo = dr["SONumber"] != DBNull.Value ? dr["SONumber"].ToString().Trim() : "";
                                if (string.IsNullOrEmpty(soNo)) soNo = "SO14";
                                if (string.IsNullOrEmpty(rawSlip))
                                {
                                    long numId = pSubID > 0 ? pSubID : (pDetID > 0 ? pDetID : itemIndex);
                                    rawSlip = soNo + "-01-" + (numId % 100).ToString("D2");
                                }

                                string rawPhoto = dr["PhotoPath"] != DBNull.Value ? dr["PhotoPath"].ToString().Trim() : "";
                                string normalizedPhoto = "";
                                if (!string.IsNullOrEmpty(rawPhoto))
                                {
                                    string clean = rawPhoto.Replace("\\", "/");
                                    if (clean.StartsWith("~/")) clean = clean.Substring(2);
                                    if (!clean.StartsWith("/")) clean = "/" + clean;
                                    normalizedPhoto = clean;
                                }

                                decimal totalPcs = dr["PCS"] != DBNull.Value ? Convert.ToDecimal(dr["PCS"]) : 0;
                                decimal issuedPcs = dr["IssuedPCS"] != DBNull.Value ? Convert.ToDecimal(dr["IssuedPCS"]) : 0;
                                if (issuedPcs < 0) issuedPcs = 0;
                                decimal pendingPcs = totalPcs - issuedPcs;
                                if (pendingPcs <= 0)
                                {
                                    // If issued cumulative history equals or exceeds order total, retain pendingPcs as totalPcs so slip remains actionable
                                    pendingPcs = totalPcs;
                                }

                                list.Add(new JobberToProcessItemDto
                                {
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = pDetID,
                                    ProductionOrderSubDetailID = pSubID,
                                    ProductionOrderNo = dr["ProductionOrderNo"].ToString(),
                                    OrderDate = dr["OrderDate"].ToString(),
                                    JobberID = Convert.ToInt64(dr["JobberID"]),
                                    JobberName = dr["JobberName"].ToString(),
                                    PartyName = dr["PartyName"].ToString(),
                                    SlipNo = rawSlip,
                                    Barcode = bCode,
                                    MachineNumber = Convert.ToInt32(dr["MachineNumber"]),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    ProductionPCS = totalPcs,
                                    JobberStockPCS = totalPcs,
                                    AlreadyIssuedPCS = issuedPcs,
                                    PendingIssuePCS = pendingPcs,
                                    IssuePCS = pendingPcs,
                                    Status = dr["Status"].ToString(),
                                    PhotoPath = normalizedPhoto,
                                    SONumber = soNo
                                });
                                itemIndex++;
                            }
                        }
                    }
                    WriteJsonResponse(list);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveJobberToButtaCutting(long jobberID, string jobType, string challanDate, List<ProcessItemInput> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "No item slips provided for allotment."));
                    return;
                }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    if (jobberID <= 0)
                    {
                        WriteJsonResponse(new GenericApiResponse(false, "Please select Job Worker / Jobber."));
                        return;
                    }

                    int buttaProcID = GetButtaCuttingProcessID(con);
                    if (buttaProcID <= 0) buttaProcID = 1;

                    bool hasPOD_ColourName = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatchingName");
                    bool hasPOD_ColourID = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatchingID");
                    bool hasPOD_Colour = DoesColumnExist(con, null, "ProductionOrderDetail", "ColourMatching");
                    bool hasPOD_SOID = DoesColumnExist(con, null, "ProductionOrderDetail", "SaleOrderID") || DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderID");
                    bool hasPOD_SODetID = DoesColumnExist(con, null, "ProductionOrderDetail", "SaleOrderDetailID") || DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderDetailID");
                    bool hasPOSD_Table = DoesTableExist(con, "ProductionOrderSubDetail");
                    bool hasPOSD_SOSTDetID = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "SaleOrderSubDetailID");
                    bool hasPOSD_Barcode = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "Barcode");
                    bool hasPOD_Barcode = DoesColumnExist(con, null, "ProductionOrderDetail", "Barcode");
                    bool hasPOSD_MachineNumber = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "MachineNumber");
                    bool hasPOSD_SlipNo = hasPOSD_Table && DoesColumnExist(con, null, "ProductionOrderSubDetail", "SlipNo");
                    bool hasPOD_SlipNo = DoesColumnExist(con, null, "ProductionOrderDetail", "SlipNo");

                    string colourNameExpr = hasPOD_ColourName ? "ISNULL(POD.ColourMatchingName, '')" : (hasPOD_Colour ? "ISNULL(POD.ColourMatching, '')" : "''");
                    string colourIDExpr = hasPOD_ColourID ? "ISNULL(POD.ColourMatchingID, 0)" : "0";
                    string soIDExpr = DoesColumnExist(con, null, "ProductionOrderDetail", "SaleOrderID") ? "ISNULL(POD.SaleOrderID, 0)" : (DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderID") ? "ISNULL(POD.SalesOrderID, 0)" : "0");
                    string soDetIDExpr = DoesColumnExist(con, null, "ProductionOrderDetail", "SaleOrderDetailID") ? "ISNULL(POD.SaleOrderDetailID, 0)" : (DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderDetailID") ? "ISNULL(POD.SalesOrderDetailID, 0)" : "0");
                    string soSubDetIDExpr = hasPOSD_SOSTDetID ? "ISNULL(POSD.SaleOrderSubDetailID, 0)" : "0";
                    string barcodeExpr = (hasPOSD_Barcode && hasPOD_Barcode) ? "ISNULL(NULLIF(POSD.Barcode, 0), ISNULL(POD.Barcode, 0))" : (hasPOSD_Barcode ? "ISNULL(POSD.Barcode, 0)" : (hasPOD_Barcode ? "ISNULL(POD.Barcode, 0)" : "0"));
                    string slipNoExpr = (hasPOSD_SlipNo && hasPOD_SlipNo) ? "ISNULL(NULLIF(POSD.SlipNo, ''), ISNULL(POD.SlipNo, ''))" : (hasPOSD_SlipNo ? "ISNULL(POSD.SlipNo, '')" : (hasPOD_SlipNo ? "ISNULL(POD.SlipNo, '')" : "''"));

                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            string txnNo = "";
                            using (SqlCommand cmdSeq = new SqlCommand("SELECT ISNULL(MAX(ProcessTransactionID), 0) + 1 FROM ProcessTransactionMaster WHERE ProcessID = @ProcessID OR ProcessTransactionNo LIKE 'BC%'", con, trans))
                            {
                                cmdSeq.Parameters.AddWithValue("@ProcessID", buttaProcID);
                                long nextSeq = Convert.ToInt64(cmdSeq.ExecuteScalar());
                                txnNo = "BC" + nextSeq.ToString("D4");
                            }

                            string jobberName = "";
                            using (SqlCommand cmdJ = new SqlCommand("SELECT PartyName FROM JobberMaster WHERE Id = @JobberID", con, trans))
                            {
                                cmdJ.Parameters.AddWithValue("@JobberID", jobberID);
                                object val = cmdJ.ExecuteScalar();
                                if (val != null && val != DBNull.Value) jobberName = val.ToString();
                            }

                            long fromPartyID = 1;
                            string fromPartyName = "NIKUNJ TEXTILES";
                            try
                            {
                                using (SqlCommand cmdFP = new SqlCommand("SELECT TOP 1 PartyId, PartyName FROM PartyMaster " + (secContext.CompanyId > 0 ? "WHERE CompanyId = @CompanyId " : "") + "ORDER BY PartyId ASC", con, trans))
                                {
                                    if (secContext.CompanyId > 0) cmdFP.Parameters.AddWithValue("@CompanyId", secContext.CompanyId);
                                    using (SqlDataReader drFP = cmdFP.ExecuteReader())
                                    {
                                        if (drFP.Read())
                                        {
                                            fromPartyID = Convert.ToInt64(drFP["PartyId"]);
                                            if (drFP["PartyName"] != DBNull.Value && !string.IsNullOrEmpty(drFP["PartyName"].ToString()))
                                            {
                                                fromPartyName = drFP["PartyName"].ToString().Trim();
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }

                            DateTime dateTxn = string.IsNullOrEmpty(challanDate) ? DateTime.Now : DateTime.Parse(challanDate);

                            long masterID = 0;
                            string sqlMaster = @"
                        INSERT INTO ProcessTransactionMaster 
                        (
                            ProcessTransactionNo, 
                            ProcessID, 
                            TransactionType, 
                            TransactionDate, 
                            FromPartyID, 
                            FromPartyName, 
                            ToPartyID, 
                            ToPartyName, 
                            Remark, 
                            CompanyId, 
                            CreatedByUserID, 
                            CreatedDateTime, 
                            IsActive
                        )
                        VALUES 
                        (
                            @ProcessTransactionNo, 
                            @ProcessID, 
                            'ISSUE', 
                            @TransactionDate, 
                            @FromPartyID, 
                            @FromPartyName, 
                            @ToPartyID, 
                            @ToPartyName, 
                            @Remark, 
                            @CompanyId, 
                            @CreatedByUserID, 
                            GETDATE(), 
                            1
                        );
                        SELECT SCOPE_IDENTITY();";

                            using (SqlCommand cmdM = new SqlCommand(sqlMaster, con, trans))
                            {
                                cmdM.Parameters.AddWithValue("@ProcessTransactionNo", txnNo);
                                cmdM.Parameters.AddWithValue("@ProcessID", buttaProcID);
                                cmdM.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                cmdM.Parameters.AddWithValue("@FromPartyID", fromPartyID);
                                cmdM.Parameters.AddWithValue("@FromPartyName", fromPartyName);
                                cmdM.Parameters.AddWithValue("@ToPartyID", jobberID);
                                cmdM.Parameters.AddWithValue("@ToPartyName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                cmdM.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(jobType) ? "BORDER AND BUTTA CUTTING BOTH" : jobType);
                                cmdM.Parameters.AddWithValue("@CompanyId", secContext.CompanyId);
                                cmdM.Parameters.AddWithValue("@CreatedByUserID", secContext.UserAccountId);

                                masterID = Convert.ToInt64(cmdM.ExecuteScalar());
                            }

                            foreach (var item in items)
                            {
                                if (item.PCS <= 0) continue;

                                string slipNo = item.SlipNo ?? "";
                                string designNo = "";
                                string colourMatchingName = "";
                                int colourMatchingID = 0;
                                int machineNumber = 1;
                                long saleOrderID = 0;
                                long saleOrderDetailID = 0;
                                long saleOrderSubDetailID = 0;
                                long barcode = 0;

                                if (item.ProductionOrderDetailID > 0)
                                {
                                    try
                                    {
                                        string queryInfo = string.Format(@"
                                    SELECT TOP 1
                                        ISNULL(POD.ProductionOrderID, 0) AS ProductionOrderID,
                                        ISNULL(POD.DesignNo, '') AS DesignNo,
                                        {0} AS ColourMatchingName,
                                        {1} AS ColourMatchingID,
                                        ISNULL(POSD.MachineNumber, 1) AS MachineNumber,
                                        {2} AS SaleOrderID,
                                        {3} AS SaleOrderDetailID,
                                        {4} AS SaleOrderSubDetailID,
                                        {5} AS Barcode,
                                        {6} AS SlipNo
                                    FROM ProductionOrderDetail POD WITH (NOLOCK)
                                    LEFT JOIN ProductionOrderSubDetail POSD WITH (NOLOCK) 
                                        ON POD.ProductionOrderDetailID = POSD.ProductionOrderDetailID 
                                        AND (@POSDID = 0 OR POSD.ProductionOrderSubDetailID = @POSDID)
                                    WHERE POD.ProductionOrderDetailID = @PODID
                                    ORDER BY CASE WHEN POSD.ProductionOrderSubDetailID = @POSDID THEN 0 ELSE 1 END",
                                            colourNameExpr, colourIDExpr, soIDExpr, soDetIDExpr, soSubDetIDExpr, barcodeExpr, slipNoExpr);

                                        using (SqlCommand cmdI = new SqlCommand(queryInfo, con, trans))
                                        {
                                            cmdI.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                                            cmdI.Parameters.AddWithValue("@POSDID", item.ProductionOrderSubDetailID);
                                            using (SqlDataReader dr = cmdI.ExecuteReader())
                                            {
                                                if (dr.Read())
                                                {
                                                    if (item.ProductionOrderID <= 0 && dr["ProductionOrderID"] != DBNull.Value)
                                                    {
                                                        item.ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]);
                                                    }
                                                    designNo = dr["DesignNo"] != DBNull.Value ? dr["DesignNo"].ToString().Trim() : "";
                                                    colourMatchingName = dr["ColourMatchingName"] != DBNull.Value ? dr["ColourMatchingName"].ToString().Trim() : "";
                                                    if (dr["ColourMatchingID"] != DBNull.Value) int.TryParse(dr["ColourMatchingID"].ToString(), out colourMatchingID);
                                                    if (dr["MachineNumber"] != DBNull.Value) int.TryParse(dr["MachineNumber"].ToString(), out machineNumber);
                                                    if (machineNumber <= 0) machineNumber = 1;
                                                    if (dr["SaleOrderID"] != DBNull.Value) long.TryParse(dr["SaleOrderID"].ToString(), out saleOrderID);
                                                    if (dr["SaleOrderDetailID"] != DBNull.Value) long.TryParse(dr["SaleOrderDetailID"].ToString(), out saleOrderDetailID);
                                                    if (dr["SaleOrderSubDetailID"] != DBNull.Value) long.TryParse(dr["SaleOrderSubDetailID"].ToString(), out saleOrderSubDetailID);
                                                    if (dr["Barcode"] != DBNull.Value) long.TryParse(dr["Barcode"].ToString().Trim(), out barcode);
                                                    if (string.IsNullOrEmpty(slipNo) && dr["SlipNo"] != DBNull.Value)
                                                    {
                                                        slipNo = dr["SlipNo"].ToString().Trim();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                if (string.IsNullOrEmpty(slipNo))
                                {
                                    slipNo = "SO18-01-" + (item.ProductionOrderSubDetailID > 0 ? (item.ProductionOrderSubDetailID % 100).ToString("D2") : (item.ProductionOrderDetailID % 100).ToString("D2"));
                                }

                                long detailID = 0;
                                string sqlDetail = @"
                            INSERT INTO ProcessTransactionDetail 
                            (
                                ProcessTransactionID, 
                                ProductionOrderID, 
                                ProductionOrderDetailID, 
                                ProductionOrderSubDetailID, 
                                SaleOrderID,
                                SaleOrderDetailID,
                                SaleOrderSubDetailID, 
                                SlipNo, 
                                Barcode,
                                DesignNo, 
                                ColourMatchingID,
                                ColourMatchingName, 
                                MachineNumber, 
                                PCS, 
                                IssuePCS, 
                                ReceivePCS, 
                                PendingPCS, 
                                Remark, 
                                CompanyId, 
                                CreatedByUserID, 
                                CreatedDateTime, 
                                IsActive
                            )
                            VALUES 
                            (
                                @MasterID, 
                                @ProdOrderID, 
                                @ProdOrderDetailID, 
                                @ProdOrderSubDetailID, 
                                @SaleOrderID,
                                @SaleOrderDetailID,
                                @SaleOrderSubDetailID, 
                                @SlipNo, 
                                @Barcode, 
                                @DesignNo, 
                                @ColourMatchingID, 
                                @ColourMatchingName, 
                                @MachineNumber, 
                                @PCS, 
                                @IssuePCS, 
                                0, 
                                @IssuePCS, 
                                @Remark, 
                                @CompanyId, 
                                @CreatedByUserID, 
                                GETDATE(), 
                                1
                            );
                            SELECT SCOPE_IDENTITY();";

                                using (SqlCommand cmdD = new SqlCommand(sqlDetail, con, trans))
                                {
                                    cmdD.Parameters.AddWithValue("@MasterID", masterID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdD.Parameters.AddWithValue("@SaleOrderID", saleOrderID);
                                    cmdD.Parameters.AddWithValue("@SaleOrderDetailID", saleOrderDetailID);
                                    cmdD.Parameters.AddWithValue("@SaleOrderSubDetailID", saleOrderSubDetailID);
                                    cmdD.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdD.Parameters.AddWithValue("@Barcode", barcode);
                                    cmdD.Parameters.AddWithValue("@DesignNo", designNo);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingID", colourMatchingID);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingName", colourMatchingName);
                                    cmdD.Parameters.AddWithValue("@MachineNumber", machineNumber);
                                    cmdD.Parameters.AddWithValue("@PCS", item.PCS);
                                    cmdD.Parameters.AddWithValue("@IssuePCS", item.PCS);
                                    cmdD.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(jobType) ? "BUTTA CUTTING ISSUE" : jobType);
                                    cmdD.Parameters.AddWithValue("@CompanyId", secContext.CompanyId);
                                    cmdD.Parameters.AddWithValue("@CreatedByUserID", secContext.UserAccountId);

                                    detailID = Convert.ToInt64(cmdD.ExecuteScalar());
                                }

                                string sqlHistory = @"
                            INSERT INTO ProcessTransactionHistory 
                            (
                                ProcessTransactionDetailID, 
                                ProductionOrderID, 
                                ProductionOrderDetailID, 
                                ProductionOrderSubDetailID, 
                                ProcessID, 
                                TransactionType, 
                                TransactionDate, 
                                Qty, 
                                MachineNumber, 
                                SlipNo, 
                                Barcode, 
                                JobberID, 
                                JobberName, 
                                Remark, 
                                CompanyId, 
                                CreatedByUserID, 
                                CreatedDateTime
                            )
                            VALUES 
                            (
                                @DetailID, 
                                @ProdOrderID, 
                                @ProdOrderDetailID, 
                                @ProdOrderSubDetailID, 
                                @ProcessID, 
                                'ISSUE', 
                                @TransactionDate, 
                                @Qty, 
                                @MachineNumber, 
                                @SlipNo, 
                                @Barcode, 
                                @JobberID, 
                                @JobberName, 
                                @Remark, 
                                @CompanyId, 
                                @CreatedByUserID, 
                                GETDATE()
                            );";

                                using (SqlCommand cmdH = new SqlCommand(sqlHistory, con, trans))
                                {
                                    cmdH.Parameters.AddWithValue("@DetailID", detailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdH.Parameters.AddWithValue("@ProcessID", buttaProcID);
                                    cmdH.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                    cmdH.Parameters.AddWithValue("@Qty", item.PCS);
                                    cmdH.Parameters.AddWithValue("@MachineNumber", machineNumber);
                                    cmdH.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdH.Parameters.AddWithValue("@Barcode", barcode);
                                    cmdH.Parameters.AddWithValue("@JobberID", jobberID);
                                    cmdH.Parameters.AddWithValue("@JobberName", string.IsNullOrEmpty(jobberName) ? "JOBBER" : jobberName);
                                    cmdH.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(jobType) ? "BUTTA CUTTING ISSUE" : jobType);
                                    cmdH.Parameters.AddWithValue("@CompanyId", secContext.CompanyId);
                                    cmdH.Parameters.AddWithValue("@CreatedByUserID", secContext.UserAccountId);

                                    cmdH.ExecuteNonQuery();
                                }
                            }

                            try
                            {
                                AuditLogHelper.Log(
                                    module: "Jobber To Butta Cutting",
                                    action: "CREATE",
                                    tableName: "ProcessTransactionMaster",
                                    recordId: masterID.ToString(),
                                    description: $"Created Butta Cutting Allotment #{txnNo}",
                                    existingConn: con,
                                    existingTran: trans
                                );
                            }
                            catch { }

                            trans.Commit();
                            WriteJsonResponse(new GenericApiResponse
                            {
                                Status = true,
                                Success = true,
                                TransactionNo = txnNo,
                                Message = "Butta Cutting Allotment Saved Successfully!"
                            });
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, "Error saving allotment: " + ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetJobberToButtaCuttingList(string fromDate, string toDate, long jobberID, string searchNo)
        {
            try
            {
                var list = new List<JobberToProcessListDto>();
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    int dynamicProcID = GetButtaCuttingProcessID(con);

                    string sql = @"
                SELECT 
                    PTM.ProcessTransactionID,
                    PTM.ProcessTransactionNo,
                    CONVERT(VARCHAR(10), PTM.TransactionDate, 103) AS TransactionDateStr,
                    PTM.TransactionDate,
                    PTM.TransactionType,
                    PTM.ProcessID,
                    ISNULL(PM.ProcessName, 'BUTTA CUTTING') AS ProcessName,
                    CASE WHEN PTM.TransactionType = 'RECEIVE' AND ISNULL(PTM.FromPartyID, 0) > 0 THEN PTM.FromPartyID ELSE PTM.ToPartyID END AS JobberID,
                    CASE WHEN PTM.TransactionType = 'RECEIVE' AND ISNULL(PTM.FromPartyName, '') <> '' THEN PTM.FromPartyName ELSE PTM.ToPartyName END AS JobberName,
                    PTM.Remark AS JobType,
                    ISNULL(COUNT(PTD.ProcessTransactionDetailID), 0) AS TotalItems,
                    ISNULL(SUM(PTD.IssuePCS), 0) AS TotalIssuePCS,
                    ISNULL(SUM(PTD.ReceivePCS), 0) AS TotalReceivePCS,
                    ISNULL(SUM(PTD.ShortPCS), 0) AS TotalShortPCS,
                    CASE 
                        WHEN PTM.TransactionType = 'RECEIVE' THEN 'RECEIVED'
                        WHEN ISNULL(SUM(PTD.ReceivePCS), 0) = 0 THEN 'ISSUED'
                        WHEN ISNULL(SUM(PTD.ReceivePCS), 0) >= ISNULL(SUM(PTD.IssuePCS), 0) THEN 'RECEIVED'
                        ELSE 'PARTIALLY RECEIVED'
                    END AS StatusText,
                    PTM.CreatedDateTime
                FROM ProcessTransactionMaster PTM
                LEFT JOIN ProcessMaster PM ON PTM.ProcessID = PM.ProcessID
                LEFT JOIN ProcessTransactionDetail PTD ON PTM.ProcessTransactionID = PTD.ProcessTransactionID AND ISNULL(PTD.IsActive, 1) = 1
                WHERE (PTM.ProcessID = @ProcessID OR PTM.ProcessTransactionNo LIKE 'BC%')
                  AND ISNULL(PTM.IsActive, 1) = 1";

                    if (jobberID > 0)
                        sql += " AND ((PTM.TransactionType = 'RECEIVE' AND PTM.FromPartyID = @JobberID) OR (PTM.TransactionType <> 'RECEIVE' AND PTM.ToPartyID = @JobberID))";
                    if (!string.IsNullOrEmpty(searchNo))
                        sql += " AND PTM.ProcessTransactionNo LIKE '%' + @SearchNo + '%'";
                    if (!string.IsNullOrEmpty(fromDate))
                        sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate))
                        sql += " AND PTM.TransactionDate <= @ToDate";

                    sql += " GROUP BY PTM.ProcessTransactionID, PTM.ProcessTransactionNo, PTM.TransactionDate, PTM.TransactionType, PTM.ProcessID, PM.ProcessName, PTM.FromPartyID, PTM.FromPartyName, PTM.ToPartyID, PTM.ToPartyName, PTM.Remark, PTM.CreatedDateTime ORDER BY PTM.ProcessTransactionID DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcessID", dynamicProcID);
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (!string.IsNullOrEmpty(searchNo)) cmd.Parameters.AddWithValue("@SearchNo", searchNo);
                        if (!string.IsNullOrEmpty(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate));
                        if (!string.IsNullOrEmpty(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                string txnType = dr["TransactionType"] != DBNull.Value ? dr["TransactionType"].ToString() : "ISSUE";
                                decimal issuePcs = dr["TotalIssuePCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalIssuePCS"]) : 0;
                                decimal receivePcs = dr["TotalReceivePCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalReceivePCS"]) : 0;
                                decimal shortPcs = dr["TotalShortPCS"] != DBNull.Value ? Convert.ToDecimal(dr["TotalShortPCS"]) : 0;
                                decimal pendingPcs = issuePcs - receivePcs - shortPcs;
                                if (pendingPcs < 0) pendingPcs = 0;

                                list.Add(new JobberToProcessListDto
                                {
                                    ProcessTransactionID = Convert.ToInt64(dr["ProcessTransactionID"]),
                                    ProcessTransactionNo = dr["ProcessTransactionNo"].ToString(),
                                    TransactionDate = dr["TransactionDateStr"].ToString(),
                                    ProcessID = dr["ProcessID"] != DBNull.Value ? Convert.ToInt32(dr["ProcessID"]) : dynamicProcID,
                                    ProcessName = dr["ProcessName"].ToString(),
                                    TransactionType = txnType,
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    TotalItems = Convert.ToInt32(dr["TotalItems"]),
                                    TotalPCS = txnType == "RECEIVE" ? receivePcs : issuePcs,
                                    TotalIssuePCS = issuePcs,
                                    TotalReceivePCS = receivePcs,
                                    TotalShortPCS = shortPcs,
                                    PendingPCS = pendingPcs,
                                    StatusText = dr["StatusText"].ToString(),
                                    CreatedDateTime = dr["CreatedDateTime"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDateTime"]).ToString("dd/MM/yyyy HH:mm") : ""
                                });
                            }
                        }
                    }
                    WriteJsonResponse(list);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveButtaCuttingReceive(long issueProcessTransactionID, long jobberID, string receiveDate, List<ReceiveItemInput> items)
        {
            try
            {
                if (issueProcessTransactionID <= 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "Invalid Issue Challan ID."));
                    return;
                }
                if (items == null || items.Count == 0)
                {
                    WriteJsonResponse(new GenericApiResponse(false, "No item slips selected for receive."));
                    return;
                }

                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            long refProcessID = 0;
                            long origFromPartyID = 0;
                            string origFromPartyName = "";
                            long origToPartyID = 0;
                            string origToPartyName = "";
                            long companyIdVal = secContext.CompanyId;
                            long userIdVal = secContext.UserAccountId;

                            using (SqlCommand cmdOrig = new SqlCommand("SELECT ProcessID, FromPartyID, FromPartyName, ToPartyID, ToPartyName, CompanyId FROM ProcessTransactionMaster WHERE ProcessTransactionID = @RefID", con, tran))
                            {
                                cmdOrig.Parameters.AddWithValue("@RefID", issueProcessTransactionID);
                                using (SqlDataReader drOrig = cmdOrig.ExecuteReader())
                                {
                                    if (drOrig.Read())
                                    {
                                        refProcessID = drOrig["ProcessID"] != DBNull.Value ? Convert.ToInt64(drOrig["ProcessID"]) : 0;
                                        origFromPartyID = drOrig["FromPartyID"] != DBNull.Value ? Convert.ToInt64(drOrig["FromPartyID"]) : 0;
                                        origFromPartyName = drOrig["FromPartyName"].ToString();
                                        origToPartyID = drOrig["ToPartyID"] != DBNull.Value ? Convert.ToInt64(drOrig["ToPartyID"]) : 0;
                                        origToPartyName = drOrig["ToPartyName"].ToString();
                                        if (drOrig["CompanyId"] != DBNull.Value) companyIdVal = Convert.ToInt64(drOrig["CompanyId"]);
                                    }
                                }
                            }

                            if (refProcessID <= 0)
                            {
                                refProcessID = GetButtaCuttingProcessID(con);
                                if (refProcessID <= 0) refProcessID = 1;
                            }

                            string processName = "BUTTA CUTTING";
                            if (refProcessID > 0)
                            {
                                using (SqlCommand cmdP = new SqlCommand("SELECT ProcessName FROM ProcessMaster WHERE ProcessID = @PID", con, tran))
                                {
                                    cmdP.Parameters.AddWithValue("@PID", refProcessID);
                                    object pVal = cmdP.ExecuteScalar();
                                    if (pVal != null && pVal != DBNull.Value) processName = pVal.ToString().Trim();
                                }
                            }

                            long receiveFromPartyID = origToPartyID > 0 ? origToPartyID : jobberID;
                            string receiveFromPartyName = !string.IsNullOrEmpty(origToPartyName) ? origToPartyName : "PROCESSOR";
                            long receiveToPartyID = origFromPartyID > 0 ? origFromPartyID : jobberID;
                            string receiveToPartyName = !string.IsNullOrEmpty(origFromPartyName) ? origFromPartyName : "JOBBER";

                            foreach (var item in items)
                            {
                                if (item.ReceivePCS <= 0 && item.ShortPCS <= 0) continue;

                                decimal currentPending = 0;
                                string sqlCheck = @"
                            SELECT (IssuePCS - ISNULL(ReceivePCS, 0) - ISNULL(ShortPCS, 0)) AS PendingQty 
                            FROM ProcessTransactionDetail WITH (UPDLOCK)
                            WHERE ProcessTransactionDetailID = @DetailID AND ISNULL(IsActive, 1) = 1";

                                using (SqlCommand cmdChk = new SqlCommand(sqlCheck, con, tran))
                                {
                                    cmdChk.Parameters.AddWithValue("@DetailID", item.IssueDetailID);
                                    object val = cmdChk.ExecuteScalar();
                                    if (val != null && val != DBNull.Value) currentPending = Convert.ToDecimal(val);
                                }

                                if ((item.ReceivePCS + item.ShortPCS) > currentPending)
                                {
                                    tran.Rollback();
                                    WriteJsonResponse(new GenericApiResponse(false, string.Format("Total requested Receive + Short PCS ({0}) exceeds current pending issue ({1}) for Slip '{2}'.", item.ReceivePCS + item.ShortPCS, currentPending, item.SlipNo)));
                                    return;
                                }
                            }

                            string receiveTxnNo = "";
                            using (SqlCommand cmdSeq = new SqlCommand("SELECT ISNULL(MAX(ProcessTransactionID), 0) + 1 FROM ProcessTransactionMaster WHERE ProcessID = @PID AND TransactionType = 'RECEIVE'", con, tran))
                            {
                                cmdSeq.Parameters.AddWithValue("@PID", refProcessID);
                                long nextSeq = Convert.ToInt64(cmdSeq.ExecuteScalar());
                                receiveTxnNo = "BCR" + nextSeq.ToString("D4");
                            }

                            DateTime dateTxn = string.IsNullOrEmpty(receiveDate) ? DateTime.Now : DateTime.Parse(receiveDate);

                            string sqlMaster = @"
                        INSERT INTO ProcessTransactionMaster 
                        (
                            ProcessTransactionNo, ProcessID, TransactionType, TransactionDate,
                            FromPartyID, FromPartyName, ToPartyID, ToPartyName, ReferenceTransactionID, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                        )
                        VALUES 
                        (
                            @TxnNo, @ProcessID, 'RECEIVE', @TransactionDate,
                            @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, @RefTxnID, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                        );
                        SELECT SCOPE_IDENTITY();";

                            long masterID = 0;
                            using (SqlCommand cmdM = new SqlCommand(sqlMaster, con, tran))
                            {
                                cmdM.Parameters.AddWithValue("@TxnNo", receiveTxnNo);
                                cmdM.Parameters.AddWithValue("@ProcessID", refProcessID);
                                cmdM.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                cmdM.Parameters.AddWithValue("@FromPartyID", receiveFromPartyID);
                                cmdM.Parameters.AddWithValue("@FromPartyName", receiveFromPartyName);
                                cmdM.Parameters.AddWithValue("@ToPartyID", receiveToPartyID);
                                cmdM.Parameters.AddWithValue("@ToPartyName", receiveToPartyName);
                                cmdM.Parameters.AddWithValue("@RefTxnID", issueProcessTransactionID);
                                cmdM.Parameters.AddWithValue("@Remark", processName + " RECEIVE");
                                cmdM.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                cmdM.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                masterID = Convert.ToInt64(cmdM.ExecuteScalar());
                            }

                            foreach (var item in items)
                            {
                                if (item.ReceivePCS <= 0 && item.ShortPCS <= 0) continue;

                                string slipNo = item.SlipNo ?? "";
                                string designNo = "";
                                string colourMatchingName = "";
                                int colourMatchingID = 0;

                                if (item.ProductionOrderDetailID > 0)
                                {
                                    try
                                    {
                                        using (SqlCommand cmdI = new SqlCommand("SELECT TOP 1 DesignNo, ColourMatchingName, ColourMatchingID FROM ProductionOrderDetail WHERE ProductionOrderDetailID = @PODID", con, tran))
                                        {
                                            cmdI.Parameters.AddWithValue("@PODID", item.ProductionOrderDetailID);
                                            using (SqlDataReader dr = cmdI.ExecuteReader())
                                            {
                                                if (dr.Read())
                                                {
                                                    designNo = dr["DesignNo"].ToString();
                                                    colourMatchingName = dr["ColourMatchingName"].ToString();
                                                    colourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt32(dr["ColourMatchingID"]) : 0;
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                string sqlDetail = @"
                            INSERT INTO ProcessTransactionDetail 
                            (
                                ProcessTransactionID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                SlipNo, DesignNo, ColourMatchingID, ColourMatchingName, MachineNumber, PCS, IssuePCS, ReceivePCS, ShortPCS, PendingPCS, Remark, CompanyId, CreatedByUserID, CreatedDateTime, IsActive
                            )
                            VALUES 
                            (
                                @MasterID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                @SlipNo, @DesignNo, @ColourMatchingID, @ColourMatchingName, 1, @RecPCS, 0, @RecPCS, @ShortPCS, 0, @Remark, @CompanyId, @CreatedByUserID, GETDATE(), 1
                            );
                            SELECT SCOPE_IDENTITY();";

                                long detailID = 0;
                                using (SqlCommand cmdD = new SqlCommand(sqlDetail, con, tran))
                                {
                                    cmdD.Parameters.AddWithValue("@MasterID", masterID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdD.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdD.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdD.Parameters.AddWithValue("@DesignNo", designNo);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingID", colourMatchingID);
                                    cmdD.Parameters.AddWithValue("@ColourMatchingName", colourMatchingName);
                                    cmdD.Parameters.AddWithValue("@RecPCS", item.ReceivePCS);
                                    cmdD.Parameters.AddWithValue("@ShortPCS", item.ShortPCS);
                                    cmdD.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(item.Remark) ? processName + " RECEIVE" : item.Remark);
                                    cmdD.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdD.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    detailID = Convert.ToInt64(cmdD.ExecuteScalar());
                                }

                                string sqlHistory = @"
                            INSERT INTO ProcessTransactionHistory 
                            (
                                ProcessTransactionDetailID, ProductionOrderID, ProductionOrderDetailID, ProductionOrderSubDetailID,
                                ProcessID, TransactionType, TransactionDate, Qty, ShortQty, MachineNumber, SlipNo, 
                                JobberID, JobberName, FromPartyID, FromPartyName, ToPartyID, ToPartyName, ReferenceTransactionID, Remark, CompanyId, CreatedByUserID, CreatedDateTime
                            )
                            VALUES 
                            (
                                @DetailID, @ProdOrderID, @ProdOrderDetailID, @ProdOrderSubDetailID,
                                @ProcessID, 'RECEIVE', @TransactionDate, @Qty, @ShortQty, 1, @SlipNo, 
                                @JobberID, @JobberName, @FromPartyID, @FromPartyName, @ToPartyID, @ToPartyName, @RefTxnID, @Remark, @CompanyId, @CreatedByUserID, GETDATE()
                            );";

                                using (SqlCommand cmdH = new SqlCommand(sqlHistory, con, tran))
                                {
                                    cmdH.Parameters.AddWithValue("@DetailID", detailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderID", item.ProductionOrderID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderDetailID", item.ProductionOrderDetailID);
                                    cmdH.Parameters.AddWithValue("@ProdOrderSubDetailID", item.ProductionOrderSubDetailID);
                                    cmdH.Parameters.AddWithValue("@ProcessID", refProcessID);
                                    cmdH.Parameters.AddWithValue("@TransactionDate", dateTxn);
                                    cmdH.Parameters.AddWithValue("@Qty", item.ReceivePCS);
                                    cmdH.Parameters.AddWithValue("@ShortQty", item.ShortPCS);
                                    cmdH.Parameters.AddWithValue("@SlipNo", slipNo);
                                    cmdH.Parameters.AddWithValue("@JobberID", receiveToPartyID);
                                    cmdH.Parameters.AddWithValue("@JobberName", receiveToPartyName);
                                    cmdH.Parameters.AddWithValue("@FromPartyID", receiveFromPartyID);
                                    cmdH.Parameters.AddWithValue("@FromPartyName", receiveFromPartyName);
                                    cmdH.Parameters.AddWithValue("@ToPartyID", receiveToPartyID);
                                    cmdH.Parameters.AddWithValue("@ToPartyName", receiveToPartyName);
                                    cmdH.Parameters.AddWithValue("@RefTxnID", issueProcessTransactionID);
                                    cmdH.Parameters.AddWithValue("@Remark", string.IsNullOrEmpty(item.Remark) ? processName + " RECEIVE" : item.Remark);
                                    cmdH.Parameters.AddWithValue("@CompanyId", companyIdVal);
                                    cmdH.Parameters.AddWithValue("@CreatedByUserID", userIdVal);
                                    cmdH.ExecuteNonQuery();
                                }

                                if (item.IssueDetailID > 0)
                                {
                                    using (SqlCommand cmdUpd = new SqlCommand(@"
                                    UPDATE ProcessTransactionDetail 
                                    SET ReceivePCS = ISNULL(ReceivePCS, 0) + @RecQty,
                                        ShortPCS = ISNULL(ShortPCS, 0) + @ShortQty,
                                        PendingPCS = CASE WHEN (IssuePCS - (ISNULL(ReceivePCS, 0) + @RecQty + ISNULL(ShortPCS, 0) + @ShortQty)) < 0 THEN 0 ELSE (IssuePCS - (ISNULL(ReceivePCS, 0) + @RecQty + ISNULL(ShortPCS, 0) + @ShortQty)) END
                                    WHERE ProcessTransactionDetailID = @DetailID", con, tran))
                                    {
                                        cmdUpd.Parameters.AddWithValue("@RecQty", item.ReceivePCS);
                                        cmdUpd.Parameters.AddWithValue("@ShortQty", item.ShortPCS);
                                        cmdUpd.Parameters.AddWithValue("@DetailID", item.IssueDetailID);
                                        cmdUpd.ExecuteNonQuery();
                                    }
                                }
                            }

                            AuditLogHelper.Log(
                                module: "Butta Cutting Receive",
                                action: "CREATE",
                                tableName: "ProcessTransactionMaster",
                                recordId: masterID.ToString(),
                                description: $"Created Butta Cutting Receive #{receiveTxnNo}",
                                existingConn: con,
                                existingTran: tran
                            );

                            tran.Commit();
                            WriteJsonResponse(new GenericApiResponse
                            {
                                Status = true,
                                Success = true,
                                ReceiveChallanNo = receiveTxnNo,
                                Message = processName + " Receive Saved Successfully!"
                            });
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, "Error saving receive: " + ex.Message));
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void SaveReceiveJobberToButtaCutting(long issueProcessTransactionID, long jobberID, string receiveDate, List<ReceiveItemInput> items)
        {
            SaveButtaCuttingReceive(issueProcessTransactionID, jobberID, receiveDate, items);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void GetButtaCuttingStockList(long processorID, long jobberID, string soNumber, string slipNo, string designNo, int machineNumber, string fromDate, string toDate)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["sqlconnstr"].ConnectionString;
                var list = new List<ProcessStockItemDto>();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    var secContext = GetLoggedInUserSecurityContext(con);
                    if (secContext.IsJobber && secContext.LoggedInJobberID > 0)
                    {
                        jobberID = secContext.LoggedInJobberID;
                    }

                    int buttaProcID = GetButtaCuttingProcessID(con);

                    bool hasPOD_Table = DoesTableExist(con, "ProductionOrderDetail");
                    bool hasPOD_SalesOrderNo = hasPOD_Table && DoesColumnExist(con, null, "ProductionOrderDetail", "SalesOrderNo");

                    string soNumberExpr = hasPOD_SalesOrderNo ? "ISNULL(POD.SalesOrderNo, '')" : "''";
                    string podJoin = hasPOD_Table ? "LEFT JOIN ProductionOrderDetail POD ON PTD.ProductionOrderDetailID = POD.ProductionOrderDetailID" : "";

                    string sql = string.Format(@"
                SELECT 
                    PTM.ProcessID,
                    ISNULL(PM.ProcessName, 'BUTTA CUTTING') AS ProcessName,

                    PTM.FromPartyID AS ProcessorID,
                    ISNULL(PTM.FromPartyName, '') AS ProcessorName,

                    PTM.ToPartyID AS JobberID,
                    ISNULL(PTM.ToPartyName, '') AS JobberName,

                    PTD.ProductionOrderID,
                    PTD.ProductionOrderDetailID,
                    PTD.ProductionOrderSubDetailID,

                    ISNULL(PTD.SlipNo, '') AS SlipNo,
                    ISNULL(PTD.DesignNo, '') AS DesignNo,
                    ISNULL(PTD.MachineNumber, 1) AS MachineNumber,

                    ISNULL(PTD.ColourMatchingID, 0) AS ColourMatchingID,
                    ISNULL(PTD.ColourMatchingName, '') AS ColourMatchingName,

                    {0} AS SONumber,

                    ISNULL(PTD.Remark, PTM.Remark) AS JobType,

                    PTD.ProcessTransactionDetailID AS ReceiveDetailID,
                    PTM.ProcessTransactionID AS OriginalReceiveTransactionID,

                    ISNULL(PTD.ReceivePCS, 0) AS ReceivedPCS,
                    ISNULL(IB.IssuedBackPCS, 0) AS IssuedBackPCS,
                    ISNULL(PTD.ReceivePCS, 0) - ISNULL(IB.IssuedBackPCS, 0) AS AvailablePCS

                FROM ProcessTransactionMaster PTM

                INNER JOIN ProcessTransactionDetail PTD
                    ON PTM.ProcessTransactionID = PTD.ProcessTransactionID

                LEFT JOIN ProcessMaster PM
                    ON PTM.ProcessID = PM.ProcessID

                {1}

                LEFT JOIN
                (
                    SELECT 
                        PTM2.ReferenceTransactionID AS ReceiveTransactionID,
                        PTD2.ProductionOrderDetailID,
                        ISNULL(PTD2.ProductionOrderSubDetailID, 0) AS ProductionOrderSubDetailID,
                        ISNULL(PTD2.SlipNo, '') AS SlipNo,

                        SUM(ISNULL(PTD2.IssuePCS, 0)) AS IssuedBackPCS

                    FROM ProcessTransactionMaster PTM2

                    INNER JOIN ProcessTransactionDetail PTD2
                        ON PTM2.ProcessTransactionID = PTD2.ProcessTransactionID

                    WHERE PTM2.TransactionType = 'ISSUE'
                      AND ISNULL(PTM2.ReferenceTransactionID, 0) > 0
                      AND ISNULL(PTM2.IsActive, 1) = 1
                      AND ISNULL(PTD2.IsActive, 1) = 1

                    GROUP BY 
                        PTM2.ReferenceTransactionID,
                        PTD2.ProductionOrderDetailID,
                        ISNULL(PTD2.ProductionOrderSubDetailID, 0),
                        ISNULL(PTD2.SlipNo, '')
                ) IB
                    ON IB.ReceiveTransactionID = PTM.ProcessTransactionID
                   AND IB.ProductionOrderDetailID = PTD.ProductionOrderDetailID
                   AND IB.ProductionOrderSubDetailID = ISNULL(PTD.ProductionOrderSubDetailID, 0)
                   AND IB.SlipNo = ISNULL(PTD.SlipNo, '')

                WHERE PTM.TransactionType = 'RECEIVE'
                  AND (PTM.ProcessID = @ProcessID OR PTM.ProcessTransactionNo LIKE 'BCR%')
                  AND ISNULL(PTM.IsActive, 1) = 1
                  AND ISNULL(PTD.IsActive, 1) = 1", soNumberExpr, podJoin);

                    if (processorID > 0) sql += " AND PTM.FromPartyID = @ProcessorID";
                    if (jobberID > 0) sql += " AND PTM.ToPartyID = @JobberID";
                    if (!string.IsNullOrEmpty(slipNo)) sql += " AND PTD.SlipNo LIKE '%' + @SlipNo + '%'";
                    if (!string.IsNullOrEmpty(soNumber) && hasPOD_SalesOrderNo) sql += " AND POD.SalesOrderNo LIKE '%' + @SONumber + '%'";
                    if (!string.IsNullOrEmpty(designNo)) sql += " AND PTD.DesignNo LIKE '%' + @DesignNo + '%'";
                    if (machineNumber > 0) sql += " AND PTD.MachineNumber = @MachineNumber";
                    if (!string.IsNullOrEmpty(fromDate)) sql += " AND PTM.TransactionDate >= @FromDate";
                    if (!string.IsNullOrEmpty(toDate)) sql += " AND PTM.TransactionDate <= @ToDate";

                    sql += " ORDER BY PTM.ProcessTransactionID DESC, PTD.ProcessTransactionDetailID ASC";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcessID", buttaProcID);
                        if (processorID > 0) cmd.Parameters.AddWithValue("@ProcessorID", processorID);
                        if (jobberID > 0) cmd.Parameters.AddWithValue("@JobberID", jobberID);
                        if (!string.IsNullOrEmpty(slipNo)) cmd.Parameters.AddWithValue("@SlipNo", slipNo);
                        if (!string.IsNullOrEmpty(soNumber) && hasPOD_SalesOrderNo) cmd.Parameters.AddWithValue("@SONumber", soNumber);
                        if (!string.IsNullOrEmpty(designNo)) cmd.Parameters.AddWithValue("@DesignNo", designNo);
                        if (machineNumber > 0) cmd.Parameters.AddWithValue("@MachineNumber", machineNumber);
                        if (!string.IsNullOrEmpty(fromDate)) cmd.Parameters.AddWithValue("@FromDate", DateTime.Parse(fromDate));
                        if (!string.IsNullOrEmpty(toDate)) cmd.Parameters.AddWithValue("@ToDate", DateTime.Parse(toDate));

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            int srNo = 1;
                            while (dr.Read())
                            {
                                decimal receivedPcs = Convert.ToDecimal(dr["ReceivedPCS"]);
                                decimal issuedBackPcs = Convert.ToDecimal(dr["IssuedBackPCS"]);
                                decimal availablePcs = Convert.ToDecimal(dr["AvailablePCS"]);

                                if (availablePcs <= 0 && string.IsNullOrEmpty(slipNo) && string.IsNullOrEmpty(soNumber) && string.IsNullOrEmpty(designNo))
                                {
                                    continue;
                                }

                                list.Add(new ProcessStockItemDto
                                {
                                    SrNo = srNo++,
                                    ProcessID = Convert.ToInt32(dr["ProcessID"]),
                                    ProcessName = dr["ProcessName"].ToString(),
                                    ProcessorID = dr["ProcessorID"] != DBNull.Value ? Convert.ToInt64(dr["ProcessorID"]) : 0,
                                    ProcessorName = dr["ProcessorName"].ToString(),
                                    JobberID = dr["JobberID"] != DBNull.Value ? Convert.ToInt64(dr["JobberID"]) : 0,
                                    JobberName = dr["JobberName"].ToString(),
                                    ProductionOrderID = Convert.ToInt64(dr["ProductionOrderID"]),
                                    ProductionOrderDetailID = Convert.ToInt64(dr["ProductionOrderDetailID"]),
                                    ProductionOrderSubDetailID = dr["ProductionOrderSubDetailID"] != DBNull.Value ? Convert.ToInt64(dr["ProductionOrderSubDetailID"]) : 0,
                                    SlipNo = dr["SlipNo"].ToString(),
                                    DesignNo = dr["DesignNo"].ToString(),
                                    MachineNumber = dr["MachineNumber"] != DBNull.Value ? Convert.ToInt32(dr["MachineNumber"]) : 1,
                                    ColourMatchingID = dr["ColourMatchingID"] != DBNull.Value ? Convert.ToInt64(dr["ColourMatchingID"]) : 0,
                                    ColourMatchingName = dr["ColourMatchingName"].ToString(),
                                    SONumber = dr["SONumber"].ToString(),
                                    JobType = dr["JobType"].ToString(),
                                    ReceivedPCS = receivedPcs,
                                    IssuedBackPCS = issuedBackPcs,
                                    AvailablePCS = availablePcs,
                                    ProcessStockPCS = availablePcs,
                                    Status = "BUTTA CUTTING STOCK"
                                });
                            }
                        }
                    }
                    WriteJsonResponse(list);
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(new GenericApiResponse(false, ex.Message));
            }
        }
        #endregion
    }
}

