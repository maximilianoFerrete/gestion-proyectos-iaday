using GestionProyectos.Domain.Excepciones;
using Microsoft.Data.SqlClient;

namespace GestionProyectos.Infrastructure.SqlServer.Conexion;

/// <summary>
/// Traduce violaciones conocidas de SQL Server (2627/2601 unicidad, 547 FK) a errores del catálogo.
/// Es un respaldo: las validaciones propias de Application se ejecutan antes.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Restricciones de incidencias y FK compuestas (RF-002)
/// </remarks>
internal static class TraductorErroresSql
{
    private const int ViolacionRestriccionUnica = 2627;
    private const int ViolacionIndiceUnico = 2601;
    private const int ViolacionReferencia = 547;

    private static readonly IReadOnlyDictionary<string, (string Codigo, string Mensaje)> Duplicados =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["UQ_Personas_Email"] =
                (CodigosError.PersonaEmailDuplicado, "Ya existe una persona con ese email."),
            ["UQ_EstadosProyecto_Nombre"] =
                (CodigosError.EstadoNombreDuplicado, "Ya existe un estado de proyecto con ese nombre."),
            ["UQ_Agrupaciones_ProyectoId_Nombre"] =
                (CodigosError.AgrupacionNombreDuplicado, "Ya existe una agrupación con ese nombre en el proyecto."),
            ["UQ_NivelesIncidencia_Numero"] =
                (CodigosError.NivelNumeroDuplicado, "Ya existe un nivel con ese número."),
            ["UQ_NivelesIncidencia_Nombre"] =
                (CodigosError.NivelNombreDuplicado, "Ya existe un nivel con ese nombre."),
            ["UQ_TiposIncidencia_Nombre"] =
                (CodigosError.TipoNombreDuplicado, "Ya existe un tipo de incidencia con ese nombre."),
            ["UQ_EstadosIncidencia_Nombre"] =
                (CodigosError.EstadoIncidenciaNombreDuplicado, "Ya existe un estado de incidencia con ese nombre."),
            ["UQ_FlujosIncidencia_Nombre"] =
                (CodigosError.FlujoNombreDuplicado, "Ya existe un flujo con ese nombre."),
            ["UQ_PrioridadesIncidencia_Nombre"] =
                (CodigosError.PrioridadNombreDuplicado, "Ya existe una prioridad con ese nombre."),
            ["UQ_RelacionesTiposIncidencia_TipoPadreId_TipoHijoId"] =
                (CodigosError.RelacionDuplicada, "Ya existe la relación entre esos tipos."),
        };

    private static readonly IReadOnlyDictionary<string, (string Codigo, string Mensaje)> Referencias =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["FK_Incidencias_Incidencias_Padre"] =
                (CodigosError.PadreInvalido, "La incidencia padre no existe en el proyecto."),
            ["FK_Incidencias_Agrupaciones"] =
                (CodigosError.AgrupacionInvalida, "La agrupación no existe en el proyecto."),
        };

    /// <summary>Indica si es una violación conocida (duplicado o referencia inválida).</summary>
    public static bool EsConocido(SqlException ex) => Buscar(ex) is not null;

    /// <summary>Convierte la violación en ConflictoException (409) o ValidacionNegocioException (400).</summary>
    public static ExcepcionDominio Traducir(SqlException ex)
    {
        var (esDuplicado, codigo, mensaje) = Buscar(ex)
            ?? throw new InvalidOperationException("La excepción no es un error SQL conocido.", ex);
        return esDuplicado
            ? new ConflictoException(codigo, mensaje)
            : new ValidacionNegocioException(codigo, mensaje);
    }

    private static (bool EsDuplicado, string Codigo, string Mensaje)? Buscar(SqlException ex)
    {
        var mapa = ex.Number switch
        {
            ViolacionRestriccionUnica or ViolacionIndiceUnico => Duplicados,
            ViolacionReferencia => Referencias,
            _ => null,
        };
        var nombre = mapa?.Keys.FirstOrDefault(n => ex.Message.Contains($"\"{n}\"", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains($"'{n}'", StringComparison.OrdinalIgnoreCase));
        if (mapa is null || nombre is null)
        {
            return null;
        }

        var (codigo, mensaje) = mapa[nombre];
        return (ReferenceEquals(mapa, Duplicados), codigo, mensaje);
    }
}
