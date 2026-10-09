/**
 * Modelos de Relaciones permitidas entre tipos según el contrato API de RF-002. Las relaciones
 * solo se crean: no hay edición ni eliminación (RN-09).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Referencia a un tipo (`TipoReferenciaDto`), usada en relaciones e incidencias. */
export interface TipoReferenciaDto {
  readonly id: number;
  readonly nombre: string;
  readonly activo: boolean;
  readonly nivelNumero: number;
}

/** `RelacionTipoIncidenciaDto` devuelto por `/api/relaciones-tipos-incidencia`. */
export interface RelacionTipoIncidenciaDto {
  readonly id: number;
  readonly tipoPadre: TipoReferenciaDto;
  readonly tipoHijo: TipoReferenciaDto;
  readonly fechaCreacion: string;
}

/** Body de `POST /api/relaciones-tipos-incidencia`. */
export interface CrearRelacionTipoIncidenciaRequest {
  readonly tipoPadreId: number;
  readonly tipoHijoId: number;
}

/** Filtros de `GET /api/relaciones-tipos-incidencia`. */
export interface FiltrosRelacionesTipos {
  readonly tipoPadreId?: number;
  readonly tipoHijoId?: number;
}
