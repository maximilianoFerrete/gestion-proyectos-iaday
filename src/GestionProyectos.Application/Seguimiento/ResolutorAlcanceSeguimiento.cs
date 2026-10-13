using FluentValidation;
using FluentValidation.Results;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Tipo del catálogo con su nivel, para resolver el alcance (activos e inactivos, RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record TipoCatalogoAlcance(int Id, string Nombre, bool Activo, int NivelId, int NivelNumero);

/// <summary>
/// Alcance resuelto: DTO para la respuesta y tipos a filtrar (null = sin filtro de tipos).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record AlcanceResuelto(AlcanceSeguimientoDto Dto, IReadOnlyList<int>? TipoIds);

/// <summary>
/// Función pura que combina el catálogo con los nivelId/tipoId pedidos (RN-04 a RN-06).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public static class ResolutorAlcanceSeguimiento
{
    public const string ClaveNivel = "nivelId";
    public const string ClaveTipo = "tipoId";
    public const string MensajeInterseccionVacia =
        "Ninguno de los tipos seleccionados pertenece a los niveles seleccionados.";

    /// <summary>Resuelve el alcance; lanza ValidationException ante ids inexistentes (RN-05).</summary>
    public static AlcanceResuelto Resolver(IReadOnlyList<NivelReferenciaDto> niveles,
        IReadOnlyList<TipoCatalogoAlcance> tipos, IReadOnlyCollection<int>? nivelIds,
        IReadOnlyCollection<int>? tipoIds)
    {
        ArgumentNullException.ThrowIfNull(niveles);
        ArgumentNullException.ThrowIfNull(tipos);
        var nivelesPedidos = (nivelIds ?? []).Distinct().ToList();
        var tiposPedidos = (tipoIds ?? []).Distinct().ToList();
        AsegurarExistentes(nivelesPedidos, niveles.Select(n => n.Id), tiposPedidos, tipos.Select(t => t.Id));

        var nivelesSeleccionados = niveles
            .Where(n => nivelesPedidos.Count == 0 || nivelesPedidos.Contains(n.Id))
            .OrderBy(n => n.Numero)
            .ToList();
        var tiposDeNiveles = tipos
            .Where(t => nivelesSeleccionados.Exists(n => n.Id == t.NivelId))
            .OrderBy(t => t.NivelNumero).ThenBy(t => t.Nombre, ComparadoresSeguimiento.Nombre).ThenBy(t => t.Id)
            .ToList();
        var efectivos = tiposDeNiveles
            .Where(t => tiposPedidos.Count == 0 || tiposPedidos.Contains(t.Id))
            .ToList();
        if (tiposPedidos.Count > 0 && efectivos.Count == 0)
        {
            throw Error(ClaveTipo, MensajeInterseccionVacia);
        }

        var todosLosNiveles = nivelesPedidos.Count == 0;
        var todosLosTipos = efectivos.Count == tiposDeNiveles.Count;
        var dto = new AlcanceSeguimientoDto(todosLosNiveles, nivelesSeleccionados, todosLosTipos,
            efectivos.Select(t => new TipoReferenciaDto(t.Id, t.Nombre, t.Activo, t.NivelNumero)).ToList());
        var filtroTipos = todosLosNiveles && todosLosTipos ? null : efectivos.Select(t => t.Id).ToList();
        return new AlcanceResuelto(dto, filtroTipos);
    }

    private static void AsegurarExistentes(List<int> nivelesPedidos, IEnumerable<int> nivelesExistentes,
        List<int> tiposPedidos, IEnumerable<int> tiposExistentes)
    {
        var errores = Inexistentes(nivelesPedidos, nivelesExistentes)
            .Select(id => new ValidationFailure(ClaveNivel, $"No existe el nivel de incidencia con id {id}."))
            .Concat(Inexistentes(tiposPedidos, tiposExistentes)
                .Select(id => new ValidationFailure(ClaveTipo, $"No existe el tipo de incidencia con id {id}.")))
            .ToList();
        if (errores.Count > 0)
        {
            throw new ValidationException(errores);
        }
    }

    private static IEnumerable<int> Inexistentes(List<int> pedidos, IEnumerable<int> existentes)
    {
        var conjunto = existentes.ToHashSet();
        return pedidos.Where(id => !conjunto.Contains(id));
    }

    private static ValidationException Error(string clave, string mensaje) =>
        new(new[] { new ValidationFailure(clave, mensaje) });
}
