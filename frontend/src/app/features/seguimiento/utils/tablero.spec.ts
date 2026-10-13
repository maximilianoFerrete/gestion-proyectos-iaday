import { ItemTableroDto } from '../models/seguimiento.model';
import { alternarOrden, columnasVisibles, ordenVigente, ordenarTablero } from './tablero';

/** Ítem del tablero con los datos que usan los criterios de orden. */
function item(
  numero: number,
  tipo: { nombre: string; nivel: number },
  extra: Partial<ItemTableroDto> = {},
): ItemTableroDto {
  return {
    id: numero,
    codigo: `PRJ-0001-${numero}`,
    numero,
    titulo: `Incidencia ${numero}`,
    tipo: { id: tipo.nivel, nombre: tipo.nombre, activo: true, nivelNumero: tipo.nivel },
    estado: {
      id: 1,
      nombre: 'Por hacer',
      orden: 10,
      activo: true,
      esFinal: false,
      categoria: null,
    },
    estimacion: null,
    vencida: false,
    responsable: null,
    ...extra,
  };
}

const EPICA = { nombre: 'Épica', nivel: 1 };
const HU = { nombre: 'Historia de Usuario', nivel: 2 };
const SUBTAREA = { nombre: 'Subtarea', nivel: 3 };
const BUG = { nombre: 'Bug', nivel: 3 };

/** Números de los ítems en el orden recibido. */
function numeros(items: readonly ItemTableroDto[]): number[] {
  return items.map((i) => i.numero);
}

