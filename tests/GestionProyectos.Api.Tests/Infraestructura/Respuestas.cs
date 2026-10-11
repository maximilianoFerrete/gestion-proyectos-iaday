using System.Text.Json;
using GestionProyectos.Application.Agrupaciones;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.EstadosProyecto;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Personas;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Application.RelacionesTiposIncidencia;
using GestionProyectos.Application.TiposIncidencia;

namespace GestionProyectos.Api.Tests.Infraestructura;

/// <summary>
/// ProblemDetails leído de una respuesta HTTP (campos usados en las aserciones).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// Modificado: 2026-10-10 - Lee la extensión "incidencias" (tests RF-002, 0.2)
/// </remarks>
public sealed record Problema(int Status, string? Codigo, string? Detail, string? Title,
    IReadOnlyDictionary<string, string[]> Errors)
{
    /// <summary>Extensión "incidencias" (códigos afectados) o null si no viene.</summary>
    public string[]? Incidencias { get; init; }

    /// <summary>Lee el cuerpo ProblemDetails de la respuesta.</summary>
    public static async Task<Problema> LeerAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        var errores = raiz.TryGetProperty("errors", out var e)
            ? e.Deserialize<Dictionary<string, string[]>>() ?? []
            : [];
        return new Problema(raiz.GetProperty("status").GetInt32(), Texto(raiz, "codigo"),
            Texto(raiz, "detail"), Texto(raiz, "title"), errores)
        {
            Incidencias = raiz.TryGetProperty("incidencias", out var i) ? i.Deserialize<string[]>() : null,
        };
    }

    private static string? Texto(JsonElement raiz, string propiedad) =>
        raiz.TryGetProperty(propiedad, out var valor) ? valor.GetString() : null;
}

/// <summary>
/// DTOs de ejemplo devueltos por los servicios sustitutos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// Modificado: 2026-10-09 - DTOs con CantidadIncidencias (RF-002)
/// Modificado: 2026-10-10 - DTOs de incidencias y tipos (tests RF-002)
/// </remarks>
public static class Dtos
{
    private static readonly DateTime Creado = new(2026, 10, 9, 13, 5, 0, DateTimeKind.Utc);

    /// <summary>JSON camelCase como el de la API.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Proyecto de ejemplo.</summary>
    public static ProyectoDto Proyecto(int id = 3, bool archivado = false) =>
        new(id, $"PRJ-{id:D4}", "Portal clientes", "Nuevo portal de autogestión.",
            new PersonaReferenciaDto(2, "Ana Gómez", true), new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 15),
            new EstadoReferenciaDto(1, "En planificación", false, true, false), archivado, null, null, null, 0,
            Creado, null, 0);

    /// <summary>Agrupación de ejemplo.</summary>
    public static AgrupacionDto Agrupacion(int id = 8, int proyectoId = 3) =>
        new(id, proyectoId, "Módulo pagos", "Incidencias del circuito de pagos.", Creado, null, 0);

    /// <summary>Persona de ejemplo.</summary>
    public static PersonaDto Persona(int id = 2, bool activo = true) =>
        new(id, "Ana Gómez", "ana.gomez@empresa.com", false, activo, Creado, null);

    /// <summary>Incidencia de ejemplo del proyecto indicado.</summary>
    public static IncidenciaDto Incidencia(int id = 16, int proyectoId = 1) =>
        new(id, $"PRJ-0001-{id}", id, new ProyectoReferenciaIncidenciaDto(proyectoId, "PRJ-0001", "Portal", false),
            "Validar tarjeta", "Validar número.", new TipoReferenciaDto(3, "Subtarea", true, 3),
            new EstadoIncidenciaReferenciaDto(1, "Por hacer", true, false), new PrioridadReferenciaDto(2, "Media", 20, true),
            null, new DateOnly(2026, 11, 20), null, false, null, [], [], Creado, null);

    /// <summary>Tipo de incidencia de ejemplo.</summary>
    public static TipoIncidenciaDto Tipo(int id = 6) =>
        new(id, "Spike", null, new NivelReferenciaDto(3, 3, "Nivel 3"), new FlujoReferenciaDto(1, "Flujo general"),
            true, 0, Creado, null);

    /// <summary>Relación de tipos de ejemplo.</summary>
    public static RelacionTipoIncidenciaDto Relacion(int id = 7) =>
        new(id, new TipoReferenciaDto(2, "Historia de Usuario", true, 2), new TipoReferenciaDto(6, "Spike", true, 3),
            Creado);

    /// <summary>Estado de ejemplo.</summary>
    public static EstadoProyectoDto Estado(int id = 2, bool esInicial = false, bool activo = true) =>
        new(id, "Planificado", null, 20, esInicial, false, activo, false, null, 0);
}
