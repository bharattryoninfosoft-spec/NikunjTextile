-- ============================================================================
-- Central Audit Logging System Schema for WeaveLaneSoft ERP
-- Tables: AuditLog, LoginHistory, ErrorLog
-- Stored Procedures: sp_GetAuditLogs, sp_InsertAuditLog, sp_InsertLoginHistory, sp_InsertErrorLog
-- Compatibility: SQL Server 2012 or newer required (uses OFFSET/FETCH pagination)
-- Production Target: SQL Server 16.0 RTM (SQL Server 2022) - Fully Compatible
-- ============================================================================

USE [weavelanesoftDB];
GO


-- 1. Main AuditLog Table
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
        [ActionName]        NVARCHAR(100) NOT NULL, -- CREATE, UPDATE, DELETE, LOGIN, LOGOUT, APPROVE, REJECT, EXPORT, PRINT, etc.
        
        [TableName]         NVARCHAR(200) NULL,
        [RecordID]          NVARCHAR(200) NULL,
        
        [Description]       NVARCHAR(1000) NULL,
        [OldValue]          NVARCHAR(MAX) NULL,     -- JSON String (Pre-change state)
        [NewValue]          NVARCHAR(MAX) NULL,     -- JSON String (Post-change state)
        
        [RequestURL]        NVARCHAR(1000) NULL,
        [IPAddress]         NVARCHAR(100) NULL,
        [UserAgent]         NVARCHAR(1000) NULL,
        
        [Status]            NVARCHAR(30) NOT NULL DEFAULT 'Success', -- Success, Error, Warning
        [ErrorMessage]      NVARCHAR(MAX) NULL,
        [ExceptionDetails]  NVARCHAR(MAX) NULL,
        [SessionID]         NVARCHAR(200) NULL,
        
        [CreatedDate]       DATETIME2(3) NOT NULL DEFAULT SYSDATETIME()
    );
END
GO

