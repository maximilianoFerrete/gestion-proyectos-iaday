using GestionProyectos.Application.EstadosIncidencia;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia y consultas del catálogo de estados de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Flujos donde el estado es inicial o final (RF-005, RN-18)
/// </remarks>
public interface IEstadoIncidenciaRepository
{
    /// <summary>Obtiene el estado por id o null si no existe.</summary>
    Task<EstadoIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene los estados con los ids indicados (los inexistentes se omiten).</summary>
    Task<IReadOnlyList<EstadoIncidencia>> ListarPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    /// <summary>Obtiene el DTO del estado (incidencias y flujos) o null.</summary>
    Task<EstadoIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct);

    /// <summary>Lista estados ordenados por orden y nombre.</summary>
    Task<IReadOnlyList<EstadoIncidenciaDto>> ListarAsync(bool soloActivos, CancellationToken ct);

    /// <summary>Indica si el nombre ya existe (sin distinguir mayúsculas), excluyendo un id.</summary>
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);

    /// <summary>Nombres de los flujos donde el estado es el inicial (RN-11).</summary>
    Task<IReadOnlyList<string>> ListarFlujosDondeEsInicialAsync(int estadoId, CancellationToken ct);

    /// <summary>Nombres de los flujos donde el estado es inicial o final (RF-005 RN-18).</summary>
    Task<IReadOnlyList<string>> ListarFlujosDondeEsInicialOFinalAsync(int estadoId, CancellationToken ct);

    /// <summary>Inserta el estado y le asigna el id generado.</summary>
    Task AgregarAsync(EstadoIncidencia estado, CancellationToken ct);

    /// <summary>Guarda los cambios del estado.</summary>
    Task ActualizarAsync(EstadoIncidencia estado, CancellationToken ct);
}
