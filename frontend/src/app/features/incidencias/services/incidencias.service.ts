import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { Paginado } from '../../../core/models/paginado.model';
import { crearParams } from '../../../core/services/http-params.util';
import {
  CambiarTipoIncidenciaRequest,
  CrearIncidenciaRequest,
  EditarIncidenciaRequest,
  FiltrosIncidencias,
  IncidenciaDto,
  IncidenciaResumenDto,
  NodoArbolIncidenciaDto,
} from '../models/incidencia.model';

/**
 * Cliente HTTP de `/api/proyectos/{proyectoId}/incidencias`. No hay eliminación (RN-34).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class IncidenciasService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  /** Lista paginada; envía solo los filtros definidos (booleans solo si son true). */
  listar(
    proyectoId: number,
    filtros: Partial<FiltrosIncidencias> = {},
  ): Observable<Paginado<IncidenciaResumenDto>> {
    const params = crearParams({
      texto: filtros.texto?.trim(),
      tipoId: filtros.tipoIds ?? [],
      estadoId: filtros.estadoId,
      prioridadId: filtros.prioridadId,
      responsableId: filtros.responsableId,
      sinResponsable: filtros.sinResponsable ? true : undefined,
      agrupacionId: filtros.agrupacionId,
      sinAgrupacion: filtros.sinAgrupacion ? true : undefined,
      pagina: filtros.pagina,
      tamanioPagina: filtros.tamanioPagina,
      ordenarPor: filtros.ordenarPor,
      direccion: filtros.direccion,
    });
    return this.http.get<Paginado<IncidenciaResumenDto>>(this.urlBase(proyectoId), { params });
  }

  /** Lista plana de todas las incidencias del proyecto para armar el árbol. */
  arbol(proyectoId: number): Observable<NodoArbolIncidenciaDto[]> {
    return this.http.get<NodoArbolIncidenciaDto[]>(`${this.urlBase(proyectoId)}/arbol`);
  }

  /** Detalle de una incidencia del proyecto (con padre, hijas y tipos de hija permitidos). */
  obtener(proyectoId: number, id: number): Observable<IncidenciaDto> {
    return this.http.get<IncidenciaDto>(`${this.urlBase(proyectoId)}/${id}`);
  }

  /** Crea una incidencia (el sistema asigna número, código y estado inicial). */
  crear(proyectoId: number, request: CrearIncidenciaRequest): Observable<IncidenciaDto> {
    return this.http.post<IncidenciaDto>(this.urlBase(proyectoId), request);
  }

  /** Edita una incidencia (reemplazo completo; el tipo no se modifica aquí). */
  editar(
    proyectoId: number,
    id: number,
    request: EditarIncidenciaRequest,
  ): Observable<IncidenciaDto> {
    return this.http.put<IncidenciaDto>(`${this.urlBase(proyectoId)}/${id}`, request);
  }

  /** Cambia el tipo (requiere permiso Incidencias.CambiarTipo). */
  cambiarTipo(
    proyectoId: number,
    id: number,
    request: CambiarTipoIncidenciaRequest,
  ): Observable<IncidenciaDto> {
    return this.http.post<IncidenciaDto>(`${this.urlBase(proyectoId)}/${id}/cambiar-tipo`, request);
  }

  /** URL de la colección de incidencias de un proyecto. */
  private urlBase(proyectoId: number): string {
    return `${this.apiUrl}/proyectos/${proyectoId}/incidencias`;
  }
}
