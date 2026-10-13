using GestionProyectos.Domain.Reglas;

namespace GestionProyectos.Application.Incidencias;

/// <summary>
/// Completa el campo Vencida de los DTO de incidencia con la fecha de referencia (RF-005 RN-36).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, C-RF002-02)
/// </remarks>
public static class VencidaIncidenciaExtensions
{
    /// <summary>Devuelve la fila del listado con Vencida calculado.</summary>
    public static IncidenciaResumenDto ConVencida(this IncidenciaResumenDto dto, DateOnly hoy)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return dto with { Vencida = ReglasVencimiento.EstaVencida(dto.FechaVencimiento, dto.Estado.EsFinal, hoy) };
    }

    /// <summary>Devuelve el nodo del árbol con Vencida calculado.</summary>
    public static NodoArbolIncidenciaDto ConVencida(this NodoArbolIncidenciaDto dto, DateOnly hoy)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return dto with { Vencida = ReglasVencimiento.EstaVencida(dto.FechaVencimiento, dto.Estado.EsFinal, hoy) };
    }

    /// <summary>Devuelve la referencia (padre o hija) con Vencida calculado.</summary>
    public static IncidenciaReferenciaDto ConVencida(this IncidenciaReferenciaDto dto, DateOnly hoy)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return dto with { Vencida = ReglasVencimiento.EstaVencida(dto.FechaVencimiento, dto.Estado.EsFinal, hoy) };
    }

    /// <summary>Devuelve el detalle con Vencida calculado en la incidencia, su padre y sus hijas.</summary>
    public static IncidenciaDto ConVencida(this IncidenciaDto dto, DateOnly hoy)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return dto with
        {
            Vencida = ReglasVencimiento.EstaVencida(dto.FechaVencimiento, dto.Estado.EsFinal, hoy),
            Padre = dto.Padre?.ConVencida(hoy),
            Hijas = dto.Hijas.Select(h => h.ConVencida(hoy)).ToList(),
        };
    }
}
