using FluentValidation;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de IncidenciaService.CrearAsync (RN-18 a RN-22, RN-25 a RN-31, RN-36, RN-38).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// Modificado: 2026-10-11 - Estimación en el alta (tests RF-005, RN-34, CA-41, CA-42)
/// </remarks>
public class IncidenciaServiceCrearTests : ContextoIncidenciaService
{
    private static readonly DateOnly Nov30 = new(2026, 11, 30);
    private static readonly DateOnly Dic01 = new(2026, 12, 1);

    private static readonly CrearIncidenciaRequest Request =
        new(Hu, "Pagar con tarjeta", "Como cliente quiero pagar con tarjeta.", null, null, null, null, null);

    private static CrearIncidenciaRequest HijaDe(int padreId, int tipoId = Subtarea) =>
        Request with { TipoId = tipoId, PadreId = padreId };

    // RN-18, CA-13: número desde el contador del proyecto y código formateado
    [Fact]
    public async Task CrearAsync_ValidoSinPadre_AsignaNumeroYCodigoDelContador()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(15, Agregada!.Numero);
        Assert.Equal("PRJ-0001-15", Agregada.Codigo);
    }

    // RN-20, CA-15: estado = inicial del flujo del tipo (aunque no sea "Por hacer")
    [Fact]
    public async Task CrearAsync_TipoConOtroFlujo_AsignaEstadoInicialDeEseFlujo()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request with { TipoId = Defecto }, CancellationToken.None);

        // Assert
        Assert.Equal(Nuevo, Agregada!.EstadoId);
    }

    // RN-20, CA-15: una Historia de Usuario se crea en "Por hacer"
    [Fact]
    public async Task CrearAsync_HistoriaDeUsuario_AsignaPorHacer()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(PorHacer, Agregada!.EstadoId);
    }

    // RN-19, CA-15: FechaCreacion la asigna el sistema con el reloj (UTC, sin fracción)
    [Fact]
    public async Task CrearAsync_Valido_AsignaFechaCreacionDelReloj()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(DatosPrueba.AhoraTruncado, Agregada!.FechaCreacion);
    }

    // RN-38, spec 0.3: el contador y el insert van dentro de una transacción con commit
    [Fact]
    public async Task CrearAsync_Valido_EjecutaEnTransaccionConCommit()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            Uow.IniciarAsync(Arg.Any<CancellationToken>());
            Incidencias.ObtenerSiguienteNumeroAsync(ProyectoId, Arg.Any<CancellationToken>());
            Incidencias.AgregarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
            Uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
    }

    // Contrato POST: devuelve el detalle de la incidencia creada
    [Fact]
    public async Task CrearAsync_Valido_DevuelveDetalleDeLaCreada()
    {
        // Act
        var dto = await Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(NuevoId, dto.Id);
    }

    // RN-21, CA-15: sin prioridadId usa la predeterminada
    [Fact]
    public async Task CrearAsync_SinPrioridad_UsaLaPredeterminada()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(Media, Agregada!.PrioridadId);
    }

    // RN-21, CA-11: si la predeterminada pasó a ser "Alta", la nueva incidencia toma "Alta"
    [Fact]
    public async Task CrearAsync_SinPrioridadConAltaPredeterminada_UsaAlta()
    {
        // Arrange
        ConfigurarPrioridades(predeterminada: Alta);

        // Act
        await Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(Alta, Agregada!.PrioridadId);
    }

    // RN-21: prioridad inactiva o inexistente → 400 PRIORIDAD_INVALIDA
    [Theory]
    [InlineData(PrioridadInactiva)]
    [InlineData(99)]
    public async Task CrearAsync_PrioridadInactivaOInexistente_LanzaPrioridadInvalida(int prioridadId)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().CrearAsync(ProyectoId,
            Request with { PrioridadId = prioridadId }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PrioridadInvalida, ex.Codigo);
    }

    // RN-07, CA-12: tipo inexistente o inactivo → 400 TIPO_INVALIDO, sin insertar
    [Theory]
    [InlineData(TipoInactivo)]
    [InlineData(99)]
    public async Task CrearAsync_TipoInactivoOInexistente_LanzaTipoInvalido(int tipoId)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().CrearAsync(ProyectoId,
            Request with { TipoId = tipoId }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.TipoInvalido, ex.Codigo);
        await Incidencias.DidNotReceive().AgregarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-22: responsable inactivo o inexistente → 400 RESPONSABLE_INVALIDO
    [Theory]
    [InlineData(PersonaInactiva)]
    [InlineData(99)]
    public async Task CrearAsync_ResponsableInactivoOInexistente_LanzaResponsableInvalido(int responsableId)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().CrearAsync(ProyectoId,
            Request with { ResponsableId = responsableId }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ResponsableInvalido, ex.Codigo);
    }

    // RN-22: responsable opcional
    [Fact]
    public async Task CrearAsync_SinResponsable_CreaSinResponsable()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Assert.Null(Agregada!.ResponsableId);
        await Personas.DidNotReceive().ObtenerPorIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // RN-22: responsable activo se asigna
    [Fact]
    public async Task CrearAsync_ResponsableActivo_AsignaResponsable()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request with { ResponsableId = PersonaActiva }, CancellationToken.None);

        // Assert
        Assert.Equal(PersonaActiva, Agregada!.ResponsableId);
    }

    // RN-28: agrupación de otro proyecto o inexistente → 400 AGRUPACION_INVALIDA
    [Theory]
    [InlineData(AgrupacionOtroProyecto)]
    [InlineData(99)]
    public async Task CrearAsync_AgrupacionDeOtroProyectoOInexistente_LanzaAgrupacionInvalida(int agrupacionId)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().CrearAsync(ProyectoId,
            Request with { AgrupacionId = agrupacionId }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionInvalida, ex.Codigo);
    }

    // RN-28: sin padre, la agrupación del proyecto se asigna
    [Fact]
    public async Task CrearAsync_SinPadreConAgrupacionDelProyecto_AsignaAgrupacion()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request with { AgrupacionId = AgrupacionPagos }, CancellationToken.None);

        // Assert
        Assert.Equal(AgrupacionPagos, Agregada!.AgrupacionId);
    }

    // RN-27, CA-27: padre de otro proyecto → 400 PADRE_INVALIDO
    [Fact]
    public async Task CrearAsync_PadreDeOtroProyecto_LanzaPadreInvalido()
    {
        // Arrange
        Existe(Incidencia(40, proyectoId: OtroProyectoId, tipoId: Hu));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => Sut().CrearAsync(ProyectoId, HijaDe(40), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PadreInvalido, ex.Codigo);
    }

    // RN-27: padre inexistente → 400 PADRE_INVALIDO
    [Fact]
    public async Task CrearAsync_PadreInexistente_LanzaPadreInvalido()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => Sut().CrearAsync(ProyectoId, HijaDe(99), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PadreInvalido, ex.Codigo);
    }

    // RN-25, CA-24: Épica con padre HU (relación inexistente) → 400 RELACION_NO_PERMITIDA
    [Fact]
    public async Task CrearAsync_PadreConRelacionNoPermitida_LanzaRelacionNoPermitida()
    {
        // Arrange
        Existe(Incidencia(15, tipoId: Hu));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => Sut().CrearAsync(ProyectoId, HijaDe(15, Epica), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.RelacionNoPermitida, ex.Codigo);
        await Incidencias.DidNotReceive().AgregarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-25, CA-04, CA-23: hija con relación permitida queda bajo el padre
    [Fact]
    public async Task CrearAsync_PadreConRelacionPermitida_CreaHijaDelPadre()
    {
        // Arrange
        Existe(Incidencia(15, tipoId: Hu));

        // Act
        await Sut().CrearAsync(ProyectoId, HijaDe(15, Defecto), CancellationToken.None);

        // Assert
        Assert.Equal(15, Agregada!.PadreId);
    }

    // RN-25, CA-25: el padre es opcional para todos los tipos (Subtarea sin padre)
    [Fact]
    public async Task CrearAsync_SubtareaSinPadre_Crea()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request with { TipoId = Subtarea }, CancellationToken.None);

        // Assert
        Assert.Null(Agregada!.PadreId);
        Assert.Equal(Subtarea, Agregada.TipoId);
    }

    // RN-28, RN-29, CA-23: con padre y agrupacionId null hereda la del padre
    [Fact]
    public async Task CrearAsync_ConPadreSinAgrupacion_HeredaLaDelPadre()
    {
        // Arrange
        Existe(Incidencia(15, tipoId: Hu, agrupacionId: AgrupacionPagos));

        // Act
        await Sut().CrearAsync(ProyectoId, HijaDe(15), CancellationToken.None);

        // Assert
        Assert.Equal(AgrupacionPagos, Agregada!.AgrupacionId);
    }

    // RN-28, CA-29: con padre y agrupacionId distinto → 400 AGRUPACION_HEREDADA
    [Fact]
    public async Task CrearAsync_ConPadreYAgrupacionDistinta_LanzaAgrupacionHeredada()
    {
        // Arrange
        Existe(Incidencia(15, tipoId: Hu, agrupacionId: AgrupacionPagos));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().CrearAsync(ProyectoId,
            HijaDe(15) with { AgrupacionId = AgrupacionEnvios }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionHeredada, ex.Codigo);
    }

    // RN-31, CA-31: vencimiento posterior al del padre → 400 VENCIMIENTO_POSTERIOR_AL_PADRE
    [Fact]
    public async Task CrearAsync_VencimientoPosteriorAlPadre_LanzaVencimientoPosteriorAlPadre()
    {
        // Arrange
        Existe(Incidencia(15, tipoId: Hu, vencimiento: Nov30));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().CrearAsync(ProyectoId,
            HijaDe(15) with { FechaVencimiento = Dic01 }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.VencimientoPosteriorAlPadre, ex.Codigo);
    }

    // RN-31, CA-31: vencimiento igual al del padre se acepta
    [Fact]
    public async Task CrearAsync_VencimientoIgualAlPadre_Crea()
    {
        // Arrange
        Existe(Incidencia(15, tipoId: Hu, vencimiento: Nov30));

        // Act
        await Sut().CrearAsync(ProyectoId, HijaDe(15) with { FechaVencimiento = Nov30 }, CancellationToken.None);

        // Assert
        Assert.Equal(Nov30, Agregada!.FechaVencimiento);
    }

    // RN-36, CA-21: proyecto archivado → ProyectoArchivadoException, sin insertar y con rollback
    [Fact]
    public async Task CrearAsync_ProyectoArchivado_LanzaProyectoArchivadoSinInsertar()
    {
        // Arrange
        ConfigurarProyecto(archivado: true);

        // Act
        var accion = () => Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProyectoArchivadoException>(accion);
        await Incidencias.DidNotReceive().AgregarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
        await Uow.Received(1).RevertirAsync(Arg.Any<CancellationToken>());
    }

    // Contrato POST: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task CrearAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Sut().CrearAsync(99, Request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RN-38: si falla el insert se revierte la transacción (incluido el contador) y no se confirma
    [Fact]
    public async Task CrearAsync_FallaElInsert_RevierteSinConfirmar()
    {
        // Arrange
        Incidencias.AgregarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Error de BD"));

        // Act
        var accion = () => Sut().CrearAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        await Uow.Received(1).RevertirAsync(Arg.Any<CancellationToken>());
        await Uow.DidNotReceive().ConfirmarAsync(Arg.Any<CancellationToken>());
    }

    // RN-19, CA-16: título vacío → ValidationException antes de abrir la transacción
    [Fact]
    public async Task CrearAsync_TituloVacio_LanzaValidationExceptionSinTransaccion()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Sut().CrearAsync(ProyectoId,
            Request with { Titulo = "   " }, CancellationToken.None));

        // Assert
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CrearIncidenciaRequest.Titulo));
        await Uow.DidNotReceive().IniciarAsync(Arg.Any<CancellationToken>());
    }

    // RN-39: título y descripción se guardan recortados
    [Fact]
    public async Task CrearAsync_TextosConEspacios_GuardaRecortados()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request with { Titulo = "  Pagar  ", Descripcion = " Desc " },
            CancellationToken.None);

        // Assert
        Assert.Equal(("Pagar", "Desc"), (Agregada!.Titulo, Agregada.Descripcion));
    }

    // RF-005 RN-34, CA-41: crear con estimación 2.5 la persiste
    [Fact]
    public async Task CrearAsync_ConEstimacion_PersisteLaEstimacion()
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request with { Estimacion = 2.5m }, CancellationToken.None);

        // Assert
        Assert.Equal(2.5m, Agregada!.Estimacion);
    }

    // RF-005 RN-34, CA-41: crear sin estimación la persiste como null (y 0 se conserva como 0)
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task CrearAsync_SinEstimacionOCero_PersisteElValor(int? estimacion)
    {
        // Act
        await Sut().CrearAsync(ProyectoId, Request with { Estimacion = estimacion }, CancellationToken.None);

        // Assert
        Assert.Equal((decimal?)estimacion, Agregada!.Estimacion);
    }

    // RF-005 RN-34, CA-42: estimación negativa → ValidationException sin insertar
    [Fact]
    public async Task CrearAsync_EstimacionNegativa_LanzaValidationExceptionSinInsertar()
    {
        // Act
        var accion = () => Sut().CrearAsync(ProyectoId, Request with { Estimacion = -1m }, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await Incidencias.DidNotReceive().AgregarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }
}
