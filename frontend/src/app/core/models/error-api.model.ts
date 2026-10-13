/**
 * Modelos de error de la API: ProblemDetails (RFC 9457) con `codigo` y su versión normalizada
 * `ErrorApi`, que es lo que reciben los llamadores tras pasar por `errorInterceptor` (spec 0.3).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Código `ESTADO_CATEGORIA_INCOMPATIBLE` (RF-005)
 */

/** Códigos de error del catálogo de la spec (RF-001 0.3 y RF-002 "Nuevos códigos de error"). */
export type CodigoError =
  | 'VALIDACION'
  | 'RESPONSABLE_INVALIDO'
  | 'ESTADO_INVALIDO'
  | 'USUARIO_NO_IDENTIFICADO'
  | 'USUARIO_INVALIDO'
  | 'PERMISO_DENEGADO'
  | 'PERSONA_NO_ENCONTRADA'
  | 'ESTADO_NO_ENCONTRADO'
  | 'PROYECTO_NO_ENCONTRADO'
  | 'AGRUPACION_NO_ENCONTRADA'
  | 'PERSONA_EMAIL_DUPLICADO'
  | 'PERSONA_ULTIMO_ADMIN'
  | 'ESTADO_NOMBRE_DUPLICADO'
  | 'ESTADO_SISTEMA_NO_MODIFICABLE'
  | 'ESTADO_INICIAL_NO_DESACTIVABLE'
  | 'ESTADO_INICIAL_NO_FINAL'
  | 'ESTADO_NO_ELEGIBLE_INICIAL'
  | 'PROYECTO_ARCHIVADO'
  | 'AGRUPACION_NOMBRE_DUPLICADO'
  // RF-002
  | 'NIVEL_NO_ENCONTRADO'
  | 'TIPO_INCIDENCIA_NO_ENCONTRADO'
  | 'ESTADO_INCIDENCIA_NO_ENCONTRADO'
  | 'FLUJO_NO_ENCONTRADO'
  | 'PRIORIDAD_NO_ENCONTRADA'
  | 'INCIDENCIA_NO_ENCONTRADA'
  | 'NIVEL_INVALIDO'
  | 'FLUJO_INVALIDO'
  | 'TIPO_INVALIDO'
  | 'ESTADO_INCIDENCIA_INVALIDO'
  | 'PRIORIDAD_INVALIDA'
  | 'AGRUPACION_INVALIDA'
  | 'PADRE_INVALIDO'
  | 'RELACION_NO_PERMITIDA'
  | 'RELACION_NIVELES_INVALIDOS'
  | 'AGRUPACION_HEREDADA'
  | 'VENCIMIENTO_POSTERIOR_AL_PADRE'
  | 'NIVEL_NOMBRE_DUPLICADO'
  | 'NIVEL_NUMERO_DUPLICADO'
  | 'TIPO_NOMBRE_DUPLICADO'
  | 'RELACION_DUPLICADA'
  | 'ESTADO_INCIDENCIA_NOMBRE_DUPLICADO'
  | 'FLUJO_NOMBRE_DUPLICADO'
  | 'PRIORIDAD_NOMBRE_DUPLICADO'
  | 'ESTADO_INCIDENCIA_INICIAL_EN_FLUJO'
  | 'FLUJO_ESTADO_EN_USO'
  | 'TIPO_FLUJO_INCOMPATIBLE'
  | 'PRIORIDAD_PREDETERMINADA_NO_DESACTIVABLE'
  | 'PRIORIDAD_NO_ELEGIBLE_PREDETERMINADA'
  | 'RELACION_CIRCULAR'
  | 'TIPO_INCOMPATIBLE_CON_HIJAS'
  | 'VENCIMIENTO_ANTERIOR_A_HIJAS'
  // RF-005
  | 'ESTADO_CATEGORIA_INCOMPATIBLE';

/** Cuerpo ProblemDetails tal como lo envía la API. */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly codigo?: string;
  readonly traceId?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
  /** Extensión de RF-002 (0.2): códigos de las incidencias afectadas (p. ej. hijas). */
  readonly incidencias?: readonly unknown[];
}

/** Errores de validación por campo (clave camelCase = nombre del campo). */
export type ErroresPorCampo = Readonly<Record<string, readonly string[]>>;

/**
 * Error normalizado de una llamada HTTP a la API.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Agrega `incidencias` (RF-002 0.2)
 */
export class ErrorApi extends Error {
  constructor(
    /** Código HTTP (0 si no hubo respuesta del servidor). */
    readonly status: number,
    /** Código de negocio del catálogo o null si la respuesta no lo trae. */
    readonly codigo: string | null,
    /** Mensaje para mostrar al usuario (`detail`, `title` o genérico). */
    readonly detalle: string,
    /** Errores de validación por campo (`errors`), vacío si no hay. */
    readonly errores: ErroresPorCampo = {},
    /** Códigos de incidencias afectadas (extensión `incidencias`, RF-002 0.2), vacío si no hay. */
    readonly incidencias: readonly string[] = [],
  ) {
    super(detalle);
    this.name = 'ErrorApi';
  }

  /** Indica si el error tiene el código de negocio indicado. */
  tieneCodigo(codigo: CodigoError): boolean {
    return this.codigo === codigo;
  }
}

/** Type guard para errores normalizados de la API. */
export function esErrorApi(error: unknown): error is ErrorApi {
  return error instanceof ErrorApi;
}
