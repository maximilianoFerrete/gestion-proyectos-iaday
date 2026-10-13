import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ConteoEstadoDto, EstadoSeguimientoDto } from '../../models/seguimiento.model';
import { porcentaje } from '../../utils/indicadores';

/** Segmento de la barra apilada. */
interface SegmentoBarra {
  readonly id: number;
  readonly clase: string;
  readonly ancho: number;
  readonly titulo: string;
}

/** Cantidad de colores alternativos para estados sin categoría ni finales. */
const COLORES_NORMALES = 4;

/** Clase de color del segmento según el estado (finales, categorías, resto alternado). */
export function claseSegmentoEstado(estado: EstadoSeguimientoDto, indice: number): string {
  if (estado.esFinal) {
    return 'segmento segmento-final';
  }
  if (estado.categoria === 'BLOQUEADA') {
    return 'segmento segmento-bloqueada';
  }
  if (estado.categoria === 'EN_ESPERA') {
    return 'segmento segmento-en-espera';
  }
  return `segmento segmento-normal-${indice % COLORES_NORMALES}`;
}

/**
 * Barra horizontal apilada con la cantidad de incidencias por estado (RF-005: conteo por estado
 * del Resumen y mini-barra de T1). Solo presentación; los estados con 0 no ocupan espacio.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-barra-estados',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="barra-estados"
      [class.barra-estados-mini]="mini()"
      role="img"
      [attr.aria-label]="descripcion()"
      [title]="descripcion()"
    >
      @for (segmento of segmentos(); track segmento.id) {
        <span
          [class]="segmento.clase"
          [style.width.%]="segmento.ancho"
          [title]="segmento.titulo"
        ></span>
      }
    </div>
  `,
})
export class BarraEstadosComponent {
  readonly porEstado = input.required<readonly ConteoEstadoDto[]>();
  /** Versión compacta (tabla de T1). */
  readonly mini = input(false);

  readonly total = computed(() => this.porEstado().reduce((suma, c) => suma + c.cantidad, 0));

  readonly segmentos = computed<SegmentoBarra[]>(() => {
    const total = this.total();
    return this.porEstado()
      .map((conteo, indice) => ({ conteo, indice }))
      .filter(({ conteo }) => conteo.cantidad > 0)
      .map(({ conteo, indice }) => ({
        id: conteo.estado.id,
        clase: claseSegmentoEstado(conteo.estado, indice),
        ancho: total > 0 ? (conteo.cantidad * 100) / total : 0,
        titulo: `${conteo.estado.nombre}: ${conteo.cantidad}`,
      }));
  });

  /** Texto accesible: "Por hacer: 13 (31 %) · ...". */
  readonly descripcion = computed(() => {
    const total = this.total();
    if (total === 0) {
      return 'Sin incidencias';
    }
    return this.porEstado()
      .filter((c) => c.cantidad > 0)
      .map((c) => `${c.estado.nombre}: ${c.cantidad} (${porcentaje(c.cantidad, total)} %)`)
      .join(' · ');
  });
}
