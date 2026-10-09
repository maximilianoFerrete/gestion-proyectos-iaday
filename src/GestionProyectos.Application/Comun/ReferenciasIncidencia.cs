namespace GestionProyectos.Application.Comun;

/// <summary>
/// Referencia resumida a un tipo de incidencia (con el número de su nivel).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record TipoReferenciaDto(int Id, string Nombre, bool Activo, int NivelNumero);

/// <summary>
/// Referencia a un estado de incidencia; EsFinal según el flujo del tipo de la incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record EstadoIncidenciaReferenciaDto(int Id, string Nombre, bool Activo, bool EsFinal);

/// <summary>
/// Referencia resumida a una prioridad de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record PrioridadReferenciaDto(int Id, string Nombre, int Orden, bool Activo);

/// <summary>
/// Referencia resumida a una agrupación.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record AgrupacionReferenciaDto(int Id, string Nombre);

/// <summary>
/// Referencia a un nivel de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record NivelReferenciaDto(int Id, int Numero, string Nombre);

/// <summary>
/// Referencia a un flujo de trabajo de incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record FlujoReferenciaDto(int Id, string Nombre);
