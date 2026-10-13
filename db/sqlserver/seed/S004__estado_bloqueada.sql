-- Script: S004__estado_bloqueada.sql
-- Spec:   RF-005 (DA-02)
-- Fecha:  2026-10-10
-- Desc:   Semilla del estado de incidencia "Bloqueada" (categoria BLOQUEADA, Orden 24, activo)
--         y su inclusion en el "Flujo general" como estado ni inicial ni final, con
--         ubicado inmediatamente antes del estado final: toma el Orden del (primer) estado
--         final y este y los posteriores corren +1. Excepcion aprobada por el usuario a "no
--         modifica filas existentes", limitada a FlujosIncidenciaEstados.Orden del Flujo general.
--         Si Bloqueada ya esta en el flujo con Orden mayor que el final, se reubica antes del
--         final; si ya esta antes del final (orden manual del administrador), no se toca.
--         Sin estado final en el flujo: se agrega con Orden = maximo + 1.
--         Idempotente: inserta solo lo que no existe por la clave indicada en la spec
--         (estado por Nombre; estado del flujo por (FlujoId, EstadoId)). No modifica filas
--         existentes (salvo el Orden indicado arriba): si ya existe un estado "Bloqueada" se respeta tal cual (con su categoria).
--         Si el "Flujo general" no existe, no se agrega a ningun flujo.
--         NO crea el estado "En espera" ni asigna EN_ESPERA a ningun estado.
--         Requiere V010 (columna Categoria) y S003.
-- Nota:   archivo UTF-8; ejecutar sqlcmd con -f 65001.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requerido para DML sobre tablas con indices filtrados

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. EstadosIncidencia (clave Nombre) ---------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadosIncidencia WHERE Nombre = N'Bloqueada')
        INSERT INTO dbo.EstadosIncidencia (Nombre, Descripcion, Orden, Activo, Categoria)
        VALUES (N'Bloqueada', N'La incidencia no puede avanzar por un impedimento.', 24, 1, N'BLOQUEADA');

    -- 2. FlujosIncidenciaEstados del "Flujo general" (clave FlujoId, EstadoId) ---------------
    DECLARE @FlujoGeneralId INT = (SELECT Id FROM dbo.FlujosIncidencia WHERE Nombre = N'Flujo general');
    DECLARE @BloqueadaId    INT = (SELECT Id FROM dbo.EstadosIncidencia WHERE Nombre = N'Bloqueada');

    -- Orden del (primer) estado final del flujo; NULL si el flujo no tiene final.
    DECLARE @OrdenFinal INT = (SELECT MIN(Orden) FROM dbo.FlujosIncidenciaEstados
                               WHERE FlujoId = @FlujoGeneralId AND EsFinal = 1);
    DECLARE @OrdenBloqueada INT = (SELECT Orden FROM dbo.FlujosIncidenciaEstados
                                   WHERE FlujoId = @FlujoGeneralId AND EstadoId = @BloqueadaId);

    IF @FlujoGeneralId IS NOT NULL AND @BloqueadaId IS NOT NULL
    BEGIN
        IF @OrdenBloqueada IS NULL
        BEGIN
            IF @OrdenFinal IS NOT NULL
            BEGIN
                -- 2a. Se inserta en el lugar del estado final; final(es) y posteriores corren +1.
                UPDATE dbo.FlujosIncidenciaEstados
                   SET Orden = Orden + 1
                 WHERE FlujoId = @FlujoGeneralId AND Orden >= @OrdenFinal;

                INSERT INTO dbo.FlujosIncidenciaEstados (FlujoId, EstadoId, Orden, EsInicial, EsFinal)
                VALUES (@FlujoGeneralId, @BloqueadaId, @OrdenFinal, 0, 0);
            END
            ELSE
                -- 2b. Flujo sin estado final: se agrega al final.
                INSERT INTO dbo.FlujosIncidenciaEstados (FlujoId, EstadoId, Orden, EsInicial, EsFinal)
                SELECT @FlujoGeneralId, @BloqueadaId, ISNULL(MAX(fe.Orden), 0) + 1, 0, 0
                FROM dbo.FlujosIncidenciaEstados fe
                WHERE fe.FlujoId = @FlujoGeneralId;
        END
        ELSE IF @OrdenFinal IS NOT NULL AND @OrdenBloqueada > @OrdenFinal
        BEGIN
            -- 2c. Correccion: Bloqueada quedo despues del final (version previa de S004).
            --     Se corren +1 las filas entre el final y Bloqueada, y Bloqueada toma el
            --     Orden del final. Si ya esta antes del final, no se toca.
            UPDATE dbo.FlujosIncidenciaEstados
               SET Orden = Orden + 1
             WHERE FlujoId = @FlujoGeneralId
               AND Orden >= @OrdenFinal AND Orden < @OrdenBloqueada
               AND EstadoId <> @BloqueadaId;

            UPDATE dbo.FlujosIncidenciaEstados
               SET Orden = @OrdenFinal
             WHERE FlujoId = @FlujoGeneralId AND EstadoId = @BloqueadaId;
        END
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'S004')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'S004', N'seed_estado_bloqueada');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
