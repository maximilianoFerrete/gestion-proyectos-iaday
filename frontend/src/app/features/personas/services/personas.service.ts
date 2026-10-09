import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { crearParams } from '../../../core/services/http-params.util';
import { FiltrosPersonas, PersonaDto, PersonaRequest } from '../models/persona.model';

/**
 * Cliente HTTP de `/api/personas`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Injectable({ providedIn: 'root' })
export class PersonasService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/personas`;

  /** Lista personas ordenadas por nombre; por defecto solo activas. */
  listar(filtros: FiltrosPersonas = {}): Observable<PersonaDto[]> {
    const params = crearParams({
      texto: filtros.texto?.trim(),
      incluirInactivas: filtros.incluirInactivas,
    });
    return this.http.get<PersonaDto[]>(this.url, { params });
  }

  /** Obtiene una persona por id. */
  obtener(id: number): Observable<PersonaDto> {
    return this.http.get<PersonaDto>(`${this.url}/${id}`);
  }

  /** Crea una persona. */
  crear(request: PersonaRequest): Observable<PersonaDto> {
    return this.http.post<PersonaDto>(this.url, request);
  }

  /** Edita nombre, email y marca de administrador de una persona. */
  editar(id: number, request: PersonaRequest): Observable<PersonaDto> {
    return this.http.put<PersonaDto>(`${this.url}/${id}`, request);
  }

  /** Desactiva una persona (idempotente). */
  desactivar(id: number): Observable<PersonaDto> {
    return this.http.post<PersonaDto>(`${this.url}/${id}/desactivar`, null);
  }

  /** Reactiva una persona (idempotente). */
  activar(id: number): Observable<PersonaDto> {
    return this.http.post<PersonaDto>(`${this.url}/${id}/activar`, null);
  }
}
