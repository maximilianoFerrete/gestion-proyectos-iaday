namespace GestionProyectos.Application.Agrupaciones;

/// <summary>
/// Casos de uso de agrupaciones dentro de un proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - La eliminación desasocia las incidencias (RF-002, RN-30)
/// </remarks>
public interface IAgrupacionService
{
    /// <summary>Lista las agrupaciones del proyecto, también si está archivado.</summary>
    Task<IReadOnlyList<AgrupacionDto>> ListarAsync(int proyectoId, CancellationToken ct);

    /// <summary>Obtiene una agrupación del proyecto (404 si no existe o es de otro proyecto).</summary>
    Task<AgrupacionDto> ObtenerAsync(int proyectoId, int id, CancellationToken ct);

    /// <summary>Crea una agrupación en un proyecto no archivado (RN-26, RN-27).</summary>
    Task<AgrupacionDto> CrearAsync(int proyectoId, AgrupacionRequest request, CancellationToken ct);

    /// <summary>Edita una agrupación de un proyecto no archivado.</summary>
    Task<AgrupacionDto> EditarAsync(int proyectoId, int id, AgrupacionRequest request, CancellationToken ct);

    /// <summary>Desasocia sus incidencias y elimina la agrupación en una transacción (RN-30).</summary>
    Task EliminarAsync(int proyectoId, int id, CancellationToken ct);
}
