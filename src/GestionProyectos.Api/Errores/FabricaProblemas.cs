using System.Diagnostics;
using System.Text.Json;
using GestionProyectos.Domain.Excepciones;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Errores;

/// <summary>
/// Construye ProblemDetails con el formato de la spec (type, title, codigo, traceId, instance).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Copia las extensiones de la excepción de negocio (RF-002, 0.2)
/// </remarks>
public sealed class FabricaProblemas
{
    private const string BaseTipo = "https://httpstatuses.io/";

    private static readonly IReadOnlyDictionary<int, string> Titulos = new Dictionary<int, string>
    {
        [StatusCodes.Status400BadRequest] = "Solicitud inválida",
        [StatusCodes.Status401Unauthorized] = "No autenticado",
        [StatusCodes.Status403Forbidden] = "Permiso denegado",
        [StatusCodes.Status404NotFound] = "No encontrado",
        [StatusCodes.Status409Conflict] = "Conflicto",
        [StatusCodes.Status500InternalServerError] = "Error interno",
    };

    /// <summary>Crea un ProblemDetails de negocio con su código del catálogo.</summary>
    public ProblemDetails Crear(HttpContext contexto, int status, string codigo, string detalle)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var problema = new ProblemDetails
        {
            Type = BaseTipo + status,
            Title = Titulos.GetValueOrDefault(status, "Error"),
            Status = status,
            Detail = detalle,
        };
        return Completar(problema, contexto, codigo);
    }

    /// <summary>Crea el ProblemDetails de una excepción de negocio, con sus extensiones.</summary>
    public ProblemDetails Crear(HttpContext contexto, int status, ExcepcionDominio excepcion)
    {
        ArgumentNullException.ThrowIfNull(excepcion);
        var problema = Crear(contexto, status, excepcion.Codigo, excepcion.Message);
        foreach (var (clave, valor) in excepcion.Extensiones)
        {
            problema.Extensions.TryAdd(clave, valor);
        }

        return problema;
    }

    /// <summary>Crea un ValidationProblemDetails (400 VALIDACION) con claves en camelCase.</summary>
    public ValidationProblemDetails CrearValidacion(HttpContext contexto,
        IEnumerable<KeyValuePair<string, string>> errores)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(errores);
        var agrupados = errores
            .GroupBy(e => NormalizarClave(e.Key), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Value).Distinct().ToArray(), StringComparer.Ordinal);

        var problema = new ValidationProblemDetails(agrupados)
        {
            Type = BaseTipo + StatusCodes.Status400BadRequest,
            Title = "Error de validación",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Uno o más campos tienen valores inválidos.",
        };
        Completar(problema, contexto, CodigosError.Validacion);
        return problema;
    }

    /// <summary>Normaliza la clave de un campo: quita el prefijo JSON "$." y pasa a camelCase.</summary>
    public static string NormalizarClave(string clave)
    {
        var sinPrefijo = (clave ?? string.Empty).TrimStart('$').TrimStart('.');
        return sinPrefijo.Length == 0 ? string.Empty : JsonNamingPolicy.CamelCase.ConvertName(sinPrefijo);
    }

    private static ProblemDetails Completar(ProblemDetails problema, HttpContext contexto, string codigo)
    {
        problema.Instance = contexto.Request.Path;
        problema.Extensions["codigo"] = codigo;
        problema.Extensions["traceId"] = Activity.Current?.Id ?? contexto.TraceIdentifier;
        return problema;
    }
}
