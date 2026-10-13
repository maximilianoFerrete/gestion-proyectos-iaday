import { EstadoIncidenciaDto, LIMITES_ESTADO_INCIDENCIA } from '../models/estado-incidencia.model';
import { NivelIncidenciaDto } from '../models/nivel-incidencia.model';
import { PrioridadIncidenciaDto } from '../models/prioridad-incidencia.model';
import { RelacionTipoIncidenciaDto } from '../models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../models/tipo-incidencia.model';

/**
 * Reglas puras de las pantallas de configuración de incidencias (C1–C5): visibilidad de
 * acciones, elegibilidad en selects y textos de confirmación.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Regla de categoría: ni inicial ni final (RF-005 RN-18)
 */

/** Bloque de C1: un nivel con sus tipos. */
export interface BloqueNivel {
  readonly nivel: NivelIncidenciaDto;
  readonly tipos: readonly TipoIncidenciaDto[];
}

/** Agrupa los tipos por nivel (niveles por número, tipos por nombre). */
export function agruparTiposPorNivel(
  niveles: readonly NivelIncidenciaDto[],
  tipos: readonly TipoIncidenciaDto[],
): BloqueNivel[] {
  return [...niveles]
    .sort((a, b) => a.numero - b.numero)
    .map((nivel) => ({
      nivel,
      tipos: tipos
        .filter((t) => t.nivel.id === nivel.id)
        .sort((a, b) => a.nombre.localeCompare(b.nombre)),
    }));
}

/** Confirmación al desactivar un tipo (C1, RN-07). */
export function mensajeDesactivarTipo(tipo: TipoIncidenciaDto): string {
  return (
    `Las ${tipo.cantidadIncidencias} incidencias de este tipo lo conservan, pero no podrán ` +
    'crearse nuevas incidencias de este tipo.'
  );
}

/** Tipos elegibles como padre de una relación nueva: activos (RN-08). */
export function tiposPadreElegibles(tipos: readonly TipoIncidenciaDto[]): TipoIncidenciaDto[] {
  return tipos.filter((t) => t.activo);
}

/**
 * Tipos elegibles como hijo (C2, RN-08): activos, de nivel mayor que el del padre y sin
 * relación existente con ese padre.
 */
export function tiposHijoElegibles(
  tipos: readonly TipoIncidenciaDto[],
  relaciones: readonly RelacionTipoIncidenciaDto[],
  tipoPadreId: number | null,
): TipoIncidenciaDto[] {
  const padre = tipos.find((t) => t.id === tipoPadreId);
  if (!padre) {
    return [];
  }
  const existentes = new Set(
    relaciones.filter((r) => r.tipoPadre.id === padre.id).map((r) => r.tipoHijo.id),
  );
  return tipos.filter(
    (t) => t.activo && t.nivel.numero > padre.nivel.numero && !existentes.has(t.id),
  );
}

/** Filtro de la tabla de relaciones (C2). */
export interface FiltroRelaciones {
  readonly tipoPadreId: number | null;
  readonly tipoHijoId: number | null;
}

/** Aplica el filtro por tipo padre / tipo hijo. */
export function filtrarRelaciones(
  relaciones: readonly RelacionTipoIncidenciaDto[],
  filtro: FiltroRelaciones,
): RelacionTipoIncidenciaDto[] {
  return relaciones.filter(
    (r) =>
      (filtro.tipoPadreId === null || r.tipoPadre.id === filtro.tipoPadreId) &&
      (filtro.tipoHijoId === null || r.tipoHijo.id === filtro.tipoHijoId),
  );
}

/** Confirmación al crear una relación (C2, RN-09). */
export function mensajeCrearRelacion(padre: string, hijo: string): string {
  return `La relación ${padre} → ${hijo} no podrá modificarse ni eliminarse.`;
}

/** Indica si el estado es inicial en algún flujo (RN-11). */
export function esInicialEnAlgunFlujo(estado: EstadoIncidenciaDto): boolean {
  return estado.flujos.some((f) => f.esInicial);
}

/** Indica si el estado es inicial o final en algún flujo (no admite categoría, RF-005 RN-18). */
export function esInicialOFinalEnAlgunFlujo(estado: EstadoIncidenciaDto): boolean {
  return estado.flujos.some((f) => f.esInicial || f.esFinal);
}

/** Desactivar estado: activo y no inicial en ningún flujo (C3, RN-11). */
export function puedeDesactivarEstado(estado: EstadoIncidenciaDto): boolean {
  return estado.activo && !esInicialEnAlgunFlujo(estado);
}

/** Activar estado: inactivo. */
export function puedeActivarEstado(estado: EstadoIncidenciaDto): boolean {
  return !estado.activo;
}

/** Orden sugerido en el alta de estado (C3): mayor orden + 10, tope 9999. */
export function ordenSugeridoEstado(estados: readonly EstadoIncidenciaDto[]): number {
  const maximo = estados.length > 0 ? Math.max(...estados.map((e) => e.orden)) : 0;
  return Math.min(
    maximo + LIMITES_ESTADO_INCIDENCIA.incrementoOrdenSugerido,
    LIMITES_ESTADO_INCIDENCIA.ordenMax,
  );
}

/** Confirmación al desactivar un estado, con la cantidad de incidencias (C3). */
export function mensajeDesactivarEstado(estado: EstadoIncidenciaDto): string {
  if (estado.cantidadIncidencias > 0) {
    return (
      `Hay ${estado.cantidadIncidencias} incidencias en el estado ${estado.nombre}; lo ` +
      'conservarán, pero no podrá asignarse a otras.'
    );
  }
  return `¿Desactivar el estado ${estado.nombre}?`;
}

/** "Marcar como predeterminada": activas no predeterminadas (C5, RN-17). */
export function puedeMarcarPredeterminada(prioridad: PrioridadIncidenciaDto): boolean {
  return prioridad.activo && !prioridad.esPredeterminada;
}

/** Desactivar prioridad: activa y no predeterminada (C5, RN-17). */
export function puedeDesactivarPrioridad(prioridad: PrioridadIncidenciaDto): boolean {
  return prioridad.activo && !prioridad.esPredeterminada;
}

/** Activar prioridad: inactiva. */
export function puedeActivarPrioridad(prioridad: PrioridadIncidenciaDto): boolean {
  return !prioridad.activo;
}

/** Confirmación al desactivar una prioridad, con la cantidad de incidencias. */
export function mensajeDesactivarPrioridad(prioridad: PrioridadIncidenciaDto): string {
  if (prioridad.cantidadIncidencias > 0) {
    return (
      `Hay ${prioridad.cantidadIncidencias} incidencias con la prioridad ${prioridad.nombre}; ` +
      'la conservarán, pero no podrá asignarse a otras.'
    );
  }
  return `¿Desactivar la prioridad ${prioridad.nombre}?`;
}
