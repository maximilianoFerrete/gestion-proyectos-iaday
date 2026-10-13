using GestionProyectos.Application.Seguimiento;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// SQL del seguimiento: conteos crudos, proyectos y grilla del tablero. Los fragmentos dinámicos
/// salen de listas blancas; los valores siempre van como parámetros (RF-005 0.2, 0.3).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
internal static class ConsultasSeguimiento
{
    /// <summary>Incidencia abierta: estado no final en el flujo de su tipo (RN-09).</summary>
    private const string Abierta = "ISNULL(fe.EsFinal, 0) = 0";

    /// <summary>Joins comunes; EsFinal es el del flujo del tipo de la incidencia (como RF-002).</summary>
    private const string JoinsFlujo = """
        FROM dbo.Incidencias i
        INNER JOIN dbo.TiposIncidencia t ON t.Id = i.TipoId
        LEFT JOIN dbo.FlujosIncidenciaEstados fe ON fe.FlujoId = t.FlujoId AND fe.EstadoId = i.EstadoId
        """;

    /// <summary>Catálogo completo de estados de incidencia.</summary>
    public const string Estados = """
        SELECT Id, Nombre, Descripcion, Orden, Activo, Categoria, FechaCreacion, FechaModificacion
        FROM dbo.EstadosIncidencia;
        """;

    /// <summary>
    /// Condición SQL única de cada indicador (replica RN-09 a RN-14). Usa los alias i y fe y los
    /// parámetros @FechaReferencia y @CategoriaIndicador.
    /// </summary>
    public static string CondicionIndicador(IndicadorSeguimiento indicador) => indicador switch
    {
        IndicadorSeguimiento.Abiertas => Abierta,
        IndicadorSeguimiento.Vencidas => $"{Abierta} AND i.FechaVencimiento < @FechaReferencia",
        IndicadorSeguimiento.Bloqueadas or IndicadorSeguimiento.EnEspera =>
            $"{Abierta} AND i.EstadoId IN (SELECT ec.Id FROM dbo.EstadosIncidencia ec "
            + "WHERE ec.Categoria = @CategoriaIndicador)",
        IndicadorSeguimiento.SinResponsable => $"{Abierta} AND i.ResponsableId IS NULL",
        _ => throw new ArgumentOutOfRangeException(nameof(indicador), indicador, null),
    };

    /// <summary>Proyectos del tablero general con responsable, estado y cantidad de agrupaciones.</summary>
    public static string Proyectos(FiltroProyectosSeguimiento filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        return $"""
            SELECT p.Id, p.Codigo, p.Nombre, p.FechaObjetivo, p.FechaArchivado,
                   r.Id AS ResponsableId, r.Nombre AS ResponsableNombre, r.Activo AS ResponsableActivo,
                   e.Id AS EstadoId, e.Nombre AS EstadoNombre, e.EsFinal AS EstadoEsFinal,
                   e.Activo AS EstadoActivo,
                   CAST(CASE WHEN e.Codigo IS NULL THEN 0 ELSE 1 END AS BIT) AS EstadoEsSistema,
                   (SELECT COUNT(*) FROM dbo.Agrupaciones g WHERE g.ProyectoId = p.Id) AS CantidadAgrupaciones
            FROM dbo.Proyectos p
            INNER JOIN dbo.Personas r ON r.Id = p.ResponsableId
            INNER JOIN dbo.EstadosProyecto e ON e.Id = p.EstadoId
            WHERE {CondicionProyectos(filtro)}
            OPTION (RECOMPILE);
            """;
    }

    /// <summary>Conteos crudos por (grupo, estado, EsFinal) con GROUP BY y SUM(CASE ...) (0.2, 0.3).</summary>
    public static string Conteo(FiltroConteoIncidencias filtro, AgrupacionConteo agruparPor)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var (grupoSelect, grupoGroupBy) = agruparPor switch
        {
            AgrupacionConteo.Ninguna => ("CAST(NULL AS INT)", string.Empty),
            AgrupacionConteo.Proyecto => ("i.ProyectoId", "i.ProyectoId, "),
            AgrupacionConteo.Agrupacion => ("i.AgrupacionId", "i.AgrupacionId, "),
            _ => throw new ArgumentOutOfRangeException(nameof(agruparPor), agruparPor, null),
        };
        var condiciones = new List<string>(CondicionesAmbito(filtro.TipoIds is not null));
        string joinProyectos;
        if (filtro.Proyectos is { } proyectos)
        {
            joinProyectos = "INNER JOIN dbo.Proyectos p ON p.Id = i.ProyectoId";
            condiciones.Insert(0, CondicionProyectos(proyectos));
        }
        else
        {
            joinProyectos = string.Empty;
            condiciones.Insert(0, "i.ProyectoId = @ProyectoId");
        }

