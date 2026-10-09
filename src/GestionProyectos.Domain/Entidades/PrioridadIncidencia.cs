using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Prioridad de incidencias; existe exactamente una predeterminada y activa (RN-17).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class PrioridadIncidencia : Entidad
{
    public string Nombre { get; private set; }

    public string? Descripcion { get; private set; }

    public int Orden { get; private set; }

    public bool EsPredeterminada { get; private set; }

    public bool Activo { get; private set; }

    /// <summary>Crea una prioridad activa y no predeterminada.</summary>
    public PrioridadIncidencia(string nombre, string? descripcion, int orden, DateTime ahora) : base(ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        Activo = true;
    }

    private PrioridadIncidencia(int id, string nombre, string? descripcion, int orden, bool esPredeterminada,
        bool activo, DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        EsPredeterminada = esPredeterminada;
        Activo = activo;
    }

    /// <summary>Reconstruye una prioridad leída desde la persistencia.</summary>
    public static PrioridadIncidencia Reconstituir(int id, string nombre, string? descripcion, int orden,
        bool esPredeterminada, bool activo, DateTime fechaCreacion, DateTime? fechaModificacion) =>
        new(id, nombre, descripcion, orden, esPredeterminada, activo, fechaCreacion, fechaModificacion);

    /// <summary>Edita nombre, descripción y orden.</summary>
    public void Actualizar(string nombre, string? descripcion, int orden, DateTime ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        MarcarModificada(ahora);
    }

    /// <summary>Valida que pueda ser la predeterminada (activa, RN-17) sin modificarla.</summary>
    public void AsegurarElegibleComoPredeterminada()
    {
        if (!Activo)
        {
            throw new ConflictoException(CodigosError.PrioridadNoElegiblePredeterminada,
                $"La prioridad '{Nombre}' está inactiva y no puede ser la predeterminada.");
        }
    }

    /// <summary>Marca la prioridad como predeterminada; debe estar activa (RN-17).</summary>
    public void MarcarPredeterminada(DateTime ahora)
    {
        AsegurarElegibleComoPredeterminada();
        EsPredeterminada = true;
        MarcarModificada(ahora);
    }

    /// <summary>Quita la marca; solo al marcar otra prioridad como predeterminada (RN-17).</summary>
    public void DesmarcarPredeterminada(DateTime ahora)
    {
        EsPredeterminada = false;
        MarcarModificada(ahora);
    }

    /// <summary>Desactiva la prioridad si no es la predeterminada (RN-17); idempotente.</summary>
    public void Desactivar(DateTime ahora)
    {
        if (!Activo)
        {
            return;
        }

        if (EsPredeterminada)
        {
            throw new ConflictoException(CodigosError.PrioridadPredeterminadaNoDesactivable,
                $"La prioridad '{Nombre}' es la predeterminada y no puede desactivarse.");
        }

        Activo = false;
        MarcarModificada(ahora);
    }

    /// <summary>Activa la prioridad; idempotente.</summary>
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
