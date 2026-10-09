using FluentValidation;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Consultas de incidencias de un proyecto (también si está archivado, RN-36).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IIncidenciaConsultaService
{
    /// <summary>Lista incidencias del proyecto con filtros, orden y paginación (CA-19).</summary>
    Task<Paginado<IncidenciaResumenDto>> ListarAsync(int proyectoId, ListarIncidenciasQuery query,
        CancellationToken ct);

    /// <summary>Devuelve todas las incidencias del proyecto en lista plana por número (DA-11).</summary>
    Task<IReadOnlyList<NodoArbolIncidenciaDto>> ObtenerArbolAsync(int proyectoId, CancellationToken ct);

    /// <summary>Obtiene el detalle; 404 si no existe o es de otro proyecto (RN-37).</summary>
    Task<IncidenciaDto> ObtenerAsync(int proyectoId, int id, CancellationToken ct);
}

/// <summary>
/// Implementa las consultas de incidencias (listado, árbol y detalle).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class IncidenciaConsultaService(
    IIncidenciaRepository incidencias,
    IProyectoRepository proyectos,
    IValidator<ListarIncidenciasQuery> validadorListado) : IIncidenciaConsultaService
{
    /// <inheritdoc />
    public async Task<Paginado<IncidenciaResumenDto>> ListarAsync(int proyectoId, ListarIncidenciasQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var normalizada = query.Normalizar();
        await validadorListado.ValidateAndThrowAsync(normalizada, ct);
        await AsegurarProyectoAsync(proyectoId, ct);
        return await incidencias.ListarAsync(OpcionesListadoIncidencias.CrearFiltro(proyectoId, normalizada), ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NodoArbolIncidenciaDto>> ObtenerArbolAsync(int proyectoId,
        CancellationToken ct)
    {
        await AsegurarProyectoAsync(proyectoId, ct);
        return await incidencias.ListarArbolAsync(proyectoId, ct);
    }

    /// <inheritdoc />
    public async Task<IncidenciaDto> ObtenerAsync(int proyectoId, int id, CancellationToken ct)
    {
        await AsegurarProyectoAsync(proyectoId, ct);
        var detalle = await incidencias.ObtenerDetalleAsync(id, ct);
        if (detalle is null || detalle.Proyecto.Id != proyectoId)
        {
            throw IncidenciaNoEncontrada(proyectoId, id);
        }

        return detalle;
    }

    /// <summary>Error 404 de incidencia inexistente o de otro proyecto (RN-37).</summary>
    public static EntidadNoEncontradaException IncidenciaNoEncontrada(int proyectoId, int id) =>
        new(CodigosError.IncidenciaNoEncontrada, $"No existe la incidencia con id {id} en el proyecto {proyectoId}.");

    /// <summary>Error 404 de proyecto inexistente.</summary>
    public static EntidadNoEncontradaException ProyectoNoEncontrado(int proyectoId) =>
        new(CodigosError.ProyectoNoEncontrado, $"No existe el proyecto con id {proyectoId}.");

    private async Task AsegurarProyectoAsync(int proyectoId, CancellationToken ct)
    {
        if (await proyectos.ObtenerPorIdAsync(proyectoId, ct) is null)
        {
            throw ProyectoNoEncontrado(proyectoId);
        }
    }
}
