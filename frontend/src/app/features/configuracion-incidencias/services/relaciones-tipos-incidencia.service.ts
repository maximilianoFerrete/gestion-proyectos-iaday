import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { crearParams } from '../../../core/services/http-params.util';
import {
  CrearRelacionTipoIncidenciaRequest,
  FiltrosRelacionesTipos,
  RelacionTipoIncidenciaDto,
} from '../models/relacion-tipo-incidencia.model';

/**
 * Cliente HTTP de `/api/relaciones-tipos-incidencia`. Solo consulta y alta: las relaciones no
 * se modifican ni eliminan (RN-09), por eso no hay métodos `editar` ni `eliminar`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class RelacionesTiposIncidenciaService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/relaciones-tipos-incidencia`;

  /** Lista las relaciones, opcionalmente filtradas por tipo padre y/o tipo hijo. */
  listar(filtros: FiltrosRelacionesTipos = {}): Observable<RelacionTipoIncidenciaDto[]> {
    const params = crearParams({
      tipoPadreId: filtros.tipoPadreId,
      tipoHijoId: filtros.tipoHijoId,
    });
    return this.http.get<RelacionTipoIncidenciaDto[]>(this.url, { params });
  }

  /** Crea una relación tipo padre → tipo hijo (inmutable una vez creada). */
  crear(request: CrearRelacionTipoIncidenciaRequest): Observable<RelacionTipoIncidenciaDto> {
    return this.http.post<RelacionTipoIncidenciaDto>(this.url, request);
  }
}
