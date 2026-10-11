using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using FluentValidation.Results;
using GestionProyectos.Api.Tests.Infraestructura;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionProyectos.Api.Tests.Controllers;

/// <summary>
/// Tests HTTP de /api/proyectos/{proyectoId}/incidencias con los servicios sustituidos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public sealed class IncidenciasEndpointsTests : IDisposable
{
    private const string Base = "/api/proyectos/1/incidencias";
    private readonly ApiFactory _api = new();

    private static readonly object AltaBody = new
    {
        tipoId = 3, titulo = "Validar tarjeta", descripcion = "Validar número y vencimiento.", prioridadId = (int?)null,
        responsableId = 2, fechaVencimiento = "2026-11-20", agrupacionId = (int?)null, padreId = 15,
    };

    private static readonly object EdicionBody = new
    {
        titulo = "Pagar", descripcion = "Como cliente.", estadoId = 3, prioridadId = 3, responsableId = (int?)null,
        fechaVencimiento = "2026-11-10", agrupacionId = (int?)null, padreId = 3,
    };

    public void Dispose() => _api.Dispose();

    // RN-18, CA-13: POST válido → 201 con Location /api/proyectos/{p}/incidencias/{id}
    [Fact]
    public async Task Crear_Valido_Devuelve201ConLocation()
    {
        // Arrange
        _api.Incidencias.CrearAsync(1, Arg.Any<CrearIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Incidencia(16, 1));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync(Base, AltaBody);

        // Assert
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/proyectos/1/incidencias/16", respuesta.Headers.Location?.OriginalString);
    }

    // RN-20, DA-16: el alta no exige usuario y pasa el request (sin estado) al servicio
    [Fact]
    public async Task Crear_SinUsuario_PasaElRequestAlServicio()
    {
        // Arrange
        _api.Incidencias.CrearAsync(1, Arg.Any<CrearIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Incidencia(16, 1));

        // Act
        await _api.Cliente().PostAsJsonAsync(Base, AltaBody);

        // Assert
        await _api.Incidencias.Received(1).CrearAsync(1, new CrearIncidenciaRequest(3, "Validar tarjeta",
            "Validar número y vencimiento.", null, 2, new DateOnly(2026, 11, 20), null, 15), Arg.Any<CancellationToken>());
    }

    // RN-25, CA-24: 400 RELACION_NO_PERMITIDA
    [Fact]
    public async Task Crear_RelacionNoPermitida_Devuelve400ConCodigo()
    {
        // Arrange
        _api.Incidencias.CrearAsync(1, Arg.Any<CrearIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidacionNegocioException(CodigosError.RelacionNoPermitida, "No permitida."));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync(Base, AltaBody);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(CodigosError.RelacionNoPermitida, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-36, CA-21: crear en proyecto archivado → 409 PROYECTO_ARCHIVADO
    [Fact]
    public async Task Crear_ProyectoArchivado_Devuelve409ProyectoArchivado()
    {
        // Arrange
        _api.Incidencias.CrearAsync(1, Arg.Any<CrearIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProyectoArchivadoException("PRJ-0001"));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync(Base, AltaBody);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.ProyectoArchivado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // CA-16: fecha inválida en el body → 400 VALIDACION
    [Fact]
    public async Task Crear_FechaInvalida_Devuelve400Validacion()
    {
        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync(Base, new { tipoId = 3, titulo = "T", descripcion = "D",
            fechaVencimiento = "30/11/2026" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(CodigosError.Validacion, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // Spec 0.2, RN-32, CA-32: 409 con extensión "incidencias"
    [Fact]
    public async Task Editar_VencimientoAnteriorAHijas_Devuelve409ConExtensionIncidencias()
    {
        // Arrange
        _api.Incidencias.EditarAsync(1, 15, Arg.Any<EditarIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.VencimientoAnteriorAHijas, "Anterior a hijas.",
                new Dictionary<string, object?> { ["incidencias"] = new[] { "PRJ-0001-16" } }));

        // Act
        var respuesta = await _api.Cliente().PutAsJsonAsync($"{Base}/15", EdicionBody);

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((409, CodigosError.VencimientoAnteriorAHijas), (problema.Status, problema.Codigo));
        Assert.Equal(new[] { "PRJ-0001-16" }, problema.Incidencias);
    }

    // CA-17: PUT válido → 200 con el detalle
    [Fact]
    public async Task Editar_Valido_Devuelve200ConDto()
    {
        // Arrange
        _api.Incidencias.EditarAsync(1, 15, Arg.Any<EditarIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Incidencia(15, 1));

        // Act
        var dto = await (await _api.Cliente().PutAsJsonAsync($"{Base}/15", EdicionBody))
            .Content.ReadFromJsonAsync<IncidenciaDto>(Dtos.Json);

        // Assert
        Assert.Equal(15, dto!.Id);
    }

    // RN-26, CA-26: 409 RELACION_CIRCULAR
    [Fact]
    public async Task Editar_RelacionCircular_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Incidencias.EditarAsync(1, 15, Arg.Any<EditarIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.RelacionCircular, "Circular."));

        // Act
        var respuesta = await _api.Cliente().PutAsJsonAsync($"{Base}/15", EdicionBody);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.RelacionCircular, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-34, CA-22: no existe DELETE de incidencias → 405
    [Fact]
    public async Task Eliminar_Incidencia_Devuelve405()
    {
        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString()).DeleteAsync($"{Base}/15");

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);
    }

    // RN-37, CA-20: incidencia de otro proyecto → 404 INCIDENCIA_NO_ENCONTRADA
    [Fact]
    public async Task Obtener_IncidenciaDeOtroProyecto_Devuelve404ConCodigo()
    {
        // Arrange
        _api.ConsultasIncidencias.ObtenerAsync(1, 40, Arg.Any<CancellationToken>()).ThrowsAsync(
            new EntidadNoEncontradaException(CodigosError.IncidenciaNoEncontrada, "No existe."));

        // Act
        var respuesta = await _api.Cliente().GetAsync($"{Base}/40");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(CodigosError.IncidenciaNoEncontrada, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // CA-34: la ruta "arbol" no choca con {id:int} → 200
    [Fact]
    public async Task Arbol_Proyecto_Devuelve200()
    {
        // Arrange
        _api.ConsultasIncidencias.ObtenerArbolAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var respuesta = await _api.Cliente().GetAsync($"{Base}/arbol");

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await _api.ConsultasIncidencias.Received(1).ObtenerArbolAsync(1, Arg.Any<CancellationToken>());
    }

    // CA-19: tipoId repetible y sinResponsable llegan al servicio
    [Fact]
    public async Task Listar_ConQuery_PasaLosParametrosAlServicio()
    {
        // Arrange
        ListarIncidenciasQuery? recibida = null;
        _api.ConsultasIncidencias.ListarAsync(1, Arg.Do<ListarIncidenciasQuery>(q => recibida = q),
            Arg.Any<CancellationToken>()).Returns(new Application.Comun.Paginado<IncidenciaResumenDto>([], 1, 20, 0));

        // Act
        await _api.Cliente().GetAsync($"{Base}?tipoId=1&tipoId=2&sinResponsable=true&sinAgrupacion=true");

        // Assert
        Assert.Equal([1, 2], recibida!.TipoId!);
        Assert.Equal((true, true), (recibida.SinResponsable, recibida.SinAgrupacion));
    }

    // CA-19: el servicio rechaza responsableId + sinResponsable → 400 VALIDACION
    [Fact]
    public async Task Listar_ServicioRechazaParametros_Devuelve400Validacion()
    {
        // Arrange
        _api.ConsultasIncidencias.ListarAsync(1, Arg.Any<ListarIncidenciasQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException([new ValidationFailure("ResponsableId", "Excluyentes.")]));

        // Act
        var respuesta = await _api.Cliente().GetAsync($"{Base}?responsableId=2&sinResponsable=true");

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((400, CodigosError.Validacion), (problema.Status, problema.Codigo));
        Assert.True(problema.Errors.ContainsKey("responsableId"));
    }

    // RN-02, CA-36: cambiar tipo sin usuario → 401 USUARIO_NO_IDENTIFICADO
    [Fact]
    public async Task CambiarTipo_SinUsuario_Devuelve401UsuarioNoIdentificado()
    {
        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync($"{Base}/16/cambiar-tipo", new { tipoId = 5 });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(CodigosError.UsuarioNoIdentificado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-02, RN-03, CA-36: no admin → 403 PERMISO_DENEGADO que menciona el permiso
    [Fact]
    public async Task CambiarTipo_UsuarioNoAdmin_Devuelve403ConElPermisoEnElDetalle()
    {
        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdNoAdmin.ToString())
            .PostAsJsonAsync($"{Base}/16/cambiar-tipo", new { tipoId = 5 });

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal((403, CodigosError.PermisoDenegado), (problema.Status, problema.Codigo));
        Assert.Equal("La operación requiere el permiso 'Incidencias.CambiarTipo'.", problema.Detail);
        await _api.Incidencias.DidNotReceive().CambiarTipoAsync(Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CambiarTipoIncidenciaRequest>(), Arg.Any<CancellationToken>());
    }

    // RN-03, CA-37: admin → 200 y el servicio recibe el request
    [Fact]
    public async Task CambiarTipo_UsuarioAdmin_Devuelve200()
    {
        // Arrange
        _api.Incidencias.CambiarTipoAsync(1, 16, new CambiarTipoIncidenciaRequest(5, null), Arg.Any<CancellationToken>())
            .Returns(Dtos.Incidencia(16, 1));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .PostAsJsonAsync($"{Base}/16/cambiar-tipo", new { tipoId = 5, estadoId = (int?)null });

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // RN-25, CA-38: 409 TIPO_INCOMPATIBLE_CON_HIJAS con extensión "incidencias"
    [Fact]
    public async Task CambiarTipo_HijasIncompatibles_Devuelve409ConExtensionIncidencias()
    {
        // Arrange
        _api.Incidencias.CambiarTipoAsync(1, 15, Arg.Any<CambiarTipoIncidenciaRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.TipoIncompatibleConHijas, "Incompatible.",
                new Dictionary<string, object?> { ["incidencias"] = new[] { "PRJ-0001-17" } }));

        // Act
        var respuesta = await _api.Cliente(ApiFactory.IdAdmin.ToString())
            .PostAsJsonAsync($"{Base}/15/cambiar-tipo", new { tipoId = 10 });

        // Assert
        var problema = await Problema.LeerAsync(respuesta);
        Assert.Equal(CodigosError.TipoIncompatibleConHijas, problema.Codigo);
        Assert.Equal(new[] { "PRJ-0001-17" }, problema.Incidencias);
    }
}
