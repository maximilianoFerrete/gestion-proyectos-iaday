using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Proyecto con código legible inmutable; al archivarse queda en solo lectura (RN-21, RN-22).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class Proyecto : Entidad
{
    public string Codigo { get; }

    public string Nombre { get; private set; }

    public string Descripcion { get; private set; }

    public int ResponsableId { get; private set; }

    public DateOnly FechaInicio { get; private set; }

    public DateOnly FechaObjetivo { get; private set; }

    public int EstadoId { get; private set; }

    public DateTime? FechaArchivado { get; private set; }

    public int? ArchivadoPorId { get; private set; }

    public int? EstadoAlArchivarId { get; private set; }

    /// <summary>Indica si el proyecto está archivado (FechaArchivado no nula).</summary>
    public bool EstaArchivado => FechaArchivado is not null;

    /// <summary>Crea un proyecto nuevo con su código y el estado inicial (RN-14, RN-18).</summary>
    public Proyecto(string codigo, DatosProyecto datos, int estadoInicialId, DateTime ahora)
        : base(ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentNullException.ThrowIfNull(datos);
        Codigo = codigo;
        Nombre = datos.Nombre;
        Descripcion = datos.Descripcion;
        ResponsableId = datos.ResponsableId;
        FechaInicio = datos.FechaInicio;
        FechaObjetivo = datos.FechaObjetivo;
        EstadoId = estadoInicialId;
    }

    private Proyecto(int id, string codigo, DatosProyecto datos, int estadoId, DateTime fechaCreacion,
        DateTime? fechaModificacion) : base(id, fechaCreacion, fechaModificacion)
    {
        Codigo = codigo;
        Nombre = datos.Nombre;
        Descripcion = datos.Descripcion;
        ResponsableId = datos.ResponsableId;
        FechaInicio = datos.FechaInicio;
        FechaObjetivo = datos.FechaObjetivo;
        EstadoId = estadoId;
    }

    /// <summary>Reconstruye un proyecto leído desde la persistencia.</summary>
    public static Proyecto Reconstituir(int id, string codigo, DatosProyecto datos, int estadoId,
        DatosArchivado? archivado, DateTime fechaCreacion, DateTime? fechaModificacion)
    {
        ArgumentNullException.ThrowIfNull(datos);
        var proyecto = new Proyecto(id, codigo, datos, estadoId, fechaCreacion, fechaModificacion);
        if (archivado is not null)
        {
            proyecto.FechaArchivado = archivado.Fecha;
            proyecto.ArchivadoPorId = archivado.ArchivadoPorId;
            proyecto.EstadoAlArchivarId = archivado.EstadoAlArchivarId;
        }

        return proyecto;
    }

    /// <summary>Lanza ProyectoArchivadoException si el proyecto está archivado (RN-22).</summary>
    public void AsegurarEditable()
    {
        if (EstaArchivado)
        {
            throw new ProyectoArchivadoException(Codigo);
        }
    }

    /// <summary>Edita los datos y el estado del proyecto; el código no cambia.</summary>
    public void Actualizar(DatosProyecto datos, int estadoId, DateTime ahora)
    {
        ArgumentNullException.ThrowIfNull(datos);
        AsegurarEditable();
        Nombre = datos.Nombre;
        Descripcion = datos.Descripcion;
        ResponsableId = datos.ResponsableId;
        FechaInicio = datos.FechaInicio;
        FechaObjetivo = datos.FechaObjetivo;
        EstadoId = estadoId;
        MarcarModificada(ahora);
    }

    /// <summary>Archiva el proyecto conservando el estado que tenía (RN-21).</summary>
    public void Archivar(int estadoArchivadoId, int archivadoPorId, DateTime ahora)
    {
        AsegurarEditable();
        EstadoAlArchivarId = EstadoId;
        EstadoId = estadoArchivadoId;
        FechaArchivado = ahora;
        ArchivadoPorId = archivadoPorId;
        MarcarModificada(ahora);
    }
}
