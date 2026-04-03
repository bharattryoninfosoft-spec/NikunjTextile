using NikunjTextile.Class;
using NikunjTextile.Class.API;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace NikunjTextile
{
    [WebService(Namespace = "http://tempuri.org/")]
    [ScriptService]  

    public class WebServiceMoblieAPI : WebService
    {
        [WebMethod]
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

                SqlCommand cmd = new SqlCommand(
                @"SELECT * FROM UserAccountMaster 
          WHERE UserAccountMobileNo=@username 
          AND UserAccountPassword=@password", con);

                cmd.Parameters.AddWithValue("@username", Username);
                cmd.Parameters.AddWithValue("@password", Password);

                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();

                JavaScriptSerializer js = new JavaScriptSerializer();
                object result;

                if (rdr.Read())
                {
                    string allowLogin = rdr["AllowLogin"].ToString();

                    if (allowLogin == "1")
                    {
                        result = new
                        {
                            success = true,
                            message = "Login Successfully",
                            data = new
                            {
                                UserAccountId = rdr["UserAccountId"].ToString(),
                                UserAccountMobileNo = rdr["UserAccountMobileNo"].ToString(),
                                UserAccountName = rdr["UserAccountName"].ToString(),
                                UserRole = rdr["UserRole"].ToString(),
                                UserAccountEmail = rdr["UserAccountEmail"].ToString(),
                                UserAccountProfile = rdr["UserAccountProfile"].ToString(),
                                AllowLogin = allowLogin
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
    }
}
