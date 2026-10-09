/**
 * Modelos de Tipos de incidencia según el contrato API de RF-002.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Referencia al nivel embebida en `TipoIncidenciaDto`. */
export interface NivelReferenciaDto {
  readonly id: number;
  readonly numero: number;
  readonly nombre: string;
}

/** Referencia al flujo embebida en `TipoIncidenciaDto`. */
export interface FlujoReferenciaDto {
  readonly id: number;
  readonly nombre: string;
}

/** `TipoIncidenciaDto` devuelto por `/api/tipos-incidencia`. */
export interface TipoIncidenciaDto {
  readonly id: number;
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly nivel: NivelReferenciaDto;
  readonly flujo: FlujoReferenciaDto;
  readonly activo: boolean;
  readonly cantidadIncidencias: number;
  readonly fechaCreacion: string;
  readonly fechaModificacion: string | null;
}

/** Body de `POST /api/tipos-incidencia`. */
export interface CrearTipoIncidenciaRequest {
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly nivelId: number;
  readonly flujoId: number;
}

/** Body de `PUT /api/tipos-incidencia/{id}` (`nivelId` no editable, RN-06). */
export type EditarTipoIncidenciaRequest = Omit<CrearTipoIncidenciaRequest, 'nivelId'>;

/** Filtros de `GET /api/tipos-incidencia`. */
export interface FiltrosTiposIncidencia {
  readonly soloActivos?: boolean;
  readonly nivelId?: number;
}

/** Límites de validación de Tipo (RN-05). */
export const LIMITES_TIPO = {
  nombreMax: 50,
  descripcionMax: 250,
} as const;
