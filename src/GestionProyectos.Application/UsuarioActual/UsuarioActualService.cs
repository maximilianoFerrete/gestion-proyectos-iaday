using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Application.UsuarioActual;

/// <summary>
/// Resuelve los datos del usuario actual a partir de IUsuarioActual y del catálogo de personas.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class UsuarioActualService(IUsuarioActual usuarioActual, IPersonaRepository personas)
    : IUsuarioActualService
{
    /// <inheritdoc />
    public async Task<UsuarioActualDto> ObtenerAsync(CancellationToken ct)
    {
        if (!usuarioActual.EstaIdentificado || usuarioActual.PersonaId is null)
        {
            return UsuarioActualDto.Anonimo;
        }

        var persona = await personas.ObtenerPorIdAsync(usuarioActual.PersonaId.Value, ct);
        if (persona is null || !persona.Activo)
        {
            throw new UsuarioInvalidoException();
        }

        return new UsuarioActualDto(true, persona.Id, persona.Nombre, persona.Email, persona.EsAdmin);
    }
}

/// <summary>
/// Busca personas activas para construir la identidad del usuario actual (spec 0.4).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class IdentificacionUsuarioService(IPersonaRepository personas) : IIdentificacionUsuarioService
{
    /// <inheritdoc />
    public async Task<IdentidadUsuario?> BuscarActivaAsync(int personaId, CancellationToken ct)
    {
        var persona = await personas.ObtenerPorIdAsync(personaId, ct);
        return persona is { Activo: true }
            ? new IdentidadUsuario(persona.Id, persona.Nombre, persona.Email, persona.EsAdmin)
            : null;
    }
}
