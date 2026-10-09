import { InjectionToken } from '@angular/core';

/**
 * Abstracción mínima del almacenamiento local (por defecto `localStorage`) para poder
 * reemplazarlo en tests o en entornos sin `window`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export interface Almacenamiento {
  getItem(clave: string): string | null;
  setItem(clave: string, valor: string): void;
  removeItem(clave: string): void;
}

/** Almacenamiento en memoria usado cuando `localStorage` no está disponible. */
function crearAlmacenamientoEnMemoria(): Almacenamiento {
  const datos = new Map<string, string>();
  return {
    getItem: (clave) => datos.get(clave) ?? null,
    setItem: (clave, valor) => void datos.set(clave, valor),
    removeItem: (clave) => void datos.delete(clave),
  };
}

export const ALMACENAMIENTO = new InjectionToken<Almacenamiento>('ALMACENAMIENTO', {
  providedIn: 'root',
  factory: () =>
    typeof localStorage === 'undefined' ? crearAlmacenamientoEnMemoria() : localStorage,
});
