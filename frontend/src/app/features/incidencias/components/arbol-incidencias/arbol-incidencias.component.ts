import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NodoArbolIncidenciaDto } from '../../models/incidencia.model';
import { NodoArbol } from '../../utils/arbol-incidencias';
import { BadgeEstadoIncidenciaComponent } from '../badge-estado-incidencia/badge-estado-incidencia.component';

/** Marca de indicador de un nodo (vista Jerarquía, RF-005). */
export interface MarcaNodo {
  readonly clave: string;
  readonly icono: string;
  readonly titulo: string;
}

/** Marcas de indicadores del nodo: vencida, bloqueada, en espera, sin responsable (RF-005). */
export function marcasNodo(nodo: NodoArbolIncidenciaDto): MarcaNodo[] {
  const marcas: MarcaNodo[] = [];
  const abierta = !nodo.estado.esFinal;
  if (nodo.vencida) {
    marcas.push({ clave: 'vencida', icono: '⚠', titulo: 'Vencida' });
  }
  if (abierta && nodo.estado.categoria === 'BLOQUEADA') {
    marcas.push({ clave: 'bloqueada', icono: '\u{1F512}', titulo: 'Bloqueada' });
  }
  if (abierta && nodo.estado.categoria === 'EN_ESPERA') {
    marcas.push({ clave: 'en-espera', icono: '\u{1F552}', titulo: 'En espera' });
  }
  if (abierta && nodo.responsable === null) {
    marcas.push({ clave: 'sin-responsable', icono: '\u{1F464}', titulo: 'Sin responsable' });
  }
  return marcas;
}

/**
 * Árbol de incidencias (I2 y vista Jerarquía de T2, presentación): render recursivo con
 * `ng-template`. Cada nodo muestra botón expandir/colapsar (si tiene hijos), código (enlace),
 * título, tipo, estado, responsable y vencimiento (alerta según `vencida` de la API, RF-005
 * RN-36). Opcionalmente muestra marcas de indicadores y atenúa los nodos fuera del alcance.
 * Los ids expandidos los maneja la página.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Vencida del servidor, marcas y nodos fuera de alcance (RF-005)
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
          <div class="arbol-nodo" [class.arbol-nodo-atenuado]="fueraDeAlcance(inc)">
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
              <span [class.texto-alerta]="inc.vencida" [class.texto-secundario]="!inc.vencida">
                Vence {{ inc.fechaVencimiento | date: 'dd/MM/yyyy' }}
                @if (inc.vencida) {
                  <span class="sr-only">(vencida)</span>
                }
              </span>
            }
            @if (mostrarMarcas()) {
              @for (marca of marcas(inc); track marca.clave) {
                <span
                  [class]="'marca-indicador marca-' + marca.clave"
                  role="img"
                  [attr.aria-label]="marca.titulo"
                  [title]="marca.titulo"
                  >{{ marca.icono }}</span
                >
              }
            }
            @if (fueraDeAlcance(inc)) {
              <span class="sr-only">(fuera del alcance)</span>
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
  /** Tipos contabilizados por el alcance; los demás nodos se atenúan (null = todos, RF-005). */
  readonly tipoIdsEnAlcance = input<ReadonlySet<number> | null>(null);
  /** Muestra las marcas de indicadores en cada nodo (vista Jerarquía, RF-005). */
  readonly mostrarMarcas = input(false);
  /** Emite el id del nodo a expandir o colapsar. */
  readonly alternar = output<number>();

  protected readonly marcas = marcasNodo;

  /** Indica si el tipo del nodo no está en el alcance actual. */
  fueraDeAlcance(nodo: NodoArbolIncidenciaDto): boolean {
    const tipos = this.tipoIdsEnAlcance();
    return tipos !== null && !tipos.has(nodo.tipo.id);
  }
}
