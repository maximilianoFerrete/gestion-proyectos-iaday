-- Script: S001__estados_proyecto_iniciales.sql
-- Spec:   RF-001
-- Fecha:  2026-10-09
-- Desc:   Semilla de EstadosProyecto (6 configurables + estado de sistema ARCHIVADO).
--         Idempotente: inserta solo los que no existen por Nombre (o por Codigo, para el de
--         sistema). No modifica filas existentes (respeta cambios hechos por el usuario).
--         Si ya hay un estado inicial, "En planificación" se inserta sin marca inicial para no
--         violar UX_EstadosProyecto_EsInicial.
-- Nota:   archivo UTF-8; ejecutar sqlcmd con -f 65001.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requerido para DML sobre tablas con indices filtrados

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Semilla TABLE (
        Nombre      NVARCHAR(50)  NOT NULL,
        Descripcion NVARCHAR(250) NULL,
        Orden       INT           NOT NULL,
        EsInicial   BIT           NOT NULL,
        EsFinal     BIT           NOT NULL,
        Activo      BIT           NOT NULL,
        Codigo      NVARCHAR(30)  NULL
    );

    INSERT INTO @Semilla (Nombre, Descripcion, Orden, EsInicial, EsFinal, Activo, Codigo)
    VALUES
        (N'En planificación', N'Se está definiendo el alcance y organizando el trabajo.',                        10, 1, 0, 1, NULL),
        (N'Planificado',      N'La planificación está definida y el proyecto está pendiente de inicio.',         20, 0, 0, 1, NULL),
        (N'En curso',         N'El proyecto se encuentra en ejecución.',                                         30, 0, 0, 1, NULL),
        (N'Pausado',          N'El proyecto está suspendido temporalmente.',                                     40, 0, 0, 1, NULL),
        (N'Finalizado',       N'El proyecto fue completado.',                                                    50, 0, 1, 1, NULL),
        (N'Cancelado',        N'Se decidió no continuar con el proyecto.',                                       60, 0, 1, 1, NULL),
        (N'Archivado',        N'Proyecto archivado: solo lectura, conservado para consultas y reportes.',      9999, 0, 0, 1, N'ARCHIVADO');

    INSERT INTO dbo.EstadosProyecto (Nombre, Descripcion, Orden, EsInicial, EsFinal, Activo, Codigo)
    SELECT s.Nombre,
           s.Descripcion,
           s.Orden,
           CASE WHEN s.EsInicial = 1
                 AND NOT EXISTS (SELECT 1 FROM dbo.EstadosProyecto e WHERE e.EsInicial = 1)
                THEN 1 ELSE 0 END,
           s.EsFinal,
           s.Activo,
           s.Codigo
    FROM @Semilla s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.EstadosProyecto e WHERE e.Nombre = s.Nombre)
      AND (s.Codigo IS NULL
           OR NOT EXISTS (SELECT 1 FROM dbo.EstadosProyecto e WHERE e.Codigo = s.Codigo))
    ORDER BY s.Orden;

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'S001')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'S001', N'seed_estados_proyecto_iniciales');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
