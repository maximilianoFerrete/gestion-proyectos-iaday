using FluentValidation;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Proyectos;

/// <summary>
/// Reglas comunes de alta y edición de proyectos (RN-15, RN-16).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public abstract class DatosProyectoRequestValidator<T> : AbstractValidator<T>
    where T : IDatosProyectoRequest
{
    public const int LargoMaximoNombre = 150;
    public const int LargoMaximoDescripcion = 2000;

    /// <summary>Define las reglas de nombre, descripción, responsable y fechas.</summary>
    protected DatosProyectoRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LargoMaximoNombre);
        RuleFor(x => x.Descripcion).TextoObligatorio("La descripción", LargoMaximoDescripcion, femenino: true);

        RuleFor(x => x.ResponsableId)
            .NotNull().WithMessage("El responsable es obligatorio.")
            .GreaterThan(0).WithMessage("El responsable es obligatorio.");

        RuleFor(x => x.FechaInicio)
            .NotNull().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.FechaObjetivo)
            .NotNull().WithMessage("La fecha objetivo es obligatoria.")
            .GreaterThanOrEqualTo(x => x.FechaInicio)
            .When(x => x.FechaInicio is not null, ApplyConditionTo.CurrentValidator)
            .WithMessage("La fecha objetivo debe ser mayor o igual a la fecha de inicio.");
    }
}

/// <summary>
/// Validación de formato del alta de proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class CrearProyectoRequestValidator : DatosProyectoRequestValidator<CrearProyectoRequest>;

/// <summary>
/// Validación de formato de la edición de proyecto (incluye el estado).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class EditarProyectoRequestValidator : DatosProyectoRequestValidator<EditarProyectoRequest>
{
    /// <summary>Agrega la regla de estado obligatorio.</summary>
    public EditarProyectoRequestValidator()
    {
        RuleFor(x => x.EstadoId)
            .NotNull().WithMessage("El estado es obligatorio.")
            .GreaterThan(0).WithMessage("El estado es obligatorio.");
    }
}

/// <summary>
/// Validación de los parámetros del listado de proyectos (lista blanca y rangos).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class ListarProyectosQueryValidator : AbstractValidator<ListarProyectosQuery>
{
    /// <summary>Define las reglas de cada parámetro de query.</summary>
    public ListarProyectosQueryValidator()
    {
        RuleFor(x => x.Texto).TextoOpcional("El texto", OpcionesListadoProyectos.LargoMaximoTexto);

        RuleFor(x => x.Archivados)
            .Must(v => v is null || OpcionesListadoProyectos.Archivados.ContainsKey(v))
            .WithMessage("Valores permitidos para archivados: excluir, solo, incluir.");

        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.TamanioPagina)
            .InclusiveBetween(1, OpcionesListadoProyectos.TamanioPaginaMaximo)
            .WithMessage($"El tamaño de página debe estar entre 1 y {OpcionesListadoProyectos.TamanioPaginaMaximo}.");

        RuleFor(x => x.OrdenarPor)
            .Must(v => v is null || OpcionesListadoProyectos.Ordenes.ContainsKey(v))
            .WithMessage("Valores permitidos para ordenarPor: codigo, nombre, fechaInicio, "
                + "fechaObjetivo, estado, responsable.");

        RuleFor(x => x.Direccion)
            .Must(v => v is null or OpcionesListadoProyectos.DireccionAsc or OpcionesListadoProyectos.DireccionDesc)
            .WithMessage("Valores permitidos para direccion: asc, desc.");
    }
}
