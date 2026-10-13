using GestionProyectos.Application.Incidencias;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// SQL de incidencias. ORDER BY y filtros opcionales salen solo de listas blancas y fragmentos fijos;
/// los valores siempre van como parámetros.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Estimación, categoría del estado, orden por estimación y árbol por ámbito (RF-005)
/// </remarks>
internal static class ConsultasIncidencia
{
    /// <summary>Columnas de una incidencia con sus referencias (tipo, estado, prioridad, etc.).</summary>
    private const string ColumnasConReferencias = """
        i.Id, i.ProyectoId, i.Numero, i.Codigo, i.Titulo, i.PadreId, i.FechaVencimiento, i.Estimacion,
               i.FechaCreacion, i.FechaModificacion,
               t.Id AS TipoId, t.Nombre AS TipoNombre, t.Activo AS TipoActivo, n.Numero AS TipoNivelNumero,
               e.Id AS EstadoId, e.Nombre AS EstadoNombre, e.Activo AS EstadoActivo,
               CAST(ISNULL(fe.EsFinal, 0) AS BIT) AS EstadoEsFinal, e.Categoria AS EstadoCategoria,
               pr.Id AS PrioridadId, pr.Nombre AS PrioridadNombre, pr.Orden AS PrioridadOrden,
               pr.Activo AS PrioridadActivo,
               r.Id AS ResponsableId, r.Nombre AS ResponsableNombre, r.Activo AS ResponsableActivo,
               g.Id AS AgrupacionId, g.Nombre AS AgrupacionNombre,
               pa.Codigo AS PadreCodigo,
               (SELECT COUNT(*) FROM dbo.Incidencias h WHERE h.PadreId = i.Id) AS CantidadHijas
        """;

    /// <summary>Joins de las referencias; el EsFinal del estado es el del flujo del tipo.</summary>
    private const string JoinsReferencias = """
        FROM dbo.Incidencias i
        INNER JOIN dbo.TiposIncidencia t ON t.Id = i.TipoId
        INNER JOIN dbo.NivelesIncidencia n ON n.Id = t.NivelId
        INNER JOIN dbo.EstadosIncidencia e ON e.Id = i.EstadoId
        LEFT JOIN dbo.FlujosIncidenciaEstados fe ON fe.FlujoId = t.FlujoId AND fe.EstadoId = i.EstadoId
        INNER JOIN dbo.PrioridadesIncidencia pr ON pr.Id = i.PrioridadId
        LEFT JOIN dbo.Personas r ON r.Id = i.ResponsableId
        LEFT JOIN dbo.Agrupaciones g ON g.Id = i.AgrupacionId
        LEFT JOIN dbo.Incidencias pa ON pa.Id = i.PadreId
        """;

    /// <summary>Incidencia por id con referencias, descripción y datos del proyecto.</summary>
    public const string Detalle = $"""
        SELECT {ColumnasConReferencias},
               i.Descripcion,
               p.Codigo AS ProyectoCodigo, p.Nombre AS ProyectoNombre, p.FechaArchivado AS ProyectoFechaArchivado
        {JoinsReferencias}
        INNER JOIN dbo.Proyectos p ON p.Id = i.ProyectoId
        WHERE i.Id = @Id;
        """;

    /// <summary>Padre de la incidencia indicada (0 o 1 fila).</summary>
    public const string Padre = $"""
        SELECT {ColumnasConReferencias}
        {JoinsReferencias}
        WHERE i.Id = (SELECT x.PadreId FROM dbo.Incidencias x WHERE x.Id = @Id);
        """;

    /// <summary>Hijas directas de la incidencia ordenadas por número.</summary>
    public const string Hijas = $"""
        SELECT {ColumnasConReferencias}
        {JoinsReferencias}
        WHERE i.PadreId = @Id
        ORDER BY i.Numero;
        """;

    /// <summary>Tipos activos que pueden ser hijos del tipo de la incidencia.</summary>
    public const string TiposHijaPermitidos = """
        SELECT th.Id, th.Nombre
        FROM dbo.RelacionesTiposIncidencia rt
        INNER JOIN dbo.TiposIncidencia th ON th.Id = rt.TipoHijoId
        WHERE rt.TipoPadreId = (SELECT x.TipoId FROM dbo.Incidencias x WHERE x.Id = @Id) AND th.Activo = 1
        ORDER BY th.Nombre, th.Id;
        """;

    /// <summary>Todas las incidencias del proyecto ordenadas por número (árbol plano).</summary>
    public const string Arbol = $"""
        SELECT {ColumnasConReferencias}
        {JoinsReferencias}
        WHERE i.ProyectoId = @ProyectoId
        ORDER BY i.Numero;
        """;

