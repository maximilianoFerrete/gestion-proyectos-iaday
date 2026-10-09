import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { EstadoFlujoRequest } from '../models/flujo-incidencia.model';

/**
 * Validador de la composición de un flujo (C4, RN-13/RN-14): al menos un estado, sin repetidos,
 * exactamente un inicial y ninguno inicial y final a la vez.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Claves de error de la composición. */
export const ERRORES_COMPOSICION_FLUJO = {
  sinEstados: 'flujoSinEstados',
  repetidos: 'flujoEstadosRepetidos',
  inicial: 'flujoInicialInvalido',
  inicialFinal: 'flujoInicialYFinal',
} as const;

/** Mensajes de cada error de composición. */
export const MENSAJES_COMPOSICION_FLUJO: Readonly<Record<string, string>> = {
  [ERRORES_COMPOSICION_FLUJO.sinEstados]: 'El flujo debe tener al menos un estado.',
  [ERRORES_COMPOSICION_FLUJO.repetidos]: 'Un estado no puede repetirse en el flujo.',
  [ERRORES_COMPOSICION_FLUJO.inicial]: 'El flujo debe tener exactamente un estado inicial.',
  [ERRORES_COMPOSICION_FLUJO.inicialFinal]: 'El estado inicial no puede ser final.',
};

/** Type guard: el valor es una lista de estados de flujo. */
function esListaEstados(valor: unknown): valor is readonly EstadoFlujoRequest[] {
  return (
    Array.isArray(valor) &&
    valor.every((v) => typeof v === 'object' && v !== null && 'estadoId' in v && 'esInicial' in v)
  );
}

/** Errores de composición de una lista de estados (función pura). */
export function erroresComposicionFlujo(
  estados: readonly EstadoFlujoRequest[],
): ValidationErrors | null {
  const errores: Record<string, true> = {};
  if (estados.length === 0) {
    errores[ERRORES_COMPOSICION_FLUJO.sinEstados] = true;
  }
  if (new Set(estados.map((e) => e.estadoId)).size !== estados.length) {
    errores[ERRORES_COMPOSICION_FLUJO.repetidos] = true;
  }
  if (estados.length > 0 && estados.filter((e) => e.esInicial).length !== 1) {
    errores[ERRORES_COMPOSICION_FLUJO.inicial] = true;
  }
  if (estados.some((e) => e.esInicial && e.esFinal)) {
    errores[ERRORES_COMPOSICION_FLUJO.inicialFinal] = true;
  }
  return Object.keys(errores).length > 0 ? errores : null;
}

/** Validador para el control (FormArray) de estados del flujo. */
export const composicionFlujoValida: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const valor: unknown = control.value;
  return erroresComposicionFlujo(esListaEstados(valor) ? valor : []);
};
