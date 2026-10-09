import { Opcion } from '../../../core/models/opcion.model';
import {
  EstadoProyectoDto,
  EstadoRefDto,
} from '../../estados-proyecto/models/estado-proyecto.model';
import { PersonaDto, PersonaRefDto } from '../../personas/models/persona.model';

/** Sufijo para opciones inactivas. */
export const SUFIJO_INACTIVO = ' (inactivo)';

/**
 * Opciones de Responsable (P2/P4, RN-17): personas activas y, si se indica, el responsable
 * actual aunque esté inactivo (marcado "(inactivo)").
 */
export function opcionesResponsable(
  personas: readonly PersonaDto[],
  actual: PersonaRefDto | null = null,
): Opcion<number>[] {
  const opciones = personas
    .filter((p) => p.activo)
    .map((p) => ({ valor: p.id, etiqueta: p.nombre }));
  if (actual && !opciones.some((o) => o.valor === actual.id)) {
    const etiqueta = actual.activo ? actual.nombre : actual.nombre + SUFIJO_INACTIVO;
    opciones.push({ valor: actual.id, etiqueta });
  }
  return opciones;
}

/**
 * Opciones de Estado en la edición (P4, RN-13/RN-19): estados activos no de sistema por orden,
 * más el estado actual si está inactivo (marcado "(inactivo)"). Nunca "Archivado".
 */
export function opcionesEstado(
  estados: readonly EstadoProyectoDto[],
  actual: EstadoRefDto | null = null,
): Opcion<number>[] {
  const opciones = estados
    .filter((e) => e.activo && !e.esSistema)
    .sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre))
    .map((e) => ({ valor: e.id, etiqueta: e.nombre }));
  if (actual && !actual.esSistema && !opciones.some((o) => o.valor === actual.id)) {
    const etiqueta = actual.activo ? actual.nombre : actual.nombre + SUFIJO_INACTIVO;
    opciones.push({ valor: actual.id, etiqueta });
  }
  return opciones;
}

/** Opciones del filtro Estado del listado (P1): todos, inactivos marcados, incluye "Archivado". */
export function opcionesFiltroEstado(estados: readonly EstadoProyectoDto[]): Opcion<number>[] {
  return estados.map((e) => ({
    valor: e.id,
    etiqueta: e.activo ? e.nombre : e.nombre + SUFIJO_INACTIVO,
  }));
}

/** Opciones del filtro Responsable del listado (P1): todas las personas, inactivas marcadas. */
export function opcionesFiltroResponsable(personas: readonly PersonaDto[]): Opcion<number>[] {
  return personas.map((p) => ({
    valor: p.id,
    etiqueta: p.activo ? p.nombre : p.nombre + SUFIJO_INACTIVO,
  }));
}
