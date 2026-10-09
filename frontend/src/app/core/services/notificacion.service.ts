import { Injectable, signal } from '@angular/core';

/** Tipo visual de una notificación. */
export type TipoNotificacion = 'exito' | 'error' | 'info';

/** Notificación (toast) visible. */
export interface Notificacion {
  readonly id: number;
  readonly tipo: TipoNotificacion;
  readonly mensaje: string;
}

/** Duración por defecto de un toast en milisegundos. */
export const DURACION_TOAST_MS = 5000;

/**
 * Toasts simples (sin librería). Mantiene la lista en un signal que renderiza
 * `ToastsComponent` en el layout.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Injectable({ providedIn: 'root' })
export class NotificacionService {
  private readonly lista = signal<readonly Notificacion[]>([]);
  private siguienteId = 1;

  /** Notificaciones visibles (solo lectura). */
  readonly notificaciones = this.lista.asReadonly();

  /** Muestra un toast de éxito. */
  exito(mensaje: string): void {
    this.agregar('exito', mensaje);
  }

  /** Muestra un toast de error. */
  error(mensaje: string): void {
    this.agregar('error', mensaje);
  }

  /** Muestra un toast informativo. */
  info(mensaje: string): void {
    this.agregar('info', mensaje);
  }

  /** Cierra la notificación indicada. */
  cerrar(id: number): void {
    this.lista.update((actual) => actual.filter((n) => n.id !== id));
  }

  /** Agrega un toast y programa su cierre automático. */
  private agregar(tipo: TipoNotificacion, mensaje: string): void {
    const id = this.siguienteId++;
    this.lista.update((actual) => [...actual, { id, tipo, mensaje }]);
    setTimeout(() => this.cerrar(id), DURACION_TOAST_MS);
  }
}
