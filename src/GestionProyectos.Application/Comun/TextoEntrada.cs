namespace GestionProyectos.Application.Comun;

/// <summary>
/// Normalización de textos de entrada (spec 0.2, RN-31).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class TextoEntrada
{
    /// <summary>Aplica Trim; devuelve null si el texto es nulo o queda vacío.</summary>
    public static string? Normalizar(string? texto)
    {
        var recortado = texto?.Trim();
        return string.IsNullOrEmpty(recortado) ? null : recortado;
    }

    /// <summary>Normaliza y pasa a minúsculas invariantes (emails, opciones de query).</summary>
    public static string? NormalizarMinusculas(string? texto) =>
        Normalizar(texto)?.ToLowerInvariant();
}
