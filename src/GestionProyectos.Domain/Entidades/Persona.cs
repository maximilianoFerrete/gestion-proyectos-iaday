namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Persona del catálogo: responsable de proyectos y usuario actual (provisional).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class Persona : Entidad
{
    public string Nombre { get; private set; }

    public string Email { get; private set; }

    public bool EsAdmin { get; private set; }

    public bool Activo { get; private set; }

    /// <summary>Crea una persona nueva y activa.</summary>
    public Persona(string nombre, string email, bool esAdmin, DateTime ahora) : base(ahora)
    {
        Nombre = nombre;
        Email = email;
        EsAdmin = esAdmin;
        Activo = true;
    }

    private Persona(int id, string nombre, string email, bool esAdmin, bool activo,
        DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        Nombre = nombre;
        Email = email;
        EsAdmin = esAdmin;
        Activo = activo;
    }

    /// <summary>Reconstruye una persona leída desde la persistencia.</summary>
    public static Persona Reconstituir(int id, string nombre, string email, bool esAdmin,
        bool activo, DateTime fechaCreacion, DateTime? fechaModificacion) =>
        new(id, nombre, email, esAdmin, activo, fechaCreacion, fechaModificacion);

    /// <summary>Indica si es un administrador activo (cuenta para RN-06).</summary>
    public bool EsAdminActivo => EsAdmin && Activo;

    /// <summary>Actualiza nombre, email y marca de administrador.</summary>
    public void Actualizar(string nombre, string email, bool esAdmin, DateTime ahora)
    {
        Nombre = nombre;
        Email = email;
        EsAdmin = esAdmin;
        MarcarModificada(ahora);
    }

    /// <summary>Desactiva la persona; no hace nada si ya estaba inactiva.</summary>
    public void Desactivar(DateTime ahora)
    {
        if (!Activo)
        {
            return;
        }

        Activo = false;
        MarcarModificada(ahora);
    }

    /// <summary>Activa la persona; no hace nada si ya estaba activa.</summary>
    public void Activar(DateTime ahora)
    {
        if (Activo)
        {
            return;
        }

        Activo = true;
        MarcarModificada(ahora);
    }
}
