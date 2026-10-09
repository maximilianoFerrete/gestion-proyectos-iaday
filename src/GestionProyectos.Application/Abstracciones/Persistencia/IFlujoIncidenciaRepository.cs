using GestionProyectos.Application.FlujosIncidencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia y consultas de flujos de trabajo y su composición de estados.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IFlujoIncidenciaRepository
{
    /// <summary>Obtiene el flujo con sus estados o null si no existe.</summary>
    Task<FlujoIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene el DTO del flujo (estados y tipos) o null.</summary>
    Task<FlujoIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct);

    /// <summary>Lista los flujos ordenados por nombre.</summary>
    Task<IReadOnlyList<FlujoIncidenciaDto>> ListarAsync(CancellationToken ct);

    /// <summary>Indica si el nombre ya existe (sin distinguir mayúsculas), excluyendo un id.</summary>
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);

    /// <summary>Cuenta por estado las incidencias de tipos que usan el flujo (RN-15).</summary>
    Task<IReadOnlyList<CantidadPorEstado>> ContarIncidenciasPorEstadoAsync(int flujoId, CancellationToken ct);

    /// <summary>Inserta el flujo y su composición (en transacción); asigna el id generado.</summary>
    Task AgregarAsync(FlujoIncidencia flujo, CancellationToken ct);

    /// <summary>Guarda datos y reemplaza la composición (llamar dentro de una transacción).</summary>
    Task ActualizarAsync(FlujoIncidencia flujo, CancellationToken ct);
}
