-- Script: V006__agrega_contador_incidencias_a_proyectos.sql
-- Spec:   RF-002
-- Fecha:  2026-10-09
-- Desc:   Agrega a Proyectos la columna UltimoNumeroIncidencia (contador de numeracion de
--         incidencias por proyecto, seccion 0.3) con DF_Proyectos_UltimoNumeroIncidencia = 0
--         (tambien para filas existentes) y CK_Proyectos_UltimoNumeroIncidencia (>= 0).
--         El CHECK se agrega con SQL dinamico porque referencia una columna creada en el
--         mismo lote (evita el error de compilacion "Invalid column name").

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.Proyectos', N'UltimoNumeroIncidencia') IS NULL
        -- NOT NULL + DEFAULT: las filas existentes quedan con 0.
        ALTER TABLE dbo.Proyectos
            ADD UltimoNumeroIncidencia INT NOT NULL
                CONSTRAINT DF_Proyectos_UltimoNumeroIncidencia DEFAULT (0);

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE name = N'CK_Proyectos_UltimoNumeroIncidencia'
                     AND parent_object_id = OBJECT_ID(N'dbo.Proyectos'))
        EXEC (N'ALTER TABLE dbo.Proyectos WITH CHECK
                  ADD CONSTRAINT CK_Proyectos_UltimoNumeroIncidencia CHECK (UltimoNumeroIncidencia >= 0);');

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V006')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V006', N'agrega_contador_incidencias_a_proyectos');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
