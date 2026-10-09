import { Opcion } from '../../../core/models/opcion.model';
import { FlujoIncidenciaDto } from '../../configuracion-incidencias/models/flujo-incidencia.model';
import { TipoIncidenciaDto } from '../../configuracion-incidencias/models/tipo-incidencia.model';
import { EstadoIncidenciaReferenciaDto } from '../models/incidencia.model';
import { estadoInicialDelFlujo, opcionesEstadoFlujo } from './opciones-incidencia';

/**
 * Reglas puras del diálogo "Cambiar tipo" (I3, RN-23).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Tipos destino: activos y distintos del actual (RN-07). */
export function tiposDestinoCambio(
  tipos: readonly TipoIncidenciaDto[],
  tipoActualId: number,
): TipoIncidenciaDto[] {
  return tipos.filter((t) => t.activo && t.id !== tipoActualId);
}

/**
 * Estado preseleccionado al elegir el tipo destino (RN-23): el actual si está en el nuevo flujo;
 * si no, el inicial del nuevo flujo (null si no hay flujo).
 */
export function resolverEstadoAlCambiarTipo(
  estadoActualId: number,
  flujoNuevo: FlujoIncidenciaDto | null,
): number | null {
  if (!flujoNuevo) {
    return null;
  }
  if (flujoNuevo.estados.some((e) => e.estadoId === estadoActualId)) {
    return estadoActualId;
  }
  return estadoInicialDelFlujo(flujoNuevo)?.estadoId ?? null;
}

/**
 * Opciones de Estado del diálogo: estados activos del nuevo flujo y el actual si pertenece a ese
 * flujo aunque esté inactivo (RN-23 permite conservarlo).
 */
export function opcionesEstadoCambio(
  flujoNuevo: FlujoIncidenciaDto | null,
  estadoActual: EstadoIncidenciaReferenciaDto,
): Opcion<number>[] {
  const actualEnFlujo = flujoNuevo?.estados.some((e) => e.estadoId === estadoActual.id) ?? false;
  return opcionesEstadoFlujo(flujoNuevo, actualEnFlujo ? estadoActual : null);
}
