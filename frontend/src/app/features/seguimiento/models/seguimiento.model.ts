import { Paginado } from '../../../core/models/paginado.model';
import { CategoriaEstadoIncidencia } from '../../configuracion-incidencias/models/estado-incidencia.model';
import { TipoReferenciaDto } from '../../configuracion-incidencias/models/relacion-tipo-incidencia.model';
import { EstadoRefDto } from '../../estados-proyecto/models/estado-proyecto.model';
import { PersonaRefDto } from '../../personas/models/persona.model';
import { FiltroArchivados } from '../../proyectos/models/proyecto.model';

/**
 * Modelos del tablero de seguimiento según el contrato API de RF-005 ("Contrato API").
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */

// ---------------------------------------------------------------------------------------------
// DTOs compartidos
// ---------------------------------------------------------------------------------------------

/** Nivel incluido en el alcance (`AlcanceSeguimientoDto.niveles`). */
export interface NivelAlcanceDto {
  readonly id: number;
  readonly numero: number;
  readonly nombre: string;
}

/** `AlcanceSeguimientoDto`: niveles y tipos efectivamente contabilizados (RN-06). */
export interface AlcanceSeguimientoDto {
  readonly todosLosNiveles: boolean;
  readonly niveles: readonly NivelAlcanceDto[];
  readonly todosLosTipos: boolean;
  /** Tipos contabilizados (intersección RN-04), por `nivelNumero` y `nombre`. */
  readonly tipos: readonly TipoReferenciaDto[];
}

/** `EstadoSeguimientoDto` (fila del conteo por estado, RN-15). */
export interface EstadoSeguimientoDto {
  readonly id: number;
  readonly nombre: string;
  readonly orden: number;
  readonly activo: boolean;
  readonly categoria: CategoriaEstadoIncidencia | null;
  /** Final en todos los flujos de los tipos contabilizados que lo incluyen. */
  readonly esFinal: boolean;
}

/** Cantidad de incidencias en un estado. */
export interface ConteoEstadoDto {
  readonly estado: EstadoSeguimientoDto;
  readonly cantidad: number;
}

/** `IndicadoresDto` (RN-09 a RN-16). */
export interface IndicadoresDto {
  readonly total: number;
  readonly abiertas: number;
  readonly finalizadas: number;
  readonly vencidas: number;
  readonly bloqueadas: number;
  readonly enEspera: number;
  readonly sinResponsable: number;
  readonly porEstado: readonly ConteoEstadoDto[];
}

// ---------------------------------------------------------------------------------------------
// Tablero general (T1)
// ---------------------------------------------------------------------------------------------

/** Proyecto en una fila del tablero general. */
export interface ProyectoTableroGeneralDto {
  readonly id: number;
  readonly codigo: string;
  readonly nombre: string;
  readonly archivado: boolean;
  readonly estado: EstadoRefDto;
  readonly responsable: PersonaRefDto;
  /** Fecha sin hora `yyyy-MM-dd`. */
  readonly fechaObjetivo: string;
}

/** Fila de proyecto del tablero general. */
export interface FilaProyectoSeguimientoDto {
  readonly proyecto: ProyectoTableroGeneralDto;
  readonly cantidadAgrupaciones: number;
  readonly indicadores: IndicadoresDto;
}

/** Response de `GET /api/seguimiento/proyectos`. */
export interface TableroProyectosDto {
  /** Fecha de referencia del servidor `yyyy-MM-dd` (RN-10). */
  readonly fechaReferencia: string;
  readonly alcance: AlcanceSeguimientoDto;
  /** Suma de todos los proyectos que cumplen el filtro (todas las páginas, RN-27). */
  readonly totales: IndicadoresDto;
  readonly proyectos: Paginado<FilaProyectoSeguimientoDto>;
}

/** Columnas admitidas por `ordenarPor` en el tablero general (lista blanca). */
export type OrdenTableroProyectos =
  | 'codigo'
  | 'nombre'
  | 'total'
  | 'abiertas'
  | 'vencidas'
  | 'bloqueadas'
  | 'enEspera'
  | 'sinResponsable';

