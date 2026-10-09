import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RelacionTipoIncidenciaDto } from '../../models/relacion-tipo-incidencia.model';

/**
 * Tabla de relaciones permitidas (C2). Sin acciones de editar ni eliminar (RN-09).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-relaciones-tabla',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    @if (relaciones().length === 0) {
      <p class="vacio">No hay relaciones que coincidan.</p>
    } @else {
      <table class="tabla">
        <caption class="sr-only">
          Relaciones permitidas entre tipos
        </caption>
        <thead>
          <tr>
            <th scope="col">Tipo padre (nivel)</th>
            <th scope="col">Tipo hijo (nivel)</th>
            <th scope="col">Fecha de creación</th>
          </tr>
        </thead>
        <tbody>
          @for (relacion of relaciones(); track relacion.id) {
            <tr>
              <td>
                {{ relacion.tipoPadre.nombre }} ({{ relacion.tipoPadre.nivelNumero }})
                @if (!relacion.tipoPadre.activo) {
                  <span class="texto-secundario">(inactivo)</span>
                }
              </td>
              <td>
                {{ relacion.tipoHijo.nombre }} ({{ relacion.tipoHijo.nivelNumero }})
                @if (!relacion.tipoHijo.activo) {
                  <span class="texto-secundario">(inactivo)</span>
                }
              </td>
              <td>{{ relacion.fechaCreacion | date: 'dd/MM/yyyy HH:mm' }}</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class RelacionesTablaComponent {
  readonly relaciones = input.required<readonly RelacionTipoIncidenciaDto[]>();
}
