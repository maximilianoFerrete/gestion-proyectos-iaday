import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { formatearEstimacion } from '../../../../shared/utils/estimacion';
import {
  DireccionOrdenIncidencias,
  IncidenciaResumenDto,
  OrdenIncidencias,
} from '../../models/incidencia.model';
import { BadgeEstadoIncidenciaComponent } from '../badge-estado-incidencia/badge-estado-incidencia.component';

/** Columna de la tabla; `orden` null = no ordenable (no está en la lista blanca). */
interface ColumnaIncidencias {
  readonly titulo: string;
  readonly orden: OrdenIncidencias | null;
}

/** Columnas en el orden de la spec (I1). */
const COLUMNAS: readonly ColumnaIncidencias[] = [
  { titulo: 'Código', orden: 'numero' },
  { titulo: 'Título', orden: 'titulo' },
  { titulo: 'Tipo', orden: 'tipo' },
  { titulo: 'Estado', orden: 'estado' },
  { titulo: 'Prioridad', orden: 'prioridad' },
  { titulo: 'Responsable', orden: 'responsable' },
  { titulo: 'Estimación', orden: 'estimacion' },
  { titulo: 'Vencimiento', orden: 'fechaVencimiento' },
  { titulo: 'Agrupación', orden: null },
  { titulo: 'Padre', orden: null },
];

/**
 * Tabla del listado de incidencias (I1): encabezados ordenables, estado con badge, vencidas con
 * estilo de alerta según `vencida` de la API (RF-005 RN-36), estimación y clic en fila → detalle.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Columna Estimación y vencida desde el servidor (RF-005)
 */
@Component({
  selector: 'app-incidencias-tabla',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, BadgeEstadoIncidenciaComponent],
  template: `
    @if (incidencias().length === 0) {
      <p class="vacio">No hay incidencias que coincidan con los filtros.</p>
    } @else {
      <table class="tabla tabla-clickable">
        <caption class="sr-only">
          Incidencias del proyecto
        </caption>
        <thead>
          <tr>
            @for (columna of columnas; track columna.titulo) {
              @if (columna.orden; as orden) {
                <th scope="col" [attr.aria-sort]="ariaSort(orden)">
                  <button type="button" class="btn-orden" (click)="ordenar.emit(orden)">
                    {{ columna.titulo }}
                    <span aria-hidden="true">{{ indicadorOrden(orden) }}</span>
                  </button>
                </th>
              } @else {
                <th scope="col">{{ columna.titulo }}</th>
              }
            }
          </tr>
        </thead>
        <tbody>
          @for (incidencia of incidencias(); track incidencia.id) {
            <tr (click)="seleccionar.emit(incidencia.id)">
              <td>
                <a
                  [routerLink]="['/proyectos', proyectoId(), 'incidencias', incidencia.id]"
                  (click)="$event.stopPropagation()"
                >
                  {{ incidencia.codigo }}
                </a>
              </td>
              <td>{{ incidencia.titulo }}</td>
              <td>
                {{ incidencia.tipo.nombre }}
                @if (!incidencia.tipo.activo) {
                  <span class="texto-secundario">(inactivo)</span>
                }
              </td>
              <td><app-badge-estado-incidencia [estado]="incidencia.estado" /></td>
              <td>{{ incidencia.prioridad.nombre }}</td>
              <td>
                @if (incidencia.responsable; as responsable) {
                  {{ responsable.nombre }}
                  @if (!responsable.activo) {
                    <span class="texto-secundario">(inactivo)</span>
                  }
                } @else {
                  <span class="texto-secundario">Sin responsable</span>
                }
              </td>
              <td class="celda-numero">{{ estimacion(incidencia.estimacion) }}</td>
              <td [class.texto-alerta]="incidencia.vencida">
                {{
                  incidencia.fechaVencimiento
                    ? (incidencia.fechaVencimiento | date: 'dd/MM/yyyy')
                    : '-'
                }}
                @if (incidencia.vencida) {
                  <span class="sr-only">(vencida)</span>
                }
              </td>
              <td>{{ incidencia.agrupacion?.nombre ?? '-' }}</td>
              <td>
                @if (incidencia.padre; as padre) {
                  <a
                    [routerLink]="['/proyectos', proyectoId(), 'incidencias', padre.id]"
                    (click)="$event.stopPropagation()"
                  >
                    {{ padre.codigo }}
                  </a>
                } @else {
                  -
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class IncidenciasTablaComponent {
  readonly proyectoId = input.required<number>();
  readonly incidencias = input.required<readonly IncidenciaResumenDto[]>();
  readonly ordenarPor = input.required<OrdenIncidencias>();
  readonly direccion = input.required<DireccionOrdenIncidencias>();
  /** Emite la columna clickeada para ordenar. */
  readonly ordenar = output<OrdenIncidencias>();
  /** Emite el id de la incidencia seleccionada. */
  readonly seleccionar = output<number>();

  protected readonly columnas = COLUMNAS;
  protected readonly estimacion = formatearEstimacion;

  /** Valor de `aria-sort` para la columna. */
  ariaSort(columna: OrdenIncidencias): 'ascending' | 'descending' | 'none' {
    if (columna !== this.ordenarPor()) {
      return 'none';
    }
    return this.direccion() === 'asc' ? 'ascending' : 'descending';
  }

  /** Flecha visual del orden actual. */
  indicadorOrden(columna: OrdenIncidencias): string {
    if (columna !== this.ordenarPor()) {
      return '';
    }
    return this.direccion() === 'asc' ? '▲' : '▼';
  }
}
