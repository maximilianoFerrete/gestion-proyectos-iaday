import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { FilaAgrupacionSeguimientoDto, IndicadoresDto } from '../../models/seguimiento.model';
import { queryParamsAmbito } from '../../utils/filtros-seguimiento';
import {
  COLUMNAS_INDICADORES,
  CampoIndicador,
} from '../tabla-proyectos-seguimiento/tabla-proyectos-seguimiento.component';

/**
 * Tabla de agrupaciones del Resumen en ámbito proyecto (RF-005 RN-26): Agrupación, Total,
 * Abiertas, Vencidas, Bloqueadas, En espera y Sin responsable; el nombre lleva a T2 con esa
 * agrupación (o "Sin agrupación") conservando el alcance.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-tabla-agrupaciones-seguimiento',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <table class="tabla">
      <caption class="sr-only">
        Indicadores por agrupación
      </caption>
      <thead>
        <tr>
          <th scope="col">Agrupación</th>
          @for (columna of columnas; track columna.campo) {
            <th scope="col" class="celda-numero">{{ columna.titulo }}</th>
          }
        </tr>
      </thead>
      <tbody>
        @for (fila of filas(); track fila.agrupacion?.id ?? 0) {
          <tr>
            <td>
              @if (fila.agrupacion; as agrupacion) {
                <a
                  [routerLink]="['/proyectos', proyectoId(), 'seguimiento']"
                  [queryParams]="queryParams(agrupacion.id, false)"
                  >{{ agrupacion.nombre }}</a
                >
              } @else {
                <a
                  [routerLink]="['/proyectos', proyectoId(), 'seguimiento']"
                  [queryParams]="queryParams(null, true)"
                  >Sin agrupación</a
                >
              }
            </td>
            @for (columna of columnas; track columna.campo) {
              <td
                class="celda-numero"
                [class.texto-alerta]="esAlerta(columna.campo, fila.indicadores)"
              >
                {{ fila.indicadores[columna.campo] }}
              </td>
            }
          </tr>
        }
      </tbody>
    </table>
  `,
})
export class TablaAgrupacionesSeguimientoComponent {
  readonly proyectoId = input.required<number>();
  readonly filas = input.required<readonly FilaAgrupacionSeguimientoDto[]>();
  /** Alcance vigente (se conserva en los enlaces). */
  readonly nivelIds = input<readonly number[]>([]);
  readonly tipoIds = input<readonly number[]>([]);

  protected readonly columnas = COLUMNAS_INDICADORES;

  /** Query params del enlace a T2 con el ámbito de la fila y el alcance vigente. */
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
}
