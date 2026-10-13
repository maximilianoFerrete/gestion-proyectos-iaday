using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Carga los catálogos que usan el alcance y los indicadores (niveles, tipos, estados y flujos).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public interface ICatalogoSeguimiento
{
    /// <summary>Resuelve el alcance pedido; 400 VALIDACION si es inválido (RN-04, RN-05).</summary>
    Task<AlcanceResuelto> ResolverAlcanceAsync(IConsultaAlcance consulta, CancellationToken ct);

    /// <summary>Crea la calculadora con los estados y los flujos de los tipos del alcance.</summary>
    Task<CalculadoraIndicadores> CrearCalculadoraAsync(AlcanceResuelto alcance, CancellationToken ct);
}

/// <summary>
/// Implementa ICatalogoSeguimiento sobre los repositorios de catálogos y de seguimiento.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class CatalogoSeguimiento(
    INivelIncidenciaRepository niveles,
    ITipoIncidenciaRepository tipos,
    ISeguimientoRepository seguimiento) : ICatalogoSeguimiento
{
    /// <inheritdoc />
    public async Task<AlcanceResuelto> ResolverAlcanceAsync(IConsultaAlcance consulta, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        var catalogoNiveles = (await niveles.ListarAsync(ct))
            .Select(n => new NivelReferenciaDto(n.Id, n.Numero, n.Nombre))
            .ToList();
        var catalogoTipos = (await tipos.ListarAsync(soloActivos: false, nivelId: null, ct))
            .Select(t => new TipoCatalogoAlcance(t.Id, t.Nombre, t.Activo, t.Nivel.Id, t.Nivel.Numero))
            .ToList();
        return ResolutorAlcanceSeguimiento.Resolver(catalogoNiveles, catalogoTipos, consulta.NivelId,
            consulta.TipoId);
    }

    /// <inheritdoc />
    public async Task<CalculadoraIndicadores> CrearCalculadoraAsync(AlcanceResuelto alcance, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alcance);
        var estados = await seguimiento.ListarEstadosAsync(ct);
        var estadosDeFlujos = await seguimiento.ListarEstadosDeFlujosAsync(alcance.TipoIds, ct);
        return new CalculadoraIndicadores(estados, estadosDeFlujos);
    }
}
