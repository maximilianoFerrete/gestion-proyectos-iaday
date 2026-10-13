import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CategoriaEstadoIncidencia } from '../../../configuracion-incidencias/models/estado-incidencia.model';

/** Datos mínimos del estado que pinta el badge (referencia de incidencia o de seguimiento). */
export interface EstadoBadgeIncidencia {
  readonly nombre: string;
  readonly activo: boolean;
  readonly esFinal: boolean;
  readonly categoria: CategoriaEstadoIncidencia | null;
}

/** Ícono y texto de cada categoría (candado = bloqueada, reloj = en espera; RF-005). */
const ICONOS_CATEGORIA: Readonly<
  Record<CategoriaEstadoIncidencia, { readonly icono: string; readonly titulo: string }>
> = {
  BLOQUEADA: { icono: '\u{1F512}', titulo: 'Categoría: Bloqueada' },
  EN_ESPERA: { icono: '\u{1F552}', titulo: 'Categoría: En espera' },
};

/**
 * Badge del estado de una incidencia: estilo diferenciado para estados finales (DA-09), marca
 * "(inactivo)" para estados desactivados (RN-11) e ícono y color según la categoría de
 * seguimiento (RF-005 CA-40).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Ícono y estilo por categoría de seguimiento (RF-005)
 */
@Component({
  selector: 'app-badge-estado-incidencia',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="clase()">
      @if (iconoCategoria(); as icono) {
        <span
          class="badge-icono"
          role="img"
          [attr.aria-label]="icono.titulo"
          [title]="icono.titulo"
          >{{ icono.icono }}</span
        >
      }
      {{ texto() }}
    </span>
  `,
})
export class BadgeEstadoIncidenciaComponent {
  readonly estado = input.required<EstadoBadgeIncidencia>();

  /** Nombre con la marca "(inactivo)" cuando corresponde. */
  readonly texto = computed(() => {
    const estado = this.estado();
    return estado.activo ? estado.nombre : `${estado.nombre} (inactivo)`;
  });

  /** Ícono de la categoría o null si el estado no tiene. */
  readonly iconoCategoria = computed(() => {
    const categoria = this.estado().categoria ?? null;
    return categoria === null ? null : ICONOS_CATEGORIA[categoria];
  });

  /** Clase CSS según el estado (final, categoría, activo). */
  readonly clase = computed(() => {
    const estado = this.estado();
    if (estado.esFinal) {
      return 'badge badge-final';
    }
    if (!estado.activo) {
      return 'badge badge-inactivo';
    }
    if (estado.categoria === 'BLOQUEADA') {
      return 'badge badge-bloqueada';
    }
    if (estado.categoria === 'EN_ESPERA') {
      return 'badge badge-en-espera';
    }
    return 'badge badge-normal';
  });
}
