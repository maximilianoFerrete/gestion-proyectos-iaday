/**
 * Modelos de Prioridades de incidencia según el contrato API de RF-002.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** `PrioridadIncidenciaDto` devuelto por `/api/prioridades-incidencia`. */
export interface PrioridadIncidenciaDto {
  readonly id: number;
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly orden: number;
  readonly esPredeterminada: boolean;
  readonly activo: boolean;
  readonly cantidadIncidencias: number;
}

/** Body de `POST` / `PUT` de `/api/prioridades-incidencia`. */
export interface PrioridadIncidenciaRequest {
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly orden: number;
}

/** Filtros de `GET /api/prioridades-incidencia`. */
export interface FiltrosPrioridadesIncidencia {
  readonly soloActivas?: boolean;
}

/** Límites de validación de Prioridad (RN-17). */
export const LIMITES_PRIORIDAD = {
  nombreMax: 50,
  descripcionMax: 250,
  ordenMin: 1,
  ordenMax: 9999,
} as const;
