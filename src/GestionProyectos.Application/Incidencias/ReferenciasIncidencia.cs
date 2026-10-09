using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Resuelve y valida las referencias de una incidencia (tipo, estado, prioridad, persona, etc.).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IReferenciasIncidencia
{
    /// <summary>Obtiene un tipo existente (error interno si no existe).</summary>
    Task<TipoIncidencia> ObtenerTipoAsync(int tipoId, CancellationToken ct);

    /// <summary>Obtiene un tipo existente y activo; si no, TIPO_INVALIDO (RN-07).</summary>
    Task<TipoIncidencia> ObtenerTipoAsignableAsync(int tipoId, CancellationToken ct);

    /// <summary>Obtiene el flujo del tipo con sus estados (error interno si no existe).</summary>
    Task<FlujoIncidencia> ObtenerFlujoAsync(TipoIncidencia tipo, CancellationToken ct);

    /// <summary>Obtiene el estado de incidencia o null si no existe.</summary>
    Task<EstadoIncidencia?> ObtenerEstadoAsync(int estadoId, CancellationToken ct);

    /// <summary>Resuelve la prioridad: predeterminada si es null; activa salvo la actual (RN-21).</summary>
    Task<int> ResolverPrioridadAsync(int? prioridadId, int? prioridadActualId, CancellationToken ct);

    /// <summary>Exige persona existente y activa, salvo el responsable actual (RN-22).</summary>
    Task AsegurarResponsableAsync(int? responsableId, int? responsableActualId, CancellationToken ct);

    /// <summary>Exige que la agrupación exista y sea del proyecto (RN-28).</summary>
    Task AsegurarAgrupacionAsync(int proyectoId, int? agrupacionId, CancellationToken ct);

    /// <summary>Obtiene el padre si existe y es del proyecto; si no, PADRE_INVALIDO (RN-27).</summary>
    Task<Incidencia> ObtenerPadreAsync(int proyectoId, int padreId, CancellationToken ct);

    /// <summary>Exige la relación tipo del padre → tipo de la hija (RN-25).</summary>
    Task AsegurarRelacionAsync(TipoIncidencia tipoPadre, TipoIncidencia tipoHijo, CancellationToken ct);

    /// <summary>Ids de los tipos que pueden ser hijos del tipo indicado.</summary>
    Task<IReadOnlyList<int>> ListarTiposHijoAsync(int tipoPadreId, CancellationToken ct);
}

/// <summary>
/// Implementación de IReferenciasIncidencia sobre los repositorios de catálogos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class ReferenciasIncidencia(
    ITipoIncidenciaRepository tipos,
    IFlujoIncidenciaRepository flujos,
    IEstadoIncidenciaRepository estados,
    IPrioridadIncidenciaRepository prioridades,
    IPersonaRepository personas,
    IAgrupacionRepository agrupaciones,
    IRelacionTipoIncidenciaRepository relaciones,
    IIncidenciaRepository incidencias) : IReferenciasIncidencia
{
    /// <inheritdoc />
    public async Task<TipoIncidencia> ObtenerTipoAsync(int tipoId, CancellationToken ct) =>
        await tipos.ObtenerPorIdAsync(tipoId, ct)
        ?? throw new InvalidOperationException($"No existe el tipo de incidencia {tipoId}.");

    /// <inheritdoc />
    public async Task<TipoIncidencia> ObtenerTipoAsignableAsync(int tipoId, CancellationToken ct)
    {
        var tipo = await tipos.ObtenerPorIdAsync(tipoId, ct);
        if (tipo is not { Activo: true })
        {
            throw new ValidacionNegocioException(CodigosError.TipoInvalido,
                $"El tipo de incidencia con id {tipoId} no existe o está inactivo.");
        }

        return tipo;
    }

    /// <inheritdoc />
    public async Task<FlujoIncidencia> ObtenerFlujoAsync(TipoIncidencia tipo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tipo);
        return await flujos.ObtenerPorIdAsync(tipo.FlujoId, ct)
            ?? throw new InvalidOperationException($"No existe el flujo {tipo.FlujoId} del tipo {tipo.Id}.");
    }

    /// <inheritdoc />
    public Task<EstadoIncidencia?> ObtenerEstadoAsync(int estadoId, CancellationToken ct) =>
        estados.ObtenerPorIdAsync(estadoId, ct);

    /// <inheritdoc />
    public async Task<int> ResolverPrioridadAsync(int? prioridadId, int? prioridadActualId, CancellationToken ct)
    {
        if (prioridadId is null)
        {
            var predeterminada = await prioridades.ObtenerPredeterminadaAsync(ct)
                ?? throw new InvalidOperationException("No hay una prioridad predeterminada configurada.");
            return predeterminada.Id;
        }

        if (prioridadId == prioridadActualId)
        {
            return prioridadId.Value;
        }

        var prioridad = await prioridades.ObtenerPorIdAsync(prioridadId.Value, ct);
        if (prioridad is not { Activo: true })
        {
            throw new ValidacionNegocioException(CodigosError.PrioridadInvalida,
                $"La prioridad con id {prioridadId} no existe o está inactiva.");
        }

        return prioridad.Id;
    }

    /// <inheritdoc />
    public async Task AsegurarResponsableAsync(int? responsableId, int? responsableActualId, CancellationToken ct)
    {
        if (responsableId is null || responsableId == responsableActualId)
        {
            return;
        }

        var persona = await personas.ObtenerPorIdAsync(responsableId.Value, ct);
        if (persona is not { Activo: true })
        {
            throw new ValidacionNegocioException(CodigosError.ResponsableInvalido,
                $"El responsable con id {responsableId} no existe o está inactivo.");
        }
    }

    /// <inheritdoc />
    public async Task AsegurarAgrupacionAsync(int proyectoId, int? agrupacionId, CancellationToken ct)
    {
        if (agrupacionId is null)
        {
            return;
        }

        var agrupacion = await agrupaciones.ObtenerPorIdAsync(agrupacionId.Value, ct);
        if (agrupacion is null || !agrupacion.PerteneceA(proyectoId))
        {
            throw new ValidacionNegocioException(CodigosError.AgrupacionInvalida,
                $"La agrupación con id {agrupacionId} no existe en el proyecto.");
        }
    }

    /// <inheritdoc />
    public async Task<Incidencia> ObtenerPadreAsync(int proyectoId, int padreId, CancellationToken ct)
    {
        var padre = await incidencias.ObtenerPorIdAsync(padreId, ct);
        if (padre is null || !padre.PerteneceA(proyectoId))
        {
            throw new ValidacionNegocioException(CodigosError.PadreInvalido,
                $"La incidencia padre con id {padreId} no existe en el proyecto.");
        }

        return padre;
    }

    /// <inheritdoc />
    public async Task AsegurarRelacionAsync(TipoIncidencia tipoPadre, TipoIncidencia tipoHijo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tipoPadre);
        var permitidos = await relaciones.ListarTiposHijoAsync(tipoPadre.Id, ct);
        ReglasJerarquia.AsegurarRelacionPermitida(tipoPadre, tipoHijo, permitidos);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> ListarTiposHijoAsync(int tipoPadreId, CancellationToken ct) =>
        relaciones.ListarTiposHijoAsync(tipoPadreId, ct);
}
