import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/**
 * Paginador simple: anterior / siguiente e indicación de página y total de registros.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-paginador',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="paginador" aria-label="Paginación">
      <button
        type="button"
        class="btn"
        [disabled]="!hayAnterior()"
        (click)="cambioPagina.emit(pagina() - 1)"
      >
        Anterior
      </button>
      <span aria-live="polite">
        Página {{ pagina() }} de {{ totalPaginas() }} · {{ total() }} registro(s)
      </span>
      <button
        type="button"
        class="btn"
        [disabled]="!haySiguiente()"
        (click)="cambioPagina.emit(pagina() + 1)"
      >
        Siguiente
      </button>
    </nav>
  `,
})
export class PaginadorComponent {
  readonly pagina = input.required<number>();
  readonly tamanioPagina = input.required<number>();
  readonly total = input.required<number>();
  /** Emite la página solicitada. */
  readonly cambioPagina = output<number>();

  readonly totalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.total() / Math.max(1, this.tamanioPagina()))),
  );
  readonly hayAnterior = computed(() => this.pagina() > 1);
  readonly haySiguiente = computed(() => this.pagina() < this.totalPaginas());
}
