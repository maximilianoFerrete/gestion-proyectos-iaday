import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IncidenciaDto } from '../../models/incidencia.model';
import { BadgeEstadoIncidenciaComponent } from '../badge-estado-incidencia/badge-estado-incidencia.component';

/**
 * Datos de una incidencia (I3) y sección Padre: código, título, descripción, tipo (nivel),
 * estado, prioridad, responsable, vencimiento, agrupación (heredada) y fechas.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencia-datos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, BadgeEstadoIncidenciaComponent],
  template: `
    @let i = incidencia();
    <section class="panel" aria-labelledby="titulo-datos-incidencia">
      <h2 id="titulo-datos-incidencia" class="sr-only">Datos de la incidencia</h2>
      <dl class="datos">
        <div>
          <dt>Código</dt>
          <dd>{{ i.codigo }}</dd>
        </div>
        <div>
          <dt>Título</dt>
          <dd>{{ i.titulo }}</dd>
        </div>
        <div class="dato-ancho">
          <dt>Descripción</dt>
          <dd class="texto-multilinea">{{ i.descripcion }}</dd>
        </div>
        <div>
          <dt>Tipo</dt>
          <dd>
            {{ i.tipo.nombre }} (nivel {{ i.tipo.nivelNumero }})
            @if (!i.tipo.activo) {
              <span class="texto-secundario">(inactivo)</span>
            }
          </dd>
        </div>
        <div>
          <dt>Estado</dt>
          <dd><app-badge-estado-incidencia [estado]="i.estado" /></dd>
        </div>
        <div>
          <dt>Prioridad</dt>
          <dd>
            {{ i.prioridad.nombre }}
            @if (!i.prioridad.activo) {
              <span class="texto-secundario">(inactivo)</span>
            }
          </dd>
        </div>
        <div>
          <dt>Responsable</dt>
          <dd>
            @if (i.responsable; as responsable) {
              {{ responsable.nombre }}
              @if (!responsable.activo) {
                <span class="texto-secundario">(inactivo)</span>
              }
            } @else {
              Sin responsable
            }
          </dd>
        </div>
        <div>
          <dt>Vencimiento</dt>
          <dd>{{ i.fechaVencimiento ? (i.fechaVencimiento | date: 'dd/MM/yyyy') : '-' }}</dd>
        </div>
        <div>
          <dt>Agrupación</dt>
          <dd>
            {{ i.agrupacion?.nombre ?? 'Sin agrupación' }}
            @if (i.agrupacionHeredada && i.padre) {
              <span class="texto-secundario">(heredada de {{ i.padre.codigo }})</span>
            }
          </dd>
        </div>
        <div>
          <dt>Fecha de creación</dt>
          <dd>{{ i.fechaCreacion | date: 'dd/MM/yyyy HH:mm' }}</dd>
        </div>
        <div>
          <dt>Última modificación</dt>
          <dd>
            {{ i.fechaModificacion ? (i.fechaModificacion | date: 'dd/MM/yyyy HH:mm') : '-' }}
          </dd>
        </div>
      </dl>
    </section>

    <section class="panel" aria-labelledby="titulo-padre-incidencia">
      <h2 id="titulo-padre-incidencia">Padre</h2>
      @if (i.padre; as padre) {
        <p>
          <a [routerLink]="['/proyectos', i.proyecto.id, 'incidencias', padre.id]">
            {{ padre.codigo }} – {{ padre.titulo }}
          </a>
          <span class="texto-secundario"> · {{ padre.tipo.nombre }} · </span>
          <app-badge-estado-incidencia [estado]="padre.estado" />
        </p>
      } @else {
        <p class="vacio">Sin padre</p>
      }
    </section>
  `,
})
export class IncidenciaDatosComponent {
  readonly incidencia = input.required<IncidenciaDto>();
}
