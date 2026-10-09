/**
 * Configuración de entorno de producción.
 * apiUrl: URL base de la API. Por defecto relativa ("/api"): el front se publica junto a la API
 * o detrás de un reverse proxy. Si la API vive en otro origen, usar la URL absoluta
 * (por ejemplo "https://api.midominio.com/api") y habilitar CORS en el backend.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const environment = {
  production: true,
  apiUrl: '/api',
};
