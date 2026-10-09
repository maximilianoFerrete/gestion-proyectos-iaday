using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.NivelesIncidencia;

/// <summary>
/// Casos de uso de niveles de incidencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface INivelIncidenciaService
{
    /// <summary>Lista los niveles ordenados por número.</summary>
    Task<IReadOnlyList<NivelIncidenciaDto>> ListarAsync(CancellationToken ct);

    /// <summary>Obtiene un nivel por id (404 si no existe).</summary>
    Task<NivelIncidenciaDto> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>Crea un nivel; exige el permiso de configuración (RN-01, RN-04).</summary>
    Task<NivelIncidenciaDto> CrearAsync(CrearNivelIncidenciaRequest request, CancellationToken ct);

    /// <summary>Edita nombre y descripción de un nivel (RN-01, RN-04).</summary>
    Task<NivelIncidenciaDto> EditarAsync(int id, EditarNivelIncidenciaRequest request, CancellationToken ct);
}

/// <summary>
/// Implementa la configuración de niveles de incidencia (RN-01, RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class NivelIncidenciaService(
    INivelIncidenciaRepository repositorio,
    IUsuarioActual usuarioActual,
    IValidator<CrearNivelIncidenciaRequest> validadorAlta,
    IValidator<EditarNivelIncidenciaRequest> validadorEdicion,
    TimeProvider reloj,
    ILogger<NivelIncidenciaService> logger) : INivelIncidenciaService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<NivelIncidenciaDto>> ListarAsync(CancellationToken ct) =>
        repositorio.ListarAsync(ct);

    /// <inheritdoc />
    public async Task<NivelIncidenciaDto> ObtenerAsync(int id, CancellationToken ct) =>
        await repositorio.ObtenerDtoAsync(id, ct) ?? throw NoEncontrado(id);

    /// <inheritdoc />
    public async Task<NivelIncidenciaDto> CrearAsync(CrearNivelIncidenciaRequest request, CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validadorAlta.ValidateAndThrowAsync(datos, ct);

        if (await repositorio.ExisteNumeroAsync(datos.Numero!.Value, ct))
        {
            throw new ConflictoException(CodigosError.NivelNumeroDuplicado,
                $"Ya existe un nivel con el número {datos.Numero}.");
        }

        await AsegurarNombreDisponibleAsync(datos.Nombre!, null, ct);
        var nivel = new NivelIncidencia(datos.Numero.Value, datos.Nombre!, datos.Descripcion, reloj.AhoraUtc());
        await repositorio.AgregarAsync(nivel, ct);
        logger.LogInformation("Nivel de incidencia {NivelId} creado", nivel.Id);
        return await ObtenerAsync(nivel.Id, ct);
    }

    /// <inheritdoc />
    public async Task<NivelIncidenciaDto> EditarAsync(int id, EditarNivelIncidenciaRequest request,
        CancellationToken ct)
    {
        usuarioActual.AsegurarPermiso(Permisos.ConfiguracionIncidencias);
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validadorEdicion.ValidateAndThrowAsync(datos, ct);

        var nivel = await repositorio.ObtenerPorIdAsync(id, ct) ?? throw NoEncontrado(id);
        await AsegurarNombreDisponibleAsync(datos.Nombre!, id, ct);
        nivel.Actualizar(datos.Nombre!, datos.Descripcion, reloj.AhoraUtc());
        await repositorio.ActualizarAsync(nivel, ct);
        logger.LogInformation("Nivel de incidencia {NivelId} actualizado", nivel.Id);
        return await ObtenerAsync(nivel.Id, ct);
    }

    private static EntidadNoEncontradaException NoEncontrado(int id) =>
        new(CodigosError.NivelNoEncontrado, $"No existe el nivel de incidencia con id {id}.");

    private async Task AsegurarNombreDisponibleAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        if (await repositorio.ExisteNombreAsync(nombre, excluirId, ct))
        {
            throw new ConflictoException(CodigosError.NivelNombreDuplicado,
                $"Ya existe un nivel con el nombre '{nombre}'.");
        }
    }
}
