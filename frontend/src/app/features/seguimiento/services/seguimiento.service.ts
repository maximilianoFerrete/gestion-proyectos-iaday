import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/config/api-url.token';
import { ValorParametro, crearParams } from '../../../core/services/http-params.util';
import {
  AlcanceConsulta,
  AmbitoConsulta,
  ConfiguracionTableroDto,
  ConfiguracionTableroRequest,
  ConsultaSeguimiento,
  ConsultaTablero,
  ConsultaTableroProyectos,
  SeguimientoDto,
  TableroIncidenciasDto,
  TableroProyectosDto,
} from '../models/seguimiento.model';

/** Parámetros de ámbito: `agrupacionId` y `sinAgrupacion` excluyentes (gana "sin agrupación"). */
function parametrosAmbito(ambito: AmbitoConsulta): Record<string, ValorParametro> {
  return {
    agrupacionId: ambito.sinAgrupacion ? undefined : ambito.agrupacionId,
    sinAgrupacion: ambito.sinAgrupacion ? true : undefined,
  };
}

/** Parámetros de alcance repetibles (`nivelId=1&nivelId=2`, `tipoId=...`). */
function parametrosAlcance(alcance: Partial<AlcanceConsulta>): Record<string, ValorParametro> {
  return { nivelId: alcance.nivelIds ?? [], tipoId: alcance.tipoIds ?? [] };
}

/**
 * Cliente HTTP del tablero de seguimiento (RF-005): tablero general, resumen de proyecto o
 * agrupación, grilla del tablero y configuración de columnas. Solo envía los parámetros
 * definidos (vacíos y `null` se omiten).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Injectable({ providedIn: 'root' })
export class SeguimientoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  /** `GET /api/seguimiento/proyectos`: indicadores de todos los proyectos (T1). */
  tableroProyectos(
    consulta: Partial<ConsultaTableroProyectos> = {},
  ): Observable<TableroProyectosDto> {
    const params = crearParams({
      texto: consulta.texto?.trim(),
      archivados: consulta.archivados,
      ...parametrosAlcance(consulta),
      pagina: consulta.pagina,
      tamanioPagina: consulta.tamanioPagina,
      ordenarPor: consulta.ordenarPor,
      direccion: consulta.direccion,
    });
    return this.http.get<TableroProyectosDto>(`${this.apiUrl}/seguimiento/proyectos`, { params });
  }

  /** `GET /api/proyectos/{id}/seguimiento`: resumen del proyecto o de una agrupación (T2). */
  seguimiento(
    proyectoId: number,
    consulta: Partial<ConsultaSeguimiento> = {},
  ): Observable<SeguimientoDto> {
    const params = crearParams({ ...parametrosAmbito(consulta), ...parametrosAlcance(consulta) });
    return this.http.get<SeguimientoDto>(this.urlSeguimiento(proyectoId), { params });
  }

  /** `GET /api/proyectos/{id}/seguimiento/tablero`: grilla filtrada (sin paginar, RN-20). */
  tablero(
    proyectoId: number,
    consulta: Partial<ConsultaTablero> = {},
  ): Observable<TableroIncidenciasDto> {
    const texto = consulta.texto?.trim();
    const params = crearParams({
      ...parametrosAmbito(consulta),
      ...parametrosAlcance(consulta),
      indicador: consulta.indicador,
      estadoId: consulta.estadoId,
      texto: texto && texto.length > 0 ? texto : undefined,
    });
    return this.http.get<TableroIncidenciasDto>(`${this.urlSeguimiento(proyectoId)}/tablero`, {
      params,
    });
  }

  /** `GET .../seguimiento/tablero/configuracion`: columnas visibles del proyecto (RN-30). */
  obtenerConfiguracionTablero(proyectoId: number): Observable<ConfiguracionTableroDto> {
    return this.http.get<ConfiguracionTableroDto>(this.urlConfiguracion(proyectoId));
  }

  /** `PUT .../seguimiento/tablero/configuracion` (permiso `Proyectos.ConfigurarTablero`). */
  guardarConfiguracionTablero(
    proyectoId: number,
    request: ConfiguracionTableroRequest,
  ): Observable<ConfiguracionTableroDto> {
    return this.http.put<ConfiguracionTableroDto>(this.urlConfiguracion(proyectoId), request);
  }

  /** URL base del seguimiento de un proyecto. */
  private urlSeguimiento(proyectoId: number): string {
    return `${this.apiUrl}/proyectos/${proyectoId}/seguimiento`;
  }

  /** URL de la configuración del tablero de un proyecto. */
  private urlConfiguracion(proyectoId: number): string {
    return `${this.urlSeguimiento(proyectoId)}/tablero/configuracion`;
  }
}
