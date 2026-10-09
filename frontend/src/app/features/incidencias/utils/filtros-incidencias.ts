import { Params } from '@angular/router';
import { parsearId } from '../../../shared/utils/ids';
import {
  DireccionOrdenIncidencias,
  FILTROS_INCIDENCIAS_POR_DEFECTO,
  FiltrosIncidencias,
  LIMITES_INCIDENCIA,
  OrdenIncidencias,
  VALORES_DIRECCION_INCIDENCIAS,
  VALORES_ORDEN_INCIDENCIAS,
} from '../models/incidencia.model';

/**
 * Conversión entre los query params de I1 y los filtros del listado de incidencias.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Query params crudos del listado (todos opcionales; `tipoId` puede repetirse). */
export interface QueryParamsIncidencias {
  readonly texto?: string;
  readonly tipoId?: string | readonly string[];
  readonly estadoId?: string;
  readonly prioridadId?: string;
  readonly responsableId?: string;
  readonly sinResponsable?: string;
  readonly agrupacionId?: string;
  readonly sinAgrupacion?: string;
  readonly pagina?: string;
  readonly tamanioPagina?: string;
  readonly ordenarPor?: string;
  readonly direccion?: string;
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

/** Ids válidos y sin repetir de un parámetro simple o repetido. */
export function parsearIds(valor: string | readonly string[] | undefined): number[] {
  const lista: readonly string[] =
    valor === undefined ? [] : typeof valor === 'string' ? [valor] : valor;
  const ids = lista.map((v) => parsearId(v)).filter((id): id is number => id !== null);
  return [...new Set(ids)];
}

/** Convierte los query params en filtros válidos; "sin X" tiene prioridad sobre el id. */
export function parsearFiltrosIncidencias(params: QueryParamsIncidencias): FiltrosIncidencias {
  const def = FILTROS_INCIDENCIAS_POR_DEFECTO;
  const texto = (params.texto ?? '').trim().slice(0, LIMITES_INCIDENCIA.textoFiltroMax);
  const sinResponsable = params.sinResponsable === 'true';
  const sinAgrupacion = params.sinAgrupacion === 'true';
  return {
    texto: texto.length > 0 ? texto : undefined,
    tipoIds: parsearIds(params.tipoId),
    estadoId: parsearId(params.estadoId) ?? undefined,
    prioridadId: parsearId(params.prioridadId) ?? undefined,
    responsableId: sinResponsable ? undefined : (parsearId(params.responsableId) ?? undefined),
    sinResponsable,
    agrupacionId: sinAgrupacion ? undefined : (parsearId(params.agrupacionId) ?? undefined),
    sinAgrupacion,
    pagina: enteroEnRango(params.pagina, 1, Number.MAX_SAFE_INTEGER, def.pagina),
    tamanioPagina: enteroEnRango(
      params.tamanioPagina,
      1,
      LIMITES_INCIDENCIA.tamanioPaginaMax,
      def.tamanioPagina,
    ),
    ordenarPor: elegir<OrdenIncidencias>(
      params.ordenarPor,
      VALORES_ORDEN_INCIDENCIAS,
      def.ordenarPor,
    ),
    direccion: elegir<DireccionOrdenIncidencias>(
      params.direccion,
      VALORES_DIRECCION_INCIDENCIAS,
      def.direccion,
    ),
  };
}

/** Convierte filtros en query params omitiendo los valores por defecto (URL limpia). */
export function serializarFiltrosIncidencias(filtros: FiltrosIncidencias): Params {
  const def = FILTROS_INCIDENCIAS_POR_DEFECTO;
  const params: Record<string, string | string[]> = {};
  if (filtros.texto) {
    params['texto'] = filtros.texto;
  }
  if (filtros.tipoIds.length > 0) {
    params['tipoId'] = filtros.tipoIds.map(String);
  }
  agregarId(params, 'estadoId', filtros.estadoId);
  agregarId(params, 'prioridadId', filtros.prioridadId);
  if (filtros.sinResponsable) {
    params['sinResponsable'] = 'true';
  } else {
    agregarId(params, 'responsableId', filtros.responsableId);
  }
  if (filtros.sinAgrupacion) {
    params['sinAgrupacion'] = 'true';
  } else {
    agregarId(params, 'agrupacionId', filtros.agrupacionId);
  }
  agregarSiDistinto(params, 'pagina', filtros.pagina, def.pagina);
  agregarSiDistinto(params, 'tamanioPagina', filtros.tamanioPagina, def.tamanioPagina);
  agregarSiDistinto(params, 'ordenarPor', filtros.ordenarPor, def.ordenarPor);
  agregarSiDistinto(params, 'direccion', filtros.direccion, def.direccion);
  return params;
}

/** Agrega un id opcional como texto. */
function agregarId(params: Record<string, string | string[]>, clave: string, id?: number): void {
  if (id !== undefined) {
    params[clave] = String(id);
  }
}

/** Agrega el parámetro solo si difiere del valor por defecto. */
function agregarSiDistinto(
  params: Record<string, string | string[]>,
  clave: string,
  valor: string | number,
  porDefecto: string | number,
): void {
  if (valor !== porDefecto) {
    params[clave] = String(valor);
  }
}

/** Nuevo orden al hacer clic en una columna: alterna dirección o empieza ascendente. */
export function siguienteOrdenIncidencias(
  actual: Pick<FiltrosIncidencias, 'ordenarPor' | 'direccion'>,
  columna: OrdenIncidencias,
): Pick<FiltrosIncidencias, 'ordenarPor' | 'direccion'> {
  if (actual.ordenarPor === columna) {
    return { ordenarPor: columna, direccion: actual.direccion === 'asc' ? 'desc' : 'asc' };
  }
  return { ordenarPor: columna, direccion: 'asc' };
}
