import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Clave de error usada para los mensajes que llegan del backend. */
export const ERROR_SERVIDOR = 'servidor';

/** Lee un número de un objeto de error de validación (p. ej. `requiredLength`). */
function leerNumero(detalle: unknown, propiedad: string): number | null {
  if (typeof detalle !== 'object' || detalle === null || !(propiedad in detalle)) {
    return null;
  }
  const valor: unknown = Reflect.get(detalle, propiedad);
  return typeof valor === 'number' ? valor : null;
}

/** Traduce los errores de un control a mensajes en castellano. */
export function traducirErrores(errores: ValidationErrors | null, etiqueta: string): string[] {
  if (!errores) {
    return [];
  }
  const mensajes: string[] = [];
  for (const [clave, detalle] of Object.entries(errores)) {
    mensajes.push(traducirError(clave, detalle, etiqueta));
  }
  return mensajes;
}

/** Mensaje para una clave de error concreta. */
function traducirError(clave: string, detalle: unknown, etiqueta: string): string {
  switch (clave) {
    case 'required':
      return `${etiqueta} es obligatorio.`;
    case 'maxlength':
      return `${etiqueta} admite como máximo ${leerNumero(detalle, 'requiredLength')} caracteres.`;
    case 'email':
      return `${etiqueta} no tiene un formato válido.`;
    case 'min':
      return `${etiqueta} debe ser mayor o igual a ${leerNumero(detalle, 'min')}.`;
    case 'max':
      return `${etiqueta} debe ser menor o igual a ${leerNumero(detalle, 'max')}.`;
    case 'entero':
      return `${etiqueta} debe ser un número entero.`;
    case ERROR_SERVIDOR:
      return typeof detalle === 'string' ? detalle : `${etiqueta} no es válido.`;
    default:
      return `${etiqueta} no es válido.`;
  }
}

/** Mensajes de un control solo si fue tocado o modificado (para no mostrar errores al abrir). */
export function mensajesVisibles(control: AbstractControl | null, etiqueta: string): string[] {
  if (!control || !(control.touched || control.dirty)) {
    return [];
  }
  return traducirErrores(control.errors, etiqueta);
}
