import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Params, RouterLink } from '@angular/router';

/**
 * Mensaje "No se encontró el recurso" con botón para volver al listado (404 al cargar).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-no-encontrado',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <section class="panel no-encontrado" role="alert">
      <p>No se encontró el recurso</p>
      <a class="btn" [routerLink]="rutaVolver()" [queryParams]="queryParamsVolver()">
        {{ textoVolver() }}
      </a>
    </section>
  `,
})
export class NoEncontradoComponent {
  readonly rutaVolver = input.required<string>();
  readonly queryParamsVolver = input<Params>({});
  readonly textoVolver = input<string>('Volver al listado');
}
