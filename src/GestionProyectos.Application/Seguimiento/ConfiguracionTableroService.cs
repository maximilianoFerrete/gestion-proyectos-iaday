using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Consulta y guarda las columnas visibles del tablero de un proyecto (RN-29 a RN-31).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, DA-15)
/// </remarks>
public interface IConfiguracionTableroService
{
    /// <summary>Configuración guardada o la de defecto (todas visibles); no exige usuario (RN-30).</summary>
    Task<ConfiguracionTableroDto> ObtenerAsync(int proyectoId, CancellationToken ct);

    /// <summary>Crea o reemplaza la configuración; exige permiso y proyecto no archivado (RN-31).</summary>
    Task<ConfiguracionTableroDto> GuardarAsync(int proyectoId, ConfiguracionTableroRequest request,
        CancellationToken ct);
}

/// <summary>
/// Implementa la configuración del tablero sobre IConfiguracionTableroRepository (upsert en transacción).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class ConfiguracionTableroService(
    IConfiguracionTableroRepository configuraciones,
    IProyectoRepository proyectos,
    IUnitOfWork unitOfWork,
    IUsuarioActual usuarioActual,
    IValidator<ConfiguracionTableroRequest> validador,
    TimeProvider reloj,
    ILogger<ConfiguracionTableroService> logger) : IConfiguracionTableroService
{
    /// <inheritdoc />
    public async Task<ConfiguracionTableroDto> ObtenerAsync(int proyectoId, CancellationToken ct)
    {
        await ObtenerProyectoAsync(proyectoId, ct);
        return ConfiguracionTableroDto.Desde(proyectoId, await configuraciones.ObtenerAsync(proyectoId, ct));
    }

    /// <inheritdoc />
    public async Task<ConfiguracionTableroDto> GuardarAsync(int proyectoId, ConfiguracionTableroRequest request,
        CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ProyectosConfigurarTablero);
        ArgumentNullException.ThrowIfNull(request);
        await validador.ValidateAndThrowAsync(request, ct);
        var proyecto = await ObtenerProyectoAsync(proyectoId, ct);
        proyecto.AsegurarEditable();

        var configuracion = new ConfiguracionTablero(proyectoId, request.MostrarEstado!.Value,
            request.MostrarEstimacion!.Value, request.MostrarResponsable!.Value, reloj.AhoraUtc());
        await unitOfWork.EjecutarEnTransaccionAsync(() => configuraciones.GuardarAsync(configuracion, ct), ct);
        logger.LogInformation("Configuración del tablero del proyecto {ProyectoId} guardada", proyectoId);
        return ConfiguracionTableroDto.Desde(proyectoId, configuracion);
    }

    private async Task<Proyecto> ObtenerProyectoAsync(int proyectoId, CancellationToken ct) =>
        await proyectos.ObtenerPorIdAsync(proyectoId, ct)
        ?? throw new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado,
            $"No existe el proyecto con id {proyectoId}.");
}
