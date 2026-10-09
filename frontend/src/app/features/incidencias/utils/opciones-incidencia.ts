import { Opcion } from '../../../core/models/opcion.model';
import { EstadoIncidenciaDto } from '../../configuracion-incidencias/models/estado-incidencia.model';
import {
  EstadoDeFlujoDto,
  FlujoIncidenciaDto,
} from '../../configuracion-incidencias/models/flujo-incidencia.model';
import { PrioridadIncidenciaDto } from '../../configuracion-incidencias/models/prioridad-incidencia.model';
import { RelacionTipoIncidenciaDto } from '../../configuracion-incidencias/models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../../configuracion-incidencias/models/tipo-incidencia.model';
import { PersonaDto, PersonaRefDto } from '../../personas/models/persona.model';
import { EstadoIncidenciaReferenciaDto, PrioridadReferenciaDto } from '../models/incidencia.model';

/**
 * Opciones de los selects de incidencias (I1, I4, I5 y diálogo "Cambiar tipo"). Funciones puras.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Sufijo para opciones inactivas. */
export const SUFIJO_INACTIVO = ' (inactivo)';

/** Etiqueta con la marca "(inactivo)" cuando corresponde. */
export function etiquetaConEstado(nombre: string, activo: boolean): string {
  return activo ? nombre : nombre + SUFIJO_INACTIVO;
}

/** Opciones de filtro: todos los elementos, inactivos marcados (I1). */
export function opcionesFiltro(
  elementos: readonly { readonly id: number; readonly nombre: string; readonly activo: boolean }[],
): Opcion<number>[] {
  return elementos.map((e) => ({ valor: e.id, etiqueta: etiquetaConEstado(e.nombre, e.activo) }));
}

/** Ids de los tipos hijos permitidos para un tipo padre según las relaciones (RN-25). */
export function idsTiposHijoPermitidos(
  relaciones: readonly RelacionTipoIncidenciaDto[],
  tipoPadreId: number,
): Set<number> {
  return new Set(
    relaciones.filter((r) => r.tipoPadre.id === tipoPadreId).map((r) => r.tipoHijo.id),
  );
}

/** Ids de los tipos que pueden ser padre de un tipo hijo según las relaciones (I5). */
export function idsTiposPadrePermitidos(
  relaciones: readonly RelacionTipoIncidenciaDto[],
  tipoHijoId: number,
): number[] {
  return [
    ...new Set(relaciones.filter((r) => r.tipoHijo.id === tipoHijoId).map((r) => r.tipoPadre.id)),
  ];
}

/**
 * Tipos elegibles en el alta (I4): activos; si hay padre, solo los hijos permitidos del tipo
 * del padre (RN-07, RN-25).
 */
export function tiposElegiblesAlta(
  tipos: readonly TipoIncidenciaDto[],
  relaciones: readonly RelacionTipoIncidenciaDto[],
  tipoPadreId: number | null,
): TipoIncidenciaDto[] {
  const activos = tipos.filter((t) => t.activo);
  if (tipoPadreId === null) {
    return activos;
  }
  const permitidos = idsTiposHijoPermitidos(relaciones, tipoPadreId);
  return activos.filter((t) => permitidos.has(t.id));
}

/** Opciones de Tipo para el alta, con el nivel como referencia. */
export function opcionesTipo(tipos: readonly TipoIncidenciaDto[]): Opcion<number>[] {
  return tipos.map((t) => ({ valor: t.id, etiqueta: `${t.nombre} (nivel ${t.nivel.numero})` }));
}

/** Estado inicial de un flujo o null si no lo tiene. */
export function estadoInicialDelFlujo(
  flujo: FlujoIncidenciaDto | null | undefined,
): EstadoDeFlujoDto | null {
  return flujo?.estados.find((e) => e.esInicial) ?? null;
}

/** Flujo del tipo indicado (por el `flujo.id` del tipo). */
export function flujoDelTipo(
  tipoId: number | null,
  tipos: readonly TipoIncidenciaDto[],
  flujos: readonly FlujoIncidenciaDto[],
): FlujoIncidenciaDto | null {
  const tipo = tipos.find((t) => t.id === tipoId);
  return tipo ? (flujos.find((f) => f.id === tipo.flujo.id) ?? null) : null;
}

/**
 * Opciones de Estado (I5, RN-11/RN-23): estados activos del flujo en el orden del flujo, más el
 * actual si está inactivo (marcado) o si no figura en el flujo.
 */
export function opcionesEstadoFlujo(
  flujo: FlujoIncidenciaDto | null,
  actual: EstadoIncidenciaReferenciaDto | null = null,
): Opcion<number>[] {
  const estados = [...(flujo?.estados ?? [])].sort((a, b) => a.orden - b.orden);
  const opciones: Opcion<number>[] = [];
  for (const estado of estados) {
    if (estado.activo || estado.estadoId === actual?.id) {
      opciones.push({
        valor: estado.estadoId,
        etiqueta: etiquetaConEstado(estado.nombre, estado.activo),
      });
    }
  }
  if (actual && !opciones.some((o) => o.valor === actual.id)) {
    opciones.push({ valor: actual.id, etiqueta: etiquetaConEstado(actual.nombre, actual.activo) });
  }
  return opciones;
}

/** Opciones de Prioridad (I4/I5, RN-21): activas por orden, más la actual si está inactiva. */
export function opcionesPrioridad(
  prioridades: readonly PrioridadIncidenciaDto[],
  actual: PrioridadReferenciaDto | null = null,
): Opcion<number>[] {
  const opciones = prioridades
    .filter((p) => p.activo)
    .sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre))
    .map((p) => ({ valor: p.id, etiqueta: p.nombre }));
  if (actual && !opciones.some((o) => o.valor === actual.id)) {
    opciones.push({ valor: actual.id, etiqueta: etiquetaConEstado(actual.nombre, actual.activo) });
  }
  return opciones;
}

/** Id de la prioridad predeterminada activa o null. */
export function prioridadPredeterminadaId(
  prioridades: readonly PrioridadIncidenciaDto[],
): number | null {
  return prioridades.find((p) => p.esPredeterminada && p.activo)?.id ?? null;
}

/** Opciones de Responsable (I4/I5, RN-22): personas activas y el actual aunque esté inactivo. */
export function opcionesResponsableIncidencia(
  personas: readonly PersonaDto[],
  actual: PersonaRefDto | null = null,
): Opcion<number>[] {
  const opciones = personas
    .filter((p) => p.activo)
    .map((p) => ({ valor: p.id, etiqueta: p.nombre }));
  if (actual && !opciones.some((o) => o.valor === actual.id)) {
    opciones.push({ valor: actual.id, etiqueta: etiquetaConEstado(actual.nombre, actual.activo) });
  }
  return opciones;
}

/** Opciones del filtro Estado (I1): todos los estados del catálogo por orden. */
export function opcionesFiltroEstado(estados: readonly EstadoIncidenciaDto[]): Opcion<number>[] {
  return opcionesFiltro(
    [...estados].sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre)),
  );
}
