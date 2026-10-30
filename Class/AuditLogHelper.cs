using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using Newtonsoft.Json;

namespace NikunjTextile.Class
{
    /// <summary>
    /// Centralized Audit Log Helper for WeaveLaneSoft ERP.
    /// Provides synchronous, failsafe logging for CUD operations, Login/Logout, and Errors.
    /// Supports participating in existing SqlTransactions.
    /// </summary>
    public static class AuditLogHelper
    {
        private static bool _tablesChecked = false;
        private static readonly object _lockObj = new object();

        private static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["sqlconnstr"]?.ConnectionString;
        }

        public static void EnsureAuditTablesExist()
        {
            if (_tablesChecked) return;

            lock (_lockObj)
            {
                if (_tablesChecked) return;

                try
                {
                    string cs = GetConnectionString();
                    if (string.IsNullOrEmpty(cs)) return;

                    using (SqlConnection conn = new SqlConnection(cs))
                    {
                        conn.Open();

                        string sql = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLog]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[AuditLog]
                            (
                                [AuditLogID]        BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [LogDateTime]       DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
                                [UserAccountID]     INT NULL,
                                [UserName]          NVARCHAR(200) NULL,
                                [ModuleName]        NVARCHAR(100) NOT NULL,
                                [PageName]          NVARCHAR(200) NULL,
                                [ActionName]        NVARCHAR(100) NOT NULL,
                                [TableName]         NVARCHAR(200) NULL,
                                [RecordID]          NVARCHAR(200) NULL,
                                [Description]       NVARCHAR(1000) NULL,
                                [OldValue]          NVARCHAR(MAX) NULL,
                                [NewValue]          NVARCHAR(MAX) NULL,
                                [RequestURL]        NVARCHAR(1000) NULL,
                                [IPAddress]         NVARCHAR(100) NULL,
                                [UserAgent]         NVARCHAR(1000) NULL,
                                [Status]            NVARCHAR(30) NOT NULL DEFAULT 'Success',
                                [ErrorMessage]      NVARCHAR(MAX) NULL,
                                [ExceptionDetails]  NVARCHAR(MAX) NULL,
                                [SessionID]         NVARCHAR(200) NULL,
                                [CreatedDate]       DATETIME2(3) NOT NULL DEFAULT SYSDATETIME()
                            );
                        END;

                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LoginHistory]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[LoginHistory]
                            (
                                [LoginHistoryID]    BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [UserAccountID]     INT NULL,
                                [UserName]          NVARCHAR(200) NULL,
                                [LoginDateTime]     DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
                                [LogoutDateTime]    DATETIME2(3) NULL,
                                [IPAddress]         NVARCHAR(100) NULL,
                                [UserAgent]         NVARCHAR(1000) NULL,
                                [LoginStatus]       NVARCHAR(30) NOT NULL,
                                [FailureReason]     NVARCHAR(500) NULL,
                                [SessionID]         NVARCHAR(200) NULL
                            );
                        END;

                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ErrorLog]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[ErrorLog]
                            (
                                [ErrorLogID]        BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [ErrorDateTime]     DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
                                [UserAccountID]     INT NULL,
                                [UserName]          NVARCHAR(200) NULL,
                                [ModuleName]        NVARCHAR(200) NULL,
                                [PageName]          NVARCHAR(500) NULL,
                                [MethodName]        NVARCHAR(500) NULL,
                                [ErrorMessage]      NVARCHAR(MAX) NULL,
                                [StackTrace]        NVARCHAR(MAX) NULL,
                                [RequestURL]        NVARCHAR(1000) NULL,
                                [RequestData]       NVARCHAR(MAX) NULL,
                                [IPAddress]         NVARCHAR(100) NULL
                            );
                        END;

                        IF OBJECT_ID(N'[dbo].[sp_GetAuditLogs]', N'P') IS NULL
                        BEGIN
                            EXEC('
                            CREATE PROCEDURE [dbo].[sp_GetAuditLogs]
                                @FromDate       DATETIME = NULL,
                                @ToDate         DATETIME = NULL,
                                @UserAccountID  INT = NULL,
                                @ModuleName     NVARCHAR(100) = NULL,
                                @ActionName     NVARCHAR(100) = NULL,
                                @TableName      NVARCHAR(200) = NULL,
                                @RecordID       NVARCHAR(200) = NULL,
                                @SearchKeyword  NVARCHAR(200) = NULL,
                                @PageNumber     INT = 1,
                                @PageSize       INT = 50
                            AS
                            BEGIN
                                SET NOCOUNT ON;

                                SELECT 
                                    A.[AuditLogID],
                                    A.[LogDateTime],
                                    A.[UserAccountID],
                                    A.[UserName],
                                    A.[ModuleName],
                                    A.[PageName],
                                    A.[ActionName],
                                    A.[TableName],
                                    A.[RecordID],
                                    A.[Description],
                                    A.[OldValue],
                                    A.[NewValue],
                                    A.[RequestURL],
                                    A.[IPAddress],
                                    A.[UserAgent],
                                    A.[Status],
                                    A.[ErrorMessage],
                                    A.[ExceptionDetails],
                                    A.[SessionID],
                                    COUNT(1) OVER() AS TotalRecords
                                FROM [dbo].[AuditLog] A WITH (NOLOCK)
                                WHERE (@FromDate IS NULL OR A.[LogDateTime] >= CAST(@FromDate AS DATE))
                                  AND (@ToDate IS NULL OR A.[LogDateTime] < DATEADD(DAY, 1, CAST(@ToDate AS DATE)))
                                  AND (@UserAccountID IS NULL OR A.[UserAccountID] = @UserAccountID)
                                  AND (@ModuleName IS NULL OR @ModuleName = '''' OR A.[ModuleName] = @ModuleName)
                                  AND (@ActionName IS NULL OR @ActionName = '''' OR A.[ActionName] = @ActionName)
                                  AND (@TableName IS NULL OR @TableName = '''' OR A.[TableName] = @TableName)
                                  AND (@RecordID IS NULL OR @RecordID = '''' OR A.[RecordID] = @RecordID)
                                  AND (@SearchKeyword IS NULL OR @SearchKeyword = '''' OR (
                                        A.[Description] LIKE ''%'' + @SearchKeyword + ''%'' OR
                                        A.[UserName] LIKE ''%'' + @SearchKeyword + ''%'' OR
                                        A.[RecordID] LIKE ''%'' + @SearchKeyword + ''%'' OR
                                        A.[ModuleName] LIKE ''%'' + @SearchKeyword + ''%''
                                  ))
                                ORDER BY A.[LogDateTime] DESC
                                OFFSET (@PageNumber - 1) * @PageSize ROWS
                                FETCH NEXT @PageSize ROWS ONLY;
                            END');
                        END;";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    _tablesChecked = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("EnsureAuditTablesExist Exception: " + ex.Message);
                }
            }
        }

        #region Standard Audit Log Entry
        /// <summary>
        /// Writes an audit record. If existingConn and existingTran are provided, executes within that transaction.
        /// </summary>
        public static void Log(
            string module,
            string action,
            string tableName = null,
            string recordId = null,
            string description = null,
            object oldValue = null,
            object newValue = null,
            string status = "Success",
            string errorMessage = null,
            Exception ex = null,
            int? customUserId = null,
            string customUserName = null,
            SqlConnection existingConn = null,
            SqlTransaction existingTran = null)
        {
            try
            {
                EnsureAuditTablesExist();
                HttpContext context = HttpContext.Current;

                int? userId = customUserId ?? GetCurrentUserId(context);
                string userName = customUserName ?? GetCurrentUserName(context);
                string ipAddress = GetClientIP(context);
                string userAgent = ParseUserAgent(context?.Request?.UserAgent);
                string requestUrl = context?.Request?.Url?.ToString();
                string pageName = context?.Request?.Path;
                string sessionId = context?.Session?.SessionID;

                if (oldValue == null && !string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(recordId) && (action == "UPDATE" || action == "DELETE"))
                {
                    try { oldValue = FetchOldValue(tableName, recordId, existingConn, existingTran); } catch { }
                }

                string jsonOld = SerializeObject(oldValue);
                string jsonNew = SerializeObject(newValue);
                string exDetails = ex != null ? $"{ex.Message}\n{ex.StackTrace}" : null;

                const string query = @"
                    INSERT INTO dbo.AuditLog
                    (
                        LogDateTime, UserAccountID, UserName, ModuleName, PageName, ActionName,
                        TableName, RecordID, Description, OldValue, NewValue, RequestURL,
                        IPAddress, UserAgent, Status, ErrorMessage, ExceptionDetails, SessionID
                    )
                    VALUES
                    (
                        SYSDATETIME(), @UserAccountID, @UserName, @ModuleName, @PageName, @ActionName,
                        @TableName, @RecordID, @Description, @OldValue, @NewValue, @RequestURL,
                        @IPAddress, @UserAgent, @Status, @ErrorMessage, @ExceptionDetails, @SessionID
                    );";

                if (existingConn != null && existingTran != null && existingConn.State == ConnectionState.Open && existingTran.Connection != null)
                {
                    try
                    {
                        // Synchronously execute inside existing transaction
                        using (SqlCommand cmd = new SqlCommand(query, existingConn, existingTran))
                        {
                            AddParameters(cmd, userId, userName, module, pageName, action, tableName, recordId,
                                description, jsonOld, jsonNew, requestUrl, ipAddress, userAgent, status, errorMessage, exDetails, sessionId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        // Fallback to independent connection if transaction is no longer active
                        using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                        {
                            conn.Open();
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                AddParameters(cmd, userId, userName, module, pageName, action, tableName, recordId,
                                    description, jsonOld, jsonNew, requestUrl, ipAddress, userAgent, status, errorMessage, exDetails, sessionId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                else
                {
                    // Synchronously execute on an independent connection
                    using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            AddParameters(cmd, userId, userName, module, pageName, action, tableName, recordId,
                                description, jsonOld, jsonNew, requestUrl, ipAddress, userAgent, status, errorMessage, exDetails, sessionId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // If this audit entry represents an error/exception or failure, also insert into dbo.ErrorLog
                bool isError = ex != null ||
                               !string.IsNullOrEmpty(errorMessage) ||
                               (status != null && status.Equals("Error", StringComparison.OrdinalIgnoreCase)) ||
                               (action != null && (action.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0 || action.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                               (description != null && (description.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0 || description.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0));

                if (isError)
                {
                    try
                    {
                        string errMessageStr = ex?.Message ?? errorMessage ?? description ?? "Unknown Error";
                        string stackTraceStr = ex?.StackTrace ?? exDetails;
                        string methodNameStr = action ?? "Execute";

                        const string errQuery = @"
                            INSERT INTO dbo.ErrorLog
                            (ErrorDateTime, UserAccountID, UserName, ModuleName, PageName, MethodName, ErrorMessage, StackTrace, RequestURL, RequestData, IPAddress)
                            VALUES
                            (SYSDATETIME(), @UserAccountID, @UserName, @ModuleName, @PageName, @MethodName, @ErrorMessage, @StackTrace, @RequestURL, @RequestData, @IPAddress);";

                        using (SqlConnection errConn = new SqlConnection(GetConnectionString()))
                        {
                            errConn.Open();
                            using (SqlCommand errCmd = new SqlCommand(errQuery, errConn))
                            {
                                errCmd.Parameters.AddWithValue("@UserAccountID", (object)userId ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@UserName", (object)userName ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@ModuleName", (object)module ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@PageName", (object)pageName ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@MethodName", (object)methodNameStr ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@ErrorMessage", (object)errMessageStr ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@StackTrace", (object)stackTraceStr ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@RequestURL", (object)requestUrl ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@RequestData", (object)jsonOld ?? (object)jsonNew ?? (object)description ?? DBNull.Value);
                                errCmd.Parameters.AddWithValue("@IPAddress", (object)ipAddress ?? DBNull.Value);
                                errCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception errLogEx)
                    {
                        System.Diagnostics.Debug.WriteLine("ErrorLog write exception: " + errLogEx.Message);
                    }
                }
            }
            catch (Exception helperEx)
            {
                // Failsafe catch block: Logging failure must never break business logic execution
                System.Diagnostics.Debug.WriteLine("AuditLogHelper Exception: " + helperEx.Message);
            }
        }
        #endregion

        #region Strict Transactional Audit Logging
        /// <summary>
        /// Strictly writes an audit record within an active SqlTransaction.
        /// Guaranteed atomicity: never falls back to an independent connection, does not suppress exceptions.
        /// If writing the audit record fails, the exception is thrown back to the caller to ensure complete transaction rollback.
        /// </summary>
        public static void LogTransactional(
            SqlConnection conn,
            SqlTransaction tran,
            string module,
            string action,
            string tableName = null,
            string recordId = null,
            string description = null,
            object oldValue = null,
            object newValue = null,
            string status = "Success",
            string errorMessage = null,
            Exception ex = null,
            int? customUserId = null,
            string customUserName = null)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn), "SqlConnection cannot be null for transactional audit logging.");

            if (tran == null)
                throw new ArgumentNullException(nameof(tran), "SqlTransaction cannot be null for transactional audit logging.");

            if (conn.State != ConnectionState.Open)
                throw new InvalidOperationException("SqlConnection must be in an Open state for transactional audit logging.");

            if (tran.Connection == null)
                throw new InvalidOperationException("SqlTransaction is no longer active or associated with a connection.");

            EnsureAuditTablesExist();

            HttpContext context = HttpContext.Current;

            int? userId = customUserId ?? GetCurrentUserId(context);
            string userName = customUserName ?? GetCurrentUserName(context);
            string ipAddress = GetClientIP(context);
            string userAgent = ParseUserAgent(context?.Request?.UserAgent);
            string requestUrl = context?.Request?.Url?.ToString();
            string pageName = context?.Request?.Path;
            string sessionId = context?.Session?.SessionID;

            string jsonOld = SerializeObject(oldValue);
            string jsonNew = SerializeObject(newValue);
            string exDetails = ex != null ? $"{ex.Message}\n{ex.StackTrace}" : null;

            const string query = @"
                INSERT INTO dbo.AuditLog
                (
                    LogDateTime, UserAccountID, UserName, ModuleName, PageName, ActionName,
                    TableName, RecordID, Description, OldValue, NewValue, RequestURL,
                    IPAddress, UserAgent, Status, ErrorMessage, ExceptionDetails, SessionID
                )
                VALUES
                (
                    SYSDATETIME(), @UserAccountID, @UserName, @ModuleName, @PageName, @ActionName,
                    @TableName, @RecordID, @Description, @OldValue, @NewValue, @RequestURL,
                    @IPAddress, @UserAgent, @Status, @ErrorMessage, @ExceptionDetails, @SessionID
                );";

            // Execute strictly within the active connection and transaction.
            // NO try-catch wrapper, NO fallback to independent connection.
            using (SqlCommand cmd = new SqlCommand(query, conn, tran))
            {
                AddParameters(cmd, userId, userName, module, pageName, action, tableName, recordId,
                    description, jsonOld, jsonNew, requestUrl, ipAddress, userAgent, status, errorMessage, exDetails, sessionId);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Login / Logout Logging
        /// <summary>
        /// Logs user login attempts (Success/Failed) to LoginHistory and main AuditLog.
        /// </summary>
        public static void LogLogin(int? userId, string userName, string status, string failureReason = null)
        {
            try
            {
                HttpContext context = HttpContext.Current;
                string ipAddress = GetClientIP(context);
                string userAgent = ParseUserAgent(context?.Request?.UserAgent);
                string sessionId = context?.Session?.SessionID;

                const string query = @"
                    INSERT INTO dbo.LoginHistory
                    (UserAccountID, UserName, LoginDateTime, IPAddress, UserAgent, LoginStatus, FailureReason, SessionID)
                    VALUES
                    (@UserAccountID, @UserName, SYSDATETIME(), @IPAddress, @UserAgent, @LoginStatus, @FailureReason, @SessionID);";

                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserAccountID", (object)userId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserName", (object)userName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IPAddress", (object)ipAddress ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserAgent", (object)userAgent ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LoginStatus", status);
                        cmd.Parameters.AddWithValue("@FailureReason", (object)failureReason ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SessionID", (object)sessionId ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                Log(
                    module: "Authentication",
                    action: status == "Success" ? "LOGIN" : "FAILED_LOGIN",
                    description: status == "Success" ? $"User '{userName}' logged in successfully" : $"Login failed for '{userName}': {failureReason}",
                    status: status,
                    customUserId: userId,
                    customUserName: userName
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LogLogin Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Logs user logout and updates LoginHistory LogoutDateTime for the latest active login record.
        /// </summary>
        public static void LogLogout(int? userId, string userName)
        {
            try
            {
                HttpContext context = HttpContext.Current;

                int? targetUserId = userId ?? GetCurrentUserId(context);
                string targetUserName = (!string.IsNullOrEmpty(userName) && userName != "User") ? userName : GetCurrentUserName(context);

                if (targetUserId.HasValue && targetUserId.Value > 0)
                {
                    const string query = @"
                        UPDATE dbo.LoginHistory
                        SET LogoutDateTime = SYSDATETIME()
                        WHERE LoginHistoryID = (
                            SELECT TOP 1 LoginHistoryID
                            FROM dbo.LoginHistory
                            WHERE UserAccountID = @UserAccountID
                              AND LoginStatus = 'Success'
                              AND LogoutDateTime IS NULL
                            ORDER BY LoginDateTime DESC
                        );";

                    using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserAccountID", targetUserId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(targetUserName) && targetUserName != "System")
                {
                    const string queryByUserName = @"
                        UPDATE dbo.LoginHistory
                        SET LogoutDateTime = SYSDATETIME()
                        WHERE LoginHistoryID = (
                            SELECT TOP 1 LoginHistoryID
                            FROM dbo.LoginHistory
                            WHERE UserName = @UserName
                              AND LoginStatus = 'Success'
                              AND LogoutDateTime IS NULL
                            ORDER BY LoginDateTime DESC
                        );";

                    using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(queryByUserName, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserName", targetUserName);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                Log(
                    module: "Authentication",
                    action: "LOGOUT",
                    description: $"User '{targetUserName}' logged out",
                    status: "Success",
                    customUserId: targetUserId,
                    customUserName: targetUserName
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LogLogout Exception: " + ex.Message);
            }
        }
        #endregion

        #region Transaction Audit Shortcuts
        /// <summary>
        /// Logs a CREATE transaction.
        /// </summary>
        public static void LogCreate(string module, string tableName, string recordId, string description = null, object newValue = null, SqlConnection conn = null, SqlTransaction tran = null)
        {
            Log(
                module: module,
                action: "CREATE",
                tableName: tableName,
                recordId: recordId,
                description: description ?? $"Created new {tableName} record (ID: {recordId})",
                oldValue: null,
                newValue: newValue,
                status: "Success",
                existingConn: conn,
                existingTran: tran
            );
        }

        /// <summary>
        /// Alias for LogCreate (INSERT).
        /// </summary>
        public static void LogInsert(string module, string tableName, string recordId, string description = null, object newValue = null, SqlConnection conn = null, SqlTransaction tran = null)
        {
            LogCreate(module, tableName, recordId, description, newValue, conn, tran);
        }

        /// <summary>
        /// Logs an UPDATE transaction.
        /// </summary>
        public static void LogUpdate(string module, string tableName, string recordId, string description = null, object oldValue = null, object newValue = null, SqlConnection conn = null, SqlTransaction tran = null)
        {
            Log(
                module: module,
                action: "UPDATE",
                tableName: tableName,
                recordId: recordId,
                description: description ?? $"Updated {tableName} record (ID: {recordId})",
                oldValue: oldValue,
                newValue: newValue,
                status: "Success",
                existingConn: conn,
                existingTran: tran
            );
        }

        /// <summary>
        /// Logs a DELETE transaction.
        /// </summary>
        public static void LogDelete(string module, string tableName, string recordId, string description = null, object oldValue = null, SqlConnection conn = null, SqlTransaction tran = null)
        {
            Log(
                module: module,
                action: "DELETE",
                tableName: tableName,
                recordId: recordId,
                description: description ?? $"Deleted {tableName} record (ID: {recordId})",
                oldValue: oldValue,
                newValue: null,
                status: "Success",
                existingConn: conn,
                existingTran: tran
            );
        }

        /// <summary>
        /// Logs any custom transaction action (APPROVE, REJECT, EXPORT, PRINT, etc.).
        /// </summary>
        public static void LogAction(string module, string action, string tableName = null, string recordId = null, string description = null, SqlConnection conn = null, SqlTransaction tran = null)
        {
            Log(
                module: module,
                action: action,
                tableName: tableName,
                recordId: recordId,
                description: description,
                status: "Success",
                existingConn: conn,
                existingTran: tran
            );
        }

        /// <summary>
        /// Logs a validation error / failed validation.
        /// </summary>
        public static void LogValidationError(string module, string tableName, string recordId, string description, string errorMessage = null, object requestData = null, SqlConnection conn = null, SqlTransaction tran = null)
        {
            Log(
                module: module,
                action: "VALIDATION_FAILED",
                tableName: tableName,
                recordId: recordId,
                description: description,
                newValue: requestData,
                status: "Failed",
                errorMessage: errorMessage ?? description,
                existingConn: conn,
                existingTran: tran
            );
        }

        /// <summary>
        /// Logs a duplicate check failure.
        /// </summary>
        public static void LogDuplicateError(string module, string tableName, string recordId, string description, string errorMessage = null, object requestData = null, SqlConnection conn = null, SqlTransaction tran = null)
        {
            Log(
                module: module,
                action: "DUPLICATE_PREVENTED",
                tableName: tableName,
                recordId: recordId,
                description: description,
                newValue: requestData,
                status: "Failed",
                errorMessage: errorMessage ?? description,
                existingConn: conn,
                existingTran: tran
            );
        }

        /// <summary>
        /// Logs a dependency blocked error (record in use elsewhere).
        /// </summary>
        public static void LogDependencyError(string module, string tableName, string recordId, string description, string errorMessage = null, object requestData = null, SqlConnection conn = null, SqlTransaction tran = null)
        {
            Log(
                module: module,
                action: "DEPENDENCY_BLOCKED",
                tableName: tableName,
                recordId: recordId,
                description: description,
                newValue: requestData,
                status: "Failed",
                errorMessage: errorMessage ?? description,
                existingConn: conn,
                existingTran: tran
            );
        }

        /// <summary>
        /// Fetches the current database record before an UPDATE or DELETE operation to capture OldValue.
        /// </summary>
        public static object FetchOldValue(string tableName, string recordId, SqlConnection conn = null, SqlTransaction tran = null)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(recordId))
                return null;

            try
            {
                string cs = GetConnectionString();
                if (string.IsNullOrEmpty(cs) && conn == null) return null;

                Func<SqlConnection, SqlTransaction, object> queryFunc = (c, t) =>
                {
                    string pkColumn = null;
                    string findPkSql = @"
                        SELECT TOP 1 c.name
                        FROM sys.indexes i
                        JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                        JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                        WHERE i.is_primary_key = 1 AND i.object_id = OBJECT_ID(@TableName);";

                    using (SqlCommand pkCmd = new SqlCommand(findPkSql, c, t))
                    {
                        pkCmd.Parameters.AddWithValue("@TableName", tableName);
                        object result = pkCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            pkColumn = result.ToString();
                    }

                    if (string.IsNullOrEmpty(pkColumn))
                    {
                        string cleanTable = tableName.Replace("dbo.", "").Replace("[", "").Replace("]", "");
                        string fallbackSql = @"
                            SELECT TOP 1 name FROM sys.columns 
                            WHERE object_id = OBJECT_ID(@TableName) 
                              AND (name = @CleanTable + 'ID' OR name = @CleanTable + 'Id' OR name = 'ID' OR name = 'Id')
                            ORDER BY column_id;";

                        using (SqlCommand fbCmd = new SqlCommand(fallbackSql, c, t))
                        {
                            fbCmd.Parameters.AddWithValue("@TableName", tableName);
                            fbCmd.Parameters.AddWithValue("@CleanTable", cleanTable);
                            object fbResult = fbCmd.ExecuteScalar();
                            if (fbResult != null && fbResult != DBNull.Value)
                                pkColumn = fbResult.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(pkColumn))
                    {
                        string firstColSql = "SELECT TOP 1 name FROM sys.columns WHERE object_id = OBJECT_ID(@TableName) ORDER BY column_id;";
                        using (SqlCommand fcCmd = new SqlCommand(firstColSql, c, t))
                        {
                            fcCmd.Parameters.AddWithValue("@TableName", tableName);
                            object fcResult = fcCmd.ExecuteScalar();
                            if (fcResult != null && fcResult != DBNull.Value)
                                pkColumn = fcResult.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(pkColumn)) return null;

                    string selectSql = $"SELECT TOP 1 * FROM [{tableName.Replace("[", "").Replace("]", "")}] WHERE [{pkColumn}] = @RecordID;";
                    using (SqlCommand selectCmd = new SqlCommand(selectSql, c, t))
                    {
                        selectCmd.Parameters.AddWithValue("@RecordID", recordId);
                        using (SqlDataReader rdr = selectCmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                var dict = new System.Collections.Generic.Dictionary<string, object>();
                                for (int col = 0; col < rdr.FieldCount; col++)
                                {
                                    dict[rdr.GetName(col)] = rdr.IsDBNull(col) ? null : rdr.GetValue(col);
                                }
                                return dict;
                            }
                        }
                    }
                    return null;
                };

                if (conn != null && conn.State == ConnectionState.Open)
                {
                    return queryFunc(conn, tran);
                }
                else
                {
                    using (SqlConnection newConn = new SqlConnection(cs))
                    {
                        newConn.Open();
                        return queryFunc(newConn, null);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FetchOldValue Exception: " + ex.Message);
                return null;
            }
        }
        #endregion

        #region Error Logging
        /// <summary>
        /// Logs unhandled application exceptions to ErrorLog and AuditLog tables.
        /// </summary>
        public static void LogError(string module, string methodName, Exception ex, string requestData = null)
        {
            try
            {
                HttpContext context = HttpContext.Current;
                int? userId = GetCurrentUserId(context);
                string userName = GetCurrentUserName(context);
                string ipAddress = GetClientIP(context);
                string requestUrl = context?.Request?.Url?.ToString();
                string pageName = context?.Request?.Path;

                const string query = @"
                    INSERT INTO dbo.ErrorLog
                    (ErrorDateTime, UserAccountID, UserName, ModuleName, PageName, MethodName, ErrorMessage, StackTrace, RequestURL, RequestData, IPAddress)
                    VALUES
                    (SYSDATETIME(), @UserAccountID, @UserName, @ModuleName, @PageName, @MethodName, @ErrorMessage, @StackTrace, @RequestURL, @RequestData, @IPAddress);";

                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserAccountID", (object)userId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserName", (object)userName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ModuleName", (object)module ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageName", (object)pageName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MethodName", (object)methodName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ErrorMessage", ex?.Message ?? "Unknown Error");
                        cmd.Parameters.AddWithValue("@StackTrace", (object)ex?.StackTrace ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@RequestURL", (object)requestUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@RequestData", (object)requestData ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IPAddress", (object)ipAddress ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                Log(
                    module: module,
                    action: "ERROR",
                    description: $"Exception in {methodName}: {ex?.Message}",
                    status: "Error",
                    errorMessage: ex?.Message,
                    ex: ex
                );
            }
            catch (Exception helperEx)
            {
                System.Diagnostics.Debug.WriteLine("LogError Exception: " + helperEx.Message);
            }
        }
        #endregion

        #region Helper Methods
        private static void AddParameters(
            SqlCommand cmd, int? userId, string userName, string module, string pageName,
            string action, string tableName, string recordId, string description,
            string jsonOld, string jsonNew, string requestUrl, string ipAddress,
            string userAgent, string status, string errorMessage, string exDetails, string sessionId)
        {
            cmd.Parameters.AddWithValue("@UserAccountID", (object)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserName", (object)userName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModuleName", (object)module ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PageName", (object)pageName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ActionName", action);
            cmd.Parameters.AddWithValue("@TableName", (object)tableName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RecordID", (object)recordId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OldValue", (object)jsonOld ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NewValue", (object)jsonNew ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RequestURL", (object)requestUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IPAddress", (object)ipAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserAgent", (object)userAgent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@ErrorMessage", (object)errorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ExceptionDetails", (object)exDetails ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SessionID", (object)sessionId ?? DBNull.Value);
        }

        private static string GetClientIP(HttpContext context)
        {
            if (context == null) return "127.0.0.1";
            string ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(ip))
            {
                string[] ips = ip.Split(',');
                if (ips.Length > 0) return ips[0].Trim();
            }
            string hostAddr = context.Request.UserHostAddress;
            if (hostAddr == "::1") return "127.0.0.1 (Localhost)";
            return hostAddr ?? "127.0.0.1";
        }

        private static int? GetCurrentUserId(HttpContext context)
        {
            if (context != null)
            {
                // 1. Try Cookie (used by application authentication)
                string uidStr = context.Request.Cookies["UserIDs"]?["UserIDs"]
                    ?? context.Request.Cookies["UserIDs"]?.Value
                    ?? context.Request.Cookies["UserID"]?["UserID"]
                    ?? context.Request.Cookies["UserID"]?.Value;

                if (!string.IsNullOrEmpty(uidStr))
                {
                    if (uidStr.Contains("="))
                    {
                        string[] parts = uidStr.Split('=');
                        if (parts.Length > 1 && int.TryParse(parts[1], out int pId))
                            return pId;
                    }
                    if (int.TryParse(uidStr, out int parseId))
                    {
                        return parseId;
                    }
                }

                // 2. Try Session fallback
                try
                {
                    if (context.Session?["UserAccountId"] != null)
                        return Convert.ToInt32(context.Session["UserAccountId"]);
                }
                catch { }
            }
            return null;
        }

        private static string GetCurrentUserName(HttpContext context)
        {
            if (context != null)
            {
                // 1. Try Cookie (used by application authentication)
                string uName = context.Request.Cookies["UserName"]?["UserName"]
                    ?? context.Request.Cookies["UserName"]?.Value;

                if (!string.IsNullOrEmpty(uName))
                {
                    if (uName.Contains("="))
                    {
                        string[] parts = uName.Split('=');
                        if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                            return parts[1];
                    }
                    return uName;
                }

                // 2. Try Session fallback
                try
                {
                    if (context.Session?["UserAccountName"] != null)
                        return context.Session["UserAccountName"].ToString();
                }
                catch { }
            }
            return "System";
        }

        private static string ParseUserAgent(string rawUserAgent)
        {
            if (string.IsNullOrEmpty(rawUserAgent)) return "Unknown Browser";

            string os = "Unknown OS";
            if (rawUserAgent.Contains("Windows NT 10.0")) os = "Windows 10/11";
            else if (rawUserAgent.Contains("Windows NT 6.3")) os = "Windows 8.1";
            else if (rawUserAgent.Contains("Windows NT 6.1")) os = "Windows 7";
            else if (rawUserAgent.Contains("Android")) os = "Android";
            else if (rawUserAgent.Contains("iPhone") || rawUserAgent.Contains("iPad")) os = "iOS";
            else if (rawUserAgent.Contains("Mac OS X")) os = "macOS";
            else if (rawUserAgent.Contains("Linux")) os = "Linux";

            string browser = "Unknown Browser";
            if (rawUserAgent.Contains("Edg/"))
            {
                string version = GetRegexVersion(rawUserAgent, @"Edg/([\d\.]+)");
                browser = string.IsNullOrEmpty(version) ? "Microsoft Edge" : $"Microsoft Edge {version}";
            }
            else if (rawUserAgent.Contains("Chrome/"))
            {
                string version = GetRegexVersion(rawUserAgent, @"Chrome/([\d\.]+)");
                browser = string.IsNullOrEmpty(version) ? "Google Chrome" : $"Google Chrome {version}";
            }
            else if (rawUserAgent.Contains("Firefox/"))
            {
                string version = GetRegexVersion(rawUserAgent, @"Firefox/([\d\.]+)");
                browser = string.IsNullOrEmpty(version) ? "Mozilla Firefox" : $"Mozilla Firefox {version}";
            }
            else if (rawUserAgent.Contains("Safari/") && !rawUserAgent.Contains("Chrome/"))
            {
                string version = GetRegexVersion(rawUserAgent, @"Version/([\d\.]+)");
                browser = string.IsNullOrEmpty(version) ? "Apple Safari" : $"Apple Safari {version}";
            }
            else if (rawUserAgent.Contains("OPR/") || rawUserAgent.Contains("Opera/"))
            {
                string version = GetRegexVersion(rawUserAgent, @"OPR/([\d\.]+)");
                browser = string.IsNullOrEmpty(version) ? "Opera" : $"Opera {version}";
            }

            return $"{browser} ({os})";
        }

        private static string GetRegexVersion(string input, string pattern)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(input, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    string fullVer = match.Groups[1].Value;
                    string[] parts = fullVer.Split('.');
                    if (parts.Length > 0) return parts[0];
                }
            }
            catch { }
            return "";
        }

        private static string SerializeObject(object obj)
        {
            if (obj == null) return null;
            if (obj is string str) return str;
            try
            {
                return JsonConvert.SerializeObject(obj, Formatting.Indented,
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            }
            catch
            {
                return obj.ToString();
            }
        }
        #endregion
    }
}
