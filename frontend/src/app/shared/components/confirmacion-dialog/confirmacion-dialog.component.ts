import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  viewChild,
} from '@angular/core';
import { SolicitudConfirmacion } from '../../../core/services/confirmacion.service';

/**
 * Diálogo de confirmación propio (sin librería) basado en `<dialog>` nativo: modal, foco
 * atrapado y cierre con Escape (equivale a cancelar).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-confirmacion-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog
      #dialogo
      class="dialogo"
      aria-labelledby="confirmacion-titulo"
      aria-describedby="confirmacion-mensaje"
      (cancel)="alCancelar($event)"
    >
      @if (solicitud(); as datos) {
        <h2 id="confirmacion-titulo">{{ datos.titulo }}</h2>
        <p id="confirmacion-mensaje">{{ datos.mensaje }}</p>
        <div class="acciones">
          <button type="button" class="btn" (click)="respuesta.emit(false)">
            {{ datos.textoCancelar }}
          </button>
          <button
            type="button"
            class="btn"
            [class.btn-peligro]="datos.peligro"
            [class.btn-primario]="!datos.peligro"
            (click)="respuesta.emit(true)"
          >
            {{ datos.textoConfirmar }}
          </button>
        </div>
      }
    </dialog>
  `,
})
export class ConfirmacionDialogComponent {
  /** Datos del diálogo; null = cerrado. */
  readonly solicitud = input<SolicitudConfirmacion | null>(null);
  /** true = confirmado, false = cancelado. */
  readonly respuesta = output<boolean>();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    effect(() => this.sincronizar(this.solicitud() !== null));
  }

  /** Escape: cancela en lugar de dejar que el navegador cierre el diálogo solo. */
  alCancelar(evento: Event): void {
    evento.preventDefault();
    this.respuesta.emit(false);
  }

  /** Abre o cierra el `<dialog>` nativo según haya solicitud. */
  private sincronizar(abierto: boolean): void {
    const elemento = this.dialogo().nativeElement;
    if (abierto && !elemento.open) {
      if (typeof elemento.showModal === 'function') {
        elemento.showModal();
      } else {
        elemento.setAttribute('open', '');
      }
    } else if (!abierto && elemento.open) {
      if (typeof elemento.close === 'function') {
        elemento.close();
      } else {
        elemento.removeAttribute('open');
      }
    }
  }
}
