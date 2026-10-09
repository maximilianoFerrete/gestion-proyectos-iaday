import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { crearParams } from '../../../core/services/http-params.util';
import {
  EstadoIncidenciaDto,
  EstadoIncidenciaRequest,
  FiltrosEstadosIncidencia,
} from '../models/estado-incidencia.model';

/**
 * Cliente HTTP de `/api/estados-incidencia`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class EstadosIncidenciaService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/estados-incidencia`;

  /** Lista los estados por orden y nombre; por defecto incluye inactivos. */
  listar(filtros: FiltrosEstadosIncidencia = {}): Observable<EstadoIncidenciaDto[]> {
    const params = crearParams({ soloActivos: filtros.soloActivos });
    return this.http.get<EstadoIncidenciaDto[]>(this.url, { params });
  }

  /** Obtiene un estado por id. */
  obtener(id: number): Observable<EstadoIncidenciaDto> {
    return this.http.get<EstadoIncidenciaDto>(`${this.url}/${id}`);
  }

  /** Crea un estado activo. */
  crear(request: EstadoIncidenciaRequest): Observable<EstadoIncidenciaDto> {
    return this.http.post<EstadoIncidenciaDto>(this.url, request);
  }

  /** Edita nombre, descripción y orden de un estado. */
  editar(id: number, request: EstadoIncidenciaRequest): Observable<EstadoIncidenciaDto> {
    return this.http.put<EstadoIncidenciaDto>(`${this.url}/${id}`, request);
  }

  /** Desactiva un estado (409 si es inicial de algún flujo). */
  desactivar(id: number): Observable<EstadoIncidenciaDto> {
    return this.http.post<EstadoIncidenciaDto>(`${this.url}/${id}/desactivar`, null);
  }

  /** Reactiva un estado (idempotente). */
  activar(id: number): Observable<EstadoIncidenciaDto> {
    return this.http.post<EstadoIncidenciaDto>(`${this.url}/${id}/activar`, null);
  }
}
