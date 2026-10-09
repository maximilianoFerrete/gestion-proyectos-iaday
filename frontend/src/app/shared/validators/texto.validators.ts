import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Devuelve el valor del control recortado o '' si no es un string. */
function valorRecortado(control: AbstractControl): string {
  const valor: unknown = control.value;
  return typeof valor === 'string' ? valor.trim() : '';
}

/** Requerido tras trim: un texto de solo espacios equivale a vacío (RN-31). Error `required`. */
export const textoRequerido: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  valorRecortado(control).length === 0 ? { required: true } : null;

/** Largo máximo tras trim (el backend recorta antes de validar). Error `maxlength`. */
export function longitudMaxima(maximo: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const largo = valorRecortado(control).length;
    return largo > maximo ? { maxlength: { requiredLength: maximo, actualLength: largo } } : null;
  };
}

/** Recorta un texto obligatorio antes de enviarlo a la API. */
export function recortar(valor: string): string {
  return valor.trim();
}

/** Recorta un texto opcional; vacío tras trim se envía como null (spec 0.2). */
export function recortarOpcional(valor: string | null | undefined): string | null {
  const recortado = (valor ?? '').trim();
  return recortado.length === 0 ? null : recortado;
}
