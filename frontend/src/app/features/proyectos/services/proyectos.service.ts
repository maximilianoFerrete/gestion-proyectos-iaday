import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { Paginado } from '../../../core/models/paginado.model';
import { crearParams } from '../../../core/services/http-params.util';
import {
  CrearProyectoRequest,
  EditarProyectoRequest,
  FiltrosProyectos,
  ProyectoDto,
  ProyectoResumenDto,
} from '../models/proyecto.model';

/**
 * Cliente HTTP de `/api/proyectos`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Injectable({ providedIn: 'root' })
export class ProyectosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/proyectos`;

  /** Lista proyectos paginados enviando solo los filtros definidos. */
  listar(filtros: FiltrosProyectos): Observable<Paginado<ProyectoResumenDto>> {
    const params = crearParams({
      texto: filtros.texto?.trim(),
      estadoId: filtros.estadoId,
      responsableId: filtros.responsableId,
      archivados: filtros.archivados,
      pagina: filtros.pagina,
      tamanioPagina: filtros.tamanioPagina,
      ordenarPor: filtros.ordenarPor,
      direccion: filtros.direccion,
    });
    return this.http.get<Paginado<ProyectoResumenDto>>(this.url, { params });
  }

  /** Obtiene el detalle de un proyecto (incluye archivados). */
  obtener(id: number): Observable<ProyectoDto> {
    return this.http.get<ProyectoDto>(`${this.url}/${id}`);
  }

  /** Crea un proyecto en el estado inicial vigente. */
  crear(request: CrearProyectoRequest): Observable<ProyectoDto> {
    return this.http.post<ProyectoDto>(this.url, request);
  }

  /** Edita un proyecto no archivado. */
  editar(id: number, request: EditarProyectoRequest): Observable<ProyectoDto> {
    return this.http.put<ProyectoDto>(`${this.url}/${id}`, request);
  }

  /** Archiva un proyecto (solo administradores, irreversible). */
  archivar(id: number): Observable<ProyectoDto> {
    return this.http.post<ProyectoDto>(`${this.url}/${id}/archivar`, null);
  }
}
