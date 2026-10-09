using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.EstadosProyecto;

/// <summary>
/// Estado de proyecto expuesto por la API, con la cantidad de proyectos que lo tienen.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record EstadoProyectoDto(
    int Id,
    string Nombre,
    string? Descripcion,
    int Orden,
    bool EsInicial,
    bool EsFinal,
    bool Activo,
    bool EsSistema,
    string? Codigo,
    int CantidadProyectos)
{
    /// <summary>Crea el DTO a partir de la entidad y su cantidad de proyectos.</summary>
    public static EstadoProyectoDto Desde(EstadoProyecto estado, int cantidadProyectos)
    {
        ArgumentNullException.ThrowIfNull(estado);
        return new EstadoProyectoDto(estado.Id, estado.Nombre, estado.Descripcion, estado.Orden,
            estado.EsInicial, estado.EsFinal, estado.Activo, estado.EsSistema, estado.Codigo,
            cantidadProyectos);
    }
}

/// <summary>
/// Datos para crear o editar un estado de proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record EstadoProyectoRequest(string? Nombre, string? Descripcion, int? Orden, bool EsFinal)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-31).</summary>
    public EstadoProyectoRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}
