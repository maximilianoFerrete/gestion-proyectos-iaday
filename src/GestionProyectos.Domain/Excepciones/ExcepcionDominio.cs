namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// Base de la jerarquía de excepciones de negocio; cada una lleva un código del catálogo (0.3).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public abstract class ExcepcionDominio(string codigo, string mensaje) : Exception(mensaje)
{
    /// <summary>Código de error del catálogo (por ejemplo PROYECTO_ARCHIVADO).</summary>
    public string Codigo { get; } = codigo;
}
