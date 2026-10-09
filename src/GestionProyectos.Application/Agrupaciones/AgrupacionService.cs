using FluentValidation;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Agrupaciones;

/// <summary>
/// Implementa el ABM de agrupaciones de un proyecto (RN-22, RN-26 a RN-30).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Cantidad de incidencias y desasociación al eliminar (RF-002, RN-30)
/// </remarks>
public class AgrupacionService(
    IAgrupacionRepository agrupaciones,
    IProyectoRepository proyectos,
    IIncidenciaRepository incidencias,
    IUnitOfWork unitOfWork,
    IValidator<AgrupacionRequest> validador,
    TimeProvider reloj,
    ILogger<AgrupacionService> logger) : IAgrupacionService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AgrupacionDto>> ListarAsync(int proyectoId, CancellationToken ct)
    {
        await ObtenerProyectoAsync(proyectoId, ct);
        var lista = await agrupaciones.ListarPorProyectoAsync(proyectoId, ct);
        var cantidades = await agrupaciones.ContarIncidenciasPorAgrupacionAsync(proyectoId, ct);
        return lista.Select(a => AgrupacionDto.Desde(a, cantidades.GetValueOrDefault(a.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<AgrupacionDto> ObtenerAsync(int proyectoId, int id, CancellationToken ct)
    {
        await ObtenerProyectoAsync(proyectoId, ct);
        var agrupacion = await ObtenerDelProyectoAsync(proyectoId, id, ct);
        return AgrupacionDto.Desde(agrupacion, await agrupaciones.ContarIncidenciasAsync(id, ct));
    }

    /// <inheritdoc />
    public async Task<AgrupacionDto> CrearAsync(int proyectoId, AgrupacionRequest request,
        CancellationToken ct)
    {
        var datos = await NormalizarYValidarAsync(request, ct);
        var proyecto = await ObtenerProyectoAsync(proyectoId, ct);
        proyecto.AsegurarEditable();
        await AsegurarNombreDisponibleAsync(proyectoId, datos.Nombre!, null, ct);

        var agrupacion = new Agrupacion(proyectoId, datos.Nombre!, datos.Descripcion!, reloj.AhoraUtc());
        await agrupaciones.AgregarAsync(agrupacion, ct);
        logger.LogInformation("Agrupación {AgrupacionId} creada en el proyecto {ProyectoId}",
            agrupacion.Id, proyectoId);
        return AgrupacionDto.Desde(agrupacion);
    }

    /// <inheritdoc />
    public async Task<AgrupacionDto> EditarAsync(int proyectoId, int id, AgrupacionRequest request,
        CancellationToken ct)
    {
        var datos = await NormalizarYValidarAsync(request, ct);
        var proyecto = await ObtenerProyectoAsync(proyectoId, ct);
        var agrupacion = await ObtenerDelProyectoAsync(proyectoId, id, ct);
        proyecto.AsegurarEditable();
        await AsegurarNombreDisponibleAsync(proyectoId, datos.Nombre!, id, ct);

        agrupacion.Actualizar(datos.Nombre!, datos.Descripcion!, reloj.AhoraUtc());
        await agrupaciones.ActualizarAsync(agrupacion, ct);
        logger.LogInformation("Agrupación {AgrupacionId} actualizada", agrupacion.Id);
        return AgrupacionDto.Desde(agrupacion, await agrupaciones.ContarIncidenciasAsync(agrupacion.Id, ct));
    }

    /// <inheritdoc />
    public async Task EliminarAsync(int proyectoId, int id, CancellationToken ct)
    {
        var proyecto = await ObtenerProyectoAsync(proyectoId, ct);
        var agrupacion = await ObtenerDelProyectoAsync(proyectoId, id, ct);
        proyecto.AsegurarEditable();

        var ahora = reloj.AhoraUtc();
        var desasociadas = await unitOfWork.EjecutarEnTransaccionAsync(async () =>
        {
            var bloqueado = await incidencias.BloquearProyectoAsync(proyectoId, ct)
                ?? throw new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado,
                    $"No existe el proyecto con id {proyectoId}.");
            bloqueado.AsegurarEditable();
            var cantidad = await incidencias.DesasociarAgrupacionAsync(proyectoId, agrupacion.Id, ahora, ct);
            await agrupaciones.EliminarAsync(agrupacion.Id, ct);
            return cantidad;
        }, ct);
        logger.LogInformation(
            "Agrupación {AgrupacionId} eliminada del proyecto {ProyectoId}; {Cantidad} incidencias desasociadas",
            agrupacion.Id, proyectoId, desasociadas);
    }

    private async Task<AgrupacionRequest> NormalizarYValidarAsync(AgrupacionRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validador.ValidateAndThrowAsync(datos, ct);
        return datos;
    }

    private async Task<Proyecto> ObtenerProyectoAsync(int proyectoId, CancellationToken ct) =>
        await proyectos.ObtenerPorIdAsync(proyectoId, ct)
        ?? throw new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado,
            $"No existe el proyecto con id {proyectoId}.");

    private async Task<Agrupacion> ObtenerDelProyectoAsync(int proyectoId, int id, CancellationToken ct)
    {
        var agrupacion = await agrupaciones.ObtenerPorIdAsync(id, ct);
        if (agrupacion is null || !agrupacion.PerteneceA(proyectoId))
        {
            throw new EntidadNoEncontradaException(CodigosError.AgrupacionNoEncontrada,
                $"No existe la agrupación con id {id} en el proyecto {proyectoId}.");
        }

        return agrupacion;
    }

    private async Task AsegurarNombreDisponibleAsync(int proyectoId, string nombre, int? excluirId,
        CancellationToken ct)
    {
        if (await agrupaciones.ExisteNombreAsync(proyectoId, nombre, excluirId, ct))
        {
            throw new ConflictoException(CodigosError.AgrupacionNombreDuplicado,
                $"Ya existe una agrupación con el nombre '{nombre}' en el proyecto.");
        }
    }
}
