-- Script: V010__agrega_categoria_a_estados_incidencia_e_indice_seguimiento.sql
-- Spec:   RF-005
-- Fecha:  2026-10-10
-- Desc:   Agrega a EstadosIncidencia la columna Categoria NVARCHAR(20) NULL (categoria de
--         seguimiento; NULL = sin categoria, valor de las filas existentes) con
--         CK_EstadosIncidencia_Categoria (NULL, 'BLOQUEADA' o 'EN_ESPERA').
--         El CHECK se agrega con SQL dinamico porque referencia una columna creada en el
--         mismo lote (como V006).
--         Crea IX_Incidencias_ProyectoId_EstadoId (ProyectoId, EstadoId)
--         INCLUDE (TipoId, AgrupacionId, ResponsableId, FechaVencimiento): cubre los conteos
--         de seguimiento por proyecto/agrupacion.
--         Invariante del backend (no expresable como CHECK): un estado con categoria no es
--         inicial ni final en ningun flujo (RF-005 RN-18). Requiere V007 y V009.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.EstadosIncidencia', N'Categoria') IS NULL
        ALTER TABLE dbo.EstadosIncidencia
            ADD Categoria NVARCHAR(20) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE name = N'CK_EstadosIncidencia_Categoria'
                     AND parent_object_id = OBJECT_ID(N'dbo.EstadosIncidencia'))
        EXEC (N'ALTER TABLE dbo.EstadosIncidencia WITH CHECK
                  ADD CONSTRAINT CK_EstadosIncidencia_Categoria
                  CHECK (Categoria IS NULL OR Categoria IN (N''BLOQUEADA'', N''EN_ESPERA''));');

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_Incidencias_ProyectoId_EstadoId'
                     AND object_id = OBJECT_ID(N'dbo.Incidencias'))
        CREATE NONCLUSTERED INDEX IX_Incidencias_ProyectoId_EstadoId
            ON dbo.Incidencias (ProyectoId, EstadoId)
            INCLUDE (TipoId, AgrupacionId, ResponsableId, FechaVencimiento);

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V010')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V010', N'agrega_categoria_a_estados_incidencia_e_indice_seguimiento');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
