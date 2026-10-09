import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PersonaDto } from '../../models/persona.model';

/**
 * Tabla de personas (P6): Nombre, Email, Admin, Activo y acciones Editar / Desactivar / Activar.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-personas-tabla',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (personas().length === 0) {
      <p class="vacio">No hay personas que coincidan con los filtros.</p>
    } @else {
      <table class="tabla">
        <caption class="sr-only">
          Personas
        </caption>
        <thead>
          <tr>
            <th scope="col">Nombre</th>
            <th scope="col">Email</th>
            <th scope="col">Admin</th>
            <th scope="col">Activo</th>
            <th scope="col"><span class="sr-only">Acciones</span></th>
          </tr>
        </thead>
        <tbody>
          @for (persona of personas(); track persona.id) {
            <tr [class.fila-inactiva]="!persona.activo">
              <td>{{ persona.nombre }}</td>
              <td>{{ persona.email }}</td>
              <td>{{ persona.esAdmin ? 'Sí' : 'No' }}</td>
              <td>{{ persona.activo ? 'Sí' : 'No' }}</td>
              <td class="acciones-fila">
                <a
                  class="btn btn-chico"
                  [routerLink]="['/personas', persona.id, 'editar']"
                  [attr.aria-label]="'Editar a ' + persona.nombre"
                >
                  Editar
                </a>
                @if (persona.activo) {
                  <button
                    type="button"
                    class="btn btn-chico btn-peligro"
                    [disabled]="deshabilitado()"
                    [attr.aria-label]="'Desactivar a ' + persona.nombre"
                    (click)="desactivar.emit(persona)"
                  >
                    Desactivar
                  </button>
                } @else {
                  <button
                    type="button"
                    class="btn btn-chico"
                    [disabled]="deshabilitado()"
                    [attr.aria-label]="'Activar a ' + persona.nombre"
                    (click)="activar.emit(persona)"
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
export class PersonasTablaComponent {
  readonly personas = input.required<readonly PersonaDto[]>();
  /** Deshabilita las acciones mientras hay una en curso. */
  readonly deshabilitado = input(false);
  readonly desactivar = output<PersonaDto>();
  readonly activar = output<PersonaDto>();
}
