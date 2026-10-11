using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de IncidenciaService.CambiarTipoAsync (RN-02, RN-07, RN-23, RN-25, RN-36, RN-37).
/// Escenario: HU 15 (sin padre) con Subtarea 16 en "En progreso" como hija.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class IncidenciaServiceCambiarTipoTests : ContextoIncidenciaService
{
    private const int HuId = 15;
    private const int SubtareaId = 16;
    private readonly Incidencia _subtarea;

    public IncidenciaServiceCambiarTipoTests()
    {
        Existe(Incidencia(HuId, tipoId: Hu));
        _subtarea = Existe(Incidencia(SubtareaId, tipoId: Subtarea, estadoId: EnProgreso, padreId: HuId));
    }

    private Task<IncidenciaDto> CambiarSubtarea(int tipoId, int? estadoId = null, IUsuarioActual? usuario = null) =>
        Sut(usuario).CambiarTipoAsync(ProyectoId, SubtareaId, new CambiarTipoIncidenciaRequest(tipoId, estadoId),
            CancellationToken.None);

    // RN-02, CA-36: usuario sin el permiso Incidencias.CambiarTipo → 403
    [Fact]
    public async Task CambiarTipoAsync_UsuarioSinPermiso_LanzaPermisoDenegadoSinTransaccion()
    {
        // Act
        var accion = () => CambiarSubtarea(Bug, usuario: Usuario(Permisos.ConfiguracionIncidencias));

        // Assert
        var ex = await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
        Assert.Equal(Permisos.IncidenciasCambiarTipo, ex.Permiso);
        await Uow.DidNotReceive().IniciarAsync(Arg.Any<CancellationToken>());
    }

    // RN-02, CA-36: sin usuario → 401
    [Fact]
    public async Task CambiarTipoAsync_UsuarioNoIdentificado_LanzaUsuarioNoIdentificado()
    {
        // Act
        var accion = () => CambiarSubtarea(Bug, usuario: UsuarioAnonimo());

        // Assert
        await Assert.ThrowsAsync<UsuarioNoIdentificadoException>(accion);
    }

    // Contrato cambiar-tipo: mismo tipo → 200 sin escritura
    [Fact]
    public async Task CambiarTipoAsync_MismoTipo_DevuelveDtoSinActualizar()
    {
        // Act
        var dto = await CambiarSubtarea(Subtarea);

        // Assert
        Assert.Equal(SubtareaId, dto.Id);
        await Incidencias.DidNotReceive().ActualizarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-07: tipo destino inactivo o inexistente → 400 TIPO_INVALIDO
    [Theory]
    [InlineData(TipoInactivo)]
    [InlineData(99)]
    public async Task CambiarTipoAsync_TipoInactivoOInexistente_LanzaTipoInvalido(int tipoId)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => CambiarSubtarea(tipoId));

        // Assert
        Assert.Equal(CodigosError.TipoInvalido, ex.Codigo);
    }

    // RN-23, CA-37: Subtarea → Bug (mismo flujo) conserva el estado
    [Fact]
    public async Task CambiarTipoAsync_SubtareaABugMismoFlujo_CambiaTipoYConservaEstado()
    {
        // Act
        await CambiarSubtarea(Bug);

        // Assert
        Assert.Equal((Bug, EnProgreso), (_subtarea.TipoId, _subtarea.EstadoId));
        await Incidencias.Received(1).ActualizarAsync(_subtarea, Arg.Any<CancellationToken>());
    }

    // RN-25, CA-37: Subtarea hija de HU → Épica (HU → Épica no existe) → 400 RELACION_NO_PERMITIDA
    [Fact]
    public async Task CambiarTipoAsync_SinRelacionConTipoDelPadre_LanzaRelacionNoPermitida()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => CambiarSubtarea(Epica));

        // Assert
        Assert.Equal(CodigosError.RelacionNoPermitida, ex.Codigo);
        await Incidencias.DidNotReceive().ActualizarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-25, CA-38: HU con hija Caso de Prueba → tipo nivel 2 sin relación hacia Caso de Prueba → 409
    [Fact]
    public async Task CambiarTipoAsync_HijaIncompatible_LanzaTipoIncompatibleConHijasConCodigos()
    {
        // Arrange
        ConHijas(HuId, Hija(SubtareaId, tipoId: Subtarea), Hija(17, tipoId: CasoPrueba));

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Sut().CambiarTipoAsync(ProyectoId, HuId,
            new CambiarTipoIncidenciaRequest(HuAlternativa, null), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.TipoIncompatibleConHijas, ex.Codigo);
        Assert.Equal(["PRJ-0001-17"], (string[])ex.Extensiones[ReglasJerarquia.ExtensionIncidencias]!);
    }

    // RN-23, CA-39: tipo con otro flujo sin estadoId y el actual fuera del flujo → inicial del nuevo flujo
    [Fact]
    public async Task CambiarTipoAsync_OtroFlujoSinEstadoId_AsignaInicialDelNuevoFlujo()
    {
        // Act
        await CambiarSubtarea(Defecto);

        // Assert
        Assert.Equal((Defecto, Nuevo), (_subtarea.TipoId, _subtarea.EstadoId));
    }

    // RN-23, CA-39: con un estadoId del nuevo flujo queda en ese estado
    [Fact]
    public async Task CambiarTipoAsync_OtroFlujoConEstadoDelFlujo_AsignaEseEstado()
    {
        // Act
        await CambiarSubtarea(Defecto, Hecho);

        // Assert
        Assert.Equal(Hecho, _subtarea.EstadoId);
    }

    // RN-23, CA-39: con un estadoId ajeno al nuevo flujo → 400 ESTADO_INCIDENCIA_INVALIDO
    [Fact]
    public async Task CambiarTipoAsync_OtroFlujoConEstadoAjeno_LanzaEstadoIncidenciaInvalido()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => CambiarSubtarea(Defecto, PorHacer));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-36, CA-21: proyecto archivado → ProyectoArchivadoException
    [Fact]
    public async Task CambiarTipoAsync_ProyectoArchivado_LanzaProyectoArchivado()
    {
        // Arrange
        ConfigurarProyecto(archivado: true);

        // Act
        var accion = () => CambiarSubtarea(Bug);

        // Assert
        await Assert.ThrowsAsync<ProyectoArchivadoException>(accion);
        await Incidencias.DidNotReceive().ActualizarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-37, CA-20: incidencia de otro proyecto → 404 INCIDENCIA_NO_ENCONTRADA
    [Fact]
    public async Task CambiarTipoAsync_IncidenciaDeOtroProyecto_LanzaIncidenciaNoEncontrada()
    {
        // Arrange
        Existe(Incidencia(40, proyectoId: OtroProyectoId, tipoId: Subtarea));

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => Sut().CambiarTipoAsync(ProyectoId, 40,
            new CambiarTipoIncidenciaRequest(Bug, null), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.IncidenciaNoEncontrada, ex.Codigo);
    }

    // RN-19: el cambio de tipo registra la fecha de modificación
    [Fact]
    public async Task CambiarTipoAsync_Valido_AsignaFechaModificacionDelReloj()
    {
        // Act
        await CambiarSubtarea(Bug);

        // Assert
        Assert.Equal(DatosPrueba.AhoraTruncado, _subtarea.FechaModificacion);
    }
}
