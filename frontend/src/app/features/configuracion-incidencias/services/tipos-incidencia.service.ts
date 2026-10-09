import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { crearParams } from '../../../core/services/http-params.util';
import {
  CrearTipoIncidenciaRequest,
  EditarTipoIncidenciaRequest,
  FiltrosTiposIncidencia,
  TipoIncidenciaDto,
} from '../models/tipo-incidencia.model';

/**
 * Cliente HTTP de `/api/tipos-incidencia`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class TiposIncidenciaService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/tipos-incidencia`;

  /** Lista los tipos ordenados por nivel y nombre; por defecto incluye inactivos. */
  listar(filtros: FiltrosTiposIncidencia = {}): Observable<TipoIncidenciaDto[]> {
    const params = crearParams({ soloActivos: filtros.soloActivos, nivelId: filtros.nivelId });
    return this.http.get<TipoIncidenciaDto[]>(this.url, { params });
  }

  /** Obtiene un tipo por id. */
  obtener(id: number): Observable<TipoIncidenciaDto> {
    return this.http.get<TipoIncidenciaDto>(`${this.url}/${id}`);
  }

  /** Crea un tipo activo en el nivel y flujo indicados. */
  crear(request: CrearTipoIncidenciaRequest): Observable<TipoIncidenciaDto> {
    return this.http.post<TipoIncidenciaDto>(this.url, request);
  }

  /** Edita nombre, descripción y flujo de un tipo (el nivel es inmutable). */
  editar(id: number, request: EditarTipoIncidenciaRequest): Observable<TipoIncidenciaDto> {
    return this.http.put<TipoIncidenciaDto>(`${this.url}/${id}`, request);
  }

  /** Desactiva un tipo (idempotente). */
  desactivar(id: number): Observable<TipoIncidenciaDto> {
    return this.http.post<TipoIncidenciaDto>(`${this.url}/${id}/desactivar`, null);
  }

  /** Reactiva un tipo (idempotente). */
  activar(id: number): Observable<TipoIncidenciaDto> {
    return this.http.post<TipoIncidenciaDto>(`${this.url}/${id}/activar`, null);
  }
}
