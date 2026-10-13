namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// Catálogo de códigos de error expuestos en el campo "codigo" de ProblemDetails (spec 0.3).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega los códigos de incidencias y su configuración (RF-002)
/// Modificado: 2026-10-10 - Agrega ESTADO_CATEGORIA_INCOMPATIBLE (RF-005, RN-18)
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

    // RF-002: 404
    public const string NivelNoEncontrado = "NIVEL_NO_ENCONTRADO";
    public const string TipoIncidenciaNoEncontrado = "TIPO_INCIDENCIA_NO_ENCONTRADO";
    public const string EstadoIncidenciaNoEncontrado = "ESTADO_INCIDENCIA_NO_ENCONTRADO";
    public const string FlujoNoEncontrado = "FLUJO_NO_ENCONTRADO";
    public const string PrioridadNoEncontrada = "PRIORIDAD_NO_ENCONTRADA";
    public const string IncidenciaNoEncontrada = "INCIDENCIA_NO_ENCONTRADA";

    // RF-002: 400
    public const string NivelInvalido = "NIVEL_INVALIDO";
    public const string FlujoInvalido = "FLUJO_INVALIDO";
    public const string TipoInvalido = "TIPO_INVALIDO";
    public const string EstadoIncidenciaInvalido = "ESTADO_INCIDENCIA_INVALIDO";
    public const string PrioridadInvalida = "PRIORIDAD_INVALIDA";
    public const string AgrupacionInvalida = "AGRUPACION_INVALIDA";
    public const string PadreInvalido = "PADRE_INVALIDO";
    public const string RelacionNoPermitida = "RELACION_NO_PERMITIDA";
    public const string RelacionNivelesInvalidos = "RELACION_NIVELES_INVALIDOS";
    public const string AgrupacionHeredada = "AGRUPACION_HEREDADA";
    public const string VencimientoPosteriorAlPadre = "VENCIMIENTO_POSTERIOR_AL_PADRE";

    // RF-002: 409
    public const string NivelNombreDuplicado = "NIVEL_NOMBRE_DUPLICADO";
    public const string NivelNumeroDuplicado = "NIVEL_NUMERO_DUPLICADO";
    public const string TipoNombreDuplicado = "TIPO_NOMBRE_DUPLICADO";
    public const string RelacionDuplicada = "RELACION_DUPLICADA";
    public const string EstadoIncidenciaNombreDuplicado = "ESTADO_INCIDENCIA_NOMBRE_DUPLICADO";
    public const string FlujoNombreDuplicado = "FLUJO_NOMBRE_DUPLICADO";
    public const string PrioridadNombreDuplicado = "PRIORIDAD_NOMBRE_DUPLICADO";
    public const string EstadoIncidenciaInicialEnFlujo = "ESTADO_INCIDENCIA_INICIAL_EN_FLUJO";
    public const string FlujoEstadoEnUso = "FLUJO_ESTADO_EN_USO";
    public const string TipoFlujoIncompatible = "TIPO_FLUJO_INCOMPATIBLE";
    public const string PrioridadPredeterminadaNoDesactivable = "PRIORIDAD_PREDETERMINADA_NO_DESACTIVABLE";
    public const string PrioridadNoElegiblePredeterminada = "PRIORIDAD_NO_ELEGIBLE_PREDETERMINADA";
    public const string RelacionCircular = "RELACION_CIRCULAR";
    public const string TipoIncompatibleConHijas = "TIPO_INCOMPATIBLE_CON_HIJAS";
    public const string VencimientoAnteriorAHijas = "VENCIMIENTO_ANTERIOR_A_HIJAS";

    // RF-005: 409
    public const string EstadoCategoriaIncompatible = "ESTADO_CATEGORIA_INCOMPATIBLE";
}
