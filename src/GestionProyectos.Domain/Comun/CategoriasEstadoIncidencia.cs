namespace GestionProyectos.Domain.Comun;

/// <summary>
/// Categorías de seguimiento de un estado de incidencia (columna EstadosIncidencia.Categoria).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, DA-01, RN-17)
/// </remarks>
public static class CategoriasEstadoIncidencia
{
    /// <summary>La incidencia no puede avanzar por un impedimento (RN-12).</summary>
    public const string Bloqueada = "BLOQUEADA";

    /// <summary>La incidencia espera algo externo (RN-13).</summary>
    public const string EnEspera = "EN_ESPERA";

    /// <summary>Todas las categorías admitidas.</summary>
    public static IReadOnlyList<string> Todas { get; } = [Bloqueada, EnEspera];

    /// <summary>Indica si el valor es null o una categoría admitida (sensible a mayúsculas).</summary>
    public static bool EsValida(string? categoria) => categoria is null || Todas.Contains(categoria);
}
