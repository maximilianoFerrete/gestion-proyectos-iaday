import { CrearIncidenciaRequest, EditarIncidenciaRequest } from '../models/incidencia.model';

/**
 * Armado de los bodies de alta y edición de incidencia a partir de los valores del formulario.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Valores emitidos por `IncidenciaFormComponent` (textos ya recortados). */
export interface ValoresIncidenciaForm {
  /** Solo en alta. */
  readonly tipoId: number | null;
  readonly titulo: string;
  readonly descripcion: string;
  /** Solo en edición. */
  readonly estadoId: number | null;
  readonly prioridadId: number | null;
  readonly responsableId: number | null;
  readonly fechaVencimiento: string | null;
  /** null si hay padre (se hereda, RN-28). */
  readonly agrupacionId: number | null;
  readonly padreId: number | null;
}

/** Body de `POST .../incidencias`; null si falta el tipo. */
export function aCrearIncidenciaRequest(
  valores: ValoresIncidenciaForm,
): CrearIncidenciaRequest | null {
  if (valores.tipoId === null) {
    return null;
  }
  return {
    tipoId: valores.tipoId,
    titulo: valores.titulo,
    descripcion: valores.descripcion,
    prioridadId: valores.prioridadId,
    responsableId: valores.responsableId,
    fechaVencimiento: valores.fechaVencimiento,
    agrupacionId: valores.padreId === null ? valores.agrupacionId : null,
    padreId: valores.padreId,
  };
}

/** Body de `PUT .../incidencias/{id}`; null si falta el estado o la prioridad. */
export function aEditarIncidenciaRequest(
  valores: ValoresIncidenciaForm,
): EditarIncidenciaRequest | null {
  if (valores.estadoId === null || valores.prioridadId === null) {
    return null;
  }
  return {
    titulo: valores.titulo,
    descripcion: valores.descripcion,
    estadoId: valores.estadoId,
    prioridadId: valores.prioridadId,
    responsableId: valores.responsableId,
    fechaVencimiento: valores.fechaVencimiento,
    agrupacionId: valores.padreId === null ? valores.agrupacionId : null,
    padreId: valores.padreId,
  };
}
