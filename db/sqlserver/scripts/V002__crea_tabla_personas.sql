-- Script: V002__crea_tabla_personas.sql
-- Spec:   RF-001
-- Fecha:  2026-10-09
-- Desc:   Crea tabla Personas (catalogo minimo: responsables y usuario actual provisional)

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Personas', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Personas (
            Id                INT IDENTITY(1,1) NOT NULL,
            Nombre            NVARCHAR(150)     NOT NULL,
            Email             NVARCHAR(254)     NOT NULL,
            EsAdmin           BIT               NOT NULL CONSTRAINT DF_Personas_EsAdmin DEFAULT (0),
            Activo            BIT               NOT NULL CONSTRAINT DF_Personas_Activo DEFAULT (1),
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_Personas_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_Personas PRIMARY KEY (Id),
            CONSTRAINT UQ_Personas_Email UNIQUE (Email)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V002')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V002', N'crea_tabla_personas');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
