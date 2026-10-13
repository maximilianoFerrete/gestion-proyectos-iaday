using System.Globalization;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Comparadores de texto compartidos por los ordenamientos en memoria del seguimiento.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public static class ComparadoresSeguimiento
{
    /// <summary>Nombres sin distinguir mayúsculas (como la collation *_CI_AS).</summary>
    public static StringComparer Nombre { get; } = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);

    /// <summary>Códigos de proyecto: primero por largo (PRJ-9999 antes que PRJ-10000), luego ordinal.</summary>
    public static IComparer<string> Codigo { get; } = Comparer<string>.Create((a, b) =>
    {
        var porLargo = (a?.Length ?? 0).CompareTo(b?.Length ?? 0);
        return porLargo != 0 ? porLargo : string.CompareOrdinal(a, b);
    });
}

/// <summary>
/// Ordena y pagina en memoria las filas del tablero general (0.2, DA-11, CA-03, CA-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public static class OrdenadorProyectosSeguimiento
{
    /// <summary>Ordena por la columna pedida; desempata por código desc y luego id desc.</summary>
    public static IReadOnlyList<FilaTableroProyectosDto> Ordenar(IEnumerable<FilaTableroProyectosDto> filas,
        OrdenTableroProyectos orden, bool descendente)
    {
        ArgumentNullException.ThrowIfNull(filas);
        var ordenadas = orden switch
        {
            OrdenTableroProyectos.Codigo => Por(filas, f => f.Proyecto.Codigo, ComparadoresSeguimiento.Codigo, descendente),
            OrdenTableroProyectos.Nombre => Por(filas, f => f.Proyecto.Nombre, ComparadoresSeguimiento.Nombre, descendente),
            OrdenTableroProyectos.Total => Por(filas, f => f.Indicadores.Total, Comparer<int>.Default, descendente),
            OrdenTableroProyectos.Abiertas => Por(filas, f => f.Indicadores.Abiertas, Comparer<int>.Default, descendente),
            OrdenTableroProyectos.Vencidas => Por(filas, f => f.Indicadores.Vencidas, Comparer<int>.Default, descendente),
            OrdenTableroProyectos.Bloqueadas =>
                Por(filas, f => f.Indicadores.Bloqueadas, Comparer<int>.Default, descendente),
            OrdenTableroProyectos.EnEspera => Por(filas, f => f.Indicadores.EnEspera, Comparer<int>.Default, descendente),
            OrdenTableroProyectos.SinResponsable =>
                Por(filas, f => f.Indicadores.SinResponsable, Comparer<int>.Default, descendente),
            _ => throw new ArgumentOutOfRangeException(nameof(orden), orden, null),
        };
        return ordenadas
            .ThenByDescending(f => f.Proyecto.Codigo, ComparadoresSeguimiento.Codigo)
            .ThenByDescending(f => f.Proyecto.Id)
            .ToList();
    }

    /// <summary>Devuelve la página pedida; fuera de rango, ítems vacíos con el total correcto.</summary>
    public static Paginado<T> Paginar<T>(IReadOnlyList<T> filas, int pagina, int tamanioPagina)
    {
        ArgumentNullException.ThrowIfNull(filas);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pagina);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tamanioPagina);
        var desde = (long)(pagina - 1) * tamanioPagina;
        IReadOnlyList<T> items = desde >= filas.Count ? [] : filas.Skip((int)desde).Take(tamanioPagina).ToList();
        return new Paginado<T>(items, pagina, tamanioPagina, filas.Count);
    }

    private static IOrderedEnumerable<FilaTableroProyectosDto> Por<TClave>(IEnumerable<FilaTableroProyectosDto> filas,
        Func<FilaTableroProyectosDto, TClave> clave, IComparer<TClave> comparador, bool descendente) =>
        descendente ? filas.OrderByDescending(clave, comparador) : filas.OrderBy(clave, comparador);
}

/// <summary>
/// Orden por defecto de la grilla del tablero: nivel del tipo, nombre del tipo y número (RN-32).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, DA-17)
/// </remarks>
public static class OrdenTableroPorDefecto
{
    /// <summary>Devuelve los ítems ordenados por Tipo (nivel, nombre) y Nro.</summary>
    public static IReadOnlyList<ItemTableroDto> Ordenar(IEnumerable<ItemTableroDto> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items
            .OrderBy(i => i.Tipo.NivelNumero)
            .ThenBy(i => i.Tipo.Nombre, ComparadoresSeguimiento.Nombre)
            .ThenBy(i => i.Numero)
            .ThenBy(i => i.Id)
            .ToList();
    }
}
