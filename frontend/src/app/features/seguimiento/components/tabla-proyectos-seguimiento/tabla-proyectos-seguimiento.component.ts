import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { BadgeEstadoComponent } from '../../../../shared/components/badge-estado/badge-estado.component';
import {
  DireccionOrdenSeguimiento,
  FilaAgrupacionSeguimientoDto,
  FilaProyectoSeguimientoDto,
  IndicadoresDto,
  OrdenTableroProyectos,
} from '../../models/seguimiento.model';
import { queryParamsAmbito } from '../../utils/filtros-seguimiento';
import { BarraEstadosComponent } from '../barra-estados/barra-estados.component';

/** Estado de carga de las agrupaciones de un proyecto expandido. */
export type CargaAgrupaciones =
  | { readonly estado: 'cargando' }
  | { readonly estado: 'ok'; readonly filas: readonly FilaAgrupacionSeguimientoDto[] }
  | { readonly estado: 'error'; readonly mensaje: string };

/** Campo numérico de los indicadores mostrado como columna. */
export type CampoIndicador = Exclude<keyof IndicadoresDto, 'porEstado' | 'finalizadas'>;

/** Columna de indicador (todas ordenables por la lista blanca del contrato). */
export interface ColumnaIndicador {
  readonly titulo: string;
  readonly campo: CampoIndicador;
  readonly orden: OrdenTableroProyectos;
}

/** Columnas de indicadores en el orden de la spec (T1). */
export const COLUMNAS_INDICADORES: readonly ColumnaIndicador[] = [
  { titulo: 'Total', campo: 'total', orden: 'total' },
  { titulo: 'Abiertas', campo: 'abiertas', orden: 'abiertas' },
  { titulo: 'Vencidas', campo: 'vencidas', orden: 'vencidas' },
  { titulo: 'Bloqueadas', campo: 'bloqueadas', orden: 'bloqueadas' },
  { titulo: 'En espera', campo: 'enEspera', orden: 'enEspera' },
  { titulo: 'Sin responsable', campo: 'sinResponsable', orden: 'sinResponsable' },
];

