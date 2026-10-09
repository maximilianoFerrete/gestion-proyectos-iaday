namespace GestionProyectos.Application.Comun;

/// <summary>
/// Página de resultados de un listado (spec 0.2).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record Paginado<T>(IReadOnlyList<T> Items, int Pagina, int TamanioPagina, int Total);
