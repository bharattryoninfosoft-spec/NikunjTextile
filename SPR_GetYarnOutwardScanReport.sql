-- ==============================================================================================
-- Stored Procedure: SPR_GetYarnOutwardScanReport
-- Purpose: Data source for Crystal Report (.rpt) for Yarn Outward Scan Challan / PDF
-- Parameter: @YarnOutwardID INT (Single Parameter)
-- Description: Dynamically binds CompanyMaster from database tables.
--              Returns Master Header, Company Info, and Detail Items matching the 8 columns:
--              SrNo, YarnCompany, YarnMaterial, Color, ColorCode, Location, BoxNo, BarcodeNo, NetWeight.
-- ==============================================================================================

IF OBJECT_ID('[dbo].[SPR_GetYarnOutwardScanReport]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[SPR_GetYarnOutwardScanReport];
GO

CREATE PROCEDURE [dbo].[SPR_GetYarnOutwardScanReport]
    @YarnOutwardID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    -- 1. Check if Scanned Records exist
    IF EXISTS (
        SELECT 1 
        FROM YARNOUTWARDScanMaster M
        INNER JOIN YARNOUTWARDScanMasterDetails D ON M.YarnOutwardScanID = D.YarnOutwardScanID
        WHERE M.YarnOutwardID = @YarnOutwardID
    )
    BEGIN
        -- COMPLETED / SCANNED CHALLAN
        SELECT
            -- Dynamic Company Master from Table Join
            CM.CompanyId,
            ISNULL(CM.CompanyName, '') AS CompanyName,
            ISNULL(CM.BusinessAddress, '') AS CompanyAddress,
            ISNULL(CM.GSTIN, '') AS CompanyGSTIN,
            ISNULL(CM.MobileNo, '') AS CompanyMobile,

            -- Challan / Outward Header
            M.YarnOutwardScanID,
            M.YarnOutwardID,
            ISNULL(M.ChallanNo, YOM.OutwardListNo) AS ChallanNo,
            CONVERT(VARCHAR(10), ISNULL(M.ChallanDate, YOM.OutwardListDate), 105) AS ChallanDate,
            ISNULL(M.ChallanDate, YOM.OutwardListDate) AS ChallanDateTime,

            -- Party Info (PM if sundry party <> 3, JM if jobber = 3)
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.Id 
                ELSE PM.PartyId 
            END AS PartyId,
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.PartyName 
                ELSE PM.PartyName 
            END AS PartyName,
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.BillingAddress 
                ELSE PM.BillingAddress 
            END AS Address,
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.GSTIN 
                ELSE PM.GSTIN 
            END AS PartyGSTIN,
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.MobileNumber 
                ELSE PM.MobileNo 
            END AS PartyMobile,

            -- Godown Location
            GLM.GodownLocationID,
            GLM.LocationTitle AS Location,

            -- Detail Items (Exact 8 Columns for Crystal Report)
            ROW_NUMBER() OVER (ORDER BY D.YarnOutwardScanDetailID) AS SrNo,
            D.YarnOutwardScanDetailID,
            D.YarnCompany AS YarnCompanyId,
            ISNULL(CPM.PartyName, '') AS YarnCompany,
            D.YarnMaterial AS YarnMaterialID,
            YMM.YarnMaterial,
            D.YarnColour AS YarnColorID,
            YCM.YarnColor AS Color,
            ISNULL(YPD.CompanyCode, '') AS ColorCode,
            D.BoxNo,
            ISNULL(D.BarcodeNo, '') AS BarcodeNo,
            CAST(ISNULL(D.NetWeight, 0) AS DECIMAL(18,2)) AS NetWeight,

            -- Running / Summary Totals
            COUNT(D.YarnOutwardScanDetailID) OVER() AS TotalBoxes,
            CAST(SUM(ISNULL(D.NetWeight, 0)) OVER() AS DECIMAL(18,2)) AS TotalNetWeight

        FROM YARNOUTWARDScanMasterDetails D
        INNER JOIN YARNOUTWARDScanMaster M 
            ON M.YarnOutwardScanID = D.YarnOutwardScanID
        LEFT JOIN YarnOutwardMaster YOM 
            ON YOM.YarnOutwardID = M.YarnOutwardID
        OUTER APPLY (
            SELECT TOP (1) 
                C.CompanyId,
                C.CompanyName,
                C.BusinessAddress,
                C.GSTIN,
                C.MobileNo
            FROM CompanyMaster C
            WHERE (ISNULL(YOM.CompanyId, 0) > 0 AND C.CompanyId = YOM.CompanyId)
               OR (ISNULL(YOM.CompanyId, 0) = 0 AND ISNULL(C.is_default, 0) = 1)
            ORDER BY C.is_default DESC, C.CompanyId ASC
        ) CM
        LEFT JOIN PartyMaster PM 
            ON PM.PartyId = D.PartyId AND ISNULL(YOM.SundryPartyID, 0) <> 3
        LEFT JOIN JobberMaster JM 
            ON JM.Id = D.PartyId AND ISNULL(YOM.SundryPartyID, 0) = 3
        LEFT JOIN PartyMaster CPM 
            ON CPM.PartyId = D.YarnCompany
        OUTER APPLY (
            SELECT TOP (1) YPD.CompanyCode
            FROM YarnPODetails YPD
            WHERE YPD.YarnColorID = D.YarnColour
              AND YPD.YarnMaterialID = D.YarnMaterial
            ORDER BY YPD.YarnPODetailID
        ) YPD
        INNER JOIN YarnMaterialMaster YMM 
            ON YMM.YarnMaterialID = D.YarnMaterial
        INNER JOIN YarnColorMaster YCM 
            ON YCM.YarnColorID = D.YarnColour
        INNER JOIN GodownLocationMaster GLM 
            ON GLM.GodownLocationID = D.GodownLocationID

        WHERE M.YarnOutwardID = @YarnOutwardID

        ORDER BY D.YarnOutwardScanDetailID;
    END
    ELSE
    BEGIN
        -- PENDING CHALLAN (From YarnOutwardMaster & YarnOutwardDetail before scan)
        SELECT
            -- Dynamic Company Master from Table Join
            CM.CompanyId,
            ISNULL(CM.CompanyName, '') AS CompanyName,
            ISNULL(CM.BusinessAddress, '') AS CompanyAddress,
            ISNULL(CM.GSTIN, '') AS CompanyGSTIN,
            ISNULL(CM.MobileNo, '') AS CompanyMobile,

            -- Challan / Outward Header
            0 AS YarnOutwardScanID,
            YOM.YarnOutwardID,
            YOM.OutwardListNo AS ChallanNo,
            CONVERT(VARCHAR(10), YOM.OutwardListDate, 105) AS ChallanDate,
            YOM.OutwardListDate AS ChallanDateTime,

            -- Party Info
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.Id 
                ELSE PM.PartyId 
            END AS PartyId,
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.PartyName 
                ELSE PM.PartyName 
            END AS PartyName,
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.BillingAddress 
                ELSE PM.BillingAddress 
            END AS Address,
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.GSTIN 
                ELSE PM.GSTIN 
            END AS PartyGSTIN,
            CASE 
                WHEN ISNULL(YOM.SundryPartyID, 0) = 3 THEN JM.MobileNumber 
                ELSE PM.MobileNo 
            END AS PartyMobile,

            -- Godown Location
            GLM.GodownLocationID,
            GLM.LocationTitle AS Location,

            -- Detail Items
            ROW_NUMBER() OVER (ORDER BY YOD.YarnOutwardDetailID) AS SrNo,
            YOD.YarnOutwardDetailID AS YarnOutwardScanDetailID,
            YOD.YarnCompanyID AS YarnCompanyId,
            ISNULL(CPM.PartyName, '') AS YarnCompany,
            YOD.YarnMaterialID,
            YMM.YarnMaterial,
            YOD.YarnColorID,
            YCM.YarnColor AS Color,
            ISNULL(YPD.CompanyCode, '') AS ColorCode,
            CAST(YOD.NoOfBox AS NVARCHAR(50)) AS BoxNo,
            '' AS BarcodeNo,
            CAST(0 AS DECIMAL(18,2)) AS NetWeight,

            -- Totals
            COUNT(YOD.YarnOutwardDetailID) OVER() AS TotalBoxes,
            CAST(0 AS DECIMAL(18,2)) AS TotalNetWeight

        FROM YarnOutwardMaster YOM
        INNER JOIN YarnOutwardDetail YOD 
            ON YOD.YarnOutwardID = YOM.YarnOutwardID
        OUTER APPLY (
            SELECT TOP (1) 
                C.CompanyId,
                C.CompanyName,
                C.BusinessAddress,
                C.GSTIN,
                C.MobileNo
            FROM CompanyMaster C
            WHERE (ISNULL(YOM.CompanyId, 0) > 0 AND C.CompanyId = YOM.CompanyId)
               OR (ISNULL(YOM.CompanyId, 0) = 0 AND ISNULL(C.is_default, 0) = 1)
            ORDER BY C.is_default DESC, C.CompanyId ASC
        ) CM
        LEFT JOIN PartyMaster PM 
            ON PM.PartyId = YOM.PartyId AND ISNULL(YOM.SundryPartyID, 0) <> 3
        LEFT JOIN JobberMaster JM 
            ON JM.Id = YOM.PartyId AND ISNULL(YOM.SundryPartyID, 0) = 3
        LEFT JOIN PartyMaster CPM 
            ON CPM.PartyId = YOD.YarnCompanyID
        OUTER APPLY (
            SELECT TOP (1) YPD.CompanyCode
            FROM YarnPODetails YPD
            WHERE YPD.YarnColorID = YOD.YarnColorID
              AND YPD.YarnMaterialID = YOD.YarnMaterialID
            ORDER BY YPD.YarnPODetailID
        ) YPD
        INNER JOIN YarnMaterialMaster YMM 
            ON YMM.YarnMaterialID = YOD.YarnMaterialID
        INNER JOIN YarnColorMaster YCM 
            ON YCM.YarnColorID = YOD.YarnColorID
        LEFT JOIN GodownLocationMaster GLM 
            ON GLM.GodownLocationID = YOD.GodownLocationID

        WHERE YOM.YarnOutwardID = @YarnOutwardID

        ORDER BY YOD.YarnOutwardDetailID;
    END
END;
GO
