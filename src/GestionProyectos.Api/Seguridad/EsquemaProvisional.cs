namespace GestionProyectos.Api.Seguridad;

/// <summary>
/// Constantes de autenticación y autorización (esquema, header, claims, rol y política).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega el claim de permisos (RF-002, 0.1)
/// </remarks>
public static class EsquemaProvisional
{
    /// <summary>Nombre del esquema de autenticación provisional (spec 0.4).</summary>
    public const string Nombre = "Provisional";

    /// <summary>Header con el id de la persona que actúa como usuario actual.</summary>
    public const string Header = "X-Usuario-Id";

    /// <summary>Política que exige rol administrador.</summary>
    public const string PoliticaSoloAdministradores = "SoloAdministradores";

    /// <summary>Rol de los administradores.</summary>
    public const string RolAdmin = "Admin";

    /// <summary>Claim con el id de la persona.</summary>
    public const string ClaimSub = "sub";

    /// <summary>Claim con el nombre de la persona.</summary>
    public const string ClaimNombre = "name";

    /// <summary>Claim con el email de la persona.</summary>
    public const string ClaimEmail = "email";

    /// <summary>Tipo de claim de los roles.</summary>
    public const string ClaimRol = "role";

    /// <summary>Tipo de claim de cada permiso del usuario (RF-002, 0.1).</summary>
    public const string ClaimPermiso = "permiso";
}

/// <summary>
/// Opciones de seguridad (sección "Seguridad" de la configuración).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed class OpcionesSeguridad
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string Seccion = "Seguridad";

    /// <summary>Habilita el header X-Usuario-Id. Solo desarrollo/demo: es falsificable.</summary>
    public bool ModoProvisional { get; set; }
}