    /// <summary>Incidencias del ámbito (agrupación o sin agrupación) ordenadas por número.</summary>
    public const string ArbolPorAmbito = $"""
        SELECT {ColumnasConReferencias}
        {JoinsReferencias}
        WHERE i.ProyectoId = @ProyectoId
          AND (@AgrupacionId IS NULL OR i.AgrupacionId = @AgrupacionId)
          AND (@SinAgrupacion = 0 OR i.AgrupacionId IS NULL)
        ORDER BY i.Numero;
        """;

    /// <summary>Ancestros de una incidencia (CTE recursiva por PadreId), sin incluirla.</summary>
    public const string Ancestros = """
        WITH Ancestros (Id, PadreId) AS (
            SELECT i.Id, i.PadreId FROM dbo.Incidencias i WHERE i.Id = @Id
            UNION ALL
            SELECT p.Id, p.PadreId FROM dbo.Incidencias p INNER JOIN Ancestros a ON p.Id = a.PadreId
        )
        SELECT Id FROM Ancestros WHERE Id <> @Id
        OPTION (MAXRECURSION 100);
        """;

    /// <summary>Propaga la agrupación a todos los descendientes (CTE recursiva) que la tengan distinta.</summary>
    public const string PropagarAgrupacion = """
        WITH Descendientes (Id) AS (
            SELECT h.Id FROM dbo.Incidencias h WHERE h.PadreId = @Id
            UNION ALL
            SELECT h.Id FROM dbo.Incidencias h INNER JOIN Descendientes d ON h.PadreId = d.Id
        )
        UPDATE i
        SET AgrupacionId = @AgrupacionId, FechaModificacion = @Ahora
        FROM dbo.Incidencias i
        INNER JOIN Descendientes d ON d.Id = i.Id
        WHERE NOT EXISTS (SELECT i.AgrupacionId INTERSECT SELECT @AgrupacionId)
        OPTION (MAXRECURSION 100);
        """;

    /// <summary>Lote con la página de resultados y el total, según el filtro.</summary>
    public static string Listado(FiltroIncidencias filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var where = Filtros(filtro);
        return $"""
            SELECT {ColumnasConReferencias}
            {JoinsReferencias}
            WHERE {where}
            ORDER BY {OrdenarPor(filtro.OrdenarPor, filtro.Descendente)}
            OFFSET @Desde ROWS FETCH NEXT @TamanioPagina ROWS ONLY
            OPTION (RECOMPILE);

            SELECT COUNT(*)
            FROM dbo.Incidencias i
            WHERE {where}
            OPTION (RECOMPILE);
            """;
    }

    private static string Filtros(FiltroIncidencias filtro)
    {
        var condiciones = new List<string>
        {
            "i.ProyectoId = @ProyectoId",
            @"(@Patron IS NULL OR i.Codigo LIKE @Patron ESCAPE '\' OR i.Titulo LIKE @Patron ESCAPE '\')",
            "(@EstadoId IS NULL OR i.EstadoId = @EstadoId)",
            "(@PrioridadId IS NULL OR i.PrioridadId = @PrioridadId)",
            "(@ResponsableId IS NULL OR i.ResponsableId = @ResponsableId)",
            "(@SinResponsable = 0 OR i.ResponsableId IS NULL)",
            "(@AgrupacionId IS NULL OR i.AgrupacionId = @AgrupacionId)",
            "(@SinAgrupacion = 0 OR i.AgrupacionId IS NULL)",
        };
        if (filtro.TipoIds.Count > 0)
        {
            // Dapper expande @TipoIds en una lista de parámetros (@TipoIds1, @TipoIds2, ...).
            condiciones.Add("i.TipoId IN @TipoIds");
        }

        return string.Join("\n  AND ", condiciones);
    }

    private static string OrdenarPor(OrdenIncidencias orden, bool descendente)
    {
        var dir = descendente ? "DESC" : "ASC";
        var columnas = orden switch
        {
            OrdenIncidencias.Numero => $"i.Numero {dir}",
            OrdenIncidencias.Titulo => $"i.Titulo {dir}",
            OrdenIncidencias.Tipo => $"t.Nombre {dir}",
            OrdenIncidencias.Estado => $"e.Orden {dir}, e.Nombre {dir}",
            OrdenIncidencias.Prioridad => $"pr.Orden {dir}, pr.Nombre {dir}",
            OrdenIncidencias.Responsable => $"r.Nombre {dir}",
            OrdenIncidencias.FechaVencimiento => $"i.FechaVencimiento {dir}",
            OrdenIncidencias.FechaCreacion => $"i.FechaCreacion {dir}",
            // Nulos al final en ambas direcciones (CASE portable, RF-005 0.3).
            OrdenIncidencias.Estimacion =>
                $"CASE WHEN i.Estimacion IS NULL THEN 1 ELSE 0 END ASC, i.Estimacion {dir}",
            _ => throw new ArgumentOutOfRangeException(nameof(orden), orden, null),
        };
        return orden == OrdenIncidencias.Numero ? columnas : $"{columnas}, i.Numero {dir}";
    }
}
