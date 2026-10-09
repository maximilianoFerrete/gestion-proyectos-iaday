namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Estado incluido en un flujo con su posición y marcas (objeto de valor inmutable).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record EstadoEnFlujo(int EstadoId, int Orden, bool EsInicial, bool EsFinal);

/// <summary>
/// Flujo de trabajo: estados incluidos, exactamente un inicial y cero o más finales (RN-13, RN-14).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class FlujoIncidencia : Entidad
{
    private List<EstadoEnFlujo> _estados;

    public string Nombre { get; private set; }

    public string? Descripcion { get; private set; }

    /// <summary>Estados del flujo ordenados por Orden.</summary>
    public IReadOnlyList<EstadoEnFlujo> Estados => _estados;

    /// <summary>Id del estado inicial del flujo (RN-14, RN-20).</summary>
    public int EstadoInicialId => _estados.Single(e => e.EsInicial).EstadoId;

    /// <summary>Crea un flujo nuevo con su composición.</summary>
    public FlujoIncidencia(string nombre, string? descripcion, IEnumerable<EstadoEnFlujo> estados, DateTime ahora)
        : base(ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        _estados = Componer(estados);
    }

    private FlujoIncidencia(int id, string nombre, string? descripcion, IEnumerable<EstadoEnFlujo> estados,
        DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        _estados = Componer(estados);
    }

    /// <summary>Reconstruye un flujo leído desde la persistencia.</summary>
    public static FlujoIncidencia Reconstituir(int id, string nombre, string? descripcion,
        IEnumerable<EstadoEnFlujo> estados, DateTime fechaCreacion, DateTime? fechaModificacion) =>
        new(id, nombre, descripcion, estados, fechaCreacion, fechaModificacion);

    /// <summary>Indica si el estado pertenece al flujo.</summary>
    public bool Incluye(int estadoId) => _estados.Exists(e => e.EstadoId == estadoId);

    /// <summary>Indica si el estado es final dentro del flujo (false si no pertenece).</summary>
    public bool EsFinal(int estadoId) => _estados.Exists(e => e.EstadoId == estadoId && e.EsFinal);

    /// <summary>Reemplaza nombre, descripción y composición completa (PUT de flujos).</summary>
    public void Actualizar(string nombre, string? descripcion, IEnumerable<EstadoEnFlujo> estados, DateTime ahora)
    {
        var nuevos = Componer(estados);
        Nombre = nombre;
        Descripcion = descripcion;
        _estados = nuevos;
        MarcarModificada(ahora);
    }

    private static List<EstadoEnFlujo> Componer(IEnumerable<EstadoEnFlujo> estados)
    {
        ArgumentNullException.ThrowIfNull(estados);
        var lista = estados.OrderBy(e => e.Orden).ToList();
        if (lista.Count == 0
            || lista.Count(e => e.EsInicial) != 1
            || lista.Exists(e => e.EsInicial && e.EsFinal)
            || lista.Select(e => e.EstadoId).Distinct().Count() != lista.Count)
        {
            throw new ArgumentException(
                "Composición de flujo inválida: requiere estados únicos y exactamente un inicial no final.",
                nameof(estados));
        }

        return lista;
    }
}
