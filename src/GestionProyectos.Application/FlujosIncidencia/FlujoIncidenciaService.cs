using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.FlujosIncidencia;

/// <summary>
/// Casos de uso de flujos de trabajo de incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IFlujoIncidenciaService
{
    /// <summary>Lista los flujos ordenados por nombre.</summary>
    Task<IReadOnlyList<FlujoIncidenciaDto>> ListarAsync(CancellationToken ct);

    /// <summary>Obtiene un flujo por id (404 si no existe).</summary>
    Task<FlujoIncidenciaDto> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>Crea un flujo con su composición (RN-01, RN-11, RN-13, RN-14).</summary>
    Task<FlujoIncidenciaDto> CrearAsync(FlujoIncidenciaRequest request, CancellationToken ct);

    /// <summary>Reemplaza datos y composición del flujo (RN-01, RN-11, RN-13 a RN-15).</summary>
    Task<FlujoIncidenciaDto> EditarAsync(int id, FlujoIncidenciaRequest request, CancellationToken ct);
}

/// <summary>
/// Implementa la configuración de flujos de trabajo (RN-01, RN-11, RN-13 a RN-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class FlujoIncidenciaService(
    IFlujoIncidenciaRepository repositorio,
    IEstadoIncidenciaRepository estados,
    IUnitOfWork unitOfWork,
    IUsuarioActual usuarioActual,
    IValidator<FlujoIncidenciaRequest> validador,
    TimeProvider reloj,
    ILogger<FlujoIncidenciaService> logger) : IFlujoIncidenciaService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<FlujoIncidenciaDto>> ListarAsync(CancellationToken ct) => repositorio.ListarAsync(ct);

    /// <inheritdoc />
    public async Task<FlujoIncidenciaDto> ObtenerAsync(int id, CancellationToken ct) =>
        await repositorio.ObtenerDtoAsync(id, ct) ?? throw NoEncontrado(id);

    /// <inheritdoc />
    public async Task<FlujoIncidenciaDto> CrearAsync(FlujoIncidenciaRequest request, CancellationToken ct)
    {
        var datos = await PrepararAsync(request, ct);
        var composicion = datos.AComposicion();
        await AsegurarEstadosAsignablesAsync(composicion, null, ct);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, null, ct);

        var flujo = new FlujoIncidencia(datos.Nombre!, datos.Descripcion, composicion, reloj.AhoraUtc());
        await unitOfWork.EjecutarEnTransaccionAsync(() => repositorio.AgregarAsync(flujo, ct), ct);
        logger.LogInformation("Flujo de incidencias {FlujoId} creado", flujo.Id);
        return await ObtenerAsync(flujo.Id, ct);
    }

    /// <inheritdoc />
    public async Task<FlujoIncidenciaDto> EditarAsync(int id, FlujoIncidenciaRequest request, CancellationToken ct)
    {
        var datos = await PrepararAsync(request, ct);
        var flujo = await repositorio.ObtenerPorIdAsync(id, ct) ?? throw NoEncontrado(id);
        var composicion = datos.AComposicion();
        await AsegurarEstadosAsignablesAsync(composicion, flujo, ct);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, id, ct);

        await unitOfWork.EjecutarEnTransaccionAsync(async () =>
        {
            var enUso = await repositorio.ContarIncidenciasPorEstadoAsync(id, ct);
            ReglasConfiguracionIncidencias.AsegurarEstadosQuitadosSinUso(flujo, composicion, enUso);
            flujo.Actualizar(datos.Nombre!, datos.Descripcion, composicion, reloj.AhoraUtc());
            await repositorio.ActualizarAsync(flujo, ct);
        }, ct);

        logger.LogInformation("Flujo de incidencias {FlujoId} actualizado", flujo.Id);
        return await ObtenerAsync(flujo.Id, ct);
    }

    private async Task<FlujoIncidenciaRequest> PrepararAsync(FlujoIncidenciaRequest request, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validador.ValidateAndThrowAsync(datos, ct);
        return datos;
    }

    private async Task AsegurarEstadosAsignablesAsync(IReadOnlyList<EstadoEnFlujo> composicion,
        FlujoIncidencia? flujoActual, CancellationToken ct)
    {
        var existentes = await estados.ListarPorIdsAsync(composicion.Select(e => e.EstadoId).ToList(), ct);
        ReglasConfiguracionIncidencias.AsegurarEstadosAsignables(composicion, existentes, flujoActual);
    }

    private static EntidadNoEncontradaException NoEncontrado(int id) =>
        new(CodigosError.FlujoNoEncontrado, $"No existe el flujo de incidencias con id {id}.");

    private async Task AsegurarNombreDisponibleAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        if (await repositorio.ExisteNombreAsync(nombre, excluirId, ct))
        {
            throw new ConflictoException(CodigosError.FlujoNombreDuplicado,
                $"Ya existe un flujo con el nombre '{nombre}'.");
        }
    }
}
