using System.Text.Json;
using GestionProyectos.Application.Agrupaciones;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.EstadosProyecto;
using GestionProyectos.Application.Personas;
using GestionProyectos.Application.Proyectos;

namespace GestionProyectos.Api.Tests.Infraestructura;

/// <summary>
/// ProblemDetails leído de una respuesta HTTP (campos usados en las aserciones).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public sealed record Problema(int Status, string? Codigo, string? Detail, string? Title,
    IReadOnlyDictionary<string, string[]> Errors)
{
    /// <summary>Lee el cuerpo ProblemDetails de la respuesta.</summary>
    public static async Task<Problema> LeerAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        var errores = raiz.TryGetProperty("errors", out var e)
            ? e.Deserialize<Dictionary<string, string[]>>() ?? []
            : [];
        return new Problema(raiz.GetProperty("status").GetInt32(), Texto(raiz, "codigo"),
            Texto(raiz, "detail"), Texto(raiz, "title"), errores);
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
            Creado, null);

    /// <summary>Agrupación de ejemplo.</summary>
    public static AgrupacionDto Agrupacion(int id = 8, int proyectoId = 3) =>
        new(id, proyectoId, "Módulo pagos", "Incidencias del circuito de pagos.", Creado, null);

    /// <summary>Persona de ejemplo.</summary>
    public static PersonaDto Persona(int id = 2, bool activo = true) =>
        new(id, "Ana Gómez", "ana.gomez@empresa.com", false, activo, Creado, null);

    /// <summary>Estado de ejemplo.</summary>
    public static EstadoProyectoDto Estado(int id = 2, bool esInicial = false, bool activo = true) =>
        new(id, "Planificado", null, 20, esInicial, false, activo, false, null, 0);
}
