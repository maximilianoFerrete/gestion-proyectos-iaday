import { FormGroup } from '@angular/forms';
import { CodigoError, ErroresPorCampo, esErrorApi } from '../../core/models/error-api.model';
import { MENSAJES_GENERICOS } from '../../core/interceptors/convertir-error';
import { ERROR_SERVIDOR } from './mensajes-error';

/** Resultado de interpretar un error de envío de formulario. */
export interface ErrorFormulario {
  /** Errores a mostrar junto a cada campo. */
  readonly campos: ErroresPorCampo;
  /** Mensaje a mostrar arriba del formulario (null si no corresponde). */
  readonly general: string | null;
}

/** Sin errores. */
export const SIN_ERRORES: ErrorFormulario = { campos: {}, general: null };

/** Mapeo de códigos de negocio que se muestran en un campo concreto (p. ej. duplicados). */
export type CamposPorCodigo = Partial<Record<CodigoError, string>>;

/**
 * Interpreta el error de un envío según la spec: 400 con `errors` → por campo; 400/404/409 con
 * `codigo` → arriba del form (o en el campo si está en `camposPorCodigo`). 401/403/5xx ya se
 * notificaron con toast en `errorInterceptor`, así que no se repiten.
 */
export function interpretarErrorFormulario(
  error: unknown,
  camposPorCodigo: CamposPorCodigo = {},
): ErrorFormulario {
  if (!esErrorApi(error)) {
    return { campos: {}, general: MENSAJES_GENERICOS.inesperado };
  }
  if (error.status === 0 || error.status === 401 || error.status === 403 || error.status >= 500) {
    return SIN_ERRORES;
  }
  if (Object.keys(error.errores).length > 0) {
    return { campos: error.errores, general: null };
  }
  const campo = Object.entries(camposPorCodigo).find(([codigo]) => codigo === error.codigo)?.[1];
  if (campo) {
    return { campos: { [campo]: [error.detalle] }, general: null };
  }
  return { campos: {}, general: error.detalle };
}

/**
 * Mensaje a mostrar como toast cuando falla una acción fuera de un formulario (archivar,
 * desactivar, eliminar...). Devuelve null si `errorInterceptor` ya lo notificó (401/403/5xx/0).
 */
export function mensajeErrorAccion(error: unknown): string | null {
  if (!esErrorApi(error)) {
    return MENSAJES_GENERICOS.inesperado;
  }
  if (error.status === 0 || error.status === 401 || error.status === 403 || error.status >= 500) {
    return null;
  }
  return error.detalle;
}

/** Indica si el error es un 404 (recurso inexistente al cargar una pantalla). */
export function esNoEncontrado(error: unknown): boolean {
  return esErrorApi(error) && error.status === 404;
}

/**
 * Marca en cada control el error `servidor` con su mensaje. Devuelve los mensajes cuyo campo no
 * existe en el formulario (se muestran arriba del form).
 */
export function aplicarErroresServidor(form: FormGroup, errores: ErroresPorCampo): string[] {
  const sinCampo: string[] = [];
  for (const [campo, mensajes] of Object.entries(errores)) {
    const control = form.get(campo);
    if (control) {
      control.setErrors({ ...control.errors, [ERROR_SERVIDOR]: mensajes.join(' ') });
      control.markAsTouched();
    } else {
      sinCampo.push(...mensajes);
    }
  }
  return sinCampo;
}
