import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { ALMACENAMIENTO } from '../config/almacenamiento.token';
import { API_URL } from '../config/api-url.token';
import { esErrorApi } from '../models/error-api.model';
import { Permiso } from '../models/permisos.model';
import { USUARIO_ANONIMO, UsuarioActualDto } from '../models/usuario-actual.model';
import { PersonaDto } from '../../features/personas/models/persona.model';
import { PersonasService } from '../../features/personas/services/personas.service';
import { NotificacionService } from './notificacion.service';

/** Clave de localStorage con el id de la persona seleccionada (spec 0.4). */
export const CLAVE_USUARIO_ACTUAL = 'usuarioActualId';

/** Mensaje mostrado cuando la persona seleccionada deja de ser válida (401). */
export const MENSAJE_USUARIO_INVALIDO = 'El usuario seleccionado ya no es válido';

/**
 * Usuario actual provisional (DA-04): guarda el id elegido en localStorage, lo valida con
 * `GET /api/usuario-actual` y expone el rol y los permisos para ocultar acciones.
 * El header `X-Usuario-Id` lo agrega `usuarioActualInterceptor`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Agrega `permisos` y `tienePermiso` (RF-002)
 */
@Injectable({ providedIn: 'root' })
export class UsuarioActualService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);
  private readonly almacenamiento = inject(ALMACENAMIENTO);
  private readonly personasService = inject(PersonasService);
  private readonly notificacion = inject(NotificacionService);

  private readonly idSeleccionado = signal<number | null>(this.leerIdGuardado());
  private readonly usuarioVerificado = signal<UsuarioActualDto>(USUARIO_ANONIMO);
  private readonly personasActivas = signal<readonly PersonaDto[]>([]);

  /** Id de la persona seleccionada (lo usa el interceptor). */
  readonly personaId = this.idSeleccionado.asReadonly();
  /** Datos del usuario según la API. */
  readonly usuario = this.usuarioVerificado.asReadonly();
  /** Personas activas para el selector. */
  readonly personasSeleccionables = this.personasActivas.asReadonly();
  /** Indica si el usuario actual es administrador (solo para mostrar/ocultar acciones). */
  readonly esAdmin = computed(() => this.usuarioVerificado().esAdmin);
  /** Permisos del usuario actual (RF-002 0.1); tolera respuestas sin `permisos`. */
  readonly permisos = computed<ReadonlySet<string>>(
    () => new Set(this.usuarioVerificado().permisos ?? []),
  );

  /** Indica si el usuario tiene el permiso (reactivo: lee el signal `permisos`). */
  tienePermiso(permiso: Permiso): boolean {
    return this.permisos().has(permiso);
  }

  /** Verifica la selección guardada y carga las personas del selector. */
  inicializar(): void {
    this.verificar();
    this.recargarPersonas();
  }

  /** Cambia el usuario actual (null = "Sin usuario") y lo valida con la API. */
  seleccionar(personaId: number | null): void {
    if (personaId === null) {
      this.almacenamiento.removeItem(CLAVE_USUARIO_ACTUAL);
    } else {
      this.almacenamiento.setItem(CLAVE_USUARIO_ACTUAL, String(personaId));
    }
    this.idSeleccionado.set(personaId);
    this.usuarioVerificado.set(USUARIO_ANONIMO);
    this.verificar();
  }

  /** Consulta `GET /api/usuario-actual`; ante 401 limpia la selección. */
  verificar(): void {
    this.http.get<UsuarioActualDto>(`${this.apiUrl}/usuario-actual`).subscribe({
      next: (usuario) => {
        // Descarta respuestas de una selección anterior (cambios rápidos del selector).
        if (usuario.personaId === this.idSeleccionado()) {
          this.usuarioVerificado.set(usuario);
        }
      },
      error: (error: unknown) => {
        if (esErrorApi(error) && error.status === 401) {
          this.limpiar(MENSAJE_USUARIO_INVALIDO);
        }
      },
    });
  }

  /** Limpia la selección; si había una y se indica mensaje, lo muestra como toast. */
  limpiar(mensaje?: string): void {
    const habiaSeleccion = this.idSeleccionado() !== null;
    this.almacenamiento.removeItem(CLAVE_USUARIO_ACTUAL);
    this.idSeleccionado.set(null);
    this.usuarioVerificado.set(USUARIO_ANONIMO);
    if (habiaSeleccion && mensaje) {
      this.notificacion.error(mensaje);
    }
  }

  /** Recarga la lista de personas activas del selector. */
  recargarPersonas(): void {
    this.personasService.listar().subscribe({
      next: (personas) => this.personasActivas.set(personas.filter((p) => p.activo)),
      error: () => this.personasActivas.set([]),
    });
  }

  /** Lee el id guardado en localStorage (null si no hay o no es un entero positivo). */
  private leerIdGuardado(): number | null {
    const valor = this.almacenamiento.getItem(CLAVE_USUARIO_ACTUAL);
    const id = valor === null ? NaN : Number(valor);
    return Number.isInteger(id) && id > 0 ? id : null;
  }
}
