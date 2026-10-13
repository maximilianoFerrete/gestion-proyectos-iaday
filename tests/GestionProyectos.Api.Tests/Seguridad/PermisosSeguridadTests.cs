using System.Reflection;
using System.Security.Claims;
using GestionProyectos.Api.Controllers;
using GestionProyectos.Api.Errores;
using GestionProyectos.Api.Seguridad;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.UsuarioActual;
using GestionProyectos.Domain.Excepciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NSubstitute;

namespace GestionProyectos.Api.Tests.Seguridad;

/// <summary>
/// Tests unitarios de permisos (claims, políticas, mensaje 403) y extensiones de ProblemDetails (RF-002 0.1, 0.2).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// Modificado: 2026-10-11 - Tres permisos con Proyectos.ConfigurarTablero (tests RF-005)
/// </remarks>
public class PermisosSeguridadTests
{
    private static readonly IdentidadUsuario Admin = new(1, "Administrador", "admin@gestionproyectos.local", true);

    private static UsuarioActualDesdeClaims UsuarioDesde(ClaimsPrincipal principal)
    {
        var accesor = Substitute.For<IHttpContextAccessor>();
        accesor.HttpContext.Returns(new DefaultHttpContext { User = principal });
        return new UsuarioActualDesdeClaims(accesor);
    }

    private static ClaimsPrincipal PrincipalCon(params string[] permisos) =>
        new(ProvisionalAuthenticationHandler.CrearIdentidad(Admin, EsquemaProvisional.Nombre, permisos));

