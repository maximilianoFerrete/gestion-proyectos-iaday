import { Injectable, inject } from '@angular/core';
import { Observable, catchError, filter, map, of, switchMap } from 'rxjs';
import {
  ConfirmacionService,
  OpcionesConfirmacion,
} from '../../../core/services/confirmacion.service';
import { NotificacionService } from '../../../core/services/notificacion.service';
import { mensajeErrorAccion } from '../../../shared/forms/errores-servidor';

/**
 * Ejecuta acciones puntuales de las pantallas de configuración (activar, desactivar, marcar
 * predeterminada...): confirmación opcional, toast de éxito o del `detail` del error (409).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class EjecutorAccionesService {
  private readonly confirmacion = inject(ConfirmacionService);
  private readonly notificacion = inject(NotificacionService);

  /** Ejecuta la acción; emite true si tuvo éxito y false si falló (ya notificado). */
  ejecutar<T>(accion$: Observable<T>, mensajeExito: string): Observable<boolean> {
    return accion$.pipe(
      map(() => {
        this.notificacion.exito(mensajeExito);
        return true;
      }),
      catchError((error: unknown) => {
        const mensaje = mensajeErrorAccion(error);
        if (mensaje) {
          this.notificacion.error(mensaje);
        }
        return of(false);
      }),
    );
  }

  /** Pide confirmación y, si se acepta, ejecuta; no emite si el usuario cancela. */
  confirmarYEjecutar<T>(
    opciones: OpcionesConfirmacion,
    accion: () => Observable<T>,
    mensajeExito: string,
  ): Observable<boolean> {
    return this.confirmacion.confirmar(opciones).pipe(
      filter((confirmado) => confirmado),
      switchMap(() => this.ejecutar(accion(), mensajeExito)),
    );
  }
}
