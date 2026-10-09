/**
 * Modelos de Flujos de trabajo de incidencias según el contrato API de RF-002.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Estado incluido en un flujo (el `orden` es la posición, desde 1). */
export interface EstadoDeFlujoDto {
  readonly estadoId: number;
  readonly nombre: string;
  readonly activo: boolean;
  readonly orden: number;
  readonly esInicial: boolean;
  readonly esFinal: boolean;
}

/** Tipo que usa el flujo. */
export interface TipoDeFlujoDto {
  readonly id: number;
  readonly nombre: string;
  readonly activo: boolean;
}

/** `FlujoIncidenciaDto` devuelto por `/api/flujos-incidencia`. */
export interface FlujoIncidenciaDto {
  readonly id: number;
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly estados: readonly EstadoDeFlujoDto[];
  readonly tipos: readonly TipoDeFlujoDto[];
  readonly fechaCreacion: string;
  readonly fechaModificacion: string | null;
}

/** Estado dentro de `FlujoIncidenciaRequest` (el orden es la posición en el array). */
export type EstadoFlujoRequest = Pick<EstadoDeFlujoDto, 'estadoId' | 'esInicial' | 'esFinal'>;

/** Body de `POST` / `PUT` de `/api/flujos-incidencia` (el PUT reemplaza la composición). */
export interface FlujoIncidenciaRequest {
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly estados: readonly EstadoFlujoRequest[];
}

/** Límites de validación de Flujo (RN-13). */
export const LIMITES_FLUJO = {
  nombreMax: 100,
  descripcionMax: 250,
} as const;
