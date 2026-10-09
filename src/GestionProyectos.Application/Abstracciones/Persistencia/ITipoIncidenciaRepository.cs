using GestionProyectos.Application.TiposIncidencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia y consultas de tipos de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface ITipoIncidenciaRepository
{
    /// <summary>Obtiene el tipo por id o null si no existe.</summary>
    Task<TipoIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene el DTO del tipo (nivel, flujo y cantidad de incidencias) o null.</summary>
    Task<TipoIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct);

    /// <summary>Lista tipos ordenados por número de nivel y nombre.</summary>
    Task<IReadOnlyList<TipoIncidenciaDto>> ListarAsync(bool soloActivos, int? nivelId, CancellationToken ct);

    /// <summary>Indica si el nombre ya existe (sin distinguir mayúsculas), excluyendo un id.</summary>
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);

    /// <summary>Cuenta las incidencias del tipo agrupadas por estado (RN-12).</summary>
    Task<IReadOnlyList<CantidadPorEstado>> ContarIncidenciasPorEstadoAsync(int tipoId, CancellationToken ct);

    /// <summary>Inserta el tipo y le asigna el id generado.</summary>
    Task AgregarAsync(TipoIncidencia tipo, CancellationToken ct);

    /// <summary>Guarda nombre, descripción, flujo, activo y fecha de modificación.</summary>
    Task ActualizarAsync(TipoIncidencia tipo, CancellationToken ct);
}
