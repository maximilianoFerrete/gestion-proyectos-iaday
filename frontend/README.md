# Frontend – Gestión de Proyectos (Angular 22)

SPA Angular (standalone components, signals, Reactive Forms tipados, `OnPush`, zoneless, sin
librería de UI) de la spec `docs/specs/RF-001-proyectos-agrupaciones.md`.

## Requisitos

- Node.js en una versión soportada por Angular 22 (probado con Node 24.20 / npm 12.2).
- Angular CLI local (no hace falta instalarlo global: usar `npx ng ...` o los scripts de npm).

## Scripts

| Comando | Descripción |
|---|---|
| `npm install` | Instala dependencias |
| `npm start` | `ng serve` en http://localhost:4200 con proxy hacia la API |
| `npm run build` | Build de producción en `dist/frontend` |
| `npm test` | Tests unitarios (Vitest, runner por defecto del CLI). `npx ng test --watch=false` para una sola corrida |

## URL de la API y proxy de desarrollo

- La URL base de la API se toma de `environment.apiUrl` (`src/environments/`), expuesta con el
  token `API_URL`. En ambos entornos vale `/api` (relativa).
- En desarrollo, `ng serve` usa `proxy.conf.json` (configurado en `angular.json` →
  `serve.options.proxyConfig`) y redirige `/api/**` a **`http://localhost:5000`**.
  Si el backend corre en otro puerto, cambiar `target` en `proxy.conf.json`.
- Alternativa sin proxy: poner `apiUrl: 'http://localhost:5000/api'` en
  `environment.development.ts` (requiere CORS para `http://localhost:4200` en el backend).
- Producción: `/api` asume mismo origen (o reverse proxy). Si la API está en otro dominio,
  usar la URL absoluta en `environment.ts`.

## Usuario actual (mecanismo provisional, DA-04)

El selector "Usuario actual" de la barra superior guarda el id elegido en `localStorage`
(clave `usuarioActualId`) y `usuarioActualInterceptor` envía el header `X-Usuario-Id` en cada
request a la API. Es solo para desarrollo/demo: el header es falsificable.

## Estructura

```
src/app/
  core/       config (tokens), models (errores, paginado, usuario actual), services
              (UsuarioActualService, NotificacionService, ConfirmacionService),
              interceptors (usuarioActual, error), layout (shell, selector, toasts)
  shared/     components (ConfirmacionDialog, Paginador, BadgeEstado, CampoError, NoEncontrado),
              forms (mensajes de error, errores del servidor), validators, utils
  features/
    proyectos/         P1–P5 (listado, alta/edición, detalle, agrupaciones)
    personas/          P6
    estados-proyecto/  P7 (/configuracion/estados-proyecto)
```
