import { NodoArbolIncidenciaDto } from '../models/incidencia.model';
import {
  alternarId,
  construirArbol,
  idsConHijos,
  idsExpandidosHastaNivel,
  idsExpandidosIniciales,
  numeroDesdeCodigo,
} from './arbol-incidencias';

/** Nodo plano mínimo del árbol con el número deducido del código. */
function nodo(id: number, numero: number, padreId: number | null): NodoArbolIncidenciaDto {
  return {
    id,
    codigo: `PRJ-0001-${numero}`,
    titulo: `Incidencia ${numero}`,
    padreId,
    tipo: { id: 1, nombre: 'Épica', activo: true, nivelNumero: 1 },
    estado: { id: 1, nombre: 'Por hacer', activo: true, esFinal: false, categoria: null },
    prioridad: { id: 2, nombre: 'Media', orden: 20, activo: true },
    responsable: null,
    fechaVencimiento: null,
    vencida: false,
    agrupacion: null,
  };
}

/** Tests de las utilidades puras del árbol de incidencias (I2, CA-34). */
describe('arbol-incidencias', () => {
  // CA-34: raíces por número e hijos anidados por número aunque la lista venga desordenada
  it('construirArbol con nodos desordenados ordena raíces e hijos por número', () => {
    const arbol = construirArbol([
      nodo(17, 17, 3),
      nodo(10, 10, null),
      nodo(15, 15, 3),
      nodo(3, 3, null),
      nodo(16, 16, 15),
    ]);

    expect(arbol.map((n) => n.incidencia.id)).toEqual([3, 10]);
    expect(arbol[0].hijos.map((n) => n.incidencia.id)).toEqual([15, 17]);
    expect(arbol[0].hijos[0].hijos.map((n) => n.incidencia.id)).toEqual([16]);
    expect(arbol[1].hijos).toEqual([]);
  });

  // CA-34: el orden es numérico (2 antes que 10), no alfabético por código
  it('construirArbol ordena por número y no alfabéticamente', () => {
    const arbol = construirArbol([nodo(10, 10, null), nodo(2, 2, null)]);

    expect(arbol.map((n) => n.incidencia.codigo)).toEqual(['PRJ-0001-2', 'PRJ-0001-10']);
  });

  // Defensivo: padreId inexistente en la lista → raíz
  it('construirArbol trata como raíz un nodo cuyo padre no está en la lista', () => {
    const arbol = construirArbol([nodo(16, 16, 99), nodo(3, 3, null)]);

    expect(arbol.map((n) => n.incidencia.id)).toEqual([3, 16]);
  });

  // CA-34: proyecto sin incidencias
  it('construirArbol con lista vacía devuelve un árbol vacío', () => {
    expect(construirArbol([])).toEqual([]);
  });

  // Código → número
  it('numeroDesdeCodigo extrae el número final del código', () => {
    expect(numeroDesdeCodigo('PRJ-10000-15')).toBe(15);
    expect(numeroDesdeCodigo('sin-numero')).toBe(0);
  });

  // CA-34: "Expandir todo" usa todos los nodos con hijos
  it('idsConHijos devuelve los ids de todos los nodos con hijos en cualquier nivel', () => {
    const arbol = construirArbol([
      nodo(3, 3, null),
      nodo(15, 15, 3),
      nodo(16, 16, 15),
      nodo(20, 20, null),
    ]);

    expect([...idsConHijos(arbol)].sort((a, b) => a - b)).toEqual([3, 15]);
  });

  // I2: inicialmente se expanden las raíces con hijos
  it('idsExpandidosIniciales devuelve solo las raíces con hijos', () => {
    const arbol = construirArbol([
      nodo(3, 3, null),
      nodo(15, 15, 3),
      nodo(16, 16, 15),
      nodo(20, 20, null),
    ]);

    expect([...idsExpandidosIniciales(arbol)]).toEqual([3]);
  });

  // CA-34: expandir/colapsar un nodo
  it('alternarId agrega el id si no está y lo quita si está', () => {
    const expandido = alternarId(new Set([3]), 15);
    const colapsado = alternarId(expandido, 3);

    expect([...expandido].sort((a, b) => a - b)).toEqual([3, 15]);
    expect([...colapsado]).toEqual([15]);
  });

  describe('idsExpandidosHastaNivel (RF-005)', () => {
    /** Nodo con el nivel del tipo indicado. */
    function nodoNivel(id: number, padreId: number | null, nivel: number): NodoArbolIncidenciaDto {
      const base = nodo(id, id, padreId);
      return { ...base, tipo: { ...base.tipo, id: nivel, nivelNumero: nivel } };
    }

    /** Épica 3 (N1) → HU 15 (N2) → Subtarea 16 (N3); Épica 20 (N1) sin hijas. */
    const ARBOL = construirArbol([
      nodoNivel(3, null, 1),
      nodoNivel(15, 3, 2),
      nodoNivel(16, 15, 3),
      nodoNivel(20, null, 1),
    ]);

    // RN-22, CA-27: hasta nivel 2 expande los nodos con hijos de nivel < 2 (solo la Épica)
    it('con nivel 2 expande solo los nodos con hijos de nivel 1', () => {
      expect([...idsExpandidosHastaNivel(ARBOL, 2)]).toEqual([3]);
    });

    // RN-22, CA-27: hasta nivel 3 expande Épica y HU (se ve todo)
    it('con nivel 3 expande los nodos con hijos de nivel 1 y 2', () => {
      expect([...idsExpandidosHastaNivel(ARBOL, 3)].sort((a, b) => a - b)).toEqual([3, 15]);
    });

    // RN-22: con nivel 1 no se expande ninguno
    it('con nivel 1 devuelve un conjunto vacío', () => {
      expect(idsExpandidosHastaNivel(ARBOL, 1).size).toBe(0);
    });
  });
});
