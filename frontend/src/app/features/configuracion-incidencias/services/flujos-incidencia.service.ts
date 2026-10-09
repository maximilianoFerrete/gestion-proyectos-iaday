import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { FlujoIncidenciaDto, FlujoIncidenciaRequest } from '../models/flujo-incidencia.model';

/**
 * Cliente HTTP de `/api/flujos-incidencia` (los flujos no se desactivan ni eliminan, DA-14).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class FlujosIncidenciaService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/flujos-incidencia`;

  /** Lista los flujos ordenados por nombre (con estados y tipos). */
  listar(): Observable<FlujoIncidenciaDto[]> {
    return this.http.get<FlujoIncidenciaDto[]>(this.url);
  }

  /** Obtiene un flujo por id. */
  obtener(id: number): Observable<FlujoIncidenciaDto> {
    return this.http.get<FlujoIncidenciaDto>(`${this.url}/${id}`);
  }

  /** Crea un flujo con su composición de estados. */
  crear(request: FlujoIncidenciaRequest): Observable<FlujoIncidenciaDto> {
    return this.http.post<FlujoIncidenciaDto>(this.url, request);
  }

  /** Edita un flujo reemplazando la composición completa de estados. */
  editar(id: number, request: FlujoIncidenciaRequest): Observable<FlujoIncidenciaDto> {
    return this.http.put<FlujoIncidenciaDto>(`${this.url}/${id}`, request);
  }
}
