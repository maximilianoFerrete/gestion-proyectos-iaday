using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Parámetros comunes de alcance (niveles y tipos) de las consultas de seguimiento (RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public interface IConsultaAlcance
{
    int[]? NivelId { get; }

    int[]? TipoId { get; }
}

/// <summary>
/// Parámetros comunes de ámbito de las consultas de seguimiento de un proyecto (RN-01).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public interface IConsultaAmbito : IConsultaAlcance
{
    int? AgrupacionId { get; }

    bool? SinAgrupacion { get; }
}

/// <summary>
/// Parámetros de query del tablero general (sin defaults aplicados).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ConsultaTableroProyectosQuery(
    string? Texto = null,
    string? Archivados = null,
    int[]? NivelId = null,
    int[]? TipoId = null,
    int? Pagina = null,
    int? TamanioPagina = null,
    string? OrdenarPor = null,
    string? Direccion = null) : IConsultaAlcance
{
    /// <summary>Devuelve una copia con trim y opciones en minúsculas.</summary>
    public ConsultaTableroProyectosQuery Normalizar() => this with
    {
        Texto = TextoEntrada.Normalizar(Texto),
        Archivados = TextoEntrada.NormalizarMinusculas(Archivados),
        OrdenarPor = TextoEntrada.NormalizarMinusculas(OrdenarPor),
        Direccion = TextoEntrada.NormalizarMinusculas(Direccion),
    };
}

/// <summary>
/// Parámetros de query del resumen de seguimiento de un proyecto o agrupación.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ConsultaSeguimientoQuery(
    int? AgrupacionId = null,
    bool? SinAgrupacion = null,
    int[]? NivelId = null,
    int[]? TipoId = null) : IConsultaAmbito;

/// <summary>
/// Parámetros de query de la grilla del tablero: ámbito, alcance y filtros de RN-21.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ConsultaTableroIncidenciasQuery(
    int? AgrupacionId = null,
    bool? SinAgrupacion = null,
    int[]? NivelId = null,
    int[]? TipoId = null,
    string? Indicador = null,
    int? EstadoId = null,
    string? Texto = null) : IConsultaAmbito
{
    /// <summary>Devuelve una copia con el texto recortado y el indicador en minúsculas.</summary>
    public ConsultaTableroIncidenciasQuery Normalizar() => this with
    {
        Texto = TextoEntrada.Normalizar(Texto),
        Indicador = TextoEntrada.NormalizarMinusculas(Indicador),
    };
}

/// <summary>
/// Indicadores por los que se puede filtrar la grilla del tablero (RN-21).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public enum IndicadorSeguimiento
{
    Abiertas,
    Vencidas,
    Bloqueadas,
    EnEspera,
    SinResponsable,
}

/// <summary>
/// Columnas por las que se ordena el tablero general (lista blanca).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public enum OrdenTableroProyectos
{
    Codigo,
    Nombre,
    Total,
    Abiertas,
    Vencidas,
    Bloqueadas,
    EnEspera,
    SinResponsable,
}

/// <summary>
/// Valores admitidos y por defecto de los parámetros de seguimiento.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public static class OpcionesSeguimiento
{
    public const int LargoMaximoTextoProyectos = 150;
    public const int LargoMaximoTextoTablero = 200;
    public const string AmbitoProyecto = "proyecto";
    public const string AmbitoAgrupacion = "agrupacion";
    public const string AmbitoSinAgrupacion = "sinAgrupacion";
    public const string MensajeEstadoInexistente = "El estado no existe.";

    /// <summary>Valores de query admitidos para "indicador" (en minúsculas, canónicos).</summary>
    public static IReadOnlyDictionary<string, IndicadorSeguimiento> Indicadores { get; } =
        new Dictionary<string, IndicadorSeguimiento>
        {
            ["abiertas"] = IndicadorSeguimiento.Abiertas,
            ["vencidas"] = IndicadorSeguimiento.Vencidas,
            ["bloqueadas"] = IndicadorSeguimiento.Bloqueadas,
            ["en-espera"] = IndicadorSeguimiento.EnEspera,
            ["sin-responsable"] = IndicadorSeguimiento.SinResponsable,
        };

    /// <summary>Valores de query admitidos para "ordenarPor" del tablero general (en minúsculas).</summary>
    public static IReadOnlyDictionary<string, OrdenTableroProyectos> Ordenes { get; } =
        new Dictionary<string, OrdenTableroProyectos>
        {
            ["codigo"] = OrdenTableroProyectos.Codigo,
            ["nombre"] = OrdenTableroProyectos.Nombre,
            ["total"] = OrdenTableroProyectos.Total,
            ["abiertas"] = OrdenTableroProyectos.Abiertas,
            ["vencidas"] = OrdenTableroProyectos.Vencidas,
            ["bloqueadas"] = OrdenTableroProyectos.Bloqueadas,
            ["enespera"] = OrdenTableroProyectos.EnEspera,
            ["sinresponsable"] = OrdenTableroProyectos.SinResponsable,
        };
}
