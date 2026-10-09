namespace GestionProyectos.Application.Comun;

/// <summary>
/// Referencia resumida a una persona dentro de otro DTO.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record PersonaReferenciaDto(int Id, string Nombre, bool Activo);

/// <summary>
/// Referencia resumida a un estado de proyecto dentro de otro DTO.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record EstadoReferenciaDto(int Id, string Nombre, bool EsFinal, bool Activo, bool EsSistema);
