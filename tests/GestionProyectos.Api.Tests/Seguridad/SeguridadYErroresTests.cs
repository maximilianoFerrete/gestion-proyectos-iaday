using System.Reflection;
using System.Security.Claims;
using GestionProyectos.Api.Controllers;
using GestionProyectos.Api.Errores;
using GestionProyectos.Api.Seguridad;
using GestionProyectos.Application.UsuarioActual;
using GestionProyectos.Domain.Excepciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace GestionProyectos.Api.Tests.Seguridad;

/// <summary>
/// Tests unitarios del mecanismo provisional de usuario actual y del mapeo de errores.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class SeguridadYErroresTests
{
    private static UsuarioActualDesdeClaims UsuarioDesde(ClaimsPrincipal principal)
    {
        var accesor = Substitute.For<IHttpContextAccessor>();
        accesor.HttpContext.Returns(new DefaultHttpContext { User = principal });
        return new UsuarioActualDesdeClaims(accesor);
    }

    private static ClaimsPrincipal Principal(bool esAdmin) =>
        new(ProvisionalAuthenticationHandler.CrearIdentidad(
            new IdentidadUsuario(1, "Administrador", "admin@gestionproyectos.local", esAdmin),
            EsquemaProvisional.Nombre));

    public static TheoryData<ExcepcionDominio, int> ExcepcionesYStatus => new()
    {
        { new EntidadNoEncontradaException(CodigosError.ProyectoNoEncontrado, "x"), 404 },
        { new ValidacionNegocioException(CodigosError.ResponsableInvalido, "x"), 400 },
        { new ConflictoException(CodigosError.AgrupacionNombreDuplicado, "x"), 409 },
        { new ProyectoArchivadoException("PRJ-0001"), 409 },
        { new UsuarioNoIdentificadoException(), 401 },
        { new UsuarioInvalidoException(), 401 },
        { new PermisoDenegadoException(), 403 },
    };

    // Spec 0.4: persona admin → claims sub, name y rol Admin
    [Fact]
    public void CrearIdentidad_PersonaAdmin_IncluyeSubNameYRolAdmin()
    {
        // Act
        var principal = Principal(esAdmin: true);

        // Assert
        Assert.Equal("1", principal.FindFirst(EsquemaProvisional.ClaimSub)?.Value);
        Assert.Equal("Administrador", principal.Identity?.Name);
        Assert.True(principal.IsInRole(EsquemaProvisional.RolAdmin));
    }

    // Spec 0.4: persona no admin → sin rol Admin
    [Fact]
    public void CrearIdentidad_PersonaNoAdmin_SinRolAdmin()
    {
        // Act
        var principal = Principal(esAdmin: false);

        // Assert
        Assert.False(principal.IsInRole(EsquemaProvisional.RolAdmin));
        Assert.True(principal.Identity?.IsAuthenticated);
    }

    // Spec 0.4: principal anónimo → no identificado y no admin
    [Fact]
    public void UsuarioActualDesdeClaims_PrincipalAnonimo_NoIdentificadoNiAdmin()
    {
        // Arrange
        var sut = UsuarioDesde(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        var identificado = sut.EstaIdentificado;

        // Assert
        Assert.False(identificado);
        Assert.False(sut.EsAdmin);
        Assert.Null(sut.PersonaId);
    }

    // Spec 0.4: principal con rol Admin → EsAdmin y PersonaId correctos
    [Fact]
    public void UsuarioActualDesdeClaims_PrincipalAdmin_EsAdminConPersonaId()
    {
        // Arrange
        var sut = UsuarioDesde(Principal(esAdmin: true));

        // Act
        var esAdmin = sut.EsAdmin;

        // Assert
        Assert.True(esAdmin);
        Assert.Equal(1, sut.PersonaId);
        Assert.Equal("Administrador", sut.Nombre);
    }

    // Spec 0.4: claims de una identidad no autenticada se ignoran
    [Fact]
    public void UsuarioActualDesdeClaims_ClaimsSinAutenticar_NoIdentificado()
    {
        // Arrange
        var identidad = new ClaimsIdentity([new Claim(EsquemaProvisional.ClaimSub, "1"),
            new Claim(EsquemaProvisional.ClaimRol, EsquemaProvisional.RolAdmin)]);
        var sut = UsuarioDesde(new ClaimsPrincipal(identidad));

        // Act
        var identificado = sut.EstaIdentificado;

        // Assert
        Assert.False(identificado);
        Assert.False(sut.EsAdmin);
    }

    // RN-20: el endpoint archivar exige la política SoloAdministradores
    [Fact]
    public void Archivar_Endpoint_TieneAuthorizePoliticaSoloAdministradores()
    {
        // Arrange
        var metodo = typeof(ProyectosController).GetMethod(nameof(ProyectosController.Archivar));

        // Act
        var atributo = metodo?.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.Equal(EsquemaProvisional.PoliticaSoloAdministradores, atributo?.Policy);
    }

    // Spec 0.3: cada excepción de negocio se mapea a su código HTTP
    [Theory]
    [MemberData(nameof(ExcepcionesYStatus))]
    public void StatusPara_ExcepcionDeNegocio_DevuelveStatusDelCatalogo(ExcepcionDominio excepcion, int status)
    {
        // Act
        var resultado = ManejadorExcepciones.StatusPara(excepcion);

        // Assert
        Assert.Equal(status, resultado);
    }

    // Spec 0.3: claves de errors en camelCase y sin prefijo JSON "$."
    [Theory]
    [InlineData("FechaObjetivo", "fechaObjetivo")]
    [InlineData("$.fechaInicio", "fechaInicio")]
    [InlineData("Nombre", "nombre")]
    [InlineData("$", "")]
    public void NormalizarClave_Clave_DevuelveCamelCaseSinPrefijo(string clave, string esperado)
    {
        // Act
        var resultado = FabricaProblemas.NormalizarClave(clave);

        // Assert
        Assert.Equal(esperado, resultado);
    }
}
