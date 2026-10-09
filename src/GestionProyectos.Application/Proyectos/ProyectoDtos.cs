using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Proyectos;

/// <summary>
/// Fila del listado de proyectos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record ProyectoResumenDto(
    int Id,
    string Codigo,
    string Nombre,
    PersonaReferenciaDto Responsable,
    DateOnly FechaInicio,
    DateOnly FechaObjetivo,
    EstadoReferenciaDto Estado,
    bool Archivado);

/// <summary>
/// Detalle de un proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record ProyectoDto(
    int Id,
    string Codigo,
    string Nombre,
    string Descripcion,
    PersonaReferenciaDto Responsable,
    DateOnly FechaInicio,
    DateOnly FechaObjetivo,
    EstadoReferenciaDto Estado,
    bool Archivado,
    DateTime? FechaArchivado,
    PersonaReferenciaDto? ArchivadoPor,
    EstadoReferenciaDto? EstadoAlArchivar,
    int CantidadAgrupaciones,
    DateTime FechaCreacion,
    DateTime? FechaModificacion);
