namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Tipo de incidencia: nivel inmutable, flujo editable y activación (RN-05 a RN-07, RN-12).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class TipoIncidencia : Entidad
{
    public string Nombre { get; private set; }

    public string? Descripcion { get; private set; }

    public int NivelId { get; }

    public int FlujoId { get; private set; }

    public bool Activo { get; private set; }

    /// <summary>Crea un tipo nuevo y activo.</summary>
    public TipoIncidencia(string nombre, string? descripcion, int nivelId, int flujoId, DateTime ahora)
        : base(ahora)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nivelId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flujoId);
        Nombre = nombre;
        Descripcion = descripcion;
        NivelId = nivelId;
        FlujoId = flujoId;
        Activo = true;
    }

    private TipoIncidencia(int id, string nombre, string? descripcion, int nivelId, int flujoId, bool activo,
        DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        NivelId = nivelId;
        FlujoId = flujoId;
        Activo = activo;
    }

    /// <summary>Reconstruye un tipo leído desde la persistencia.</summary>
    public static TipoIncidencia Reconstituir(int id, string nombre, string? descripcion, int nivelId,
        int flujoId, bool activo, DateTime fechaCreacion, DateTime? fechaModificacion) =>
        new(id, nombre, descripcion, nivelId, flujoId, activo, fechaCreacion, fechaModificacion);

    /// <summary>Edita nombre, descripción y flujo; el nivel no cambia (RN-06).</summary>
    public void Actualizar(string nombre, string? descripcion, int flujoId, DateTime ahora)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flujoId);
        Nombre = nombre;
        Descripcion = descripcion;
        FlujoId = flujoId;
        MarcarModificada(ahora);
    }

    /// <summary>Desactiva el tipo; idempotente (RN-07).</summary>
    public void Desactivar(DateTime ahora)
    {
        if (!Activo)
        {
            return;
        }

        Activo = false;
        MarcarModificada(ahora);
    }

    /// <summary>Activa el tipo; idempotente (RN-07).</summary>
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
