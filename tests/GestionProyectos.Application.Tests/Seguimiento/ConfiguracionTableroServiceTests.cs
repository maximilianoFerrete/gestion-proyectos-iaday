using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de ConfiguracionTableroService: consulta con defaults y guardado con permiso (RN-29 a RN-31).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class ConfiguracionTableroServiceTests
{
    private const int ProyectoId = 1;
    private static readonly ConfiguracionTableroRequest Request = new(true, false, true);
    private readonly IConfiguracionTableroRepository _configuraciones = Substitute.For<IConfiguracionTableroRepository>();
    private readonly IProyectoRepository _proyectos = Substitute.For<IProyectoRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public ConfiguracionTableroServiceTests() => ExisteProyecto(archivado: false);

    private void ExisteProyecto(bool archivado) =>
        _proyectos.ObtenerPorIdAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Proyecto(ProyectoId, archivado: archivado, codigo: "PRJ-0001"));

    private ConfiguracionTableroService Sut(IUsuarioActual? usuario = null) =>
        new(_configuraciones, _proyectos, _uow, usuario ?? Usuario(Permisos.ProyectosConfigurarTablero),
            new ConfiguracionTableroRequestValidator(), DatosPrueba.Reloj(),
            NullLogger<ConfiguracionTableroService>.Instance);

    private Task NoGuardo() =>
        _configuraciones.DidNotReceive().GuardarAsync(Arg.Any<ConfiguracionTablero>(), Arg.Any<CancellationToken>());

    // RN-30, CA-36: sin fila guardada → personalizada false, todo visible y fechaModificacion null
    [Fact]
    public async Task ObtenerAsync_SinConfiguracion_DevuelveDefaults()
    {
        // Act
        var dto = await Sut(UsuarioAnonimo()).ObtenerAsync(ProyectoId, CancellationToken.None);

        // Assert
        Assert.Equal(new ConfiguracionTableroDto(ProyectoId, false, true, true, true, null), dto);
    }

    // RN-30, CA-32: con fila guardada → sus valores y personalizada true
    [Fact]
    public async Task ObtenerAsync_ConConfiguracion_DevuelveLaGuardada()
    {
        // Arrange
        _configuraciones.ObtenerAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(new ConfiguracionTablero(ProyectoId, false, true, false, DatosPrueba.FechaAnterior));

        // Act
        var dto = await Sut().ObtenerAsync(ProyectoId, CancellationToken.None);

        // Assert
        Assert.Equal(new ConfiguracionTableroDto(ProyectoId, true, false, true, false, DatosPrueba.FechaAnterior), dto);
    }

    // CA-35: el GET de configuración funciona para un proyecto archivado
    [Fact]
    public async Task ObtenerAsync_ProyectoArchivado_DevuelveLaConfiguracion()
    {
        // Arrange
        ExisteProyecto(archivado: true);

        // Act
        var dto = await Sut(UsuarioAnonimo()).ObtenerAsync(ProyectoId, CancellationToken.None);

        // Assert
        Assert.False(dto.Personalizada);
    }

    // Contrato GET configuración: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ObtenerAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Sut().ObtenerAsync(99, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RN-31, CA-34: guardar sin usuario → 401 sin llamar al repositorio
    [Fact]
    public async Task GuardarAsync_Anonimo_LanzaUsuarioNoIdentificadoSinGuardar()
    {
        // Act
        var accion = () => Sut(UsuarioAnonimo()).GuardarAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UsuarioNoIdentificadoException>(accion);
        await NoGuardo();
    }

    // RN-31, CA-34: guardar sin el permiso → 403 que nombra el permiso, sin llamar al repositorio
    [Fact]
    public async Task GuardarAsync_SinPermiso_LanzaPermisoDenegadoSinGuardar()
    {
        // Act
        var ex = await Assert.ThrowsAsync<PermisoDenegadoException>(
            () => Sut(Usuario(Permisos.ConfiguracionIncidencias)).GuardarAsync(ProyectoId, Request, CancellationToken.None));

        // Assert
        Assert.Equal("La operación requiere el permiso 'Proyectos.ConfigurarTablero'.", ex.Message);
        await NoGuardo();
    }

    // RN-31, CA-35: proyecto archivado → 409 PROYECTO_ARCHIVADO sin upsert
    [Fact]
    public async Task GuardarAsync_ProyectoArchivado_LanzaProyectoArchivadoSinGuardar()
    {
        // Arrange
        ExisteProyecto(archivado: true);

        // Act
        var ex = await Assert.ThrowsAsync<ProyectoArchivadoException>(
            () => Sut().GuardarAsync(ProyectoId, Request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoArchivado, ex.Codigo);
        await NoGuardo();
    }

    // Contrato PUT configuración: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task GuardarAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Sut().GuardarAsync(99, Request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
        await NoGuardo();
    }

    // RN-31, CA-33: algún mostrar* null → ValidationException sin guardar
    [Fact]
    public async Task GuardarAsync_ValorNull_LanzaValidationExceptionSinGuardar()
    {
        // Act
        var accion = () => Sut().GuardarAsync(ProyectoId, new ConfiguracionTableroRequest(null, true, true),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await NoGuardo();
    }

    // CA-32: guardar válido hace upsert con los tres valores y FechaModificacion del reloj
    [Fact]
    public async Task GuardarAsync_Valido_GuardaLosTresValoresConLaFechaDelReloj()
    {
        // Act
        await Sut().GuardarAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        await _configuraciones.Received(1).GuardarAsync(
            new ConfiguracionTablero(ProyectoId, true, false, true, DatosPrueba.AhoraTruncado),
            Arg.Any<CancellationToken>());
    }

    // CA-32: guardar válido devuelve personalizada = true con los valores y la fecha de modificación
    [Fact]
    public async Task GuardarAsync_Valido_DevuelvePersonalizada()
    {
        // Act
        var dto = await Sut().GuardarAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Assert.Equal(new ConfiguracionTableroDto(ProyectoId, true, true, false, true, DatosPrueba.AhoraTruncado), dto);
    }

    // Spec 0.3: el upsert se ejecuta dentro de una transacción confirmada
    [Fact]
    public async Task GuardarAsync_Valido_GuardaEnTransaccion()
    {
        // Act
        await Sut().GuardarAsync(ProyectoId, Request, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            _uow.IniciarAsync(Arg.Any<CancellationToken>());
            _configuraciones.GuardarAsync(Arg.Any<ConfiguracionTablero>(), Arg.Any<CancellationToken>());
            _uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
    }
}
