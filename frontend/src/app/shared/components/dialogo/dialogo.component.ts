import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  viewChild,
} from '@angular/core';

/**
 * Diálogo modal genérico basado en `<dialog>` nativo (foco atrapado, Escape = cerrar). El
 * contenido se proyecta; quien lo usa decide qué renderizar mientras está abierto.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-dialogo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog
      #dialogo
      class="dialogo dialogo-formulario"
      [attr.aria-labelledby]="idTitulo()"
      (cancel)="alCancelar($event)"
    >
      <h2 [id]="idTitulo()">{{ titulo() }}</h2>
      <ng-content />
    </dialog>
  `,
})
export class DialogoComponent {
  /** Indica si el diálogo está abierto. */
  readonly abierto = input.required<boolean>();
  readonly titulo = input.required<string>();
  /** Id del título (para `aria-labelledby`). */
  readonly idTitulo = input.required<string>();
  /** Se emite al presionar Escape (el padre decide si cierra). */
  readonly cerrar = output<void>();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    effect(() => this.sincronizar(this.abierto()));
  }

  /** Escape: avisa al padre en lugar de dejar que el navegador cierre el diálogo solo. */
  alCancelar(evento: Event): void {
    evento.preventDefault();
    this.cerrar.emit();
  }

  /** Abre o cierra el `<dialog>` nativo (con alternativa si el entorno no soporta showModal). */
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
