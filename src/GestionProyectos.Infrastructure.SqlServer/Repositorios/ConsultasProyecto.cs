using GestionProyectos.Application.Proyectos;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// SQL de lectura de proyectos. El ORDER BY y el filtro de archivados salen solo de listas blancas.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal static class ConsultasProyecto
{
    /// <summary>Detalle de un proyecto con responsable, estados, archivador y agrupaciones.</summary>
    public const string Detalle = """
        SELECT p.Id, p.Codigo, p.Nombre, p.Descripcion, p.FechaInicio, p.FechaObjetivo,
               p.FechaArchivado, p.FechaCreacion, p.FechaModificacion,
               r.Id AS ResponsableId, r.Nombre AS ResponsableNombre, r.Activo AS ResponsableActivo,
               e.Id AS EstadoId, e.Nombre AS EstadoNombre, e.EsFinal AS EstadoEsFinal,
               e.Activo AS EstadoActivo,
               CAST(CASE WHEN e.Codigo IS NULL THEN 0 ELSE 1 END AS BIT) AS EstadoEsSistema,
               a.Id AS ArchivadoPorId, a.Nombre AS ArchivadoPorNombre, a.Activo AS ArchivadoPorActivo,
               ea.Id AS EstadoAlArchivarId, ea.Nombre AS EstadoAlArchivarNombre,
               ea.EsFinal AS EstadoAlArchivarEsFinal, ea.Activo AS EstadoAlArchivarActivo,
               CAST(CASE WHEN ea.Codigo IS NULL THEN 0 ELSE 1 END AS BIT) AS EstadoAlArchivarEsSistema,
               (SELECT COUNT(*) FROM dbo.Agrupaciones g WHERE g.ProyectoId = p.Id) AS CantidadAgrupaciones
        FROM dbo.Proyectos p
        INNER JOIN dbo.Personas r ON r.Id = p.ResponsableId
        INNER JOIN dbo.EstadosProyecto e ON e.Id = p.EstadoId
        LEFT JOIN dbo.Personas a ON a.Id = p.ArchivadoPorId
        LEFT JOIN dbo.EstadosProyecto ea ON ea.Id = p.EstadoAlArchivarId
        WHERE p.Id = @Id;
        """;

    private const string FiltrosComunes = """
        (@Patron IS NULL OR p.Codigo LIKE @Patron ESCAPE '\' OR p.Nombre LIKE @Patron ESCAPE '\')
          AND (@EstadoId IS NULL OR p.EstadoId = @EstadoId)
          AND (@ResponsableId IS NULL OR p.ResponsableId = @ResponsableId)
        """;

    /// <summary>Lote con la página de resultados y el total, según el filtro.</summary>
    public static string Listado(FiltroProyectos filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var where = $"{FiltrosComunes}\n  AND {CondicionArchivados(filtro.Archivados)}";
        return $"""
            SELECT p.Id, p.Codigo, p.Nombre, p.FechaInicio, p.FechaObjetivo, p.FechaArchivado,
                   r.Id AS ResponsableId, r.Nombre AS ResponsableNombre, r.Activo AS ResponsableActivo,
                   e.Id AS EstadoId, e.Nombre AS EstadoNombre, e.EsFinal AS EstadoEsFinal,
                   e.Activo AS EstadoActivo,
                   CAST(CASE WHEN e.Codigo IS NULL THEN 0 ELSE 1 END AS BIT) AS EstadoEsSistema
            FROM dbo.Proyectos p
            INNER JOIN dbo.Personas r ON r.Id = p.ResponsableId
            INNER JOIN dbo.EstadosProyecto e ON e.Id = p.EstadoId
            WHERE {where}
            ORDER BY {OrdenarPor(filtro.OrdenarPor, filtro.Descendente)}
            OFFSET @Desde ROWS FETCH NEXT @TamanioPagina ROWS ONLY
            OPTION (RECOMPILE);

            SELECT COUNT(*)
            FROM dbo.Proyectos p
            WHERE {where}
            OPTION (RECOMPILE);
            """;
    }

    private static string CondicionArchivados(FiltroArchivados archivados) => archivados switch
    {
        FiltroArchivados.Excluir => "p.FechaArchivado IS NULL",
        FiltroArchivados.Solo => "p.FechaArchivado IS NOT NULL",
        FiltroArchivados.Incluir => "1 = 1",
        _ => throw new ArgumentOutOfRangeException(nameof(archivados), archivados, null),
    };

    private static string OrdenarPor(OrdenProyectos orden, bool descendente)
    {
        var dir = descendente ? "DESC" : "ASC";
        var columnas = orden switch
        {
            // LEN primero para que PRJ-10000 quede después de PRJ-9999.
            OrdenProyectos.Codigo => $"LEN(p.Codigo) {dir}, p.Codigo {dir}",
            OrdenProyectos.Nombre => $"p.Nombre {dir}",
            OrdenProyectos.FechaInicio => $"p.FechaInicio {dir}",
            OrdenProyectos.FechaObjetivo => $"p.FechaObjetivo {dir}",
            OrdenProyectos.Estado => $"e.Orden {dir}, e.Nombre {dir}",
            OrdenProyectos.Responsable => $"r.Nombre {dir}",
            _ => throw new ArgumentOutOfRangeException(nameof(orden), orden, null),
        };
        return $"{columnas}, p.Id {dir}";
    }
}
