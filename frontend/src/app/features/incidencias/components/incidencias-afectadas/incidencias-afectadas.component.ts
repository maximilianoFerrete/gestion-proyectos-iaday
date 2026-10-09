import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ReferenciaCodigo, enlacesIncidencias } from '../../utils/incidencias-afectadas';

/**
 * Lista de incidencias afectadas por un error (extensión `incidencias`, RF-002 0.2) como enlaces
 * (`TIPO_INCOMPATIBLE_CON_HIJAS`, `VENCIMIENTO_ANTERIOR_A_HIJAS`).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencias-afectadas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (enlaces().length > 0) {
      <div class="incidencias-afectadas">
        <span class="texto-secundario">Incidencias afectadas:</span>
        <ul class="lista-enlaces">
          @for (enlace of enlaces(); track enlace.codigo) {
            <li>
              <a [routerLink]="enlace.ruta" [queryParams]="enlace.queryParams">{{
                enlace.codigo
              }}</a>
            </li>
          }
        </ul>
      </div>
    }
  `,
})
export class IncidenciasAfectadasComponent {
  readonly proyectoId = input.required<number>();
  /** Códigos informados por la API. */
  readonly codigos = input.required<readonly string[]>();
  /** Incidencias conocidas (p. ej. hijas) para enlazar directo al detalle. */
  readonly conocidas = input<readonly ReferenciaCodigo[]>([]);

  readonly enlaces = computed(() =>
    enlacesIncidencias(this.proyectoId(), this.codigos(), this.conocidas()),
  );
}
