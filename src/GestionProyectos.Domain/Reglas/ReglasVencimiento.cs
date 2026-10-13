namespace GestionProyectos.Domain.Reglas;

/// <summary>
/// Regla pura de "vencida", común a seguimiento y a las pantallas de incidencias (RN-11, RN-36).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, 0.1)
/// </remarks>
public static class ReglasVencimiento
{
    /// <summary>Vencida: estado no final y vencimiento anterior a la fecha de referencia (RN-11).</summary>
    public static bool EstaVencida(DateOnly? fechaVencimiento, bool esFinal, DateOnly fechaReferencia) =>
        !esFinal && fechaVencimiento is { } vencimiento && vencimiento < fechaReferencia;
}
