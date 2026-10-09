using FluentValidation;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.EstadosProyecto;

/// <summary>
/// Implementa la configuración de estados de proyecto (RN-07 a RN-13).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class EstadoProyectoService(
    IEstadoProyectoRepository repositorio,
    IUnitOfWork unitOfWork,
    IValidator<EstadoProyectoRequest> validador,
    TimeProvider reloj,
    ILogger<EstadoProyectoService> logger) : IEstadoProyectoService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EstadoProyectoDto>> ListarAsync(bool soloActivos, CancellationToken ct)
    {
        var estados = await repositorio.ListarAsync(soloActivos, ct);
        var cantidades = await repositorio.ContarProyectosPorEstadoAsync(ct);
        return estados
            .Select(e => EstadoProyectoDto.Desde(e, cantidades.GetValueOrDefault(e.Id)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<EstadoProyectoDto> ObtenerAsync(int id, CancellationToken ct) =>
        await ADtoAsync(await ObtenerEntidadAsync(id, ct), ct);

    /// <inheritdoc />
    public async Task<EstadoProyectoDto> CrearAsync(EstadoProyectoRequest request, CancellationToken ct)
    {
        var datos = await NormalizarYValidarAsync(request, ct);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, null, ct);

        var estado = new EstadoProyecto(datos.Nombre!, datos.Descripcion, datos.Orden!.Value,
            datos.EsFinal, reloj.AhoraUtc());
        await repositorio.AgregarAsync(estado, ct);
        logger.LogInformation("Estado de proyecto {EstadoId} creado", estado.Id);
        return EstadoProyectoDto.Desde(estado, 0);
    }

    /// <inheritdoc />
    public async Task<EstadoProyectoDto> EditarAsync(int id, EstadoProyectoRequest request,
        CancellationToken ct)
    {
        var datos = await NormalizarYValidarAsync(request, ct);
        var estado = await ObtenerEntidadAsync(id, ct);
        if (!estado.EsSistema)
        {
            await AsegurarNombreDisponibleAsync(datos.Nombre!, id, ct);
        }

        estado.Actualizar(datos.Nombre!, datos.Descripcion, datos.Orden!.Value, datos.EsFinal,
            reloj.AhoraUtc());
        await repositorio.ActualizarAsync(estado, ct);
        logger.LogInformation("Estado de proyecto {EstadoId} actualizado", estado.Id);
        return await ADtoAsync(estado, ct);
    }

    /// <inheritdoc />
    public async Task<EstadoProyectoDto> MarcarInicialAsync(int id, CancellationToken ct)
    {
        var estado = await ObtenerEntidadAsync(id, ct);
        if (estado.EsInicial)
        {
            return await ADtoAsync(estado, ct);
        }

        estado.AsegurarElegibleComoInicial();
        var ahora = reloj.AhoraUtc();
        await unitOfWork.EjecutarEnTransaccionAsync(async () =>
        {
            var anterior = await repositorio.ObtenerInicialAsync(ct);
            if (anterior is not null)
            {
                anterior.DesmarcarInicial(ahora);
                await repositorio.ActualizarAsync(anterior, ct);
            }

            estado.MarcarInicial(ahora);
            await repositorio.ActualizarAsync(estado, ct);
        }, ct);

        logger.LogInformation("Estado de proyecto {EstadoId} marcado como inicial", estado.Id);
        return await ADtoAsync(estado, ct);
    }

    /// <inheritdoc />
    public async Task<EstadoProyectoDto> DesactivarAsync(int id, CancellationToken ct)
    {
        var estado = await ObtenerEntidadAsync(id, ct);
        var estabaActivo = estado.Activo;
        estado.Desactivar(reloj.AhoraUtc());
        if (estabaActivo)
        {
            await repositorio.ActualizarAsync(estado, ct);
            logger.LogInformation("Estado de proyecto {EstadoId} desactivado", estado.Id);
        }

        return await ADtoAsync(estado, ct);
    }

    /// <inheritdoc />
    public async Task<EstadoProyectoDto> ActivarAsync(int id, CancellationToken ct)
    {
        var estado = await ObtenerEntidadAsync(id, ct);
        var estabaActivo = estado.Activo;
        estado.Activar(reloj.AhoraUtc());
        if (!estabaActivo)
        {
            await repositorio.ActualizarAsync(estado, ct);
            logger.LogInformation("Estado de proyecto {EstadoId} activado", estado.Id);
        }

        return await ADtoAsync(estado, ct);
    }

    private async Task<EstadoProyectoRequest> NormalizarYValidarAsync(EstadoProyectoRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validador.ValidateAndThrowAsync(datos, ct);
        return datos;
    }

    private async Task<EstadoProyecto> ObtenerEntidadAsync(int id, CancellationToken ct) =>
        await repositorio.ObtenerPorIdAsync(id, ct)
        ?? throw new EntidadNoEncontradaException(CodigosError.EstadoNoEncontrado,
            $"No existe el estado de proyecto con id {id}.");

    private async Task AsegurarNombreDisponibleAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        if (await repositorio.ExisteNombreAsync(nombre, excluirId, ct))
        {
            throw new ConflictoException(CodigosError.EstadoNombreDuplicado,
                $"Ya existe un estado de proyecto con el nombre '{nombre}'.");
        }
    }

    private async Task<EstadoProyectoDto> ADtoAsync(EstadoProyecto estado, CancellationToken ct) =>
        EstadoProyectoDto.Desde(estado, await repositorio.ContarProyectosAsync(estado.Id, ct));
}
