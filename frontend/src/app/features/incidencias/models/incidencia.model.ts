import { PersonaRefDto } from '../../personas/models/persona.model';
import { TipoReferenciaDto } from '../../configuracion-incidencias/models/relacion-tipo-incidencia.model';

/**
 * Modelos de Incidencias según el contrato API de RF-002.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

export type { TipoReferenciaDto };

/** `EstadoIncidenciaReferenciaDto` (`esFinal` según el flujo del tipo de la incidencia). */
export interface EstadoIncidenciaReferenciaDto {
  readonly id: number;
  readonly nombre: string;
  readonly activo: boolean;
  readonly esFinal: boolean;
}

/** `PrioridadReferenciaDto`. */
export interface PrioridadReferenciaDto {
  readonly id: number;
  readonly nombre: string;
  readonly orden: number;
  readonly activo: boolean;
}

/** `AgrupacionReferenciaDto`. */
export interface AgrupacionReferenciaDto {
  readonly id: number;
  readonly nombre: string;
}

/** `IncidenciaReferenciaDto` (padre e hijas en el detalle). */
export interface IncidenciaReferenciaDto {
  readonly id: number;
  readonly codigo: string;
  readonly titulo: string;
  readonly tipo: TipoReferenciaDto;
  readonly estado: EstadoIncidenciaReferenciaDto;
  readonly responsable: PersonaRefDto | null;
  /** Fecha sin hora `yyyy-MM-dd` o null. */
  readonly fechaVencimiento: string | null;
}

/** Referencia mínima al padre en el listado. */
export interface PadreResumenDto {
  readonly id: number;
  readonly codigo: string;
}

/** Ítem de `GET /api/proyectos/{proyectoId}/incidencias` (`Paginado<IncidenciaResumenDto>`). */
export interface IncidenciaResumenDto {
  readonly id: number;
  readonly codigo: string;
  readonly numero: number;
  readonly titulo: string;
  readonly tipo: TipoReferenciaDto;
  readonly estado: EstadoIncidenciaReferenciaDto;
  readonly prioridad: PrioridadReferenciaDto;
  readonly responsable: PersonaRefDto | null;
  readonly fechaVencimiento: string | null;
  readonly agrupacion: AgrupacionReferenciaDto | null;
  readonly padre: PadreResumenDto | null;
  readonly cantidadHijas: number;
  readonly fechaCreacion: string;
}

/** Ítem de `GET /api/proyectos/{proyectoId}/incidencias/arbol` (lista plana). */
export interface NodoArbolIncidenciaDto {
  readonly id: number;
  readonly codigo: string;
  readonly titulo: string;
  readonly padreId: number | null;
  readonly tipo: TipoReferenciaDto;
  readonly estado: EstadoIncidenciaReferenciaDto;
  readonly prioridad: PrioridadReferenciaDto;
  readonly responsable: PersonaRefDto | null;
  readonly fechaVencimiento: string | null;
  readonly agrupacion: AgrupacionReferenciaDto | null;
}

/** Proyecto embebido en `IncidenciaDto`. */
export interface ProyectoReferenciaIncidenciaDto {
  readonly id: number;
  readonly codigo: string;
  readonly nombre: string;
  readonly archivado: boolean;
}

/** Tipo de hija permitido (`tiposHijaPermitidos`). */
export interface TipoHijaPermitidoDto {
  readonly id: number;
  readonly nombre: string;
}

/** `IncidenciaDto` devuelto por GET/POST/PUT de incidencias y por `cambiar-tipo`. */
export interface IncidenciaDto {
  readonly id: number;
  readonly codigo: string;
  readonly numero: number;
  readonly proyecto: ProyectoReferenciaIncidenciaDto;
  readonly titulo: string;
  readonly descripcion: string;
  readonly tipo: TipoReferenciaDto;
  readonly estado: EstadoIncidenciaReferenciaDto;
  readonly prioridad: PrioridadReferenciaDto;
  readonly responsable: PersonaRefDto | null;
  readonly fechaVencimiento: string | null;
  readonly agrupacion: AgrupacionReferenciaDto | null;
  /** `padre != null`: la agrupación se hereda del padre (RN-28). */
  readonly agrupacionHeredada: boolean;
  readonly padre: IncidenciaReferenciaDto | null;
  /** Hijas directas ordenadas por número. */
  readonly hijas: readonly IncidenciaReferenciaDto[];
  /** Tipos activos con relación desde el tipo de la incidencia. */
  readonly tiposHijaPermitidos: readonly TipoHijaPermitidoDto[];
  readonly fechaCreacion: string;
  readonly fechaModificacion: string | null;
}

