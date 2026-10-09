using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia de agrupaciones.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega conteos de incidencias por agrupación (RF-002)
/// </remarks>
public interface IAgrupacionRepository
{
    /// <summary>Obtiene la agrupación por id o null si no existe.</summary>
    Task<Agrupacion?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Lista las agrupaciones del proyecto ordenadas por nombre.</summary>
    Task<IReadOnlyList<Agrupacion>> ListarPorProyectoAsync(int proyectoId, CancellationToken ct);

    /// <summary>Indica si el nombre ya existe en el proyecto (sin mayúsculas), excluyendo un id.</summary>
    Task<bool> ExisteNombreAsync(int proyectoId, string nombre, int? excluirId, CancellationToken ct);

    /// <summary>Cuenta las incidencias asociadas a la agrupación.</summary>
    Task<int> ContarIncidenciasAsync(int id, CancellationToken ct);

    /// <summary>Cuenta las incidencias del proyecto agrupadas por AgrupacionId.</summary>
    Task<IReadOnlyDictionary<int, int>> ContarIncidenciasPorAgrupacionAsync(int proyectoId, CancellationToken ct);

    /// <summary>Inserta la agrupación y le asigna el id generado.</summary>
    Task AgregarAsync(Agrupacion agrupacion, CancellationToken ct);

    /// <summary>Guarda los cambios de la agrupación.</summary>
    Task ActualizarAsync(Agrupacion agrupacion, CancellationToken ct);

    /// <summary>Elimina físicamente la agrupación (RN-30).</summary>
    Task EliminarAsync(int id, CancellationToken ct);
}
