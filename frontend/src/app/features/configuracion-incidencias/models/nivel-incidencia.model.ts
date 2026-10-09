/**
 * Modelos de Niveles de incidencia según el contrato API de RF-002.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** `NivelIncidenciaDto` devuelto por `/api/niveles-incidencia`. */
export interface NivelIncidenciaDto {
  readonly id: number;
  /** Nivel de referencia (1 = más alto). Inmutable. */
  readonly numero: number;
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly cantidadTipos: number;
  readonly fechaCreacion: string;
  readonly fechaModificacion: string | null;
}

/** Body de `POST /api/niveles-incidencia`. */
export interface CrearNivelIncidenciaRequest {
  readonly numero: number;
  readonly nombre: string;
  readonly descripcion: string | null;
}

/** Body de `PUT /api/niveles-incidencia/{id}` (`numero` no editable). */
export type EditarNivelIncidenciaRequest = Omit<CrearNivelIncidenciaRequest, 'numero'>;

/** Límites de validación de Nivel (RN-04). */
export const LIMITES_NIVEL = {
  numeroMin: 1,
  numeroMax: 99,
  nombreMax: 50,
  descripcionMax: 250,
} as const;
