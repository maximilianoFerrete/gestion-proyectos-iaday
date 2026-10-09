using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Personas;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GestionProyectos.Application.Tests.Personas;

/// <summary>
/// Tests de PersonaService (RN-01 a RN-06).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class PersonaServiceTests
{
    private readonly IPersonaRepository _repositorio = Substitute.For<IPersonaRepository>();
    private readonly IUsuarioActual _usuario = Substitute.For<IUsuarioActual>();
    private readonly PersonaService _sut;

    public PersonaServiceTests()
    {
        _sut = new PersonaService(_repositorio, _usuario, new PersonaRequestValidator(),
            DatosPrueba.Reloj(), NullLogger<PersonaService>.Instance);
    }

    private void UsuarioAdmin() => ConfigurarUsuario(1, esAdmin: true);

    private void UsuarioNoAdmin() => ConfigurarUsuario(2, esAdmin: false);

    private void ConfigurarUsuario(int id, bool esAdmin)
    {
        _usuario.EstaIdentificado.Returns(true);
        _usuario.PersonaId.Returns(id);
        _usuario.EsAdmin.Returns(esAdmin);
    }

    // RN-02, CA-01: email guardado con trim y en minúsculas
    [Fact]
    public async Task CrearAsync_EmailConMayusculasYEspacios_GuardaEmailRecortadoEnMinusculas()
    {
        // Arrange
        Persona? capturada = null;
        await _repositorio.AgregarAsync(Arg.Do<Persona>(p => capturada = p), Arg.Any<CancellationToken>());
        var request = new PersonaRequest("  Ana Gómez ", "  Ana.Gomez@Empresa.com ", false);

        // Act
        var dto = await _sut.CrearAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(capturada);
        Assert.Equal("ana.gomez@empresa.com", capturada.Email);
        Assert.Equal("Ana Gómez", capturada.Nombre);
        Assert.Equal("ana.gomez@empresa.com", dto.Email);
    }

    // Spec 0.2: fecha de creación tomada del TimeProvider (UTC)
    [Fact]
    public async Task CrearAsync_Valido_CreaActivaConFechaDelReloj()
    {
        // Arrange
        var request = new PersonaRequest("Ana Gómez", "ana@empresa.com", false);

        // Act
        var dto = await _sut.CrearAsync(request, CancellationToken.None);

        // Assert
        Assert.True(dto.Activo);
        Assert.Equal(DatosPrueba.AhoraTruncado, dto.FechaCreacion);
        Assert.Null(dto.FechaModificacion);
    }

    // RN-03, CA-02: email duplicado sin distinguir mayúsculas
    [Fact]
    public async Task CrearAsync_EmailExistenteConOtraCapitalizacion_LanzaConflictoEmailDuplicado()
    {
        // Arrange
        _repositorio.ExisteEmailAsync("ana.gomez@empresa.com", null, Arg.Any<CancellationToken>())
            .Returns(true);
        var request = new PersonaRequest("Ana", "ANA.GOMEZ@empresa.com", false);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.CrearAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PersonaEmailDuplicado, ex.Codigo);
        await _repositorio.DidNotReceive().AgregarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-05: crear admin sin usuario → 401
    [Fact]
    public async Task CrearAsync_EsAdminSinUsuario_LanzaUsuarioNoIdentificado()
    {
        // Arrange
        var request = new PersonaRequest("Ana", "ana@empresa.com", true);

        // Act
        var accion = () => _sut.CrearAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UsuarioNoIdentificadoException>(accion);
        await _repositorio.DidNotReceive().AgregarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-05, CA-03: crear admin con usuario no admin → 403
    [Fact]
    public async Task CrearAsync_EsAdminUsuarioNoAdmin_LanzaPermisoDenegado()
    {
        // Arrange
        UsuarioNoAdmin();
        var request = new PersonaRequest("Ana", "ana@empresa.com", true);

        // Act
        var accion = () => _sut.CrearAsync(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
        await _repositorio.DidNotReceive().AgregarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-05, CA-01: un admin puede crear administradores
    [Fact]
    public async Task CrearAsync_EsAdminUsuarioAdmin_CreaAdministrador()
    {
        // Arrange
        UsuarioAdmin();
        var request = new PersonaRequest("Ana", "ana@empresa.com", true);

        // Act
        var dto = await _sut.CrearAsync(request, CancellationToken.None);

        // Assert
        Assert.True(dto.EsAdmin);
        await _repositorio.Received(1).AgregarAsync(Arg.Is<Persona>(p => p.EsAdmin), Arg.Any<CancellationToken>());
    }

    // RN-01, RN-31: nombre solo con espacios es inválido
    [Fact]
    public async Task CrearAsync_NombreSoloEspacios_LanzaValidationException()
    {
        // Arrange
        var request = new PersonaRequest("   ", "ana@empresa.com", false);

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CrearAsync(request, CancellationToken.None));

        // Assert
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(PersonaRequest.Nombre));
        await _repositorio.DidNotReceive().AgregarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-05: editar sin cambiar esAdmin no requiere admin
    [Fact]
    public async Task EditarAsync_SinCambiarEsAdminUsuarioNoAdmin_Actualiza()
    {
        // Arrange
        UsuarioNoAdmin();
        _repositorio.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(5));
        var request = new PersonaRequest("Nuevo nombre", "nuevo@empresa.com", false);

        // Act
        var dto = await _sut.EditarAsync(5, request, CancellationToken.None);

        // Assert
        Assert.Equal("Nuevo nombre", dto.Nombre);
        Assert.Equal(DatosPrueba.AhoraTruncado, dto.FechaModificacion);
        await _repositorio.Received(1).ActualizarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-05: cambiar esAdmin en cualquier sentido exige admin
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task EditarAsync_CambiandoEsAdminUsuarioNoAdmin_LanzaPermisoDenegado(bool actual, bool nuevo)
    {
        // Arrange
        UsuarioNoAdmin();
        _repositorio.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(5, esAdmin: actual));
        var request = new PersonaRequest("Ana", "ana@empresa.com", nuevo);

        // Act
        var accion = () => _sut.EditarAsync(5, request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-05: cambiar esAdmin sin usuario → 401
    [Fact]
    public async Task EditarAsync_CambiandoEsAdminSinUsuario_LanzaUsuarioNoIdentificado()
    {
        // Arrange
        _repositorio.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(5));
        var request = new PersonaRequest("Ana", "ana@empresa.com", true);

        // Act
        var accion = () => _sut.EditarAsync(5, request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UsuarioNoIdentificadoException>(accion);
    }

    // RN-06, CA-04: no se puede quitar esAdmin al último admin activo
    [Fact]
    public async Task EditarAsync_QuitandoEsAdminAlUltimoAdminActivo_LanzaConflictoUltimoAdmin()
    {
        // Arrange
        UsuarioAdmin();
        _repositorio.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(1, esAdmin: true));
        _repositorio.ContarAdministradoresActivosAsync(Arg.Any<CancellationToken>()).Returns(1);
        var request = new PersonaRequest("Administrador", "admin@empresa.com", false);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.EditarAsync(1, request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PersonaUltimoAdmin, ex.Codigo);
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-06: con otro admin activo se puede quitar esAdmin
    [Fact]
    public async Task EditarAsync_QuitandoEsAdminConOtroAdminActivo_Actualiza()
    {
        // Arrange
        UsuarioAdmin();
        _repositorio.ObtenerPorIdAsync(4, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(4, esAdmin: true));
        _repositorio.ContarAdministradoresActivosAsync(Arg.Any<CancellationToken>()).Returns(2);
        var request = new PersonaRequest("Ana", "ana@empresa.com", false);

        // Act
        var dto = await _sut.EditarAsync(4, request, CancellationToken.None);

        // Assert
        Assert.False(dto.EsAdmin);
    }

    // RN-03: el email no puede coincidir con el de otra persona (se excluye la propia)
    [Fact]
    public async Task EditarAsync_EmailDeOtraPersona_LanzaConflictoEmailDuplicado()
    {
        // Arrange
        _repositorio.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(5));
        _repositorio.ExisteEmailAsync("otro@empresa.com", 5, Arg.Any<CancellationToken>()).Returns(true);
        var request = new PersonaRequest("Ana", "Otro@Empresa.com", false);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.EditarAsync(5, request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PersonaEmailDuplicado, ex.Codigo);
    }

    // Contrato PUT /api/personas/{id}: 404 PERSONA_NO_ENCONTRADA
    [Fact]
    public async Task EditarAsync_IdInexistente_LanzaPersonaNoEncontrada()
    {
        // Arrange
        var request = new PersonaRequest("Ana", "ana@empresa.com", false);

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.EditarAsync(99, request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PersonaNoEncontrada, ex.Codigo);
    }

    // RN-06, CA-04: no se puede desactivar al último admin activo
    [Fact]
    public async Task DesactivarAsync_UltimoAdminActivo_LanzaConflictoUltimoAdmin()
    {
        // Arrange
        _repositorio.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(1, esAdmin: true));
        _repositorio.ContarAdministradoresActivosAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.DesactivarAsync(1, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PersonaUltimoAdmin, ex.Codigo);
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-06: con otro admin activo se puede desactivar a un admin
    [Fact]
    public async Task DesactivarAsync_AdminConOtroAdminActivo_Desactiva()
    {
        // Arrange
        _repositorio.ObtenerPorIdAsync(4, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(4, esAdmin: true));
        _repositorio.ContarAdministradoresActivosAsync(Arg.Any<CancellationToken>()).Returns(2);

        // Act
        var dto = await _sut.DesactivarAsync(4, CancellationToken.None);

        // Assert
        Assert.False(dto.Activo);
        await _repositorio.Received(1).ActualizarAsync(Arg.Is<Persona>(p => !p.Activo), Arg.Any<CancellationToken>());
    }

    // RN-04: desactivar es idempotente
    [Fact]
    public async Task DesactivarAsync_PersonaYaInactiva_DevuelveSinActualizar()
    {
        // Arrange
        _repositorio.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(5, activo: false));

        // Act
        var dto = await _sut.DesactivarAsync(5, CancellationToken.None);

        // Assert
        Assert.False(dto.Activo);
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // Contrato POST /desactivar: 404 PERSONA_NO_ENCONTRADA
    [Fact]
    public async Task DesactivarAsync_IdInexistente_LanzaPersonaNoEncontrada()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.DesactivarAsync(99, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PersonaNoEncontrada, ex.Codigo);
    }

    // RN-04: una persona inactiva se puede reactivar
    [Fact]
    public async Task ActivarAsync_PersonaInactiva_ActivaYActualiza()
    {
        // Arrange
        _repositorio.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(5, activo: false));

        // Act
        var dto = await _sut.ActivarAsync(5, CancellationToken.None);

        // Assert
        Assert.True(dto.Activo);
        await _repositorio.Received(1).ActualizarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-04: activar es idempotente
    [Fact]
    public async Task ActivarAsync_PersonaYaActiva_NoActualiza()
    {
        // Arrange
        _repositorio.ObtenerPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(5));

        // Act
        await _sut.ActivarAsync(5, CancellationToken.None);

        // Assert
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<Persona>(), Arg.Any<CancellationToken>());
    }

    // RN-31: el filtro de texto se recorta antes de consultar
    [Fact]
    public async Task ListarAsync_TextoConEspacios_ConsultaConTextoRecortado()
    {
        // Arrange
        _repositorio.ListarAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([DatosPrueba.Persona(2)]);

        // Act
        var lista = await _sut.ListarAsync("  ana ", true, CancellationToken.None);

        // Assert
        Assert.Single(lista);
        await _repositorio.Received(1).ListarAsync("ana", true, Arg.Any<CancellationToken>());
    }
}
