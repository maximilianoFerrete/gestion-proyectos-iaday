namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Base de las entidades persistidas: identidad y fechas de auditoría (UTC).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public abstract class Entidad
{
    /// <summary>Identificador; 0 mientras la entidad no fue persistida.</summary>
    public int Id { get; private set; }

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime FechaCreacion { get; protected set; }

    /// <summary>Fecha de la última modificación en UTC, o null si nunca se modificó.</summary>
    public DateTime? FechaModificacion { get; protected set; }

    /// <summary>Inicializa una entidad nueva con su fecha de creación.</summary>
    protected Entidad(DateTime fechaCreacion)
    {
        FechaCreacion = fechaCreacion;
    }

    /// <summary>Inicializa una entidad leída desde la persistencia.</summary>
    protected Entidad(int id, DateTime fechaCreacion, DateTime? fechaModificacion)
    {
        Id = id;
        FechaCreacion = fechaCreacion;
        FechaModificacion = fechaModificacion;
    }

    /// <summary>Asigna el id generado por la persistencia; solo puede hacerse una vez.</summary>
    public void AsignarId(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        if (Id != 0)
        {
            throw new InvalidOperationException("La entidad ya tiene un id asignado.");
        }

        Id = id;
    }

    /// <summary>Registra la fecha de modificación.</summary>
    protected void MarcarModificada(DateTime ahora) => FechaModificacion = ahora;
}
