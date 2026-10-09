using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Estado del catálogo de incidencias; las marcas inicial/final pertenecen al flujo (RN-10, RN-11).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class EstadoIncidencia : Entidad
{
    public string Nombre { get; private set; }

    public string? Descripcion { get; private set; }

    public int Orden { get; private set; }

    public bool Activo { get; private set; }

    /// <summary>Crea un estado nuevo y activo.</summary>
    public EstadoIncidencia(string nombre, string? descripcion, int orden, DateTime ahora) : base(ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        Activo = true;
    }

    private EstadoIncidencia(int id, string nombre, string? descripcion, int orden, bool activo,
        DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        Activo = activo;
    }

    /// <summary>Reconstruye un estado leído desde la persistencia.</summary>
    public static EstadoIncidencia Reconstituir(int id, string nombre, string? descripcion, int orden,
        bool activo, DateTime fechaCreacion, DateTime? fechaModificacion) =>
        new(id, nombre, descripcion, orden, activo, fechaCreacion, fechaModificacion);

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
}
