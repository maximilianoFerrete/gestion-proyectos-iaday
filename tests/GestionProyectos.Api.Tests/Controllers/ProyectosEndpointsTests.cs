using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using GestionProyectos.Api.Tests.Infraestructura;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionProyectos.Api.Tests.Controllers;

/// <summary>
/// Tests HTTP de /api/proyectos con el servicio sustituido y el manejo global de errores real.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public sealed class ProyectosEndpointsTests : IDisposable
{
    private readonly ApiFactory _api = new();

    private static readonly object AltaValida = new
    {
        nombre = "Portal clientes",
        descripcion = "Nuevo portal de autogestión.",
        responsableId = 2,
        fechaInicio = "2026-10-01",
        fechaObjetivo = "2026-12-15",
    };

    private static readonly object EdicionValida = new
    {
        nombre = "Portal clientes v2",
        descripcion = "Nuevo portal de autogestión.",
        responsableId = 2,
        fechaInicio = "2026-10-01",
        fechaObjetivo = "2027-01-31",
        estadoId = 3,
    };

    public void Dispose() => _api.Dispose();

    // Contrato POST /api/proyectos, CA-15: 201 con Location y ProyectoDto
    [Fact]
    public async Task Crear_Valido_Devuelve201ConLocationYBody()
    {
        // Arrange
        _api.Proyectos.CrearAsync(Arg.Any<CrearProyectoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Proyecto(3));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/proyectos", AltaValida);

        // Assert
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/proyectos/3", respuesta.Headers.Location?.OriginalString);
        var dto = await respuesta.Content.ReadFromJsonAsync<ProyectoDto>(Dtos.Json);
        Assert.Equal("PRJ-0003", dto?.Codigo);
    }

    // Spec 0.2: JSON camelCase y fechas DATE como yyyy-MM-dd
    [Fact]
    public async Task Crear_Valido_SerializaCamelCaseYFechasSinHora()
    {
        // Arrange
        _api.Proyectos.CrearAsync(Arg.Any<CrearProyectoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Proyecto(3));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/proyectos", AltaValida);

        // Assert
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("2026-10-01", json.RootElement.GetProperty("fechaInicio").GetString());
        Assert.Equal("Ana Gómez", json.RootElement.GetProperty("responsable").GetProperty("nombre").GetString());
    }

    // Contrato POST: el body llega al servicio con los campos del contrato (estadoId se ignora)
    [Fact]
    public async Task Crear_Valido_PasaElRequestAlServicio()
    {
        // Arrange
        _api.Proyectos.CrearAsync(Arg.Any<CrearProyectoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Proyecto(3));

        // Act
        await _api.Cliente().PostAsJsonAsync("/api/proyectos", AltaValida);

        // Assert
        var esperado = new CrearProyectoRequest("Portal clientes", "Nuevo portal de autogestión.", 2,
            new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 15));
        await _api.Proyectos.Received(1).CrearAsync(esperado, Arg.Any<CancellationToken>());
    }

    // CA-28: 404 ProblemDetails con codigo PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task Obtener_ServicioLanzaNoEncontrado_Devuelve404ConCodigo()
    {
        // Arrange
        _api.Proyectos.ObtenerAsync(99, Arg.Any<CancellationToken>()).ThrowsAsync(
            new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado, "No existe el proyecto con id 99."));

        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/proyectos/99");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal(CodigosError.ProyectoNoEncontrado, problema.Codigo);
        Assert.Equal("No existe el proyecto con id 99.", problema.Detail);
    }

    // Spec 0.3: ProblemDetails con type, title, instance y traceId
    [Fact]
    public async Task Obtener_ServicioLanzaNoEncontrado_DevuelveProblemDetailsCompleto()
    {
        // Arrange
        _api.Proyectos.ObtenerAsync(99, Arg.Any<CancellationToken>()).ThrowsAsync(
            new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado, "No existe."));

        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/proyectos/99");

        // Assert
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        Assert.Equal("https://httpstatuses.io/404", raiz.GetProperty("type").GetString());
        Assert.Equal("No encontrado", raiz.GetProperty("title").GetString());
        Assert.Equal("/api/proyectos/99", raiz.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrEmpty(raiz.GetProperty("traceId").GetString()));
    }

    // Spec 0.3: ProblemDetails con content-type application/problem+json
    [Fact]
    public async Task Obtener_ServicioLanzaNoEncontrado_DevuelveContentTypeProblemJson()
    {
        // Arrange
        _api.Proyectos.ObtenerAsync(99, Arg.Any<CancellationToken>()).ThrowsAsync(
            new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado, "No existe."));

        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/proyectos/99");

        // Assert
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    // RN-22, CA-23: editar un archivado → 409 PROYECTO_ARCHIVADO
    [Fact]
    public async Task Editar_ProyectoArchivado_Devuelve409ProyectoArchivado()
    {
        // Arrange
        _api.Proyectos.EditarAsync(3, Arg.Any<EditarProyectoRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProyectoArchivadoException("PRJ-0003"));

        // Act
        var respuesta = await _api.Cliente().PutAsJsonAsync("/api/proyectos/3", EdicionValida);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal(CodigosError.ProyectoArchivado, problema.Codigo);
        Assert.Equal("El proyecto PRJ-0003 está archivado y no admite modificaciones.", problema.Detail);
    }

    // Spec 0.3, CA-17: 400 ValidationProblemDetails con errors en camelCase y codigo VALIDACION
    [Fact]
    public async Task Crear_ValidacionFalla_Devuelve400ConErroresEnCamelCase()
    {
        // Arrange
        var falla = new ValidationFailure("FechaObjetivo",
            "La fecha objetivo debe ser mayor o igual a la fecha de inicio.");
        _api.Proyectos.CrearAsync(Arg.Any<CrearProyectoRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException([falla]));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/proyectos", AltaValida);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal(CodigosError.Validacion, problema.Codigo);
        Assert.Equal("Error de validación", problema.Title);
        Assert.Equal(["La fecha objetivo debe ser mayor o igual a la fecha de inicio."],
            problema.Errors["fechaObjetivo"]);
    }

    // Spec 0.2/0.3, CA-18: fecha con formato inválido → 400 VALIDACION (binding)
    [Fact]
    public async Task Crear_FechaConFormatoInvalido_Devuelve400Validacion()
    {
        // Arrange
        var cuerpo = new StringContent(
            """{"nombre":"P","descripcion":"D","responsableId":2,"fechaInicio":"no-fecha","fechaObjetivo":"2026-12-15"}""",
            Encoding.UTF8, "application/json");

        // Act
        var respuesta = await _api.Cliente().PostAsync("/api/proyectos", cuerpo);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal(CodigosError.Validacion, problema.Codigo);
        await _api.Proyectos.DidNotReceive().CrearAsync(Arg.Any<CrearProyectoRequest>(), Arg.Any<CancellationToken>());
    }

    // RN-17, CA-05: 400 RESPONSABLE_INVALIDO
    [Fact]
    public async Task Crear_ResponsableInvalido_Devuelve400ConCodigo()
    {
        // Arrange
        _api.Proyectos.CrearAsync(Arg.Any<CrearProyectoRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidacionNegocioException(CodigosError.ResponsableInvalido, "Responsable inválido."));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/proyectos", AltaValida);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(CodigosError.ResponsableInvalido, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-19, CA-20: asignar "Archivado" por PUT → 400 ESTADO_INVALIDO
    [Fact]
    public async Task Editar_EstadoInvalido_Devuelve400ConCodigo()
    {
        // Arrange
        _api.Proyectos.EditarAsync(3, Arg.Any<EditarProyectoRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidacionNegocioException(CodigosError.EstadoInvalido, "Estado inválido."));

        // Act
        var respuesta = await _api.Cliente().PutAsJsonAsync("/api/proyectos/3", EdicionValida);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(CodigosError.EstadoInvalido, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // CA-19: PUT válido → 200 con el proyecto
    [Fact]
    public async Task Editar_Valido_Devuelve200ConDto()
    {
        // Arrange
        _api.Proyectos.EditarAsync(3, Arg.Any<EditarProyectoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Proyecto(3));

        // Act
        var respuesta = await _api.Cliente().PutAsJsonAsync("/api/proyectos/3", EdicionValida);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(3, (await respuesta.Content.ReadFromJsonAsync<ProyectoDto>(Dtos.Json))?.Id);
    }

    // Spec 0.3: 500 sin detalles internos y con codigo ERROR_INTERNO
    [Fact]
    public async Task Obtener_ExcepcionNoControlada_Devuelve500SinDetalleInterno()
    {
        // Arrange
        _api.Proyectos.ObtenerAsync(3, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("detalle interno secreto"));

        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/proyectos/3");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("detalle interno secreto", cuerpo);
        Assert.Equal(CodigosError.ErrorInterno, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-20, CA-22: archivar sin usuario → 401 USUARIO_NO_IDENTIFICADO y no llega al servicio
    [Fact]
    public async Task Archivar_SinUsuario_Devuelve401UsuarioNoIdentificado()
    {
        // Act
        var respuesta = await _api.Cliente().PostAsync("/api/proyectos/3/archivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(CodigosError.UsuarioNoIdentificado, (await Problema.LeerAsync(respuesta)).Codigo);
        await _api.Proyectos.DidNotReceive().ArchivarAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // RN-20, CA-22: archivar con usuario no admin → 403 PERMISO_DENEGADO
    [Fact]
    public async Task Archivar_UsuarioNoAdmin_Devuelve403PermisoDenegado()
    {
        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdNoAdmin.ToString())
            .PostAsync("/api/proyectos/3/archivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(CodigosError.PermisoDenegado, (await Problema.LeerAsync(respuesta)).Codigo);
        await _api.Proyectos.DidNotReceive().ArchivarAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // RN-20, RN-21, CA-21: archivar con admin → 200 con el DTO archivado
    [Fact]
    public async Task Archivar_UsuarioAdmin_Devuelve200ConDto()
    {
        // Arrange
        _api.Proyectos.ArchivarAsync(3, Arg.Any<CancellationToken>()).Returns(Dtos.Proyecto(3, archivado: true));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .PostAsync("/api/proyectos/3/archivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True((await respuesta.Content.ReadFromJsonAsync<ProyectoDto>(Dtos.Json))?.Archivado);
    }

    // RN-22, CA-23: re-archivar → 409 PROYECTO_ARCHIVADO
    [Fact]
    public async Task Archivar_ProyectoYaArchivado_Devuelve409()
    {
        // Arrange
        _api.Proyectos.ArchivarAsync(3, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProyectoArchivadoException("PRJ-0003"));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .PostAsync("/api/proyectos/3/archivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.ProyectoArchivado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // Contrato archivar: 404 si el proyecto no existe
    [Fact]
    public async Task Archivar_ProyectoInexistente_Devuelve404()
    {
        // Arrange
        _api.Proyectos.ArchivarAsync(99, Arg.Any<CancellationToken>()).ThrowsAsync(
            new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado, "No existe."));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .PostAsync("/api/proyectos/99/archivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    // CA-35, spec 0.4: header de persona inexistente, inactiva o no numérico → 401 USUARIO_INVALIDO
    [Theory]
    [InlineData("99")]
    [InlineData("5")]
    [InlineData("abc")]
    [InlineData("-1")]
    public async Task Archivar_HeaderUsuarioInvalido_Devuelve401UsuarioInvalido(string header)
    {
        // Act
        var respuesta = await _api.Cliente(header).PostAsync("/api/proyectos/3/archivar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(CodigosError.UsuarioInvalido, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // Contrato GET /api/proyectos: la query se pasa al servicio tal cual llega
    [Fact]
    public async Task Listar_ConQuery_PasaLosParametrosAlServicio()
    {
        // Arrange
        _api.Proyectos.ListarAsync(Arg.Any<ListarProyectosQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Application.Comun.Paginado<ProyectoResumenDto>([], 1, 20, 0));

        // Act
        var respuesta = await _api.Cliente().GetAsync(
            "/api/proyectos?texto=prj-0003&estadoId=3&archivados=solo&pagina=2&tamanioPagina=5&ordenarPor=nombre&direccion=asc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var esperado = new ListarProyectosQuery("prj-0003", 3, null, "solo", 2, 5, "nombre", "asc");
        await _api.Proyectos.Received(1).ListarAsync(esperado, Arg.Any<CancellationToken>());
    }

    // CA-27: parámetro con tipo inválido → 400 VALIDACION (binding)
    [Fact]
    public async Task Listar_TamanioPaginaNoNumerico_Devuelve400Validacion()
    {
        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/proyectos?tamanioPagina=muchos");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(CodigosError.Validacion, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // CA-27: el servicio rechaza tamanioPagina=500 → 400 VALIDACION con error en el campo
    [Fact]
    public async Task Listar_ServicioRechazaParametros_Devuelve400Validacion()
    {
        // Arrange
        _api.Proyectos.ListarAsync(Arg.Any<ListarProyectosQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException([new ValidationFailure("TamanioPagina", "Fuera de rango.")]));

        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/proyectos?tamanioPagina=500");

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal(400, problema.Status);
        Assert.True(problema.Errors.ContainsKey("tamanioPagina"));
    }
}
