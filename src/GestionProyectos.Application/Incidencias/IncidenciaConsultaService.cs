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
/// Modificado: 2026-10-10 - Árbol con ámbito opcional (RF-005, C-RF002-04)
/// </remarks>
public interface IIncidenciaConsultaService
{
    /// <summary>Lista incidencias del proyecto con filtros, orden y paginación (CA-19).</summary>
    Task<Paginado<IncidenciaResumenDto>> ListarAsync(int proyectoId, ListarIncidenciasQuery query,
        CancellationToken ct);

    /// <summary>Incidencias del proyecto (o de su ámbito) en lista plana por número (DA-11).</summary>
    Task<IReadOnlyList<NodoArbolIncidenciaDto>> ObtenerArbolAsync(int proyectoId, ArbolIncidenciasQuery query,
        CancellationToken ct);

    /// <summary>Obtiene el detalle; 404 si no existe o es de otro proyecto (RN-37).</summary>
    Task<IncidenciaDto> ObtenerAsync(int proyectoId, int id, CancellationToken ct);
}

/// <summary>
/// Implementa las consultas de incidencias (listado, árbol y detalle).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Vencida con la fecha de referencia y árbol por ámbito (RF-005, RN-36)
/// </remarks>
public class IncidenciaConsultaService(
    IIncidenciaRepository incidencias,
    IProyectoRepository proyectos,
    IValidator<ListarIncidenciasQuery> validadorListado,
    IValidator<ArbolIncidenciasQuery> validadorArbol,
    IFechaReferencia fechaReferencia) : IIncidenciaConsultaService
{
    /// <inheritdoc />
    public async Task<Paginado<IncidenciaResumenDto>> ListarAsync(int proyectoId, ListarIncidenciasQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var normalizada = query.Normalizar();
        await validadorListado.ValidateAndThrowAsync(normalizada, ct);
        await AsegurarProyectoAsync(proyectoId, ct);
        var pagina = await incidencias.ListarAsync(OpcionesListadoIncidencias.CrearFiltro(proyectoId, normalizada), ct);
        var hoy = fechaReferencia.Hoy();
        return pagina with { Items = pagina.Items.Select(i => i.ConVencida(hoy)).ToList() };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NodoArbolIncidenciaDto>> ObtenerArbolAsync(int proyectoId,
        ArbolIncidenciasQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        await validadorArbol.ValidateAndThrowAsync(query, ct);
        await AsegurarProyectoAsync(proyectoId, ct);
        var nodos = query.TieneFiltros
            ? await incidencias.ListarArbolAsync(proyectoId, query.AgrupacionId, query.SinAgrupacion == true, ct)
            : await incidencias.ListarArbolAsync(proyectoId, ct);
        var hoy = fechaReferencia.Hoy();
        return nodos.Select(n => n.ConVencida(hoy)).ToList();
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

        return detalle.ConVencida(fechaReferencia.Hoy());
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
