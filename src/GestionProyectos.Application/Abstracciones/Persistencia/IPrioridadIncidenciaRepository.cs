using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia de prioridades de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IPrioridadIncidenciaRepository
{
    /// <summary>Obtiene la prioridad por id o null si no existe.</summary>
    Task<PrioridadIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene la prioridad predeterminada o null si no hay ninguna.</summary>
    Task<PrioridadIncidencia?> ObtenerPredeterminadaAsync(CancellationToken ct);

    /// <summary>Lista prioridades ordenadas por orden y nombre.</summary>
    Task<IReadOnlyList<PrioridadIncidencia>> ListarAsync(bool soloActivas, CancellationToken ct);

    /// <summary>Indica si el nombre ya existe (sin distinguir mayúsculas), excluyendo un id.</summary>
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);

    /// <summary>Cuenta las incidencias que tienen la prioridad indicada.</summary>
    Task<int> ContarIncidenciasAsync(int prioridadId, CancellationToken ct);

    /// <summary>Cuenta las incidencias agrupadas por PrioridadId.</summary>
    Task<IReadOnlyDictionary<int, int>> ContarIncidenciasPorPrioridadAsync(CancellationToken ct);

    /// <summary>Inserta la prioridad y le asigna el id generado.</summary>
    Task AgregarAsync(PrioridadIncidencia prioridad, CancellationToken ct);

    /// <summary>Guarda los cambios de la prioridad.</summary>
    Task ActualizarAsync(PrioridadIncidencia prioridad, CancellationToken ct);
}
