import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { EstadoRefDto } from '../../../features/estados-proyecto/models/estado-proyecto.model';

/**
 * Badge del estado de un proyecto: estilo diferenciado para finales, para el estado de sistema
 * "Archivado" (con ícono de archivo) y marca "(inactivo)" para estados desactivados (RN-13).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-badge-estado',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="clase()">
      @if (estado().esSistema) {
        <span class="badge-icono" aria-hidden="true">&#128452;</span>
      }
      {{ texto() }}
    </span>
  `,
})
export class BadgeEstadoComponent {
  readonly estado = input.required<EstadoRefDto>();

  /** Nombre con la marca "(inactivo)" cuando corresponde. */
  readonly texto = computed(() => {
    const estado = this.estado();
    return estado.activo ? estado.nombre : `${estado.nombre} (inactivo)`;
  });

  /** Clase CSS según el tipo de estado. */
  readonly clase = computed(() => {
    const estado = this.estado();
    if (estado.esSistema) {
      return 'badge badge-archivado';
    }
    if (estado.esFinal) {
      return 'badge badge-final';
    }
    return estado.activo ? 'badge badge-normal' : 'badge badge-inactivo';
  });
}
