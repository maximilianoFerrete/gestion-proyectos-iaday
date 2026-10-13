-- Script: V011__agrega_estimacion_a_incidencias.sql
-- Spec:   RF-005 (C-RF002-01, cambio sobre RF-002)
-- Fecha:  2026-10-10
-- Desc:   Agrega a Incidencias la columna Estimacion DECIMAL(9,2) NULL (horas; NULL = sin
--         estimar, valor de las filas existentes; 0 es valido) con CK_Incidencias_Estimacion
--         (Estimacion IS NULL OR Estimacion >= 0). El maximo 9999999.99 lo da el tipo
--         DECIMAL(9,2); el backend valida decimales y rango con mensajes propios (RN-34).
--         El CHECK se agrega con SQL dinamico (columna creada en el mismo lote, como V006).
--         Requiere V009.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.Incidencias', N'Estimacion') IS NULL
        ALTER TABLE dbo.Incidencias
            ADD Estimacion DECIMAL(9,2) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE name = N'CK_Incidencias_Estimacion'
                     AND parent_object_id = OBJECT_ID(N'dbo.Incidencias'))
        EXEC (N'ALTER TABLE dbo.Incidencias WITH CHECK
                  ADD CONSTRAINT CK_Incidencias_Estimacion
                  CHECK (Estimacion IS NULL OR Estimacion >= 0);');

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V011')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V011', N'agrega_estimacion_a_incidencias');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
