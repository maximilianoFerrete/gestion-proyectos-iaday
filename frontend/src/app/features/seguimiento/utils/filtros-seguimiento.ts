import { Params } from '@angular/router';
import { parsearId } from '../../../shared/utils/ids';
import { FiltroArchivados, VALORES_ARCHIVADOS } from '../../proyectos/models/proyecto.model';
import {
  CONSULTA_TABLERO_PROYECTOS_POR_DEFECTO,
  ConsultaTableroProyectos,
  DireccionOrdenSeguimiento,
  FiltrosTablero,
  IndicadorTablero,
  LIMITES_SEGUIMIENTO,
  OrdenTableroProyectos,
  VALORES_DIRECCION_SEGUIMIENTO,
  VALORES_INDICADOR_TABLERO,
  VALORES_ORDEN_TABLERO_PROYECTOS,
} from '../models/seguimiento.model';

/**
 * Conversión entre query params y filtros de T1 (tablero general) y T2 (seguimiento de
 * proyecto / agrupación), y navegación del Resumen al Tablero (RF-005 RN-06, RN-21, RN-24).
 * El orden de la grilla del tablero no se guarda en la URL (RN-33).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */

/** Valor crudo de un query param que puede repetirse. */
export type ValorQueryRepetible = string | readonly string[] | undefined;

/** Ids válidos y sin repetir de un parámetro simple o repetido. */
export function parsearIdsRepetidos(valor: ValorQueryRepetible): number[] {
  const lista: readonly string[] =
    valor === undefined ? [] : typeof valor === 'string' ? [valor] : valor;
  const ids = lista.map((v) => parsearId(v)).filter((id): id is number => id !== null);
  return [...new Set(ids)];
}

/** Devuelve el valor si pertenece a la lista permitida o el valor por defecto. */
function elegir<T extends string>(valor: string | undefined, permitidos: readonly T[], def: T): T {
  return permitidos.find((p) => p === valor) ?? def;
}

/** Entero dentro de [min, max] o el valor por defecto. */
function enteroEnRango(valor: string | undefined, min: number, max: number, def: number): number {
  const numero = parsearId(valor);
  return numero !== null && numero >= min && numero <= max ? numero : def;
}

/** Texto recortado y truncado al máximo; vacío → null. */
function textoOpcional(valor: string | undefined, maximo: number): string | null {
  const texto = (valor ?? '').trim().slice(0, maximo);
  return texto.length > 0 ? texto : null;
}

/** Indicador del tablero (sin distinguir mayúsculas); fuera de lista → null (RN-28). */
export function parsearIndicador(valor: string | undefined): IndicadorTablero | null {
  const normalizado = (valor ?? '').trim().toLowerCase();
  return VALORES_INDICADOR_TABLERO.find((i) => i === normalizado) ?? null;
}

// ---------------------------------------------------------------------------------------------
// T2 – Seguimiento de proyecto / agrupación
// ---------------------------------------------------------------------------------------------

/** Vistas de T2 (DA-16: no hay vista Lista). */
export type VistaSeguimiento = 'resumen' | 'tablero' | 'jerarquia';

export const VALORES_VISTA_SEGUIMIENTO: readonly VistaSeguimiento[] = [
  'resumen',
  'tablero',
  'jerarquia',
];

/** Filtros de T2 que viven en la URL: ámbito, alcance, vista y filtros del tablero. */
export interface FiltrosSeguimiento extends FiltrosTablero {
  /** Excluyente con `sinAgrupacion` (RN-01). */
  readonly agrupacionId: number | null;
  readonly sinAgrupacion: boolean;
  readonly nivelIds: readonly number[];
  readonly tipoIds: readonly number[];
  readonly vista: VistaSeguimiento;
}

/** Filtros de T2 por defecto: todo el proyecto, todo el alcance, vista Resumen. */
export const FILTROS_SEGUIMIENTO_POR_DEFECTO: FiltrosSeguimiento = {
  agrupacionId: null,
  sinAgrupacion: false,
  nivelIds: [],
  tipoIds: [],
  vista: 'resumen',
  indicador: null,
  estadoId: null,
  texto: null,
};

