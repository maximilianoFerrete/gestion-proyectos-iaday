using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de las reglas puras del estado de una incidencia (RN-11, RN-23, RN-24).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class ReglasEstadoIncidenciaTests
{
    // RN-23, CA-37: sin estadoId y el actual está en el nuevo flujo → se conserva
    [Fact]
    public void ResolverEstadoAlCambiarTipo_ActualEnNuevoFlujoSinEstadoId_ConservaElActual()
    {
        // Act
        var estado = ReglasEstadoIncidencia.ResolverEstadoAlCambiarTipo(EnProgreso, null, FlujoGeneralConEstados(), null);

        // Assert
        Assert.Equal(EnProgreso, estado);
    }

    // RN-23, CA-39: sin estadoId y el actual no está en el nuevo flujo → inicial del nuevo flujo
    [Fact]
    public void ResolverEstadoAlCambiarTipo_ActualFueraDelNuevoFlujoSinEstadoId_DevuelveInicialDelNuevoFlujo()
    {
        // Act
        var estado = ReglasEstadoIncidencia.ResolverEstadoAlCambiarTipo(EnProgreso, null, FlujoBugConEstados(), null);

        // Assert
        Assert.Equal(Nuevo, estado);
    }

    // RN-23, CA-39: estadoId del nuevo flujo → queda en ese estado
    [Fact]
    public void ResolverEstadoAlCambiarTipo_EstadoIdDelNuevoFlujo_DevuelveEseEstado()
    {
        // Act
        var estado = ReglasEstadoIncidencia.ResolverEstadoAlCambiarTipo(EnProgreso, Hecho, FlujoBugConEstados(),
            Estado(Hecho, "Hecho"));

        // Assert
        Assert.Equal(Hecho, estado);
    }

    // RN-23, CA-39: estadoId ajeno al nuevo flujo → 400 ESTADO_INCIDENCIA_INVALIDO
    [Fact]
    public void ResolverEstadoAlCambiarTipo_EstadoIdFueraDelNuevoFlujo_LanzaEstadoIncidenciaInvalido()
    {
        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() => ReglasEstadoIncidencia.ResolverEstadoAlCambiarTipo(
            PorHacer, EnProgreso, FlujoBugConEstados(), Estado(EnProgreso, "En progreso")));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-23, CA-18: estado fuera del flujo del tipo
    [Fact]
    public void AsegurarEstadoAsignable_EstadoFueraDelFlujo_LanzaEstadoIncidenciaInvalido()
    {
        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() => ReglasEstadoIncidencia.AsegurarEstadoAsignable(
            Nuevo, PorHacer, FlujoGeneralConEstados(), Estado(Nuevo, "Nuevo")));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-23: estado inexistente
    [Fact]
    public void AsegurarEstadoAsignable_EstadoInexistente_LanzaEstadoIncidenciaInvalido()
    {
        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() => ReglasEstadoIncidencia.AsegurarEstadoAsignable(
            99, PorHacer, FlujoGeneralConEstados(), null));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-11, CA-10: cambiar a un estado inactivo distinto del actual
    [Fact]
    public void AsegurarEstadoAsignable_InactivoDistintoDelActual_LanzaEstadoIncidenciaInvalido()
    {
        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() => ReglasEstadoIncidencia.AsegurarEstadoAsignable(
            EnProgreso, PorHacer, FlujoGeneralConEstados(), Estado(EnProgreso, "En progreso", activo: false)));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-11, CA-10: mantener el estado actual aunque esté inactivo
    [Fact]
    public void AsegurarEstadoAsignable_InactivoIgualAlActual_NoLanza()
    {
        // Act
        var ex = Record.Exception(() => ReglasEstadoIncidencia.AsegurarEstadoAsignable(
            EnProgreso, EnProgreso, FlujoGeneralConEstados(), Estado(EnProgreso, "En progreso", activo: false)));

        // Assert
        Assert.Null(ex);
    }

    // RN-16: transiciones libres dentro del flujo
    [Fact]
    public void AsegurarEstadoAsignable_CualquierEstadoActivoDelFlujo_NoLanza()
    {
        // Act
        var ex = Record.Exception(() => ReglasEstadoIncidencia.AsegurarEstadoAsignable(
            Hecho, PorHacer, FlujoGeneralConEstados(), Estado(Hecho, "Hecho")));

        // Assert
        Assert.Null(ex);
    }
}
