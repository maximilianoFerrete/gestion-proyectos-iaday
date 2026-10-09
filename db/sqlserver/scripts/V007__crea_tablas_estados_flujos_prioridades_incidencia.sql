-- Script: V007__crea_tablas_estados_flujos_prioridades_incidencia.sql
-- Spec:   RF-002
-- Fecha:  2026-10-09
-- Desc:   Crea las tablas de configuracion EstadosIncidencia (catalogo), FlujosIncidencia,
--         FlujosIncidenciaEstados (estados de un flujo, con marcas inicial/final) y
--         PrioridadesIncidencia. Indices unicos filtrados (especificos de SQL Server):
--         UX_FlujosIncidenciaEstados_FlujoId_EsInicial (maximo un inicial por flujo) y
--         UX_PrioridadesIncidencia_EsPredeterminada (maximo una prioridad predeterminada).

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requerido para crear indices filtrados

BEGIN TRY
    BEGIN TRANSACTION;

    -- EstadosIncidencia ---------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.EstadosIncidencia', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.EstadosIncidencia (
            Id                INT IDENTITY(1,1) NOT NULL,
            Nombre            NVARCHAR(50)      NOT NULL,
            Descripcion       NVARCHAR(250)     NULL,
            Orden             INT               NOT NULL,
            Activo            BIT               NOT NULL CONSTRAINT DF_EstadosIncidencia_Activo DEFAULT (1),
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_EstadosIncidencia_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_EstadosIncidencia PRIMARY KEY (Id),
            CONSTRAINT UQ_EstadosIncidencia_Nombre UNIQUE (Nombre),
            CONSTRAINT CK_EstadosIncidencia_Orden CHECK (Orden BETWEEN 1 AND 9999)
        );
    END;

    -- FlujosIncidencia ----------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.FlujosIncidencia', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.FlujosIncidencia (
            Id                INT IDENTITY(1,1) NOT NULL,
            Nombre            NVARCHAR(100)     NOT NULL,
            Descripcion       NVARCHAR(250)     NULL,
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_FlujosIncidencia_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_FlujosIncidencia PRIMARY KEY (Id),
            CONSTRAINT UQ_FlujosIncidencia_Nombre UNIQUE (Nombre)
        );
    END;

    -- FlujosIncidenciaEstados ---------------------------------------------------------------
    IF OBJECT_ID(N'dbo.FlujosIncidenciaEstados', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.FlujosIncidenciaEstados (
            FlujoId   INT NOT NULL,
            EstadoId  INT NOT NULL,
            Orden     INT NOT NULL,
            EsInicial BIT NOT NULL CONSTRAINT DF_FlujosIncidenciaEstados_EsInicial DEFAULT (0),
            EsFinal   BIT NOT NULL CONSTRAINT DF_FlujosIncidenciaEstados_EsFinal DEFAULT (0),
            -- Un estado aparece una sola vez por flujo; tambien sirve de indice de la FK FlujoId.
            CONSTRAINT PK_FlujosIncidenciaEstados PRIMARY KEY (FlujoId, EstadoId),
            CONSTRAINT FK_FlujosIncidenciaEstados_FlujosIncidencia_FlujoId
                FOREIGN KEY (FlujoId) REFERENCES dbo.FlujosIncidencia (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_FlujosIncidenciaEstados_EstadosIncidencia_EstadoId
                FOREIGN KEY (EstadoId) REFERENCES dbo.EstadosIncidencia (Id) ON DELETE NO ACTION,
            CONSTRAINT CK_FlujosIncidenciaEstados_Orden CHECK (Orden >= 1),
            CONSTRAINT CK_FlujosIncidenciaEstados_InicialNoFinal CHECK (NOT (EsInicial = 1 AND EsFinal = 1))
        );
    END;

    -- Como maximo un estado inicial por flujo ("exactamente uno" lo garantiza el backend, RN-14).
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'UX_FlujosIncidenciaEstados_FlujoId_EsInicial'
                     AND object_id = OBJECT_ID(N'dbo.FlujosIncidenciaEstados'))
        CREATE UNIQUE NONCLUSTERED INDEX UX_FlujosIncidenciaEstados_FlujoId_EsInicial
            ON dbo.FlujosIncidenciaEstados (FlujoId)
            WHERE EsInicial = 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_FlujosIncidenciaEstados_EstadoId'
                     AND object_id = OBJECT_ID(N'dbo.FlujosIncidenciaEstados'))
        CREATE NONCLUSTERED INDEX IX_FlujosIncidenciaEstados_EstadoId
            ON dbo.FlujosIncidenciaEstados (EstadoId);

    -- PrioridadesIncidencia -----------------------------------------------------------------
    IF OBJECT_ID(N'dbo.PrioridadesIncidencia', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PrioridadesIncidencia (
            Id                INT IDENTITY(1,1) NOT NULL,
            Nombre            NVARCHAR(50)      NOT NULL,
            Descripcion       NVARCHAR(250)     NULL,
            Orden             INT               NOT NULL,
            EsPredeterminada  BIT               NOT NULL CONSTRAINT DF_PrioridadesIncidencia_EsPredeterminada DEFAULT (0),
            Activo            BIT               NOT NULL CONSTRAINT DF_PrioridadesIncidencia_Activo DEFAULT (1),
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_PrioridadesIncidencia_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_PrioridadesIncidencia PRIMARY KEY (Id),
            CONSTRAINT UQ_PrioridadesIncidencia_Nombre UNIQUE (Nombre),
            CONSTRAINT CK_PrioridadesIncidencia_Orden CHECK (Orden BETWEEN 1 AND 9999),
            CONSTRAINT CK_PrioridadesIncidencia_PredeterminadaActiva CHECK (EsPredeterminada = 0 OR Activo = 1)
        );
    END;

    -- Como maximo una prioridad predeterminada.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'UX_PrioridadesIncidencia_EsPredeterminada'
                     AND object_id = OBJECT_ID(N'dbo.PrioridadesIncidencia'))
        CREATE UNIQUE NONCLUSTERED INDEX UX_PrioridadesIncidencia_EsPredeterminada
            ON dbo.PrioridadesIncidencia (EsPredeterminada)
            WHERE EsPredeterminada = 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V007')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V007', N'crea_tablas_estados_flujos_prioridades_incidencia');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
