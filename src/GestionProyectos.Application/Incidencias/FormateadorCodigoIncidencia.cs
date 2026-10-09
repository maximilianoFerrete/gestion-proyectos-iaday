using System.Globalization;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Arma el código legible de una incidencia: código del proyecto, guion y número (RN-18, DA-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public static class FormateadorCodigoIncidencia
{
    /// <summary>Devuelve el código sin relleno (PRJ-0001 y 15 → PRJ-0001-15).</summary>
    public static string Formatear(string codigoProyecto, int numero)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoProyecto);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numero);
        return codigoProyecto + "-" + numero.ToString(CultureInfo.InvariantCulture);
    }
}
