using System.Net;
using System.Net.Http.Json;
using GestionProyectos.Api.Tests.Infraestructura;
using GestionProyectos.Application.Agrupaciones;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionProyectos.Api.Tests.Controllers;

/// <summary>
/// Tests HTTP de /api/proyectos/{proyectoId}/agrupaciones con el servicio sustituido.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public sealed class AgrupacionesEndpointsTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private static readonly object Body = new { nombre = "Módulo pagos", descripcion = "Incidencias de pagos." };

    public void Dispose() => _api.Dispose();

    // CA-29: 201 con Location /api/proyectos/3/agrupaciones/8
    [Fact]
    public async Task Crear_Valido_Devuelve201ConLocation()
    {
        // Arrange
        _api.Agrupaciones.CrearAsync(3, Arg.Any<AgrupacionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Dtos.Agrupacion(8, 3));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/proyectos/3/agrupaciones", Body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/proyectos/3/agrupaciones/8", respuesta.Headers.Location?.OriginalString);
    }

    // RN-27, CA-30: 409 AGRUPACION_NOMBRE_DUPLICADO
    [Fact]
    public async Task Crear_NombreDuplicado_Devuelve409ConCodigo()
    {
        // Arrange
        _api.Agrupaciones.CrearAsync(3, Arg.Any<AgrupacionRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictoException(CodigosError.AgrupacionNombreDuplicado, "Duplicado."));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/proyectos/3/agrupaciones", Body);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.AgrupacionNombreDuplicado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // RN-22, CA-23: crear en proyecto archivado → 409 PROYECTO_ARCHIVADO
    [Fact]
    public async Task Crear_ProyectoArchivado_Devuelve409ProyectoArchivado()
    {
        // Arrange
        _api.Agrupaciones.CrearAsync(3, Arg.Any<AgrupacionRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProyectoArchivadoException("PRJ-0003"));

        // Act
        var respuesta = await _api.Cliente().PostAsJsonAsync("/api/proyectos/3/agrupaciones", Body);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(CodigosError.ProyectoArchivado, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // CA-31: DELETE válido → 204 sin body
    [Fact]
    public async Task Eliminar_Valido_Devuelve204SinBody()
    {
        // Act
        var respuesta = await _api.Cliente().DeleteAsync("/api/proyectos/3/agrupaciones/8");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Empty(await respuesta.Content.ReadAsByteArrayAsync());
        await _api.Agrupaciones.Received(1).EliminarAsync(3, 8, Arg.Any<CancellationToken>());
    }

    // RN-29, CA-32: agrupación de otro proyecto → 404 AGRUPACION_NO_ENCONTRADA
    [Fact]
    public async Task Obtener_AgrupacionDeOtroProyecto_Devuelve404ConCodigo()
    {
        // Arrange
        _api.Agrupaciones.ObtenerAsync(4, 8, Arg.Any<CancellationToken>()).ThrowsAsync(
            new EntidadNoEncontradaException(CodigosError.AgrupacionNoEncontrada, "No existe."));

        // Act
        var respuesta = await _api.Cliente().GetAsync("/api/proyectos/4/agrupaciones/8");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(CodigosError.AgrupacionNoEncontrada, (await Problema.LeerAsync(respuesta)).Codigo);
    }

    // CA-33: listar agrupaciones de un proyecto (también archivado) → 200
    [Fact]
    public async Task Listar_Proyecto_Devuelve200ConLista()
    {
        // Arrange
        _api.Agrupaciones.ListarAsync(3, Arg.Any<CancellationToken>()).Returns([Dtos.Agrupacion(8, 3)]);

        // Act
        var lista = await _api.Cliente().GetFromJsonAsync<List<AgrupacionDto>>("/api/proyectos/3/agrupaciones", Dtos.Json);

        // Assert
        Assert.Single(lista!);
    }

    // RN-22, CA-23: editar en proyecto archivado → 409
    [Fact]
    public async Task Editar_ProyectoArchivado_Devuelve409()
    {
        // Arrange
        _api.Agrupaciones.EditarAsync(3, 8, Arg.Any<AgrupacionRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProyectoArchivadoException("PRJ-0003"));

        // Act
        var respuesta = await _api.Cliente().PutAsJsonAsync("/api/proyectos/3/agrupaciones/8", Body);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }
}
