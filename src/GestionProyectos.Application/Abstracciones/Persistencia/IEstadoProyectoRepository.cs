using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia de estados de proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IEstadoProyectoRepository
{
    /// <summary>Obtiene el estado por id o null si no existe.</summary>
    Task<EstadoProyecto?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene el estado marcado como inicial o null si no hay ninguno.</summary>
    Task<EstadoProyecto?> ObtenerInicialAsync(CancellationToken ct);

    /// <summary>Obtiene el estado de sistema con el código indicado o null.</summary>
    Task<EstadoProyecto?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct);

    /// <summary>Lista estados ordenados por Orden y Nombre.</summary>
    Task<IReadOnlyList<EstadoProyecto>> ListarAsync(bool soloActivos, CancellationToken ct);

    /// <summary>Indica si el nombre ya existe (sin distinguir mayúsculas), excluyendo un id.</summary>
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);

    /// <summary>Cuenta los proyectos que tienen el estado indicado.</summary>
    Task<int> ContarProyectosAsync(int estadoId, CancellationToken ct);

    /// <summary>Cuenta los proyectos agrupados por EstadoId.</summary>
    Task<IReadOnlyDictionary<int, int>> ContarProyectosPorEstadoAsync(CancellationToken ct);

    /// <summary>Inserta el estado y le asigna el id generado.</summary>
    Task AgregarAsync(EstadoProyecto estado, CancellationToken ct);

    /// <summary>Guarda los cambios del estado.</summary>
    Task ActualizarAsync(EstadoProyecto estado, CancellationToken ct);
}
