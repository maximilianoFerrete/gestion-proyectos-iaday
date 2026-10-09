import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { crearParams } from '../../../core/services/http-params.util';
import {
  EstadoProyectoDto,
  EstadoProyectoRequest,
  FiltrosEstadosProyecto,
} from '../models/estado-proyecto.model';

/**
 * Cliente HTTP de `/api/estados-proyecto`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Injectable({ providedIn: 'root' })
export class EstadosProyectoService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/estados-proyecto`;

  /** Lista los estados ordenados por orden y nombre (incluye el de sistema). */
  listar(filtros: FiltrosEstadosProyecto = {}): Observable<EstadoProyectoDto[]> {
    const params = crearParams({ soloActivos: filtros.soloActivos });
    return this.http.get<EstadoProyectoDto[]>(this.url, { params });
  }

  /** Obtiene un estado por id. */
  obtener(id: number): Observable<EstadoProyectoDto> {
    return this.http.get<EstadoProyectoDto>(`${this.url}/${id}`);
  }

  /** Crea un estado configurable. */
  crear(request: EstadoProyectoRequest): Observable<EstadoProyectoDto> {
    return this.http.post<EstadoProyectoDto>(this.url, request);
  }

  /** Edita un estado configurable. */
  editar(id: number, request: EstadoProyectoRequest): Observable<EstadoProyectoDto> {
    return this.http.put<EstadoProyectoDto>(`${this.url}/${id}`, request);
  }

  /** Marca el estado como inicial (el backend desmarca el anterior). */
  marcarInicial(id: number): Observable<EstadoProyectoDto> {
    return this.http.post<EstadoProyectoDto>(`${this.url}/${id}/marcar-inicial`, null);
  }

  /** Desactiva un estado (idempotente). */
  desactivar(id: number): Observable<EstadoProyectoDto> {
    return this.http.post<EstadoProyectoDto>(`${this.url}/${id}/desactivar`, null);
  }

  /** Reactiva un estado (idempotente). */
  activar(id: number): Observable<EstadoProyectoDto> {
    return this.http.post<EstadoProyectoDto>(`${this.url}/${id}/activar`, null);
  }
}