        return $"""
            SELECT {grupoSelect} AS Grupo, i.EstadoId, CAST(ISNULL(fe.EsFinal, 0) AS BIT) AS EsFinal,
                   COUNT(*) AS Cantidad,
                   SUM(CASE WHEN i.FechaVencimiento < @FechaReferencia THEN 1 ELSE 0 END) AS ConVencimientoAnterior,
                   SUM(CASE WHEN i.ResponsableId IS NULL THEN 1 ELSE 0 END) AS SinResponsable
            {JoinsFlujo}
            {joinProyectos}
            WHERE {string.Join("\n  AND ", condiciones)}
            GROUP BY {grupoGroupBy}i.EstadoId, ISNULL(fe.EsFinal, 0)
            OPTION (RECOMPILE);
            """;
    }

    /// <summary>Estados de los flujos que usan los tipos indicados (o todos los tipos).</summary>
    public static string EstadosDeFlujos(bool filtrarTipos)
    {
        var filtroTipos = filtrarTipos ? "WHERE t.Id IN @TipoIds" : string.Empty;
        return $"""
            SELECT DISTINCT fe.FlujoId, fe.EstadoId, fe.EsFinal
            FROM dbo.FlujosIncidenciaEstados fe
            WHERE fe.FlujoId IN (SELECT t.FlujoId FROM dbo.TiposIncidencia t {filtroTipos});
            """;
    }

    /// <summary>Grilla del tablero: ámbito, tipos, estado, texto e indicador (RN-20, RN-21).</summary>
    public static string Tablero(FiltroTableroIncidencias filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var condiciones = new List<string>(CondicionesAmbito(filtro.TipoIds is not null))
        {
            "(@EstadoId IS NULL OR i.EstadoId = @EstadoId)",
            @"(@Patron IS NULL OR i.Codigo LIKE @Patron ESCAPE '\' OR i.Titulo LIKE @Patron ESCAPE '\')",
        };
        condiciones.Insert(0, "i.ProyectoId = @ProyectoId");
        if (filtro.Indicador is { } indicador)
        {
            condiciones.Add($"({CondicionIndicador(indicador)})");
        }

        return $"""
            SELECT i.Id, i.Codigo, i.Numero, i.Titulo, i.Estimacion, i.FechaVencimiento,
                   t.Id AS TipoId, t.Nombre AS TipoNombre, t.Activo AS TipoActivo, n.Numero AS TipoNivelNumero,
                   e.Id AS EstadoId, e.Nombre AS EstadoNombre, e.Orden AS EstadoOrden, e.Activo AS EstadoActivo,
                   CAST(ISNULL(fe.EsFinal, 0) AS BIT) AS EstadoEsFinal, e.Categoria AS EstadoCategoria,
                   r.Id AS ResponsableId, r.Nombre AS ResponsableNombre, r.Activo AS ResponsableActivo
            {JoinsFlujo}
            INNER JOIN dbo.NivelesIncidencia n ON n.Id = t.NivelId
            INNER JOIN dbo.EstadosIncidencia e ON e.Id = i.EstadoId
            LEFT JOIN dbo.Personas r ON r.Id = i.ResponsableId
            WHERE {string.Join("\n  AND ", condiciones)}
            ORDER BY n.Numero, t.Nombre, i.Numero
            OPTION (RECOMPILE);
            """;
    }

    private static IEnumerable<string> CondicionesAmbito(bool filtrarTipos)
    {
        yield return "(@AgrupacionId IS NULL OR i.AgrupacionId = @AgrupacionId)";
        yield return "(@SinAgrupacion = 0 OR i.AgrupacionId IS NULL)";
        if (filtrarTipos)
        {
            // Dapper expande @TipoIds en una lista de parámetros (@TipoIds1, @TipoIds2, ...).
            yield return "i.TipoId IN @TipoIds";
        }
    }

    private static string CondicionProyectos(FiltroProyectosSeguimiento filtro) =>
        @"(@PatronProyecto IS NULL OR p.Codigo LIKE @PatronProyecto ESCAPE '\' "
        + @"OR p.Nombre LIKE @PatronProyecto ESCAPE '\')"
        + $"\n  AND {ConsultasProyecto.CondicionArchivados(filtro.Archivados)}";
}
