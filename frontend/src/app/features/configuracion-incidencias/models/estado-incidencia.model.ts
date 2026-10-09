/**
 * Modelos del catálogo de Estados de incidencia según el contrato API de RF-002.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

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
}

/** Body de `POST` / `PUT` de `/api/estados-incidencia`. */
export interface EstadoIncidenciaRequest {
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly orden: number;
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
