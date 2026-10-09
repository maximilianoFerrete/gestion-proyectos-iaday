import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Muestra los mensajes de error de un campo. El `id` permite enlazarlo desde el input con
 * `aria-describedby`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-campo-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (mensajes().length > 0) {
      <div class="campo-error" [id]="idError()" aria-live="polite">
        @for (mensaje of mensajes(); track $index) {
          <p>{{ mensaje }}</p>
        }
      </div>
    }
  `,
})
export class CampoErrorComponent {
  /** Mensajes a mostrar (vacío = no se renderiza nada). */
  readonly mensajes = input.required<readonly string[]>();
  /** Id del elemento con los errores. */
  readonly idError = input.required<string>();
}
