import {
  AgrupacionReferenciaDto,
  IncidenciaDto,
  IncidenciaResumenDto,
  TipoReferenciaDto,
} from '../models/incidencia.model';

/**
 * Padre elegido en el formulario de incidencia (I4/I5) y conversiones desde los DTOs.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Datos del padre que necesita el formulario (tipo, vencimiento y agrupación a heredar). */
export interface PadreSeleccionado {
  readonly id: number;
  readonly codigo: string;
  readonly titulo: string;
  readonly tipo: TipoReferenciaDto;
  readonly fechaVencimiento: string | null;
  readonly agrupacion: AgrupacionReferenciaDto | null;
}

/** Padre desde un resultado de búsqueda del listado. */
export function padreDesdeResumen(incidencia: IncidenciaResumenDto): PadreSeleccionado {
  return {
    id: incidencia.id,
    codigo: incidencia.codigo,
    titulo: incidencia.titulo,
    tipo: incidencia.tipo,
    fechaVencimiento: incidencia.fechaVencimiento,
    agrupacion: incidencia.agrupacion,
  };
}

/** Padre desde el detalle completo (alta con `?padreId=`). */
export function padreDesdeIncidencia(incidencia: IncidenciaDto): PadreSeleccionado {
  return {
    id: incidencia.id,
    codigo: incidencia.codigo,
    titulo: incidencia.titulo,
    tipo: incidencia.tipo,
    fechaVencimiento: incidencia.fechaVencimiento,
    agrupacion: incidencia.agrupacion,
  };
}

/**
 * Padre actual de una incidencia en edición. `IncidenciaReferenciaDto` no trae la agrupación,
 * pero por RN-28 es la misma que la de la hija.
 */
export function padreActualDe(incidencia: IncidenciaDto): PadreSeleccionado | null {
  const padre = incidencia.padre;
  if (!padre) {
    return null;
  }
  return {
    id: padre.id,
    codigo: padre.codigo,
    titulo: padre.titulo,
    tipo: padre.tipo,
    fechaVencimiento: padre.fechaVencimiento,
    agrupacion: incidencia.agrupacion,
  };
}

/** Candidatos a padre: excluye la propia incidencia (I5; los ciclos los valida la API). */
export function candidatosPadre(
  resultados: readonly IncidenciaResumenDto[],
  incidenciaId: number | null,
): IncidenciaResumenDto[] {
  return resultados.filter((r) => r.id !== incidenciaId);
}
