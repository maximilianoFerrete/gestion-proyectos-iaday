import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { BadgeEstadoComponent } from '../../../../shared/components/badge-estado/badge-estado.component';
import { ProyectoDto } from '../../models/proyecto.model';

/**
 * Datos de un proyecto (P3) y banner de solo lectura cuando está archivado.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Total de incidencias (RF-002)
 */
@Component({
  selector: 'app-proyecto-datos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, BadgeEstadoComponent],
  template: `
    @let p = proyecto();
    @if (p.archivado) {
      <div class="banner-archivado" role="status">
        <span aria-hidden="true">&#128452;</span>
        Proyecto archivado el {{ p.fechaArchivado | date: 'dd/MM/yyyy' }} por
        {{ p.archivadoPor?.nombre ?? '-' }}. Estado al archivar:
        {{ p.estadoAlArchivar?.nombre ?? '-' }}. Solo lectura.
      </div>
    }
    <dl class="datos">
      <div>
        <dt>Código</dt>
        <dd>{{ p.codigo }}</dd>
      </div>
      <div>
        <dt>Nombre</dt>
        <dd>{{ p.nombre }}</dd>
      </div>
      <div class="dato-ancho">
        <dt>Descripción</dt>
        <dd class="texto-multilinea">{{ p.descripcion }}</dd>
      </div>
      <div>
        <dt>Responsable</dt>
        <dd>
          {{ p.responsable.nombre }}
          @if (!p.responsable.activo) {
            <span class="texto-secundario">(inactivo)</span>
          }
        </dd>
      </div>
      <div>
        <dt>Estado</dt>
        <dd><app-badge-estado [estado]="p.estado" /></dd>
      </div>
      <div>
        <dt>Fecha de inicio</dt>
        <dd>{{ p.fechaInicio | date: 'dd/MM/yyyy' }}</dd>
      </div>
      <div>
        <dt>Fecha objetivo</dt>
        <dd>{{ p.fechaObjetivo | date: 'dd/MM/yyyy' }}</dd>
      </div>
      <div>
        <dt>Incidencias</dt>
        <dd>{{ p.cantidadIncidencias }}</dd>
      </div>
      <div>
        <dt>Fecha de creación</dt>
        <dd>{{ p.fechaCreacion | date: 'dd/MM/yyyy HH:mm' }}</dd>
      </div>
      <div>
        <dt>Última modificación</dt>
        <dd>{{ p.fechaModificacion ? (p.fechaModificacion | date: 'dd/MM/yyyy HH:mm') : '-' }}</dd>
      </div>
    </dl>
  `,
})
export class ProyectoDatosComponent {
  readonly proyecto = input.required<ProyectoDto>();
}
