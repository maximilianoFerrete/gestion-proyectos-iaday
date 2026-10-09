using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Proyectos;

/// <summary>
/// Campos comunes del alta y la edición de proyectos (para validarlos una sola vez).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IDatosProyectoRequest
{
    string? Nombre { get; }

    string? Descripcion { get; }

    int? ResponsableId { get; }

    DateOnly? FechaInicio { get; }

    DateOnly? FechaObjetivo { get; }
}

/// <summary>
/// Datos para crear un proyecto; el estado no se acepta (RN-18).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record CrearProyectoRequest(
    string? Nombre,
    string? Descripcion,
    int? ResponsableId,
    DateOnly? FechaInicio,
    DateOnly? FechaObjetivo) : IDatosProyectoRequest
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-31).</summary>
    public CrearProyectoRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Datos para editar un proyecto, incluido el estado (RN-19).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record EditarProyectoRequest(
    string? Nombre,
    string? Descripcion,
    int? ResponsableId,
    DateOnly? FechaInicio,
    DateOnly? FechaObjetivo,
    int? EstadoId) : IDatosProyectoRequest
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-31).</summary>
    public EditarProyectoRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Parámetros de query del listado de proyectos (sin defaults aplicados).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record ListarProyectosQuery(
    string? Texto = null,
    int? EstadoId = null,
    int? ResponsableId = null,
    string? Archivados = null,
    int? Pagina = null,
    int? TamanioPagina = null,
    string? OrdenarPor = null,
    string? Direccion = null)
{
    /// <summary>Devuelve una copia con trim y opciones en minúsculas.</summary>
    public ListarProyectosQuery Normalizar() => this with
    {
        Texto = TextoEntrada.Normalizar(Texto),
        Archivados = TextoEntrada.NormalizarMinusculas(Archivados),
        OrdenarPor = TextoEntrada.NormalizarMinusculas(OrdenarPor),
        Direccion = TextoEntrada.NormalizarMinusculas(Direccion),
    };
}
