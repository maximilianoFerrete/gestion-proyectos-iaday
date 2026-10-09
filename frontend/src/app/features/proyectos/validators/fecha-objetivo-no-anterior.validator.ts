import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Clave del error de grupo cuando la fecha objetivo es anterior a la de inicio. */
export const ERROR_FECHA_OBJETIVO_ANTERIOR = 'fechaObjetivoAnterior';

/** Mensaje mostrado en el campo Fecha objetivo (P2/P4). */
export const MENSAJE_FECHA_OBJETIVO_ANTERIOR =
  'La fecha objetivo debe ser igual o posterior a la fecha de inicio.';

/** Lee un campo `yyyy-MM-dd` del grupo; null si falta o no tiene ese formato. */
function leerFecha(grupo: AbstractControl, campo: string): string | null {
  const valor: unknown = grupo.get(campo)?.value;
  return typeof valor === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(valor) ? valor : null;
}

/**
 * Validador de grupo (RN-16): error si `fechaObjetivo < fechaInicio`. Igualdad válida; si falta
 * alguna de las dos fechas no informa error (lo hace `required`).
 */
export function fechaObjetivoNoAnterior(
  campoInicio = 'fechaInicio',
  campoObjetivo = 'fechaObjetivo',
): ValidatorFn {
  return (grupo: AbstractControl): ValidationErrors | null => {
    const inicio = leerFecha(grupo, campoInicio);
    const objetivo = leerFecha(grupo, campoObjetivo);
    if (inicio === null || objetivo === null) {
      return null;
    }
    // El formato ISO yyyy-MM-dd permite comparar como texto.
    return objetivo < inicio ? { [ERROR_FECHA_OBJETIVO_ANTERIOR]: true } : null;
  };
}