/** Tests de las utilidades de la grilla del tablero (RF-005 RN-29, RN-32, RN-33). */
describe('utils/tablero', () => {
  describe('ordenarTablero', () => {
    // RN-32, CA-29: Épica #3, HU #7, HU #15, Subtarea #16
    it('sin orden aplica Tipo (nivel, nombre) + Nro', () => {
      const items = [item(15, HU), item(16, SUBTAREA), item(3, EPICA), item(7, HU)];

      expect(numeros(ordenarTablero(items, null))).toEqual([3, 7, 15, 16]);
    });

    // RN-32: dos tipos del mismo nivel se ordenan por nombre del tipo y luego por número
    it('sin orden y mismo nivel ordena por nombre del tipo y luego número', () => {
      const items = [item(16, SUBTAREA), item(30, BUG), item(12, SUBTAREA), item(20, BUG)];

      expect(numeros(ordenarTablero(items, null))).toEqual([20, 30, 12, 16]);
    });

    // RN-33, CA-30: estimación ascendente con nulos al final
    it('por estimación asc es numérico con las sin estimación al final', () => {
      const items = [
        item(1, HU, { estimacion: null }),
        item(2, HU, { estimacion: 8 }),
        item(3, HU, { estimacion: 0 }),
        item(4, HU, { estimacion: 12.5 }),
      ];

      const ordenados = ordenarTablero(items, { columna: 'estimacion', direccion: 'asc' });

      expect(numeros(ordenados)).toEqual([3, 2, 4, 1]);
    });

    // RN-33, CA-30: estimación descendente con nulos igualmente al final
    it('por estimación desc deja las sin estimación al final', () => {
      const items = [
        item(1, HU, { estimacion: null }),
        item(2, HU, { estimacion: 8 }),
        item(3, HU, { estimacion: 0 }),
        item(4, HU, { estimacion: 12.5 }),
      ];

      const ordenados = ordenarTablero(items, { columna: 'estimacion', direccion: 'desc' });

      expect(numeros(ordenados)).toEqual([4, 2, 3, 1]);
    });

    // RN-33, CA-31: estado por orden del catálogo (no alfabético), luego nombre
    it('por estado usa estado.orden y no el nombre', () => {
      const estado = (id: number, nombre: string, orden: number) => ({
        id,
        nombre,
        orden,
        activo: true,
        esFinal: false,
        categoria: null,
      });
      const items = [
        item(1, HU, { estado: estado(3, 'Hecho', 30) }),
        item(2, HU, { estado: estado(4, 'Bloqueada', 24) }),
        item(3, HU, { estado: estado(1, 'Por hacer', 10) }),
      ];

      const ordenados = ordenarTablero(items, { columna: 'estado', direccion: 'asc' });

      expect(numeros(ordenados)).toEqual([3, 2, 1]);
    });

    // RN-33, CA-31: responsable por nombre y "Sin responsable" al final en ambas direcciones
    it('por responsable ordena por nombre con los sin responsable al final', () => {
      const persona = (nombre: string) => ({ id: 1, nombre, activo: true });
      const items = [
        item(1, HU, { responsable: null }),
        item(2, HU, { responsable: persona('Luis') }),
        item(3, HU, { responsable: persona('Ana') }),
      ];

      expect(numeros(ordenarTablero(items, { columna: 'responsable', direccion: 'asc' }))).toEqual([
        3, 2, 1,
      ]);
      expect(
        numeros(ordenarTablero(items, { columna: 'responsable', direccion: 'desc' })),
      ).toEqual([2, 3, 1]);
    });

    // RN-33, CA-31: título sin distinguir mayúsculas ni acentos; empate por Tipo + Nro
    it('por título considera iguales "árbol" y "Arbol" y desempata por Tipo + Nro', () => {
      const items = [
        item(9, SUBTAREA, { titulo: 'Arbol' }),
        item(5, HU, { titulo: 'zeta' }),
        item(4, SUBTAREA, { titulo: 'árbol' }),
        item(8, HU, { titulo: 'árbol' }),
      ];

      const ordenados = ordenarTablero(items, { columna: 'titulo', direccion: 'asc' });

      expect(numeros(ordenados)).toEqual([8, 4, 9, 5]);
    });

    // RN-33: Nro numérico descendente
    it('por número desc ordena numéricamente', () => {
      const items = [item(2, HU), item(10, HU), item(1, EPICA)];

      expect(numeros(ordenarTablero(items, { columna: 'numero', direccion: 'desc' }))).toEqual([
        10, 2, 1,
      ]);
    });

    // RN-33: no muta el arreglo de entrada
    it('no muta el arreglo de entrada y devuelve uno nuevo', () => {
      const items = [item(15, HU), item(3, EPICA)];
      const copia = [...items];

      const ordenados = ordenarTablero(items, null);

      expect(items).toEqual(copia);
      expect(ordenados).not.toBe(items);
    });
  });

  describe('alternarOrden', () => {
    // RN-33: misma columna alterna asc → desc → asc
    it('en la misma columna alterna asc y desc', () => {
      const primero = alternarOrden(null, 'titulo');
      const segundo = alternarOrden(primero, 'titulo');
      const tercero = alternarOrden(segundo, 'titulo');

      expect([primero.direccion, segundo.direccion, tercero.direccion]).toEqual([
        'asc',
        'desc',
        'asc',
      ]);
    });

    // RN-33: otra columna empieza en ascendente
    it('en otra columna empieza en asc', () => {
      expect(alternarOrden({ columna: 'titulo', direccion: 'desc' }, 'estado')).toEqual({
        columna: 'estado',
        direccion: 'asc',
      });
    });
  });

  describe('columnasVisibles', () => {
    // RN-29, RN-30: sin configuración todas visibles en el orden fijo
    it('sin configuración devuelve las seis columnas en el orden de RN-29', () => {
      expect(columnasVisibles(null)).toEqual([
        'tipo',
        'numero',
        'titulo',
        'estado',
        'estimacion',
        'responsable',
      ]);
    });

    // RN-29, CA-32: Tipo, Nro y Título siempre; solo las configurables visibles
    it('con Estimación oculta conserva las obligatorias y el orden', () => {
      const visibles = columnasVisibles({
        mostrarEstado: true,
        mostrarEstimacion: false,
        mostrarResponsable: true,
      });

      expect(visibles).toEqual(['tipo', 'numero', 'titulo', 'estado', 'responsable']);
    });

    // RN-29: con las tres configurables ocultas quedan solo las obligatorias
    it('con las tres configurables ocultas deja Tipo, Nro y Título', () => {
      const visibles = columnasVisibles({
        mostrarEstado: false,
        mostrarEstimacion: false,
        mostrarResponsable: false,
      });

      expect(visibles).toEqual(['tipo', 'numero', 'titulo']);
    });
  });

  describe('ordenVigente', () => {
    // "Vista Tablero": si la columna ordenada quedó oculta vuelve al orden por defecto
    it('devuelve null si la columna del orden ya no está visible', () => {
      expect(
        ordenVigente({ columna: 'estimacion', direccion: 'asc' }, ['tipo', 'numero', 'titulo']),
      ).toBeNull();
    });

    // "Vista Tablero": si la columna sigue visible conserva el orden
    it('conserva el orden si su columna sigue visible', () => {
      const orden = { columna: 'titulo', direccion: 'desc' } as const;

      expect(ordenVigente(orden, ['tipo', 'numero', 'titulo'])).toBe(orden);
    });
  });
});
