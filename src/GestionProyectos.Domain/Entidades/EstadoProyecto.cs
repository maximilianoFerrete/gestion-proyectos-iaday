using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Estado configurable de proyecto o estado de sistema (Codigo no nulo, p. ej. ARCHIVADO).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class EstadoProyecto : Entidad
{
    public string Nombre { get; private set; }

    public string? Descripcion { get; private set; }

    public int Orden { get; private set; }

    public bool EsInicial { get; private set; }

    public bool EsFinal { get; private set; }

    public bool Activo { get; private set; }

    public string? Codigo { get; private set; }

    /// <summary>Indica si es un estado de sistema (no configurable por el usuario).</summary>
    public bool EsSistema => Codigo is not null;

    /// <summary>Indica si puede asignarse a un proyecto por edición (RN-13, RN-19).</summary>
    public bool EsAsignable => Activo && !EsSistema;

    /// <summary>Crea un estado configurable activo y no inicial.</summary>
    public EstadoProyecto(string nombre, string? descripcion, int orden, bool esFinal, DateTime ahora)
        : base(ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        EsFinal = esFinal;
        Activo = true;
    }

    private EstadoProyecto(int id, string nombre, string? descripcion, int orden, bool esInicial,
        bool esFinal, bool activo, string? codigo, DateTime fechaCreacion, DateTime? fechaModificacion)
        : base(id, fechaCreacion, fechaModificacion)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        EsInicial = esInicial;
        EsFinal = esFinal;
        Activo = activo;
        Codigo = codigo;
    }

    /// <summary>Reconstruye un estado leído desde la persistencia.</summary>
    public static EstadoProyecto Reconstituir(int id, string nombre, string? descripcion, int orden,
        bool esInicial, bool esFinal, bool activo, string? codigo, DateTime fechaCreacion,
        DateTime? fechaModificacion) =>
        new(id, nombre, descripcion, orden, esInicial, esFinal, activo, codigo,
            fechaCreacion, fechaModificacion);

    /// <summary>Edita los datos del estado (RN-09, RN-12).</summary>
    public void Actualizar(string nombre, string? descripcion, int orden, bool esFinal, DateTime ahora)
    {
        AsegurarNoSistema();
        if (EsInicial && esFinal)
        {
            throw new ConflictoException(CodigosError.EstadoInicialNoFinal,
                "El estado inicial no puede marcarse como final.");
        }

        Nombre = nombre;
        Descripcion = descripcion;
        Orden = orden;
        EsFinal = esFinal;
        MarcarModificada(ahora);
    }

    /// <summary>Valida que pueda marcarse inicial (RN-09, RN-11) sin modificarlo.</summary>
    public void AsegurarElegibleComoInicial()
    {
        AsegurarNoSistema();
        if (!Activo || EsFinal)
        {
            throw new ConflictoException(CodigosError.EstadoNoElegibleInicial,
                $"El estado '{Nombre}' no puede ser inicial: debe estar activo y no ser final.");
        }
    }

    /// <summary>Marca el estado como inicial (RN-10, RN-11).</summary>
    public void MarcarInicial(DateTime ahora)
    {
        AsegurarElegibleComoInicial();
        EsInicial = true;
        MarcarModificada(ahora);
    }

    /// <summary>Quita la marca de inicial; solo al marcar otro estado como inicial (RN-10).</summary>
    public void DesmarcarInicial(DateTime ahora)
    {
        EsInicial = false;
        MarcarModificada(ahora);
    }

    /// <summary>Desactiva el estado (RN-09, RN-12); idempotente.</summary>
    public void Desactivar(DateTime ahora)
    {
        AsegurarNoSistema();
        if (EsInicial)
        {
            throw new ConflictoException(CodigosError.EstadoInicialNoDesactivable,
                "El estado inicial no puede desactivarse.");
        }

        if (!Activo)
        {
            return;
        }

        Activo = false;
        MarcarModificada(ahora);
    }

    /// <summary>Activa el estado (RN-09); idempotente.</summary>
    public void Activar(DateTime ahora)
    {
        AsegurarNoSistema();
        if (Activo)
        {
            return;
        }

        Activo = true;
        MarcarModificada(ahora);
    }

    private void AsegurarNoSistema()
    {
        if (EsSistema)
        {
            throw new ConflictoException(CodigosError.EstadoSistemaNoModificable,
                $"El estado de sistema '{Nombre}' no puede modificarse.");
        }
    }
}
