using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Datos para crear una incidencia; el proyecto viene de la URL y el estado no se acepta (RN-20).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Estimacion opcional (RF-005, RN-34)
/// </remarks>
public sealed record CrearIncidenciaRequest(
    int? TipoId,
    string? Titulo,
    string? Descripcion,
    int? PrioridadId,
    int? ResponsableId,
    DateOnly? FechaVencimiento,
    int? AgrupacionId,
    int? PadreId,
    decimal? Estimacion = null)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-39).</summary>
    public CrearIncidenciaRequest Normalizar() => this with
    {
        Titulo = TextoEntrada.Normalizar(Titulo),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Datos para editar una incidencia (reemplazo completo); el tipo no se acepta (RN-02).
/// Sin Estimacion (o null) la incidencia queda sin estimar (RF-005 RN-34).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Estimacion opcional (RF-005, RN-34)
/// </remarks>
public sealed record EditarIncidenciaRequest(
    string? Titulo,
    string? Descripcion,
    int? EstadoId,
    int? PrioridadId,
    int? ResponsableId,
    DateOnly? FechaVencimiento,
    int? AgrupacionId,
    int? PadreId,
    decimal? Estimacion = null)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-39).</summary>
    public EditarIncidenciaRequest Normalizar() => this with
    {
        Titulo = TextoEntrada.Normalizar(Titulo),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Datos para cambiar el tipo de una incidencia; el estado es opcional (RN-23).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record CambiarTipoIncidenciaRequest(int? TipoId, int? EstadoId);

/// <summary>
/// Parámetros de query del listado de incidencias (sin defaults aplicados).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record ListarIncidenciasQuery(
    string? Texto = null,
    int[]? TipoId = null,
    int? EstadoId = null,
    int? PrioridadId = null,
    int? ResponsableId = null,
    bool? SinResponsable = null,
    int? AgrupacionId = null,
    bool? SinAgrupacion = null,
    int? Pagina = null,
    int? TamanioPagina = null,
    string? OrdenarPor = null,
    string? Direccion = null)
{
    /// <summary>Devuelve una copia con trim y opciones en minúsculas.</summary>
    public ListarIncidenciasQuery Normalizar() => this with
    {
        Texto = TextoEntrada.Normalizar(Texto),
        OrdenarPor = TextoEntrada.NormalizarMinusculas(OrdenarPor),
        Direccion = TextoEntrada.NormalizarMinusculas(Direccion),
    };
}

/// <summary>
/// Parámetros de query del árbol de incidencias: ámbito opcional (RF-005, C-RF002-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ArbolIncidenciasQuery(int? AgrupacionId = null, bool? SinAgrupacion = null)
{
    /// <summary>Indica si se pidió algún filtro de ámbito.</summary>
    public bool TieneFiltros => AgrupacionId is not null || SinAgrupacion == true;
}
