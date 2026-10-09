using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.PrioridadesIncidencia;

/// <summary>
/// Casos de uso de prioridades de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IPrioridadIncidenciaService
{
    /// <summary>Lista prioridades ordenadas por orden y nombre.</summary>
    Task<IReadOnlyList<PrioridadIncidenciaDto>> ListarAsync(bool soloActivas, CancellationToken ct);

    /// <summary>Obtiene una prioridad por id (404 si no existe).</summary>
    Task<PrioridadIncidenciaDto> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>Crea una prioridad activa y no predeterminada (RN-01, RN-17).</summary>
    Task<PrioridadIncidenciaDto> CrearAsync(PrioridadIncidenciaRequest request, CancellationToken ct);

    /// <summary>Edita nombre, descripción y orden (RN-01, RN-17).</summary>
    Task<PrioridadIncidenciaDto> EditarAsync(int id, PrioridadIncidenciaRequest request, CancellationToken ct);

    /// <summary>Marca la predeterminada y desmarca la anterior en transacción (RN-17).</summary>
    Task<PrioridadIncidenciaDto> MarcarPredeterminadaAsync(int id, CancellationToken ct);

    /// <summary>Desactiva si no es la predeterminada; idempotente (RN-17).</summary>
    Task<PrioridadIncidenciaDto> DesactivarAsync(int id, CancellationToken ct);

    /// <summary>Activa la prioridad; idempotente (RN-17).</summary>
    Task<PrioridadIncidenciaDto> ActivarAsync(int id, CancellationToken ct);
}

/// <summary>
/// Implementa la configuración de prioridades de incidencia (RN-01, RN-17).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class PrioridadIncidenciaService(
    IPrioridadIncidenciaRepository repositorio,
    IUnitOfWork unitOfWork,
    IUsuarioActual usuarioActual,
    IValidator<PrioridadIncidenciaRequest> validador,
    TimeProvider reloj,
    ILogger<PrioridadIncidenciaService> logger) : IPrioridadIncidenciaService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PrioridadIncidenciaDto>> ListarAsync(bool soloActivas, CancellationToken ct)
    {
        var prioridades = await repositorio.ListarAsync(soloActivas, ct);
        var cantidades = await repositorio.ContarIncidenciasPorPrioridadAsync(ct);
        return prioridades
            .Select(p => PrioridadIncidenciaDto.Desde(p, cantidades.GetValueOrDefault(p.Id)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<PrioridadIncidenciaDto> ObtenerAsync(int id, CancellationToken ct) =>
        await ADtoAsync(await ObtenerEntidadAsync(id, ct), ct);

    /// <inheritdoc />
    public async Task<PrioridadIncidenciaDto> CrearAsync(PrioridadIncidenciaRequest request, CancellationToken ct)
    {
        var datos = await PrepararAsync(request, ct);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, null, ct);
        var prioridad = new PrioridadIncidencia(datos.Nombre!, datos.Descripcion, datos.Orden!.Value,
            reloj.AhoraUtc());
        await repositorio.AgregarAsync(prioridad, ct);
        logger.LogInformation("Prioridad de incidencia {PrioridadId} creada", prioridad.Id);
        return PrioridadIncidenciaDto.Desde(prioridad, 0);
    }

    /// <inheritdoc />
    public async Task<PrioridadIncidenciaDto> EditarAsync(int id, PrioridadIncidenciaRequest request,
        CancellationToken ct)
    {
        var datos = await PrepararAsync(request, ct);
        var prioridad = await ObtenerEntidadAsync(id, ct);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, id, ct);
        prioridad.Actualizar(datos.Nombre!, datos.Descripcion, datos.Orden!.Value, reloj.AhoraUtc());
        await repositorio.ActualizarAsync(prioridad, ct);
        logger.LogInformation("Prioridad de incidencia {PrioridadId} actualizada", prioridad.Id);
        return await ADtoAsync(prioridad, ct);
    }

    /// <inheritdoc />
    public async Task<PrioridadIncidenciaDto> MarcarPredeterminadaAsync(int id, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        var prioridad = await ObtenerEntidadAsync(id, ct);
        if (prioridad.EsPredeterminada)
        {
            return await ADtoAsync(prioridad, ct);
        }

        prioridad.AsegurarElegibleComoPredeterminada();
        var ahora = reloj.AhoraUtc();
        await unitOfWork.EjecutarEnTransaccionAsync(async () =>
        {
            var anterior = await repositorio.ObtenerPredeterminadaAsync(ct);
            if (anterior is not null)
            {
                anterior.DesmarcarPredeterminada(ahora);
                await repositorio.ActualizarAsync(anterior, ct);
            }

            prioridad.MarcarPredeterminada(ahora);
            await repositorio.ActualizarAsync(prioridad, ct);
        }, ct);

        logger.LogInformation("Prioridad de incidencia {PrioridadId} marcada como predeterminada", prioridad.Id);
        return await ADtoAsync(prioridad, ct);
    }

    /// <inheritdoc />
    public async Task<PrioridadIncidenciaDto> DesactivarAsync(int id, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        var prioridad = await ObtenerEntidadAsync(id, ct);
        if (prioridad.Activo)
        {
            prioridad.Desactivar(reloj.AhoraUtc());
            await repositorio.ActualizarAsync(prioridad, ct);
            logger.LogInformation("Prioridad de incidencia {PrioridadId} desactivada", prioridad.Id);
        }

        return await ADtoAsync(prioridad, ct);
    }

    /// <inheritdoc />
    public async Task<PrioridadIncidenciaDto> ActivarAsync(int id, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        var prioridad = await ObtenerEntidadAsync(id, ct);
        if (!prioridad.Activo)
        {
            prioridad.Activar(reloj.AhoraUtc());
            await repositorio.ActualizarAsync(prioridad, ct);
            logger.LogInformation("Prioridad de incidencia {PrioridadId} activada", prioridad.Id);
        }

        return await ADtoAsync(prioridad, ct);
    }

    private async Task<PrioridadIncidenciaRequest> PrepararAsync(PrioridadIncidenciaRequest request,
        CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validador.ValidateAndThrowAsync(datos, ct);
        return datos;
    }

    private async Task<PrioridadIncidencia> ObtenerEntidadAsync(int id, CancellationToken ct) =>
        await repositorio.ObtenerPorIdAsync(id, ct)
        ?? throw new EntidadNoEncontradaException(CodigosError.PrioridadNoEncontrada,
            $"No existe la prioridad de incidencia con id {id}.");

    private async Task AsegurarNombreDisponibleAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        if (await repositorio.ExisteNombreAsync(nombre, excluirId, ct))
        {
            throw new ConflictoException(CodigosError.PrioridadNombreDuplicado,
                $"Ya existe una prioridad con el nombre '{nombre}'.");
        }
    }

    private async Task<PrioridadIncidenciaDto> ADtoAsync(PrioridadIncidencia prioridad, CancellationToken ct) =>
        PrioridadIncidenciaDto.Desde(prioridad, await repositorio.ContarIncidenciasAsync(prioridad.Id, ct));
}
