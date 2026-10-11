using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.RelacionesTiposIncidencia;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.RelacionesTiposIncidencia;

/// <summary>
/// Tests de RelacionTipoIncidenciaService y su validador (RN-01, RN-08, RN-09, CA-05).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class RelacionTipoIncidenciaServiceTests
{
    private const int Epica = 1;
    private const int Hu = 2;
    private const int Subtarea = 3;
    private const int Bug = 5;
    private const int Inactivo = 6;
    private readonly IRelacionTipoIncidenciaRepository _relaciones = Substitute.For<IRelacionTipoIncidenciaRepository>();
    private readonly ITipoIncidenciaRepository _tipos = Substitute.For<ITipoIncidenciaRepository>();
    private readonly INivelIncidenciaRepository _niveles = Substitute.For<INivelIncidenciaRepository>();

    public RelacionTipoIncidenciaServiceTests()
    {
        TipoIncidencia[] tipos =
        [
            Tipo(Epica, "Épica", nivelId: 1), Tipo(Hu, "Historia de Usuario", nivelId: 2),
            Tipo(Subtarea, "Subtarea", nivelId: 3), Tipo(Bug, "Bug", nivelId: 3),
            Tipo(Inactivo, "Viejo", nivelId: 3, activo: false),
        ];
        foreach (var tipo in tipos)
        {
            _tipos.ObtenerPorIdAsync(tipo.Id, Arg.Any<CancellationToken>()).Returns(tipo);
        }

        _niveles.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Nivel(1, 1));
        _niveles.ObtenerPorIdAsync(2, Arg.Any<CancellationToken>()).Returns(Nivel(2, 2));
        _niveles.ObtenerPorIdAsync(3, Arg.Any<CancellationToken>()).Returns(Nivel(3, 3));
    }

    private RelacionTipoIncidenciaService Sut(IUsuarioActual? usuario = null) =>
        new(_relaciones, _tipos, _niveles, usuario ?? Configurador(), new CrearRelacionTipoIncidenciaRequestValidator(),
            DatosPrueba.Reloj(), NullLogger<RelacionTipoIncidenciaService>.Instance);

    private Task<RelacionTipoIncidenciaDto> Crear(int padre, int hijo, IUsuarioActual? usuario = null) =>
        Sut(usuario).CrearAsync(new CrearRelacionTipoIncidenciaRequest(padre, hijo), CancellationToken.None);

    // RN-08, CA-05: Subtarea → Épica (3 → 1) o Bug → Subtarea (mismo nivel) → 400 RELACION_NIVELES_INVALIDOS
    [Theory]
    [InlineData(Subtarea, Epica)]
    [InlineData(Bug, Subtarea)]
    public async Task CrearAsync_NivelPadreNoMenor_LanzaRelacionNivelesInvalidos(int padre, int hijo)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Crear(padre, hijo));

        // Assert
        Assert.Equal(CodigosError.RelacionNivelesInvalidos, ex.Codigo);
        await _relaciones.DidNotReceive().AgregarAsync(Arg.Any<RelacionTipoIncidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-07, RN-08: tipo inactivo o inexistente → 400 TIPO_INVALIDO
    [Theory]
    [InlineData(Hu, Inactivo)]
    [InlineData(99, Subtarea)]
    public async Task CrearAsync_TipoInactivoOInexistente_LanzaTipoInvalido(int padre, int hijo)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Crear(padre, hijo));

        // Assert
        Assert.Equal(CodigosError.TipoInvalido, ex.Codigo);
    }

    // RN-08, CA-05: par existente → 409 RELACION_DUPLICADA
    [Fact]
    public async Task CrearAsync_ParExistente_LanzaRelacionDuplicada()
    {
        // Arrange
        _relaciones.ExisteAsync(Hu, Subtarea, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Crear(Hu, Subtarea));

        // Assert
        Assert.Equal(CodigosError.RelacionDuplicada, ex.Codigo);
    }

    // RN-08: mismo tipo como padre e hijo → error de validación
    [Fact]
    public async Task CrearAsync_MismoTipo_LanzaValidationException()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Crear(Hu, Hu));

        // Assert
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CrearRelacionTipoIncidenciaRequest.TipoHijoId));
    }

    // RN-08, CA-04: relación válida (nivel no consecutivo también) → inserta
    [Theory]
    [InlineData(Hu, Bug)]
    [InlineData(Epica, Subtarea)]
    public async Task CrearAsync_Valida_AgregaLaRelacion(int padre, int hijo)
    {
        // Arrange
        _relaciones.ObtenerDtoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new RelacionTipoIncidenciaDto(7, new(padre, "P", true, 1), new(hijo, "H", true, 3),
                DatosPrueba.AhoraTruncado));

        // Act
        await Crear(padre, hijo);

        // Assert
        await _relaciones.Received(1).AgregarAsync(
            Arg.Is<RelacionTipoIncidencia>(r => r.TipoPadreId == padre && r.TipoHijoId == hijo),
            Arg.Any<CancellationToken>());
    }

    // RN-01, CA-02: crear sin permiso → 403
    [Fact]
    public async Task CrearAsync_SinPermiso_LanzaPermisoDenegado()
    {
        // Act
        var accion = () => Crear(Hu, Bug, Usuario());

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
    }

    // RN-09, CA-06: el servicio no expone editar ni eliminar relaciones
    [Fact]
    public void IRelacionTipoIncidenciaService_Metodos_SoloListarYCrear()
    {
        // Act
        var metodos = typeof(IRelacionTipoIncidenciaService).GetMethods().Select(m => m.Name).Order();

        // Assert
        Assert.Equal(["CrearAsync", "ListarAsync"], metodos);
    }

    // RN-08: el validador exige ambos ids
    [Fact]
    public void Validator_IdsAusentes_ErrorEnAmbos()
    {
        // Act
        var resultado = new CrearRelacionTipoIncidenciaRequestValidator()
            .Validate(new CrearRelacionTipoIncidenciaRequest(null, null));

        // Assert
        Assert.Equal(["TipoHijoId", "TipoPadreId"], resultado.Errors.Select(e => e.PropertyName).Distinct().Order());
    }
}
