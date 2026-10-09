/**
 * Respuesta de `GET /api/usuario-actual` (spec 0.4).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export interface UsuarioActualDto {
  readonly identificado: boolean;
  readonly personaId: number | null;
  readonly nombre: string | null;
  readonly email: string | null;
  readonly esAdmin: boolean;
}

/** Usuario anónimo (sin `X-Usuario-Id`). */
export const USUARIO_ANONIMO: UsuarioActualDto = {
  identificado: false,
  personaId: null,
  nombre: null,
  email: null,
  esAdmin: false,
};
