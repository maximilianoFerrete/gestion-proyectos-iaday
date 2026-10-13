using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Escrituras de incidencias: alta, edición y cambio de tipo, siempre bajo bloqueo del proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IIncidenciaService
{
    /// <summary>Crea una incidencia con número y código del proyecto (RN-18 a RN-22, RN-25 a RN-31).</summary>
    Task<IncidenciaDto> CrearAsync(int proyectoId, CrearIncidenciaRequest request, CancellationToken ct);

    /// <summary>Edita una incidencia y propaga la agrupación (RN-21 a RN-32, RN-36 a RN-38).</summary>
    Task<IncidenciaDto> EditarAsync(int proyectoId, int id, EditarIncidenciaRequest request, CancellationToken ct);

    /// <summary>Cambia el tipo; exige el permiso Incidencias.CambiarTipo (RN-02, RN-23, RN-25).</summary>
    Task<IncidenciaDto> CambiarTipoAsync(int proyectoId, int id, CambiarTipoIncidenciaRequest request,
        CancellationToken ct);
}

/// <summary>
/// Implementa las escrituras de incidencias; las reglas puras viven en Domain.Reglas.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Persiste la estimación en alta y edición (RF-005, RN-34)
/// </remarks>
public class IncidenciaService(
    IIncidenciaRepository incidencias,
    IReferenciasIncidencia referencias,
    IIncidenciaConsultaService consultas,
    IUnitOfWork unitOfWork,
    IUsuarioActual usuarioActual,
    IncidenciaValidadores validadores,
    TimeProvider reloj,
    ILogger<IncidenciaService> logger) : IIncidenciaService
{
    /// <inheritdoc />
    public async Task<IncidenciaDto> CrearAsync(int proyectoId, CrearIncidenciaRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validadores.Alta.ValidateAndThrowAsync(datos, ct);

        var incidencia = await unitOfWork.EjecutarEnTransaccionAsync(async () =>
        {
            var asignado = await incidencias.ObtenerSiguienteNumeroAsync(proyectoId, ct)
                ?? throw IncidenciaConsultaService.ProyectoNoEncontrado(proyectoId);
            asignado.Proyecto.AsegurarEditable();

            var tipo = await referencias.ObtenerTipoAsignableAsync(datos.TipoId!.Value, ct);
            var flujo = await referencias.ObtenerFlujoAsync(tipo, ct);
            var prioridadId = await referencias.ResolverPrioridadAsync(datos.PrioridadId, null, ct);
            await referencias.AsegurarResponsableAsync(datos.ResponsableId, null, ct);
            var ubicacion = await ResolverUbicacionAltaAsync(proyectoId, tipo, datos, ct);

            var nueva = new Incidencia(proyectoId, asignado.Numero,
                FormateadorCodigoIncidencia.Formatear(asignado.Proyecto.Codigo, asignado.Numero),
                tipo.Id, flujo.EstadoInicialId, ADatos(datos.Titulo, datos.Descripcion, prioridadId,
                    datos.ResponsableId, datos.FechaVencimiento, datos.Estimacion), ubicacion, reloj.AhoraUtc());
            await incidencias.AgregarAsync(nueva, ct);
            return nueva;
        }, ct);

        logger.LogInformation("Incidencia {IncidenciaId} creada con código {Codigo}", incidencia.Id, incidencia.Codigo);
        return await consultas.ObtenerAsync(proyectoId, incidencia.Id, ct);
    }

    /// <inheritdoc />
    public async Task<IncidenciaDto> EditarAsync(int proyectoId, int id, EditarIncidenciaRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validadores.Edicion.ValidateAndThrowAsync(datos, ct);

        await unitOfWork.EjecutarEnTransaccionAsync(async () =>
        {
            var incidencia = await BloquearYObtenerEditableAsync(proyectoId, id, ct);
            var tipo = await referencias.ObtenerTipoAsync(incidencia.TipoId, ct);
            var flujo = await referencias.ObtenerFlujoAsync(tipo, ct);
            var estado = await referencias.ObtenerEstadoAsync(datos.EstadoId!.Value, ct);
            ReglasEstadoIncidencia.AsegurarEstadoAsignable(datos.EstadoId.Value, incidencia.EstadoId, flujo, estado);
            var prioridadId = await referencias.ResolverPrioridadAsync(datos.PrioridadId, incidencia.PrioridadId, ct);
            await referencias.AsegurarResponsableAsync(datos.ResponsableId, incidencia.ResponsableId, ct);
            var ubicacion = await ResolverUbicacionEdicionAsync(proyectoId, incidencia, tipo, datos, ct);
            if (datos.FechaVencimiento != incidencia.FechaVencimiento)
            {
                ReglasJerarquia.AsegurarVencimientoNoAnteriorAHijas(datos.FechaVencimiento,
                    await incidencias.ListarHijasAsync(id, ct));
            }

            await GuardarEdicionAsync(incidencia, ADatos(datos.Titulo, datos.Descripcion, prioridadId,
                datos.ResponsableId, datos.FechaVencimiento, datos.Estimacion), datos.EstadoId.Value, ubicacion, ct);
        }, ct);

        logger.LogInformation("Incidencia {IncidenciaId} actualizada", id);
        return await consultas.ObtenerAsync(proyectoId, id, ct);
    }

    /// <inheritdoc />
    public async Task<IncidenciaDto> CambiarTipoAsync(int proyectoId, int id, CambiarTipoIncidenciaRequest request,
        CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.IncidenciasCambiarTipo);
        ArgumentNullException.ThrowIfNull(request);
        await validadores.CambioTipo.ValidateAndThrowAsync(request, ct);

        await unitOfWork.EjecutarEnTransaccionAsync(async () =>
        {
            var incidencia = await BloquearYObtenerEditableAsync(proyectoId, id, ct);
            if (request.TipoId == incidencia.TipoId)
            {
                return;
            }

            var nuevoTipo = await referencias.ObtenerTipoAsignableAsync(request.TipoId!.Value, ct);
            await AsegurarJerarquiaCompatibleAsync(proyectoId, incidencia, nuevoTipo, ct);
            var flujoNuevo = await referencias.ObtenerFlujoAsync(nuevoTipo, ct);
            var estadoSolicitado = request.EstadoId is { } estadoId
                ? await referencias.ObtenerEstadoAsync(estadoId, ct)
                : null;
            var estadoFinalId = ReglasEstadoIncidencia.ResolverEstadoAlCambiarTipo(incidencia.EstadoId,
                request.EstadoId, flujoNuevo, estadoSolicitado);

            incidencia.CambiarTipo(nuevoTipo.Id, estadoFinalId, reloj.AhoraUtc());
            await incidencias.ActualizarAsync(incidencia, ct);
            logger.LogInformation("Incidencia {IncidenciaId} cambiada al tipo {TipoId}", id, nuevoTipo.Id);
        }, ct);

        return await consultas.ObtenerAsync(proyectoId, id, ct);
    }

    private static DatosIncidencia ADatos(string? titulo, string? descripcion, int prioridadId,
        int? responsableId, DateOnly? fechaVencimiento, decimal? estimacion) =>
        new(titulo!, descripcion!, prioridadId, responsableId, fechaVencimiento, estimacion);

    private async Task<Incidencia> BloquearYObtenerEditableAsync(int proyectoId, int id, CancellationToken ct)
    {
        var proyecto = await incidencias.BloquearProyectoAsync(proyectoId, ct)
            ?? throw IncidenciaConsultaService.ProyectoNoEncontrado(proyectoId);
        var incidencia = await incidencias.ObtenerPorIdAsync(id, ct);
        if (incidencia is null || !incidencia.PerteneceA(proyectoId))
        {
            throw IncidenciaConsultaService.IncidenciaNoEncontrada(proyectoId, id);
        }

        proyecto.AsegurarEditable();
        return incidencia;
    }

    private async Task<UbicacionIncidencia> ResolverUbicacionAltaAsync(int proyectoId, TipoIncidencia tipo,
        CrearIncidenciaRequest datos, CancellationToken ct)
    {
        if (datos.PadreId is not { } padreId)
        {
            await referencias.AsegurarAgrupacionAsync(proyectoId, datos.AgrupacionId, ct);
            return new UbicacionIncidencia(null, datos.AgrupacionId);
        }

        var padre = await referencias.ObtenerPadreAsync(proyectoId, padreId, ct);
        await referencias.AsegurarRelacionAsync(await referencias.ObtenerTipoAsync(padre.TipoId, ct), tipo, ct);
        var agrupacionId = ReglasJerarquia.ResolverAgrupacionConPadre(datos.AgrupacionId, padre);
        ReglasJerarquia.AsegurarVencimientoNoPosteriorAlPadre(datos.FechaVencimiento, padre);
        return new UbicacionIncidencia(padre.Id, agrupacionId);
    }

    private async Task<UbicacionIncidencia> ResolverUbicacionEdicionAsync(int proyectoId, Incidencia incidencia,
        TipoIncidencia tipo, EditarIncidenciaRequest datos, CancellationToken ct)
    {
        if (datos.PadreId is not { } padreId)
        {
            var agrupacionId = ReglasJerarquia.ResolverAgrupacionSinPadre(datos.AgrupacionId, incidencia);
            if (agrupacionId != incidencia.AgrupacionId)
            {
                await referencias.AsegurarAgrupacionAsync(proyectoId, agrupacionId, ct);
            }

            return new UbicacionIncidencia(null, agrupacionId);
        }

        var padre = await referencias.ObtenerPadreAsync(proyectoId, padreId, ct);
        if (padreId != incidencia.PadreId)
        {
            ReglasJerarquia.AsegurarSinCiclo(incidencia.Id, padreId,
                await incidencias.ObtenerIdsAncestrosAsync(padreId, ct));
            await referencias.AsegurarRelacionAsync(await referencias.ObtenerTipoAsync(padre.TipoId, ct), tipo, ct);
        }

        var heredada = ReglasJerarquia.ResolverAgrupacionConPadre(datos.AgrupacionId, padre);
        ReglasJerarquia.AsegurarVencimientoNoPosteriorAlPadre(datos.FechaVencimiento, padre);
        return new UbicacionIncidencia(padre.Id, heredada);
    }

    private async Task GuardarEdicionAsync(Incidencia incidencia, DatosIncidencia datos, int estadoId,
        UbicacionIncidencia ubicacion, CancellationToken ct)
    {
        var agrupacionAnterior = incidencia.AgrupacionId;
        var padreAnterior = incidencia.PadreId;
        var ahora = reloj.AhoraUtc();
        incidencia.Actualizar(datos, estadoId, ubicacion, ahora);
        await incidencias.ActualizarAsync(incidencia, ct);
        if (ReglasJerarquia.RequierePropagacion(agrupacionAnterior, padreAnterior, ubicacion))
        {
            var cantidad = await incidencias.ActualizarAgrupacionDescendientesAsync(incidencia.Id,
                ubicacion.AgrupacionId, ahora, ct);
            logger.LogInformation("Agrupación de la incidencia {IncidenciaId} propagada a {Cantidad} descendientes",
                incidencia.Id, cantidad);
        }
    }

    private async Task AsegurarJerarquiaCompatibleAsync(int proyectoId, Incidencia incidencia,
        TipoIncidencia nuevoTipo, CancellationToken ct)
    {
        if (incidencia.PadreId is { } padreId)
        {
            var padre = await referencias.ObtenerPadreAsync(proyectoId, padreId, ct);
            await referencias.AsegurarRelacionAsync(await referencias.ObtenerTipoAsync(padre.TipoId, ct),
                nuevoTipo, ct);
        }

        var hijas = await incidencias.ListarHijasAsync(incidencia.Id, ct);
        if (hijas.Count > 0)
        {
            ReglasJerarquia.AsegurarHijasCompatibles(nuevoTipo, hijas,
                await referencias.ListarTiposHijoAsync(nuevoTipo.Id, ct));
        }
    }
}

/// <summary>
/// Agrupa los validadores que usa IncidenciaService.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record IncidenciaValidadores(
    IValidator<CrearIncidenciaRequest> Alta,
    IValidator<EditarIncidenciaRequest> Edicion,
    IValidator<CambiarTipoIncidenciaRequest> CambioTipo);
