-- Script: V012__crea_configuraciones_tablero_proyecto.sql
-- Spec:   RF-005
-- Fecha:  2026-10-10
-- Desc:   Crea ConfiguracionesTableroProyecto: columnas visibles de la grilla del tablero de
--         un proyecto. Relacion 1:0..1 con Proyectos (PK = FK ProyectoId); la ausencia de
--         fila significa configuracion por defecto (todas visibles, RN-30). Los BIT no tienen
--         DEFAULT: el backend informa siempre los tres valores. FechaModificacion (UTC) la
--         asigna el backend en cada guardado. Upsert en el backend con UPDATE + INSERT (sin
--         MERGE). Requiere V004.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.ConfiguracionesTableroProyecto', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ConfiguracionesTableroProyecto (
            ProyectoId         INT          NOT NULL,
            MostrarEstado      BIT          NOT NULL,
            MostrarEstimacion  BIT          NOT NULL,
            MostrarResponsable BIT          NOT NULL,
            FechaModificacion  DATETIME2(0) NOT NULL,
            CONSTRAINT PK_ConfiguracionesTableroProyecto PRIMARY KEY (ProyectoId),
            CONSTRAINT FK_ConfiguracionesTableroProyecto_Proyectos_ProyectoId
                FOREIGN KEY (ProyectoId) REFERENCES dbo.Proyectos (Id) ON DELETE NO ACTION
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'V012')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'V012', N'crea_configuraciones_tablero_proyecto');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
