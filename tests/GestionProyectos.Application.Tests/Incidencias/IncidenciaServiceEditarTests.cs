using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de IncidenciaService.EditarAsync (RN-11, RN-17, RN-21 a RN-32, RN-36 a RN-38).
/// Escenario: Épica 3 (Pagos, vence 15/12) → HU 15 (vence 30/11) → Subtarea 16; Épica 4 (Envíos).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// Modificado: 2026-10-11 - Estimación en la edición (tests RF-005, RN-34, RN-35, CA-43)
/// </remarks>
public class IncidenciaServiceEditarTests : ContextoIncidenciaService
{
    private const int EpicaPagos = 3;
    private const int EpicaEnvios = 4;
    private const int HuId = 15;
    private const int SubtareaId = 16;
    private static readonly DateOnly Nov20 = new(2026, 11, 20);
    private static readonly DateOnly Nov22 = new(2026, 11, 22);
    private static readonly DateOnly Nov25 = new(2026, 11, 25);
    private static readonly DateOnly Nov30 = new(2026, 11, 30);
    private static readonly DateOnly Dic10 = new(2026, 12, 10);
    private static readonly DateOnly Dic15 = new(2026, 12, 15);
    private static readonly DateOnly Dic20 = new(2026, 12, 20);

    private readonly Incidencia _hu;

    /// <summary>Request que reproduce el estado actual de la HU 15 (sin cambios).</summary>
    private static readonly EditarIncidenciaRequest SinCambios =
        new("Título", "Descripción", EnProgreso, Alta, PersonaActiva, Nov30, null, EpicaPagos);

    public IncidenciaServiceEditarTests()
    {
        Existe(Incidencia(EpicaPagos, tipoId: Epica, agrupacionId: AgrupacionPagos, vencimiento: Dic15));
        Existe(Incidencia(EpicaEnvios, tipoId: Epica, agrupacionId: AgrupacionEnvios, vencimiento: Dic20));
        _hu = Existe(Incidencia(HuId, tipoId: Hu, estadoId: EnProgreso, prioridadId: Alta,
            responsableId: PersonaActiva, vencimiento: Nov30, agrupacionId: AgrupacionPagos, padreId: EpicaPagos));
        Existe(Incidencia(SubtareaId, tipoId: Subtarea, agrupacionId: AgrupacionPagos, padreId: HuId));
        Incidencias.ObtenerIdsAncestrosAsync(SubtareaId, Arg.Any<CancellationToken>()).Returns([HuId, EpicaPagos]);
    }

    private Task<IncidenciaDto> EditarHu(EditarIncidenciaRequest request) =>
        Sut().EditarAsync(ProyectoId, HuId, request, CancellationToken.None);

    // RN-37, CA-20: incidencia de otro proyecto → 404 INCIDENCIA_NO_ENCONTRADA
    [Fact]
    public async Task EditarAsync_IncidenciaDeOtroProyecto_LanzaIncidenciaNoEncontrada()
    {
        // Arrange
        Existe(Incidencia(40, proyectoId: OtroProyectoId));

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Sut().EditarAsync(ProyectoId, 40, SinCambios with { PadreId = null }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.IncidenciaNoEncontrada, ex.Codigo);
    }

