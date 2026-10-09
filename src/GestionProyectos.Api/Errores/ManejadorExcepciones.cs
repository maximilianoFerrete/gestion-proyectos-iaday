using FluentValidation;
using GestionProyectos.Domain.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Errores;

/// <summary>
/// Manejo global de excepciones: traduce la jerarquía de negocio a ProblemDetails (spec 0.3).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Incluye las extensiones de la excepción (RF-002, 0.2)
/// </remarks>
public sealed class ManejadorExcepciones(
    FabricaProblemas fabrica,
    EscritorProblemas escritor,
    ILogger<ManejadorExcepciones> logger) : IExceptionHandler
{
    private const string DetalleErrorInterno = "Ocurrió un error inesperado. Intente nuevamente más tarde.";

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var problema = Traducir(httpContext, exception);
        if (problema.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        await escritor.EscribirAsync(httpContext, problema, exception);
        return true;
    }

    /// <summary>Obtiene el código HTTP correspondiente a una excepción de negocio.</summary>
    public static int StatusPara(ExcepcionDominio excepcion) => excepcion switch
    {
        EntidadNoEncontradaException => StatusCodes.Status404NotFound,
        ValidacionNegocioException => StatusCodes.Status400BadRequest,
        ConflictoException => StatusCodes.Status409Conflict,
        UsuarioNoIdentificadoException or UsuarioInvalidoException => StatusCodes.Status401Unauthorized,
        PermisoDenegadoException => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };

    private ProblemDetails Traducir(HttpContext contexto, Exception excepcion) => excepcion switch
    {
        ValidationException validacion => fabrica.CrearValidacion(contexto,
            validacion.Errors.Select(e => new KeyValuePair<string, string>(e.PropertyName, e.ErrorMessage))),
        ExcepcionDominio negocio => fabrica.Crear(contexto, StatusPara(negocio), negocio),
        _ => fabrica.Crear(contexto, StatusCodes.Status500InternalServerError, CodigosError.ErrorInterno,
            DetalleErrorInterno),
    };
}
