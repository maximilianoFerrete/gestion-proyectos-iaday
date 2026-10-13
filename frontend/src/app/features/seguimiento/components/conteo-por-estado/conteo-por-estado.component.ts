import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { BadgeEstadoIncidenciaComponent } from '../../../incidencias/components/badge-estado-incidencia/badge-estado-incidencia.component';
import { ConteoEstadoDto } from '../../models/seguimiento.model';
import { porcentaje } from '../../utils/indicadores';
import { BarraEstadosComponent } from '../barra-estados/barra-estados.component';
import { TITULO_VER_EN_TABLERO } from '../indicadores/indicadores.component';

/**
 * Conteo por estado del Resumen (RF-005 RN-15, DA-12): barra apilada y tabla Estado (badge con
 * ícono de categoría) / Cantidad / %, en el orden recibido; finales con estilo diferenciado. Con
 * `clicable`, el estado es un enlace que emite su id (→ Tablero filtrado, RN-24).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-conteo-por-estado',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BadgeEstadoIncidenciaComponent, BarraEstadosComponent],
  template: `
    <app-barra-estados [porEstado]="porEstado()" />
    @if (porEstado().length === 0) {
      <p class="vacio">No hay estados en el alcance.</p>
    } @else {
      <table class="tabla tabla-conteo-estados">
        <caption class="sr-only">
          Cantidad de incidencias por estado
        </caption>
        <thead>
          <tr>
            <th scope="col">Estado</th>
            <th scope="col" class="celda-numero">Cantidad</th>
            <th scope="col" class="celda-numero">%</th>
          </tr>
        </thead>
        <tbody>
          @for (conteo of porEstado(); track conteo.estado.id) {
            <tr [class.fila-final]="conteo.estado.esFinal">
              <td>
                @if (clicable()) {
                  <button
                    type="button"
                    role="link"
                    class="btn-enlace"
                    [title]="tituloVerEnTablero"
                    [attr.aria-label]="conteo.estado.nombre + ': ' + tituloVerEnTablero"
                    (click)="seleccionar.emit(conteo.estado.id)"
                  >
                    <app-badge-estado-incidencia [estado]="conteo.estado" />
                  </button>
                } @else {
                  <app-badge-estado-incidencia [estado]="conteo.estado" />
                }
              </td>
              <td class="celda-numero">{{ conteo.cantidad }}</td>
              <td class="celda-numero">{{ porcentaje(conteo.cantidad) }} %</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class ConteoPorEstadoComponent {
  readonly porEstado = input.required<readonly ConteoEstadoDto[]>();
  /** Estados clicables (→ Tablero filtrado por estado). */
  readonly clicable = input(true);
  /** Emite el id del estado elegido. */
  readonly seleccionar = output<number>();

  protected readonly tituloVerEnTablero = TITULO_VER_EN_TABLERO;

  readonly total = computed(() => this.porEstado().reduce((suma, c) => suma + c.cantidad, 0));

  /** Porcentaje entero de la cantidad sobre el total del conteo. */
  porcentaje(cantidad: number): number {
    return porcentaje(cantidad, this.total());
  }
}
