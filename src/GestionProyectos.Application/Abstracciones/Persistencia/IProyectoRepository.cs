using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia y consultas de proyectos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IProyectoRepository
{
    /// <summary>Obtiene la entidad proyecto por id o null si no existe.</summary>
    Task<Proyecto?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene el detalle del proyecto (con referencias resueltas) o null.</summary>
    Task<ProyectoDto?> ObtenerDetalleAsync(int id, CancellationToken ct);

    /// <summary>Lista proyectos con filtros, orden y paginación.</summary>
    Task<Paginado<ProyectoResumenDto>> ListarAsync(FiltroProyectos filtro, CancellationToken ct);

    /// <summary>Obtiene el siguiente número de la secuencia de códigos de proyecto.</summary>
    Task<int> ObtenerSiguienteNumeroAsync(CancellationToken ct);

    /// <summary>Inserta el proyecto y le asigna el id generado.</summary>
    Task AgregarAsync(Proyecto proyecto, CancellationToken ct);

    /// <summary>Guarda los cambios del proyecto (incluidos los datos de archivado).</summary>
    Task ActualizarAsync(Proyecto proyecto, CancellationToken ct);
}