    // RN-37: incidencia inexistente → 404 INCIDENCIA_NO_ENCONTRADA
    [Fact]
    public async Task EditarAsync_IncidenciaInexistente_LanzaIncidenciaNoEncontrada()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Sut().EditarAsync(ProyectoId, 99, SinCambios, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.IncidenciaNoEncontrada, ex.Codigo);
    }

    // RN-23, CA-18: estado fuera del flujo del tipo → 400 ESTADO_INCIDENCIA_INVALIDO
    [Fact]
    public async Task EditarAsync_EstadoFueraDelFlujo_LanzaEstadoIncidenciaInvalido()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => EditarHu(SinCambios with { EstadoId = Nuevo }));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
        await Incidencias.DidNotReceive().ActualizarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-11, CA-10: mantener el estado actual aunque esté inactivo
    [Fact]
    public async Task EditarAsync_MantieneEstadoActualInactivo_Actualiza()
    {
        // Arrange
        ConfigurarEstado(Estado(EnProgreso, "En progreso", activo: false));

        // Act
        await EditarHu(SinCambios with { Titulo = "Nuevo título" });

        // Assert
        await Incidencias.Received(1).ActualizarAsync(_hu, Arg.Any<CancellationToken>());
        Assert.Equal(EnProgreso, _hu.EstadoId);
    }

    // RN-11, CA-10: cambiar a un estado inactivo → 400 ESTADO_INCIDENCIA_INVALIDO
    [Fact]
    public async Task EditarAsync_CambiaAEstadoInactivo_LanzaEstadoIncidenciaInvalido()
    {
        // Arrange
        ConfigurarEstado(Estado(Hecho, "Hecho", activo: false));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => EditarHu(SinCambios with { EstadoId = Hecho }));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-17, RN-21: mantener la prioridad actual aunque esté inactiva
    [Fact]
    public async Task EditarAsync_MantienePrioridadActualInactiva_Actualiza()
    {
        // Arrange
        var incidencia = Existe(Incidencia(30, tipoId: Hu, prioridadId: PrioridadInactiva));

        // Act
        await Sut().EditarAsync(ProyectoId, 30, SinCambios with { EstadoId = PorHacer, PrioridadId = PrioridadInactiva,
            ResponsableId = null, PadreId = null }, CancellationToken.None);

        // Assert
        Assert.Equal(PrioridadInactiva, incidencia.PrioridadId);
    }

    // RN-21: cambiar a una prioridad inactiva → 400 PRIORIDAD_INVALIDA
    [Fact]
    public async Task EditarAsync_CambiaAPrioridadInactiva_LanzaPrioridadInvalida()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => EditarHu(SinCambios with { PrioridadId = PrioridadInactiva }));

        // Assert
        Assert.Equal(CodigosError.PrioridadInvalida, ex.Codigo);
    }

    // RN-22: mantener el responsable actual aunque esté inactivo
    [Fact]
    public async Task EditarAsync_MantieneResponsableActualInactivo_Actualiza()
    {
        // Arrange
        var incidencia = Existe(Incidencia(30, tipoId: Hu, prioridadId: Alta, responsableId: PersonaInactiva));

        // Act
        await Sut().EditarAsync(ProyectoId, 30, SinCambios with { EstadoId = PorHacer,
            ResponsableId = PersonaInactiva, PadreId = null }, CancellationToken.None);

        // Assert
        Assert.Equal(PersonaInactiva, incidencia.ResponsableId);
    }

    // RN-22: cambiar a un responsable inactivo → 400 RESPONSABLE_INVALIDO
    [Fact]
    public async Task EditarAsync_CambiaAResponsableInactivo_LanzaResponsableInvalido()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => EditarHu(SinCambios with { ResponsableId = PersonaInactiva }));

        // Assert
        Assert.Equal(CodigosError.ResponsableInvalido, ex.Codigo);
    }

    // RN-26, CA-26: poner como padre a sí misma → 409 RELACION_CIRCULAR
    [Fact]
    public async Task EditarAsync_PadreEsSiMisma_LanzaRelacionCircular()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => EditarHu(SinCambios with { PadreId = HuId }));

        // Assert
        Assert.Equal(CodigosError.RelacionCircular, ex.Codigo);
    }

    // RN-26, CA-26: poner como padre a una descendiente → 409 RELACION_CIRCULAR
    [Fact]
    public async Task EditarAsync_PadreEsDescendiente_LanzaRelacionCircular()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => EditarHu(SinCambios with { PadreId = SubtareaId }));

        // Assert
        Assert.Equal(CodigosError.RelacionCircular, ex.Codigo);
        await Incidencias.DidNotReceive().ActualizarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-27, CA-27: padre de otro proyecto → 400 PADRE_INVALIDO
    [Fact]
    public async Task EditarAsync_PadreDeOtroProyecto_LanzaPadreInvalido()
    {
        // Arrange
        Existe(Incidencia(40, proyectoId: OtroProyectoId, tipoId: Epica));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => EditarHu(SinCambios with { PadreId = 40 }));

        // Assert
        Assert.Equal(CodigosError.PadreInvalido, ex.Codigo);
    }

    // RN-25: cambiar a un padre cuyo tipo no admite el de la hija → 400 RELACION_NO_PERMITIDA
    [Fact]
    public async Task EditarAsync_NuevoPadreConRelacionNoPermitida_LanzaRelacionNoPermitida()
    {
        // Arrange
        Existe(Incidencia(31, tipoId: Bug));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => EditarHu(SinCambios with { PadreId = 31 }));

        // Assert
        Assert.Equal(CodigosError.RelacionNoPermitida, ex.Codigo);
    }

    // RN-29, CA-30: cambio de padre válido → propaga la agrupación del nuevo padre a los descendientes
    [Fact]
    public async Task EditarAsync_CambioDePadreValido_PropagaAgrupacionDelNuevoPadre()
    {
        // Act
        await EditarHu(SinCambios with { PadreId = EpicaEnvios });

        // Assert
        Assert.Equal(EpicaEnvios, _hu.PadreId);
        Assert.Equal(AgrupacionEnvios, _hu.AgrupacionId);
        await Incidencias.Received(1).ActualizarAgrupacionDescendientesAsync(HuId, AgrupacionEnvios,
            DatosPrueba.AhoraTruncado, Arg.Any<CancellationToken>());
    }

    // RN-29, CA-30: quitar el padre sin enviar agrupación conserva la actual y no propaga
    [Fact]
    public async Task EditarAsync_QuitaPadreSinAgrupacion_ConservaAgrupacionSinPropagar()
    {
        // Act
        await EditarHu(SinCambios with { PadreId = null });

        // Assert
        Assert.Null(_hu.PadreId);
        Assert.Equal(AgrupacionPagos, _hu.AgrupacionId);
        await Incidencias.DidNotReceive().ActualizarAgrupacionDescendientesAsync(Arg.Any<int>(), Arg.Any<int?>(),
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    // RN-29: quitar el padre eligiendo otra agrupación → se propaga
    [Fact]
    public async Task EditarAsync_QuitaPadreConOtraAgrupacion_PropagaLaNueva()
    {
        // Act
        await EditarHu(SinCambios with { PadreId = null, AgrupacionId = AgrupacionEnvios });

        // Assert
        await Incidencias.Received(1).ActualizarAgrupacionDescendientesAsync(HuId, AgrupacionEnvios,
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    // RN-29, CA-28: sin padre, cambio de agrupación → propaga a los descendientes
    [Fact]
    public async Task EditarAsync_SinPadreCambiaAgrupacion_PropagaADescendientes()
    {
        // Act
        await Sut().EditarAsync(ProyectoId, EpicaPagos, new EditarIncidenciaRequest("Checkout", "Épica",
            PorHacer, Media, null, Dic15, AgrupacionEnvios, null), CancellationToken.None);

        // Assert
        await Incidencias.Received(1).ActualizarAgrupacionDescendientesAsync(EpicaPagos, AgrupacionEnvios,
            DatosPrueba.AhoraTruncado, Arg.Any<CancellationToken>());
    }

    // RN-28: sin padre, agrupación de otro proyecto → 400 AGRUPACION_INVALIDA
    [Fact]
    public async Task EditarAsync_SinPadreAgrupacionDeOtroProyecto_LanzaAgrupacionInvalida()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().EditarAsync(ProyectoId, EpicaPagos,
            new EditarIncidenciaRequest("Checkout", "Épica", PorHacer, Media, null, Dic15, AgrupacionOtroProyecto, null),
            CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionInvalida, ex.Codigo);
    }

    // RN-28, CA-29: con padre y agrupacionId distinto del padre → 400 AGRUPACION_HEREDADA
    [Fact]
    public async Task EditarAsync_ConPadreYAgrupacionDistinta_LanzaAgrupacionHeredada()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => EditarHu(SinCambios with { AgrupacionId = AgrupacionEnvios }));

        // Assert
        Assert.Equal(CodigosError.AgrupacionHeredada, ex.Codigo);
    }

    // RN-32, CA-32: adelantar el vencimiento por debajo de una hija → 409 sin actualizar
    [Fact]
    public async Task EditarAsync_VencimientoAnteriorAUnaHija_LanzaVencimientoAnteriorAHijasSinActualizar()
    {
        // Arrange
        ConHijas(HuId, Hija(SubtareaId, vencimiento: Nov20), Hija(17, vencimiento: Nov25));

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => EditarHu(SinCambios with { FechaVencimiento = Nov22 }));

        // Assert
        Assert.Equal(CodigosError.VencimientoAnteriorAHijas, ex.Codigo);
        Assert.Equal(["PRJ-0001-17"], (string[])ex.Extensiones[ReglasJerarquia.ExtensionIncidencias]!);
        await Incidencias.DidNotReceive().ActualizarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-32, CA-32: quitar el vencimiento del padre con hijas con fecha se acepta
    [Fact]
    public async Task EditarAsync_QuitaVencimientoConHijasConFecha_Actualiza()
    {
        // Arrange
        ConHijas(HuId, Hija(SubtareaId, vencimiento: Nov20), Hija(17, vencimiento: Nov25));

        // Act
        await EditarHu(SinCambios with { FechaVencimiento = null });

        // Assert
        Assert.Null(_hu.FechaVencimiento);
    }

    // RN-31, CA-33: mover bajo un padre con vencimiento menor → 400 VENCIMIENTO_POSTERIOR_AL_PADRE
    [Fact]
    public async Task EditarAsync_MueveBajoPadreConVencimientoMenor_LanzaVencimientoPosteriorAlPadre()
    {
        // Arrange
        Existe(Incidencia(5, tipoId: Epica, vencimiento: Nov30));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => EditarHu(SinCambios with { PadreId = 5, FechaVencimiento = Dic10 }));

        // Assert
        Assert.Equal(CodigosError.VencimientoPosteriorAlPadre, ex.Codigo);
    }

    // RN-31, CA-31: editar el vencimiento de la hija por encima del padre → 400
    [Fact]
    public async Task EditarAsync_VencimientoPosteriorAlPadreActual_LanzaVencimientoPosteriorAlPadre()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => EditarHu(SinCambios with { FechaVencimiento = Dic20 }));

        // Assert
        Assert.Equal(CodigosError.VencimientoPosteriorAlPadre, ex.Codigo);
    }

    // RN-36, CA-21: proyecto archivado → ProyectoArchivadoException sin actualizar
    [Fact]
    public async Task EditarAsync_ProyectoArchivado_LanzaProyectoArchivado()
    {
        // Arrange
        ConfigurarProyecto(archivado: true);

        // Act
        var accion = () => EditarHu(SinCambios);

        // Assert
        await Assert.ThrowsAsync<ProyectoArchivadoException>(accion);
        await Incidencias.DidNotReceive().ActualizarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-02: la edición no cambia el tipo (el request no lo acepta)
    [Fact]
    public async Task EditarAsync_Valido_ConservaElTipo()
    {
        // Act
        await EditarHu(SinCambios with { EstadoId = Hecho });

        // Assert
        Assert.Equal(Hu, _hu.TipoId);
    }

    // CA-17: editar estado, prioridad, responsable y vencimiento no toca otras incidencias
    [Fact]
    public async Task EditarAsync_CambiaDatosSinTocarJerarquia_ActualizaSoloLaIncidencia()
    {
        // Act
        await EditarHu(SinCambios with { EstadoId = Hecho, PrioridadId = Media, ResponsableId = null,
            FechaVencimiento = Nov25 });

        // Assert
        Assert.Equal((Hecho, Media, (int?)null, (DateOnly?)Nov25),
            (_hu.EstadoId, _hu.PrioridadId, _hu.ResponsableId, _hu.FechaVencimiento));
        await Incidencias.DidNotReceive().ActualizarAgrupacionDescendientesAsync(Arg.Any<int>(), Arg.Any<int?>(),
            Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    // RN-19: FechaModificacion la asigna el reloj
    [Fact]
    public async Task EditarAsync_Valido_AsignaFechaModificacionDelReloj()
    {
        // Act
        await EditarHu(SinCambios);

        // Assert
        Assert.Equal(DatosPrueba.AhoraTruncado, _hu.FechaModificacion);
    }

    // RN-38, spec 0.3: bloquea el proyecto dentro de la transacción y confirma
    [Fact]
    public async Task EditarAsync_Valido_BloqueaProyectoEnTransaccionYConfirma()
    {
        // Act
        await EditarHu(SinCambios);

        // Assert
        Received.InOrder(() =>
        {
            Uow.IniciarAsync(Arg.Any<CancellationToken>());
            Incidencias.BloquearProyectoAsync(ProyectoId, Arg.Any<CancellationToken>());
            Incidencias.ActualizarAsync(_hu, Arg.Any<CancellationToken>());
            Uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
    }

    // RF-005 RN-34, CA-43: editar con estimación null teniendo 8 deja la incidencia sin estimar
    [Fact]
    public async Task EditarAsync_EstimacionNullTeniendo8_PersisteNull()
    {
        // Arrange
        var hu = Existe(Incidencia(HuId, tipoId: Hu, estadoId: EnProgreso, prioridadId: Alta,
            responsableId: PersonaActiva, vencimiento: Nov30, agrupacionId: AgrupacionPagos, padreId: EpicaPagos,
            estimacion: 8m));

        // Act
        await EditarHu(SinCambios with { Estimacion = null });

        // Assert
        Assert.Null(hu.Estimacion);
        await Incidencias.Received(1).ActualizarAsync(hu, Arg.Any<CancellationToken>());
    }

    // RF-005 RN-34: editar con estimación 12.5 la persiste
    [Fact]
    public async Task EditarAsync_ConEstimacion_PersisteLaEstimacion()
    {
        // Act
        await EditarHu(SinCambios with { Estimacion = 12.5m });

        // Assert
        Assert.Equal(12.5m, _hu.Estimacion);
    }

    // RF-005 RN-35: la estimación del padre no se valida contra la de sus hijas
    [Fact]
    public async Task EditarAsync_PadreConEstimacionMenorQueLaDeSusHijas_Actualiza()
    {
        // Arrange
        Existe(Incidencia(SubtareaId, tipoId: Subtarea, agrupacionId: AgrupacionPagos, padreId: HuId,
            estimacion: 40m));

        // Act
        await EditarHu(SinCambios with { Estimacion = 1m });

        // Assert
        Assert.Equal(1m, _hu.Estimacion);
        await Incidencias.Received(1).ActualizarAsync(_hu, Arg.Any<CancellationToken>());
    }

    // RF-005 RN-34, CA-42: estimación con 3 decimales → ValidationException sin actualizar
    [Fact]
    public async Task EditarAsync_EstimacionConTresDecimales_LanzaValidationExceptionSinActualizar()
    {
        // Act
        var accion = () => EditarHu(SinCambios with { Estimacion = 1.234m });

        // Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(accion);
        await Incidencias.DidNotReceive().ActualizarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }
}
