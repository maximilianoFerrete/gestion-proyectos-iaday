import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Notificacion } from '../services/notificacion.service';

/**
 * Lista de toasts (notificaciones) anunciada a lectores de pantalla.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-toasts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toasts" aria-live="polite" aria-atomic="false">
      @for (notificacion of notificaciones(); track notificacion.id) {
        <div [class]="'toast toast-' + notificacion.tipo" role="status">
          <span>{{ notificacion.mensaje }}</span>
          <button
            type="button"
            class="toast-cerrar"
            aria-label="Cerrar notificación"
            (click)="cerrar.emit(notificacion.id)"
          >
            &times;
          </button>
        </div>
      }
    </div>
  `,
})
export class ToastsComponent {
  readonly notificaciones = input.required<readonly Notificacion[]>();
  /** Emite el id de la notificación a cerrar. */
  readonly cerrar = output<number>();
}
