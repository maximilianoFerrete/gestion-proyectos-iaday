import { Injectable, signal } from '@angular/core';
import { Observable, Subject, take } from 'rxjs';

/** Datos del diálogo de confirmación. */
export interface SolicitudConfirmacion {
  readonly titulo: string;
  readonly mensaje: string;
  readonly textoConfirmar: string;
  readonly textoCancelar: string;
  /** Resalta el botón de confirmación como acción destructiva. */
  readonly peligro: boolean;
}

/** Opciones al pedir una confirmación (título y textos de botones opcionales). */
export type OpcionesConfirmacion = Pick<SolicitudConfirmacion, 'mensaje'> &
  Partial<Omit<SolicitudConfirmacion, 'mensaje'>>;

/**
 * Orquesta el `ConfirmacionDialogComponent` único del layout: las páginas piden una
 * confirmación y reciben un Observable<boolean> (true = confirmado). Permite mockearlo en tests.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Injectable({ providedIn: 'root' })
export class ConfirmacionService {
  private readonly solicitudActual = signal<SolicitudConfirmacion | null>(null);
  private respuesta: Subject<boolean> | null = null;

  /** Solicitud visible o null si el diálogo está cerrado. */
  readonly solicitud = this.solicitudActual.asReadonly();

  /** Abre el diálogo y emite una única vez la respuesta del usuario. */
  confirmar(opciones: OpcionesConfirmacion): Observable<boolean> {
    this.responder(false);
    this.respuesta = new Subject<boolean>();
    this.solicitudActual.set({
      titulo: opciones.titulo ?? 'Confirmar',
      mensaje: opciones.mensaje,
      textoConfirmar: opciones.textoConfirmar ?? 'Aceptar',
      textoCancelar: opciones.textoCancelar ?? 'Cancelar',
      peligro: opciones.peligro ?? false,
    });
    return this.respuesta.pipe(take(1));
  }

  /** Cierra el diálogo devolviendo la respuesta al solicitante. */
  responder(confirmado: boolean): void {
    const respuesta = this.respuesta;
    this.respuesta = null;
    this.solicitudActual.set(null);
    if (respuesta) {
      respuesta.next(confirmado);
      respuesta.complete();
    }
  }
}
