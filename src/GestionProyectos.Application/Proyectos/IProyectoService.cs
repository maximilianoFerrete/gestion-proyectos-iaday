using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Proyectos;

/// <summary>
/// Casos de uso de proyectos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IProyectoService
{
    /// <summary>Lista proyectos con filtros, orden y paginación (RN-24).</summary>
    Task<Paginado<ProyectoResumenDto>> ListarAsync(ListarProyectosQuery query, CancellationToken ct);

    /// <summary>Obtiene el detalle de un proyecto (404 si no existe).</summary>
    Task<ProyectoDto> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>Crea un proyecto en el estado inicial con código generado (RN-14 a RN-18).</summary>
    Task<ProyectoDto> CrearAsync(CrearProyectoRequest request, CancellationToken ct);

    /// <summary>Edita un proyecto no archivado (RN-15 a RN-19, RN-22).</summary>
    Task<ProyectoDto> EditarAsync(int id, EditarProyectoRequest request, CancellationToken ct);

    /// <summary>Archiva el proyecto; solo administradores (RN-20, RN-21).</summary>
    Task<ProyectoDto> ArchivarAsync(int id, CancellationToken ct);
}
