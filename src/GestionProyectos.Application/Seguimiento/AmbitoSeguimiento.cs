using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Ámbito resuelto: proyecto, agrupación (si corresponde) y filtro de incidencias (RN-01).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record AmbitoResuelto(Proyecto Proyecto, Agrupacion? Agrupacion, AmbitoIncidencias Ambito);

/// <summary>
/// Resuelve el proyecto y la agrupación de un ámbito de seguimiento (404 si no existen, RN-01).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public interface IAmbitoSeguimiento
{
    /// <summary>Resuelve el ámbito; PROYECTO_NO_ENCONTRADO o AGRUPACION_NO_ENCONTRADA si no existen.</summary>
    Task<AmbitoResuelto> ResolverAsync(int proyectoId, IConsultaAmbito consulta, CancellationToken ct);

    /// <summary>Agrupaciones del proyecto ordenadas por nombre (RN-26).</summary>
    Task<IReadOnlyList<Agrupacion>> ListarAgrupacionesAsync(int proyectoId, CancellationToken ct);
}

/// <summary>
/// Implementa IAmbitoSeguimiento sobre los repositorios de proyectos y agrupaciones.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class AmbitoSeguimiento(IProyectoRepository proyectos, IAgrupacionRepository agrupaciones)
    : IAmbitoSeguimiento
{
    /// <inheritdoc />
    public async Task<AmbitoResuelto> ResolverAsync(int proyectoId, IConsultaAmbito consulta, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        var proyecto = await proyectos.ObtenerPorIdAsync(proyectoId, ct)
            ?? throw new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado,
                $"No existe el proyecto con id {proyectoId}.");
        if (consulta.AgrupacionId is not { } agrupacionId)
        {
            return new AmbitoResuelto(proyecto, null, new AmbitoIncidencias(null, consulta.SinAgrupacion == true));
        }

        var agrupacion = await agrupaciones.ObtenerPorIdAsync(agrupacionId, ct);
        if (agrupacion is null || !agrupacion.PerteneceA(proyectoId))
        {
            throw new EntidadNoEncontradaException(CodigosError.AgrupacionNoEncontrada,
                $"No existe la agrupación con id {agrupacionId} en el proyecto {proyectoId}.");
        }

        return new AmbitoResuelto(proyecto, agrupacion, new AmbitoIncidencias(agrupacionId, false));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Agrupacion>> ListarAgrupacionesAsync(int proyectoId, CancellationToken ct) =>
        agrupaciones.ListarPorProyectoAsync(proyectoId, ct);
}
