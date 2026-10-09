using System.Globalization;

namespace GestionProyectos.Application.Proyectos;

/// <summary>
/// Arma el código legible del proyecto a partir del número de secuencia (RN-14).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class FormateadorCodigoProyecto
{
    public const string Prefijo = "PRJ-";

    /// <summary>Devuelve PRJ- más el número con relleno a 4 dígitos (PRJ-0001, PRJ-10000).</summary>
    public static string Formatear(int numero)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numero);
        return Prefijo + numero.ToString("D4", CultureInfo.InvariantCulture);
    }
}
