namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Relación permitida tipo padre → tipo hijo; inmutable una vez creada (RN-08, RN-09).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class RelacionTipoIncidencia : Entidad
{
    public int TipoPadreId { get; }

    public int TipoHijoId { get; }

    /// <summary>Crea una relación entre dos tipos distintos.</summary>
    public RelacionTipoIncidencia(int tipoPadreId, int tipoHijoId, DateTime ahora) : base(ahora)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tipoPadreId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tipoHijoId);
        if (tipoPadreId == tipoHijoId)
        {
            throw new ArgumentException("El tipo padre y el tipo hijo deben ser distintos.", nameof(tipoHijoId));
        }

        TipoPadreId = tipoPadreId;
        TipoHijoId = tipoHijoId;
    }
}
