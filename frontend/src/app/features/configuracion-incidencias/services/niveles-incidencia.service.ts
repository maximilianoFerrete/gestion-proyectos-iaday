import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import {
  CrearNivelIncidenciaRequest,
  EditarNivelIncidenciaRequest,
  NivelIncidenciaDto,
} from '../models/nivel-incidencia.model';

/**
 * Cliente HTTP de `/api/niveles-incidencia` (sin eliminar ni desactivar, RN-04).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class NivelesIncidenciaService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/niveles-incidencia`;

  /** Lista los niveles ordenados por número. */
  listar(): Observable<NivelIncidenciaDto[]> {
    return this.http.get<NivelIncidenciaDto[]>(this.url);
  }

  /** Obtiene un nivel por id. */
  obtener(id: number): Observable<NivelIncidenciaDto> {
    return this.http.get<NivelIncidenciaDto>(`${this.url}/${id}`);
  }

  /** Crea un nivel (requiere permiso Configuracion.Incidencias). */
  crear(request: CrearNivelIncidenciaRequest): Observable<NivelIncidenciaDto> {
    return this.http.post<NivelIncidenciaDto>(this.url, request);
  }

  /** Edita nombre y descripción de un nivel (el número es inmutable). */
  editar(id: number, request: EditarNivelIncidenciaRequest): Observable<NivelIncidenciaDto> {
    return this.http.put<NivelIncidenciaDto>(`${this.url}/${id}`, request);
  }
}
