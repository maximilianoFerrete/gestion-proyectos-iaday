import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Error `entero` si el valor no es un número entero (null/'' se deja a `required`). */
export const entero: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor: unknown = control.value;
  if (valor === null || valor === undefined || valor === '') {
    return null;
  }
  return typeof valor === 'number' && Number.isInteger(valor) ? null : { entero: true };
};
