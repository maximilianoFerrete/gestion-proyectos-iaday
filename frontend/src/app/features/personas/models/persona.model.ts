/**
 * Modelos de Personas según el contrato API de RF-001.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */

/** `PersonaDto` devuelto por `/api/personas`. */
export interface PersonaDto {
  readonly id: number;
  readonly nombre: string;
  readonly email: string;
  readonly esAdmin: boolean;
  readonly activo: boolean;
  /** UTC ISO-8601. */
  readonly fechaCreacion: string;
  readonly fechaModificacion: string | null;
}

/** Body de `POST /api/personas` y `PUT /api/personas/{id}`. */
export interface PersonaRequest {
  readonly nombre: string;
  readonly email: string;
  readonly esAdmin: boolean;
}

/** Filtros de `GET /api/personas`. */
export interface FiltrosPersonas {
  readonly texto?: string;
  readonly incluirInactivas?: boolean;
}

/** Referencia resumida a una persona embebida en otros DTOs (responsable, archivadoPor). */
export type PersonaRefDto = Pick<PersonaDto, 'id' | 'nombre' | 'activo'>;

/** Límites de validación de Persona (RN-01, RN-02). */
export const LIMITES_PERSONA = {
  nombreMax: 150,
  emailMax: 254,
} as const;
