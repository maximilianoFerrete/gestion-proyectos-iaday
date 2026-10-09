using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.TiposIncidencia;

/// <summary>
/// Casos de uso de tipos de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface ITipoIncidenciaService
{
    /// <summary>Lista tipos (opcionalmente solo activos y de un nivel).</summary>
    Task<IReadOnlyList<TipoIncidenciaDto>> ListarAsync(bool soloActivos, int? nivelId, CancellationToken ct);

    /// <summary>Obtiene un tipo por id (404 si no existe).</summary>
    Task<TipoIncidenciaDto> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>Crea un tipo activo (RN-01, RN-05, RN-06).</summary>
    Task<TipoIncidenciaDto> CrearAsync(CrearTipoIncidenciaRequest request, CancellationToken ct);

    /// <summary>Edita nombre, descripción y flujo (RN-01, RN-05, RN-12).</summary>
    Task<TipoIncidenciaDto> EditarAsync(int id, EditarTipoIncidenciaRequest request, CancellationToken ct);

    /// <summary>Desactiva el tipo; idempotente (RN-07).</summary>
    Task<TipoIncidenciaDto> DesactivarAsync(int id, CancellationToken ct);

    /// <summary>Activa el tipo; idempotente (RN-07).</summary>
    Task<TipoIncidenciaDto> ActivarAsync(int id, CancellationToken ct);
}

/// <summary>
/// Implementa la configuración de tipos de incidencia (RN-01, RN-05 a RN-07, RN-12).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class TipoIncidenciaService(
    ITipoIncidenciaRepository repositorio,
    INivelIncidenciaRepository niveles,
    IFlujoIncidenciaRepository flujos,
    IUsuarioActual usuarioActual,
    TipoIncidenciaValidadores validadores,
    TimeProvider reloj,
    ILogger<TipoIncidenciaService> logger) : ITipoIncidenciaService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<TipoIncidenciaDto>> ListarAsync(bool soloActivos, int? nivelId,
        CancellationToken ct) =>
        repositorio.ListarAsync(soloActivos, nivelId, ct);

    /// <inheritdoc />
    public async Task<TipoIncidenciaDto> ObtenerAsync(int id, CancellationToken ct) =>
        await repositorio.ObtenerDtoAsync(id, ct) ?? throw NoEncontrado(id);

    /// <inheritdoc />
    public async Task<TipoIncidenciaDto> CrearAsync(CrearTipoIncidenciaRequest request, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validadores.Alta.ValidateAndThrowAsync(datos, ct);

        if (await niveles.ObtenerPorIdAsync(datos.NivelId!.Value, ct) is null)
        {
            throw new ValidacionNegocioException(CodigosError.NivelInvalido,
                $"No existe el nivel de incidencia con id {datos.NivelId}.");
        }

        await ObtenerFlujoValidoAsync(datos.FlujoId!.Value, ct);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, null, ct);

        var tipo = new TipoIncidencia(datos.Nombre!, datos.Descripcion, datos.NivelId.Value,
            datos.FlujoId.Value, reloj.AhoraUtc());
        await repositorio.AgregarAsync(tipo, ct);
        logger.LogInformation("Tipo de incidencia {TipoId} creado", tipo.Id);
        return await ObtenerAsync(tipo.Id, ct);
    }

    /// <inheritdoc />
    public async Task<TipoIncidenciaDto> EditarAsync(int id, EditarTipoIncidenciaRequest request,
        CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validadores.Edicion.ValidateAndThrowAsync(datos, ct);

        var tipo = await repositorio.ObtenerPorIdAsync(id, ct) ?? throw NoEncontrado(id);
        var flujoNuevo = await ObtenerFlujoValidoAsync(datos.FlujoId!.Value, ct);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, id, ct);
        if (flujoNuevo.Id != tipo.FlujoId)
        {
            var porEstado = await repositorio.ContarIncidenciasPorEstadoAsync(id, ct);
            ReglasConfiguracionIncidencias.AsegurarFlujoCompatible(flujoNuevo, porEstado);
        }

        tipo.Actualizar(datos.Nombre!, datos.Descripcion, flujoNuevo.Id, reloj.AhoraUtc());
        await repositorio.ActualizarAsync(tipo, ct);
        logger.LogInformation("Tipo de incidencia {TipoId} actualizado", tipo.Id);
        return await ObtenerAsync(tipo.Id, ct);
    }

    /// <inheritdoc />
    public Task<TipoIncidenciaDto> DesactivarAsync(int id, CancellationToken ct) =>
        CambiarActivoAsync(id, activar: false, ct);

    /// <inheritdoc />
    public Task<TipoIncidenciaDto> ActivarAsync(int id, CancellationToken ct) =>
        CambiarActivoAsync(id, activar: true, ct);

    private async Task<TipoIncidenciaDto> CambiarActivoAsync(int id, bool activar, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        var tipo = await repositorio.ObtenerPorIdAsync(id, ct) ?? throw NoEncontrado(id);
        if (tipo.Activo != activar)
        {
            var ahora = reloj.AhoraUtc();
            if (activar)
            {
                tipo.Activar(ahora);
            }
            else
            {
                tipo.Desactivar(ahora);
            }

            await repositorio.ActualizarAsync(tipo, ct);
            logger.LogInformation("Tipo de incidencia {TipoId} activo={Activo}", tipo.Id, activar);
        }

        return await ObtenerAsync(tipo.Id, ct);
    }

    private async Task<FlujoIncidencia> ObtenerFlujoValidoAsync(int flujoId, CancellationToken ct) =>
        await flujos.ObtenerPorIdAsync(flujoId, ct)
        ?? throw new ValidacionNegocioException(CodigosError.FlujoInvalido,
            $"No existe el flujo de incidencias con id {flujoId}.");

    private static EntidadNoEncontradaException NoEncontrado(int id) =>
        new(CodigosError.TipoIncidenciaNoEncontrado, $"No existe el tipo de incidencia con id {id}.");

    private async Task AsegurarNombreDisponibleAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        if (await repositorio.ExisteNombreAsync(nombre, excluirId, ct))
        {
            throw new ConflictoException(CodigosError.TipoNombreDuplicado,
                $"Ya existe un tipo de incidencia con el nombre '{nombre}'.");
        }
    }
}

/// <summary>
/// Agrupa los validadores que usa TipoIncidenciaService.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record TipoIncidenciaValidadores(
    IValidator<CrearTipoIncidenciaRequest> Alta,
    IValidator<EditarTipoIncidenciaRequest> Edicion);