/** Query params crudos de T2. */
export interface QueryParamsSeguimiento {
  readonly agrupacionId?: string;
  readonly sinAgrupacion?: string;
  readonly nivelId?: ValorQueryRepetible;
  readonly tipoId?: ValorQueryRepetible;
  readonly vista?: string;
  readonly indicador?: string;
  readonly estadoId?: string;
  readonly texto?: string;
}

/**
 * Convierte los query params de T2 en filtros válidos: "sin agrupación" gana sobre el id; una
 * vista desconocida (incluida la antigua `lista`) se trata como `resumen` (CA-49).
 */
export function filtrosSeguimientoDesdeQueryParams(
  params: QueryParamsSeguimiento,
): FiltrosSeguimiento {
  const sinAgrupacion = params.sinAgrupacion === 'true';
  return {
    agrupacionId: sinAgrupacion ? null : parsearId(params.agrupacionId),
    sinAgrupacion,
    nivelIds: parsearIdsRepetidos(params.nivelId),
    tipoIds: parsearIdsRepetidos(params.tipoId),
    vista: elegir<VistaSeguimiento>(params.vista, VALORES_VISTA_SEGUIMIENTO, 'resumen'),
    indicador: parsearIndicador(params.indicador),
    estadoId: parsearId(params.estadoId),
    texto: textoOpcional(params.texto, LIMITES_SEGUIMIENTO.textoTableroMax),
  };
}

/** Convierte los filtros de T2 en query params omitiendo los valores por defecto. */
export function aQueryParams(filtros: FiltrosSeguimiento): Params {
  const params: Record<string, string | string[]> = {};
  if (filtros.sinAgrupacion) {
    params['sinAgrupacion'] = 'true';
  } else if (filtros.agrupacionId !== null) {
    params['agrupacionId'] = String(filtros.agrupacionId);
  }
  if (filtros.nivelIds.length > 0) {
    params['nivelId'] = filtros.nivelIds.map(String);
  }
  if (filtros.tipoIds.length > 0) {
    params['tipoId'] = filtros.tipoIds.map(String);
  }
  if (filtros.vista !== 'resumen') {
    params['vista'] = filtros.vista;
  }
  if (filtros.indicador !== null) {
    params['indicador'] = filtros.indicador;
  }
  if (filtros.estadoId !== null) {
    params['estadoId'] = String(filtros.estadoId);
  }
  if (filtros.texto !== null && filtros.texto.length > 0) {
    params['texto'] = filtros.texto;
  }
  return params;
}

/** Indica si hay algún filtro propio del tablero activo (indicador, estado o texto). */
export function hayFiltrosTablero(filtros: FiltrosTablero): boolean {
  return filtros.indicador !== null || filtros.estadoId !== null || filtros.texto !== null;
}

/** Destino de un clic en el Resumen (RN-24). */
export type DestinoResumen =
  | { readonly total: true }
  | { readonly indicador: IndicadorTablero }
  | { readonly estadoId: number };

/**
 * Query params al hacer clic en el Resumen (RN-24): vista Tablero conservando ámbito y alcance;
 * "Total" sin filtros, una tarjeta con `indicador` y un estado del conteo con `estadoId`.
 */
export function navegacionDesdeResumen(
  filtros: FiltrosSeguimiento,
  destino: DestinoResumen,
): Params {
  return aQueryParams({
    ...filtros,
    vista: 'tablero',
    indicador: 'indicador' in destino ? destino.indicador : null,
    estadoId: 'estadoId' in destino ? destino.estadoId : null,
    texto: null,
  });
}

/** Filtros con otro ámbito (proyecto, agrupación o sin agrupación), conservando el resto. */
export function conAmbito(
  filtros: FiltrosSeguimiento,
  agrupacionId: number | null,
  sinAgrupacion: boolean,
): FiltrosSeguimiento {
  return { ...filtros, agrupacionId: sinAgrupacion ? null : agrupacionId, sinAgrupacion };
}

