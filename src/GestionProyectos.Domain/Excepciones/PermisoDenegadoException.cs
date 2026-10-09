namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// El usuario actual no tiene permiso para la operación (se traduce a HTTP 403).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega el constructor por permiso (RF-002, 0.1)
/// </remarks>
public class PermisoDenegadoException : ExcepcionDominio
{
    /// <summary>La operación requiere un usuario administrador (RF-001).</summary>
    public PermisoDenegadoException()
        : base(CodigosError.PermisoDenegado, "La operación requiere un usuario administrador.")
    {
    }

    /// <summary>La operación requiere el permiso indicado (RF-002).</summary>
    public PermisoDenegadoException(string permiso)
        : base(CodigosError.PermisoDenegado, $"La operación requiere el permiso '{permiso}'.")
    {
        Permiso = permiso;
    }

    /// <summary>Permiso exigido, o null si se exigía ser administrador.</summary>
    public string? Permiso { get; }
}
