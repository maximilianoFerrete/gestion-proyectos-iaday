using FluentValidation;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Límites de formato de las incidencias (RN-19, DA-06).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public static class LimitesIncidencia
{
    public const int LargoMaximoTitulo = 200;
    public const int LargoMaximoDescripcion = 4000;
}

/// <summary>
/// Validación de formato del alta de incidencia (RN-19).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class CrearIncidenciaRequestValidator : AbstractValidator<CrearIncidenciaRequest>
{
    /// <summary>Define las reglas de tipo, título y descripción.</summary>
    public CrearIncidenciaRequestValidator()
    {
        RuleFor(x => x.TipoId)
            .NotNull().WithMessage("El tipo es obligatorio.")
            .GreaterThan(0).WithMessage("El tipo es obligatorio.");
        RuleFor(x => x.Titulo).TextoObligatorio("El título", LimitesIncidencia.LargoMaximoTitulo);
        RuleFor(x => x.Descripcion)
            .TextoObligatorio("La descripción", LimitesIncidencia.LargoMaximoDescripcion, femenino: true);
    }
}

/// <summary>
/// Validación de formato de la edición de incidencia (RN-19, RN-21, RN-23).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class EditarIncidenciaRequestValidator : AbstractValidator<EditarIncidenciaRequest>
{
    /// <summary>Define las reglas de título, descripción, estado y prioridad.</summary>
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
                + "responsable, fechaVencimiento, fechaCreacion.");

        RuleFor(x => x.Direccion)
            .Must(v => v is null or OpcionesListadoIncidencias.DireccionAsc or OpcionesListadoIncidencias.DireccionDesc)
            .WithMessage("Valores permitidos para direccion: asc, desc.");
    }
}
