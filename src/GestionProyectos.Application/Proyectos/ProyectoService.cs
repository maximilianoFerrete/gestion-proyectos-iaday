using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Proyectos;

/// <summary>
/// Implementa alta, consulta, edición y archivado de proyectos (RN-14 a RN-25).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class ProyectoService(
    IProyectoRepository proyectos,
    IPersonaRepository personas,
    IEstadoProyectoRepository estados,
    IUnitOfWork unitOfWork,
    IUsuarioActual usuarioActual,
    ProyectoValidadores validadores,
    TimeProvider reloj,
    ILogger<ProyectoService> logger) : IProyectoService
{
    /// <inheritdoc />
    public async Task<Paginado<ProyectoResumenDto>> ListarAsync(ListarProyectosQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var normalizada = query.Normalizar();
        await validadores.Listado.ValidateAndThrowAsync(normalizada, ct);
        return await proyectos.ListarAsync(OpcionesListadoProyectos.CrearFiltro(normalizada), ct);
    }

    /// <inheritdoc />
    public async Task<ProyectoDto> ObtenerAsync(int id, CancellationToken ct) =>
        await proyectos.ObtenerDetalleAsync(id, ct) ?? throw ProyectoNoEncontrado(id);

    /// <inheritdoc />
    public async Task<ProyectoDto> CrearAsync(CrearProyectoRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validadores.Alta.ValidateAndThrowAsync(datos, ct);
        await AsegurarResponsableAsignableAsync(datos.ResponsableId!.Value, ct);

        var estadoInicial = await estados.ObtenerInicialAsync(ct)
            ?? throw new InvalidOperationException("No hay un estado de proyecto inicial configurado.");
        var numero = await proyectos.ObtenerSiguienteNumeroAsync(ct);
        var proyecto = new Proyecto(FormateadorCodigoProyecto.Formatear(numero), ADatos(datos),
            estadoInicial.Id, reloj.AhoraUtc());

        await proyectos.AgregarAsync(proyecto, ct);
        logger.LogInformation("Proyecto {ProyectoId} creado con código {Codigo}", proyecto.Id, proyecto.Codigo);
        return await ObtenerAsync(proyecto.Id, ct);
    }

    /// <inheritdoc />
    public async Task<ProyectoDto> EditarAsync(int id, EditarProyectoRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var datos = request.Normalizar();
        await validadores.Edicion.ValidateAndThrowAsync(datos, ct);

        var proyecto = await proyectos.ObtenerPorIdAsync(id, ct) ?? throw ProyectoNoEncontrado(id);
        proyecto.AsegurarEditable();
        if (datos.ResponsableId != proyecto.ResponsableId)
        {
            await AsegurarResponsableAsignableAsync(datos.ResponsableId!.Value, ct);
        }

        if (datos.EstadoId != proyecto.EstadoId)
        {
            await AsegurarEstadoAsignableAsync(datos.EstadoId!.Value, ct);
        }

        proyecto.Actualizar(ADatos(datos), datos.EstadoId!.Value, reloj.AhoraUtc());
        await proyectos.ActualizarAsync(proyecto, ct);
        logger.LogInformation("Proyecto {ProyectoId} actualizado", proyecto.Id);
        return await ObtenerAsync(proyecto.Id, ct);
    }

    /// <inheritdoc />
    public async Task<ProyectoDto> ArchivarAsync(int id, CancellationToken ct)
    {
        var archivadoPorId = usuarioActual.AsegurarAdministrador();
        var proyecto = await proyectos.ObtenerPorIdAsync(id, ct) ?? throw ProyectoNoEncontrado(id);
        proyecto.AsegurarEditable();

        var estadoArchivado = await estados.ObtenerPorCodigoAsync(CodigosEstado.Archivado, ct)
            ?? throw new InvalidOperationException("No existe el estado de sistema ARCHIVADO.");
        var ahora = reloj.AhoraUtc();
        await unitOfWork.EjecutarEnTransaccionAsync(() =>
        {
            proyecto.Archivar(estadoArchivado.Id, archivadoPorId, ahora);
            return proyectos.ActualizarAsync(proyecto, ct);
        }, ct);

        logger.LogInformation("Proyecto {ProyectoId} archivado por la persona {PersonaId}",
            proyecto.Id, archivadoPorId);
        return await ObtenerAsync(proyecto.Id, ct);
    }

    private static DatosProyecto ADatos(IDatosProyectoRequest datos) =>
        new(datos.Nombre!, datos.Descripcion!, datos.ResponsableId!.Value,
            datos.FechaInicio!.Value, datos.FechaObjetivo!.Value);

    private static EntidadNoEncontradaException ProyectoNoEncontrado(int id) =>
        new(CodigosError.ProyectoNoEncontrado, $"No existe el proyecto con id {id}.");

    private async Task AsegurarResponsableAsignableAsync(int responsableId, CancellationToken ct)
    {
        var responsable = await personas.ObtenerPorIdAsync(responsableId, ct);
        if (responsable is not { Activo: true })
        {
            throw new ValidacionNegocioException(CodigosError.ResponsableInvalido,
                $"El responsable con id {responsableId} no existe o está inactivo.");
        }
    }

    private async Task AsegurarEstadoAsignableAsync(int estadoId, CancellationToken ct)
    {
        var estado = await estados.ObtenerPorIdAsync(estadoId, ct);
        if (estado is not { EsAsignable: true })
        {
            throw new ValidacionNegocioException(CodigosError.EstadoInvalido,
                $"El estado con id {estadoId} no existe, está inactivo o no puede asignarse.");
        }
    }
}

/// <summary>
/// Agrupa los validadores que usa ProyectoService (evita un constructor con demasiados parámetros).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record ProyectoValidadores(
    IValidator<CrearProyectoRequest> Alta,
    IValidator<EditarProyectoRequest> Edicion,
    IValidator<ListarProyectosQuery> Listado);
