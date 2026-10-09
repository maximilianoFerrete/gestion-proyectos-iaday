import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AgrupacionDto } from '../../models/agrupacion.model';

/** Largo máximo de la descripción mostrada en la tabla antes de truncar. */
export const LARGO_DESCRIPCION_TRUNCADA = 80;

/** Trunca un texto agregando "..." si supera el largo indicado. */
export function truncar(texto: string, largo = LARGO_DESCRIPCION_TRUNCADA): string {
  return texto.length > largo ? `${texto.slice(0, largo).trimEnd()}...` : texto;
}

/**
 * Sección Agrupaciones del detalle de proyecto (P3). En solo lectura (proyecto archivado)
 * oculta Nueva / Editar / Eliminar.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-agrupaciones-lista',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <section class="panel" aria-labelledby="titulo-agrupaciones">
      <header class="encabezado-seccion">
        <h2 id="titulo-agrupaciones">Agrupaciones</h2>
        @if (!soloLectura()) {
          <a
            class="btn btn-primario"
            [routerLink]="['/proyectos', proyectoId(), 'agrupaciones', 'nueva']"
          >
            Nueva agrupación
          </a>
        }
      </header>
      @if (agrupaciones().length === 0) {
        <p class="vacio">El proyecto no tiene agrupaciones.</p>
      } @else {
        <table class="tabla">
          <caption class="sr-only">
            Agrupaciones del proyecto
          </caption>
          <thead>
            <tr>
              <th scope="col">Nombre</th>
              <th scope="col">Descripción</th>
              @if (!soloLectura()) {
                <th scope="col"><span class="sr-only">Acciones</span></th>
              }
            </tr>
          </thead>
          <tbody>
            @for (agrupacion of agrupaciones(); track agrupacion.id) {
              <tr>
                <td>{{ agrupacion.nombre }}</td>
                <td [title]="agrupacion.descripcion">{{ truncar(agrupacion.descripcion) }}</td>
                @if (!soloLectura()) {
                  <td class="acciones-fila">
                    <a
                      class="btn btn-chico"
                      [routerLink]="[
                        '/proyectos',
                        proyectoId(),
                        'agrupaciones',
                        agrupacion.id,
                        'editar',
                      ]"
                      [attr.aria-label]="'Editar agrupación ' + agrupacion.nombre"
                    >
                      Editar
                    </a>
                    <button
                      type="button"
                      class="btn btn-chico btn-peligro"
                      [attr.aria-label]="'Eliminar agrupación ' + agrupacion.nombre"
                      (click)="eliminar.emit(agrupacion)"
                    >
                      Eliminar
                    </button>
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class AgrupacionesListaComponent {
  readonly proyectoId = input.required<number>();
  readonly agrupaciones = input.required<readonly AgrupacionDto[]>();
  readonly soloLectura = input(false);
  /** Solicita eliminar la agrupación (la página pide confirmación). */
  readonly eliminar = output<AgrupacionDto>();

  protected readonly truncar = truncar;
}
