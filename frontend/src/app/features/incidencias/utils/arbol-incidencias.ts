import { NodoArbolIncidenciaDto } from '../models/incidencia.model';

/**
 * Utilidades puras para armar el árbol de incidencias (I2) a partir de la lista plana de
 * `GET .../incidencias/arbol`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - `idsExpandidosHastaNivel` (RF-005 RN-22)
 */

/** Nodo del árbol: la incidencia y sus hijas ordenadas por número. */
export interface NodoArbol {
  readonly incidencia: NodoArbolIncidenciaDto;
  readonly hijos: readonly NodoArbol[];
}

/** Número de la incidencia a partir del código (`PRJ-0001-15` → 15); 0 si no se puede leer. */
export function numeroDesdeCodigo(codigo: string): number {
  const coincidencia = /-(\d+)$/.exec(codigo);
  return coincidencia ? Number(coincidencia[1]) : 0;
}

/** Compara por número (el árbol no trae `numero`; se deduce del código) y desempata por id. */
function compararPorNumero(a: NodoArbolIncidenciaDto, b: NodoArbolIncidenciaDto): number {
  return numeroDesdeCodigo(a.codigo) - numeroDesdeCodigo(b.codigo) || a.id - b.id;
}

/**
 * Arma el árbol: raíces = `padreId` nulo; hijos ordenados por número. Un nodo cuyo padre no
 * está en la lista se trata como raíz (defensivo).
 */
export function construirArbol(nodos: readonly NodoArbolIncidenciaDto[]): NodoArbol[] {
  const ids = new Set(nodos.map((n) => n.id));
  const hijosPorPadre = new Map<number, NodoArbolIncidenciaDto[]>();
  const raices: NodoArbolIncidenciaDto[] = [];
  for (const nodo of nodos) {
    if (nodo.padreId === null || !ids.has(nodo.padreId) || nodo.padreId === nodo.id) {
      raices.push(nodo);
    } else {
      hijosPorPadre.set(nodo.padreId, [...(hijosPorPadre.get(nodo.padreId) ?? []), nodo]);
    }
  }
  const visitados = new Set<number>();
  const armar = (incidencia: NodoArbolIncidenciaDto): NodoArbol => {
    visitados.add(incidencia.id);
    const hijos = (hijosPorPadre.get(incidencia.id) ?? [])
      .filter((h) => !visitados.has(h.id))
      .sort(compararPorNumero)
      .map(armar);
    return { incidencia, hijos };
  };
  return [...raices].sort(compararPorNumero).map(armar);
}

/** Ids de todos los nodos que tienen hijos (para "Expandir todo"). */
export function idsConHijos(arbol: readonly NodoArbol[]): Set<number> {
  const ids = new Set<number>();
  const recorrer = (nodos: readonly NodoArbol[]): void => {
    for (const nodo of nodos) {
      if (nodo.hijos.length > 0) {
        ids.add(nodo.incidencia.id);
        recorrer(nodo.hijos);
      }
    }
  };
  recorrer(arbol);
  return ids;
}

/** Ids expandidos al abrir: las raíces con hijos (se ve el primer nivel con sus hijas). */
export function idsExpandidosIniciales(arbol: readonly NodoArbol[]): Set<number> {
  return new Set(arbol.filter((n) => n.hijos.length > 0).map((n) => n.incidencia.id));
}

/** Devuelve un nuevo conjunto con el id agregado o quitado. */
export function alternarId(ids: ReadonlySet<number>, id: number): Set<number> {
  const nuevo = new Set(ids);
  if (nuevo.has(id)) {
    nuevo.delete(id);
  } else {
    nuevo.add(id);
  }
  return nuevo;
}

/**
 * "Expandir hasta el nivel N" (RF-005 RN-22): ids de los nodos con hijos cuyo tipo tiene
 * `nivelNumero < N`; el resto queda colapsado. Con N = 1 no se expande ninguno.
 */
export function idsExpandidosHastaNivel(arbol: readonly NodoArbol[], nivel: number): Set<number> {
  const ids = new Set<number>();
  const recorrer = (nodos: readonly NodoArbol[]): void => {
    for (const nodo of nodos) {
      if (nodo.hijos.length > 0 && nodo.incidencia.tipo.nivelNumero < nivel) {
        ids.add(nodo.incidencia.id);
      }
      recorrer(nodo.hijos);
    }
  };
  recorrer(arbol);
  return ids;
}
