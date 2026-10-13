using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Arma los indicadores a partir de conteos crudos, el catálogo de estados y las marcas finales
/// de los flujos contabilizados. Sin acceso a datos (RN-08 a RN-16, 0.2).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed class CalculadoraIndicadores
{
    private readonly IReadOnlyDictionary<int, EstadoIncidencia> _estados;
    private readonly IReadOnlyDictionary<int, bool> _esFinalEnFlujos;

    /// <summary>Crea la calculadora con el catálogo de estados y los estados de los flujos.</summary>
    public CalculadoraIndicadores(IEnumerable<EstadoIncidencia> estados,
        IEnumerable<EstadoEnFlujoSeguimiento> estadosDeFlujos)
    {
        ArgumentNullException.ThrowIfNull(estados);
        ArgumentNullException.ThrowIfNull(estadosDeFlujos);
        _estados = estados.ToDictionary(e => e.Id);
        _esFinalEnFlujos = estadosDeFlujos
            .GroupBy(e => e.EstadoId)
            .ToDictionary(g => g.Key, g => g.All(e => e.EsFinal));
    }

    /// <summary>Calcula los indicadores de las filas indicadas (vacío = todo en cero, RN-15).</summary>
    public IndicadoresDto Calcular(IEnumerable<ConteoIncidencias> filas)
    {
        ArgumentNullException.ThrowIfNull(filas);
        var lista = filas.ToList();
        var abiertas = lista.Where(f => !f.EsFinal).ToList();
        var total = lista.Sum(f => f.Cantidad);
        var finalizadas = lista.Where(f => f.EsFinal).Sum(f => f.Cantidad);
        return new IndicadoresDto(
            total,
            total - finalizadas,
            finalizadas,
            abiertas.Sum(f => f.ConVencimientoAnterior),
            abiertas.Where(f => TieneCategoria(f.EstadoId, CategoriasEstadoIncidencia.Bloqueada)).Sum(f => f.Cantidad),
            abiertas.Where(f => TieneCategoria(f.EstadoId, CategoriasEstadoIncidencia.EnEspera)).Sum(f => f.Cantidad),
            abiertas.Sum(f => f.SinResponsable),
            PorEstado(lista));
    }

    /// <summary>Suma dos conjuntos de indicadores campo a campo y por estado (RN-26, RN-27).</summary>
    public static IndicadoresDto Sumar(IndicadoresDto a, IndicadoresDto b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var porEstado = a.PorEstado.Concat(b.PorEstado)
            .GroupBy(c => c.Estado.Id)
            .Select(g => new ConteoEstadoDto(
                g.First().Estado with { EsFinal = g.All(c => c.Estado.EsFinal) },
                g.Sum(c => c.Cantidad)))
            .OrderBy(c => c.Estado.Orden).ThenBy(c => c.Estado.Nombre, ComparadoresSeguimiento.Nombre).ThenBy(c => c.Estado.Id)
            .ToList();
        return new IndicadoresDto(a.Total + b.Total, a.Abiertas + b.Abiertas, a.Finalizadas + b.Finalizadas,
            a.Vencidas + b.Vencidas, a.Bloqueadas + b.Bloqueadas, a.EnEspera + b.EnEspera,
            a.SinResponsable + b.SinResponsable, porEstado);
    }

    private bool TieneCategoria(int estadoId, string categoria) =>
        _estados.TryGetValue(estadoId, out var estado) && estado.Categoria == categoria;

    private List<ConteoEstadoDto> PorEstado(List<ConteoIncidencias> filas)
    {
        var cantidades = filas.GroupBy(f => f.EstadoId).ToDictionary(g => g.Key, g => g.ToList());
        var ids = _esFinalEnFlujos.Keys
            .Where(id => _estados.TryGetValue(id, out var estado) && estado.Activo)
            .Concat(cantidades.Where(c => c.Value.Sum(f => f.Cantidad) > 0).Select(c => c.Key))
            .Distinct();
        return ids
            .Select(id => new ConteoEstadoDto(
                Estado(id, cantidades.GetValueOrDefault(id) ?? []),
                cantidades.GetValueOrDefault(id)?.Sum(f => f.Cantidad) ?? 0))
            .OrderBy(c => c.Estado.Orden).ThenBy(c => c.Estado.Nombre, ComparadoresSeguimiento.Nombre).ThenBy(c => c.Estado.Id)
            .ToList();
    }

    private EstadoSeguimientoDto Estado(int id, List<ConteoIncidencias> filas)
    {
        var esFinal = _esFinalEnFlujos.TryGetValue(id, out var final)
            ? final
            : filas.Count > 0 && filas.TrueForAll(f => f.EsFinal);
        return _estados.TryGetValue(id, out var estado)
            ? new EstadoSeguimientoDto(estado.Id, estado.Nombre, estado.Orden, estado.Activo, estado.Categoria, esFinal)
            : new EstadoSeguimientoDto(id, $"Estado {id}", int.MaxValue, false, null, esFinal);
    }
}
