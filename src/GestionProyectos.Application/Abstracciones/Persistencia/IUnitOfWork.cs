namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Unidad de trabajo: agrupa operaciones de repositorios en una transacción.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Inicia una transacción que usarán los repositorios de la misma solicitud.</summary>
    Task IniciarAsync(CancellationToken ct);

    /// <summary>Confirma la transacción en curso.</summary>
    Task ConfirmarAsync(CancellationToken ct);

    /// <summary>Revierte la transacción en curso, si existe.</summary>
    Task RevertirAsync(CancellationToken ct);
}
