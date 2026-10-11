using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.UsuarioActual;
using GestionProyectos.Domain.Excepciones;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.UsuarioActual;

/// <summary>
/// Tests del resolutor provisional de permisos y de AsegurarPermiso (RF-002 0.1, RN-01 a RN-03).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class PermisosTests
{
    private readonly ResolutorPermisosPorAdministrador _resolutor = new();

    // RN-03, CA-03: un administrador tiene todos los permisos
    [Fact]
    public async Task ObtenerPermisosAsync_Admin_DevuelveAmbosPermisos()
    {
        // Act
        var permisos = await _resolutor.ObtenerPermisosAsync(
            new IdentidadUsuario(1, "Administrador", "admin@gestionproyectos.local", true), CancellationToken.None);

        // Assert
        Assert.Equal([Permisos.ConfiguracionIncidencias, Permisos.IncidenciasCambiarTipo], permisos.Order());
    }

    // RN-03, CA-03: una persona no admin no tiene permisos
    [Fact]
    public async Task ObtenerPermisosAsync_NoAdmin_DevuelveVacio()
    {
        // Act
        var permisos = await _resolutor.ObtenerPermisosAsync(
            new IdentidadUsuario(2, "Ana Gómez", "ana@empresa.com", false), CancellationToken.None);

        // Assert
        Assert.Empty(permisos);
    }

    // Spec 0.1: Permisos.Todos contiene exactamente los dos permisos del RF
    [Fact]
    public void Todos_Constantes_ContieneLosDosPermisos()
    {
        // Act
        var todos = Permisos.Todos;

        // Assert
        Assert.Equal(["Configuracion.Incidencias", "Incidencias.CambiarTipo"], todos);
    }

    // Spec 0.1: AsegurarPermiso con usuario anónimo → 401
    [Fact]
    public void AsegurarPermiso_UsuarioAnonimo_LanzaUsuarioNoIdentificado()
    {
        // Arrange
        var usuario = UsuarioAnonimo();

        // Act
        Action accion = () => usuario.AsegurarPermiso(Permisos.ConfiguracionIncidencias);

        // Assert
        Assert.Throws<UsuarioNoIdentificadoException>(accion);
    }

    // Spec 0.1, RN-01: AsegurarPermiso sin el permiso → 403 que menciona el permiso
    [Fact]
    public void AsegurarPermiso_SinElPermiso_LanzaPermisoDenegadoConElPermiso()
    {
        // Arrange
        var usuario = Usuario(Permisos.IncidenciasCambiarTipo);

        // Act
        var ex = Assert.Throws<PermisoDenegadoException>(() => usuario.AsegurarPermiso(Permisos.ConfiguracionIncidencias));

        // Assert
        Assert.Equal(CodigosError.PermisoDenegado, ex.Codigo);
        Assert.Equal("La operación requiere el permiso 'Configuracion.Incidencias'.", ex.Message);
    }

    // Spec 0.1: AsegurarPermiso con el permiso → devuelve el PersonaId
    [Fact]
    public void AsegurarPermiso_ConElPermiso_DevuelvePersonaId()
    {
        // Arrange
        var usuario = Usuario(Permisos.ConfiguracionIncidencias);

        // Act
        var personaId = usuario.AsegurarPermiso(Permisos.ConfiguracionIncidencias);

        // Assert
        Assert.Equal(1, personaId);
    }
}
