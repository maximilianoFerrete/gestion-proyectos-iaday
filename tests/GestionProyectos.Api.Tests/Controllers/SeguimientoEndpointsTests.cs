using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using GestionProyectos.Api.Controllers;
using GestionProyectos.Api.Tests.Infraestructura;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Domain.Excepciones;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionProyectos.Api.Tests.Controllers;

/// <summary>
/// Tests HTTP de /api/seguimiento y /api/proyectos/{p}/seguimiento con los servicios sustituidos (RF-005).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public sealed class SeguimientoEndpointsTests : IDisposable
{
    private const string Base = "/api/proyectos/1/seguimiento";
    private const string Configuracion = Base + "/tablero/configuracion";
    private static readonly DateOnly Hoy = new(2026, 10, 10);
    private static readonly object ConfiguracionBody =
        new { mostrarEstado = true, mostrarEstimacion = false, mostrarResponsable = true };

    private readonly ApiFactory _api = new();

    public SeguimientoEndpointsTests()
    {
        _api.Seguimiento.ObtenerTableroProyectosAsync(Arg.Any<ConsultaTableroProyectosQuery>(),
            Arg.Any<CancellationToken>()).Returns(TableroProyectos());
        _api.Seguimiento.ObtenerSeguimientoAsync(1, Arg.Any<ConsultaSeguimientoQuery>(), Arg.Any<CancellationToken>())
            .Returns(Seguimiento());
        _api.Seguimiento.ObtenerTableroAsync(1, Arg.Any<ConsultaTableroIncidenciasQuery>(),
            Arg.Any<CancellationToken>()).Returns(Tablero());
        _api.ConfiguracionTablero.ObtenerAsync(1, Arg.Any<CancellationToken>()).Returns(ConfiguracionDto(false));
        _api.ConfiguracionTablero.GuardarAsync(1, Arg.Any<ConfiguracionTableroRequest>(), Arg.Any<CancellationToken>())
            .Returns(ConfiguracionDto(true));
    }

    public void Dispose() => _api.Dispose();

    private static AlcanceSeguimientoDto Alcance() => new(true, [], true, []);

    private static IndicadoresDto Indicadores() => new(0, 0, 0, 0, 0, 0, 0, []);

    private static ConfiguracionTableroDto ConfiguracionDto(bool personalizada) =>
        new(1, personalizada, true, !personalizada, true,
            personalizada ? new DateTime(2026, 10, 10, 13, 0, 0, DateTimeKind.Utc) : null);

    private static TableroProyectosDto TableroProyectos() =>
        new(Hoy, Alcance(), Indicadores(), new Paginado<FilaTableroProyectosDto>([], 1, 20, 0));

    private static SeguimientoDto Seguimiento() =>
        new(Hoy, new ProyectoReferenciaIncidenciaDto(1, "PRJ-0001", "Portal", false), "proyecto", null, Alcance(),
            Indicadores(), []);

    private static TableroIncidenciasDto Tablero() =>
        new(Hoy, new ProyectoReferenciaIncidenciaDto(1, "PRJ-0001", "Portal", false), Alcance(),
            new FiltrosTableroDto(null, null, null), ConfiguracionDto(false), 2,
            [
                new ItemTableroDto(15, "PRJ-0001-15", 15, "Pagar con tarjeta", new TipoReferenciaDto(2, "HU", true, 2),
                    new EstadoTableroDto(4, "Bloqueada", 24, true, false, "BLOQUEADA"), 12.5m, true, null),
                new ItemTableroDto(16, "PRJ-0001-16", 16, "Validar", new TipoReferenciaDto(3, "Subtarea", true, 3),
                    new EstadoTableroDto(1, "Por hacer", 10, true, false, null), null, false,
                    new PersonaReferenciaDto(5, "Luis", false)),
            ]);

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    // RN-25, CA-47: los GET de seguimiento, tablero y configuración responden 200 sin X-Usuario-Id
    [Theory]
    [InlineData("/api/seguimiento/proyectos")]
    [InlineData(Base)]
    [InlineData(Base + "/tablero")]
    [InlineData(Configuracion)]
    public async Task Get_SinUsuario_Devuelve200(string url)
    {
        // Act
        var respuesta = await _api.Cliente().GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // RN-25: las acciones GET de seguimiento no tienen [Authorize]
    [Theory]
    [InlineData(typeof(SeguimientoController), nameof(SeguimientoController.TableroProyectos))]
    [InlineData(typeof(SeguimientoProyectoController), nameof(SeguimientoProyectoController.Resumen))]
    [InlineData(typeof(SeguimientoProyectoController), nameof(SeguimientoProyectoController.Tablero))]
    [InlineData(typeof(SeguimientoProyectoController), nameof(SeguimientoProyectoController.ObtenerConfiguracion))]
    public void Get_Endpoint_SinAuthorize(Type controller, string accion)
    {
        // Act
        var atributo = controller.GetMethod(accion)?.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.Null(atributo);
    }

    // RN-31: el PUT de configuración exige la política Proyectos.ConfigurarTablero
    [Fact]
    public void GuardarConfiguracion_Endpoint_TieneAuthorizeConfigurarTablero()
    {
        // Act
        var atributo = typeof(SeguimientoProyectoController)
            .GetMethod(nameof(SeguimientoProyectoController.GuardarConfiguracion))
            ?.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.Equal("Proyectos.ConfigurarTablero", atributo?.Policy);
    }

    // Contrato tablero general: nivelId y tipoId repetibles llegan como listas al servicio
    [Fact]
    public async Task TableroProyectos_IdsRepetidos_LleganComoListas()
    {
        // Act
        await _api.Cliente().GetAsync("/api/seguimiento/proyectos?nivelId=1&nivelId=2&tipoId=3&ordenarPor=vencidas");

        // Assert
        await _api.Seguimiento.Received(1).ObtenerTableroProyectosAsync(
            Arg.Is<ConsultaTableroProyectosQuery>(q => q.NivelId!.SequenceEqual(new[] { 1, 2 })
                && q.TipoId!.SequenceEqual(new[] { 3 }) && q.OrdenarPor == "vencidas"),
            Arg.Any<CancellationToken>());
    }

    // CA-05: el servicio rechaza parámetros inválidos → 400 VALIDACION con la clave en camelCase
    [Fact]
    public async Task TableroProyectos_ValidacionDelServicio_Devuelve400()
    {
        // Arrange
        _api.Seguimiento.ObtenerTableroProyectosAsync(Arg.Any<ConsultaTableroProyectosQuery>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException([new ValidationFailure("TamanioPagina", "Fuera de rango.")]));

        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/seguimiento/proyectos?tamanioPagina=101");

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((400, "VALIDACION"), (problema.Status, problema.Codigo));
        Assert.True(problema.Errors.ContainsKey("tamanioPagina"));
    }

    // RN-01: agrupacionId y sinAgrupacion del resumen llegan al servicio
    [Fact]
    public async Task Resumen_AmbitoYAlcance_LleganAlServicio()
    {
        // Act
        await _api.Cliente().GetAsync($"{Base}?agrupacionId=8&nivelId=2&tipoId=5&tipoId=3");

        // Assert
        await _api.Seguimiento.Received(1).ObtenerSeguimientoAsync(1, Arg.Is<ConsultaSeguimientoQuery>(q =>
            q.AgrupacionId == 8 && q.SinAgrupacion == null && q.NivelId!.SequenceEqual(new[] { 2 })
            && q.TipoId!.SequenceEqual(new[] { 5, 3 })), Arg.Any<CancellationToken>());
    }

    // RN-01, CA-21: AGRUPACION_NO_ENCONTRADA → 404 ProblemDetails con código
    [Fact]
    public async Task Resumen_AgrupacionNoEncontrada_Devuelve404ConCodigo()
    {
        // Arrange
        _api.Seguimiento.ObtenerSeguimientoAsync(1, Arg.Any<ConsultaSeguimientoQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntidadNoEncontradaException(CodigosError.AgrupacionNoEncontrada, "No existe."));

        // Act
        var respuesta = await _api.Cliente().GetAsync($"{Base}?agrupacionId=20");

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((404, "AGRUPACION_NO_ENCONTRADA"), (problema.Status, problema.Codigo));
    }

    // CA-23: PROYECTO_NO_ENCONTRADO en el tablero → 404 con código
    [Fact]
    public async Task Tablero_ProyectoNoEncontrado_Devuelve404ConCodigo()
    {
        // Arrange
        _api.Seguimiento.ObtenerTableroAsync(99, Arg.Any<ConsultaTableroIncidenciasQuery>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado, "No existe."));

        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/proyectos/99/seguimiento/tablero");

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((404, "PROYECTO_NO_ENCONTRADO"), (problema.Status, problema.Codigo));
    }

    // RN-21: indicador, estadoId, texto y sinAgrupacion llegan al servicio
    [Fact]
    public async Task Tablero_TresFiltros_LleganAlServicio()
    {
        // Act
        await _api.Cliente().GetAsync($"{Base}/tablero?indicador=en-espera&estadoId=4&texto=pago&sinAgrupacion=true");

        // Assert
        await _api.Seguimiento.Received(1).ObtenerTableroAsync(1, Arg.Is<ConsultaTableroIncidenciasQuery>(q =>
            q.Indicador == "en-espera" && q.EstadoId == 4 && q.Texto == "pago" && q.SinAgrupacion == true),
            Arg.Any<CancellationToken>());
    }

    // RN-21, CA-26: ValidationException con clave EstadoId → 400 VALIDACION con errors.estadoId
    [Fact]
    public async Task Tablero_EstadoInexistente_Devuelve400ConErrorEnEstadoId()
    {
        // Arrange
        _api.Seguimiento.ObtenerTableroAsync(1, Arg.Any<ConsultaTableroIncidenciasQuery>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException([new ValidationFailure("EstadoId", "El estado no existe.")]));

        // Act
        var respuesta = await _api.Cliente().GetAsync($"{Base}/tablero?estadoId=9999");

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((400, "VALIDACION"), (problema.Status, problema.Codigo));
        Assert.Equal(["El estado no existe."], problema.Errors["estadoId"]);
    }

    // Contrato GET tablero: estadoId no numérico → 400 VALIDACION del binding
    [Fact]
    public async Task Tablero_EstadoIdNoNumerico_Devuelve400()
    {
        // Act
        var respuesta = await _api.Cliente().GetAsync($"{Base}/tablero?estadoId=abc");

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((400, "VALIDACION"), (problema.Status, problema.Codigo));
    }

    // RN-36, contrato: fechaReferencia yyyy-MM-dd, estimacion número o null, vencida booleano, categoria
    [Fact]
    public async Task Tablero_Serializacion_RespetaElContrato()
    {
        // Act
        var json = await JsonAsync(await _api.Cliente().GetAsync($"{Base}/tablero"));

        // Assert
        var items = json.GetProperty("items");
        Assert.Equal("2026-10-10", json.GetProperty("fechaReferencia").GetString());
        Assert.Equal(12.5m, items[0].GetProperty("estimacion").GetDecimal());
        Assert.Equal(JsonValueKind.True, items[0].GetProperty("vencida").ValueKind);
        Assert.Equal("BLOQUEADA", items[0].GetProperty("estado").GetProperty("categoria").GetString());
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("estimacion").ValueKind);
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("estado").GetProperty("categoria").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("filtros").GetProperty("indicador").ValueKind);
    }

    // RN-30, CA-36: GET configuración devuelve los defaults con fechaModificacion null
    [Fact]
    public async Task ObtenerConfiguracion_SinConfiguracion_DevuelveDefaults()
    {
        // Act
        var json = await JsonAsync(await _api.Cliente().GetAsync(Configuracion));

        // Assert
        Assert.False(json.GetProperty("personalizada").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("fechaModificacion").ValueKind);
    }

    // RN-31, CA-34: PUT sin X-Usuario-Id → 401 USUARIO_NO_IDENTIFICADO sin llamar al servicio
    [Fact]
    public async Task GuardarConfiguracion_SinUsuario_Devuelve401()
    {
        // Act
        var respuesta = await _api.Cliente().PutAsJsonAsync(Configuracion, ConfiguracionBody);

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((401, "USUARIO_NO_IDENTIFICADO"), (problema.Status, problema.Codigo));
        await _api.ConfiguracionTablero.DidNotReceive().GuardarAsync(Arg.Any<int>(),
            Arg.Any<ConfiguracionTableroRequest>(), Arg.Any<CancellationToken>());
    }

    // RN-31, CA-34: PUT con persona no admin → 403 PERMISO_DENEGADO que nombra el permiso
    [Fact]
    public async Task GuardarConfiguracion_NoAdmin_Devuelve403ConElPermiso()
    {
        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdNoAdmin.ToString()).PutAsJsonAsync(Configuracion,
            ConfiguracionBody);

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((403, "PERMISO_DENEGADO"), (problema.Status, problema.Codigo));
        Assert.Equal("La operación requiere el permiso 'Proyectos.ConfigurarTablero'.", problema.Detail);
        await _api.ConfiguracionTablero.DidNotReceive().GuardarAsync(Arg.Any<int>(),
            Arg.Any<ConfiguracionTableroRequest>(), Arg.Any<CancellationToken>());
    }

    // CA-32: PUT del admin → 200 con personalizada = true y el request llega al servicio
    [Fact]
    public async Task GuardarConfiguracion_Admin_Devuelve200YPasaElRequest()
    {
        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString()).PutAsJsonAsync(Configuracion,
            ConfiguracionBody);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True((await JsonAsync(respuesta)).GetProperty("personalizada").GetBoolean());
        await _api.ConfiguracionTablero.Received(1).GuardarAsync(1, new ConfiguracionTableroRequest(true, false, true),
            Arg.Any<CancellationToken>());
    }

    // RN-31, CA-33: un campo ausente llega como null al servicio (lo rechaza su validador)
    [Fact]
    public async Task GuardarConfiguracion_SinMostrarResponsable_LlegaNullAlServicio()
    {
        // Act
        await _api.Cliente(ApiFactory.IdAdmin.ToString()).PutAsJsonAsync(Configuracion,
            new { mostrarEstado = true, mostrarEstimacion = true });

        // Assert
        await _api.ConfiguracionTablero.Received(1).GuardarAsync(1, new ConfiguracionTableroRequest(true, true, null),
            Arg.Any<CancellationToken>());
    }

    // RN-31, CA-33: ValidationException del servicio → 400 VALIDACION con la clave del campo
    [Fact]
    public async Task GuardarConfiguracion_ValorNull_Devuelve400ConLaClave()
    {
        // Arrange
        _api.ConfiguracionTablero.GuardarAsync(1, Arg.Any<ConfiguracionTableroRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException([new ValidationFailure("MostrarEstado", "Indique.")]));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString()).PutAsJsonAsync(Configuracion,
            new { mostrarEstado = (bool?)null, mostrarEstimacion = true, mostrarResponsable = true });

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((400, "VALIDACION"), (problema.Status, problema.Codigo));
        Assert.True(problema.Errors.ContainsKey("mostrarEstado"));
    }

    // RN-31, CA-35: proyecto archivado → 409 PROYECTO_ARCHIVADO
    [Fact]
    public async Task GuardarConfiguracion_ProyectoArchivado_Devuelve409ConCodigo()
    {
        // Arrange
        _api.ConfiguracionTablero.GuardarAsync(1, Arg.Any<ConfiguracionTableroRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProyectoArchivadoException("PRJ-0001"));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString()).PutAsJsonAsync(Configuracion,
            ConfiguracionBody);

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((409, "PROYECTO_ARCHIVADO"), (problema.Status, problema.Codigo));
    }

    // Contrato PUT configuración: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task GuardarConfiguracion_ProyectoInexistente_Devuelve404()
    {
        // Arrange
        _api.ConfiguracionTablero.GuardarAsync(99, Arg.Any<ConfiguracionTableroRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado, "No existe."));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .PutAsJsonAsync("/api/proyectos/99/seguimiento/tablero/configuracion", ConfiguracionBody);

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((404, "PROYECTO_NO_ENCONTRADO"), (problema.Status, problema.Codigo));
    }
}
