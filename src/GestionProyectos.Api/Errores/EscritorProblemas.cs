using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Errores;

/// <summary>
/// Escribe ProblemDetails en la respuesta desde middlewares y handlers fuera de MVC.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed class EscritorProblemas(IProblemDetailsService servicio, FabricaProblemas fabrica)
{
    /// <summary>Escribe un error de negocio con su status y código.</summary>
    public Task EscribirAsync(HttpContext contexto, int status, string codigo, string detalle) =>
        EscribirAsync(contexto, fabrica.Crear(contexto, status, codigo, detalle));

    /// <summary>Escribe el ProblemDetails indicado usando su Status como código HTTP.</summary>
    public async Task EscribirAsync(HttpContext contexto, ProblemDetails problema, Exception? excepcion = null)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(problema);
        contexto.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;
        await servicio.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema,
            Exception = excepcion,
        });
    }
}
