using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;
using NSubstitute;

namespace GestionProyectos.Application.Tests.Comun;

/// <summary>
/// Constructores de entidades de incidencias y su configuración para los tests de RF-002.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
internal static class DatosIncidencias
{
    public const int FlujoGeneral = 1;
    public const int FlujoBug = 2;
    public const int PorHacer = 1;
    public const int EnProgreso = 2;
    public const int Hecho = 3;
    public const int Nuevo = 4;

    /// <summary>Flujo general: Por hacer (inicial) → En progreso → Hecho (final).</summary>
    public static FlujoIncidencia FlujoGeneralConEstados() =>
        FlujoIncidencia.Reconstituir(FlujoGeneral, "Flujo general", null,
            [new EstadoEnFlujo(PorHacer, 1, true, false), new EstadoEnFlujo(EnProgreso, 2, false, false),
                new EstadoEnFlujo(Hecho, 3, false, true)], DatosPrueba.FechaAnterior, null);

    /// <summary>Flujo de bugs: Nuevo (inicial) → Hecho (final); no incluye Por hacer ni En progreso.</summary>
    public static FlujoIncidencia FlujoBugConEstados() =>
        FlujoIncidencia.Reconstituir(FlujoBug, "Flujo bug", null,
            [new EstadoEnFlujo(Nuevo, 1, true, false), new EstadoEnFlujo(Hecho, 2, false, true)],
            DatosPrueba.FechaAnterior, null);

    /// <summary>Crea un estado de incidencia persistido.</summary>
    public static EstadoIncidencia Estado(int id, string nombre = "Estado", bool activo = true, int orden = 10) =>
        EstadoIncidencia.Reconstituir(id, nombre, null, orden, activo, DatosPrueba.FechaAnterior, null);

    /// <summary>Crea un tipo de incidencia persistido.</summary>
    public static TipoIncidencia Tipo(int id, string nombre = "Tipo", int nivelId = 2, int flujoId = FlujoGeneral,
        bool activo = true) =>
        TipoIncidencia.Reconstituir(id, nombre, null, nivelId, flujoId, activo, DatosPrueba.FechaAnterior, null);

    /// <summary>Crea un nivel persistido.</summary>
    public static NivelIncidencia Nivel(int id, int numero) =>
        NivelIncidencia.Reconstituir(id, numero, $"Nivel {numero}", null, DatosPrueba.FechaAnterior, null);

    /// <summary>Crea una prioridad persistida.</summary>
    public static PrioridadIncidencia Prioridad(int id, string nombre = "Media", bool predeterminada = false,
        bool activo = true) =>
        PrioridadIncidencia.Reconstituir(id, nombre, null, id * 10, predeterminada, activo,
            DatosPrueba.FechaAnterior, null);

    /// <summary>Crea una incidencia persistida.</summary>
    public static Incidencia Incidencia(int id, int proyectoId = 1, int tipoId = 2, int estadoId = PorHacer,
        int prioridadId = 2, int? responsableId = null, DateOnly? vencimiento = null, int? agrupacionId = null,
        int? padreId = null, string? codigo = null) =>
        Domain.Entidades.Incidencia.Reconstituir(id, proyectoId, id, codigo ?? $"PRJ-0001-{id}", tipoId, estadoId,
            new DatosIncidencia("Título", "Descripción", prioridadId, responsableId, vencimiento),
            new UbicacionIncidencia(padreId, agrupacionId), DatosPrueba.FechaAnterior, null);

    /// <summary>Hija directa para las reglas de jerarquía.</summary>
    public static HijaIncidencia Hija(int id, int tipoId = 3, DateOnly? vencimiento = null) =>
        new(id, $"PRJ-0001-{id}", tipoId, vencimiento);

    /// <summary>Detalle mínimo de una incidencia del proyecto indicado.</summary>
    public static IncidenciaDto Dto(int id, int proyectoId = 1) =>
        new(id, $"PRJ-0001-{id}", id, new ProyectoReferenciaIncidenciaDto(proyectoId, "PRJ-0001", "Portal", false),
            "Título", "Descripción", new TipoReferenciaDto(2, "Historia de Usuario", true, 2),
            new EstadoIncidenciaReferenciaDto(PorHacer, "Por hacer", true, false),
            new PrioridadReferenciaDto(2, "Media", 20, true), null, null, null, false, null, [], [],
            DatosPrueba.FechaAnterior, null);

    /// <summary>Usuario anónimo.</summary>
    public static IUsuarioActual UsuarioAnonimo()
    {
        var usuario = Substitute.For<IUsuarioActual>();
        usuario.EstaIdentificado.Returns(false);
        usuario.PersonaId.Returns((int?)null);
        return usuario;
    }

    /// <summary>Usuario identificado con los permisos indicados.</summary>
    public static IUsuarioActual Usuario(params string[] permisos)
    {
        var usuario = Substitute.For<IUsuarioActual>();
        usuario.EstaIdentificado.Returns(true);
        usuario.PersonaId.Returns(1);
        usuario.Permisos.Returns(permisos);
        usuario.TienePermiso(Arg.Any<string>()).Returns(c => permisos.Contains(c.Arg<string>()));
        return usuario;
    }

    /// <summary>Usuario identificado con el permiso de configuración (RN-01).</summary>
    public static IUsuarioActual Configurador() => Usuario(Permisos.ConfiguracionIncidencias);
}
