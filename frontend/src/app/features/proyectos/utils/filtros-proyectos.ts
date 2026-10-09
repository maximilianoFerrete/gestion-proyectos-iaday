import { Params } from '@angular/router';
import { parsearId } from '../../../shared/utils/ids';
import {
  DireccionOrden,
  FILTROS_PROYECTOS_POR_DEFECTO,
  FiltroArchivados,
  FiltrosProyectos,
  LIMITES_PROYECTO,
  OrdenProyectos,
  VALORES_ARCHIVADOS,
  VALORES_DIRECCION,
  VALORES_ORDEN,
} from '../models/proyecto.model';

/** Query params crudos del listado de proyectos (todos opcionales, como strings). */
export interface QueryParamsProyectos {
  readonly texto?: string;
  readonly estadoId?: string;
  readonly responsableId?: string;
  readonly archivados?: string;
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

/** Convierte los query params de la URL en filtros válidos (valores inválidos → default). */
export function parsearFiltros(params: QueryParamsProyectos): FiltrosProyectos {
  const def = FILTROS_PROYECTOS_POR_DEFECTO;
  const texto = (params.texto ?? '').trim().slice(0, LIMITES_PROYECTO.textoFiltroMax);
  return {
    texto: texto.length > 0 ? texto : undefined,
    estadoId: parsearId(params.estadoId) ?? undefined,
    responsableId: parsearId(params.responsableId) ?? undefined,
    archivados: elegir<FiltroArchivados>(params.archivados, VALORES_ARCHIVADOS, def.archivados),
    pagina: enteroEnRango(params.pagina, 1, Number.MAX_SAFE_INTEGER, def.pagina),
    tamanioPagina: enteroEnRango(
      params.tamanioPagina,
      1,
      LIMITES_PROYECTO.tamanioPaginaMax,
      def.tamanioPagina,
    ),
    ordenarPor: elegir<OrdenProyectos>(params.ordenarPor, VALORES_ORDEN, def.ordenarPor),
    direccion: elegir<DireccionOrden>(params.direccion, VALORES_DIRECCION, def.direccion),
  };
}

/** Convierte filtros en query params omitiendo los valores por defecto (URL limpia). */
export function serializarFiltros(filtros: FiltrosProyectos): Params {
  const def = FILTROS_PROYECTOS_POR_DEFECTO;
  const params: Record<string, string> = {};
  if (filtros.texto) {
    params['texto'] = filtros.texto;
  }
  if (filtros.estadoId !== undefined) {
    params['estadoId'] = String(filtros.estadoId);
  }
  if (filtros.responsableId !== undefined) {
    params['responsableId'] = String(filtros.responsableId);
  }
  agregarSiDistinto(params, 'archivados', filtros.archivados, def.archivados);
  agregarSiDistinto(params, 'pagina', filtros.pagina, def.pagina);
  agregarSiDistinto(params, 'tamanioPagina', filtros.tamanioPagina, def.tamanioPagina);
  agregarSiDistinto(params, 'ordenarPor', filtros.ordenarPor, def.ordenarPor);
  agregarSiDistinto(params, 'direccion', filtros.direccion, def.direccion);
  return params;
}

/** Agrega el parámetro solo si difiere del valor por defecto. */
function agregarSiDistinto(
  params: Record<string, string>,
  clave: string,
  valor: string | number,
  porDefecto: string | number,
): void {
  if (valor !== porDefecto) {
    params[clave] = String(valor);
  }
}

/** Nuevo orden al hacer clic en una columna: alterna dirección o empieza ascendente. */
export function siguienteOrden(
  actual: Pick<FiltrosProyectos, 'ordenarPor' | 'direccion'>,
  columna: OrdenProyectos,
): Pick<FiltrosProyectos, 'ordenarPor' | 'direccion'> {
  if (actual.ordenarPor === columna) {
    return { ordenarPor: columna, direccion: actual.direccion === 'asc' ? 'desc' : 'asc' };
  }
  return { ordenarPor: columna, direccion: 'asc' };
}
