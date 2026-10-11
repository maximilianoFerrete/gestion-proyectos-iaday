using System.Net;
using System.Net.Http.Json;
using GestionProyectos.Api.Tests.Infraestructura;
using GestionProyectos.Application.EstadosProyecto;
using GestionProyectos.Application.Personas;
using GestionProyectos.Application.UsuarioActual;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionProyectos.Api.Tests.Controllers;

/// <summary>
/// Tests HTTP de personas, estados de proyecto y usuario actual con servicios sustituidos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// Modificado: 2026-10-09 - El admin informa sus permisos en usuario-actual (RF-002, 0.1)
/// Modificado: 2026-10-10 - Persona no admin recibe permisos vacío (tests RF-002, CA-03)
/// </remarks>
public sealed class CatalogosEndpointsTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private static readonly object PersonaBody = new { nombre = "Ana Gómez", email = "Ana.Gomez@Empresa.com", esAdmin = false };
    private static readonly object EstadoBody = new { nombre = "En revisión", descripcion = (string?)null, orden = 35, esFinal = false };

    public void Dispose() => _api.Dispose();

    // CA-01: 201 con Location /api/personas/{id}
    [Fact]
    public async Task CrearPersona_Valido_Devuelve201ConLocation()
    {
        // Arrange
        _api.Personas.CrearAsync(Arg.Any<PersonaRequest>(), Arg.Any<CancellationToken>()).Returns(Dtos.Persona(2));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/personas", PersonaBody);

        // Assert
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/personas/2", respuesta.Headers.Location?.OriginalString);
    }

    // RN-03, CA-02: 409 PERSONA_EMAIL_DUPLICADO
    [Fact]
    public async Task CrearPersona_EmailDuplicado_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Personas.CrearAsync(Arg.Any<PersonaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.PersonaEmailDuplicado, "Duplicado."));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/personas", PersonaBody);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.PersonaEmailDuplicado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-05, CA-03: 403 PERMISO_DENEGADO
    [Fact]
    public async Task CrearPersona_PermisoDenegado_Devuelve403ConCodigo()
    {
        // Arrange
        _api.Personas.CrearAsync(Arg.Any<PersonaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PermisoDenegadoException());

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdNoAdmin.ToString()).PostAsJsonAsync("/api/personas", PersonaBody);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(CodigosError.PermisoDenegado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-05: 401 USUARIO_NO_IDENTIFICADO
    [Fact]
    public async Task CrearPersona_UsuarioNoIdentificado_Devuelve401ConCodigo()
    {
        // Arrange
        _api.Personas.CrearAsync(Arg.Any<PersonaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UsuarioNoIdentificadoException());

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/personas", PersonaBody);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(CodigosError.UsuarioNoIdentificado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-06, CA-04: desactivar al último admin → 409 PERSONA_ULTIMO_ADMIN
    [Fact]
    public async Task DesactivarPersona_UltimoAdmin_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Personas.DesactivarAsync(1, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.PersonaUltimoAdmin, "Último admin."));

        // Act
        var respuesta = await _api.Cliente().PostAsync("/api/personas/1/desactivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.PersonaUltimoAdmin, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-04: acción desactivar → 200 con el DTO
    [Fact]
    public async Task DesactivarPersona_Valido_Devuelve200ConDto()
    {
        // Arrange
        _api.Personas.DesactivarAsync(2, Arg.Any<CancellationToken>()).Returns(Dtos.Persona(2, activo: false));

        // Act
        var respuesta = await _api.Cliente().PostAsync("/api/personas/2/desactivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False((await respuesta.Content.ReadFromJsonAsync<PersonaDto>(Dtos.Json))?.Activo);
    }

    // RN-10, CA-10: marcar-inicial → 200 con el DTO
    [Fact]
    public async Task MarcarInicial_Valido_Devuelve200ConDto()
    {
        // Arrange
        _api.Estados.MarcarInicialAsync(2, Arg.Any<CancellationToken>()).Returns(Dtos.Estado(2, esInicial: true));

        // Act
        var respuesta = await _api.Cliente().PostAsync("/api/estados-proyecto/2/marcar-inicial", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True((await respuesta.Content.ReadFromJsonAsync<EstadoProyectoDto>(Dtos.Json))?.EsInicial);
    }

    // RN-11, CA-11: marcar inicial un estado final/inactivo → 409 ESTADO_NO_ELEGIBLE_INICIAL
    [Fact]
    public async Task MarcarInicial_NoElegible_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Estados.MarcarInicialAsync(5, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.EstadoNoElegibleInicial, "No elegible."));

        // Act
        var respuesta = await _api.Cliente().PostAsync("/api/estados-proyecto/5/marcar-inicial", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.EstadoNoElegibleInicial, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-12, CA-12: desactivar el inicial → 409 ESTADO_INICIAL_NO_DESACTIVABLE
    [Fact]
    public async Task DesactivarEstado_Inicial_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Estados.DesactivarAsync(1, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.EstadoInicialNoDesactivable, "Inicial."));

        // Act
        var respuesta = await _api.Cliente().PostAsync("/api/estados-proyecto/1/desactivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.EstadoInicialNoDesactivable, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-13, CA-13: desactivar → 200 con el DTO
    [Fact]
    public async Task DesactivarEstado_Valido_Devuelve200ConDto()
    {
        // Arrange
        _api.Estados.DesactivarAsync(4, Arg.Any<CancellationToken>()).Returns(Dtos.Estado(4, activo: false));

        // Act
        var respuesta = await _api.Cliente().PostAsync("/api/estados-proyecto/4/desactivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // RN-09, CA-14: editar "Archivado" → 409 ESTADO_SISTEMA_NO_MODIFICABLE
    [Fact]
    public async Task EditarEstado_Sistema_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Estados.EditarAsync(7, Arg.Any<EstadoProyectoRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.EstadoSistemaNoModificable, "Sistema."));

        // Act
        var respuesta = await _api.Cliente().PutAsJsonAsync("/api/estados-proyecto/7", EstadoBody);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.EstadoSistemaNoModificable, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-07, CA-09: nombre duplicado → 409 ESTADO_NOMBRE_DUPLICADO
    [Fact]
    public async Task CrearEstado_NombreDuplicado_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Estados.CrearAsync(Arg.Any<EstadoProyectoRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.EstadoNombreDuplicado, "Duplicado."));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/estados-proyecto", EstadoBody);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.EstadoNombreDuplicado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // CA-07: crear estado → 201 con Location
    [Fact]
    public async Task CrearEstado_Valido_Devuelve201ConLocation()
    {
        // Arrange
        _api.Estados.CrearAsync(Arg.Any<EstadoProyectoRequest>(), Arg.Any<CancellationToken>()).Returns(Dtos.Estado(9));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/estados-proyecto", EstadoBody);

        // Assert
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/estados-proyecto/9", respuesta.Headers.Location?.OriginalString);
    }

    // Spec 0.4: sin header → 200 anónimo
    [Fact]
    public async Task UsuarioActual_SinHeader_Devuelve200Anonimo()
    {
        // Act
        var dto = await _api.Cliente().GetFromJsonAsync<UsuarioActualDto>("/api/usuario-actual", Dtos.Json);

        // Assert
        Assert.Equal(UsuarioActualDto.Anonimo, dto);
    }

    // CA-34, spec 0.4: header de admin activo → 200 con esAdmin true
    [Fact]
    public async Task UsuarioActual_HeaderAdmin_Devuelve200ConDatosDelAdmin()
    {
        // Act
        var dto = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .GetFromJsonAsync<UsuarioActualDto>("/api/usuario-actual", Dtos.Json);

        // Assert
        Assert.Equal(new UsuarioActualDto(true, 1, "Administrador", "admin@gestionproyectos.local", true)
        {
            Permisos = ["Configuracion.Incidencias", "Incidencias.CambiarTipo"], // RF-002 0.1, CA-03
        }, dto);
    }

    // RF-002 RN-03, CA-03: persona no admin → 200 con permisos []
    [Fact]
    public async Task UsuarioActual_HeaderNoAdmin_Devuelve200SinPermisos()
    {
        // Act
        var dto = await _api.Cliente(ApiFactory.IdNoAdmin.ToString())
            .GetFromJsonAsync<UsuarioActualDto>("/api/usuario-actual", Dtos.Json);

        // Assert
        Assert.Equal(new UsuarioActualDto(true, 2, "Ana Gómez", "ana.gomez@empresa.com", false), dto);
        Assert.Empty(dto!.Permisos);
    }

    // CA-35: header de persona inactiva → 401 USUARIO_INVALIDO
    [Fact]
    public async Task UsuarioActual_HeaderPersonaInactiva_Devuelve401UsuarioInvalido()
    {
        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdInactivo.ToString()).GetAsync("/api/usuario-actual");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(CodigosError.UsuarioInvalido, (await Problema.LeerAsync(respuesta)).Codigo);
    }
}
