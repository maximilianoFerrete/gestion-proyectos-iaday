-- Script: V005__crea_tabla_agrupaciones.sql
-- Spec:   RF-001
-- Fecha:  2026-10-09
-- Desc:   Crea tabla Agrupaciones con FK a Proyectos, unicidad de nombre por proyecto y
--         UQ_Agrupaciones_ProyectoId_Id (preparacion para la FK compuesta de Incidencias, RF01.11)

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Agrupaciones', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Agrupaciones (
            Id                INT IDENTITY(1,1) NOT NULL,
            ProyectoId        INT               NOT NULL,
            Nombre            NVARCHAR(100)     NOT NULL,
            Descripcion       NVARCHAR(1000)    NOT NULL,
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_Agrupaciones_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_Agrupaciones PRIMARY KEY (Id),
            CONSTRAINT FK_Agrupaciones_Proyectos_ProyectoId
                FOREIGN KEY (ProyectoId) REFERENCES dbo.Proyectos (Id) ON DELETE NO ACTION,
            -- Tambien sirve de indice de la FK ProyectoId.
            CONSTRAINT UQ_Agrupaciones_ProyectoId_Nombre UNIQUE (ProyectoId, Nombre),
            -- Destino de la futura FK compuesta Incidencias (ProyectoId, AgrupacionId).
            CONSTRAINT UQ_Agrupaciones_ProyectoId_Id UNIQUE (ProyectoId, Id)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V005')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V005', N'crea_tabla_agrupaciones');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
