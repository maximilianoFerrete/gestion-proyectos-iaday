namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Columnas permitidas para ordenar el listado de incidencias (lista blanca).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public enum OrdenIncidencias
{
    Numero,
    Titulo,
    Tipo,
    Estado,
    Prioridad,
    Responsable,
    FechaVencimiento,
    FechaCreacion,
}

/// <summary>
/// Criterios ya validados y con defaults aplicados para listar incidencias de un proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record FiltroIncidencias(
    int ProyectoId,
    string? Texto,
    IReadOnlyList<int> TipoIds,
    int? EstadoId,
    int? PrioridadId,
    int? ResponsableId,
    bool SinResponsable,
    int? AgrupacionId,
    bool SinAgrupacion,
    int Pagina,
    int TamanioPagina,
    OrdenIncidencias OrdenarPor,
    bool Descendente);

/// <summary>
/// Valores admitidos y por defecto de los parámetros del listado de incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public static class OpcionesListadoIncidencias
{
    public const int PaginaPorDefecto = 1;
    public const int TamanioPaginaPorDefecto = 20;
    public const int TamanioPaginaMaximo = 100;
    public const int LargoMaximoTexto = 200;
    public const string DireccionAsc = "asc";
    public const string DireccionDesc = "desc";

    /// <summary>Valores de query admitidos para "ordenarPor" (en minúsculas).</summary>
    public static IReadOnlyDictionary<string, OrdenIncidencias> Ordenes { get; } =
        new Dictionary<string, OrdenIncidencias>
        {
            ["numero"] = OrdenIncidencias.Numero,
            ["titulo"] = OrdenIncidencias.Titulo,
            ["tipo"] = OrdenIncidencias.Tipo,
            ["estado"] = OrdenIncidencias.Estado,
            ["prioridad"] = OrdenIncidencias.Prioridad,
            ["responsable"] = OrdenIncidencias.Responsable,
            ["fechavencimiento"] = OrdenIncidencias.FechaVencimiento,
            ["fechacreacion"] = OrdenIncidencias.FechaCreacion,
        };

    /// <summary>Convierte la query normalizada y validada en el filtro con defaults.</summary>
    public static FiltroIncidencias CrearFiltro(int proyectoId, ListarIncidenciasQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new FiltroIncidencias(
            proyectoId,
            query.Texto,
            (query.TipoId ?? []).Distinct().ToList(),
            query.EstadoId,
            query.PrioridadId,
            query.ResponsableId,
            query.SinResponsable ?? false,
            query.AgrupacionId,
            query.SinAgrupacion ?? false,
            query.Pagina ?? PaginaPorDefecto,
            query.TamanioPagina ?? TamanioPaginaPorDefecto,
            query.OrdenarPor is null ? OrdenIncidencias.Numero : Ordenes[query.OrdenarPor],
            query.Direccion is null or DireccionDesc);
    }
}
