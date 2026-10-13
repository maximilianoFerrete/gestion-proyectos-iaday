-- Script: D900__borra_datos_prueba.sql
-- Spec:   N/A (datos de prueba para desarrollo local)
-- Fecha:  2026-10-13
-- Desc:   Elimina SOLO los datos cargados por D001:
--         - Proyectos cuyo nombre empieza con "[PRUEBA] " y todo lo que depende de ellos:
--           incidencias (primero se anula PadreId para no violar FK_Incidencias_Incidencias_Padre),
--           ConfiguracionesTableroProyecto y Agrupaciones.
--         - Personas con email "@prueba.local" que ya no esten referenciadas por ningun
--           proyecto o incidencia (si alguien las asigno a datos reales, se conservan y se avisa).
--         No toca proyectos, incidencias ni personas reales. No reinicia SQ_Proyectos_Numero
--         (el backend nunca reutiliza codigos; la proxima carga usa codigos nuevos).
--         Idempotente: sin datos de prueba no hace nada.
-- Nota:   archivo UTF-8; ejecutar sqlcmd con -b -I -f 65001.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requerido para DML sobre tablas con indices filtrados

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Prefijo NVARCHAR(20) = N'[PRUEBA] ';
    DECLARE @DominioPrueba NVARCHAR(50) = N'@prueba.local';

    DECLARE @Proyectos TABLE (Id INT NOT NULL PRIMARY KEY);
    INSERT INTO @Proyectos (Id)
    SELECT Id FROM dbo.Proyectos WHERE LEFT(Nombre, LEN(@Prefijo)) = @Prefijo;

    DECLARE @CantProy INT = (SELECT COUNT(*) FROM @Proyectos);

    -- 1. Incidencias: anular padres y borrar (la FK de padre es del mismo proyecto).
    UPDATE i SET PadreId = NULL
    FROM dbo.Incidencias i
    JOIN @Proyectos p ON p.Id = i.ProyectoId
    WHERE i.PadreId IS NOT NULL;

    DELETE i
    FROM dbo.Incidencias i
    JOIN @Proyectos p ON p.Id = i.ProyectoId;
    DECLARE @CantInc INT = @@ROWCOUNT;

    -- 2. Configuracion de tablero y agrupaciones.
    DELETE c
    FROM dbo.ConfiguracionesTableroProyecto c
    JOIN @Proyectos p ON p.Id = c.ProyectoId;

    DELETE a
    FROM dbo.Agrupaciones a
    JOIN @Proyectos p ON p.Id = a.ProyectoId;
    DECLARE @CantAgr INT = @@ROWCOUNT;

    -- 3. Proyectos.
    DELETE pr
    FROM dbo.Proyectos pr
    JOIN @Proyectos p ON p.Id = pr.Id;

    -- 4. Personas de prueba no referenciadas.
    DELETE pe
    FROM dbo.Personas pe
    WHERE pe.Email LIKE N'%' + @DominioPrueba
      AND NOT EXISTS (SELECT 1 FROM dbo.Proyectos pr WHERE pr.ResponsableId = pe.Id OR pr.ArchivadoPorId = pe.Id)
      AND NOT EXISTS (SELECT 1 FROM dbo.Incidencias i WHERE i.ResponsableId = pe.Id);
    DECLARE @CantPer INT = @@ROWCOUNT;

    IF EXISTS (SELECT 1 FROM dbo.Personas WHERE Email LIKE N'%' + @DominioPrueba)
        PRINT N'Aviso: quedan personas @prueba.local referenciadas por datos que no son de prueba; no se borraron.';

    PRINT CONCAT(N'Datos de prueba eliminados: ', @CantProy, N' proyectos, ', @CantInc, N' incidencias, ',
                 @CantAgr, N' agrupaciones, ', @CantPer, N' personas.');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
