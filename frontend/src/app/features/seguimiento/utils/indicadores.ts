import { formatearFecha } from '../../../shared/utils/fechas';
import {
  CategoriaEstadoIncidencia,
  EstadoIncidenciaDto,
  NOMBRES_CATEGORIA_ESTADO,
} from '../../configuracion-incidencias/models/estado-incidencia.model';
import { IndicadorTablero, IndicadoresDto } from '../models/seguimiento.model';

/**
 * Definiciones y tarjetas de los indicadores de seguimiento (RF-005 RN-09 a RN-14, RN-23).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */

/** Clave de una tarjeta: el total o uno de los indicadores filtrables del tablero. */
export type ClaveTarjetaIndicador = 'total' | IndicadorTablero;

/** Tarjeta de indicador a mostrar. */
export interface TarjetaIndicador {
  readonly clave: ClaveTarjetaIndicador;
  readonly titulo: string;
  readonly valor: number;
  /** Estilo de alerta (vencidas y bloqueadas con valor > 0). */
  readonly alerta: boolean;
  readonly definicion: string;
}

/** Nota común de la leyenda (RN-08). */
export const NOTA_SIN_ROLLUP =
  'Cada incidencia se cuenta por sí misma (las épicas no suman a sus hijas).';

/** Definición de cada indicador con la fecha de referencia (RN-23). */
export function definicionIndicador(clave: ClaveTarjetaIndicador, fechaReferencia: string): string {
  switch (clave) {
    case 'total':
      return 'Todas las incidencias del alcance, incluidas las finalizadas.';
    case 'abiertas':
      return 'Incidencias en un estado no final de su flujo.';
    case 'vencidas':
      return `Abiertas con vencimiento anterior al ${formatearFecha(fechaReferencia)}.`;
    case 'bloqueadas':
      return 'Abiertas en un estado de categoría Bloqueada.';
    case 'en-espera':
      return 'Abiertas en un estado de categoría En espera.';
    case 'sin-responsable':
      return 'Abiertas sin responsable asignado.';
  }
}

/** Tarjetas en el orden de la spec: Total, Abiertas, Vencidas, Bloqueadas, En espera, Sin resp. */
export function tarjetasIndicadores(
  indicadores: IndicadoresDto,
  fechaReferencia: string,
): TarjetaIndicador[] {
  const tarjeta = (
    clave: ClaveTarjetaIndicador,
    titulo: string,
    valor: number,
    alerta = false,
  ): TarjetaIndicador => ({
    clave,
    titulo,
    valor,
    alerta: alerta && valor > 0,
    definicion: definicionIndicador(clave, fechaReferencia),
  });
  return [
    tarjeta('total', 'Total', indicadores.total),
    tarjeta('abiertas', 'Abiertas', indicadores.abiertas),
    tarjeta('vencidas', 'Vencidas', indicadores.vencidas, true),
    tarjeta('bloqueadas', 'Bloqueadas', indicadores.bloqueadas, true),
    tarjeta('en-espera', 'En espera', indicadores.enEspera),
    tarjeta('sin-responsable', 'Sin responsable', indicadores.sinResponsable),
  ];
}

/** Nombres de los estados del catálogo con la categoría indicada (orden del catálogo). */
export function estadosConCategoria(
  estados: readonly EstadoIncidenciaDto[],
  categoria: CategoriaEstadoIncidencia,
): string[] {
  return [...estados]
    .filter((e) => e.categoria === categoria)
    .sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre, 'es'))
    .map((e) => (e.activo ? e.nombre : `${e.nombre} (inactivo)`));
}

/** Definición de un indicador por categoría con los estados que la tienen (leyenda, RN-23). */
export function definicionCategoria(
  categoria: CategoriaEstadoIncidencia,
  nombresEstados: readonly string[],
): string {
  const nombre = NOMBRES_CATEGORIA_ESTADO[categoria];
  return nombresEstados.length > 0
    ? `Abiertas en un estado de categoría ${nombre} (${nombresEstados.join(', ')}).`
    : `Abiertas en un estado de categoría ${nombre}.`;
}

/** Aviso cuando ningún estado tiene la categoría (CA-12). */
export function avisoSinEstadosCategoria(categoria: CategoriaEstadoIncidencia): string {
  return (
    `No hay estados configurados como ${NOMBRES_CATEGORIA_ESTADO[categoria]}; ` +
    'el indicador siempre será 0.'
  );
}

/** Porcentaje entero de una cantidad sobre el total (0 si el total es 0). */
export function porcentaje(cantidad: number, total: number): number {
  return total > 0 ? Math.round((cantidad * 100) / total) : 0;
}
