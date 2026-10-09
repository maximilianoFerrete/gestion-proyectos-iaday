-- Script: V001__crea_tabla_versiones_esquema.sql
-- Spec:   RF-001
-- Fecha:  2026-10-09
-- Desc:   Crea la tabla de control VersionesEsquema y registra V001

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.VersionesEsquema', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VersionesEsquema (
            Version         NVARCHAR(50)  NOT NULL,
            Descripcion     NVARCHAR(250) NOT NULL,
            FechaAplicacion DATETIME2(0)  NOT NULL
                CONSTRAINT DF_VersionesEsquema_FechaAplicacion DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_VersionesEsquema PRIMARY KEY (Version)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V001')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V001', N'crea_tabla_versiones_esquema');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
