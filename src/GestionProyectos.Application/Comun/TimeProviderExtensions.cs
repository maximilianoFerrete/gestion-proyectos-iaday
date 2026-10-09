namespace GestionProyectos.Application.Comun;

/// <summary>
/// Fecha y hora actual en UTC con la precisión de DATETIME2(0).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class TimeProviderExtensions
{
    /// <summary>Devuelve el instante actual UTC truncado a segundos.</summary>
    public static DateTime AhoraUtc(this TimeProvider reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);
        var ahora = reloj.GetUtcNow().UtcDateTime;
        return new DateTime(ahora.Ticks - (ahora.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }
}
