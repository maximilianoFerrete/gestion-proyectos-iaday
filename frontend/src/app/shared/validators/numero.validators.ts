import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Validadores numéricos compartidos.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Agrega `maxDecimales` (RF-005 RN-34)
 */

/** Error `entero` si el valor no es un número entero (null/'' se deja a `required`). */
export const entero: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor: unknown = control.value;
  if (valor === null || valor === undefined || valor === '') {
    return null;
  }
  return typeof valor === 'number' && Number.isInteger(valor) ? null : { entero: true };
};

/** Indica si el número tiene como máximo la cantidad de decimales indicada. */
export function tieneHastaDecimales(valor: number, decimales: number): boolean {
  const factor = 10 ** decimales;
  return Math.round(valor * factor) / factor === valor;
}

/** Error `maxDecimales` si el número tiene más decimales que los permitidos (vacío es válido). */
export function maxDecimales(decimales: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const valor: unknown = control.value;
    if (typeof valor !== 'number' || !Number.isFinite(valor)) {
      return null;
    }
    return tieneHastaDecimales(valor, decimales) ? null : { maxDecimales: { decimales } };
  };
}
