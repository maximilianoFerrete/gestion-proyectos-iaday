-- Script: V009__crea_tabla_incidencias.sql
-- Spec:   RF-002
-- Fecha:  2026-10-09
-- Desc:   Crea la tabla Incidencias con:
--         - UQ_Incidencias_ProyectoId_Numero (numeracion por proyecto) y UQ_Incidencias_Codigo.
--         - UQ_Incidencias_ProyectoId_Id: destino de la FK compuesta autorreferencial del padre.
--         - FK_Incidencias_Agrupaciones (ProyectoId, AgrupacionId) -> Agrupaciones (ProyectoId, Id)
--           contra UQ_Agrupaciones_ProyectoId_Id (RF-001), sin ON DELETE SET NULL.
--         - FK_Incidencias_Incidencias_Padre (ProyectoId, PadreId) -> Incidencias (ProyectoId, Id):
--           padre e hija del mismo proyecto. Con AgrupacionId/PadreId nulos la FK no se evalua.
--         - CK_Incidencias_PadreDistinto e indices de FK / CTE de descendientes.
--         Ciclos, relacion de tipos, estado dentro del flujo, herencia de agrupacion y
--         vencimientos los valida el backend. Requiere V005, V007 y V008.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Incidencias', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Incidencias (
            Id                INT IDENTITY(1,1) NOT NULL,
            ProyectoId        INT               NOT NULL,
            Numero            INT               NOT NULL,
            Codigo            NVARCHAR(30)      NOT NULL,
            Titulo            NVARCHAR(200)     NOT NULL,
            Descripcion       NVARCHAR(4000)    NOT NULL,
            TipoId            INT               NOT NULL,
            EstadoId          INT               NOT NULL,
            PrioridadId       INT               NOT NULL,
            ResponsableId     INT               NULL,
            FechaVencimiento  DATE              NULL,
            AgrupacionId      INT               NULL,
            PadreId           INT               NULL,
            FechaCreacion     DATETIME2(0)      NOT NULL CONSTRAINT DF_Incidencias_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaModificacion DATETIME2(0)      NULL,
            CONSTRAINT PK_Incidencias PRIMARY KEY (Id),
            -- Tambien sirve de indice de la FK ProyectoId y para ordenar el listado por numero.
            CONSTRAINT UQ_Incidencias_ProyectoId_Numero UNIQUE (ProyectoId, Numero),
            CONSTRAINT UQ_Incidencias_Codigo UNIQUE (Codigo),
            -- Destino de FK_Incidencias_Incidencias_Padre.
            CONSTRAINT UQ_Incidencias_ProyectoId_Id UNIQUE (ProyectoId, Id),
            CONSTRAINT FK_Incidencias_Proyectos_ProyectoId
                FOREIGN KEY (ProyectoId) REFERENCES dbo.Proyectos (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Incidencias_TiposIncidencia_TipoId
                FOREIGN KEY (TipoId) REFERENCES dbo.TiposIncidencia (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Incidencias_EstadosIncidencia_EstadoId
                FOREIGN KEY (EstadoId) REFERENCES dbo.EstadosIncidencia (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Incidencias_PrioridadesIncidencia_PrioridadId
                FOREIGN KEY (PrioridadId) REFERENCES dbo.PrioridadesIncidencia (Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Incidencias_Personas_ResponsableId
                FOREIGN KEY (ResponsableId) REFERENCES dbo.Personas (Id) ON DELETE NO ACTION,
            -- Agrupacion del mismo proyecto (RF-001). Sin ON DELETE SET NULL: la desasociacion
            -- al eliminar una agrupacion la hace el backend en la misma transaccion.
            CONSTRAINT FK_Incidencias_Agrupaciones
                FOREIGN KEY (ProyectoId, AgrupacionId) REFERENCES dbo.Agrupaciones (ProyectoId, Id) ON DELETE NO ACTION,
            CONSTRAINT CK_Incidencias_Numero CHECK (Numero >= 1),
            CONSTRAINT CK_Incidencias_PadreDistinto CHECK (PadreId IS NULL OR PadreId <> Id)
        );
    END;

    -- FK compuesta autorreferencial: se agrega despues de crear UQ_Incidencias_ProyectoId_Id.
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE name = N'FK_Incidencias_Incidencias_Padre'
                     AND parent_object_id = OBJECT_ID(N'dbo.Incidencias'))
        ALTER TABLE dbo.Incidencias WITH CHECK
            ADD CONSTRAINT FK_Incidencias_Incidencias_Padre
                FOREIGN KEY (ProyectoId, PadreId) REFERENCES dbo.Incidencias (ProyectoId, Id) ON DELETE NO ACTION;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Incidencias_ProyectoId_AgrupacionId' AND object_id = OBJECT_ID(N'dbo.Incidencias'))
        CREATE NONCLUSTERED INDEX IX_Incidencias_ProyectoId_AgrupacionId ON dbo.Incidencias (ProyectoId, AgrupacionId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Incidencias_ProyectoId_PadreId' AND object_id = OBJECT_ID(N'dbo.Incidencias'))
        CREATE NONCLUSTERED INDEX IX_Incidencias_ProyectoId_PadreId ON dbo.Incidencias (ProyectoId, PadreId);

    -- CTE recursiva de descendientes (JOIN por PadreId).
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Incidencias_PadreId' AND object_id = OBJECT_ID(N'dbo.Incidencias'))
        CREATE NONCLUSTERED INDEX IX_Incidencias_PadreId ON dbo.Incidencias (PadreId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Incidencias_TipoId' AND object_id = OBJECT_ID(N'dbo.Incidencias'))
        CREATE NONCLUSTERED INDEX IX_Incidencias_TipoId ON dbo.Incidencias (TipoId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Incidencias_EstadoId' AND object_id = OBJECT_ID(N'dbo.Incidencias'))
        CREATE NONCLUSTERED INDEX IX_Incidencias_EstadoId ON dbo.Incidencias (EstadoId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Incidencias_PrioridadId' AND object_id = OBJECT_ID(N'dbo.Incidencias'))
        CREATE NONCLUSTERED INDEX IX_Incidencias_PrioridadId ON dbo.Incidencias (PrioridadId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Incidencias_ResponsableId' AND object_id = OBJECT_ID(N'dbo.Incidencias'))
        CREATE NONCLUSTERED INDEX IX_Incidencias_ResponsableId ON dbo.Incidencias (ResponsableId);

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V009')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V009', N'crea_tabla_incidencias');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
