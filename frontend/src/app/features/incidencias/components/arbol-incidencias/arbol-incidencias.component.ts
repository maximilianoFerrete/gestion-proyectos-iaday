import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { fechaHoyIso } from '../../../../shared/utils/fechas';
import { NodoArbol } from '../../utils/arbol-incidencias';
import { estaVencida } from '../../utils/vencimiento';
import { BadgeEstadoIncidenciaComponent } from '../badge-estado-incidencia/badge-estado-incidencia.component';

/**
 * Árbol de incidencias (I2, presentación): render recursivo con `ng-template`. Cada nodo muestra
 * botón expandir/colapsar (si tiene hijos), código (enlace), título, tipo, estado, responsable y
 * vencimiento. Los ids expandidos los maneja la página.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-arbol-incidencias',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, NgTemplateOutlet, RouterLink, BadgeEstadoIncidenciaComponent],
  template: `
    <ng-template #rama let-nodos>
      @for (nodo of nodos; track nodo.incidencia.id) {
        @let inc = nodo.incidencia;
        <li>
          <div class="arbol-nodo">
            @if (nodo.hijos.length > 0) {
              <button
                type="button"
                class="btn btn-chico arbol-alternar"
                [attr.aria-expanded]="expandidos().has(inc.id)"
                [attr.aria-label]="
                  (expandidos().has(inc.id) ? 'Colapsar ' : 'Expandir ') + inc.codigo
                "
                (click)="alternar.emit(inc.id)"
              >
                {{ expandidos().has(inc.id) ? '−' : '+' }}
              </button>
            } @else {
              <span class="arbol-espacio" aria-hidden="true"></span>
            }
            <a [routerLink]="['/proyectos', proyectoId(), 'incidencias', inc.id]">{{
              inc.codigo
            }}</a>
            <span>{{ inc.titulo }}</span>
            <span class="texto-secundario">{{ inc.tipo.nombre }}</span>
            <app-badge-estado-incidencia [estado]="inc.estado" />
            <span class="texto-secundario">{{ inc.responsable?.nombre ?? 'Sin responsable' }}</span>
            @if (inc.fechaVencimiento) {
              <span
                [class.texto-alerta]="vencida(inc.fechaVencimiento, inc.estado.esFinal)"
                [class.texto-secundario]="!vencida(inc.fechaVencimiento, inc.estado.esFinal)"
              >
                Vence {{ inc.fechaVencimiento | date: 'dd/MM/yyyy' }}
              </span>
            }
          </div>
          @if (nodo.hijos.length > 0 && expandidos().has(inc.id)) {
            <ul>
              <ng-container *ngTemplateOutlet="rama; context: { $implicit: nodo.hijos }" />
            </ul>
          }
        </li>
      }
    </ng-template>

    <ul class="arbol" aria-label="Árbol de incidencias">
      <ng-container *ngTemplateOutlet="rama; context: { $implicit: arbol() }" />
    </ul>
  `,
})
export class ArbolIncidenciasComponent {
  readonly proyectoId = input.required<number>();
  readonly arbol = input.required<readonly NodoArbol[]>();
  /** Ids de los nodos expandidos. */
  readonly expandidos = input.required<ReadonlySet<number>>();
  /** Fecha de hoy `yyyy-MM-dd` (inyectable para tests). */
  readonly hoy = input<string>(fechaHoyIso());
  /** Emite el id del nodo a expandir o colapsar. */
  readonly alternar = output<number>();

  /** Vencida y estado no final (estilo de alerta). */
  vencida(fecha: string, esFinal: boolean): boolean {
    return estaVencida(fecha, esFinal, this.hoy());
  }
}
