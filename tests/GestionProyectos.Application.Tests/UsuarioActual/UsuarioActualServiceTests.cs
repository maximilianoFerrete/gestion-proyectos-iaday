using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Application.UsuarioActual;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;

namespace GestionProyectos.Application.Tests.UsuarioActual;

/// <summary>
/// Tests de UsuarioActualService e IdentificacionUsuarioService (spec 0.4, CA-34, CA-35).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// Modificado: 2026-10-10 - Permisos del usuario actual (tests RF-002, 0.1, CA-03)
/// </remarks>
public class UsuarioActualServiceTests
{
    private readonly IPersonaRepository _personas = Substitute.For<IPersonaRepository>();
    private readonly IUsuarioActual _usuario = Substitute.For<IUsuarioActual>();

    private void Identificado(int id)
    {
        _usuario.EstaIdentificado.Returns(true);
        _usuario.PersonaId.Returns(id);
    }

    // Spec 0.4: sin usuario devuelve el anónimo
    [Fact]
    public async Task ObtenerAsync_SinUsuario_DevuelveAnonimo()
    {
        // Arrange
        var sut = new UsuarioActualService(_usuario, _personas);

        // Act
        var dto = await sut.ObtenerAsync(CancellationToken.None);

        // Assert
        Assert.Equal(UsuarioActualDto.Anonimo, dto);
    }

    // CA-34: persona activa admin
    [Fact]
    public async Task ObtenerAsync_PersonaActivaAdmin_DevuelveDatosConEsAdmin()
    {
        // Arrange
        Identificado(1);
        _personas.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(1, esAdmin: true, email: "admin@gestionproyectos.local"));
        var sut = new UsuarioActualService(_usuario, _personas);

        // Act
        var dto = await sut.ObtenerAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new UsuarioActualDto(true, 1, "Persona 1", "admin@gestionproyectos.local", true), dto);
    }

    // RF-002 0.1, CA-03: el admin informa sus permisos ordenados
    [Fact]
    public async Task ObtenerAsync_AdminConPermisos_DevuelvePermisosOrdenados()
    {
        // Arrange
        Identificado(1);
        _usuario.Permisos.Returns([Permisos.IncidenciasCambiarTipo, Permisos.ConfiguracionIncidencias]);
        _personas.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(1, esAdmin: true));
        var sut = new UsuarioActualService(_usuario, _personas);

        // Act
        var dto = await sut.ObtenerAsync(CancellationToken.None);

        // Assert
        Assert.Equal(["Configuracion.Incidencias", "Incidencias.CambiarTipo"], dto.Permisos);
    }

    // RF-002 0.1, CA-03: persona no admin sin permisos → permisos vacío
    [Fact]
    public async Task ObtenerAsync_PersonaSinPermisos_DevuelvePermisosVacio()
    {
        // Arrange
        Identificado(2);
        _usuario.Permisos.Returns([]);
        _personas.ObtenerPorIdAsync(2, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(2));
        var sut = new UsuarioActualService(_usuario, _personas);

        // Act
        var dto = await sut.ObtenerAsync(CancellationToken.None);

        // Assert
        Assert.Empty(dto.Permisos);
    }

    // RF-002 0.1: el anónimo no tiene permisos
    [Fact]
    public void Anonimo_Permisos_Vacio()
    {
        // Act
        var permisos = UsuarioActualDto.Anonimo.Permisos;

        // Assert
        Assert.Empty(permisos);
    }

    // CA-35: persona desactivada después de identificarse → 401 USUARIO_INVALIDO
    [Fact]
    public async Task ObtenerAsync_PersonaInactiva_LanzaUsuarioInvalido()
    {
        // Arrange
        Identificado(5);
        _personas.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(5, activo: false));
        var sut = new UsuarioActualService(_usuario, _personas);

        // Act
        var accion = () => sut.ObtenerAsync(CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UsuarioInvalidoException>(accion);
    }

    // CA-35: persona inexistente → 401 USUARIO_INVALIDO
    [Fact]
    public async Task ObtenerAsync_PersonaInexistente_LanzaUsuarioInvalido()
    {
        // Arrange
        Identificado(99);
        var sut = new UsuarioActualService(_usuario, _personas);

        // Act
        var accion = () => sut.ObtenerAsync(CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UsuarioInvalidoException>(accion);
    }

    // Spec 0.4: la identificación solo acepta personas activas
    [Fact]
    public async Task BuscarActivaAsync_PersonaActiva_DevuelveIdentidad()
    {
        // Arrange
        _personas.ObtenerPorIdAsync(2, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(2));
        var sut = new IdentificacionUsuarioService(_personas);

        // Act
        var identidad = await sut.BuscarActivaAsync(2, CancellationToken.None);

        // Assert
        Assert.Equal(new IdentidadUsuario(2, "Persona 2", "persona@empresa.com", false), identidad);
    }

    // RN-04, CA-35: persona inactiva no puede usarse como usuario actual
    [Fact]
    public async Task BuscarActivaAsync_PersonaInactiva_DevuelveNull()
    {
        // Arrange
        _personas.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(5, activo: false));
        var sut = new IdentificacionUsuarioService(_personas);

        // Act
        var identidad = await sut.BuscarActivaAsync(5, CancellationToken.None);

        // Assert
        Assert.Null(identidad);
    }

    // RN-20: AsegurarAdministrador exige usuario identificado con PersonaId
    [Fact]
    public void AsegurarAdministrador_IdentificadoSinPersonaId_LanzaUsuarioNoIdentificado()
    {
        // Arrange
        _usuario.EstaIdentificado.Returns(true);
        _usuario.EsAdmin.Returns(true);

        // Act
        Action accion = () => _usuario.AsegurarAdministrador();

        // Assert
        Assert.Throws<UsuarioNoIdentificadoException>(accion);
    }
}
