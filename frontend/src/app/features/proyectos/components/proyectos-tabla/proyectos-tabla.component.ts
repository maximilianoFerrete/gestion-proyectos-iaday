import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BadgeEstadoComponent } from '../../../../shared/components/badge-estado/badge-estado.component';
import { DireccionOrden, OrdenProyectos, ProyectoResumenDto } from '../../models/proyecto.model';

/** Columna ordenable de la tabla. */
interface ColumnaProyectos {
  readonly clave: OrdenProyectos;
  readonly titulo: string;
}

/** Columnas en el orden de la spec (P1). */
const COLUMNAS: readonly ColumnaProyectos[] = [
  { clave: 'codigo', titulo: 'Código' },
  { clave: 'nombre', titulo: 'Nombre' },
  { clave: 'responsable', titulo: 'Responsable' },
  { clave: 'fechaInicio', titulo: 'Fecha inicio' },
  { clave: 'fechaObjetivo', titulo: 'Fecha objetivo' },
  { clave: 'estado', titulo: 'Estado' },
];

/**
 * Tabla del listado de proyectos (P1): encabezados ordenables y clic en fila → detalle.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-proyectos-tabla',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, BadgeEstadoComponent],
  template: `
    @if (proyectos().length === 0) {
      <p class="vacio">No hay proyectos que coincidan con los filtros.</p>
    } @else {
      <table class="tabla tabla-clickable">
        <caption class="sr-only">
          Proyectos
        </caption>
        <thead>
          <tr>
            @for (columna of columnas; track columna.clave) {
              <th scope="col" [attr.aria-sort]="ariaSort(columna.clave)">
                <button type="button" class="btn-orden" (click)="ordenar.emit(columna.clave)">
                  {{ columna.titulo }}
                  <span aria-hidden="true">{{ indicadorOrden(columna.clave) }}</span>
                </button>
              </th>
            }
          </tr>
        </thead>
        <tbody>
          @for (proyecto of proyectos(); track proyecto.id) {
            <tr [class.fila-archivada]="proyecto.archivado" (click)="seleccionar.emit(proyecto.id)">
              <td>
                <a [routerLink]="['/proyectos', proyecto.id]" (click)="$event.stopPropagation()">
                  {{ proyecto.codigo }}
                </a>
              </td>
              <td>{{ proyecto.nombre }}</td>
              <td>
                {{ proyecto.responsable.nombre }}
                @if (!proyecto.responsable.activo) {
                  <span class="texto-secundario">(inactivo)</span>
                }
              </td>
              <td>{{ proyecto.fechaInicio | date: 'dd/MM/yyyy' }}</td>
              <td>{{ proyecto.fechaObjetivo | date: 'dd/MM/yyyy' }}</td>
              <td><app-badge-estado [estado]="proyecto.estado" /></td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class ProyectosTablaComponent {
  readonly proyectos = input.required<readonly ProyectoResumenDto[]>();
  readonly ordenarPor = input.required<OrdenProyectos>();
  readonly direccion = input.required<DireccionOrden>();
  /** Emite la columna clickeada para ordenar. */
  readonly ordenar = output<OrdenProyectos>();
  /** Emite el id del proyecto seleccionado. */
  readonly seleccionar = output<number>();

  protected readonly columnas = COLUMNAS;

  /** Valor de `aria-sort` para la columna. */
  ariaSort(columna: OrdenProyectos): 'ascending' | 'descending' | 'none' {
    if (columna !== this.ordenarPor()) {
      return 'none';
    }
    return this.direccion() === 'asc' ? 'ascending' : 'descending';
  }

  /** Flecha visual del orden actual. */
  indicadorOrden(columna: OrdenProyectos): string {
    if (columna !== this.ordenarPor()) {
      return '';
    }
    return this.direccion() === 'asc' ? '▲' : '▼';
  }
}
