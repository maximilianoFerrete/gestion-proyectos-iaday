namespace GestionProyectos.Application.Proyectos;

/// <summary>
/// Filtro por archivado del listado de proyectos (RN-24).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public enum FiltroArchivados
{
    Excluir,
    Solo,
    Incluir,
}

/// <summary>
/// Columnas permitidas para ordenar el listado de proyectos (lista blanca).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public enum OrdenProyectos
{
    Codigo,
    Nombre,
    FechaInicio,
    FechaObjetivo,
    Estado,
    Responsable,
}

/// <summary>
/// Criterios ya validados y con defaults aplicados para listar proyectos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record FiltroProyectos(
    string? Texto,
    int? EstadoId,
    int? ResponsableId,
    FiltroArchivados Archivados,
    int Pagina,
    int TamanioPagina,
    OrdenProyectos OrdenarPor,
    bool Descendente);

/// <summary>
/// Valores admitidos y por defecto de los parámetros del listado de proyectos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class OpcionesListadoProyectos
{
    public const int PaginaPorDefecto = 1;
    public const int TamanioPaginaPorDefecto = 20;
    public const int TamanioPaginaMaximo = 100;
    public const int LargoMaximoTexto = 150;
    public const string DireccionAsc = "asc";
    public const string DireccionDesc = "desc";

    /// <summary>Valores de query admitidos para "archivados".</summary>
    public static IReadOnlyDictionary<string, FiltroArchivados> Archivados { get; } =
        new Dictionary<string, FiltroArchivados>
        {
            ["excluir"] = FiltroArchivados.Excluir,
            ["solo"] = FiltroArchivados.Solo,
            ["incluir"] = FiltroArchivados.Incluir,
        };

    /// <summary>Valores de query admitidos para "ordenarPor".</summary>
    public static IReadOnlyDictionary<string, OrdenProyectos> Ordenes { get; } =
        new Dictionary<string, OrdenProyectos>
        {
            ["codigo"] = OrdenProyectos.Codigo,
            ["nombre"] = OrdenProyectos.Nombre,
            ["fechainicio"] = OrdenProyectos.FechaInicio,
            ["fechaobjetivo"] = OrdenProyectos.FechaObjetivo,
            ["estado"] = OrdenProyectos.Estado,
            ["responsable"] = OrdenProyectos.Responsable,
        };

    /// <summary>Convierte la query normalizada y validada en el filtro con defaults.</summary>
    public static FiltroProyectos CrearFiltro(ListarProyectosQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new FiltroProyectos(
            query.Texto,
            query.EstadoId,
            query.ResponsableId,
            query.Archivados is null ? FiltroArchivados.Excluir : Archivados[query.Archivados],
            query.Pagina ?? PaginaPorDefecto,
            query.TamanioPagina ?? TamanioPaginaPorDefecto,
            query.OrdenarPor is null ? OrdenProyectos.Codigo : Ordenes[query.OrdenarPor],
            query.Direccion is null or DireccionDesc);
    }
}
