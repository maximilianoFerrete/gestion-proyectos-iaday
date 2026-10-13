using FluentValidation;
using FluentValidation.Results;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Consultas de seguimiento: tablero general, resumen y grilla del tablero (sin usuario, RN-25).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public interface ISeguimientoService
{
    /// <summary>Indicadores de todos los proyectos filtrados y página ordenada (RN-02, RN-27).</summary>
    Task<TableroProyectosDto> ObtenerTableroProyectosAsync(ConsultaTableroProyectosQuery query,
        CancellationToken ct);

    /// <summary>Resumen de un proyecto, agrupación o sin agrupación (RN-01, RN-26).</summary>
    Task<SeguimientoDto> ObtenerSeguimientoAsync(int proyectoId, ConsultaSeguimientoQuery query,
        CancellationToken ct);

    /// <summary>Grilla de incidencias del ámbito, alcance y filtros (RN-20, RN-21, RN-32).</summary>
    Task<TableroIncidenciasDto> ObtenerTableroAsync(int proyectoId, ConsultaTableroIncidenciasQuery query,
        CancellationToken ct);
}

/// <summary>
/// Agrupa los validadores que usa SeguimientoService.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record SeguimientoValidadores(
    IValidator<ConsultaTableroProyectosQuery> TableroProyectos,
    IValidator<ConsultaSeguimientoQuery> Seguimiento,
    IValidator<ConsultaTableroIncidenciasQuery> TableroIncidencias);

