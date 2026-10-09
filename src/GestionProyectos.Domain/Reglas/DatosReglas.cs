namespace GestionProyectos.Domain.Reglas;

/// <summary>
/// Hija directa de una incidencia con los datos que usan las reglas de jerarquía (RN-25, RN-32).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record HijaIncidencia(int Id, string Codigo, int TipoId, DateOnly? FechaVencimiento);

/// <summary>
/// Cantidad de incidencias en un estado (para RN-12 y RN-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record CantidadPorEstado(int EstadoId, string EstadoNombre, int Cantidad);
