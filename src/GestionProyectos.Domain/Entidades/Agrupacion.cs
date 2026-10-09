namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Agrupación de incidencias dentro de un proyecto; su proyecto es inmutable (RN-28).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class Agrupacion : Entidad
{
    public int ProyectoId { get; }

    public string Nombre { get; private set; }

    public string Descripcion { get; private set; }

    /// <summary>Crea una agrupación nueva en el proyecto indicado.</summary>
    public Agrupacion(int proyectoId, string nombre, string descripcion, DateTime ahora) : base(ahora)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proyectoId);
        ProyectoId = proyectoId;
        Nombre = nombre;
        Descripcion = descripcion;
    }

    private Agrupacion(int id, int proyectoId, string nombre, string descripcion,
        DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        ProyectoId = proyectoId;
        Nombre = nombre;
        Descripcion = descripcion;
    }

    /// <summary>Reconstruye una agrupación leída desde la persistencia.</summary>
    public static Agrupacion Reconstituir(int id, int proyectoId, string nombre, string descripcion,
        DateTime fechaCreacion, DateTime? fechaModificacion) =>
        new(id, proyectoId, nombre, descripcion, fechaCreacion, fechaModificacion);

    /// <summary>Indica si pertenece al proyecto indicado (RN-29).</summary>
    public bool PerteneceA(int proyectoId) => ProyectoId == proyectoId;

    /// <summary>Edita nombre y descripción.</summary>
    public void Actualizar(string nombre, string descripcion, DateTime ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        MarcarModificada(ahora);
    }
}
