using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;

namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Estado del catálogo de incidencias; las marcas inicial/final pertenecen al flujo (RN-10, RN-11).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega la categoría de seguimiento (RF-005, RN-17, RN-18)
/// </remarks>
public class EstadoIncidencia : Entidad
{
    public string Nombre { get; private set; }

    public string? Descripcion { get; private set; }

    public int Orden { get; private set; }

    public bool Activo { get; private set; }

    /// <summary>Categoría de seguimiento (CategoriasEstadoIncidencia) o null (RN-17).</summary>
    public string? Categoria { get; private set; }

    /// <summary>Crea un estado nuevo y activo; sin flujos, cualquier categoría es válida (RN-18).</summary>
    public EstadoIncidencia(string nombre, string? descripcion, int orden, DateTime ahora,
        string? categoria = null) : base(ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        Activo = true;
        Categoria = ValidarCategoria(categoria);
    }

    private EstadoIncidencia(int id, string nombre, string? descripcion, int orden, bool activo,
        string? categoria, DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        Activo = activo;
        Categoria = categoria;
    }

    /// <summary>Reconstruye un estado leído desde la persistencia.</summary>
    public static EstadoIncidencia Reconstituir(int id, string nombre, string? descripcion, int orden,
        bool activo, DateTime fechaCreacion, DateTime? fechaModificacion, string? categoria = null) =>
        new(id, nombre, descripcion, orden, activo, categoria, fechaCreacion, fechaModificacion);

    /// <summary>Cambia la categoría; con categoría no puede ser inicial ni final en flujos (RN-18).</summary>
    public void CambiarCategoria(string? categoria, IReadOnlyCollection<string> flujosDondeEsInicialOFinal,
        DateTime ahora)
    {
        var nueva = ValidarCategoria(categoria);
        if (nueva == Categoria)
        {
            return;
        }

        ReglasCategoriaEstado.AsegurarCategoriaAsignable(Nombre, nueva, flujosDondeEsInicialOFinal);
        Categoria = nueva;
        MarcarModificada(ahora);
    }

    /// <summary>Edita nombre, descripción y orden (RN-10).</summary>
    public void Actualizar(string nombre, string? descripcion, int orden, DateTime ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        MarcarModificada(ahora);
    }

    /// <summary>Desactiva si no es inicial de ningún flujo (RN-11); idempotente.</summary>
    public void Desactivar(IReadOnlyCollection<string> flujosDondeEsInicial, DateTime ahora)
    {
        ArgumentNullException.ThrowIfNull(flujosDondeEsInicial);
        if (!Activo)
        {
            return;
        }

        if (flujosDondeEsInicial.Count > 0)
        {
            throw new ConflictoException(CodigosError.EstadoIncidenciaInicialEnFlujo,
                $"El estado '{Nombre}' es el inicial de los flujos: {string.Join(", ", flujosDondeEsInicial)}.");
        }

        Activo = false;
        MarcarModificada(ahora);
    }

    /// <summary>Activa el estado; idempotente.</summary>
    public void Activar(DateTime ahora)
    {
        if (Activo)
        {
            return;
        }

        Activo = true;
        MarcarModificada(ahora);
    }

    private static string? ValidarCategoria(string? categoria)
    {
        if (!CategoriasEstadoIncidencia.EsValida(categoria))
        {
            throw new ArgumentException($"Categoría de seguimiento desconocida: {categoria}.", nameof(categoria));
        }

        return categoria;
    }
}
