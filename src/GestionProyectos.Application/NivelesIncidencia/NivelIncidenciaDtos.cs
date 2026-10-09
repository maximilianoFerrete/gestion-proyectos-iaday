using FluentValidation;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.NivelesIncidencia;

/// <summary>
/// Nivel de incidencia expuesto por la API, con la cantidad de tipos que lo usan.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record NivelIncidenciaDto(
    int Id,
    int Numero,
    string Nombre,
    string? Descripcion,
    int CantidadTipos,
    DateTime FechaCreacion,
    DateTime? FechaModificacion);

/// <summary>
/// Datos para crear un nivel (RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record CrearNivelIncidenciaRequest(int? Numero, string? Nombre, string? Descripcion)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-39).</summary>
    public CrearNivelIncidenciaRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Datos para editar un nivel; el número no es editable (RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record EditarNivelIncidenciaRequest(string? Nombre, string? Descripcion)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-39).</summary>
    public EditarNivelIncidenciaRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Límites de formato de los niveles (RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public static class LimitesNivel
{
    public const int NumeroMinimo = 1;
    public const int NumeroMaximo = 99;
    public const int LargoMaximoNombre = 50;
    public const int LargoMaximoDescripcion = 250;
}

/// <summary>
/// Validación de formato del alta de nivel (RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class CrearNivelIncidenciaRequestValidator : AbstractValidator<CrearNivelIncidenciaRequest>
{
    /// <summary>Define las reglas de número, nombre y descripción.</summary>
    public CrearNivelIncidenciaRequestValidator()
    {
        RuleFor(x => x.Numero)
            .NotNull().WithMessage("El número es obligatorio.")
            .InclusiveBetween(LimitesNivel.NumeroMinimo, LimitesNivel.NumeroMaximo)
            .WithMessage($"El número debe estar entre {LimitesNivel.NumeroMinimo} y {LimitesNivel.NumeroMaximo}.");
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LimitesNivel.LargoMaximoNombre);
        RuleFor(x => x.Descripcion).TextoOpcional("La descripción", LimitesNivel.LargoMaximoDescripcion);
    }
}

/// <summary>
/// Validación de formato de la edición de nivel (RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class EditarNivelIncidenciaRequestValidator : AbstractValidator<EditarNivelIncidenciaRequest>
{
    /// <summary>Define las reglas de nombre y descripción.</summary>
    public EditarNivelIncidenciaRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LimitesNivel.LargoMaximoNombre);
        RuleFor(x => x.Descripcion).TextoOpcional("La descripción", LimitesNivel.LargoMaximoDescripcion);
    }
}
