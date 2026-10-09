import { Injectable, signal } from '@angular/core';
import { Params } from '@angular/router';

/**
 * Recuerda los query params del último listado de proyectos para que "Volver" desde el detalle
 * conserve filtros, página y orden (P3).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Injectable({ providedIn: 'root' })
export class ProyectosListadoEstadoService {
  private readonly params = signal<Params>({});

  /** Query params del último listado visitado. */
  readonly queryParams = this.params.asReadonly();

  /** Guarda los query params actuales del listado. */
  guardar(params: Params): void {
    this.params.set({ ...params });
  }
}
