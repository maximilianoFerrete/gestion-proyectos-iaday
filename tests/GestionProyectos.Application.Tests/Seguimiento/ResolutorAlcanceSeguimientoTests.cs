using FluentValidation;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Seguimiento;
using static GestionProyectos.Application.Tests.Comun.DatosSeguimiento;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de ResolutorAlcanceSeguimiento: niveles y tipos efectivos del alcance (RN-04 a RN-06).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class ResolutorAlcanceSeguimientoTests
{
    private static AlcanceResuelto Resolver(int[]? nivelIds = null, int[]? tipoIds = null) =>
        ResolutorAlcanceSeguimiento.Resolver(Niveles(), TiposAlcance(), nivelIds, tipoIds);

    // RN-06, CA-14: sin nivelId ni tipoId → todos los niveles y tipos; sin filtro de tipos
    [Fact]
    public void Resolver_SinFiltros_DevuelveTodosLosNivelesYTipos()
    {
        // Act
        var alcance = Resolver();

        // Assert
        Assert.True(alcance.Dto.TodosLosNiveles);
        Assert.True(alcance.Dto.TodosLosTipos);
        Assert.Equal([Nivel1, Nivel2, Nivel3], alcance.Dto.Niveles.Select(n => n.Id));
        Assert.Equal(5, alcance.Dto.Tipos.Count);
        Assert.Null(alcance.TipoIds);
    }

    // RN-04, CA-17: nivelId=3 → tipos del nivel 3 (incluido el inactivo) y todosLosTipos = true
    [Fact]
    public void Resolver_Nivel3_DevuelveLosTiposDelNivelIncluidosInactivos()
    {
        // Act
        var alcance = Resolver(nivelIds: [Nivel3]);

        // Assert
        Assert.False(alcance.Dto.TodosLosNiveles);
        Assert.True(alcance.Dto.TodosLosTipos);
        Assert.Equal(["Bug", "Subtarea", "Viejo"], alcance.Dto.Tipos.Select(t => t.Nombre));
        Assert.Equal([Bug, Subtarea, TipoViejo], alcance.TipoIds);
    }

    // CA-17: el tipo inactivo figura en alcance.tipos con activo = false
    [Fact]
    public void Resolver_SinFiltros_IncluyeElTipoInactivoComoInactivo()
    {
        // Act
        var alcance = Resolver();

        // Assert
        Assert.False(alcance.Dto.Tipos.Single(t => t.Id == TipoViejo).Activo);
    }

    // RN-04, CA-15: nivelId=3 con tipoId Subtarea y Bug → exactamente esos dos
    [Fact]
    public void Resolver_Nivel3ConSubtareaYBug_DevuelveSoloEsosDos()
    {
        // Act
        var alcance = Resolver(nivelIds: [Nivel3], tipoIds: [Subtarea, Bug]);

        // Assert
        Assert.False(alcance.Dto.TodosLosTipos);
        Assert.Equal([Bug, Subtarea], alcance.Dto.Tipos.Select(t => t.Id));
        Assert.Equal([Bug, Subtarea], alcance.TipoIds);
    }

    // RN-04: tipoId sin nivelId → esos tipos; niveles = todos
    [Fact]
    public void Resolver_TipoSinNivel_DevuelveEsosTiposConTodosLosNiveles()
    {
        // Act
        var alcance = Resolver(tipoIds: [Hu]);

        // Assert
        Assert.True(alcance.Dto.TodosLosNiveles);
        Assert.Equal(3, alcance.Dto.Niveles.Count);
        Assert.Equal([Hu], alcance.Dto.Tipos.Select(t => t.Id));
        Assert.Equal([Hu], alcance.TipoIds);
    }

    // RN-05, CA-16: nivel 1 con Subtarea (sin intersección) → error en tipoId
    [Fact]
    public void Resolver_InterseccionVacia_LanzaValidationExceptionEnTipoId()
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => Resolver(nivelIds: [Nivel1], tipoIds: [Subtarea]));

        // Assert
        var error = Assert.Single(ex.Errors);
        Assert.Equal(("tipoId", "Ninguno de los tipos seleccionados pertenece a los niveles seleccionados."),
            (error.PropertyName, error.ErrorMessage));
    }

    // RN-05, CA-16: nivelId o tipoId inexistente → error en la clave correspondiente
    [Theory]
    [InlineData(new[] { 99 }, null, "nivelId")]
    [InlineData(null, new[] { 99 }, "tipoId")]
    public void Resolver_IdInexistente_LanzaValidationExceptionEnLaClave(int[]? nivelIds, int[]? tipoIds,
        string clave)
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => Resolver(nivelIds, tipoIds));

        // Assert
        Assert.Equal(clave, Assert.Single(ex.Errors).PropertyName);
    }

    // RN-04: ids repetidos se deduplican
    [Fact]
    public void Resolver_IdsRepetidos_LosDeduplica()
    {
        // Act
        var alcance = Resolver(nivelIds: [Nivel3, Nivel3], tipoIds: [Bug, Bug]);

        // Assert
        Assert.Single(alcance.Dto.Niveles);
        Assert.Equal([Bug], alcance.TipoIds);
    }

    // RN-06: niveles por número; tipos por nivel y nombre, aunque el catálogo venga desordenado
    [Fact]
    public void Resolver_CatalogoDesordenado_OrdenaNivelesPorNumeroYTiposPorNivelYNombre()
    {
        // Arrange
        NivelReferenciaDto[] niveles = [new(30, 3, "Nivel 3"), new(10, 1, "Nivel 1")];
        TipoCatalogoAlcance[] tipos =
        [
            new(3, "subtarea", true, 30, 3), new(5, "Bug", true, 30, 3), new(1, "Épica", true, 10, 1),
        ];

        // Act
        var alcance = ResolutorAlcanceSeguimiento.Resolver(niveles, tipos, null, null);

        // Assert
        Assert.Equal([1, 3], alcance.Dto.Niveles.Select(n => n.Numero));
        Assert.Equal(["Épica", "Bug", "subtarea"], alcance.Dto.Tipos.Select(t => t.Nombre));
    }
}
