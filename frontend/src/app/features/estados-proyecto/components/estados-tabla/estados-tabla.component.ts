import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EstadoProyectoDto } from '../../models/estado-proyecto.model';
import {
  puedeActivar,
  puedeDesactivar,
  puedeEditar,
  puedeMarcarInicial,
} from '../../utils/acciones-estado';

/**
 * Tabla de estados de proyecto (P7) con acciones según las reglas RN-09 a RN-12.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-estados-tabla',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (estados().length === 0) {
      <p class="vacio">No hay estados configurados.</p>
    } @else {
      <table class="tabla">
        <caption class="sr-only">
          Estados de proyecto
        </caption>
        <thead>
          <tr>
            <th scope="col">Orden</th>
            <th scope="col">Nombre</th>
            <th scope="col">Descripción</th>
            <th scope="col">Inicial</th>
            <th scope="col">Final</th>
            <th scope="col">Activo</th>
            <th scope="col">Sistema</th>
            <th scope="col">Proyectos</th>
            <th scope="col"><span class="sr-only">Acciones</span></th>
          </tr>
        </thead>
        <tbody>
          @for (estado of estados(); track estado.id) {
            <tr [class.fila-inactiva]="!estado.activo">
              <td>{{ estado.orden }}</td>
              <td>{{ estado.nombre }}</td>
              <td>{{ estado.descripcion ?? '' }}</td>
              <td>{{ marca(estado.esInicial) }}</td>
              <td>{{ marca(estado.esFinal) }}</td>
              <td>{{ estado.activo ? 'Sí' : 'No' }}</td>
              <td>{{ marca(estado.esSistema) }}</td>
              <td>{{ estado.cantidadProyectos }}</td>
              <td class="acciones-fila">
                @if (puedeEditar(estado)) {
                  <a
                    class="btn btn-chico"
                    [routerLink]="['/configuracion/estados-proyecto', estado.id, 'editar']"
                    [attr.aria-label]="'Editar estado ' + estado.nombre"
                  >
                    Editar
                  </a>
                }
                @if (puedeMarcarInicial(estado)) {
                  <button
                    type="button"
                    class="btn btn-chico"
                    [disabled]="deshabilitado()"
                    [attr.aria-label]="'Marcar como inicial el estado ' + estado.nombre"
                    (click)="marcarInicial.emit(estado)"
                  >
                    Marcar como inicial
                  </button>
                }
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
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class EstadosTablaComponent {
  readonly estados = input.required<readonly EstadoProyectoDto[]>();
  readonly deshabilitado = input(false);
  readonly marcarInicial = output<EstadoProyectoDto>();
  readonly desactivar = output<EstadoProyectoDto>();
  readonly activar = output<EstadoProyectoDto>();

  protected readonly puedeEditar = puedeEditar;
  protected readonly puedeMarcarInicial = puedeMarcarInicial;
  protected readonly puedeDesactivar = puedeDesactivar;
  protected readonly puedeActivar = puedeActivar;

  /** Texto de una marca booleana ("Sí" o vacío). */
  marca(valor: boolean): string {
    return valor ? 'Sí' : '';
  }
}
