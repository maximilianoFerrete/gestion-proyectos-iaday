import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { formatearEstimacion } from '../../../../shared/utils/estimacion';
import { BadgeEstadoIncidenciaComponent } from '../../../incidencias/components/badge-estado-incidencia/badge-estado-incidencia.component';
import { ConfiguracionTableroDto, ItemTableroDto } from '../../models/seguimiento.model';
import {
  ColumnaTablero,
  OrdenTablero,
  TITULOS_COLUMNA_TABLERO,
  alternarOrden,
  columnasVisibles,
  ordenVigente,
  ordenarTablero,
} from '../../utils/tablero';

/**
 * Grilla del Tablero (RF-005 DA-12, RN-29, RN-32, RN-33, RN-36): una fila por incidencia con las
 * columnas visibles según la configuración del proyecto (Tipo, Nro y Título siempre). Ordena en
 * el cliente al hacer clic en un encabezado (▲/▼, `aria-sort`); "Orden predeterminado" vuelve a
 * Tipo + Nro. El orden no se persiste: vuelve al predeterminado cuando llegan otros datos o si la
 * columna ordenada queda oculta. Nro enlaza a I3 con marca de vencida; clic en la fila → I3.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-tabla-tablero',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, BadgeEstadoIncidenciaComponent],
  template: `
    <div class="acciones acciones-tablero">
      <button
        type="button"
        class="btn btn-chico"
        [disabled]="ordenActual() === null"
        (click)="restablecerOrden()"
      >
        Orden predeterminado
      </button>
    </div>
    <div class="tabla-desplazable">
      <table class="tabla tabla-clickable tabla-tablero">
        <caption class="sr-only">
          Incidencias del tablero
        </caption>
        <thead>
          <tr>
            @for (columna of columnas(); track columna) {
              <th
                scope="col"
                [class.celda-numero]="columna === 'estimacion'"
                [attr.aria-sort]="ariaSort(columna)"
              >
                <button type="button" class="btn-orden" (click)="ordenar(columna)">
                  {{ titulos[columna] }}
                  <span aria-hidden="true">{{ flecha(columna) }}</span>
                </button>
              </th>
            }
          </tr>
        </thead>
        <tbody>
          @for (item of itemsOrdenados(); track item.id) {
            <tr (click)="seleccionar.emit(item.id)">
              @for (columna of columnas(); track columna) {
                @switch (columna) {
                  @case ('tipo') {
                    <td>
                      {{ item.tipo.nombre }}
                      @if (!item.tipo.activo) {
                        <span class="texto-secundario">(inactivo)</span>
                      }
                    </td>
                  }
                  @case ('numero') {
                    <td class="celda-nro">
                      <a
                        [routerLink]="['/proyectos', proyectoId(), 'incidencias', item.id]"
                        [title]="item.codigo"
                        (click)="$event.stopPropagation()"
                        >{{ item.numero }}</a
                      >
                      @if (item.vencida) {
                        <span class="marca-vencida" role="img" aria-label="Vencida" title="Vencida"
                          >⚠</span
                        >
                      }
                    </td>
                  }
                  @case ('titulo') {
                    <td class="celda-truncada" [title]="item.titulo">{{ item.titulo }}</td>
                  }
                  @case ('estado') {
                    <td><app-badge-estado-incidencia [estado]="item.estado" /></td>
                  }
                  @case ('estimacion') {
                    <td class="celda-numero">{{ estimacion(item.estimacion) }}</td>
                  }
                  @case ('responsable') {
                    <td>
                      @if (item.responsable; as responsable) {
                        {{ responsable.nombre }}
                        @if (!responsable.activo) {
                          <span class="texto-secundario">(inactivo)</span>
                        }
                      } @else {
                        <span class="texto-secundario">Sin responsable</span>
                      }
                    </td>
                  }
                }
              }
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class TablaTableroComponent {
  readonly proyectoId = input.required<number>();
  /** Ítems en el orden por defecto del servidor (RN-32). */
  readonly items = input.required<readonly ItemTableroDto[]>();
  /** Columnas visibles del proyecto (null = todas, RN-30). */
  readonly configuracion = input<ConfiguracionTableroDto | null>(null);
  /** Emite el id de la incidencia de la fila elegida (→ I3). */
  readonly seleccionar = output<number>();

  protected readonly titulos = TITULOS_COLUMNA_TABLERO;
  protected readonly estimacion = formatearEstimacion;

  readonly columnas = computed(() => columnasVisibles(this.configuracion()));

  /** Orden elegido; vuelve a null (predeterminado) cada vez que llegan otros ítems (RN-33). */
  private readonly orden = linkedSignal<readonly ItemTableroDto[], OrdenTablero | null>({
    source: this.items,
    computation: () => null,
  });

  /** Orden efectivo: si su columna quedó oculta, el predeterminado. */
  readonly ordenActual = computed(() => ordenVigente(this.orden(), this.columnas()));

  readonly itemsOrdenados = computed(() => ordenarTablero(this.items(), this.ordenActual()));

  /** Clic en un encabezado: asc, luego desc, alternando; otra columna empieza en asc. */
  ordenar(columna: ColumnaTablero): void {
    this.orden.set(alternarOrden(this.ordenActual(), columna));
  }

  /** Vuelve al orden predeterminado (Tipo + Nro). */
  restablecerOrden(): void {
    this.orden.set(null);
  }

  /** Valor de `aria-sort` de la columna. */
  ariaSort(columna: ColumnaTablero): 'ascending' | 'descending' | 'none' {
    const orden = this.ordenActual();
    if (orden === null || orden.columna !== columna) {
      return 'none';
    }
    return orden.direccion === 'asc' ? 'ascending' : 'descending';
  }

  /** Flecha visual del orden actual. */
  flecha(columna: ColumnaTablero): string {
    const orden = this.ordenActual();
    if (orden === null || orden.columna !== columna) {
      return '';
    }
    return orden.direccion === 'asc' ? '▲' : '▼';
  }
}