/// <summary>
/// Implementa las consultas de seguimiento; las reglas de indicadores viven en CalculadoraIndicadores.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class SeguimientoService(
    ISeguimientoRepository seguimiento,
    ICatalogoSeguimiento catalogo,
    IAmbitoSeguimiento ambitos,
    IEstadoIncidenciaRepository estados,
    IConfiguracionTableroRepository configuraciones,
    IFechaReferencia fechaReferencia,
    SeguimientoValidadores validadores) : ISeguimientoService
{
    /// <inheritdoc />
    public async Task<TableroProyectosDto> ObtenerTableroProyectosAsync(ConsultaTableroProyectosQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var datos = query.Normalizar();
        await validadores.TableroProyectos.ValidateAndThrowAsync(datos, ct);
        var alcance = await catalogo.ResolverAlcanceAsync(datos, ct);
        var hoy = fechaReferencia.Hoy();
        var filtro = new FiltroProyectosSeguimiento(datos.Texto, datos.Archivados is null
            ? FiltroArchivados.Excluir
            : OpcionesListadoProyectos.Archivados[datos.Archivados]);

        var proyectos = await seguimiento.ListarProyectosAsync(filtro, ct);
        var conteos = await seguimiento.ContarAsync(
            FiltroConteoIncidencias.DeProyectos(filtro, alcance.TipoIds, hoy), AgrupacionConteo.Proyecto, ct);
        var calculadora = await catalogo.CrearCalculadoraAsync(alcance, ct);
        var porProyecto = conteos.ToLookup(c => c.Grupo);
        var filas = proyectos
            .Select(p => new FilaTableroProyectosDto(p.Proyecto, p.CantidadAgrupaciones,
                calculadora.Calcular(porProyecto[p.Proyecto.Id])))
            .ToList();
        var idsProyectos = filas.Select(f => (int?)f.Proyecto.Id).ToHashSet();
        var totales = calculadora.Calcular(conteos.Where(c => idsProyectos.Contains(c.Grupo)));

        var ordenadas = OrdenadorProyectosSeguimiento.Ordenar(filas,
            datos.OrdenarPor is null ? OrdenTableroProyectos.Codigo : OpcionesSeguimiento.Ordenes[datos.OrdenarPor],
            datos.Direccion is null or OpcionesListadoProyectos.DireccionDesc);
        var pagina = OrdenadorProyectosSeguimiento.Paginar(ordenadas,
            datos.Pagina ?? OpcionesListadoProyectos.PaginaPorDefecto,
            datos.TamanioPagina ?? OpcionesListadoProyectos.TamanioPaginaPorDefecto);
        return new TableroProyectosDto(hoy, alcance.Dto, totales, pagina);
    }

    /// <inheritdoc />
    public async Task<SeguimientoDto> ObtenerSeguimientoAsync(int proyectoId, ConsultaSeguimientoQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        await validadores.Seguimiento.ValidateAndThrowAsync(query, ct);
        var ambito = await ambitos.ResolverAsync(proyectoId, query, ct);
        var alcance = await catalogo.ResolverAlcanceAsync(query, ct);
        var hoy = fechaReferencia.Hoy();
        var esProyecto = ambito.Ambito == AmbitoIncidencias.Proyecto;

        var conteos = await seguimiento.ContarAsync(
            FiltroConteoIncidencias.DeProyecto(proyectoId, ambito.Ambito, alcance.TipoIds, hoy),
            esProyecto ? AgrupacionConteo.Agrupacion : AgrupacionConteo.Ninguna, ct);
        var calculadora = await catalogo.CrearCalculadoraAsync(alcance, ct);
        IReadOnlyList<FilaAgrupacionSeguimientoDto> filas = esProyecto
            ? await FilasPorAgrupacionAsync(proyectoId, conteos, calculadora, ct)
            : [];
        var agrupacion = ambito.Agrupacion is { } a ? new AgrupacionSeguimientoDto(a.Id, a.Nombre, a.Descripcion) : null;
        return new SeguimientoDto(hoy, AReferencia(ambito.Proyecto), ambito.Ambito.Nombre, agrupacion, alcance.Dto,
            calculadora.Calcular(conteos), filas);
    }

    /// <inheritdoc />
    public async Task<TableroIncidenciasDto> ObtenerTableroAsync(int proyectoId,
        ConsultaTableroIncidenciasQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var datos = query.Normalizar();
        await validadores.TableroIncidencias.ValidateAndThrowAsync(datos, ct);
        var ambito = await ambitos.ResolverAsync(proyectoId, datos, ct);
        var alcance = await catalogo.ResolverAlcanceAsync(datos, ct);
        var estadoFiltro = await ObtenerEstadoFiltroAsync(datos.EstadoId, ct);
        var hoy = fechaReferencia.Hoy();

        var leidas = await seguimiento.ListarIncidenciasTableroAsync(new FiltroTableroIncidencias(proyectoId,
            ambito.Ambito, alcance.TipoIds,
            datos.Indicador is null ? null : OpcionesSeguimiento.Indicadores[datos.Indicador],
            datos.EstadoId, datos.Texto, hoy), ct);
        var items = OrdenTableroPorDefecto.Ordenar(leidas.Select(l => AItem(l, hoy)));
        var configuracion = ConfiguracionTableroDto.Desde(proyectoId, await configuraciones.ObtenerAsync(proyectoId, ct));
        return new TableroIncidenciasDto(hoy, AReferencia(ambito.Proyecto), alcance.Dto,
            new FiltrosTableroDto(datos.Indicador, estadoFiltro, datos.Texto), configuracion, items.Count, items);
    }

    private static ProyectoReferenciaIncidenciaDto AReferencia(Proyecto proyecto) =>
        new(proyecto.Id, proyecto.Codigo, proyecto.Nombre, proyecto.EstaArchivado);

    private static ItemTableroDto AItem(IncidenciaTableroLeida leida, DateOnly hoy) => new(
        leida.Id, leida.Codigo, leida.Numero, leida.Titulo, leida.Tipo, leida.Estado, leida.Estimacion,
        ReglasVencimiento.EstaVencida(leida.FechaVencimiento, leida.Estado.EsFinal, hoy), leida.Responsable);

    private async Task<IReadOnlyList<FilaAgrupacionSeguimientoDto>> FilasPorAgrupacionAsync(int proyectoId,
        IReadOnlyList<ConteoIncidencias> conteos, CalculadoraIndicadores calculadora, CancellationToken ct)
    {
        var porAgrupacion = conteos.ToLookup(c => c.Grupo);
        var filas = (await ambitos.ListarAgrupacionesAsync(proyectoId, ct))
            .Select(a => new FilaAgrupacionSeguimientoDto(new AgrupacionReferenciaDto(a.Id, a.Nombre),
                calculadora.Calcular(porAgrupacion[a.Id])))
            .ToList();
        filas.Add(new FilaAgrupacionSeguimientoDto(null, calculadora.Calcular(porAgrupacion[null])));
        return filas;
    }

    private async Task<EstadoFiltroTableroDto?> ObtenerEstadoFiltroAsync(int? estadoId, CancellationToken ct)
    {
        if (estadoId is not { } id)
        {
            return null;
        }

        var estado = await estados.ObtenerPorIdAsync(id, ct)
            ?? throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(ConsultaTableroIncidenciasQuery.EstadoId),
                    OpcionesSeguimiento.MensajeEstadoInexistente),
            });
        return new EstadoFiltroTableroDto(estado.Id, estado.Nombre, estado.Activo, estado.Categoria);
    }
}
