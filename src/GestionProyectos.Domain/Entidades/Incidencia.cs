using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Entidades;

/// <summary>
/// Datos editables de una incidencia (objeto de valor inmutable).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record DatosIncidencia(
    string Titulo,
    string Descripcion,
    int PrioridadId,
    int? ResponsableId,
    DateOnly? FechaVencimiento);

/// <summary>
/// Posición de una incidencia en la jerarquía: padre y agrupación (objeto de valor inmutable).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record UbicacionIncidencia(int? PadreId, int? AgrupacionId);

/// <summary>
/// Incidencia de un proyecto, con número y código inmutables (RN-18, RN-35).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class Incidencia : Entidad
{
    public int ProyectoId { get; }

    public int Numero { get; }

    public string Codigo { get; }

    public int TipoId { get; private set; }

    public int EstadoId { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string Descripcion { get; private set; } = string.Empty;

    public int PrioridadId { get; private set; }

    public int? ResponsableId { get; private set; }

    public DateOnly? FechaVencimiento { get; private set; }

    public int? AgrupacionId { get; private set; }

    public int? PadreId { get; private set; }

    /// <summary>Crea una incidencia con su número, código y estado inicial (RN-18, RN-20).</summary>
    public Incidencia(int proyectoId, int numero, string codigo, int tipoId, int estadoId,
        DatosIncidencia datos, UbicacionIncidencia ubicacion, DateTime ahora) : base(ahora)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proyectoId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numero);
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ProyectoId = proyectoId;
        Numero = numero;
        Codigo = codigo;
        TipoId = tipoId;
        EstadoId = estadoId;
        AplicarDatos(datos);
        AplicarUbicacion(ubicacion);
    }

    private Incidencia(int id, int proyectoId, int numero, string codigo, DateTime fechaCreacion,
        DateTime? fechaModificacion) : base(id, fechaCreacion, fechaModificacion)
    {
        ProyectoId = proyectoId;
        Numero = numero;
        Codigo = codigo;
    }

    /// <summary>Reconstruye una incidencia leída desde la persistencia.</summary>
    public static Incidencia Reconstituir(int id, int proyectoId, int numero, string codigo, int tipoId,
        int estadoId, DatosIncidencia datos, UbicacionIncidencia ubicacion, DateTime fechaCreacion,
        DateTime? fechaModificacion)
    {
        var incidencia = new Incidencia(id, proyectoId, numero, codigo, fechaCreacion, fechaModificacion)
        {
            TipoId = tipoId,
            EstadoId = estadoId,
        };
        incidencia.AplicarDatos(datos);
        incidencia.AplicarUbicacion(ubicacion);
        return incidencia;
    }

    /// <summary>Indica si pertenece al proyecto indicado (RN-37).</summary>
    public bool PerteneceA(int proyectoId) => ProyectoId == proyectoId;

    /// <summary>Reemplaza datos, estado, padre y agrupación (edición; RN-23, RN-26, RN-28).</summary>
    public void Actualizar(DatosIncidencia datos, int estadoId, UbicacionIncidencia ubicacion, DateTime ahora)
    {
        ArgumentNullException.ThrowIfNull(ubicacion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(estadoId);
        if (ubicacion.PadreId is { } padreId && padreId == Id)
        {
            throw new ConflictoException(CodigosError.RelacionCircular,
                "Una incidencia no puede ser su propio padre.");
        }

        AplicarDatos(datos);
        AplicarUbicacion(ubicacion);
        EstadoId = estadoId;
        MarcarModificada(ahora);
    }

    /// <summary>Cambia el tipo y el estado resuelto para el nuevo flujo (RN-23).</summary>
    public void CambiarTipo(int tipoId, int estadoId, DateTime ahora)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tipoId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(estadoId);
        TipoId = tipoId;
        EstadoId = estadoId;
        MarcarModificada(ahora);
    }

    private void AplicarDatos(DatosIncidencia datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        Titulo = datos.Titulo;
        Descripcion = datos.Descripcion;
        PrioridadId = datos.PrioridadId;
        ResponsableId = datos.ResponsableId;
        FechaVencimiento = datos.FechaVencimiento;
    }

    private void AplicarUbicacion(UbicacionIncidencia ubicacion)
    {
        ArgumentNullException.ThrowIfNull(ubicacion);
        PadreId = ubicacion.PadreId;
        AgrupacionId = ubicacion.AgrupacionId;
    }
}
