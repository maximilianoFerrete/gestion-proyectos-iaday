import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { formatearFecha } from '../../../shared/utils/fechas';
import { esVencimientoPosteriorAlPadre } from '../utils/vencimiento';

/**
 * Validador del campo Vencimiento de I4/I5 contra el vencimiento del padre elegido (RN-31).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Clave del error cuando el vencimiento supera al del padre. */
export const ERROR_VENCIMIENTO_POSTERIOR_AL_PADRE = 'vencimientoPosteriorAlPadre';

/** Mensaje del error con la fecha del padre en `dd/MM/yyyy`. */
export function mensajeVencimientoPosteriorAlPadre(vencimientoPadre: string): string {
  return `El vencimiento no puede ser posterior al del padre (${formatearFecha(vencimientoPadre)}).`;
}

/**
 * Error `vencimientoPosteriorAlPadre` (con `fechaPadre`) si el valor del control es mayor que el
 * vencimiento del padre. Válido si son iguales o si alguno es nulo. El padre puede cambiar, por
 * eso se recibe una función; tras cambiarlo hay que llamar a `updateValueAndValidity`.
 */
export function vencimientoNoPosteriorAlPadre(
  obtenerVencimientoPadre: () => string | null,
): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const valor: unknown = control.value;
    const hija = typeof valor === 'string' && valor.length > 0 ? valor : null;
    const padre = obtenerVencimientoPadre();
    return padre !== null && esVencimientoPosteriorAlPadre(hija, padre)
      ? { [ERROR_VENCIMIENTO_POSTERIOR_AL_PADRE]: { fechaPadre: padre } }
      : null;
  };
}
