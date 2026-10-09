/**
 * Modelos de error de la API: ProblemDetails (RFC 9457) con `codigo` y su versión normalizada
 * `ErrorApi`, que es lo que reciben los llamadores tras pasar por `errorInterceptor` (spec 0.3).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */

/** Códigos de error del catálogo de la spec (0.3). */
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
  | 'AGRUPACION_NOMBRE_DUPLICADO';

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
}

/** Errores de validación por campo (clave camelCase = nombre del campo). */
export type ErroresPorCampo = Readonly<Record<string, readonly string[]>>;

/**
 * Error normalizado de una llamada HTTP a la API.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
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
