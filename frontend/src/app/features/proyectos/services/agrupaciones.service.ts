import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { AgrupacionDto, AgrupacionRequest } from '../models/agrupacion.model';

/**
 * Cliente HTTP de `/api/proyectos/{proyectoId}/agrupaciones`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Injectable({ providedIn: 'root' })
export class AgrupacionesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  /** Lista las agrupaciones del proyecto ordenadas por nombre. */
  listar(proyectoId: number): Observable<AgrupacionDto[]> {
    return this.http.get<AgrupacionDto[]>(this.urlBase(proyectoId));
  }

  /** Obtiene una agrupación del proyecto. */
  obtener(proyectoId: number, id: number): Observable<AgrupacionDto> {
    return this.http.get<AgrupacionDto>(`${this.urlBase(proyectoId)}/${id}`);
  }

  /** Crea una agrupación en el proyecto. */
  crear(proyectoId: number, request: AgrupacionRequest): Observable<AgrupacionDto> {
    return this.http.post<AgrupacionDto>(this.urlBase(proyectoId), request);
  }

  /** Edita una agrupación del proyecto. */
  editar(proyectoId: number, id: number, request: AgrupacionRequest): Observable<AgrupacionDto> {
    return this.http.put<AgrupacionDto>(`${this.urlBase(proyectoId)}/${id}`, request);
  }

  /** Elimina físicamente una agrupación del proyecto. */
  eliminar(proyectoId: number, id: number): Observable<void> {
    return this.http.delete<void>(`${this.urlBase(proyectoId)}/${id}`);
  }

  /** URL de la colección de agrupaciones de un proyecto. */
  private urlBase(proyectoId: number): string {
    return `${this.apiUrl}/proyectos/${proyectoId}/agrupaciones`;
  }
}
