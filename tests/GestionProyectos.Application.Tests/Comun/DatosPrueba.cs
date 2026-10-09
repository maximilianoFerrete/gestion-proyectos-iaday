using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Entidades;
using Microsoft.Extensions.Time.Testing;

namespace GestionProyectos.Application.Tests.Comun;

/// <summary>
/// Constructores de entidades, DTOs y reloj falso para los tests de Application.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// Modificado: 2026-10-09 - ProyectoDto con CantidadIncidencias (RF-002)
/// </remarks>
internal static class DatosPrueba
{
    /// <summary>Instante fijo del reloj falso (con milisegundos para probar el truncado).</summary>
    public static readonly DateTimeOffset Ahora = new(2026, 10, 9, 14, 30, 15, 789, TimeSpan.Zero);

    /// <summary>Instante esperado tras truncar a segundos (DATETIME2(0)).</summary>
    public static readonly DateTime AhoraTruncado = new(2026, 10, 9, 14, 30, 15, DateTimeKind.Utc);

    /// <summary>Fecha de creación usada en las entidades reconstituidas.</summary>
    public static readonly DateTime FechaAnterior = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

    public static readonly DateOnly FechaInicio = new(2026, 10, 1);
    public static readonly DateOnly FechaObjetivo = new(2026, 12, 15);

    /// <summary>Crea un reloj falso fijado en <see cref="Ahora"/>.</summary>
    public static FakeTimeProvider Reloj() => new(Ahora);

    /// <summary>Crea una persona persistida.</summary>
    public static Persona Persona(int id, bool esAdmin = false, bool activo = true,
        string email = "persona@empresa.com") =>
        Domain.Entidades.Persona.Reconstituir(id, $"Persona {id}", email, esAdmin, activo,
            FechaAnterior, null);

    /// <summary>Crea un estado de proyecto persistido.</summary>
    public static EstadoProyecto Estado(int id, string nombre = "En curso", int orden = 30,
        bool esInicial = false, bool esFinal = false, bool activo = true, string? codigo = null) =>
        EstadoProyecto.Reconstituir(id, nombre, null, orden, esInicial, esFinal, activo, codigo,
            FechaAnterior, null);

    /// <summary>Crea el estado de sistema "Archivado".</summary>
    public static EstadoProyecto EstadoArchivado(int id = 7) =>
        Estado(id, "Archivado", 9999, codigo: CodigosEstado.Archivado);

    /// <summary>Crea un proyecto persistido, opcionalmente archivado.</summary>
    public static Proyecto Proyecto(int id, int estadoId = 3, int responsableId = 2,
        bool archivado = false, string codigo = "PRJ-0003")
    {
        var datos = new DatosProyecto("Portal clientes", "Nuevo portal de autogestión.", responsableId,
            FechaInicio, FechaObjetivo);
        var archivo = archivado ? new DatosArchivado(FechaAnterior, 1, 5) : null;
        return Domain.Entidades.Proyecto.Reconstituir(id, codigo, datos, estadoId, archivo,
            FechaAnterior, null);
    }

    /// <summary>Crea una agrupación persistida.</summary>
    public static Agrupacion Agrupacion(int id, int proyectoId, string nombre = "Módulo pagos") =>
        Domain.Entidades.Agrupacion.Reconstituir(id, proyectoId, nombre, "Incidencias de pagos.",
            FechaAnterior, null);

    /// <summary>Crea un ProyectoDto mínimo con el id y código indicados.</summary>
    public static ProyectoDto ProyectoDto(int id, string codigo = "PRJ-0003") =>
        new(id, codigo, "Portal clientes", "Nuevo portal de autogestión.",
            new PersonaReferenciaDto(2, "Ana Gómez", true), FechaInicio, FechaObjetivo,
            new EstadoReferenciaDto(1, "En planificación", false, true, false), false, null, null, null,
            0, FechaAnterior, null, 0);

    /// <summary>Texto de la longitud indicada.</summary>
    public static string Texto(int largo) => new('a', largo);
}