/**
 * Tabla de proyectos del tablero general (T1, RF-005): proyecto (enlace a P3), estado, responsable,
 * fecha objetivo, agrupaciones, indicadores y mini-barra por estado. Encabezados ordenables
 * (`ordenarPor`). Acciones "Seguimiento" (→ T2) y expandir: la primera vez emite
 * `cargarAgrupaciones` y muestra las filas de agrupaciones con enlaces a T2 por ámbito (CA-06).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-tabla-proyectos-seguimiento',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, BadgeEstadoComponent, BarraEstadosComponent],
  template: `
    @if (filas().length === 0) {
      <p class="vacio">No hay proyectos que coincidan con los filtros.</p>
    } @else {
      <div class="tabla-desplazable">
        <table class="tabla tabla-seguimiento">
          <caption class="sr-only">
            Indicadores por proyecto
          </caption>
          <thead>
            <tr>
              <th scope="col" [attr.aria-sort]="ariaSortProyecto()">
                <button type="button" class="btn-orden" (click)="ordenar.emit('codigo')">
                  Proyecto <span aria-hidden="true">{{ flecha('codigo') }}</span>
                </button>
                <button
                  type="button"
                  class="btn-orden btn-orden-secundario"
                  (click)="ordenar.emit('nombre')"
                >
                  (nombre <span aria-hidden="true">{{ flecha('nombre') }}</span
                  >)
                </button>
              </th>
              <th scope="col">Estado</th>
              <th scope="col">Responsable</th>
              <th scope="col">Fecha objetivo</th>
              <th scope="col" class="celda-numero">Agrupaciones</th>
              @for (columna of columnas; track columna.campo) {
                <th scope="col" class="celda-numero" [attr.aria-sort]="ariaSort(columna.orden)">
                  <button type="button" class="btn-orden" (click)="ordenar.emit(columna.orden)">
                    {{ columna.titulo }}
                    <span aria-hidden="true">{{ flecha(columna.orden) }}</span>
                  </button>
                </th>
              }
              <th scope="col">Por estado</th>
              <th scope="col"><span class="sr-only">Acciones</span></th>
            </tr>
          </thead>
          <tbody>
            @for (fila of filas(); track fila.proyecto.id) {
              @let p = fila.proyecto;
              <tr [class.fila-archivada]="p.archivado">
                <td>
                  <a [routerLink]="['/proyectos', p.id]">{{ p.codigo }}</a>
                  <span> – {{ p.nombre }}</span>
                </td>
                <td><app-badge-estado [estado]="p.estado" /></td>
                <td>
                  {{ p.responsable.nombre }}
                  @if (!p.responsable.activo) {
                    <span class="texto-secundario">(inactivo)</span>
                  }
                </td>
                <td>{{ p.fechaObjetivo | date: 'dd/MM/yyyy' }}</td>
                <td class="celda-numero">{{ fila.cantidadAgrupaciones }}</td>
                @for (columna of columnas; track columna.campo) {
                  <td
                    class="celda-numero"
                    [class.texto-alerta]="esAlerta(columna.campo, fila.indicadores)"
                  >
                    {{ fila.indicadores[columna.campo] }}
                  </td>
                }
                <td class="celda-barra">
                  <app-barra-estados [porEstado]="fila.indicadores.porEstado" [mini]="true" />
                </td>
                <td class="acciones-fila">
                  <a
                    class="btn btn-chico"
                    [routerLink]="['/proyectos', p.id, 'seguimiento']"
                    [queryParams]="queryParams(null, false)"
                    [attr.aria-label]="'Seguimiento de ' + p.codigo"
                  >
                    Seguimiento
                  </a>
                  <button
                    type="button"
                    class="btn btn-chico"
                    [attr.aria-expanded]="expandidos().has(p.id)"
                    [attr.aria-label]="
                      (expandidos().has(p.id)
                        ? 'Ocultar agrupaciones de '
                        : 'Ver agrupaciones de ') + p.codigo
                    "
                    (click)="alternar(p.id)"
                  >
                    {{ expandidos().has(p.id) ? '▾' : '▸' }}
                  </button>
                </td>
              </tr>
              @if (expandidos().has(p.id)) {
                @if (filasAgrupaciones(p.id); as filasAgr) {
                  @for (filaAgr of filasAgr; track filaAgr.agrupacion?.id ?? 0) {
                    <tr class="fila-agrupacion">
                      <td colspan="5">
                        <span class="sangria" aria-hidden="true">↳</span>
                        @if (filaAgr.agrupacion; as agrupacion) {
                          <a
                            [routerLink]="['/proyectos', p.id, 'seguimiento']"
                            [queryParams]="queryParams(agrupacion.id, false)"
                            >{{ agrupacion.nombre }}</a
                          >
                        } @else {
                          <a
                            [routerLink]="['/proyectos', p.id, 'seguimiento']"
                            [queryParams]="queryParams(null, true)"
                            >Sin agrupación</a
                          >
                        }
                      </td>
                      @for (columna of columnas; track columna.campo) {
                        <td
                          class="celda-numero"
                          [class.texto-alerta]="esAlerta(columna.campo, filaAgr.indicadores)"
                        >
                          {{ filaAgr.indicadores[columna.campo] }}
                        </td>
                      }
                      <td class="celda-barra">
                        <app-barra-estados
                          [porEstado]="filaAgr.indicadores.porEstado"
                          [mini]="true"
                        />
                      </td>
                      <td></td>
                    </tr>
                  } @empty {
                    <tr class="fila-agrupacion">
                      <td [attr.colspan]="totalColumnas" class="vacio">Sin agrupaciones.</td>
                    </tr>
                  }
                } @else if (errorAgrupaciones(p.id); as mensaje) {
                  <tr class="fila-agrupacion">
                    <td [attr.colspan]="totalColumnas" class="texto-alerta" role="alert">
                      {{ mensaje }}
                    </td>
                  </tr>
                } @else {
                  <tr class="fila-agrupacion">
                    <td [attr.colspan]="totalColumnas" aria-live="polite">Cargando...</td>
                  </tr>
                }
              }
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class TablaProyectosSeguimientoComponent {
  readonly filas = input.required<readonly FilaProyectoSeguimientoDto[]>();
  readonly ordenarPor = input.required<OrdenTableroProyectos>();
  readonly direccion = input.required<DireccionOrdenSeguimiento>();
  /** Alcance vigente (se conserva en los enlaces a T2). */
  readonly nivelIds = input<readonly number[]>([]);
  readonly tipoIds = input<readonly number[]>([]);
  /** Agrupaciones cargadas por proyecto (la página las pide una sola vez). */
  readonly agrupaciones = input<ReadonlyMap<number, CargaAgrupaciones>>(new Map());
  /** Emite la columna clickeada para ordenar. */
  readonly ordenar = output<OrdenTableroProyectos>();
  /** Pide cargar el seguimiento del proyecto (solo si todavía no se pidió). */
  readonly cargarAgrupaciones = output<number>();

  protected readonly columnas = COLUMNAS_INDICADORES;
  /** Columnas de la tabla (para `colspan`). */
  protected readonly totalColumnas = 5 + COLUMNAS_INDICADORES.length + 2;

  /** Proyectos expandidos; se colapsan todos cuando llegan otras filas (otra consulta). */
  readonly expandidos = linkedSignal<readonly FilaProyectoSeguimientoDto[], ReadonlySet<number>>({
    source: this.filas,
    computation: () => new Set<number>(),
  });

  /** `aria-sort` de la columna Proyecto (código o nombre). */
  readonly ariaSortProyecto = computed(() => {
    const orden = this.ordenarPor();
    return orden === 'codigo' || orden === 'nombre' ? this.ariaSort(orden) : 'none';
  });

  /** Expande o colapsa un proyecto; al expandir por primera vez pide sus agrupaciones. */
  alternar(proyectoId: number): void {
    const nuevo = new Set(this.expandidos());
    if (nuevo.has(proyectoId)) {
      nuevo.delete(proyectoId);
    } else {
      nuevo.add(proyectoId);
      if (!this.agrupaciones().has(proyectoId)) {
        this.cargarAgrupaciones.emit(proyectoId);
      }
    }
    this.expandidos.set(nuevo);
  }

  /** Filas de agrupaciones cargadas del proyecto (null si están cargando o fallaron). */
  filasAgrupaciones(proyectoId: number): readonly FilaAgrupacionSeguimientoDto[] | null {
    const carga = this.agrupaciones().get(proyectoId);
    return carga?.estado === 'ok' ? carga.filas : null;
  }

  /** Mensaje de error de la carga de agrupaciones del proyecto (null si no falló). */
  errorAgrupaciones(proyectoId: number): string | null {
    const carga = this.agrupaciones().get(proyectoId);
    return carga?.estado === 'error' ? carga.mensaje : null;
  }

  /** Query params del enlace a T2 con el ámbito indicado y el alcance vigente. */
  queryParams(agrupacionId: number | null, sinAgrupacion: boolean): Params {
    return queryParamsAmbito(
      { nivelIds: this.nivelIds(), tipoIds: this.tipoIds() },
      agrupacionId,
      sinAgrupacion,
    );
  }

  /** Vencidas y bloqueadas > 0 con estilo de alerta. */
  esAlerta(campo: CampoIndicador, indicadores: IndicadoresDto): boolean {
    return (campo === 'vencidas' || campo === 'bloqueadas') && indicadores[campo] > 0;
  }

  /** Valor de `aria-sort` para la columna. */
  ariaSort(columna: OrdenTableroProyectos): 'ascending' | 'descending' | 'none' {
    if (columna !== this.ordenarPor()) {
      return 'none';
    }
    return this.direccion() === 'asc' ? 'ascending' : 'descending';
  }

  /** Flecha visual del orden actual. */
  flecha(columna: OrdenTableroProyectos): string {
    if (columna !== this.ordenarPor()) {
      return '';
    }
    return this.direccion() === 'asc' ? '▲' : '▼';
  }
}
