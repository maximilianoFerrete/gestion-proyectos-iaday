import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IncidenciaReferenciaDto } from '../../models/incidencia.model';
import { BadgeEstadoIncidenciaComponent } from '../badge-estado-incidencia/badge-estado-incidencia.component';

/**
 * Sección Hijas del detalle de incidencia (I3): tabla con enlaces a cada hija directa.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencia-hijas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, BadgeEstadoIncidenciaComponent],
  template: `
    <section class="panel" aria-labelledby="titulo-hijas-incidencia">
      <h2 id="titulo-hijas-incidencia">Hijas</h2>
      @if (hijas().length === 0) {
        <p class="vacio">No tiene incidencias hijas.</p>
      } @else {
        <table class="tabla">
          <caption class="sr-only">
            Incidencias hijas
          </caption>
          <thead>
            <tr>
              <th scope="col">Código</th>
              <th scope="col">Título</th>
              <th scope="col">Tipo</th>
              <th scope="col">Estado</th>
              <th scope="col">Responsable</th>
              <th scope="col">Vencimiento</th>
            </tr>
          </thead>
          <tbody>
            @for (hija of hijas(); track hija.id) {
              <tr>
                <td>
                  <a [routerLink]="['/proyectos', proyectoId(), 'incidencias', hija.id]">
                    {{ hija.codigo }}
                  </a>
                </td>
                <td>{{ hija.titulo }}</td>
                <td>{{ hija.tipo.nombre }}</td>
                <td><app-badge-estado-incidencia [estado]="hija.estado" /></td>
                <td>{{ hija.responsable?.nombre ?? 'Sin responsable' }}</td>
                <td>
                  {{ hija.fechaVencimiento ? (hija.fechaVencimiento | date: 'dd/MM/yyyy') : '-' }}
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class IncidenciaHijasComponent {
  readonly proyectoId = input.required<number>();
  readonly hijas = input.required<readonly IncidenciaReferenciaDto[]>();
}
