using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia de personas.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IPersonaRepository
{
    /// <summary>Obtiene la persona por id o null si no existe.</summary>
    Task<Persona?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Lista personas filtrando por texto en nombre o email, ordenadas por nombre.</summary>
    Task<IReadOnlyList<Persona>> ListarAsync(string? texto, bool incluirInactivas, CancellationToken ct);

    /// <summary>Indica si el email ya existe (sin distinguir mayúsculas), excluyendo un id.</summary>
    Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct);

    /// <summary>Cuenta las personas administradoras y activas.</summary>
    Task<int> ContarAdministradoresActivosAsync(CancellationToken ct);

    /// <summary>Inserta la persona y le asigna el id generado.</summary>
    Task AgregarAsync(Persona persona, CancellationToken ct);

    /// <summary>Guarda los cambios de la persona.</summary>
    Task ActualizarAsync(Persona persona, CancellationToken ct);
}