export const VALORES_ORDEN_TABLERO_PROYECTOS: readonly OrdenTableroProyectos[] = [
  'codigo',
  'nombre',
  'total',
  'abiertas',
  'vencidas',
  'bloqueadas',
  'enEspera',
  'sinResponsable',
];

export type DireccionOrdenSeguimiento = 'asc' | 'desc';

export const VALORES_DIRECCION_SEGUIMIENTO: readonly DireccionOrdenSeguimiento[] = ['asc', 'desc'];

/** Alcance pedido (`nivelId` / `tipoId` repetibles). */
export interface AlcanceConsulta {
  readonly nivelIds: readonly number[];
  readonly tipoIds: readonly number[];
}

/** Parámetros de `GET /api/seguimiento/proyectos`. */
export interface ConsultaTableroProyectos extends AlcanceConsulta {
  readonly texto?: string;
  readonly archivados: FiltroArchivados;
  readonly pagina: number;
  readonly tamanioPagina: number;
  readonly ordenarPor: OrdenTableroProyectos;
  readonly direccion: DireccionOrdenSeguimiento;
}

/** Valores por defecto del tablero general según el contrato. */
export const CONSULTA_TABLERO_PROYECTOS_POR_DEFECTO: ConsultaTableroProyectos = {
  archivados: 'excluir',
  nivelIds: [],
  tipoIds: [],
  pagina: 1,
  tamanioPagina: 20,
  ordenarPor: 'codigo',
  direccion: 'desc',
};

// ---------------------------------------------------------------------------------------------
// Seguimiento de proyecto / agrupación (T2 – Resumen)
// ---------------------------------------------------------------------------------------------

/** Proyecto embebido en las respuestas de seguimiento. */
export interface ProyectoSeguimientoDto {
  readonly id: number;
  readonly codigo: string;
  readonly nombre: string;
  readonly archivado: boolean;
}

/** Ámbito del seguimiento (RN-01). */
export type AmbitoSeguimiento = 'proyecto' | 'agrupacion' | 'sinAgrupacion';

/** Agrupación del ámbito "agrupación" (rol de detalle de agrupación, DA-08). */
export interface AgrupacionSeguimientoDto {
  readonly id: number;
  readonly nombre: string;
  readonly descripcion: string;
}

/** Referencia a una agrupación en la tabla de agrupaciones del resumen. */
export interface AgrupacionReferenciaSeguimientoDto {
  readonly id: number;
  readonly nombre: string;
}

/** Fila de la tabla de agrupaciones del resumen; `agrupacion: null` = "Sin agrupación". */
export interface FilaAgrupacionSeguimientoDto {
  readonly agrupacion: AgrupacionReferenciaSeguimientoDto | null;
  readonly indicadores: IndicadoresDto;
}

/** Response de `GET /api/proyectos/{proyectoId}/seguimiento`. */
export interface SeguimientoDto {
  readonly fechaReferencia: string;
  readonly proyecto: ProyectoSeguimientoDto;
  readonly ambito: AmbitoSeguimiento;
  readonly agrupacion: AgrupacionSeguimientoDto | null;
  readonly alcance: AlcanceSeguimientoDto;
  readonly indicadores: IndicadoresDto;
  /** Solo en ámbito proyecto (en los otros, vacío, RN-26). */
  readonly agrupaciones: readonly FilaAgrupacionSeguimientoDto[];
}

/** Ámbito pedido (`agrupacionId` y `sinAgrupacion` son excluyentes, RN-01). */
export interface AmbitoConsulta {
  readonly agrupacionId?: number | null;
  readonly sinAgrupacion?: boolean;
}

/** Parámetros de `GET /api/proyectos/{proyectoId}/seguimiento`. */
export interface ConsultaSeguimiento extends AmbitoConsulta, AlcanceConsulta {}

// ---------------------------------------------------------------------------------------------
// Tablero (grilla de incidencias, T2 – Tablero)
// ---------------------------------------------------------------------------------------------

/** Valores del filtro `indicador` del tablero (RN-21). */
export type IndicadorTablero =
  'abiertas' | 'vencidas' | 'bloqueadas' | 'en-espera' | 'sin-responsable';

