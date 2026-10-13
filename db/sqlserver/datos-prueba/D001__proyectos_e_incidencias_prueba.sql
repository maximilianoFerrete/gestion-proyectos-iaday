-- Script: D001__proyectos_e_incidencias_prueba.sql
-- Spec:   N/A (datos de prueba para desarrollo local; NO es parte del esquema ni de la semilla)
-- Fecha:  2026-10-13
-- Desc:   Carga datos de prueba para probar la app a mano en LocalDB:
--         - 9 personas de prueba (email @prueba.local), activas, no administradoras.
--         - 12 proyectos con prefijo "[PRUEBA] " en el nombre (codigo PRJ-#### desde
--           SQ_Proyectos_Numero, igual que el backend), estados configurables de proyecto en
--           rotacion y uno archivado (si existe una persona administradora real).
--         - 2 a 5 agrupaciones por proyecto.
--         - Entre 100 y 990 incidencias por proyecto (5590 en total), con jerarquia valida
--           segun RelacionesTiposIncidencia, numeracion por proyecto, codigo <proyecto>-<n> y
--           Proyectos.UltimoNumeroIncidencia actualizado.
--         La configuracion (estados de proyecto, niveles, tipos, relaciones, flujos, estados
--         de incidencia, prioridades) se lee de la base; no hay ids fijos.
--         Reglas del backend que se respetan (no cubiertas por constraints):
--         - Solo tipos, prioridades, estados de incidencia y responsables activos.
--         - Estado de la incidencia dentro del flujo de su tipo.
--         - Padre del mismo proyecto con relacion tipo padre -> tipo hija permitida; la hija
--           hereda la agrupacion del padre; vencimiento de la hija <= vencimiento del padre.
--         - Numero correlativo por proyecto en orden de FechaCreacion (el padre siempre se
--           creo antes que la hija); FechaCreacion/FechaModificacion en UTC sin fraccion.
--         - FechaModificacion NULL salvo que la fila haya sido "editada" (estado distinto del
--           inicial o edicion simulada), siempre >= FechaCreacion.
--         Idempotente: si ya hay proyectos "[PRUEBA] " no inserta nada nuevo (las personas se
--         insertan solo si faltan). Para recrear: ejecutar D900 y despues este script.
--         No registra version en dbo.VersionesEsquema (no es un cambio de esquema).
-- Nota:   archivo UTF-8; ejecutar sqlcmd con -b -I -f 65001.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requerido para DML sobre tablas con indices filtrados

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Prefijo NVARCHAR(20) = N'[PRUEBA] ';
    DECLARE @DominioPrueba NVARCHAR(50) = N'@prueba.local';
    -- Igual que TimeProviderExtensions.AhoraUtc: UTC sin fraccion de segundo.
    DECLARE @Ahora DATETIME2(0) = DATEADD(SECOND, -1, CAST(SYSUTCDATETIME() AS DATETIME2(0)));
    DECLARE @Hoy DATE = CAST(SYSDATETIME() AS DATE);

    -------------------------------------------------------------------------------------------
    -- 1. Personas de prueba (idempotente por Email)
    -------------------------------------------------------------------------------------------
    DECLARE @PersonasSemilla TABLE (Idx INT NOT NULL, Nombre NVARCHAR(150) NOT NULL, Email NVARCHAR(254) NOT NULL);
    INSERT INTO @PersonasSemilla (Idx, Nombre, Email)
    VALUES
        (1, N'Lucía Fernández',   N'lucia.fernandez@prueba.local'),
        (2, N'Martín Gómez',      N'martin.gomez@prueba.local'),
        (3, N'Sofía Rodríguez',   N'sofia.rodriguez@prueba.local'),
        (4, N'Juan Pablo Díaz',   N'juanpablo.diaz@prueba.local'),
        (5, N'Valentina López',   N'valentina.lopez@prueba.local'),
        (6, N'Diego Martínez',    N'diego.martinez@prueba.local'),
        (7, N'Camila Sánchez',    N'camila.sanchez@prueba.local'),
        (8, N'Federico Romero',   N'federico.romero@prueba.local'),
        (9, N'Agustina Pereyra',  N'agustina.pereyra@prueba.local');

    INSERT INTO dbo.Personas (Nombre, Email, EsAdmin, Activo, FechaCreacion, FechaModificacion)
    SELECT s.Nombre, s.Email, 0, 1, DATEADD(DAY, -500, @Ahora), NULL
    FROM @PersonasSemilla s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Personas p WHERE p.Email = s.Email)
    ORDER BY s.Idx;

    IF EXISTS (SELECT 1 FROM dbo.Proyectos WHERE LEFT(Nombre, LEN(@Prefijo)) = @Prefijo)
    BEGIN
        PRINT N'Ya existen proyectos de prueba ("[PRUEBA] "): no se insertan datos nuevos. '
            + N'Para recrearlos, ejecutar D900__borra_datos_prueba.sql y volver a ejecutar este script.';
        COMMIT TRANSACTION;
        RETURN;
    END;

    -------------------------------------------------------------------------------------------
    -- 2. Configuracion leida de la base
    -------------------------------------------------------------------------------------------
    -- Personas de prueba activas (responsables).
    CREATE TABLE #Personas (Idx INT NOT NULL PRIMARY KEY, PersonaId INT NOT NULL, Cant INT NOT NULL);
    INSERT INTO #Personas (Idx, PersonaId, Cant)
    SELECT ROW_NUMBER() OVER (ORDER BY p.Id), p.Id, COUNT(*) OVER ()
    FROM dbo.Personas p
    WHERE p.Email LIKE N'%' + @DominioPrueba AND p.Activo = 1;

    -- Estados de proyecto asignables (activos, no de sistema), en orden.
    CREATE TABLE #EstadosProy (Idx INT NOT NULL PRIMARY KEY, EstadoId INT NOT NULL, Nombre NVARCHAR(50) NOT NULL,
                               EsInicial BIT NOT NULL, EsFinal BIT NOT NULL, Cant INT NOT NULL);
    INSERT INTO #EstadosProy (Idx, EstadoId, Nombre, EsInicial, EsFinal, Cant)
    SELECT ROW_NUMBER() OVER (ORDER BY e.Orden, e.Id), e.Id, e.Nombre, e.EsInicial, e.EsFinal, COUNT(*) OVER ()
    FROM dbo.EstadosProyecto e
    WHERE e.Activo = 1 AND e.Codigo IS NULL;

    DECLARE @EstadoProyInicialId INT = (SELECT Id FROM dbo.EstadosProyecto WHERE EsInicial = 1);
    DECLARE @EstadoArchivadoId INT = (SELECT Id FROM dbo.EstadosProyecto WHERE Codigo = N'ARCHIVADO');
    DECLARE @AdminId INT = (SELECT TOP (1) Id FROM dbo.Personas
                            WHERE EsAdmin = 1 AND Activo = 1 AND Email NOT LIKE N'%' + @DominioPrueba
                            ORDER BY Id);

    -- Tipos activos con su nivel (NivelIdx = posicion del nivel entre los niveles con tipos activos).
    CREATE TABLE #Tipos (TipoId INT NOT NULL PRIMARY KEY, Nombre NVARCHAR(50) NOT NULL, FlujoId INT NOT NULL,
                         NivelIdx INT NOT NULL, IdxEnNivel INT NOT NULL, CantEnNivel INT NOT NULL);
    INSERT INTO #Tipos (TipoId, Nombre, FlujoId, NivelIdx, IdxEnNivel, CantEnNivel)
    SELECT t.Id, t.Nombre, t.FlujoId,
           DENSE_RANK() OVER (ORDER BY n.Numero),
           ROW_NUMBER() OVER (PARTITION BY n.Numero ORDER BY t.Id),
           COUNT(*) OVER (PARTITION BY n.Numero)
    FROM dbo.TiposIncidencia t
    JOIN dbo.NivelesIncidencia n ON n.Id = t.NivelId
    WHERE t.Activo = 1;

    -- Relaciones entre tipos activos.
    CREATE TABLE #Relaciones (TipoPadreId INT NOT NULL, TipoHijoId INT NOT NULL, PRIMARY KEY (TipoPadreId, TipoHijoId));
    INSERT INTO #Relaciones (TipoPadreId, TipoHijoId)
    SELECT r.TipoPadreId, r.TipoHijoId
    FROM dbo.RelacionesTiposIncidencia r
    JOIN #Tipos tp ON tp.TipoId = r.TipoPadreId
    JOIN #Tipos th ON th.TipoId = r.TipoHijoId;

    -- Estados activos de cada flujo usado, mas el inicial y el primer final de cada flujo.
    CREATE TABLE #EstadosFlujo (FlujoId INT NOT NULL, Idx INT NOT NULL, EstadoId INT NOT NULL, Cant INT NOT NULL,
                                PRIMARY KEY (FlujoId, Idx));
    INSERT INTO #EstadosFlujo (FlujoId, Idx, EstadoId, Cant)
    SELECT fe.FlujoId, ROW_NUMBER() OVER (PARTITION BY fe.FlujoId ORDER BY fe.Orden, fe.EstadoId), fe.EstadoId,
           COUNT(*) OVER (PARTITION BY fe.FlujoId)
    FROM dbo.FlujosIncidenciaEstados fe
    JOIN dbo.EstadosIncidencia e ON e.Id = fe.EstadoId
    WHERE e.Activo = 1
      AND fe.FlujoId IN (SELECT FlujoId FROM #Tipos);

    CREATE TABLE #Flujos (FlujoId INT NOT NULL PRIMARY KEY, EstadoInicialId INT NULL, EstadoFinalId INT NULL);
    INSERT INTO #Flujos (FlujoId, EstadoInicialId, EstadoFinalId)
    SELECT f.FlujoId,
           (SELECT fe.EstadoId FROM dbo.FlujosIncidenciaEstados fe WHERE fe.FlujoId = f.FlujoId AND fe.EsInicial = 1),
           (SELECT TOP (1) fe.EstadoId FROM dbo.FlujosIncidenciaEstados fe
            JOIN dbo.EstadosIncidencia e ON e.Id = fe.EstadoId
            WHERE fe.FlujoId = f.FlujoId AND fe.EsFinal = 1 AND e.Activo = 1
            ORDER BY fe.Orden, fe.EstadoId)
    FROM (SELECT DISTINCT FlujoId FROM #Tipos) f;

    -- Prioridades activas y predeterminada.
    CREATE TABLE #Prioridades (Idx INT NOT NULL PRIMARY KEY, PrioridadId INT NOT NULL, Cant INT NOT NULL);
    INSERT INTO #Prioridades (Idx, PrioridadId, Cant)
    SELECT ROW_NUMBER() OVER (ORDER BY p.Orden, p.Id), p.Id, COUNT(*) OVER ()
    FROM dbo.PrioridadesIncidencia p
    WHERE p.Activo = 1;
    DECLARE @PrioridadPredId INT = (SELECT Id FROM dbo.PrioridadesIncidencia WHERE EsPredeterminada = 1);

    -- Validaciones de prerequisitos (seeds S001-S004 aplicados).
    IF NOT EXISTS (SELECT 1 FROM #Personas)
        THROW 50001, N'No hay personas de prueba activas.', 1;
    IF NOT EXISTS (SELECT 1 FROM #EstadosProy) OR @EstadoProyInicialId IS NULL
        THROW 50002, N'Faltan estados de proyecto (aplicar S001).', 1;
    IF NOT EXISTS (SELECT 1 FROM #Tipos)
        THROW 50003, N'No hay tipos de incidencia activos (aplicar S003).', 1;
    IF EXISTS (SELECT 1 FROM #Flujos WHERE EstadoInicialId IS NULL)
       OR EXISTS (SELECT 1 FROM #Flujos f WHERE NOT EXISTS (SELECT 1 FROM #EstadosFlujo ef WHERE ef.FlujoId = f.FlujoId))
        THROW 50004, N'Hay flujos de tipos activos sin estado inicial o sin estados activos.', 1;
    IF @PrioridadPredId IS NULL OR NOT EXISTS (SELECT 1 FROM #Prioridades)
        THROW 50005, N'Falta la prioridad predeterminada o prioridades activas (aplicar S003).', 1;

    -- Tabla de numeros 1..10000.
    CREATE TABLE #Num (n INT NOT NULL PRIMARY KEY);
    WITH d AS (SELECT v FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) x(v))
    INSERT INTO #Num (n)
    SELECT a.v * 1000 + b.v * 100 + c.v * 10 + e.v + 1
    FROM d a CROSS JOIN d b CROSS JOIN d c CROSS JOIN d e;

    -------------------------------------------------------------------------------------------
    -- 3. Proyectos
    -------------------------------------------------------------------------------------------
    CREATE TABLE #Proy (
        Idx                INT            NOT NULL PRIMARY KEY,
        Nombre             NVARCHAR(150)  NOT NULL,
        Descripcion        NVARCHAR(2000) NOT NULL,
        Cantidad           INT            NOT NULL,
        ResponsableId      INT            NULL,
        EstadoBaseId       INT            NULL,  -- estado configurable (antes de archivar)
        Categoria          CHAR(1)        NULL,  -- I = inicial/planificado, F = final, C = en curso
        FechaInicio        DATE           NULL,
        FechaObjetivo      DATE           NULL,
        FechaCreacion      DATETIME2(0)   NULL,
        FechaModificacion  DATETIME2(0)   NULL,
        EstadoId           INT            NULL,
        FechaArchivado     DATETIME2(0)   NULL,
        ArchivadoPorId     INT            NULL,
        EstadoAlArchivarId INT            NULL,
        IniCreacion        DATETIME2(0)   NULL,  -- rango de FechaCreacion de sus incidencias
        FinCreacion        DATETIME2(0)   NULL,
        FinModificacion    DATETIME2(0)   NULL,
        ProyectoId         INT            NULL,
        Codigo             NVARCHAR(20)   NULL
    );

    INSERT INTO #Proy (Idx, Nombre, Descripcion, Cantidad)
    VALUES
        (1,  N'Portal de autogestión de clientes',
             N'Portal web para que los clientes consulten facturas, descarguen comprobantes, registren reclamos y actualicen sus datos sin intervención del área comercial.', 420),
        (2,  N'Migración del ERP a la nube',
             N'Migración del ERP on-premise a infraestructura en la nube, incluyendo base de datos, integraciones existentes, backups y plan de contingencia.', 185),
        (3,  N'App móvil de ventas en campo',
             N'Aplicación móvil para vendedores en la calle: toma de pedidos sin conexión, consulta de stock y precios, y sincronización con el sistema central.', 990),
        (4,  N'Sistema de turnos para sucursales',
             N'Sistema de reserva de turnos online y por tótem para la atención en sucursales, con recordatorios por correo y tablero de espera.', 290),
        (5,  N'Tablero de indicadores comerciales',
             N'Tablero con indicadores de ventas, cobranzas y rentabilidad por zona y vendedor, con actualización diaria desde el ERP.', 140),
        (6,  N'Integración con AFIP - factura electrónica',
             N'Integración con los web services de AFIP para emitir facturas, notas de crédito y débito electrónicas, y gestionar el CAE.', 530),
        (7,  N'Rediseño del sitio institucional',
             N'Nuevo diseño del sitio institucional, adaptable a dispositivos móviles, con gestor de contenidos y formulario de contacto.', 100),
        (8,  N'Plataforma de e-commerce B2B',
             N'Tienda online para clientes mayoristas con listas de precios por cliente, cuenta corriente, pedidos recurrentes y seguimiento de entregas.', 760),
        (9,  N'Gestión de depósitos y logística',
             N'Sistema de gestión de depósitos: recepción, ubicaciones, picking, armado de remitos y planificación de rutas de entrega.', 945),
        (10, N'Mesa de ayuda interna',
             N'Herramienta de mesa de ayuda para soporte interno de sistemas: alta de tickets, asignación, SLA y base de conocimiento.', 360),
        (11, N'Módulo de liquidación de sueldos',
             N'Módulo de liquidación de haberes con conceptos configurables, recibos digitales y exportación de archivos para bancos y organismos.', 230),
        (12, N'Programa de fidelización de clientes',
             N'Programa de puntos y beneficios para clientes frecuentes, con catálogo de canje, campañas y segmentación.', 640);

    UPDATE p
    SET Nombre        = @Prefijo + p.Nombre,
        ResponsableId = pe.PersonaId,
        EstadoBaseId  = ep.EstadoId,
        Categoria     = CASE WHEN ep.EsInicial = 1 OR ep.Nombre = N'Planificado' THEN 'I'
                             WHEN ep.EsFinal = 1 THEN 'F'
                             ELSE 'C' END
    FROM #Proy p
    JOIN #Personas pe ON pe.Idx = ((p.Idx - 1) % pe.Cant) + 1
    JOIN #EstadosProy ep ON ep.Idx = ((p.Idx - 1) % ep.Cant) + 1;

    -- Fechas coherentes con el estado (CK_Proyectos_Fechas: FechaObjetivo >= FechaInicio).
    UPDATE #Proy
    SET FechaInicio   = CASE Categoria
                            WHEN 'I' THEN DATEADD(DAY, 10 + Idx * 3, @Hoy)
                            WHEN 'F' THEN DATEADD(DAY, -(330 + Idx * 7), @Hoy)
                            ELSE          DATEADD(DAY, -(90 + Idx * 11), @Hoy) END,
        FechaObjetivo = CASE Categoria
                            WHEN 'I' THEN DATEADD(DAY, 160 + Idx * 8, @Hoy)
                            WHEN 'F' THEN DATEADD(DAY, -(20 + Idx * 4), @Hoy)
                            ELSE          DATEADD(DAY, 40 + Idx * 9, @Hoy) END;

    UPDATE #Proy
    SET FechaCreacion = CASE Categoria
                            WHEN 'I' THEN DATEADD(DAY, -(40 + Idx), @Ahora)
                            ELSE DATEADD(HOUR, 12 + Idx % 6, CAST(DATEADD(DAY, -10, FechaInicio) AS DATETIME2(0))) END;

    -- Un proyecto archivado (flujo real: lo archiva una persona administradora).
    UPDATE #Proy
    SET EstadoAlArchivarId = EstadoBaseId,
        EstadoId           = @EstadoArchivadoId,
        FechaArchivado     = DATEADD(DAY, -3, @Ahora),
        ArchivadoPorId     = @AdminId
    WHERE Idx = 11 AND @AdminId IS NOT NULL AND @EstadoArchivadoId IS NOT NULL;

    UPDATE #Proy
    SET EstadoId          = COALESCE(EstadoId, EstadoBaseId),
        -- Estado distinto del inicial o archivado => el proyecto fue editado.
        FechaModificacion = CASE WHEN FechaArchivado IS NOT NULL THEN FechaArchivado
                                 WHEN EstadoBaseId <> @EstadoProyInicialId THEN DATEADD(DAY, 5, FechaCreacion)
                                 ELSE NULL END,
        IniCreacion       = DATEADD(DAY, 2, FechaCreacion),
        FinModificacion   = COALESCE(FechaArchivado, @Ahora);

    UPDATE #Proy
    SET FinCreacion = CASE WHEN FechaArchivado IS NOT NULL THEN DATEADD(DAY, -1, FechaArchivado)
                           WHEN Categoria = 'F'
                                AND DATEADD(DAY, 10, CAST(FechaObjetivo AS DATETIME2(0))) < DATEADD(DAY, -1, @Ahora)
                               THEN DATEADD(DAY, 10, CAST(FechaObjetivo AS DATETIME2(0)))
                           ELSE DATEADD(HOUR, -2, @Ahora) END;

    -- Alta con la secuencia, igual que ProyectoRepository.ObtenerSiguienteNumeroAsync
    -- y FormateadorCodigoProyecto ("PRJ-" + numero con al menos 4 digitos).
    DECLARE @k INT = 1, @MaxK INT = (SELECT MAX(Idx) FROM #Proy), @Numero INT, @Codigo NVARCHAR(20);
    WHILE @k <= @MaxK
    BEGIN
        SET @Numero = NEXT VALUE FOR dbo.SQ_Proyectos_Numero;
        SET @Codigo = N'PRJ-' + CASE WHEN @Numero < 10000
                                     THEN RIGHT(N'000' + CAST(@Numero AS NVARCHAR(10)), 4)
                                     ELSE CAST(@Numero AS NVARCHAR(10)) END;

        INSERT INTO dbo.Proyectos
            (Codigo, Nombre, Descripcion, ResponsableId, FechaInicio, FechaObjetivo, EstadoId,
             FechaArchivado, ArchivadoPorId, EstadoAlArchivarId, FechaCreacion, FechaModificacion,
             UltimoNumeroIncidencia)
        SELECT @Codigo, Nombre, Descripcion, ResponsableId, FechaInicio, FechaObjetivo, EstadoId,
               FechaArchivado, ArchivadoPorId, EstadoAlArchivarId, FechaCreacion, FechaModificacion, 0
        FROM #Proy
        WHERE Idx = @k;

        UPDATE #Proy SET ProyectoId = CAST(SCOPE_IDENTITY() AS INT), Codigo = @Codigo WHERE Idx = @k;
        SET @k += 1;
    END;

    -------------------------------------------------------------------------------------------
    -- 4. Agrupaciones (2 a 5 por proyecto)
    -------------------------------------------------------------------------------------------
    DECLARE @AgrupacionesSemilla TABLE (Idx INT NOT NULL, Nombre NVARCHAR(100) NOT NULL, Descripcion NVARCHAR(1000) NOT NULL);
    INSERT INTO @AgrupacionesSemilla (Idx, Nombre, Descripcion)
    VALUES
        (1, N'Sprint 1',        N'Primer sprint: funcionalidades base y configuración inicial.'),
        (2, N'Sprint 2',        N'Segundo sprint: flujos principales y validaciones.'),
        (3, N'Release 1.0',     N'Alcance comprometido para la primera versión productiva.'),
        (4, N'Backlog técnico', N'Deuda técnica, mejoras de rendimiento y tareas de infraestructura.'),
        (5, N'Mejoras post-lanzamiento', N'Ajustes y mejoras surgidas del uso en producción.');

    INSERT INTO dbo.Agrupaciones (ProyectoId, Nombre, Descripcion, FechaCreacion, FechaModificacion)
    SELECT p.ProyectoId, s.Nombre, s.Descripcion, DATEADD(DAY, 1, p.FechaCreacion), NULL
    FROM #Proy p
    JOIN @AgrupacionesSemilla s ON s.Idx <= 2 + (p.Idx % 4)
    ORDER BY p.Idx, s.Idx;

    CREATE TABLE #Agr (ProyectoId INT NOT NULL, Idx INT NOT NULL, AgrupacionId INT NOT NULL, Cant INT NOT NULL,
                       PRIMARY KEY (ProyectoId, Idx));
    INSERT INTO #Agr (ProyectoId, Idx, AgrupacionId, Cant)
    SELECT a.ProyectoId, ROW_NUMBER() OVER (PARTITION BY a.ProyectoId ORDER BY a.Id), a.Id,
           COUNT(*) OVER (PARTITION BY a.ProyectoId)
    FROM dbo.Agrupaciones a
    JOIN #Proy p ON p.ProyectoId = a.ProyectoId;

    -------------------------------------------------------------------------------------------
    -- 5. Arbol de incidencias (en memoria), nivel por nivel
    -------------------------------------------------------------------------------------------
    -- Cuota por nivel: peso i^3 (p. ej. 3 niveles: ~3% / ~22% / ~75%), minimo un nodo por tipo
    -- del nivel; el ultimo nivel recibe el resto.
    DECLARE @MaxNivel INT = (SELECT MAX(NivelIdx) FROM #Tipos);
    DECLARE @SumaPesos FLOAT = (SELECT SUM(POWER(CAST(NivelIdx AS FLOAT), 3))
                                FROM (SELECT DISTINCT NivelIdx FROM #Tipos) x);

    CREATE TABLE #Cuotas (ProyectoIdx INT NOT NULL, NivelIdx INT NOT NULL, Cantidad INT NOT NULL,
                          PRIMARY KEY (ProyectoIdx, NivelIdx));
    INSERT INTO #Cuotas (ProyectoIdx, NivelIdx, Cantidad)
    SELECT p.Idx, n.NivelIdx,
           CASE WHEN FLOOR(p.Cantidad * POWER(CAST(n.NivelIdx AS FLOAT), 3) / @SumaPesos) < n.CantTipos
                THEN n.CantTipos
                ELSE CAST(FLOOR(p.Cantidad * POWER(CAST(n.NivelIdx AS FLOAT), 3) / @SumaPesos) AS INT) END
    FROM #Proy p
    CROSS JOIN (SELECT NivelIdx, COUNT(*) AS CantTipos FROM #Tipos GROUP BY NivelIdx) n
    WHERE n.NivelIdx < @MaxNivel;

    INSERT INTO #Cuotas (ProyectoIdx, NivelIdx, Cantidad)
    SELECT p.Idx, @MaxNivel, p.Cantidad - COALESCE((SELECT SUM(c.Cantidad) FROM #Cuotas c WHERE c.ProyectoIdx = p.Idx), 0)
    FROM #Proy p;

    CREATE TABLE #Nodos (
        Clave            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProyectoIdx      INT          NOT NULL,
        ProyectoId       INT          NOT NULL,
        NivelIdx         INT          NOT NULL,
        Ord              INT          NOT NULL,
        TipoId           INT          NOT NULL,
        Semilla          VARBINARY(32) NULL,
        PadreClave       INT          NULL,
        AgrupacionId     INT          NULL,
        FechaCreacion    DATETIME2(0) NULL,
        FechaVencimiento DATE         NULL,
        Numero           INT          NULL,
        EstadoId         INT          NULL,
        PrioridadId      INT          NULL,
        ResponsableId    INT          NULL,
        Estimacion       DECIMAL(9,2) NULL,
        FechaModificacion DATETIME2(0) NULL,
        Titulo           NVARCHAR(200) NULL,
        Descripcion      NVARCHAR(4000) NULL,
        IncidenciaId     INT          NULL,
        -- Valores pseudoaleatorios deterministas derivados de la semilla (SHA2_256).
        RB  AS CAST(SUBSTRING(Semilla, 1, 4) AS BIGINT),
        RB2 AS CAST(SUBSTRING(Semilla, 5, 4) AS BIGINT),
        R1  AS CAST(SUBSTRING(Semilla, 9, 2) AS INT),
        R2  AS CAST(SUBSTRING(Semilla, 11, 2) AS INT),
        R3  AS CAST(SUBSTRING(Semilla, 13, 2) AS INT),
        R4  AS CAST(SUBSTRING(Semilla, 15, 2) AS INT),
        R5  AS CAST(SUBSTRING(Semilla, 17, 2) AS INT),
        R6  AS CAST(SUBSTRING(Semilla, 19, 2) AS INT),
        R7  AS CAST(SUBSTRING(Semilla, 21, 2) AS INT),
        R8  AS CAST(SUBSTRING(Semilla, 23, 2) AS INT),
        R9  AS CAST(SUBSTRING(Semilla, 25, 2) AS INT),
        R10 AS CAST(SUBSTRING(Semilla, 27, 2) AS INT),
        R11 AS CAST(SUBSTRING(Semilla, 29, 2) AS INT),
        R12 AS CAST(SUBSTRING(Semilla, 31, 2) AS INT)
    );

    DECLARE @i INT = 1;
    WHILE @i <= @MaxNivel
    BEGIN
        -- Nodos del nivel; los tipos del nivel se reparten en rotacion.
        INSERT INTO #Nodos (ProyectoIdx, ProyectoId, NivelIdx, Ord, TipoId)
        SELECT p.Idx, p.ProyectoId, @i, n.n, t.TipoId
        FROM #Cuotas c
        JOIN #Proy p ON p.Idx = c.ProyectoIdx
        JOIN #Num n ON n.n <= c.Cantidad
        JOIN #Tipos t ON t.NivelIdx = @i AND t.IdxEnNivel = ((n.n - 1) % t.CantEnNivel) + 1
        WHERE c.NivelIdx = @i
        ORDER BY p.Idx, n.n;

        UPDATE #Nodos
        SET Semilla = HASHBYTES('SHA2_256', CONCAT(N'D001|', ProyectoIdx, N'|', NivelIdx, N'|', Ord))
        WHERE NivelIdx = @i;

        -- Padre (~85% de los nodos de niveles > 1): un nodo de un nivel anterior del mismo
        -- proyecto cuyo tipo admita al tipo del nodo como hijo. Sin candidato => sin padre.
        IF @i > 1
        BEGIN
            WITH Candidatos AS (
                SELECT n.Clave, p.Clave AS PadreClave,
                       ROW_NUMBER() OVER (PARTITION BY n.Clave ORDER BY p.Clave) AS Rn,
                       COUNT(*) OVER (PARTITION BY n.Clave) AS Cnt
                FROM #Nodos n
                JOIN #Nodos p ON p.ProyectoId = n.ProyectoId AND p.NivelIdx < @i
                JOIN #Relaciones r ON r.TipoPadreId = p.TipoId AND r.TipoHijoId = n.TipoId
                WHERE n.NivelIdx = @i AND n.R1 % 100 < 85
            )
            UPDATE n
            SET PadreClave = c.PadreClave
            FROM #Nodos n
            JOIN Candidatos c ON c.Clave = n.Clave AND c.Rn = (n.R2 % c.Cnt) + 1;
        END;

        -- Raices: agrupacion (~60%) y FechaCreacion dentro del rango del proyecto.
        UPDATE n
        SET AgrupacionId  = CASE WHEN n.R3 % 100 < 60 THEN a.AgrupacionId END,
            FechaCreacion = DATEADD(SECOND,
                                    CAST(n.RB % DATEDIFF(SECOND, p.IniCreacion, p.FinCreacion) AS INT),
                                    p.IniCreacion)
        FROM #Nodos n
        JOIN #Proy p ON p.Idx = n.ProyectoIdx
        LEFT JOIN #Agr a ON a.ProyectoId = n.ProyectoId AND a.Idx = ((n.R3 / 100) % a.Cant) + 1
        WHERE n.NivelIdx = @i AND n.PadreClave IS NULL;

        -- Hijas: heredan la agrupacion del padre (RN-28) y se crean despues del padre.
        UPDATE n
        SET AgrupacionId  = pa.AgrupacionId,
            FechaCreacion = DATEADD(SECOND,
                                    60 + CAST(n.RB % CASE WHEN DATEDIFF(SECOND, pa.FechaCreacion, p.FinCreacion) / 2 > 1
                                                          THEN DATEDIFF(SECOND, pa.FechaCreacion, p.FinCreacion) / 2
                                                          ELSE 1 END AS INT),
                                    pa.FechaCreacion)
        FROM #Nodos n
        JOIN #Nodos pa ON pa.Clave = n.PadreClave
        JOIN #Proy p ON p.Idx = n.ProyectoIdx
        WHERE n.NivelIdx = @i;

        -- Vencimiento: hija de padre con vencimiento => <= al del padre (RN-31), ~70% con fecha.
        UPDATE n
        SET FechaVencimiento = CASE WHEN n.R4 % 100 < 70
                                    THEN DATEADD(DAY, -((n.R4 / 100) % 21), pa.FechaVencimiento) END
        FROM #Nodos n
        JOIN #Nodos pa ON pa.Clave = n.PadreClave
        WHERE n.NivelIdx = @i AND pa.FechaVencimiento IS NOT NULL;

        -- Resto (raices o padre sin vencimiento): ~65% con fecha entre la creacion + 7 dias y
        -- la fecha objetivo del proyecto + 30 dias (muchas quedan vencidas en proyectos viejos).
        UPDATE n
        SET FechaVencimiento = CASE WHEN n.R4 % 100 < 65
                                    THEN DATEADD(DAY,
                                                 (n.R4 / 100) % CASE WHEN DATEDIFF(DAY, DATEADD(DAY, 7, CAST(n.FechaCreacion AS DATE)),
                                                                                  DATEADD(DAY, 30, p.FechaObjetivo)) > 30
                                                                     THEN DATEDIFF(DAY, DATEADD(DAY, 7, CAST(n.FechaCreacion AS DATE)),
                                                                                   DATEADD(DAY, 30, p.FechaObjetivo))
                                                                     ELSE 30 END,
                                                 DATEADD(DAY, 7, CAST(n.FechaCreacion AS DATE))) END
        FROM #Nodos n
        JOIN #Proy p ON p.Idx = n.ProyectoIdx
        LEFT JOIN #Nodos pa ON pa.Clave = n.PadreClave
        WHERE n.NivelIdx = @i AND (n.PadreClave IS NULL OR pa.FechaVencimiento IS NULL);

        SET @i += 1;
    END;

    -- Numero por proyecto en orden de creacion (el padre siempre tiene numero menor).
    WITH Orden AS (
        SELECT Clave, ROW_NUMBER() OVER (PARTITION BY ProyectoId ORDER BY FechaCreacion, Clave) AS Numero
        FROM #Nodos
    )
    UPDATE n SET Numero = o.Numero
    FROM #Nodos n JOIN Orden o ON o.Clave = n.Clave;

    -- Estado (dentro del flujo del tipo), prioridad, responsable, estimacion.
    UPDATE n
    SET EstadoId      = CASE WHEN p.Categoria = 'F' AND n.R5 % 100 < 75 AND f.EstadoFinalId IS NOT NULL
                                 THEN f.EstadoFinalId
                             WHEN p.Categoria = 'I' AND n.R5 % 100 < 70
                                 THEN f.EstadoInicialId
                             ELSE ef.EstadoId END,
        PrioridadId   = CASE WHEN n.R7 % 100 < 35 THEN @PrioridadPredId ELSE pr.PrioridadId END,
        ResponsableId = CASE WHEN n.R8 % 100 < 78 THEN pe.PersonaId END,
        Estimacion    = CASE WHEN n.R9 % 100 < 60 THEN CAST(((n.R9 / 100) % 81) * 0.5 AS DECIMAL(9,2)) END
    FROM #Nodos n
    JOIN #Proy p ON p.Idx = n.ProyectoIdx
    JOIN #Tipos t ON t.TipoId = n.TipoId
    JOIN #Flujos f ON f.FlujoId = t.FlujoId
    JOIN #EstadosFlujo ef ON ef.FlujoId = t.FlujoId AND ef.Idx = (n.R6 % ef.Cant) + 1
    JOIN #Prioridades pr ON pr.Idx = ((n.R7 / 100) % pr.Cant) + 1
    JOIN #Personas pe ON pe.Idx = ((n.R8 / 100) % pe.Cant) + 1;

    -- FechaModificacion: obligatoria si el estado no es el inicial (hubo edicion); ~30% de las
    -- que siguen en el estado inicial tambien fueron editadas. Siempre entre creacion y fin.
    UPDATE n
    SET FechaModificacion = CASE WHEN n.EstadoId <> f.EstadoInicialId OR n.R10 % 100 < 30
                                 THEN DATEADD(SECOND,
                                              1 + CAST(n.RB2 % CASE WHEN DATEDIFF(SECOND, n.FechaCreacion, p.FinModificacion) > 1
                                                                    THEN DATEDIFF(SECOND, n.FechaCreacion, p.FinModificacion) - 1
                                                                    ELSE 1 END AS INT),
                                              n.FechaCreacion) END
    FROM #Nodos n
    JOIN #Proy p ON p.Idx = n.ProyectoIdx
    JOIN #Tipos t ON t.TipoId = n.TipoId
    JOIN #Flujos f ON f.FlujoId = t.FlujoId;

    -- Titulos y descripciones verosimiles segun el tipo.
    DECLARE @Acciones TABLE (Idx INT NOT NULL PRIMARY KEY, Texto NVARCHAR(30) NOT NULL);
    INSERT INTO @Acciones (Idx, Texto)
    VALUES (1, N'registrar'), (2, N'consultar'), (3, N'editar'), (4, N'exportar'), (5, N'filtrar'),
           (6, N'aprobar'), (7, N'anular'), (8, N'imprimir'), (9, N'buscar'), (10, N'importar'),
           (11, N'validar'), (12, N'notificar');

    DECLARE @Objetos TABLE (Idx INT NOT NULL PRIMARY KEY, Texto NVARCHAR(40) NOT NULL);
    INSERT INTO @Objetos (Idx, Texto)
    VALUES (1, N'facturas'), (2, N'clientes'), (3, N'pedidos'), (4, N'órdenes de compra'),
           (5, N'reportes de ventas'), (6, N'usuarios'), (7, N'proveedores'), (8, N'pagos'),
           (9, N'turnos'), (10, N'productos en stock'), (11, N'remitos'), (12, N'presupuestos'),
           (13, N'notas de crédito'), (14, N'contratos'), (15, N'reclamos');

    DECLARE @Roles TABLE (Idx INT NOT NULL PRIMARY KEY, Texto NVARCHAR(40) NOT NULL);
    INSERT INTO @Roles (Idx, Texto)
    VALUES (1, N'administrador'), (2, N'vendedor'), (3, N'supervisor de ventas'),
           (4, N'operador de depósito'), (5, N'cliente'), (6, N'analista contable');

    DECLARE @PrefijosSubtarea TABLE (Idx INT NOT NULL PRIMARY KEY, Texto NVARCHAR(50) NOT NULL);
    INSERT INTO @PrefijosSubtarea (Idx, Texto)
    VALUES (1, N'Implementar el servicio para '), (2, N'Diseñar la pantalla para '),
           (3, N'Escribir tests unitarios para '), (4, N'Ajustar la consulta SQL para '),
           (5, N'Documentar el endpoint para ');

    UPDATE n
    SET Titulo = CASE
                     WHEN t.Nombre = N'Épica' OR (t.NivelIdx = 1 AND t.Nombre NOT IN (N'Historia de Usuario', N'Subtarea', N'Caso de Prueba', N'Bug'))
                         THEN CONCAT(N'Gestión de ', o.Texto,
                                     CASE (n.R12 / 100) % 3 WHEN 1 THEN N' - etapa 2' WHEN 2 THEN N' - mejoras' ELSE N'' END)
                     WHEN t.Nombre = N'Historia de Usuario'
                         THEN CONCAT(N'Como ', r.Texto, N' quiero ', a.Texto, N' ', o.Texto)
                     WHEN t.Nombre = N'Subtarea'
                         THEN CONCAT(ps.Texto, a.Texto, N' ', o.Texto)
                     WHEN t.Nombre = N'Caso de Prueba'
                         THEN CONCAT(N'Verificar que se puedan ', a.Texto, N' ', o.Texto)
                     WHEN t.Nombre = N'Bug'
                         THEN CONCAT(N'Error al ', a.Texto, N' ', o.Texto)
                     ELSE CONCAT(t.Nombre, N': ', a.Texto, N' ', o.Texto)
                 END,
        Descripcion = CASE
                     WHEN t.Nombre = N'Épica' OR (t.NivelIdx = 1 AND t.Nombre NOT IN (N'Historia de Usuario', N'Subtarea', N'Caso de Prueba', N'Bug'))
                         THEN CONCAT(N'Agrupa las historias de usuario relacionadas con la gestión de ', o.Texto,
                                     N' dentro del proyecto.')
                     WHEN t.Nombre = N'Historia de Usuario'
                         THEN CONCAT(N'Como ', r.Texto, N', quiero ', a.Texto, N' ', o.Texto,
                                     N' para agilizar mi trabajo diario. Criterios de aceptación: la operación queda ',
                                     N'registrada en el historial y se informa el resultado al usuario.')
                     WHEN t.Nombre = N'Subtarea'
                         THEN CONCAT(N'Tarea técnica para ', a.Texto, N' ', o.Texto,
                                     N'. Incluye implementación, pruebas unitarias y revisión de código.')
                     WHEN t.Nombre = N'Caso de Prueba'
                         THEN CONCAT(N'Precondición: usuario con permisos. Pasos: ingresar al módulo de ', o.Texto,
                                     N' e intentar ', a.Texto, N'. Resultado esperado: la operación finaliza sin errores ',
                                     N'y los datos quedan actualizados.')
                     WHEN t.Nombre = N'Bug'
                         THEN CONCAT(N'Al intentar ', a.Texto, N' ', o.Texto,
                                     N' el sistema muestra un mensaje de error inesperado. Pasos para reproducir: ingresar ',
                                     N'al módulo, completar los datos y confirmar. Resultado esperado: la operación se completa correctamente.')
                     ELSE CONCAT(t.Nombre, N' para ', a.Texto, N' ', o.Texto, N'.')
                 END
    FROM #Nodos n
    JOIN #Tipos t ON t.TipoId = n.TipoId
    JOIN @Acciones a ON a.Idx = (n.R11 % 12) + 1
    JOIN @Objetos o ON o.Idx = (n.R12 % 15) + 1
    JOIN @Roles r ON r.Idx = ((n.R11 / 12) % 6) + 1
    JOIN @PrefijosSubtarea ps ON ps.Idx = ((n.R11 / 72) % 5) + 1;

    -------------------------------------------------------------------------------------------
    -- 6. Insercion en Incidencias (orden de numero) y enlace de padres
    -------------------------------------------------------------------------------------------
    INSERT INTO dbo.Incidencias
        (ProyectoId, Numero, Codigo, Titulo, Descripcion, TipoId, EstadoId, PrioridadId, ResponsableId,
         FechaVencimiento, Estimacion, AgrupacionId, PadreId, FechaCreacion, FechaModificacion)
    SELECT n.ProyectoId, n.Numero,
           CONCAT(p.Codigo, N'-', n.Numero),           -- FormateadorCodigoIncidencia: sin relleno
           n.Titulo, n.Descripcion, n.TipoId, n.EstadoId, n.PrioridadId, n.ResponsableId,
           n.FechaVencimiento, n.Estimacion, n.AgrupacionId, NULL, n.FechaCreacion, n.FechaModificacion
    FROM #Nodos n
    JOIN #Proy p ON p.Idx = n.ProyectoIdx
    ORDER BY n.ProyectoId, n.Numero;

    UPDATE n
    SET IncidenciaId = i.Id
    FROM #Nodos n
    JOIN dbo.Incidencias i ON i.ProyectoId = n.ProyectoId AND i.Numero = n.Numero;

    UPDATE i
    SET PadreId = pa.IncidenciaId
    FROM dbo.Incidencias i
    JOIN #Nodos n ON n.IncidenciaId = i.Id
    JOIN #Nodos pa ON pa.Clave = n.PadreClave;

    -- Contador de numeracion (IncidenciaRepository.ObtenerSiguienteNumeroAsync).
    UPDATE pr
    SET UltimoNumeroIncidencia = x.MaxNumero
    FROM dbo.Proyectos pr
    JOIN (SELECT ProyectoId, MAX(Numero) AS MaxNumero FROM #Nodos GROUP BY ProyectoId) x
        ON x.ProyectoId = pr.Id;

    DECLARE @CantProy INT = (SELECT COUNT(*) FROM #Proy), @CantInc INT = (SELECT COUNT(*) FROM #Nodos);
    PRINT CONCAT(N'Datos de prueba cargados: ', @CantProy, N' proyectos, ', @CantInc, N' incidencias.');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
