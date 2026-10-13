using System.Net;
using System.Net.Http.Json;
using GestionProyectos.Api.Tests.Infraestructura;
using GestionProyectos.Application.EstadosIncidencia;
using GestionProyectos.Application.FlujosIncidencia;
using GestionProyectos.Application.RelacionesTiposIncidencia;
using GestionProyectos.Application.TiposIncidencia;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionProyectos.Api.Tests.Controllers;

/// <summary>
/// Tests HTTP de la configuración de incidencias (niveles, tipos, relaciones, estados, flujos, prioridades).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// Modificado: 2026-10-11 - Categoría de estados y RN-18 en flujos (tests RF-005)
/// </remarks>
public sealed class ConfiguracionIncidenciasEndpointsTests : IDisposable
{
    private readonly ApiFactory _api = new();

    public void Dispose() => _api.Dispose();

    /// <summary>Endpoints de escritura de configuración (método, ruta) protegidos por permiso.</summary>
    public static TheoryData<string, string> EscriturasDeConfiguracion => new()
    {
        { "POST", "/api/niveles-incidencia" },
        { "PUT", "/api/niveles-incidencia/2" },
        { "POST", "/api/tipos-incidencia" },
        { "PUT", "/api/tipos-incidencia/2" },
        { "POST", "/api/tipos-incidencia/2/desactivar" },
        { "POST", "/api/tipos-incidencia/2/activar" },
        { "POST", "/api/relaciones-tipos-incidencia" },
        { "POST", "/api/estados-incidencia" },
        { "PUT", "/api/estados-incidencia/2" },
        { "POST", "/api/estados-incidencia/2/desactivar" },
        { "POST", "/api/estados-incidencia/2/activar" },
        { "POST", "/api/flujos-incidencia" },
        { "PUT", "/api/flujos-incidencia/1" },
        { "POST", "/api/prioridades-incidencia" },
        { "PUT", "/api/prioridades-incidencia/2" },
        { "POST", "/api/prioridades-incidencia/2/marcar-predeterminada" },
        { "POST", "/api/prioridades-incidencia/2/desactivar" },
        { "POST", "/api/prioridades-incidencia/2/activar" },
    };

    private static HttpRequestMessage Solicitud(string metodo, string ruta) =>
        new(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };

