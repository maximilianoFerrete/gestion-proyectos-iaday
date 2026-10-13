using FluentValidation;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Límites de formato de las incidencias (RN-19, DA-06).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Límites de la estimación (RF-005, RN-34)
/// </remarks>
public static class LimitesIncidencia
{
    public const int LargoMaximoTitulo = 200;
    public const int LargoMaximoDescripcion = 4000;
    public const decimal EstimacionMaxima = 9999999.99m;
    public const int DecimalesEstimacion = 2;
}

/// <summary>
/// Regla de FluentValidation de la estimación en horas (RF-005 RN-34).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public static class ValidacionEstimacionExtensions
{
    /// <summary>Opcional, no negativa, hasta 2 decimales y hasta 9.999.999,99.</summary>
    public static IRuleBuilderOptions<T, decimal?> Estimacion<T>(this IRuleBuilder<T, decimal?> regla) =>
        regla
            .Must(v => v is null || v >= 0)
            .WithMessage("La estimación no puede ser negativa.")
            .Must(v => v is null || decimal.Round(v.Value, LimitesIncidencia.DecimalesEstimacion) == v.Value)
            .WithMessage("La estimación admite hasta 2 decimales.")
            .Must(v => v is null || v <= LimitesIncidencia.EstimacionMaxima)
            .WithMessage("La estimación no puede superar 9.999.999,99 horas.");
}

/// <summary>
/// Validación de formato del alta de incidencia (RN-19).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Valida la estimación (RF-005, RN-34)
/// </remarks>
public class CrearIncidenciaRequestValidator : AbstractValidator<CrearIncidenciaRequest>
{
    /// <summary>Define las reglas de tipo, título, descripción y estimación.</summary>
    public CrearIncidenciaRequestValidator()
    {
        RuleFor(x => x.TipoId)
            .NotNull().WithMessage("El tipo es obligatorio.")
            .GreaterThan(0).WithMessage("El tipo es obligatorio.");
        RuleFor(x => x.Titulo).TextoObligatorio("El título", LimitesIncidencia.LargoMaximoTitulo);
        RuleFor(x => x.Descripcion)
            .TextoObligatorio("La descripción", LimitesIncidencia.LargoMaximoDescripcion, femenino: true);
        RuleFor(x => x.Estimacion).Estimacion();
    }
}

/// <summary>
/// Validación de formato de la edición de incidencia (RN-19, RN-21, RN-23).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Valida la estimación (RF-005, RN-34)
/// </remarks>
public class EditarIncidenciaRequestValidator : AbstractValidator<EditarIncidenciaRequest>
{
    /// <summary>Define las reglas de título, descripción, estado, prioridad y estimación.</summary>
    public EditarIncidenciaRequestValidator()
    {
        RuleFor(x => x.Titulo).TextoObligatorio("El título", LimitesIncidencia.LargoMaximoTitulo);
        RuleFor(x => x.Descripcion)
            .TextoObligatorio("La descripción", LimitesIncidencia.LargoMaximoDescripcion, femenino: true);
        RuleFor(x => x.EstadoId)
            .NotNull().WithMessage("El estado es obligatorio.")
            .GreaterThan(0).WithMessage("El estado es obligatorio.");
        RuleFor(x => x.PrioridadId)
            .NotNull().WithMessage("La prioridad es obligatoria.")
            .GreaterThan(0).WithMessage("La prioridad es obligatoria.");
        RuleFor(x => x.Estimacion).Estimacion();
    }
}

/// <summary>
/// Validación de formato del cambio de tipo (RN-02, RN-23).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class CambiarTipoIncidenciaRequestValidator : AbstractValidator<CambiarTipoIncidenciaRequest>
{
    /// <summary>Define la regla de tipo obligatorio.</summary>
    public CambiarTipoIncidenciaRequestValidator()
    {
        RuleFor(x => x.TipoId)
            .NotNull().WithMessage("El tipo es obligatorio.")
            .GreaterThan(0).WithMessage("El tipo es obligatorio.");
    }
}

/// <summary>
/// Validación de los parámetros del listado de incidencias (CA-19).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Admite ordenarPor=estimacion (RF-005)
/// </remarks>
public class ListarIncidenciasQueryValidator : AbstractValidator<ListarIncidenciasQuery>
{
    /// <summary>Define las reglas de cada parámetro de query.</summary>
    public ListarIncidenciasQueryValidator()
    {
        RuleFor(x => x.Texto).TextoOpcional("El texto", OpcionesListadoIncidencias.LargoMaximoTexto);

        RuleFor(x => x.ResponsableId)
            .Null()
            .When(x => x.SinResponsable == true)
            .WithMessage("responsableId y sinResponsable son excluyentes.");

        RuleFor(x => x.AgrupacionId)
            .Null()
            .When(x => x.SinAgrupacion == true)
            .WithMessage("agrupacionId y sinAgrupacion son excluyentes.");

        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.TamanioPagina)
            .InclusiveBetween(1, OpcionesListadoIncidencias.TamanioPaginaMaximo)
            .WithMessage($"El tamaño de página debe estar entre 1 y {OpcionesListadoIncidencias.TamanioPaginaMaximo}.");

        RuleFor(x => x.OrdenarPor)
            .Must(v => v is null || OpcionesListadoIncidencias.Ordenes.ContainsKey(v))
            .WithMessage("Valores permitidos para ordenarPor: numero, titulo, tipo, estado, prioridad, "
                + "responsable, fechaVencimiento, fechaCreacion, estimacion.");

        RuleFor(x => x.Direccion)
            .Must(v => v is null or OpcionesListadoIncidencias.DireccionAsc or OpcionesListadoIncidencias.DireccionDesc)
            .WithMessage("Valores permitidos para direccion: asc, desc.");
    }
}

/// <summary>
/// Validación de los parámetros del árbol de incidencias: ámbito excluyente (RF-005, C-RF002-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class ArbolIncidenciasQueryValidator : AbstractValidator<ArbolIncidenciasQuery>
{
    /// <summary>Define la regla de agrupacionId y sinAgrupacion excluyentes.</summary>
    public ArbolIncidenciasQueryValidator()
    {
        RuleFor(x => x.AgrupacionId)
            .Null()
            .When(x => x.SinAgrupacion == true)
            .WithMessage("agrupacionId y sinAgrupacion son excluyentes.");
    }
}
