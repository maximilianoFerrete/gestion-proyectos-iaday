using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Referencia a una incidencia (padre o hija) dentro del detalle.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Vencida calculado en el servidor (RF-005, RN-36)
/// </remarks>
public sealed record IncidenciaReferenciaDto(
    int Id,
    string Codigo,
    string Titulo,
    TipoReferenciaDto Tipo,
    EstadoIncidenciaReferenciaDto Estado,
    PersonaReferenciaDto? Responsable,
    DateOnly? FechaVencimiento,
    bool Vencida = false);

/// <summary>
/// Referencia mínima al padre en el listado.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record PadreResumenDto(int Id, string Codigo);

/// <summary>
/// Fila del listado paginado de incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Estimacion y Vencida (RF-005, RN-34, RN-36)
/// </remarks>
public sealed record IncidenciaResumenDto(
    int Id,
    string Codigo,
    int Numero,
    string Titulo,
    TipoReferenciaDto Tipo,
    EstadoIncidenciaReferenciaDto Estado,
    PrioridadReferenciaDto Prioridad,
    PersonaReferenciaDto? Responsable,
    DateOnly? FechaVencimiento,
    AgrupacionReferenciaDto? Agrupacion,
    PadreResumenDto? Padre,
    int CantidadHijas,
    DateTime FechaCreacion,
    decimal? Estimacion = null,
    bool Vencida = false);

/// <summary>
/// Nodo plano del árbol de incidencias; el frontend arma la jerarquía con PadreId.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Vencida (RF-005, RN-36)
/// </remarks>
public sealed record NodoArbolIncidenciaDto(
    int Id,
    string Codigo,
    string Titulo,
    int? PadreId,
    TipoReferenciaDto Tipo,
    EstadoIncidenciaReferenciaDto Estado,
    PrioridadReferenciaDto Prioridad,
    PersonaReferenciaDto? Responsable,
    DateOnly? FechaVencimiento,
    AgrupacionReferenciaDto? Agrupacion,
    bool Vencida = false);

/// <summary>
/// Proyecto al que pertenece una incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record ProyectoReferenciaIncidenciaDto(int Id, string Codigo, string Nombre, bool Archivado);

/// <summary>
/// Tipo que puede usarse para crear hijas de una incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record TipoHijaPermitidoDto(int Id, string Nombre);

/// <summary>
/// Detalle de una incidencia con su padre, hijas directas y tipos de hija permitidos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Estimacion y Vencida (RF-005, RN-34, RN-36)
/// </remarks>
public sealed record IncidenciaDto(
    int Id,
    string Codigo,
    int Numero,
    ProyectoReferenciaIncidenciaDto Proyecto,
    string Titulo,
    string Descripcion,
    TipoReferenciaDto Tipo,
    EstadoIncidenciaReferenciaDto Estado,
    PrioridadReferenciaDto Prioridad,
    PersonaReferenciaDto? Responsable,
    DateOnly? FechaVencimiento,
    AgrupacionReferenciaDto? Agrupacion,
    bool AgrupacionHeredada,
    IncidenciaReferenciaDto? Padre,
    IReadOnlyList<IncidenciaReferenciaDto> Hijas,
    IReadOnlyList<TipoHijaPermitidoDto> TiposHijaPermitidos,
    DateTime FechaCreacion,
    DateTime? FechaModificacion,
    decimal? Estimacion = null,
    bool Vencida = false);

/// <summary>
/// Proyecto bloqueado al inicio de una escritura de incidencias (spec 0.3).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record ProyectoBloqueado(int Id, string Codigo, bool Archivado)
{
    /// <summary>Lanza ProyectoArchivadoException si el proyecto está archivado (RN-36).</summary>
    public void AsegurarEditable()
    {
        if (Archivado)
        {
            throw new Domain.Excepciones.ProyectoArchivadoException(Codigo);
        }
    }
}

/// <summary>
/// Número asignado por el contador del proyecto, con los datos del proyecto bloqueado (RN-18).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record NumeroIncidenciaAsignado(int Numero, ProyectoBloqueado Proyecto);
