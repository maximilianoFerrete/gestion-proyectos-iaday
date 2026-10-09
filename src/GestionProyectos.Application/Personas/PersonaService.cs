using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Personas;

/// <summary>
/// Implementa el ABM mínimo de personas con las reglas de administradores (RN-01 a RN-06).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class PersonaService(
    IPersonaRepository repositorio,
    IUsuarioActual usuarioActual,
    IValidator<PersonaRequest> validador,
    TimeProvider reloj,
    ILogger<PersonaService> logger) : IPersonaService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonaDto>> ListarAsync(string? texto, bool incluirInactivas,
        CancellationToken ct)
    {
        var personas = await repositorio.ListarAsync(TextoEntrada.Normalizar(texto), incluirInactivas, ct);
        return personas.Select(PersonaDto.Desde).ToList();
    }

    /// <inheritdoc />
    public async Task<PersonaDto> ObtenerAsync(int id, CancellationToken ct) =>
        PersonaDto.Desde(await ObtenerEntidadAsync(id, ct));

    /// <inheritdoc />
    public async Task<PersonaDto> CrearAsync(PersonaRequest request, CancellationToken ct)
    {
        var datos = await NormalizarYValidarAsync(request, ct);
        if (datos.EsAdmin)
        {
            usuarioActual.AsegurarAdministrador();
        }

        await AsegurarEmailDisponibleAsync(datos.Email!, null, ct);

        var persona = new Persona(datos.Nombre!, datos.Email!, datos.EsAdmin, reloj.AhoraUtc());
        await repositorio.AgregarAsync(persona, ct);
        logger.LogInformation("Persona {PersonaId} creada (admin: {EsAdmin})", persona.Id, persona.EsAdmin);
        return PersonaDto.Desde(persona);
    }

    /// <inheritdoc />
    public async Task<PersonaDto> EditarAsync(int id, PersonaRequest request, CancellationToken ct)
    {
        var datos = await NormalizarYValidarAsync(request, ct);
        var persona = await ObtenerEntidadAsync(id, ct);
        if (datos.EsAdmin != persona.EsAdmin)
        {
            usuarioActual.AsegurarAdministrador();
        }

        await AsegurarEmailDisponibleAsync(datos.Email!, id, ct);
        if (persona.EsAdminActivo && !datos.EsAdmin)
        {
            await AsegurarNoEsUltimoAdministradorAsync(ct);
        }

        persona.Actualizar(datos.Nombre!, datos.Email!, datos.EsAdmin, reloj.AhoraUtc());
        await repositorio.ActualizarAsync(persona, ct);
        logger.LogInformation("Persona {PersonaId} actualizada", persona.Id);
        return PersonaDto.Desde(persona);
    }

    /// <inheritdoc />
    public async Task<PersonaDto> DesactivarAsync(int id, CancellationToken ct)
    {
        var persona = await ObtenerEntidadAsync(id, ct);
        if (!persona.Activo)
        {
            return PersonaDto.Desde(persona);
        }

        if (persona.EsAdmin)
        {
            await AsegurarNoEsUltimoAdministradorAsync(ct);
        }

        persona.Desactivar(reloj.AhoraUtc());
        await repositorio.ActualizarAsync(persona, ct);
        logger.LogInformation("Persona {PersonaId} desactivada", persona.Id);
        return PersonaDto.Desde(persona);
    }

    /// <inheritdoc />
    public async Task<PersonaDto> ActivarAsync(int id, CancellationToken ct)
    {
        var persona = await ObtenerEntidadAsync(id, ct);
        if (persona.Activo)
        {
            return PersonaDto.Desde(persona);
        }

        persona.Activar(reloj.AhoraUtc());
        await repositorio.ActualizarAsync(persona, ct);
        logger.LogInformation("Persona {PersonaId} activada", persona.Id);
        return PersonaDto.Desde(persona);
    }

    private async Task<PersonaRequest> NormalizarYValidarAsync(PersonaRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validador.ValidateAndThrowAsync(datos, ct);
        return datos;
    }

    private async Task<Persona> ObtenerEntidadAsync(int id, CancellationToken ct) =>
        await repositorio.ObtenerPorIdAsync(id, ct)
        ?? throw new EntidadNoEncontradaException(CodigosError.PersonaNoEncontrada,
            $"No existe la persona con id {id}.");

    private async Task AsegurarEmailDisponibleAsync(string email, int? excluirId, CancellationToken ct)
    {
        if (await repositorio.ExisteEmailAsync(email, excluirId, ct))
        {
            throw new ConflictoException(CodigosError.PersonaEmailDuplicado,
                $"Ya existe una persona con el email {email}.");
        }
    }

    private async Task AsegurarNoEsUltimoAdministradorAsync(CancellationToken ct)
    {
        if (await repositorio.ContarAdministradoresActivosAsync(ct) <= 1)
        {
            throw new ConflictoException(CodigosError.PersonaUltimoAdmin,
                "Debe existir al menos un administrador activo.");
        }
    }
}
