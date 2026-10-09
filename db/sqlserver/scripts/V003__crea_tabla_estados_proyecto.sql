-- Script: V003__crea_tabla_estados_proyecto.sql
-- Spec:   RF-001
-- Fecha:  2026-10-09
-- Desc:   Crea tabla EstadosProyecto con CHECKs y los indices unicos filtrados
--         UX_EstadosProyecto_EsInicial (maximo un estado inicial) y UX_EstadosProyecto_Codigo
--         (codigos de estados de sistema unicos). Indices filtrados: especificos de SQL Server.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requerido para crear indices filtrados

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.EstadosProyecto', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.EstadosProyecto (
            Id                INT IDENTITY(1,1) NOT NULL,
            Nombre            NVARCHAR(50)      NOT NULL,
            Descripcion       NVARCHAR(250)     NULL,
            Orden             INT               NOT NULL,
            EsInicial         BIT               NOT NULL CONSTRAINT DF_EstadosProyecto_EsInicial DEFAULT (0),
            EsFinal           BIT               NOT NULL CONSTRAINT DF_EstadosProyecto_EsFinal DEFAULT (0),
            Activo            BIT               NOT NULL CONSTRAINT DF_EstadosProyecto_Activo DEFAULT (1),
            Codigo            NVARCHAR(30)      NULL,
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_EstadosProyecto_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_EstadosProyecto PRIMARY KEY (Id),
            CONSTRAINT UQ_EstadosProyecto_Nombre UNIQUE (Nombre),
            CONSTRAINT CK_EstadosProyecto_Orden CHECK (Orden BETWEEN 1 AND 9999),
            CONSTRAINT CK_EstadosProyecto_InicialNoFinal CHECK (NOT (EsInicial = 1 AND EsFinal = 1)),
            CONSTRAINT CK_EstadosProyecto_InicialActivo CHECK (EsInicial = 0 OR Activo = 1),
            CONSTRAINT CK_EstadosProyecto_SistemaNoInicial CHECK (Codigo IS NULL OR EsInicial = 0)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'UX_EstadosProyecto_EsInicial' AND object_id = OBJECT_ID(N'dbo.EstadosProyecto'))
        CREATE UNIQUE NONCLUSTERED INDEX UX_EstadosProyecto_EsInicial
            ON dbo.EstadosProyecto (EsInicial)
            WHERE EsInicial = 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'UX_EstadosProyecto_Codigo' AND object_id = OBJECT_ID(N'dbo.EstadosProyecto'))
        CREATE UNIQUE NONCLUSTERED INDEX UX_EstadosProyecto_Codigo
            ON dbo.EstadosProyecto (Codigo)
            WHERE Codigo IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V003')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V003', N'crea_tabla_estados_proyecto');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
