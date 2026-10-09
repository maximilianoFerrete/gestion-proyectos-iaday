import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PrioridadIncidenciaDto } from '../../models/prioridad-incidencia.model';
import {
  puedeActivarPrioridad,
  puedeDesactivarPrioridad,
  puedeMarcarPredeterminada,
} from '../../utils/reglas-configuracion';

/**
 * Tabla de prioridades (C5). Acciones solo con permiso: "Marcar como predeterminada" en activas
 * no predeterminadas; Desactivar oculto en la predeterminada (RN-17).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-prioridades-tabla',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (prioridades().length === 0) {
      <p class="vacio">No hay prioridades configuradas.</p>
    } @else {
      <table class="tabla">
        <caption class="sr-only">
          Prioridades de incidencia
        </caption>
        <thead>
          <tr>
            <th scope="col">Orden</th>
            <th scope="col">Nombre</th>
            <th scope="col">Descripción</th>
            <th scope="col">Predeterminada</th>
            <th scope="col">Activo</th>
            <th scope="col">Incidencias</th>
            @if (puedeEditar()) {
              <th scope="col"><span class="sr-only">Acciones</span></th>
            }
          </tr>
        </thead>
        <tbody>
          @for (prioridad of prioridades(); track prioridad.id) {
            <tr [class.fila-inactiva]="!prioridad.activo">
              <td>{{ prioridad.orden }}</td>
              <td>{{ prioridad.nombre }}</td>
              <td>{{ prioridad.descripcion ?? '' }}</td>
              <td>{{ prioridad.esPredeterminada ? 'Sí' : '' }}</td>
              <td>{{ prioridad.activo ? 'Sí' : 'No' }}</td>
              <td>{{ prioridad.cantidadIncidencias }}</td>
              @if (puedeEditar()) {
                <td class="acciones-fila">
                  <a
                    class="btn btn-chico"
                    [routerLink]="[
                      '/configuracion/incidencias/prioridades',
                      prioridad.id,
                      'editar',
                    ]"
                    [attr.aria-label]="'Editar prioridad ' + prioridad.nombre"
                  >
                    Editar
                  </a>
                  @if (puedeMarcar(prioridad)) {
                    <button
                      type="button"
                      class="btn btn-chico"
                      [disabled]="deshabilitado()"
                      [attr.aria-label]="
                        'Marcar como predeterminada la prioridad ' + prioridad.nombre
                      "
                      (click)="marcarPredeterminada.emit(prioridad)"
                    >
                      Marcar como predeterminada
                    </button>
                  }
                  @if (puedeDesactivar(prioridad)) {
                    <button
                      type="button"
                      class="btn btn-chico btn-peligro"
                      [disabled]="deshabilitado()"
                      [attr.aria-label]="'Desactivar prioridad ' + prioridad.nombre"
                      (click)="desactivar.emit(prioridad)"
                    >
                      Desactivar
                    </button>
                  }
                  @if (puedeActivar(prioridad)) {
                    <button
                      type="button"
                      class="btn btn-chico"
                      [disabled]="deshabilitado()"
                      [attr.aria-label]="'Activar prioridad ' + prioridad.nombre"
                      (click)="activar.emit(prioridad)"
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
export class PrioridadesTablaComponent {
  readonly prioridades = input.required<readonly PrioridadIncidenciaDto[]>();
  readonly puedeEditar = input(false);
  readonly deshabilitado = input(false);
  readonly marcarPredeterminada = output<PrioridadIncidenciaDto>();
  readonly desactivar = output<PrioridadIncidenciaDto>();
  readonly activar = output<PrioridadIncidenciaDto>();

  protected readonly puedeMarcar = puedeMarcarPredeterminada;
  protected readonly puedeDesactivar = puedeDesactivarPrioridad;
  protected readonly puedeActivar = puedeActivarPrioridad;
}