export const VALORES_INDICADOR_TABLERO: readonly IndicadorTablero[] = [
  'abiertas',
  'vencidas',
  'bloqueadas',
  'en-espera',
  'sin-responsable',
];

/** Nombre visible de cada indicador. */
export const NOMBRES_INDICADOR_TABLERO: Readonly<Record<IndicadorTablero, string>> = {
  abiertas: 'Abiertas',
  vencidas: 'Vencidas',
  bloqueadas: 'Bloqueadas',
  'en-espera': 'En espera',
  'sin-responsable': 'Sin responsable',
};

/** Estado del filtro `estadoId` informado en `filtros.estado`. */
export interface EstadoFiltroTableroDto {
  readonly id: number;
  readonly nombre: string;
  readonly activo: boolean;
  readonly categoria: CategoriaEstadoIncidencia | null;
}

/** Eco de los filtros aplicados (`TableroIncidenciasDto.filtros`). */
export interface FiltrosAplicadosTableroDto {
  readonly indicador: IndicadorTablero | null;
  readonly estado: EstadoFiltroTableroDto | null;
  readonly texto: string | null;
}

/** `ConfiguracionTableroDto` (columnas visibles por proyecto, RN-29/RN-30). */
export interface ConfiguracionTableroDto {
  readonly proyectoId: number;
  /** false = sin configuración guardada (todas visibles). */
  readonly personalizada: boolean;
  readonly mostrarEstado: boolean;
  readonly mostrarEstimacion: boolean;
  readonly mostrarResponsable: boolean;
  /** UTC ISO-8601 o null. */
  readonly fechaModificacion: string | null;
}

/** Body de `PUT .../seguimiento/tablero/configuracion` (reemplazo completo, RN-31). */
export interface ConfiguracionTableroRequest {
  readonly mostrarEstado: boolean;
  readonly mostrarEstimacion: boolean;
  readonly mostrarResponsable: boolean;
}

/** Estado de un ítem del tablero (`orden` para ordenar en el cliente). */
export interface EstadoTableroDto {
  readonly id: number;
  readonly nombre: string;
  readonly orden: number;
  readonly activo: boolean;
  readonly esFinal: boolean;
  readonly categoria: CategoriaEstadoIncidencia | null;
}

/** Ítem de la grilla del tablero. */
export interface ItemTableroDto {
  readonly id: number;
  readonly codigo: string;
  readonly numero: number;
  readonly titulo: string;
  readonly tipo: TipoReferenciaDto;
  readonly estado: EstadoTableroDto;
  readonly estimacion: number | null;
  /** Vencida según la fecha de referencia del servidor (RN-36). */
  readonly vencida: boolean;
  readonly responsable: PersonaRefDto | null;
}

/** Response de `GET /api/proyectos/{proyectoId}/seguimiento/tablero`. */
export interface TableroIncidenciasDto {
  readonly fechaReferencia: string;
  readonly proyecto: ProyectoSeguimientoDto;
  readonly alcance: AlcanceSeguimientoDto;
  readonly filtros: FiltrosAplicadosTableroDto;
  readonly configuracion: ConfiguracionTableroDto;
  /** Cantidad de `items`. */
  readonly total: number;
  /** Todas las incidencias del ámbito, alcance y filtros, en el orden por defecto (RN-32). */
  readonly items: readonly ItemTableroDto[];
}

/** Filtros propios del tablero (RN-21). */
export interface FiltrosTablero {
  readonly indicador: IndicadorTablero | null;
  readonly estadoId: number | null;
  readonly texto: string | null;
}

/** Sin filtros del tablero. */
export const SIN_FILTROS_TABLERO: FiltrosTablero = { indicador: null, estadoId: null, texto: null };

/** Parámetros de `GET /api/proyectos/{proyectoId}/seguimiento/tablero`. */
export interface ConsultaTablero extends AmbitoConsulta, AlcanceConsulta {
  readonly indicador?: IndicadorTablero | null;
  readonly estadoId?: number | null;
  readonly texto?: string | null;
}

/** Límites de los filtros del seguimiento. */
export const LIMITES_SEGUIMIENTO = {
  textoProyectosMax: 150,
  textoTableroMax: 200,
  tamanioPaginaMax: 100,
} as const;