    /// <summary>Acciones POST/PUT de los controllers de configuración de incidencias.</summary>
    public static TheoryData<string, string> EscriturasDeConfiguracion()
    {
        Type[] controllers =
        [
            typeof(NivelesIncidenciaController), typeof(TiposIncidenciaController),
            typeof(RelacionesTiposIncidenciaController), typeof(EstadosIncidenciaController),
            typeof(FlujosIncidenciaController), typeof(PrioridadesIncidenciaController),
        ];
        var datos = new TheoryData<string, string>();
        foreach (var metodo in controllers.SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance
                     | BindingFlags.DeclaredOnly))
                     .Where(m => m.GetCustomAttributes<HttpMethodAttribute>()
                         .Any(a => a is HttpPostAttribute or HttpPutAttribute)))
        {
            datos.Add(metodo.DeclaringType!.Name, metodo.Name);
        }

        return datos;
    }

    // RN-01: cada POST/PUT de configuración exige la política Configuracion.Incidencias
    [Theory]
    [MemberData(nameof(EscriturasDeConfiguracion))]
    public void EscrituraDeConfiguracion_Endpoint_TieneAuthorizeConfiguracionIncidencias(string controller,
        string accion)
    {
        // Arrange
        var metodo = typeof(IncidenciasController).Assembly.GetTypes().Single(t => t.Name == controller)
            .GetMethod(accion);

        // Act
        var atributo = metodo?.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.Equal("Configuracion.Incidencias", atributo?.Policy);
    }

    // Sanidad del MemberData: se encontraron las 18 escrituras de configuración del contrato
    [Fact]
    public void EscriturasDeConfiguracion_Reflexion_Encuentra18Acciones()
    {
        // Act
        var cantidad = EscriturasDeConfiguracion().Count();

        // Assert
        Assert.Equal(18, cantidad);
    }

    // RN-02: cambiar-tipo exige la política Incidencias.CambiarTipo
    [Fact]
    public void CambiarTipo_Endpoint_TieneAuthorizeIncidenciasCambiarTipo()
    {
        // Arrange
        var metodo = typeof(IncidenciasController).GetMethod(nameof(IncidenciasController.CambiarTipo));

        // Act
        var atributo = metodo?.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.Equal("Incidencias.CambiarTipo", atributo?.Policy);
    }

    // DA-16: crear y editar incidencias no exigen usuario
    [Theory]
    [InlineData(nameof(IncidenciasController.Crear))]
    [InlineData(nameof(IncidenciasController.Editar))]
    [InlineData(nameof(IncidenciasController.Listar))]
    public void CrearEditarListar_Endpoint_SinAuthorize(string accion)
    {
        // Arrange
        var metodo = typeof(IncidenciasController).GetMethod(accion);

        // Act
        var atributo = metodo?.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.Null(atributo);
    }

    // RN-34: el controller de incidencias no tiene DELETE
    [Fact]
    public void IncidenciasController_Acciones_SinHttpDelete()
    {
        // Act
        var deletes = typeof(IncidenciasController).GetMethods()
            .Where(m => m.GetCustomAttribute<HttpDeleteAttribute>() is not null);

        // Assert
        Assert.Empty(deletes);
    }

    // Spec 0.1: el handler provisional con admin agrega un claim "permiso" por permiso
    [Fact]
    public void CrearIdentidad_AdminConPermisos_IncluyeClaimsPermiso()
    {
        // Act
        var principal = PrincipalCon([.. Permisos.Todos]);

        // Assert
        Assert.Equal(["Configuracion.Incidencias", "Incidencias.CambiarTipo", "Proyectos.ConfigurarTablero"],
            principal.FindAll(EsquemaProvisional.ClaimPermiso).Select(c => c.Value).Order());
    }

    // Spec 0.1: permisos repetidos generan un único claim
    [Fact]
    public void CrearIdentidad_PermisosRepetidos_UnClaimPorPermiso()
    {
        // Act
        var principal = PrincipalCon(Permisos.ConfiguracionIncidencias, Permisos.ConfiguracionIncidencias);

        // Assert
        Assert.Single(principal.FindAll(EsquemaProvisional.ClaimPermiso));
    }

    // Spec 0.1: TienePermiso es true solo para los claims presentes
    [Theory]
    [InlineData("Configuracion.Incidencias", true)]
    [InlineData("Incidencias.CambiarTipo", false)]
    [InlineData("", false)]
    public void UsuarioActualDesdeClaims_TienePermiso_SoloParaClaimsPresentes(string permiso, bool esperado)
    {
        // Arrange
        var sut = UsuarioDesde(PrincipalCon(Permisos.ConfiguracionIncidencias));

        // Act
        var resultado = sut.TienePermiso(permiso);

        // Assert
        Assert.Equal(esperado, resultado);
    }

    // Spec 0.1: Permisos lee los claims "permiso"
    [Fact]
    public void UsuarioActualDesdeClaims_Permisos_DevuelveLosClaims()
    {
        // Arrange
        var sut = UsuarioDesde(PrincipalCon([.. Permisos.Todos]));

        // Act
        var permisos = sut.Permisos;

        // Assert
        Assert.Equal(["Configuracion.Incidencias", "Incidencias.CambiarTipo", "Proyectos.ConfigurarTablero"],
            permisos.Order());
    }

    // Spec 0.1: anónimo → sin permisos
    [Fact]
    public void UsuarioActualDesdeClaims_Anonimo_SinPermisos()
    {
        // Arrange
        var sut = UsuarioDesde(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        var tiene = sut.TienePermiso(Permisos.ConfiguracionIncidencias);

        // Assert
        Assert.False(tiene);
        Assert.Empty(sut.Permisos);
    }

    // Spec 0.1: claims "permiso" de una identidad no autenticada se ignoran
    [Fact]
    public void UsuarioActualDesdeClaims_ClaimsPermisoSinAutenticar_SinPermisos()
    {
        // Arrange
        var identidad = new ClaimsIdentity([new Claim(EsquemaProvisional.ClaimSub, "1"),
            new Claim(EsquemaProvisional.ClaimPermiso, Permisos.ConfiguracionIncidencias)]);
        var sut = UsuarioDesde(new ClaimsPrincipal(identidad));

        // Act
        var tiene = sut.TienePermiso(Permisos.ConfiguracionIncidencias);

        // Assert
        Assert.False(tiene);
    }

    // Spec 0.1: el 403 de una política por permiso menciona el permiso
    [Fact]
    public void MensajePermisoDenegado_PoliticaDePermiso_MencionaElPermiso()
    {
        // Arrange
        var politica = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
            .RequireClaim(EsquemaProvisional.ClaimPermiso, Permisos.IncidenciasCambiarTipo).Build();

        // Act
        var mensaje = ManejadorResultadoAutorizacion.MensajePermisoDenegado(politica);

        // Assert
        Assert.Equal("La operación requiere el permiso 'Incidencias.CambiarTipo'.", mensaje);
    }

    // Spec 0.1: la política SoloAdministradores conserva el mensaje de RF-001
    [Fact]
    public void MensajePermisoDenegado_PoliticaSoloAdministradores_MantieneMensajeDeAdministrador()
    {
        // Arrange
        var politica = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
            .RequireRole(EsquemaProvisional.RolAdmin).Build();

        // Act
        var mensaje = ManejadorResultadoAutorizacion.MensajePermisoDenegado(politica);

        // Assert
        Assert.Equal("La operación requiere un usuario administrador.", mensaje);
    }

    // Spec 0.2: FabricaProblemas copia las extensiones de la excepción
    [Fact]
    public void Crear_ExcepcionConExtensionIncidencias_CopiaLaExtension()
    {
        // Arrange
        var excepcion = new ConflictoException(CodigosError.VencimientoAnteriorAHijas, "Anterior.",
            new Dictionary<string, object?> { ["incidencias"] = new[] { "PRJ-0001-16" } });

        // Act
        var problema = new FabricaProblemas().Crear(new DefaultHttpContext(), 409, excepcion);

        // Assert
        Assert.Equal(["PRJ-0001-16"], (string[])problema.Extensions["incidencias"]!);
        Assert.Equal(CodigosError.VencimientoAnteriorAHijas, problema.Extensions["codigo"]);
    }

    public static TheoryData<ExcepcionDominio, int> ExcepcionesRf002 => new()
    {
        { new EntidadNoEncontradaException(CodigosError.IncidenciaNoEncontrada, "x"), 404 },
        { new ValidacionNegocioException(CodigosError.RelacionNoPermitida, "x"), 400 },
        { new ConflictoException(CodigosError.RelacionCircular, "x"), 409 },
        { new PermisoDenegadoException(Permisos.ConfiguracionIncidencias), 403 },
    };

    // Catálogo de errores RF-002: cada excepción se mapea a su código HTTP
    [Theory]
    [MemberData(nameof(ExcepcionesRf002))]
    public void StatusPara_ExcepcionRf002_DevuelveStatusDelCatalogo(ExcepcionDominio excepcion, int status)
    {
        // Act
        var resultado = ManejadorExcepciones.StatusPara(excepcion);

        // Assert
        Assert.Equal(status, resultado);
    }
}
