-- Script: S003__configuracion_incidencias_inicial.sql
-- Spec:   RF-002
-- Fecha:  2026-10-09
-- Desc:   Semilla de la configuracion de incidencias. Orden: niveles, estados, flujo y estados
--         del flujo, tipos, relaciones, prioridades.
--         Idempotente: inserta solo lo que no existe por la clave indicada en la spec
--         (niveles por Numero; estados, flujos, tipos y prioridades por Nombre; estados del flujo
--         por (FlujoId, EstadoId); relaciones por par de nombres de tipo). No modifica filas
--         existentes (respeta cambios hechos por el usuario).
--         La marca inicial del flujo se inserta solo si el flujo no tiene ya un inicial
--         (UX_FlujosIncidenciaEstados_FlujoId_EsInicial) y la prioridad predeterminada solo si no
--         existe ya una (UX_PrioridadesIncidencia_EsPredeterminada).
-- Nota:   archivo UTF-8; ejecutar sqlcmd con -f 65001.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requerido para DML sobre tablas con indices filtrados

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. NivelesIncidencia (clave Numero) ---------------------------------------------------
    DECLARE @Niveles TABLE (
        Numero      INT           NOT NULL,
        Nombre      NVARCHAR(50)  NOT NULL,
        Descripcion NVARCHAR(250) NULL
    );

    INSERT INTO @Niveles (Numero, Nombre, Descripcion)
    VALUES
        (1, N'Nivel 1', N'Agrupa historias de usuario (p. ej. Épica).'),
        (2, N'Nivel 2', N'Trabajo funcional que puede incluir subtareas, casos de prueba y bugs (p. ej. Historia de Usuario).'),
        (3, N'Nivel 3', N'Tareas, pruebas y defectos que dependen de una historia de usuario.');

    INSERT INTO dbo.NivelesIncidencia (Numero, Nombre, Descripcion)
    SELECT s.Numero, s.Nombre, s.Descripcion
    FROM @Niveles s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.NivelesIncidencia n WHERE n.Numero = s.Numero)
    ORDER BY s.Numero;

    -- 2. EstadosIncidencia (clave Nombre) ---------------------------------------------------
    DECLARE @Estados TABLE (
        Nombre NVARCHAR(50) NOT NULL,
        Orden  INT          NOT NULL
    );

    INSERT INTO @Estados (Nombre, Orden)
    VALUES
        (N'Por hacer',   10),
        (N'En progreso', 20),
        (N'Hecho',       30);

    INSERT INTO dbo.EstadosIncidencia (Nombre, Descripcion, Orden, Activo)
    SELECT s.Nombre, NULL, s.Orden, 1
    FROM @Estados s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.EstadosIncidencia e WHERE e.Nombre = s.Nombre)
    ORDER BY s.Orden;

    -- 3. FlujosIncidencia (clave Nombre) y sus estados (clave FlujoId, EstadoId) ------------
    IF NOT EXISTS (SELECT 1 FROM dbo.FlujosIncidencia WHERE Nombre = N'Flujo general')
        INSERT INTO dbo.FlujosIncidencia (Nombre, Descripcion)
        VALUES (N'Flujo general', N'Flujo por defecto: Por hacer → En progreso → Hecho.');

    DECLARE @FlujoGeneralId INT = (SELECT Id FROM dbo.FlujosIncidencia WHERE Nombre = N'Flujo general');

    DECLARE @EstadosFlujo TABLE (
        Estado    NVARCHAR(50) NOT NULL,
        Orden     INT          NOT NULL,
        EsInicial BIT          NOT NULL,
        EsFinal   BIT          NOT NULL
    );

    INSERT INTO @EstadosFlujo (Estado, Orden, EsInicial, EsFinal)
    VALUES
        (N'Por hacer',   1, 1, 0),
        (N'En progreso', 2, 0, 0),
        (N'Hecho',       3, 0, 1);

    INSERT INTO dbo.FlujosIncidenciaEstados (FlujoId, EstadoId, Orden, EsInicial, EsFinal)
    SELECT @FlujoGeneralId,
           e.Id,
           s.Orden,
           CASE WHEN s.EsInicial = 1
                 AND NOT EXISTS (SELECT 1 FROM dbo.FlujosIncidenciaEstados fe
                                 WHERE fe.FlujoId = @FlujoGeneralId AND fe.EsInicial = 1)
                THEN 1 ELSE 0 END,
           s.EsFinal
    FROM @EstadosFlujo s
    JOIN dbo.EstadosIncidencia e ON e.Nombre = s.Estado
    WHERE NOT EXISTS (SELECT 1 FROM dbo.FlujosIncidenciaEstados fe
                      WHERE fe.FlujoId = @FlujoGeneralId AND fe.EstadoId = e.Id)
    ORDER BY s.Orden;

    -- 4. TiposIncidencia (clave Nombre; nivel por Numero; flujo "Flujo general") ------------
    DECLARE @Tipos TABLE (
        Nombre      NVARCHAR(50)  NOT NULL,
        NivelNumero INT           NOT NULL,
        Descripcion NVARCHAR(250) NULL,
        Orden       INT           NOT NULL
    );

    INSERT INTO @Tipos (Nombre, NivelNumero, Descripcion, Orden)
    VALUES
        (N'Épica',               1, N'Agrupa historias de usuario.',                    1),
        (N'Historia de Usuario', 2, N'Funcionalidad desde la perspectiva del usuario.', 2),
        (N'Subtarea',            3, N'Tarea de una historia de usuario.',               3),
        (N'Caso de Prueba',      3, N'Caso de prueba de una historia de usuario.',      4),
        (N'Bug',                 3, N'Defecto detectado.',                              5);

    INSERT INTO dbo.TiposIncidencia (Nombre, Descripcion, NivelId, FlujoId, Activo)
    SELECT s.Nombre, s.Descripcion, n.Id, @FlujoGeneralId, 1
    FROM @Tipos s
    JOIN dbo.NivelesIncidencia n ON n.Numero = s.NivelNumero
    WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposIncidencia t WHERE t.Nombre = s.Nombre)
    ORDER BY s.Orden;

    -- 5. RelacionesTiposIncidencia (clave par de nombres) -----------------------------------
    DECLARE @Relaciones TABLE (
        TipoPadre NVARCHAR(50) NOT NULL,
        TipoHijo  NVARCHAR(50) NOT NULL,
        Orden     INT          NOT NULL
    );

    INSERT INTO @Relaciones (TipoPadre, TipoHijo, Orden)
    VALUES
        (N'Épica',               N'Historia de Usuario', 1),
        (N'Historia de Usuario', N'Subtarea',            2),
        (N'Historia de Usuario', N'Caso de Prueba',      3),
        (N'Historia de Usuario', N'Bug',                 4);

    INSERT INTO dbo.RelacionesTiposIncidencia (TipoPadreId, TipoHijoId)
    SELECT tp.Id, th.Id
    FROM @Relaciones s
    JOIN dbo.TiposIncidencia tp ON tp.Nombre = s.TipoPadre
    JOIN dbo.TiposIncidencia th ON th.Nombre = s.TipoHijo
    WHERE NOT EXISTS (SELECT 1 FROM dbo.RelacionesTiposIncidencia r
                      WHERE r.TipoPadreId = tp.Id AND r.TipoHijoId = th.Id)
    ORDER BY s.Orden;

    -- 6. PrioridadesIncidencia (clave Nombre) -----------------------------------------------
    DECLARE @Prioridades TABLE (
        Nombre           NVARCHAR(50) NOT NULL,
        Orden            INT          NOT NULL,
        EsPredeterminada BIT          NOT NULL
    );

    INSERT INTO @Prioridades (Nombre, Orden, EsPredeterminada)
    VALUES
        (N'Baja',    10, 0),
        (N'Media',   20, 1),
        (N'Alta',    30, 0),
        (N'Crítica', 40, 0);

    INSERT INTO dbo.PrioridadesIncidencia (Nombre, Descripcion, Orden, EsPredeterminada, Activo)
    SELECT s.Nombre,
           NULL,
           s.Orden,
           CASE WHEN s.EsPredeterminada = 1
                 AND NOT EXISTS (SELECT 1 FROM dbo.PrioridadesIncidencia p WHERE p.EsPredeterminada = 1)
                THEN 1 ELSE 0 END,
           1
    FROM @Prioridades s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PrioridadesIncidencia p WHERE p.Nombre = s.Nombre)
    ORDER BY s.Orden;

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'S003')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'S003', N'seed_configuracion_incidencias_inicial');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
