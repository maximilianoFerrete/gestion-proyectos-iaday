using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.EstadosIncidencia;

/// <summary>
/// Casos de uso del catálogo de estados de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IEstadoIncidenciaService
{
    /// <summary>Lista estados ordenados por orden y nombre.</summary>
    Task<IReadOnlyList<EstadoIncidenciaDto>> ListarAsync(bool soloActivos, CancellationToken ct);

    /// <summary>Obtiene un estado por id (404 si no existe).</summary>
    Task<EstadoIncidenciaDto> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>Crea un estado activo (RN-01, RN-10).</summary>
    Task<EstadoIncidenciaDto> CrearAsync(EstadoIncidenciaRequest request, CancellationToken ct);

    /// <summary>Edita nombre, descripción y orden (RN-01, RN-10).</summary>
    Task<EstadoIncidenciaDto> EditarAsync(int id, EstadoIncidenciaRequest request, CancellationToken ct);

    /// <summary>Desactiva si no es inicial de ningún flujo; idempotente (RN-11).</summary>
    Task<EstadoIncidenciaDto> DesactivarAsync(int id, CancellationToken ct);

    /// <summary>Activa el estado; idempotente (RN-10).</summary>
    Task<EstadoIncidenciaDto> ActivarAsync(int id, CancellationToken ct);
}

/// <summary>
/// Implementa la configuración de estados de incidencia (RN-01, RN-10, RN-11).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class EstadoIncidenciaService(
    IEstadoIncidenciaRepository repositorio,
    IUsuarioActual usuarioActual,
    IValidator<EstadoIncidenciaRequest> validador,
    TimeProvider reloj,
    ILogger<EstadoIncidenciaService> logger) : IEstadoIncidenciaService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<EstadoIncidenciaDto>> ListarAsync(bool soloActivos, CancellationToken ct) =>
        repositorio.ListarAsync(soloActivos, ct);

    /// <inheritdoc />
    public async Task<EstadoIncidenciaDto> ObtenerAsync(int id, CancellationToken ct) =>
        await repositorio.ObtenerDtoAsync(id, ct) ?? throw NoEncontrado(id);

    /// <inheritdoc />
    public async Task<EstadoIncidenciaDto> CrearAsync(EstadoIncidenciaRequest request, CancellationToken ct)
    {
        var datos = await PrepararAsync(request, ct);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, null, ct);
        var estado = new EstadoIncidencia(datos.Nombre!, datos.Descripcion, datos.Orden!.Value, reloj.AhoraUtc());
        await repositorio.AgregarAsync(estado, ct);
        logger.LogInformation("Estado de incidencia {EstadoId} creado", estado.Id);
        return await ObtenerAsync(estado.Id, ct);
    }

    /// <inheritdoc />
    public async Task<EstadoIncidenciaDto> EditarAsync(int id, EstadoIncidenciaRequest request,
        CancellationToken ct)
    {
        var datos = await PrepararAsync(request, ct);
        var estado = await repositorio.ObtenerPorIdAsync(id, ct) ?? throw NoEncontrado(id);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, id, ct);
        estado.Actualizar(datos.Nombre!, datos.Descripcion, datos.Orden!.Value, reloj.AhoraUtc());
        await repositorio.ActualizarAsync(estado, ct);
        logger.LogInformation("Estado de incidencia {EstadoId} actualizado", estado.Id);
        return await ObtenerAsync(estado.Id, ct);
    }

    /// <inheritdoc />
    public async Task<EstadoIncidenciaDto> DesactivarAsync(int id, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        var estado = await repositorio.ObtenerPorIdAsync(id, ct) ?? throw NoEncontrado(id);
        if (estado.Activo)
        {
            var flujosDondeEsInicial = await repositorio.ListarFlujosDondeEsInicialAsync(id, ct);
            estado.Desactivar(flujosDondeEsInicial, reloj.AhoraUtc());
            await repositorio.ActualizarAsync(estado, ct);
            logger.LogInformation("Estado de incidencia {EstadoId} desactivado", estado.Id);
        }

        return await ObtenerAsync(estado.Id, ct);
    }

    /// <inheritdoc />
    public async Task<EstadoIncidenciaDto> ActivarAsync(int id, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        var estado = await repositorio.ObtenerPorIdAsync(id, ct) ?? throw NoEncontrado(id);
        if (!estado.Activo)
        {
            estado.Activar(reloj.AhoraUtc());
            await repositorio.ActualizarAsync(estado, ct);
            logger.LogInformation("Estado de incidencia {EstadoId} activado", estado.Id);
        }

        return await ObtenerAsync(estado.Id, ct);
    }

    private async Task<EstadoIncidenciaRequest> PrepararAsync(EstadoIncidenciaRequest request, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validador.ValidateAndThrowAsync(datos, ct);
        return datos;
    }

    private static EntidadNoEncontradaException NoEncontrado(int id) =>
        new(CodigosError.EstadoIncidenciaNoEncontrado, $"No existe el estado de incidencia con id {id}.");

    private async Task AsegurarNombreDisponibleAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        if (await repositorio.ExisteNombreAsync(nombre, excluirId, ct))
        {
            throw new ConflictoException(CodigosError.EstadoIncidenciaNombreDuplicado,
                $"Ya existe un estado de incidencia con el nombre '{nombre}'.");
        }
    }
}
