namespace GestionProyectos.Application.Personas;

/// <summary>
/// Casos de uso del catálogo de personas.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IPersonaService
{
    /// <summary>Lista personas filtrando por texto; por defecto solo activas.</summary>
    Task<IReadOnlyList<PersonaDto>> ListarAsync(string? texto, bool incluirInactivas, CancellationToken ct);

    /// <summary>Obtiene una persona por id (404 si no existe).</summary>
    Task<PersonaDto> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>Crea una persona (RN-01 a RN-05).</summary>
    Task<PersonaDto> CrearAsync(PersonaRequest request, CancellationToken ct);

    /// <summary>Edita una persona (RN-01 a RN-06); no modifica Activo.</summary>
    Task<PersonaDto> EditarAsync(int id, PersonaRequest request, CancellationToken ct);

    /// <summary>Desactiva una persona; idempotente (RN-04, RN-06).</summary>
    Task<PersonaDto> DesactivarAsync(int id, CancellationToken ct);

    /// <summary>Activa una persona; idempotente (RN-04).</summary>
    Task<PersonaDto> ActivarAsync(int id, CancellationToken ct);
}
