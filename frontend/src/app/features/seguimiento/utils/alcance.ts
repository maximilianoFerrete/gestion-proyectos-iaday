import { NivelIncidenciaDto } from '../../configuracion-incidencias/models/nivel-incidencia.model';
import { TipoIncidenciaDto } from '../../configuracion-incidencias/models/tipo-incidencia.model';
import { AlcanceSeguimientoDto } from '../models/seguimiento.model';

/**
 * Utilidades puras del alcance de seguimiento (niveles y tipos, RF-005 RN-04 a RN-06, RN-23).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */

/** Leyenda cuando se contabilizan todos los niveles y tipos (CA-14). */
export const LEYENDA_TODO_EL_ALCANCE = 'Se contabilizan: todos los niveles y tipos de incidencia.';

/** Indica si el alcance abarca todos los niveles y tipos. */
export function esAlcanceCompleto(alcance: AlcanceSeguimientoDto): boolean {
  return alcance.todosLosNiveles && alcance.todosLosTipos;
}

/**
 * Leyenda del alcance (RN-23): "Se contabilizan: todos los niveles y tipos de incidencia." o
 * "Se contabilizan: Nivel 2 (Historia de Usuario) · Nivel 3 (Subtarea, Bug).".
 */
export function textoLeyendaAlcance(alcance: AlcanceSeguimientoDto): string {
  if (esAlcanceCompleto(alcance)) {
    return LEYENDA_TODO_EL_ALCANCE;
  }
  const numeros = [...new Set([...alcance.niveles.map((n) => n.numero)])].sort((a, b) => a - b);
  const partes = numeros
    .map((numero) => {
      const tipos = alcance.tipos.filter((t) => t.nivelNumero === numero).map((t) => t.nombre);
      return tipos.length > 0 ? `Nivel ${numero} (${tipos.join(', ')})` : null;
    })
    .filter((parte): parte is string => parte !== null);
  return partes.length > 0
    ? `Se contabilizan: ${partes.join(' · ')}.`
    : 'Se contabilizan: ningún tipo de incidencia.';
}

/** Lista de números como rango ("2–3") si son consecutivos o separados por coma ("1, 3"). */
function textoNumeros(numeros: readonly number[]): string {
  const consecutivos = numeros.every((n, i) => i === 0 || n === numeros[i - 1] + 1);
  return consecutivos && numeros.length > 1
    ? `${numeros[0]}–${numeros[numeros.length - 1]}`
    : numeros.join(', ');
}

/** Subtítulo corto del alcance para las tarjetas de indicadores ("Niveles 2–3"). */
export function textoSubtituloAlcance(alcance: AlcanceSeguimientoDto): string {
  const numeros = [...new Set(alcance.niveles.map((n) => n.numero))].sort((a, b) => a - b);
  const niveles = alcance.todosLosNiveles
    ? 'Todos los niveles'
    : `${numeros.length === 1 ? 'Nivel' : 'Niveles'} ${textoNumeros(numeros)}`;
  return alcance.todosLosTipos ? niveles : `${niveles} · tipos seleccionados`;
}

/** Ids de los tipos contabilizados (null = todos: no se atenúa nada en la jerarquía). */
export function tipoIdsEnAlcance(
  alcance: AlcanceSeguimientoDto | null,
): ReadonlySet<number> | null {
  if (alcance === null || esAlcanceCompleto(alcance)) {
    return null;
  }
  return new Set(alcance.tipos.map((t) => t.id));
}

/** Tipos que se pueden elegir según los niveles seleccionados (sin selección, todos). */
export function tiposDisponibles(
  tipos: readonly TipoIncidenciaDto[],
  nivelIdsSeleccionados: readonly number[],
): TipoIncidenciaDto[] {
  if (nivelIdsSeleccionados.length === 0) {
    return [...tipos];
  }
  const niveles = new Set(nivelIdsSeleccionados);
  return tipos.filter((t) => niveles.has(t.nivel.id));
}

/** Quita de la selección los tipos que quedaron fuera de los niveles elegidos (CA-16). */
export function depurarTiposSeleccionados(
  tipoIds: readonly number[],
  tipos: readonly TipoIncidenciaDto[],
  nivelIdsSeleccionados: readonly number[],
): number[] {
  const disponibles = new Set(tiposDisponibles(tipos, nivelIdsSeleccionados).map((t) => t.id));
  return tipoIds.filter((id) => disponibles.has(id));
}

/** Ids de los niveles que tienen al menos un tipo (los demás no se pueden elegir, RN-05). */
export function nivelIdsConTipos(
  niveles: readonly NivelIncidenciaDto[],
  tipos: readonly TipoIncidenciaDto[],
): Set<number> {
  const conTipos = new Set(tipos.map((t) => t.nivel.id));
  return new Set(niveles.filter((n) => conTipos.has(n.id)).map((n) => n.id));
}

/** Bloque de tipos de un nivel para el filtro de alcance. */
export interface GrupoTiposNivel {
  readonly nivel: NivelIncidenciaDto;
  readonly tipos: readonly TipoIncidenciaDto[];
}

/** Tipos disponibles agrupados por nivel (niveles por número, tipos por nombre). */
export function tiposAgrupadosPorNivel(
  niveles: readonly NivelIncidenciaDto[],
  tipos: readonly TipoIncidenciaDto[],
  nivelIdsSeleccionados: readonly number[],
): GrupoTiposNivel[] {
  const disponibles = tiposDisponibles(tipos, nivelIdsSeleccionados);
  return [...niveles]
    .sort((a, b) => a.numero - b.numero)
    .map((nivel) => ({
      nivel,
      tipos: disponibles
        .filter((t) => t.nivel.id === nivel.id)
        .sort((a, b) => a.nombre.localeCompare(b.nombre, 'es')),
    }))
    .filter((grupo) => grupo.tipos.length > 0);
}
