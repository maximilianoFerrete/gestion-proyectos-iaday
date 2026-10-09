import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { EstadoIncidenciaReferenciaDto } from '../../models/incidencia.model';

/**
 * Badge del estado de una incidencia: estilo diferenciado para estados finales (DA-09) y marca
 * "(inactivo)" para estados desactivados (RN-11).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-badge-estado-incidencia',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span [class]="clase()">{{ texto() }}</span>`,
})
export class BadgeEstadoIncidenciaComponent {
  readonly estado = input.required<EstadoIncidenciaReferenciaDto>();

  /** Nombre con la marca "(inactivo)" cuando corresponde. */
  readonly texto = computed(() => {
    const estado = this.estado();
    return estado.activo ? estado.nombre : `${estado.nombre} (inactivo)`;
  });

  /** Clase CSS según el estado. */
  readonly clase = computed(() => {
    const estado = this.estado();
    if (estado.esFinal) {
      return 'badge badge-final';
    }
    return estado.activo ? 'badge badge-normal' : 'badge badge-inactivo';
  });
}
