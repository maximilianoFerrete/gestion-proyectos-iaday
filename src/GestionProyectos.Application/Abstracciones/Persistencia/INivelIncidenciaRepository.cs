using GestionProyectos.Application.NivelesIncidencia;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia y consultas de niveles de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface INivelIncidenciaRepository
{
    /// <summary>Obtiene el nivel por id o null si no existe.</summary>
    Task<NivelIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene el DTO del nivel (con cantidad de tipos) o null.</summary>
    Task<NivelIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct);

    /// <summary>Lista los niveles ordenados por número.</summary>
    Task<IReadOnlyList<NivelIncidenciaDto>> ListarAsync(CancellationToken ct);

    /// <summary>Indica si ya existe un nivel con ese número.</summary>
    Task<bool> ExisteNumeroAsync(int numero, CancellationToken ct);

    /// <summary>Indica si el nombre ya existe (sin distinguir mayúsculas), excluyendo un id.</summary>
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);

    /// <summary>Inserta el nivel y le asigna el id generado.</summary>
    Task AgregarAsync(NivelIncidencia nivel, CancellationToken ct);

    /// <summary>Guarda nombre, descripción y fecha de modificación.</summary>
    Task ActualizarAsync(NivelIncidencia nivel, CancellationToken ct);
}