/** Body de `POST /api/proyectos/{proyectoId}/incidencias` (sin estado, RN-20). */
export interface CrearIncidenciaRequest {
  readonly tipoId: number;
  readonly titulo: string;
  readonly descripcion: string;
  /** null = prioridad predeterminada (RN-21). */
  readonly prioridadId: number | null;
  readonly responsableId: number | null;
  readonly fechaVencimiento: string | null;
  readonly agrupacionId: number | null;
  readonly padreId: number | null;
}

/** Body de `PUT /api/proyectos/{proyectoId}/incidencias/{id}` (reemplazo completo, sin tipo). */
export interface EditarIncidenciaRequest {
  readonly titulo: string;
  readonly descripcion: string;
  readonly estadoId: number;
  readonly prioridadId: number;
  readonly responsableId: number | null;
  readonly fechaVencimiento: string | null;
  readonly agrupacionId: number | null;
  readonly padreId: number | null;
}

/** Body de `POST .../incidencias/{id}/cambiar-tipo` (`estadoId` opcional, RN-23). */
export interface CambiarTipoIncidenciaRequest {
  readonly tipoId: number;
  readonly estadoId: number | null;
}

/** Columnas admitidas por `ordenarPor` (lista blanca del contrato). */
export type OrdenIncidencias =
  | 'numero'
  | 'titulo'
  | 'tipo'
  | 'estado'
  | 'prioridad'
  | 'responsable'
  | 'fechaVencimiento'
  | 'fechaCreacion';

export type DireccionOrdenIncidencias = 'asc' | 'desc';

/** Parámetros de `GET /api/proyectos/{proyectoId}/incidencias`. */
export interface FiltrosIncidencias {
  readonly texto?: string;
  /** Repetible: `tipoId=1&tipoId=2`. */
  readonly tipoIds: readonly number[];
  readonly estadoId?: number;
  readonly prioridadId?: number;
  /** Excluyente con `sinResponsable`. */
  readonly responsableId?: number;
  readonly sinResponsable: boolean;
  /** Excluyente con `sinAgrupacion`. */
  readonly agrupacionId?: number;
  readonly sinAgrupacion: boolean;
  readonly pagina: number;
  readonly tamanioPagina: number;
  readonly ordenarPor: OrdenIncidencias;
  readonly direccion: DireccionOrdenIncidencias;
}

export const VALORES_ORDEN_INCIDENCIAS: readonly OrdenIncidencias[] = [
  'numero',
  'titulo',
  'tipo',
  'estado',
  'prioridad',
  'responsable',
  'fechaVencimiento',
  'fechaCreacion',
];
export const VALORES_DIRECCION_INCIDENCIAS: readonly DireccionOrdenIncidencias[] = ['asc', 'desc'];

/** Valores por defecto del listado según el contrato. */
export const FILTROS_INCIDENCIAS_POR_DEFECTO: FiltrosIncidencias = {
  tipoIds: [],
  sinResponsable: false,
  sinAgrupacion: false,
  pagina: 1,
  tamanioPagina: 20,
  ordenarPor: 'numero',
  direccion: 'desc',
};

/** Límites de validación de Incidencia (RN-19) y del listado. */
export const LIMITES_INCIDENCIA = {
  tituloMax: 200,
  descripcionMax: 4000,
  textoFiltroMax: 200,
  tamanioPaginaMax: 100,
  /** Cantidad de candidatos a padre que se piden al buscar. */
  resultadosBusquedaPadre: 10,
} as const;
