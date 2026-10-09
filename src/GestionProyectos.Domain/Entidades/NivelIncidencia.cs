namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Nivel de la jerarquía de incidencias (1 = más alto); el número es inmutable (RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class NivelIncidencia : Entidad
{
    public int Numero { get; }

    public string Nombre { get; private set; }

    public string? Descripcion { get; private set; }

    /// <summary>Crea un nivel nuevo.</summary>
    public NivelIncidencia(int numero, string nombre, string? descripcion, DateTime ahora) : base(ahora)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numero);
        Numero = numero;
        Nombre = nombre;
        Descripcion = descripcion;
    }

    private NivelIncidencia(int id, int numero, string nombre, string? descripcion,
        DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        Numero = numero;
        Nombre = nombre;
        Descripcion = descripcion;
    }

    /// <summary>Reconstruye un nivel leído desde la persistencia.</summary>
    public static NivelIncidencia Reconstituir(int id, int numero, string nombre, string? descripcion,
        DateTime fechaCreacion, DateTime? fechaModificacion) =>
        new(id, numero, nombre, descripcion, fechaCreacion, fechaModificacion);

    /// <summary>Edita nombre y descripción; el número no cambia (RN-04).</summary>
    public void Actualizar(string nombre, string? descripcion, DateTime ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        MarcarModificada(ahora);
    }
}
