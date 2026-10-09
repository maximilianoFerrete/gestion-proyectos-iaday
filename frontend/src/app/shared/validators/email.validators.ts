import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Validador de formato de email alineado con el backend (PersonaRequestValidator, RN-32).
 * Usa la misma regex que el backend para que el front no deje enviar emails que la API rechaza.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001, reemplaza Validators.email)
 */

/** Regex de email del backend: algo@algo.algo, sin espacios ni @ adicionales. */
export const PATRON_EMAIL = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

/** Formato de email tras trim; vacío lo resuelve `textoRequerido`. Error `email`. */
export const emailValido: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor: unknown = control.value;
  const recortado = typeof valor === 'string' ? valor.trim() : '';
  if (recortado.length === 0) {
    return null;
  }
  return PATRON_EMAIL.test(recortado) ? null : { email: true };
};
