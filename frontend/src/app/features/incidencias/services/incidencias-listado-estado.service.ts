import { Injectable, signal } from '@angular/core';
import { Params } from '@angular/router';

/** Query params guardados del listado de un proyecto. */
interface EstadoListado {
  readonly proyectoId: number;
  readonly params: Params;
}

/**
 * Recuerda los query params del último listado de incidencias (por proyecto) para que "Volver"
 * desde el detalle conserve filtros, página y orden (I3).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class IncidenciasListadoEstadoService {
  private readonly estado = signal<EstadoListado | null>(null);

  /** Guarda los query params del listado del proyecto. */
  guardar(proyectoId: number, params: Params): void {
    this.estado.set({ proyectoId, params: { ...params } });
  }

  /** Query params guardados para el proyecto (vacío si son de otro proyecto). */
  queryParams(proyectoId: number | null): Params {
    const estado = this.estado();
    return estado !== null && estado.proyectoId === proyectoId ? estado.params : {};
  }
}
