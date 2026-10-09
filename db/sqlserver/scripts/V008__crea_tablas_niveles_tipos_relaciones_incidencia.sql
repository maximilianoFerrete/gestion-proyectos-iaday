-- Script: V008__crea_tablas_niveles_tipos_relaciones_incidencia.sql
-- Spec:   RF-002
-- Fecha:  2026-10-09
-- Desc:   Crea NivelesIncidencia, TiposIncidencia (FK a NivelesIncidencia y FlujosIncidencia)
--         y RelacionesTiposIncidencia (relaciones permitidas tipo padre -> tipo hijo, filas
--         inmutables). La regla "nivel del padre < nivel del hijo" (RN-08) la valida el backend.
--         Requiere V007 (FlujosIncidencia).

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- NivelesIncidencia ---------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.NivelesIncidencia', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.NivelesIncidencia (
            Id                INT IDENTITY(1,1) NOT NULL,
            Numero            INT               NOT NULL,
            Nombre            NVARCHAR(50)      NOT NULL,
            Descripcion       NVARCHAR(250)     NULL,
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_NivelesIncidencia_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_NivelesIncidencia PRIMARY KEY (Id),
            CONSTRAINT UQ_NivelesIncidencia_Numero UNIQUE (Numero),
            CONSTRAINT UQ_NivelesIncidencia_Nombre UNIQUE (Nombre),
            CONSTRAINT CK_NivelesIncidencia_Numero CHECK (Numero BETWEEN 1 AND 99)
        );
    END;

    -- TiposIncidencia -----------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.TiposIncidencia', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TiposIncidencia (
            Id                INT IDENTITY(1,1) NOT NULL,
            Nombre            NVARCHAR(50)      NOT NULL,
            Descripcion       NVARCHAR(250)     NULL,
            NivelId           INT               NOT NULL,
            FlujoId           INT               NOT NULL,
            Activo            BIT               NOT NULL CONSTRAINT DF_TiposIncidencia_Activo DEFAULT (1),
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_TiposIncidencia_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_TiposIncidencia PRIMARY KEY (Id),
            CONSTRAINT UQ_TiposIncidencia_Nombre UNIQUE (Nombre),
            CONSTRAINT FK_TiposIncidencia_NivelesIncidencia_NivelId
                FOREIGN KEY (NivelId) REFERENCES dbo.NivelesIncidencia (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_TiposIncidencia_FlujosIncidencia_FlujoId
                FOREIGN KEY (FlujoId) REFERENCES dbo.FlujosIncidencia (Id) ON DELETE NO ACTION
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TiposIncidencia_NivelId' AND object_id = OBJECT_ID(N'dbo.TiposIncidencia'))
        CREATE NONCLUSTERED INDEX IX_TiposIncidencia_NivelId ON dbo.TiposIncidencia (NivelId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TiposIncidencia_FlujoId' AND object_id = OBJECT_ID(N'dbo.TiposIncidencia'))
        CREATE NONCLUSTERED INDEX IX_TiposIncidencia_FlujoId ON dbo.TiposIncidencia (FlujoId);

    -- RelacionesTiposIncidencia (solo INSERT; sin FechaModificacion, RN-09) -----------------
    IF OBJECT_ID(N'dbo.RelacionesTiposIncidencia', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.RelacionesTiposIncidencia (
            Id            INT IDENTITY(1,1) NOT NULL,
            TipoPadreId   INT               NOT NULL,
            TipoHijoId    INT               NOT NULL,
            FechaCreacion DATETIME2(0)      NOT NULL CONSTRAINT DF_RelacionesTiposIncidencia_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_RelacionesTiposIncidencia PRIMARY KEY (Id),
            CONSTRAINT FK_RelacionesTiposIncidencia_TiposIncidencia_TipoPadreId
                FOREIGN KEY (TipoPadreId) REFERENCES dbo.TiposIncidencia (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_RelacionesTiposIncidencia_TiposIncidencia_TipoHijoId
                FOREIGN KEY (TipoHijoId) REFERENCES dbo.TiposIncidencia (Id) ON DELETE NO ACTION,
            -- Tambien sirve de indice de la FK TipoPadreId.
            CONSTRAINT UQ_RelacionesTiposIncidencia_TipoPadreId_TipoHijoId UNIQUE (TipoPadreId, TipoHijoId),
            CONSTRAINT CK_RelacionesTiposIncidencia_TiposDistintos CHECK (TipoPadreId <> TipoHijoId)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RelacionesTiposIncidencia_TipoHijoId' AND object_id = OBJECT_ID(N'dbo.RelacionesTiposIncidencia'))
        CREATE NONCLUSTERED INDEX IX_RelacionesTiposIncidencia_TipoHijoId ON dbo.RelacionesTiposIncidencia (TipoHijoId);

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V008')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V008', N'crea_tablas_niveles_tipos_relaciones_incidencia');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
