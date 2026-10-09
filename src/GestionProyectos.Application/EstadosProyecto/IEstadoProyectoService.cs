namespace GestionProyectos.Application.EstadosProyecto;

/// <summary>
/// Casos de uso de la configuración de estados de proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IEstadoProyectoService
{
    /// <summary>Lista los estados ordenados por Orden y Nombre.</summary>
    Task<IReadOnlyList<EstadoProyectoDto>> ListarAsync(bool soloActivos, CancellationToken ct);

    /// <summary>Obtiene un estado por id (404 si no existe).</summary>
    Task<EstadoProyectoDto> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>Crea un estado configurable (RN-07).</summary>
    Task<EstadoProyectoDto> CrearAsync(EstadoProyectoRequest request, CancellationToken ct);

    /// <summary>Edita un estado configurable (RN-07, RN-09, RN-12).</summary>
    Task<EstadoProyectoDto> EditarAsync(int id, EstadoProyectoRequest request, CancellationToken ct);

    /// <summary>Marca el estado como inicial y desmarca el anterior (RN-10, RN-11).</summary>
    Task<EstadoProyectoDto> MarcarInicialAsync(int id, CancellationToken ct);

    /// <summary>Desactiva el estado; idempotente (RN-09, RN-12, RN-13).</summary>
    Task<EstadoProyectoDto> DesactivarAsync(int id, CancellationToken ct);

    /// <summary>Activa el estado; idempotente (RN-09).</summary>
    Task<EstadoProyectoDto> ActivarAsync(int id, CancellationToken ct);
}
