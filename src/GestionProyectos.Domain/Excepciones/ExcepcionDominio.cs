namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// Base de la jerarquía de excepciones de negocio; cada una lleva un código del catálogo (0.3).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega Extensiones para ProblemDetails (RF-002, 0.2)
/// </remarks>
public abstract class ExcepcionDominio(
    string codigo,
    string mensaje,
    IReadOnlyDictionary<string, object?>? extensiones = null) : Exception(mensaje)
{
    private static readonly IReadOnlyDictionary<string, object?> SinExtensiones =
        new Dictionary<string, object?>();

    /// <summary>Código de error del catálogo (por ejemplo PROYECTO_ARCHIVADO).</summary>
    public string Codigo { get; } = codigo;

    /// <summary>Datos adicionales que se copian a ProblemDetails.Extensions (vacío por defecto).</summary>
    public IReadOnlyDictionary<string, object?> Extensiones { get; } = extensiones ?? SinExtensiones;
}