-- AuditLog Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AuditLog_LogDateTime' AND object_id = OBJECT_ID(N'[dbo].[AuditLog]'))
    CREATE NONCLUSTERED INDEX [IX_AuditLog_LogDateTime] ON [dbo].[AuditLog] ([LogDateTime] DESC);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AuditLog_Module_Action' AND object_id = OBJECT_ID(N'[dbo].[AuditLog]'))
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Module_Action] ON [dbo].[AuditLog] ([ModuleName], [ActionName]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AuditLog_User' AND object_id = OBJECT_ID(N'[dbo].[AuditLog]'))
    CREATE NONCLUSTERED INDEX [IX_AuditLog_User] ON [dbo].[AuditLog] ([UserAccountID], [UserName]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AuditLog_Table_Record' AND object_id = OBJECT_ID(N'[dbo].[AuditLog]'))
    CREATE NONCLUSTERED INDEX [IX_AuditLog_Table_Record] ON [dbo].[AuditLog] ([TableName], [RecordID]);
GO

-- 2. Separate LoginHistory Table
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
        [LoginStatus]       NVARCHAR(30) NOT NULL, -- Success, Failed, Locked
        [FailureReason]     NVARCHAR(500) NULL,
        [SessionID]         NVARCHAR(200) NULL
    );
END
GO

-- LoginHistory Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_LoginHistory_User_Date' AND object_id = OBJECT_ID(N'[dbo].[LoginHistory]'))
    CREATE NONCLUSTERED INDEX [IX_LoginHistory_User_Date] ON [dbo].[LoginHistory] ([UserAccountID], [LoginDateTime] DESC);
GO

-- 3. Separate ErrorLog Table
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
END
GO

-- ErrorLog Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_ErrorLog_ErrorDateTime' AND object_id = OBJECT_ID(N'[dbo].[ErrorLog]'))
    CREATE NONCLUSTERED INDEX [IX_ErrorLog_ErrorDateTime] ON [dbo].[ErrorLog] ([ErrorDateTime] DESC);
GO

-- 4. Stored Procedure: Fetch Audit Logs for Dashboard Filter & Pagination
IF OBJECT_ID(N'[dbo].[sp_GetAuditLogs]', N'P') IS NULL
    EXEC('CREATE PROCEDURE [dbo].[sp_GetAuditLogs] AS RETURN 0;');
GO

ALTER PROCEDURE [dbo].[sp_GetAuditLogs]
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
      AND (@ModuleName IS NULL OR @ModuleName = '' OR A.[ModuleName] = @ModuleName)
      AND (@ActionName IS NULL OR @ActionName = '' OR A.[ActionName] = @ActionName)
      AND (@TableName IS NULL OR @TableName = '' OR A.[TableName] = @TableName)
      AND (@RecordID IS NULL OR @RecordID = '' OR A.[RecordID] = @RecordID)
      AND (@SearchKeyword IS NULL OR @SearchKeyword = '' OR (
            A.[Description] LIKE '%' + @SearchKeyword + '%' OR
            A.[UserName] LIKE '%' + @SearchKeyword + '%' OR
            A.[RecordID] LIKE '%' + @SearchKeyword + '%' OR
            A.[ModuleName] LIKE '%' + @SearchKeyword + '%'
      ))
    ORDER BY A.[LogDateTime] DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- 5. Stored Procedure: Insert Audit Log Entry
IF OBJECT_ID(N'[dbo].[sp_InsertAuditLog]', N'P') IS NULL
    EXEC('CREATE PROCEDURE [dbo].[sp_InsertAuditLog] AS RETURN 0;');
GO

ALTER PROCEDURE [dbo].[sp_InsertAuditLog]
    @UserAccountID      INT = NULL,
    @UserName           NVARCHAR(200) = NULL,
    @ModuleName         NVARCHAR(100),
    @PageName           NVARCHAR(200) = NULL,
    @ActionName         NVARCHAR(100),
    @TableName          NVARCHAR(200) = NULL,
    @RecordID           NVARCHAR(200) = NULL,
    @Description        NVARCHAR(1000) = NULL,
    @OldValue           NVARCHAR(MAX) = NULL,
    @NewValue           NVARCHAR(MAX) = NULL,
    @RequestURL         NVARCHAR(1000) = NULL,
    @IPAddress          NVARCHAR(100) = NULL,
    @UserAgent          NVARCHAR(1000) = NULL,
    @Status             NVARCHAR(30) = 'Success',
    @ErrorMessage       NVARCHAR(MAX) = NULL,
    @ExceptionDetails   NVARCHAR(MAX) = NULL,
    @SessionID          NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[AuditLog]
    (
        [LogDateTime], [UserAccountID], [UserName], [ModuleName], [PageName], [ActionName],
        [TableName], [RecordID], [Description], [OldValue], [NewValue], [RequestURL],
        [IPAddress], [UserAgent], [Status], [ErrorMessage], [ExceptionDetails], [SessionID]
    )
    VALUES
    (
        SYSDATETIME(), @UserAccountID, @UserName, @ModuleName, @PageName, @ActionName,
        @TableName, @RecordID, @Description, @OldValue, @NewValue, @RequestURL,
        @IPAddress, @UserAgent, @Status, @ErrorMessage, @ExceptionDetails, @SessionID
    );
END
GO

-- 6. Stored Procedure: Insert Login History Entry
IF OBJECT_ID(N'[dbo].[sp_InsertLoginHistory]', N'P') IS NULL
    EXEC('CREATE PROCEDURE [dbo].[sp_InsertLoginHistory] AS RETURN 0;');
GO

ALTER PROCEDURE [dbo].[sp_InsertLoginHistory]
    @UserAccountID  INT = NULL,
    @UserName       NVARCHAR(200) = NULL,
    @IPAddress      NVARCHAR(100) = NULL,
    @UserAgent      NVARCHAR(1000) = NULL,
    @LoginStatus    NVARCHAR(30),
    @FailureReason  NVARCHAR(500) = NULL,
    @SessionID       NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[LoginHistory]
    (
        [UserAccountID], [UserName], [LoginDateTime], [IPAddress], [UserAgent], [LoginStatus], [FailureReason], [SessionID]
    )
    VALUES
    (
        @UserAccountID, @UserName, SYSDATETIME(), @IPAddress, @UserAgent, @LoginStatus, @FailureReason, @SessionID
    );
END
GO

-- 7. Stored Procedure: Insert Error Log Entry
IF OBJECT_ID(N'[dbo].[sp_InsertErrorLog]', N'P') IS NULL
    EXEC('CREATE PROCEDURE [dbo].[sp_InsertErrorLog] AS RETURN 0;');
GO

ALTER PROCEDURE [dbo].[sp_InsertErrorLog]
    @UserAccountID  INT = NULL,
    @UserName       NVARCHAR(200) = NULL,
    @ModuleName     NVARCHAR(200) = NULL,
    @PageName       NVARCHAR(500) = NULL,
    @MethodName     NVARCHAR(500) = NULL,
    @ErrorMessage   NVARCHAR(MAX) = NULL,
    @StackTrace     NVARCHAR(MAX) = NULL,
    @RequestURL     NVARCHAR(1000) = NULL,
    @RequestData    NVARCHAR(MAX) = NULL,
    @IPAddress      NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[ErrorLog]
    (
        [ErrorDateTime], [UserAccountID], [UserName], [ModuleName], [PageName], [MethodName],
        [ErrorMessage], [StackTrace], [RequestURL], [RequestData], [IPAddress]
    )
    VALUES
    (
        SYSDATETIME(), @UserAccountID, @UserName, @ModuleName, @PageName, @MethodName,
        @ErrorMessage, @StackTrace, @RequestURL, @RequestData, @IPAddress
    );
END
GO


