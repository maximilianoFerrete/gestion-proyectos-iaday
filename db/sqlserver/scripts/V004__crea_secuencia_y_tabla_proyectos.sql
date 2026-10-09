-- Script: V004__crea_secuencia_y_tabla_proyectos.sql
-- Spec:   RF-001
-- Fecha:  2026-10-09
-- Desc:   Crea la secuencia SQ_Proyectos_Numero (codigo legible PRJ-0001, RN-14) y la tabla
--         Proyectos con FKs a Personas y EstadosProyecto, CHECKs e indices

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.SQ_Proyectos_Numero', N'SO') IS NULL
        CREATE SEQUENCE dbo.SQ_Proyectos_Numero AS INT START WITH 1 INCREMENT BY 1 NO CYCLE;

    IF OBJECT_ID(N'dbo.Proyectos', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Proyectos (
            Id                 INT IDENTITY(1,1) NOT NULL,
            Codigo             NVARCHAR(20)      NOT NULL,
            Nombre             NVARCHAR(150)     NOT NULL,
            Descripcion        NVARCHAR(2000)    NOT NULL,
            ResponsableId      INT               NOT NULL,
            FechaInicio        DATE              NOT NULL,
            FechaObjetivo      DATE              NOT NULL,
            EstadoId           INT               NOT NULL,
            FechaArchivado     DATETIME2(0)      NULL,
            ArchivadoPorId     INT               NULL,
            EstadoAlArchivarId INT               NULL,
            FechaCreacion      DATETIME2(0)      NOT NULL CONSTRAINT DF_Proyectos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion  DATETIME2(0)      NULL,
            CONSTRAINT PK_Proyectos PRIMARY KEY (Id),
            CONSTRAINT UQ_Proyectos_Codigo UNIQUE (Codigo),
            CONSTRAINT FK_Proyectos_Personas_ResponsableId
                FOREIGN KEY (ResponsableId) REFERENCES dbo.Personas (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Proyectos_EstadosProyecto_EstadoId
                FOREIGN KEY (EstadoId) REFERENCES dbo.EstadosProyecto (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Proyectos_Personas_ArchivadoPorId
                FOREIGN KEY (ArchivadoPorId) REFERENCES dbo.Personas (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Proyectos_EstadosProyecto_EstadoAlArchivarId
                FOREIGN KEY (EstadoAlArchivarId) REFERENCES dbo.EstadosProyecto (Id) ON DELETE NO ACTION,
            CONSTRAINT CK_Proyectos_Fechas CHECK (FechaObjetivo >= FechaInicio),
            CONSTRAINT CK_Proyectos_Archivado CHECK (
                   (FechaArchivado IS NULL AND ArchivadoPorId IS NULL AND EstadoAlArchivarId IS NULL)
                OR (FechaArchivado IS NOT NULL AND ArchivadoPorId IS NOT NULL AND EstadoAlArchivarId IS NOT NULL)
            )
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proyectos_ResponsableId' AND object_id = OBJECT_ID(N'dbo.Proyectos'))
        CREATE NONCLUSTERED INDEX IX_Proyectos_ResponsableId ON dbo.Proyectos (ResponsableId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proyectos_EstadoId' AND object_id = OBJECT_ID(N'dbo.Proyectos'))
        CREATE NONCLUSTERED INDEX IX_Proyectos_EstadoId ON dbo.Proyectos (EstadoId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proyectos_ArchivadoPorId' AND object_id = OBJECT_ID(N'dbo.Proyectos'))
        CREATE NONCLUSTERED INDEX IX_Proyectos_ArchivadoPorId ON dbo.Proyectos (ArchivadoPorId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proyectos_EstadoAlArchivarId' AND object_id = OBJECT_ID(N'dbo.Proyectos'))
        CREATE NONCLUSTERED INDEX IX_Proyectos_EstadoAlArchivarId ON dbo.Proyectos (EstadoAlArchivarId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proyectos_FechaArchivado_Codigo' AND object_id = OBJECT_ID(N'dbo.Proyectos'))
        CREATE NONCLUSTERED INDEX IX_Proyectos_FechaArchivado_Codigo ON dbo.Proyectos (FechaArchivado, Codigo);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Proyectos_Nombre' AND object_id = OBJECT_ID(N'dbo.Proyectos'))
        CREATE NONCLUSTERED INDEX IX_Proyectos_Nombre ON dbo.Proyectos (Nombre);

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V004')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V004', N'crea_secuencia_y_tabla_proyectos');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
