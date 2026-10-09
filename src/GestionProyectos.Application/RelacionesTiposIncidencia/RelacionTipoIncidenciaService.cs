using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.RelacionesTiposIncidencia;

/// <summary>
/// Casos de uso de relaciones permitidas; no hay edición ni baja (RN-09).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IRelacionTipoIncidenciaService
{
    /// <summary>Lista relaciones filtradas por tipo padre y/o tipo hijo.</summary>
    Task<IReadOnlyList<RelacionTipoIncidenciaDto>> ListarAsync(int? tipoPadreId, int? tipoHijoId,
        CancellationToken ct);

    /// <summary>Crea una relación entre tipos activos de niveles crecientes (RN-01, RN-08).</summary>
    Task<RelacionTipoIncidenciaDto> CrearAsync(CrearRelacionTipoIncidenciaRequest request, CancellationToken ct);
}

/// <summary>
/// Implementa el alta y consulta de relaciones permitidas (RN-01, RN-08, RN-09).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class RelacionTipoIncidenciaService(
    IRelacionTipoIncidenciaRepository repositorio,
    ITipoIncidenciaRepository tipos,
    INivelIncidenciaRepository niveles,
    IUsuarioActual usuarioActual,
    IValidator<CrearRelacionTipoIncidenciaRequest> validador,
    TimeProvider reloj,
    ILogger<RelacionTipoIncidenciaService> logger) : IRelacionTipoIncidenciaService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<RelacionTipoIncidenciaDto>> ListarAsync(int? tipoPadreId, int? tipoHijoId,
        CancellationToken ct) =>
        repositorio.ListarAsync(tipoPadreId, tipoHijoId, ct);

    /// <inheritdoc />
    public async Task<RelacionTipoIncidenciaDto> CrearAsync(CrearRelacionTipoIncidenciaRequest request,
        CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        ArgumentNullException.ThrowIfNull(request);
        await validador.ValidateAndThrowAsync(request, ct);

        var padre = await ObtenerTipoActivoAsync(request.TipoPadreId!.Value, "padre", ct);
        var hijo = await ObtenerTipoActivoAsync(request.TipoHijoId!.Value, "hijo", ct);
        ReglasConfiguracionIncidencias.AsegurarNivelesDeRelacion(
            await ObtenerNivelAsync(padre.NivelId, ct), await ObtenerNivelAsync(hijo.NivelId, ct));
        if (await repositorio.ExisteAsync(padre.Id, hijo.Id, ct))
        {
            throw new ConflictoException(CodigosError.RelacionDuplicada,
                $"Ya existe la relación {padre.Nombre} → {hijo.Nombre}.");
        }

        var relacion = new RelacionTipoIncidencia(padre.Id, hijo.Id, reloj.AhoraUtc());
        await repositorio.AgregarAsync(relacion, ct);
        logger.LogInformation("Relación de tipos {RelacionId} creada: {PadreId} → {HijoId}",
            relacion.Id, padre.Id, hijo.Id);
        return await repositorio.ObtenerDtoAsync(relacion.Id, ct)
            ?? throw new InvalidOperationException($"No se encontró la relación {relacion.Id} recién creada.");
    }

    private async Task<TipoIncidencia> ObtenerTipoActivoAsync(int id, string rol, CancellationToken ct)
    {
        var tipo = await tipos.ObtenerPorIdAsync(id, ct);
        if (tipo is not { Activo: true })
        {
            throw new ValidacionNegocioException(CodigosError.TipoInvalido,
                $"El tipo {rol} con id {id} no existe o está inactivo.");
        }

        return tipo;
    }

    private async Task<NivelIncidencia> ObtenerNivelAsync(int nivelId, CancellationToken ct) =>
        await niveles.ObtenerPorIdAsync(nivelId, ct)
        ?? throw new InvalidOperationException($"No existe el nivel {nivelId} referenciado por un tipo.");
}
