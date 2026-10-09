using GestionProyectos.Application.RelacionesTiposIncidencia;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia de relaciones permitidas tipo padre → tipo hijo (solo alta y consulta, RN-09).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IRelacionTipoIncidenciaRepository
{
    /// <summary>Lista relaciones filtradas, por nivel y nombre del padre y nombre del hijo.</summary>
    Task<IReadOnlyList<RelacionTipoIncidenciaDto>> ListarAsync(int? tipoPadreId, int? tipoHijoId,
        CancellationToken ct);

    /// <summary>Obtiene el DTO de la relación o null.</summary>
    Task<RelacionTipoIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct);

    /// <summary>Indica si ya existe el par tipo padre → tipo hijo.</summary>
    Task<bool> ExisteAsync(int tipoPadreId, int tipoHijoId, CancellationToken ct);

    /// <summary>Ids de los tipos hijo permitidos para el tipo padre indicado (RN-25).</summary>
    Task<IReadOnlyList<int>> ListarTiposHijoAsync(int tipoPadreId, CancellationToken ct);

    /// <summary>Inserta la relación y le asigna el id generado.</summary>
    Task AgregarAsync(RelacionTipoIncidencia relacion, CancellationToken ct);
}
