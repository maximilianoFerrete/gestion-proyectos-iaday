/**
 * Permisos conocidos por el frontend (RF-002, spec 0.1). Deben coincidir con las constantes
 * `Permisos` del backend; el front solo los usa para mostrar u ocultar acciones (la
 * autorización real la hace la API).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Agrega `Proyectos.ConfigurarTablero` (RF-005)
 */
export const PERMISOS = {
  configuracionIncidencias: 'Configuracion.Incidencias',
  incidenciasCambiarTipo: 'Incidencias.CambiarTipo',
  /** Modificar las columnas visibles del tablero de seguimiento (RF-005 RN-31). */
  proyectosConfigurarTablero: 'Proyectos.ConfigurarTablero',
} as const;

/** Nombre de un permiso conocido. */
export type Permiso = (typeof PERMISOS)[keyof typeof PERMISOS];
