import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { crearParams } from '../../../core/services/http-params.util';
import {
  FiltrosPrioridadesIncidencia,
  PrioridadIncidenciaDto,
  PrioridadIncidenciaRequest,
} from '../models/prioridad-incidencia.model';

/**
 * Cliente HTTP de `/api/prioridades-incidencia`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class PrioridadesIncidenciaService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/prioridades-incidencia`;

  /** Lista las prioridades por orden y nombre; por defecto incluye inactivas. */
  listar(filtros: FiltrosPrioridadesIncidencia = {}): Observable<PrioridadIncidenciaDto[]> {
    const params = crearParams({ soloActivas: filtros.soloActivas });
    return this.http.get<PrioridadIncidenciaDto[]>(this.url, { params });
  }

  /** Obtiene una prioridad por id. */
  obtener(id: number): Observable<PrioridadIncidenciaDto> {
    return this.http.get<PrioridadIncidenciaDto>(`${this.url}/${id}`);
  }

  /** Crea una prioridad activa y no predeterminada. */
  crear(request: PrioridadIncidenciaRequest): Observable<PrioridadIncidenciaDto> {
    return this.http.post<PrioridadIncidenciaDto>(this.url, request);
  }

  /** Edita nombre, descripción y orden de una prioridad. */
  editar(id: number, request: PrioridadIncidenciaRequest): Observable<PrioridadIncidenciaDto> {
    return this.http.put<PrioridadIncidenciaDto>(`${this.url}/${id}`, request);
  }

  /** Marca la prioridad como predeterminada (el backend desmarca la anterior). */
  marcarPredeterminada(id: number): Observable<PrioridadIncidenciaDto> {
    return this.http.post<PrioridadIncidenciaDto>(`${this.url}/${id}/marcar-predeterminada`, null);
  }

  /** Desactiva una prioridad (409 si es la predeterminada). */
  desactivar(id: number): Observable<PrioridadIncidenciaDto> {
    return this.http.post<PrioridadIncidenciaDto>(`${this.url}/${id}/desactivar`, null);
  }

  /** Reactiva una prioridad (idempotente). */
  activar(id: number): Observable<PrioridadIncidenciaDto> {
    return this.http.post<PrioridadIncidenciaDto>(`${this.url}/${id}/activar`, null);
  }
}
