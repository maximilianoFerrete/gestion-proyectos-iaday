import { PersonaRefDto } from '../../personas/models/persona.model';
import { EstadoRefDto } from '../../estados-proyecto/models/estado-proyecto.model';

/**
 * Modelos de Proyectos según el contrato API de RF-001.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */

/** Ítem de `GET /api/proyectos` (`Paginado<ProyectoResumenDto>`). */
export interface ProyectoResumenDto {
  readonly id: number;
  readonly codigo: string;
  readonly nombre: string;
  readonly responsable: PersonaRefDto;
  /** Fecha sin hora `yyyy-MM-dd`. */
  readonly fechaInicio: string;
  /** Fecha sin hora `yyyy-MM-dd`. */
  readonly fechaObjetivo: string;
  readonly estado: EstadoRefDto;
  readonly archivado: boolean;
}

/** `ProyectoDto` devuelto por `GET/POST/PUT /api/proyectos` y `archivar`. */
export interface ProyectoDto extends ProyectoResumenDto {
  readonly descripcion: string;
  /** UTC ISO-8601 o null si no está archivado. */
  readonly fechaArchivado: string | null;
  readonly archivadoPor: PersonaRefDto | null;
  readonly estadoAlArchivar: EstadoRefDto | null;
  readonly cantidadAgrupaciones: number;
  readonly fechaCreacion: string;
  readonly fechaModificacion: string | null;
}

/** Body de `POST /api/proyectos` (sin estado: se asigna el inicial, RN-18). */
export interface CrearProyectoRequest {
  readonly nombre: string;
  readonly descripcion: string;
  readonly responsableId: number;
  readonly fechaInicio: string;
  readonly fechaObjetivo: string;
}

/** Body de `PUT /api/proyectos/{id}`. */
export interface EditarProyectoRequest extends CrearProyectoRequest {
  readonly estadoId: number;
}

/** Valores admitidos del filtro `archivados`. */
export type FiltroArchivados = 'excluir' | 'solo' | 'incluir';

/** Columnas admitidas por `ordenarPor` (lista blanca del contrato). */
export type OrdenProyectos =
  'codigo' | 'nombre' | 'fechaInicio' | 'fechaObjetivo' | 'estado' | 'responsable';

export type DireccionOrden = 'asc' | 'desc';

/** Parámetros de `GET /api/proyectos`. */
export interface FiltrosProyectos {
  readonly texto?: string;
  readonly estadoId?: number;
  readonly responsableId?: number;
  readonly archivados: FiltroArchivados;
  readonly pagina: number;
  readonly tamanioPagina: number;
  readonly ordenarPor: OrdenProyectos;
  readonly direccion: DireccionOrden;
}

export const VALORES_ARCHIVADOS: readonly FiltroArchivados[] = ['excluir', 'solo', 'incluir'];
export const VALORES_ORDEN: readonly OrdenProyectos[] = [
  'codigo',
  'nombre',
  'fechaInicio',
  'fechaObjetivo',
  'estado',
  'responsable',
];
export const VALORES_DIRECCION: readonly DireccionOrden[] = ['asc', 'desc'];

/** Valores por defecto del listado según el contrato. */
export const FILTROS_PROYECTOS_POR_DEFECTO: FiltrosProyectos = {
  archivados: 'excluir',
  pagina: 1,
  tamanioPagina: 20,
  ordenarPor: 'codigo',
  direccion: 'desc',
};

/** Límites de validación de Proyecto (RN-15) y del filtro de texto. */
export const LIMITES_PROYECTO = {
  nombreMax: 150,
  descripcionMax: 2000,
  textoFiltroMax: 150,
  tamanioPaginaMax: 100,
} as const;
