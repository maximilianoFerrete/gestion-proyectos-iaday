namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// Catálogo de códigos de error expuestos en el campo "codigo" de ProblemDetails (spec 0.3).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class CodigosError
{
    public const string Validacion = "VALIDACION";
    public const string ResponsableInvalido = "RESPONSABLE_INVALIDO";
    public const string EstadoInvalido = "ESTADO_INVALIDO";
    public const string UsuarioNoIdentificado = "USUARIO_NO_IDENTIFICADO";
    public const string UsuarioInvalido = "USUARIO_INVALIDO";
    public const string PermisoDenegado = "PERMISO_DENEGADO";
    public const string PersonaNoEncontrada = "PERSONA_NO_ENCONTRADA";
    public const string EstadoNoEncontrado = "ESTADO_NO_ENCONTRADO";
    public const string ProyectoNoEncontrado = "PROYECTO_NO_ENCONTRADO";
    public const string AgrupacionNoEncontrada = "AGRUPACION_NO_ENCONTRADA";
    public const string PersonaEmailDuplicado = "PERSONA_EMAIL_DUPLICADO";
    public const string PersonaUltimoAdmin = "PERSONA_ULTIMO_ADMIN";
    public const string EstadoNombreDuplicado = "ESTADO_NOMBRE_DUPLICADO";
    public const string EstadoSistemaNoModificable = "ESTADO_SISTEMA_NO_MODIFICABLE";
    public const string EstadoInicialNoDesactivable = "ESTADO_INICIAL_NO_DESACTIVABLE";
    public const string EstadoInicialNoFinal = "ESTADO_INICIAL_NO_FINAL";
    public const string EstadoNoElegibleInicial = "ESTADO_NO_ELEGIBLE_INICIAL";
    public const string ProyectoArchivado = "PROYECTO_ARCHIVADO";
    public const string AgrupacionNombreDuplicado = "AGRUPACION_NOMBRE_DUPLICADO";
    public const string ErrorInterno = "ERROR_INTERNO";
}
