import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EstadoIncidenciaDto } from '../../models/estado-incidencia.model';
import { puedeActivarEstado, puedeDesactivarEstado } from '../../utils/reglas-configuracion';

/**
 * Tabla del catálogo de estados de incidencia (C3). Acciones solo con permiso; Desactivar oculto
 * en estados iniciales de algún flujo (RN-11).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-estados-incidencia-tabla',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (estados().length === 0) {
      <p class="vacio">No hay estados configurados.</p>
    } @else {
      <table class="tabla">
        <caption class="sr-only">
          Estados de incidencia
        </caption>
        <thead>
          <tr>
            <th scope="col">Orden</th>
            <th scope="col">Nombre</th>
            <th scope="col">Descripción</th>
            <th scope="col">Activo</th>
            <th scope="col">Flujos</th>
            <th scope="col">Incidencias</th>
            @if (puedeEditar()) {
              <th scope="col"><span class="sr-only">Acciones</span></th>
            }
          </tr>
        </thead>
        <tbody>
          @for (estado of estados(); track estado.id) {
            <tr [class.fila-inactiva]="!estado.activo">
              <td>{{ estado.orden }}</td>
              <td>{{ estado.nombre }}</td>
              <td>{{ estado.descripcion ?? '' }}</td>
              <td>{{ estado.activo ? 'Sí' : 'No' }}</td>
              <td>
                @for (flujo of estado.flujos; track flujo.id; let ultimo = $last) {
                  {{ flujo.nombre }}{{ marcas(flujo.esInicial, flujo.esFinal)
                  }}{{ ultimo ? '' : ', ' }}
                } @empty {
                  <span class="texto-secundario">-</span>
                }
              </td>
              <td>{{ estado.cantidadIncidencias }}</td>
              @if (puedeEditar()) {
                <td class="acciones-fila">
                  <a
                    class="btn btn-chico"
                    [routerLink]="['/configuracion/incidencias/estados', estado.id, 'editar']"
                    [attr.aria-label]="'Editar estado ' + estado.nombre"
                  >
                    Editar
                  </a>
                  @if (puedeDesactivar(estado)) {
                    <button
                      type="button"
                      class="btn btn-chico btn-peligro"
                      [disabled]="deshabilitado()"
                      [attr.aria-label]="'Desactivar estado ' + estado.nombre"
                      (click)="desactivar.emit(estado)"
                    >
                      Desactivar
                    </button>
                  }
                  @if (puedeActivar(estado)) {
                    <button
                      type="button"
                      class="btn btn-chico"
                      [disabled]="deshabilitado()"
                      [attr.aria-label]="'Activar estado ' + estado.nombre"
                      (click)="activar.emit(estado)"
                    >
                      Activar
                    </button>
                  }
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class EstadosIncidenciaTablaComponent {
  readonly estados = input.required<readonly EstadoIncidenciaDto[]>();
  readonly puedeEditar = input(false);
  readonly deshabilitado = input(false);
  readonly desactivar = output<EstadoIncidenciaDto>();
  readonly activar = output<EstadoIncidenciaDto>();

  protected readonly puedeDesactivar = puedeDesactivarEstado;
  protected readonly puedeActivar = puedeActivarEstado;

  /** Marcas "(inicial)" / "(final)" del estado dentro de un flujo. */
  marcas(esInicial: boolean, esFinal: boolean): string {
    if (esInicial) {
      return ' (inicial)';
    }
    return esFinal ? ' (final)' : '';
  }
}
