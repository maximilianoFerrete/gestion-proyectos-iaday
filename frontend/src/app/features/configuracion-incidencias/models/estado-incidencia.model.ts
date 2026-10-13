/**
 * Modelos del catálogo de Estados de incidencia según el contrato API de RF-002.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Categoría de seguimiento (RF-005 RN-17)
 */

/** Categoría de seguimiento de un estado (RF-005 DA-01); `null` = sin categoría. */
export type CategoriaEstadoIncidencia = 'BLOQUEADA' | 'EN_ESPERA';

/** Categorías admitidas por el contrato. */
export const CATEGORIAS_ESTADO_INCIDENCIA: readonly CategoriaEstadoIncidencia[] = [
  'BLOQUEADA',
  'EN_ESPERA',
];

/** Nombre visible de cada categoría. */
export const NOMBRES_CATEGORIA_ESTADO: Readonly<Record<CategoriaEstadoIncidencia, string>> = {
  BLOQUEADA: 'Bloqueada',
  EN_ESPERA: 'En espera',
};

/** Nombre visible de la categoría ("—" si no tiene). */
export function nombreCategoriaEstado(categoria: CategoriaEstadoIncidencia | null): string {
  return categoria === null ? '—' : NOMBRES_CATEGORIA_ESTADO[categoria];
}

/** Flujo en el que participa un estado, con sus marcas dentro de ese flujo. */
export interface EstadoEnFlujoDto {
  readonly id: number;
  readonly nombre: string;
  readonly esInicial: boolean;
  readonly esFinal: boolean;
}

/** `EstadoIncidenciaDto` devuelto por `/api/estados-incidencia`. */
export interface EstadoIncidenciaDto {
  readonly id: number;
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly orden: number;
  readonly activo: boolean;
  readonly cantidadIncidencias: number;
  readonly flujos: readonly EstadoEnFlujoDto[];
  /** Categoría de seguimiento (RF-005). */
  readonly categoria: CategoriaEstadoIncidencia | null;
}

/** Body de `POST` / `PUT` de `/api/estados-incidencia`. */
export interface EstadoIncidenciaRequest {
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly orden: number;
  /** Categoría de seguimiento (RF-005); `null` = ninguna. */
  readonly categoria: CategoriaEstadoIncidencia | null;
}

/** Filtros de `GET /api/estados-incidencia`. */
export interface FiltrosEstadosIncidencia {
  readonly soloActivos?: boolean;
}

/** Límites de validación de Estado de incidencia (RN-10). */
export const LIMITES_ESTADO_INCIDENCIA = {
  nombreMax: 50,
  descripcionMax: 250,
  ordenMin: 1,
  ordenMax: 9999,
  incrementoOrdenSugerido: 10,
} as const;
