/**
 * Conversión de respuestas de error HTTP (ProblemDetails) a `ErrorApi`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Expone la extensión `incidencias` (RF-002 0.2)
 */
import { HttpErrorResponse } from '@angular/common/http';
import { ErrorApi, ErroresPorCampo, ProblemDetails } from '../models/error-api.model';

/** Mensajes genéricos cuando la respuesta no trae `detail`. */
export const MENSAJES_GENERICOS = {
  sinConexion: 'No se pudo conectar con el servidor.',
  noEncontrado: 'No se encontró el recurso',
  inesperado: 'Ocurrió un error inesperado. Intentá nuevamente.',
  validacion: 'Hay datos inválidos. Revisá el formulario.',
} as const;

/** Type guard: el valor tiene forma de ProblemDetails. */
export function esProblemDetails(valor: unknown): valor is ProblemDetails {
  return typeof valor === 'object' && valor !== null && ('status' in valor || 'title' in valor);
}

/** Normaliza una clave de `errors` a camelCase (`$.FechaInicio` → `fechaInicio`). */
export function normalizarClaveCampo(clave: string): string {
  const sinPrefijo = clave.replace(/^\$\./, '');
  return sinPrefijo.charAt(0).toLowerCase() + sinPrefijo.slice(1);
}

/** Copia `errors` normalizando claves y descartando valores que no sean arrays de strings. */
function normalizarErrores(errores: ProblemDetails['errors']): ErroresPorCampo {
  const resultado: Record<string, string[]> = {};
  if (!errores) {
    return resultado;
  }
  for (const [clave, mensajes] of Object.entries(errores)) {
    if (Array.isArray(mensajes)) {
      const clavesNormalizada = normalizarClaveCampo(clave);
      const textos = mensajes.filter((m): m is string => typeof m === 'string');
      resultado[clavesNormalizada] = [...(resultado[clavesNormalizada] ?? []), ...textos];
    }
  }
  return resultado;
}

/** Lee la extensión `incidencias` (RF-002 0.2) descartando valores que no sean strings. */
export function leerIncidenciasAfectadas(cuerpo: ProblemDetails): string[] {
  const valor = cuerpo.incidencias;
  return Array.isArray(valor) ? valor.filter((c): c is string => typeof c === 'string') : [];
}

/** Mensaje por defecto según el código HTTP cuando no hay `detail`. */
function mensajePorDefecto(status: number, titulo: string | undefined): string {
  if (status === 0) {
    return MENSAJES_GENERICOS.sinConexion;
  }
  if (status === 404) {
    return MENSAJES_GENERICOS.noEncontrado;
  }
  if (status >= 500) {
    return MENSAJES_GENERICOS.inesperado;
  }
  return titulo ?? MENSAJES_GENERICOS.inesperado;
}

/** Convierte un HttpErrorResponse (con o sin ProblemDetails) en un ErrorApi. */
export function convertirError(respuesta: HttpErrorResponse): ErrorApi {
  const cuerpo: unknown = respuesta.error;
  if (!esProblemDetails(cuerpo)) {
    return new ErrorApi(respuesta.status, null, mensajePorDefecto(respuesta.status, undefined));
  }
  const errores = normalizarErrores(cuerpo.errors);
  const tieneErroresCampo = Object.keys(errores).length > 0;
  const detalle =
    respuesta.status >= 500
      ? MENSAJES_GENERICOS.inesperado
      : (cuerpo.detail ??
        (tieneErroresCampo
          ? MENSAJES_GENERICOS.validacion
          : mensajePorDefecto(respuesta.status, cuerpo.title)));
  return new ErrorApi(
    respuesta.status,
    cuerpo.codigo ?? null,
    detalle,
    errores,
    leerIncidenciasAfectadas(cuerpo),
  );
}
