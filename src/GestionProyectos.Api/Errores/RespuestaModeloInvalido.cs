using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GestionProyectos.Api.Errores;

/// <summary>
/// Respuesta 400 VALIDACION para errores de binding (JSON mal formado, tipos o fechas inválidas).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class RespuestaModeloInvalido
{
    private const string MensajeFormatoInvalido = "El valor tiene un formato inválido.";

    /// <summary>Crea el resultado 400 a partir del ModelState inválido.</summary>
    public static IActionResult Crear(ActionContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var fabrica = contexto.HttpContext.RequestServices.GetRequiredService<FabricaProblemas>();
        var problema = fabrica.CrearValidacion(contexto.HttpContext, Errores(contexto.ModelState));
        return new BadRequestObjectResult(problema)
        {
            ContentTypes = { "application/problem+json" },
        };
    }

    private static IEnumerable<KeyValuePair<string, string>> Errores(ModelStateDictionary modelState) =>
        modelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .SelectMany(e => e.Value!.Errors.Select(error => new KeyValuePair<string, string>(
                e.Key, Mensaje(e.Key, error))));

    private static string Mensaje(string clave, ModelError error) =>
        clave.StartsWith('$') || string.IsNullOrEmpty(error.ErrorMessage)
            ? MensajeFormatoInvalido
            : error.ErrorMessage;
}
