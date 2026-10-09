namespace GestionProyectos.Infrastructure.SqlServer.Conexion;

/// <summary>
/// Utilidades puras para armar parámetros y leer valores de SQL Server.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal static class SqlUtil
{
    /// <summary>Patrón LIKE "contiene" con comodines escapados (usar con ESCAPE '\'); null si no hay texto.</summary>
    public static string? PatronContiene(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return null;
        }

        var escapado = texto
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal)
            .Replace("[", @"\[", StringComparison.Ordinal);
        return "%" + escapado + "%";
    }

    /// <summary>Marca como UTC una fecha leída de DATETIME2 (la BD guarda UTC).</summary>
    public static DateTime Utc(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

    /// <summary>Marca como UTC una fecha opcional leída de DATETIME2.</summary>
    public static DateTime? Utc(DateTime? fecha) => fecha is null ? null : Utc(fecha.Value);

    /// <summary>Convierte un valor DATE leído como DateTime a DateOnly.</summary>
    public static DateOnly Fecha(DateTime fecha) => DateOnly.FromDateTime(fecha);

    /// <summary>Convierte DateOnly a DateTime para enviarlo como parámetro DATE.</summary>
    public static DateTime Parametro(DateOnly fecha) => fecha.ToDateTime(TimeOnly.MinValue);
}
