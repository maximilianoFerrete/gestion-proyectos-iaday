/**
 * Modelos de Estados de proyecto según el contrato API de RF-001.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */

/** `EstadoProyectoDto` devuelto por `/api/estados-proyecto`. */
export interface EstadoProyectoDto {
  readonly id: number;
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly orden: number;
  readonly esInicial: boolean;
  readonly esFinal: boolean;
  readonly activo: boolean;
  /** `true` si `codigo != null` (estado de sistema, p. ej. "Archivado"). */
  readonly esSistema: boolean;
  readonly codigo: string | null;
  /** Cantidad de proyectos con ese estado (informativo). */
  readonly cantidadProyectos: number;
}

/** Body de `POST /api/estados-proyecto` y `PUT /api/estados-proyecto/{id}`. */
export interface EstadoProyectoRequest {
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly orden: number;
  readonly esFinal: boolean;
}

/** Filtros de `GET /api/estados-proyecto`. */
export interface FiltrosEstadosProyecto {
  readonly soloActivos?: boolean;
}

/** Referencia resumida a un estado embebida en los DTOs de proyecto. */
export type EstadoRefDto = Pick<
  EstadoProyectoDto,
  'id' | 'nombre' | 'esFinal' | 'activo' | 'esSistema'
>;

/** Límites de validación de Estado de proyecto (RN-07). */
export const LIMITES_ESTADO = {
  nombreMax: 50,
  descripcionMax: 250,
  ordenMin: 1,
  ordenMax: 9998,
  incrementoOrdenSugerido: 10,
} as const;