    // RN-01, CA-02: usuario no admin → 403 PERMISO_DENEGADO con el permiso en el detalle
    [Theory]
    [MemberData(nameof(EscriturasDeConfiguracion))]
    public async Task Escritura_UsuarioNoAdmin_Devuelve403PermisoDenegado(string metodo, string ruta)
    {
        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdNoAdmin.ToString()).SendAsync(Solicitud(metodo, ruta));

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((403, CodigosError.PermisoDenegado), (problema.Status, problema.Codigo));
        Assert.Equal("La operación requiere el permiso 'Configuracion.Incidencias'.", problema.Detail);
    }

    // RN-01, CA-02: sin usuario → 401 USUARIO_NO_IDENTIFICADO
    [Theory]
    [MemberData(nameof(EscriturasDeConfiguracion))]
    public async Task Escritura_SinUsuario_Devuelve401UsuarioNoIdentificado(string metodo, string ruta)
    {
        // Act
        var respuesta = await _api.Cliente().SendAsync(Solicitud(metodo, ruta));

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((401, CodigosError.UsuarioNoIdentificado), (problema.Status, problema.Codigo));
    }

    // RN-01, CA-04: admin crea un tipo → 201 con Location
    [Fact]
    public async Task CrearTipo_Admin_Devuelve201ConLocation()
    {
        // Arrange
        _api.Tipos.CrearAsync(Arg.Any<CrearTipoIncidenciaRequest>(), Arg.Any<CancellationToken>()).Returns(Dtos.Tipo(6));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString()).PostAsJsonAsync("/api/tipos-incidencia",
            new { nombre = "Spike", descripcion = "Investigación acotada.", nivelId = 3, flujoId = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/tipos-incidencia/6", respuesta.Headers.Location?.OriginalString);
    }

    // RN-08, CA-04: admin crea una relación → 201 con Location informativo
    [Fact]
    public async Task CrearRelacion_Admin_Devuelve201ConLocation()
    {
        // Arrange
        _api.Relaciones.CrearAsync(Arg.Any<CrearRelacionTipoIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Relacion(7));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .PostAsJsonAsync("/api/relaciones-tipos-incidencia", new { tipoPadreId = 2, tipoHijoId = 6 });

        // Assert
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/relaciones-tipos-incidencia/7", respuesta.Headers.Location?.OriginalString);
    }

    // RN-08, CA-05: 400 RELACION_NIVELES_INVALIDOS
    [Fact]
    public async Task CrearRelacion_NivelesInvalidos_Devuelve400ConCodigo()
    {
        // Arrange
        _api.Relaciones.CrearAsync(Arg.Any<CrearRelacionTipoIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidacionNegocioException(CodigosError.RelacionNivelesInvalidos, "Niveles."));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .PostAsJsonAsync("/api/relaciones-tipos-incidencia", new { tipoPadreId = 3, tipoHijoId = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(CodigosError.RelacionNivelesInvalidos, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-09, CA-06: PUT y DELETE de una relación no existen → 404
    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Relacion_PutODeletePorId_Devuelve404(string metodo)
    {
        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .SendAsync(Solicitud(metodo, "/api/relaciones-tipos-incidencia/3"));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    // RN-01: las consultas de configuración no exigen usuario
    [Theory]
    [InlineData("/api/niveles-incidencia")]
    [InlineData("/api/tipos-incidencia?soloActivos=true&nivelId=3")]
    [InlineData("/api/relaciones-tipos-incidencia?tipoPadreId=2")]
    [InlineData("/api/estados-incidencia")]
    [InlineData("/api/flujos-incidencia")]
    [InlineData("/api/prioridades-incidencia?soloActivas=true")]
    public async Task Consulta_SinUsuario_Devuelve200(string ruta)
    {
        // Act
        var respuesta = await _api.Cliente().GetAsync(ruta);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // RN-12, CA-09: 409 TIPO_FLUJO_INCOMPATIBLE
    [Fact]
    public async Task EditarTipo_FlujoIncompatible_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Tipos.EditarAsync(2, Arg.Any<EditarTipoIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.TipoFlujoIncompatible, "Incompatible."));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString()).PutAsJsonAsync("/api/tipos-incidencia/2",
            new { nombre = "HU", descripcion = (string?)null, flujoId = 2 });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.TipoFlujoIncompatible, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RF-005 RN-18, CA-39: PUT estado con categoría incompatible → 409 ESTADO_CATEGORIA_INCOMPATIBLE
    [Fact]
    public async Task EditarEstado_CategoriaIncompatible_Devuelve409ConCodigo()
    {
        // Arrange
        _api.EstadosIncidencia.EditarAsync(3, Arg.Any<EstadoIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.EstadoCategoriaIncompatible, "Incompatible."));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString()).PutAsJsonAsync("/api/estados-incidencia/3",
            new { nombre = "Hecho", descripcion = (string?)null, orden = 30, categoria = "BLOQUEADA" });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("ESTADO_CATEGORIA_INCOMPATIBLE", (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RF-005 RN-17: la categoría del request de estado llega al servicio
    [Fact]
    public async Task CrearEstado_ConCategoria_LlegaLaCategoriaAlServicio()
    {
        // Act
        await _api.Cliente(ApiFactory.IdAdmin.ToString()).PostAsJsonAsync("/api/estados-incidencia",
            new { nombre = "Esperando cliente", descripcion = "Aguarda.", orden = 22, categoria = "EN_ESPERA" });

        // Assert
        await _api.EstadosIncidencia.Received(1).CrearAsync(
            new EstadoIncidenciaRequest("Esperando cliente", "Aguarda.", 22, "EN_ESPERA"), Arg.Any<CancellationToken>());
    }

    // RF-005 RN-18 (b), CA-39: POST flujo con estado con categoría como final → 409 con código
    [Fact]
    public async Task CrearFlujo_CategoriaIncompatible_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Flujos.CrearAsync(Arg.Any<FlujoIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.EstadoCategoriaIncompatible, "Incompatible."));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString()).PostAsJsonAsync("/api/flujos-incidencia",
            new { nombre = "F", descripcion = (string?)null, estados = new[] { new { estadoId = 4, esInicial = false, esFinal = true } } });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("ESTADO_CATEGORIA_INCOMPATIBLE", (await Problema.LeerAsync(respuesta)).Codigo);
    }
}
