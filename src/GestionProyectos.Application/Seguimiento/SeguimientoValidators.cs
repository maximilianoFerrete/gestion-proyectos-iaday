using FluentValidation;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Proyectos;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Validación del tablero general: listas blancas, rangos y largo de texto (CA-05, RN-28).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class ConsultaTableroProyectosQueryValidator : AbstractValidator<ConsultaTableroProyectosQuery>
{
    /// <summary>Define las reglas de cada parámetro de query.</summary>
    public ConsultaTableroProyectosQueryValidator()
    {
        RuleFor(x => x.Texto).TextoOpcional("El texto", OpcionesSeguimiento.LargoMaximoTextoProyectos);

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
            .Must(v => v is null || OpcionesSeguimiento.Ordenes.ContainsKey(v))
            .WithMessage("Valores permitidos para ordenarPor: codigo, nombre, total, abiertas, vencidas, "
                + "bloqueadas, enEspera, sinResponsable.");

        RuleFor(x => x.Direccion)
            .Must(v => v is null or OpcionesListadoProyectos.DireccionAsc or OpcionesListadoProyectos.DireccionDesc)
            .WithMessage("Valores permitidos para direccion: asc, desc.");
    }
}

/// <summary>
/// Reglas comunes de ámbito: agrupacionId y sinAgrupacion son excluyentes (RN-01).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public abstract class ConsultaAmbitoValidator<T> : AbstractValidator<T>
    where T : IConsultaAmbito
{
    /// <summary>Define la regla de exclusión del ámbito.</summary>
    protected ConsultaAmbitoValidator()
    {
        RuleFor(x => x.AgrupacionId)
            .Null()
            .When(x => x.SinAgrupacion == true)
            .WithMessage("agrupacionId y sinAgrupacion son excluyentes.");
    }
}

/// <summary>
/// Validación del resumen de seguimiento de un proyecto o agrupación (RN-01).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class ConsultaSeguimientoQueryValidator : ConsultaAmbitoValidator<ConsultaSeguimientoQuery>;

/// <summary>
/// Validación de la grilla del tablero: ámbito, indicador, estadoId y texto (RN-21, RN-28, CA-26).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class ConsultaTableroIncidenciasQueryValidator : ConsultaAmbitoValidator<ConsultaTableroIncidenciasQuery>
{
    /// <summary>Agrega las reglas de los filtros propios del tablero.</summary>
    public ConsultaTableroIncidenciasQueryValidator()
    {
        RuleFor(x => x.Indicador)
            .Must(v => v is null || OpcionesSeguimiento.Indicadores.ContainsKey(v))
            .WithMessage(x => $"El indicador '{x.Indicador}' no es válido.");

        RuleFor(x => x.EstadoId)
            .GreaterThan(0)
            .WithMessage(OpcionesSeguimiento.MensajeEstadoInexistente);

        RuleFor(x => x.Texto).TextoOpcional("El texto", OpcionesSeguimiento.LargoMaximoTextoTablero);
    }
}
