using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Proyectos;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Ámbito de incidencias dentro de un proyecto: todo, una agrupación o sin agrupación (RN-01).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record AmbitoIncidencias(int? AgrupacionId, bool SinAgrupacion)
{
    /// <summary>Todas las incidencias del proyecto.</summary>
    public static AmbitoIncidencias Proyecto { get; } = new(null, false);

    /// <summary>Nombre del ámbito en el contrato: proyecto, agrupacion o sinAgrupacion.</summary>
    public string Nombre => AgrupacionId is not null
        ? OpcionesSeguimiento.AmbitoAgrupacion
        : SinAgrupacion ? OpcionesSeguimiento.AmbitoSinAgrupacion : OpcionesSeguimiento.AmbitoProyecto;
}

/// <summary>
/// Criterios de proyectos del tablero general (texto sobre código o nombre y archivados).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record FiltroProyectosSeguimiento(string? Texto, FiltroArchivados Archivados);

/// <summary>
/// Clave de agrupación de los conteos crudos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public enum AgrupacionConteo
{
    Ninguna,
    Proyecto,
    Agrupacion,
}

/// <summary>
/// Filtro de los conteos crudos: un proyecto (con ámbito) o los proyectos del tablero general.
/// TipoIds null = todos los tipos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, 0.2)
/// </remarks>
public sealed record FiltroConteoIncidencias(
    int? ProyectoId,
    FiltroProyectosSeguimiento? Proyectos,
    AmbitoIncidencias Ambito,
    IReadOnlyList<int>? TipoIds,
    DateOnly FechaReferencia)
{
    /// <summary>Conteo de un proyecto en el ámbito indicado.</summary>
    public static FiltroConteoIncidencias DeProyecto(int proyectoId, AmbitoIncidencias ambito,
        IReadOnlyList<int>? tipoIds, DateOnly fechaReferencia) =>
        new(proyectoId, null, ambito, tipoIds, fechaReferencia);

    /// <summary>Conteo de todos los proyectos que cumplen el filtro del tablero general.</summary>
    public static FiltroConteoIncidencias DeProyectos(FiltroProyectosSeguimiento proyectos,
        IReadOnlyList<int>? tipoIds, DateOnly fechaReferencia) =>
        new(null, proyectos, AmbitoIncidencias.Proyecto, tipoIds, fechaReferencia);
}

/// <summary>
/// Conteo crudo por (grupo, estado, EsFinal), sin aplicar la regla "solo abiertas" (0.2).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ConteoIncidencias(
    int? Grupo,
    int EstadoId,
    bool EsFinal,
    int Cantidad,
    int ConVencimientoAnterior,
    int SinResponsable);

/// <summary>
/// Marca final de un estado en un flujo usado por los tipos contabilizados (RN-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record EstadoEnFlujoSeguimiento(int FlujoId, int EstadoId, bool EsFinal);

/// <summary>
/// Proyecto del tablero general con su cantidad de agrupaciones.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ProyectoSeguimientoLeido(ProyectoSeguimientoDto Proyecto, int CantidadAgrupaciones);

/// <summary>
/// Filtro de la grilla del tablero: ámbito, tipos efectivos (null = todos) y filtros de RN-21.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record FiltroTableroIncidencias(
    int ProyectoId,
    AmbitoIncidencias Ambito,
    IReadOnlyList<int>? TipoIds,
    IndicadorSeguimiento? Indicador,
    int? EstadoId,
    string? Texto,
    DateOnly FechaReferencia);

/// <summary>
/// Incidencia leída para la grilla del tablero (Vencida lo calcula Application).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record IncidenciaTableroLeida(
    int Id,
    string Codigo,
    int Numero,
    string Titulo,
    TipoReferenciaDto Tipo,
    EstadoTableroDto Estado,
    decimal? Estimacion,
    DateOnly? FechaVencimiento,
    PersonaReferenciaDto? Responsable);
