/**
 * Configuración de entorno de desarrollo.
 * apiUrl relativa ("/api"): `ng serve` la redirige al backend mediante proxy.conf.json
 * (destino por defecto http://localhost:5000). Alternativa sin proxy: "http://localhost:5000/api"
 * (requiere CORS para http://localhost:4200 en el backend).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const environment = {
  production: false,
  apiUrl: '/api',
};
