import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EstadoDeFlujoDto, FlujoIncidenciaDto } from '../../models/flujo-incidencia.model';

/** Estados del flujo ordenados por posición. */
export function estadosOrdenados(flujo: FlujoIncidenciaDto): EstadoDeFlujoDto[] {
  return [...flujo.estados].sort((a, b) => a.orden - b.orden);
}

/**
 * Tabla de flujos de trabajo (C4): nombre, estados en orden con marcas y tipos que lo usan.
 * Editar solo con permiso.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-flujos-tabla',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (flujos().length === 0) {
      <p class="vacio">No hay flujos configurados.</p>
    } @else {
      <table class="tabla">
        <caption class="sr-only">
          Flujos de trabajo
        </caption>
        <thead>
          <tr>
            <th scope="col">Nombre</th>
            <th scope="col">Estados</th>
            <th scope="col">Tipos que lo usan</th>
            @if (puedeEditar()) {
              <th scope="col"><span class="sr-only">Acciones</span></th>
            }
          </tr>
        </thead>
        <tbody>
          @for (flujo of flujos(); track flujo.id) {
            <tr>
              <td>
                {{ flujo.nombre }}
                @if (flujo.descripcion) {
                  <div class="texto-secundario">{{ flujo.descripcion }}</div>
                }
              </td>
              <td>
                @for (estado of ordenados(flujo); track estado.estadoId; let ultimo = $last) {
                  {{ estado.nombre }}{{ marcas(estado) }}{{ ultimo ? '' : ' → ' }}
                }
              </td>
              <td>
                @for (tipo of flujo.tipos; track tipo.id; let ultimo = $last) {
                  {{ tipo.nombre }}{{ tipo.activo ? '' : ' (inactivo)' }}{{ ultimo ? '' : ', ' }}
                } @empty {
                  <span class="texto-secundario">-</span>
                }
              </td>
              @if (puedeEditar()) {
                <td class="acciones-fila">
                  <a
                    class="btn btn-chico"
                    [routerLink]="['/configuracion/incidencias/flujos', flujo.id, 'editar']"
                    [attr.aria-label]="'Editar flujo ' + flujo.nombre"
                  >
                    Editar
                  </a>
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    }
  `,
})
export class FlujosTablaComponent {
  readonly flujos = input.required<readonly FlujoIncidenciaDto[]>();
  readonly puedeEditar = input(false);

  protected readonly ordenados = estadosOrdenados;

  /** Marcas del estado dentro del flujo. */
  marcas(estado: EstadoDeFlujoDto): string {
    const marcas = [
      estado.esInicial ? 'inicial' : null,
      estado.esFinal ? 'final' : null,
      estado.activo ? null : 'inactivo',
    ].filter((m): m is string => m !== null);
    return marcas.length > 0 ? ` (${marcas.join(', ')})` : '';
  }
}
