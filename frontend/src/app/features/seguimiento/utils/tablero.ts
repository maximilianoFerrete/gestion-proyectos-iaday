import { ConfiguracionTableroDto, ItemTableroDto } from '../models/seguimiento.model';

/**
 * Utilidades puras de la grilla del tablero (RF-005 RN-29, RN-32, RN-33): columnas visibles,
 * orden por defecto y orden por clic calculado en el cliente (no se persiste).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */

/** Columnas de la grilla, en el orden fijo de RN-29. */
export type ColumnaTablero = 'tipo' | 'numero' | 'titulo' | 'estado' | 'estimacion' | 'responsable';

/** Todas las columnas en el orden de RN-29. */
export const COLUMNAS_TABLERO: readonly ColumnaTablero[] = [
  'tipo',
  'numero',
  'titulo',
  'estado',
  'estimacion',
  'responsable',
];

/** Columnas obligatorias (siempre visibles, RN-29). */
export const COLUMNAS_OBLIGATORIAS: readonly ColumnaTablero[] = ['tipo', 'numero', 'titulo'];

/** Encabezado visible de cada columna. */
export const TITULOS_COLUMNA_TABLERO: Readonly<Record<ColumnaTablero, string>> = {
  tipo: 'Tipo',
  numero: 'Nro',
  titulo: 'Título',
  estado: 'Estado',
  estimacion: 'Estimación',
  responsable: 'Responsable',
};

export type DireccionOrdenTablero = 'asc' | 'desc';

/** Orden elegido por el usuario; null = orden por defecto (RN-32). */
export interface OrdenTablero {
  readonly columna: ColumnaTablero;
  readonly direccion: DireccionOrdenTablero;
}

/** Columnas visibles según la configuración: obligatorias + configurables visibles (RN-29). */
export function columnasVisibles(
  configuracion: Pick<
    ConfiguracionTableroDto,
    'mostrarEstado' | 'mostrarEstimacion' | 'mostrarResponsable'
  > | null,
): ColumnaTablero[] {
  const visibles: Record<ColumnaTablero, boolean> = {
    tipo: true,
    numero: true,
    titulo: true,
    estado: configuracion?.mostrarEstado ?? true,
    estimacion: configuracion?.mostrarEstimacion ?? true,
    responsable: configuracion?.mostrarResponsable ?? true,
  };
  return COLUMNAS_TABLERO.filter((columna) => visibles[columna]);
}

/** Clic en un encabezado: misma columna alterna asc ↔ desc; otra columna empieza en asc. */
export function alternarOrden(actual: OrdenTablero | null, columna: ColumnaTablero): OrdenTablero {
  if (actual !== null && actual.columna === columna) {
    return { columna, direccion: actual.direccion === 'asc' ? 'desc' : 'asc' };
  }
  return { columna, direccion: 'asc' };
}

/** Comparador de textos sin distinguir mayúsculas ni acentos (`es`, `sensitivity: 'base'`). */
const COLLATOR = new Intl.Collator('es', { sensitivity: 'base' });

/** Orden por defecto (RN-32): nivel del tipo, nombre del tipo y número. */
export function compararPorDefecto(a: ItemTableroDto, b: ItemTableroDto): number {
  return (
    a.tipo.nivelNumero - b.tipo.nivelNumero ||
    COLLATOR.compare(a.tipo.nombre, b.tipo.nombre) ||
    a.numero - b.numero
  );
}

/** Compara dos valores posiblemente nulos: los nulos siempre al final, en ambas direcciones. */
function compararConNulos<T>(
  a: T | null,
  b: T | null,
  comparar: (x: T, y: T) => number,
  signo: number,
): number {
  if (a === null && b === null) {
    return 0;
  }
  if (a === null) {
    return 1;
  }
  if (b === null) {
    return -1;
  }
  return signo * comparar(a, b);
}

/** Compara dos ítems por la columna indicada (sin desempate). */
function compararPorColumna(
  a: ItemTableroDto,
  b: ItemTableroDto,
  columna: ColumnaTablero,
  signo: number,
): number {
  switch (columna) {
    case 'tipo':
      return signo * compararPorDefecto(a, b);
    case 'numero':
      return signo * (a.numero - b.numero);
    case 'titulo':
      return signo * COLLATOR.compare(a.titulo, b.titulo);
    case 'estado':
      return (
        signo *
        (a.estado.orden - b.estado.orden || COLLATOR.compare(a.estado.nombre, b.estado.nombre))
      );
    case 'estimacion':
      return compararConNulos(a.estimacion, b.estimacion, (x, y) => x - y, signo);
    case 'responsable':
      return compararConNulos(
        a.responsable?.nombre ?? null,
        b.responsable?.nombre ?? null,
        (x, y) => COLLATOR.compare(x, y),
        signo,
      );
  }
}

/**
 * Ordena los ítems del tablero (RN-33) sin mutar la entrada: null = orden por defecto (RN-32);
 * con columna, por su criterio con nulos al final y desempate por el orden por defecto.
 */
export function ordenarTablero(
  items: readonly ItemTableroDto[],
  orden: OrdenTablero | null,
): ItemTableroDto[] {
  if (orden === null) {
    return [...items].sort(compararPorDefecto);
  }
  const signo = orden.direccion === 'asc' ? 1 : -1;
  return [...items].sort(
    (a, b) => compararPorColumna(a, b, orden.columna, signo) || compararPorDefecto(a, b),
  );
}

/** Mantiene el orden si su columna sigue visible; si quedó oculta, vuelve al por defecto. */
export function ordenVigente(
  orden: OrdenTablero | null,
  visibles: readonly ColumnaTablero[],
): OrdenTablero | null {
  return orden !== null && visibles.includes(orden.columna) ? orden : null;
}
