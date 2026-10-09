namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Datos editables de un proyecto (objeto de valor inmutable).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record DatosProyecto(
    string Nombre,
    string Descripcion,
    int ResponsableId,
    DateOnly FechaInicio,
    DateOnly FechaObjetivo);

/// <summary>
/// Datos del archivado de un proyecto (objeto de valor inmutable).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record DatosArchivado(DateTime Fecha, int ArchivadoPorId, int EstadoAlArchivarId);
