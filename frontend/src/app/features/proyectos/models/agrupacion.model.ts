/**
 * Modelos de Agrupaciones según el contrato API de RF-001.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */

/** `AgrupacionDto` devuelto por `/api/proyectos/{proyectoId}/agrupaciones`. */
export interface AgrupacionDto {
  readonly id: number;
  readonly proyectoId: number;
  readonly nombre: string;
  readonly descripcion: string;
  readonly fechaCreacion: string;
  readonly fechaModificacion: string | null;
}

/** Body de alta y edición de agrupación (`proyectoId` va en la URL, RN-28). */
export type AgrupacionRequest = Pick<AgrupacionDto, 'nombre' | 'descripcion'>;

/** Límites de validación de Agrupación (RN-26). */
export const LIMITES_AGRUPACION = {
  nombreMax: 100,
  descripcionMax: 1000,
} as const;