/** Query params de T2 para un ámbito y un alcance (enlaces desde T1 y desde el Resumen). */
export function queryParamsAmbito(
  alcance: { readonly nivelIds: readonly number[]; readonly tipoIds: readonly number[] },
  agrupacionId: number | null,
  sinAgrupacion: boolean,
): Params {
  return aQueryParams({
    ...FILTROS_SEGUIMIENTO_POR_DEFECTO,
    nivelIds: alcance.nivelIds,
    tipoIds: alcance.tipoIds,
    agrupacionId,
    sinAgrupacion,
  });
}

// ---------------------------------------------------------------------------------------------
// T1 – Tablero general
// ---------------------------------------------------------------------------------------------

/** Query params crudos de T1. */
export interface QueryParamsTableroGeneral {
  readonly texto?: string;
  readonly archivados?: string;
  readonly nivelId?: ValorQueryRepetible;
  readonly tipoId?: ValorQueryRepetible;
  readonly pagina?: string;
  readonly tamanioPagina?: string;
  readonly ordenarPor?: string;
  readonly direccion?: string;
}

/** Convierte los query params de T1 en una consulta válida (valores inválidos → default). */
export function parsearConsultaTableroGeneral(
  params: QueryParamsTableroGeneral,
): ConsultaTableroProyectos {
  const def = CONSULTA_TABLERO_PROYECTOS_POR_DEFECTO;
  return {
    texto: textoOpcional(params.texto, LIMITES_SEGUIMIENTO.textoProyectosMax) ?? undefined,
    archivados: elegir<FiltroArchivados>(params.archivados, VALORES_ARCHIVADOS, def.archivados),
    nivelIds: parsearIdsRepetidos(params.nivelId),
    tipoIds: parsearIdsRepetidos(params.tipoId),
    pagina: enteroEnRango(params.pagina, 1, Number.MAX_SAFE_INTEGER, def.pagina),
    tamanioPagina: enteroEnRango(
      params.tamanioPagina,
      1,
      LIMITES_SEGUIMIENTO.tamanioPaginaMax,
      def.tamanioPagina,
    ),
    ordenarPor: elegir<OrdenTableroProyectos>(
      params.ordenarPor,
      VALORES_ORDEN_TABLERO_PROYECTOS,
      def.ordenarPor,
    ),
    direccion: elegir<DireccionOrdenSeguimiento>(
      params.direccion,
      VALORES_DIRECCION_SEGUIMIENTO,
      def.direccion,
    ),
  };
}

/** Convierte la consulta de T1 en query params omitiendo los valores por defecto. */
export function serializarConsultaTableroGeneral(consulta: ConsultaTableroProyectos): Params {
  const def = CONSULTA_TABLERO_PROYECTOS_POR_DEFECTO;
  const params: Record<string, string | string[]> = {};
  if (consulta.texto) {
    params['texto'] = consulta.texto;
  }
  if (consulta.archivados !== def.archivados) {
    params['archivados'] = consulta.archivados;
  }
  if (consulta.nivelIds.length > 0) {
    params['nivelId'] = consulta.nivelIds.map(String);
  }
  if (consulta.tipoIds.length > 0) {
    params['tipoId'] = consulta.tipoIds.map(String);
  }
  if (consulta.pagina !== def.pagina) {
    params['pagina'] = String(consulta.pagina);
  }
  if (consulta.tamanioPagina !== def.tamanioPagina) {
    params['tamanioPagina'] = String(consulta.tamanioPagina);
  }
  if (consulta.ordenarPor !== def.ordenarPor) {
    params['ordenarPor'] = consulta.ordenarPor;
  }
  if (consulta.direccion !== def.direccion) {
    params['direccion'] = consulta.direccion;
  }
  return params;
}

/** Nuevo orden de T1 al hacer clic en un encabezado: alterna dirección o empieza ascendente. */
export function siguienteOrdenTableroGeneral(
  actual: Pick<ConsultaTableroProyectos, 'ordenarPor' | 'direccion'>,
  columna: OrdenTableroProyectos,
): Pick<ConsultaTableroProyectos, 'ordenarPor' | 'direccion'> {
  if (actual.ordenarPor === columna) {
    return { ordenarPor: columna, direccion: actual.direccion === 'asc' ? 'desc' : 'asc' };
  }
  return { ordenarPor: columna, direccion: 'asc' };
}
