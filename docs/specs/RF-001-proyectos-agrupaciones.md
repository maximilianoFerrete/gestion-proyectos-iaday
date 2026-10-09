# RF-001 – Gestión de proyectos y agrupaciones

> Versión: 1.1 (APROBADA por el usuario el 2026-10-09; decisiones DA-01 a DA-15 aprobadas con la propuesta por defecto) · Fecha: 2026-10-09
> Primer requerimiento del proyecto: además del alcance funcional, esta spec fija las **bases
> técnicas** (estructura de solución, convenciones, manejo de errores y usuario actual) que
> deben respetar los RF siguientes.

## Resumen funcional

El usuario necesita registrar proyectos (nombre, descripción, responsable, fechas y estado),
consultarlos y editarlos, y archivarlos cuando ya no se trabajan, sin perder su información
para consultas y reportes. Los estados del proyecto son configurables (agregar, renombrar,
desactivar, ordenar, marcar inicial/final). Dentro de cada proyecto se definen
**Agrupaciones**, un segundo nivel de organización para las incidencias, que se implementarán
en un RF posterior. Para asignar responsables se crea un catálogo mínimo de **Personas**.

## Alcance

- Incluye:
  - Bases técnicas del repositorio (sección 0).
  - ABM mínimo de Personas (alta, consulta, edición, desactivación/reactivación) con marca de
    administrador.
  - Configuración de Estados de proyecto: alta, edición (nombre, descripción, orden, marca
    final), marcar estado inicial, desactivar/reactivar. Estado de sistema "Archivado".
  - Proyectos: alta, consulta (listado con filtros y paginación, detalle), edición y archivado
    (solo administradores, irreversible, deja el proyecto en solo lectura).
  - Agrupaciones dentro de un proyecto: alta, consulta, edición y eliminación.
  - Mecanismo **provisional** de usuario actual y rol (sin login), diseñado para ser reemplazado
    por autenticación real (SSO, usuario/contraseña, 2FA).
- No incluye:
  - Entidad Incidencia ni su ABM (RF posterior). RF01.9–RF01.11 se documentan como
    restricciones en "Impacto en RF futuros" y el modelo de Agrupación queda preparado.
  - Login, contraseñas, SSO, 2FA, gestión de roles más allá de `EsAdmin`.
  - Borrado de proyectos, borrado de estados, borrado físico de personas.
  - Desarchivar proyectos.
  - Reportes (solo se garantiza que los datos archivados se conservan y son consultables).
  - Flujo de transiciones entre estados (cualquier estado activo puede pasar a cualquier otro).
  - Auditoría detallada (historial de cambios). Solo fechas de creación/modificación y datos
    del archivado.
  - Control de concurrencia optimista (ver Decisiones abiertas DA-08).

---

## 0. Bases técnicas (fijadas por este RF)

### 0.1 Nombre de la aplicación y estructura

Nombre propuesto de la solución: **`GestionProyectos`** (a confirmar, DA-01). Base de datos en
LocalDB: **`GestionProyectos`**.

```
db/
  sqlserver/
    scripts/   V###__descripcion.sql          (DDL, idempotentes, transaccionales)
    seed/      S###__descripcion.sql          (datos iniciales, idempotentes)
    aplicar.ps1
  README.md
src/
  GestionProyectos.Domain/
  GestionProyectos.Application/
  GestionProyectos.Infrastructure.SqlServer/
  GestionProyectos.Api/
tests/
  GestionProyectos.Application.Tests/
  GestionProyectos.Api.Tests/
frontend/                                      (Angular CLI, standalone, lazy por feature)
  src/app/core | shared | features/{proyectos, personas, estados-proyecto}
docs/specs/
```

- Backend: ASP.NET Core Web API, versión LTS vigente del SDK instalado (.NET 10 si está
  disponible; si no, .NET 8). Controllers (no minimal APIs). Dapper + `Microsoft.Data.SqlClient`.
  `TimeProvider` para fechas. Validación de DTOs con **FluentValidation** (propuesta; DA-02).
- Tests backend: xUnit + NSubstitute (+ FluentAssertions opcional).
- Frontend: Angular estable vigente, standalone components, signals, Reactive Forms tipados,
  `OnPush`, sin librería de UI (CSS propio) salvo que se confirme Angular Material (DA-03).
  Runner de tests: el que genere el CLI por defecto.
- Nota sobre las skills: la skill `db` está escrita para PostgreSQL. En SQL Server usar
  `OUTPUT INSERTED.*` en lugar de `RETURNING` y `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
  en lugar de `LIMIT`. La skill `frontend` menciona `class-validator`/NestJS: **no aplica** a
  Angular; las validaciones se hacen con `Validators` de Reactive Forms.

### 0.2 Convenciones de API

- Prefijo `/api`, rutas en minúsculas, plural, kebab-case (`/api/estados-proyecto`).
- JSON en camelCase. Fechas sin hora (`DATE`) como `"yyyy-MM-dd"` (`DateOnly` en C#).
  Fechas con hora (`DATETIME2`) en UTC ISO-8601 con `Z` (`"2026-10-09T14:30:00Z"`).
- Textos: el backend aplica `Trim()` a todos los strings de entrada antes de validar y guardar.
  Strings opcionales vacíos tras trim se guardan como `NULL`.
- Acciones que no son CRUD puro se modelan como `POST /recurso/{id}/<accion>`
  (`archivar`, `desactivar`, `activar`, `marcar-inicial`) y devuelven `200` con el recurso
  actualizado.
- Listados paginados devuelven:
  ```json
  { "items": [ ... ], "pagina": 1, "tamanioPagina": 20, "total": 57 }
  ```

### 0.3 Errores (ProblemDetails, RFC 9457)

Manejo global con `IExceptionHandler`. Toda respuesta de error tiene:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflicto",
  "status": 409,
  "detail": "El proyecto PRJ-0003 está archivado y no admite modificaciones.",
  "instance": "/api/proyectos/3",
  "codigo": "PROYECTO_ARCHIVADO",
  "traceId": "00-...."
}
```

Errores de validación de formato (400) usan `ValidationProblemDetails` con `codigo: "VALIDACION"`
y diccionario `errors` (claves en camelCase con el nombre del campo):

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Error de validación",
  "status": 400,
  "codigo": "VALIDACION",
  "errors": {
    "nombre": ["El nombre es obligatorio."],
    "fechaObjetivo": ["La fecha objetivo debe ser mayor o igual a la fecha de inicio."]
  }
}
```

Criterio de códigos:
- `400`: datos de entrada inválidos (formato, obligatorios, largos, fechas, referencias en el
  body a entidades inexistentes, inactivas o no asignables).
- `401`: usuario actual no identificado o inválido cuando la operación lo requiere.
- `403`: usuario identificado sin permiso.
- `404`: el recurso de la URL no existe (o la agrupación no pertenece al proyecto de la URL).
- `409`: la operación choca con el estado actual (duplicados, proyecto archivado, reglas de
  estado inicial/sistema, último administrador).
- `500`: error no controlado (sin detalles internos en `detail`).

Catálogo de códigos (`codigo`):

| Código | HTTP | Cuándo |
|---|---|---|
| `VALIDACION` | 400 | Validación de campos (ver `errors`) |
| `RESPONSABLE_INVALIDO` | 400 | `responsableId` inexistente o persona inactiva (y distinto del actual) |
| `ESTADO_INVALIDO` | 400 | `estadoId` inexistente, inactivo (y distinto del actual) o estado de sistema |
| `USUARIO_NO_IDENTIFICADO` | 401 | Operación que requiere usuario y no se envió `X-Usuario-Id` |
| `USUARIO_INVALIDO` | 401 | `X-Usuario-Id` no numérico, inexistente o de persona inactiva |
| `PERMISO_DENEGADO` | 403 | El usuario actual no es administrador |
| `PERSONA_NO_ENCONTRADA` | 404 | |
| `ESTADO_NO_ENCONTRADO` | 404 | |
| `PROYECTO_NO_ENCONTRADO` | 404 | |
| `AGRUPACION_NO_ENCONTRADA` | 404 | Inexistente o de otro proyecto |
| `PERSONA_EMAIL_DUPLICADO` | 409 | |
| `PERSONA_ULTIMO_ADMIN` | 409 | Quitar admin o desactivar al último administrador activo |
| `ESTADO_NOMBRE_DUPLICADO` | 409 | |
| `ESTADO_SISTEMA_NO_MODIFICABLE` | 409 | Editar/desactivar/activar/marcar inicial el estado "Archivado" |
| `ESTADO_INICIAL_NO_DESACTIVABLE` | 409 | Desactivar el estado inicial |
| `ESTADO_INICIAL_NO_FINAL` | 409 | Marcar final al estado inicial |
| `ESTADO_NO_ELEGIBLE_INICIAL` | 409 | Marcar inicial un estado inactivo o final |
| `PROYECTO_ARCHIVADO` | 409 | Modificar un proyecto archivado o sus agrupaciones, o re-archivar |
| `AGRUPACION_NOMBRE_DUPLICADO` | 409 | Nombre repetido dentro del mismo proyecto |

Backend: jerarquía de excepciones propia en `Application`
(`ExcepcionDominio(codigo)` → `EntidadNoEncontradaException` (404),
`ValidacionNegocioException` (400), `ConflictoException` (409),
`ProyectoArchivadoException : ConflictoException`, `UsuarioNoIdentificadoException` (401),
`PermisoDenegadoException` (403)). El handler global mapea cada tipo a su código HTTP.

### 0.4 Usuario actual y seguridad (mecanismo provisional) — DECISIÓN A CONFIRMAR (DA-04)

Objetivo: poder exigir rol administrador hoy, sin login, y que el día que exista
autenticación real (SSO/OIDC, usuario-contraseña, 2FA) **solo cambie el mecanismo de
autenticación**, no los servicios ni los controllers.

Diseño:

1. **Application** define la abstracción:
   - `IUsuarioActual` con `bool EstaIdentificado`, `int? PersonaId`, `string? Nombre`,
     `bool EsAdmin`.
   - Los servicios de negocio dependen solo de `IUsuarioActual` (por ejemplo `ProyectoService.ArchivarAsync`
     verifica `EsAdmin` y registra `PersonaId` como `ArchivadoPorId`).
2. **Api** implementa la autenticación con el pipeline estándar de ASP.NET Core:
   - Esquema de autenticación `"Provisional"`: un `AuthenticationHandler` que lee el header
     **`X-Usuario-Id`**, busca la Persona (vía un servicio de Application) y, si existe y está
     activa, construye un `ClaimsPrincipal` con claims `sub` = PersonaId, `name` = Nombre y
     `role` = `Admin` si `EsAdmin = 1`.
   - Header ausente → principal anónimo (sin error). Header presente pero no numérico,
     inexistente o persona inactiva → `401 USUARIO_INVALIDO`.
   - `UsuarioActualDesdeClaims : IUsuarioActual` lee el `ClaimsPrincipal` de `HttpContext`.
     **No conoce el header**: cuando llegue el login real se reemplaza el esquema
     `"Provisional"` por JWT/OIDC/cookies y `UsuarioActualDesdeClaims` sigue funcionando.
   - Política de autorización `"SoloAdministradores"` (rol `Admin`), aplicada con
     `[Authorize(Policy = "SoloAdministradores")]` en los endpoints restringidos. Para
     devolver los códigos del catálogo, el `IAuthorizationMiddlewareResultHandler` (o los
     eventos del esquema) responde `401 USUARIO_NO_IDENTIFICADO` si es anónimo y
     `403 PERMISO_DENEGADO` si no es admin. Además el servicio vuelve a verificar
     `IUsuarioActual.EsAdmin` (defensa en profundidad, y permite testear la regla sin HTTP).
   - El esquema provisional se habilita con `Seguridad:ModoProvisional = true`
     (`appsettings.Development.json`). **Advertencia**: el header es falsificable; el
     mecanismo es solo para desarrollo/demo y no debe usarse en producción.
3. **Frontend**:
   - `UsuarioActualService` (core, signal) guarda el `personaId` seleccionado en
     `localStorage` (clave `usuarioActualId`).
   - Selector "Usuario actual" en la barra superior con las personas activas (muestra
     "(admin)" junto a los administradores) y la opción "Sin usuario".
   - `usuarioActualInterceptor` agrega `X-Usuario-Id` a cada request a la API si hay
     usuario seleccionado.
   - Al iniciar la app y al cambiar el selector se llama a `GET /api/usuario-actual`; si
     responde 401 (persona desactivada, por ejemplo) se limpia la selección y se muestra
     "El usuario seleccionado ya no es válido".
   - La UI oculta acciones de admin a no admins, pero la autorización real la hace el backend.

Operaciones que exigen administrador en este RF:
- `POST /api/proyectos/{id}/archivar`.
- Crear/editar una persona con `esAdmin = true`, o cambiar `esAdmin` de una persona
  existente (RN-05).

Todas las demás operaciones se permiten sin usuario identificado (ver DA-05).

---

## Modelo de datos

Todas las tablas son **nuevas**. Esquema `dbo`. Collation de la base: la por defecto de la
instancia, que debe ser **case-insensitive** (`*_CI_AS`); la unicidad de nombres y emails
es insensible a mayúsculas. El backend también compara sin distinguir mayúsculas al validar.

Fechas de auditoría (`FechaCreacion`, `FechaModificacion`) se asignan desde el backend con
`TimeProvider` (UTC). Los `DEFAULT` en BD son solo respaldo.

### Tabla `VersionesEsquema` (nueva, control de scripts)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| VersionesEsquema | Version | NVARCHAR(50) | No | PK (`PK_VersionesEsquema`). Ej.: `V001` |
| VersionesEsquema | Descripcion | NVARCHAR(250) | No | |
| VersionesEsquema | FechaAplicacion | DATETIME2(0) | No | `DF_VersionesEsquema_FechaAplicacion` = `SYSUTCDATETIME()` |

### Tabla `Personas` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| Personas | Id | INT IDENTITY(1,1) | No | `PK_Personas` |
| Personas | Nombre | NVARCHAR(150) | No | Trim, 1–150 caracteres |
| Personas | Email | NVARCHAR(254) | No | Formato email, se guarda en minúsculas; `UQ_Personas_Email` |
| Personas | EsAdmin | BIT | No | `DF_Personas_EsAdmin` = 0 |
| Personas | Activo | BIT | No | `DF_Personas_Activo` = 1 |
| Personas | FechaCreacion | DATETIME2(0) | No | `DF_Personas_FechaCreacion` = `SYSUTCDATETIME()` |
| Personas | FechaModificacion | DATETIME2(0) | Sí | |

### Tabla `EstadosProyecto` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| EstadosProyecto | Id | INT IDENTITY(1,1) | No | `PK_EstadosProyecto` |
| EstadosProyecto | Nombre | NVARCHAR(50) | No | Trim, 1–50; `UQ_EstadosProyecto_Nombre` |
| EstadosProyecto | Descripcion | NVARCHAR(250) | Sí | |
| EstadosProyecto | Orden | INT | No | `CK_EstadosProyecto_Orden`: `Orden BETWEEN 1 AND 9999` |
| EstadosProyecto | EsInicial | BIT | No | `DF_...` = 0 |
| EstadosProyecto | EsFinal | BIT | No | `DF_...` = 0 |
| EstadosProyecto | Activo | BIT | No | `DF_...` = 1 |
| EstadosProyecto | Codigo | NVARCHAR(30) | Sí | `NULL` = estado configurable por el usuario. Con valor = estado de sistema. Único valor en RF-001: `ARCHIVADO` |
| EstadosProyecto | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |
| EstadosProyecto | FechaModificacion | DATETIME2(0) | Sí | |

Constraints e índices:
- `UX_EstadosProyecto_EsInicial`: índice único **filtrado** sobre `(EsInicial) WHERE EsInicial = 1`
  → como máximo un estado inicial. "Exactamente uno" lo garantiza el backend (RN-10/RN-11).
- `UX_EstadosProyecto_Codigo`: índice único filtrado sobre `(Codigo) WHERE Codigo IS NOT NULL`.
- `CK_EstadosProyecto_InicialNoFinal`: `NOT (EsInicial = 1 AND EsFinal = 1)`.
- `CK_EstadosProyecto_InicialActivo`: `EsInicial = 0 OR Activo = 1`.
- `CK_EstadosProyecto_SistemaNoInicial`: `Codigo IS NULL OR EsInicial = 0`.
- Los índices filtrados son específicos de SQL Server (equivalen a índices parciales en
  PostgreSQL); documentarlos en `db/README.md`.

### Secuencia `SQ_Proyectos_Numero` (nueva)

`CREATE SEQUENCE dbo.SQ_Proyectos_Numero AS INT START WITH 1 INCREMENT BY 1 NO CYCLE`.
Se usa para generar el código legible del proyecto (RN-14). Las secuencias son SQL estándar
(existen también en PostgreSQL). Los huecos en la numeración (por rollback) son aceptables.

### Tabla `Proyectos` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| Proyectos | Id | INT IDENTITY(1,1) | No | `PK_Proyectos` |
| Proyectos | Codigo | NVARCHAR(20) | No | `PRJ-` + número de 4 dígitos mínimo (`PRJ-0001`, `PRJ-10000`). Inmutable. `UQ_Proyectos_Codigo` |
| Proyectos | Nombre | NVARCHAR(150) | No | Trim, 1–150. **No único** |
| Proyectos | Descripcion | NVARCHAR(2000) | No | Trim, 1–2000 (ver DA-06) |
| Proyectos | ResponsableId | INT | No | `FK_Proyectos_Personas_ResponsableId` → `Personas(Id)` |
| Proyectos | FechaInicio | DATE | No | |
| Proyectos | FechaObjetivo | DATE | No | `CK_Proyectos_Fechas`: `FechaObjetivo >= FechaInicio` |
| Proyectos | EstadoId | INT | No | `FK_Proyectos_EstadosProyecto_EstadoId` → `EstadosProyecto(Id)` |
| Proyectos | FechaArchivado | DATETIME2(0) | Sí | UTC. No nulo ⇔ proyecto archivado |
| Proyectos | ArchivadoPorId | INT | Sí | `FK_Proyectos_Personas_ArchivadoPorId` → `Personas(Id)` |
| Proyectos | EstadoAlArchivarId | INT | Sí | Estado que tenía al archivarse (para reportes). `FK_Proyectos_EstadosProyecto_EstadoAlArchivarId` |
| Proyectos | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |
| Proyectos | FechaModificacion | DATETIME2(0) | Sí | |

Constraints e índices:
- `CK_Proyectos_Archivado`:
  `(FechaArchivado IS NULL AND ArchivadoPorId IS NULL AND EstadoAlArchivarId IS NULL)
   OR (FechaArchivado IS NOT NULL AND ArchivadoPorId IS NOT NULL AND EstadoAlArchivarId IS NOT NULL)`.
- `IX_Proyectos_ResponsableId`, `IX_Proyectos_EstadoId`, `IX_Proyectos_ArchivadoPorId`,
  `IX_Proyectos_EstadoAlArchivarId` (índices en FKs).
- `IX_Proyectos_FechaArchivado_Codigo` sobre `(FechaArchivado, Codigo)` para el filtro de
  archivados y el orden por defecto del listado.
- `IX_Proyectos_Nombre` sobre `(Nombre)`.
- Todas las FK con `ON DELETE NO ACTION`.
- Consistencia "archivado" (la mantiene el backend en una sola transacción):
  `FechaArchivado IS NOT NULL` ⇔ `EstadoId` = estado con `Codigo = 'ARCHIVADO'`.
  El filtro de archivados del listado usa `FechaArchivado`.

### Tabla `Agrupaciones` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| Agrupaciones | Id | INT IDENTITY(1,1) | No | `PK_Agrupaciones` |
| Agrupaciones | ProyectoId | INT | No | `FK_Agrupaciones_Proyectos_ProyectoId` → `Proyectos(Id)`, `ON DELETE NO ACTION`. Inmutable |
| Agrupaciones | Nombre | NVARCHAR(100) | No | Trim, 1–100. Único por proyecto |
| Agrupaciones | Descripcion | NVARCHAR(1000) | No | Trim, 1–1000 (ver DA-06) |
| Agrupaciones | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |
| Agrupaciones | FechaModificacion | DATETIME2(0) | Sí | |

Constraints e índices:
- `UQ_Agrupaciones_ProyectoId_Nombre` sobre `(ProyectoId, Nombre)` (también sirve de índice
  de la FK).
- `UQ_Agrupaciones_ProyectoId_Id` sobre `(ProyectoId, Id)`: **preparación para Incidencias**.
  Permite que la futura tabla `Incidencias` tenga una FK compuesta
  `(ProyectoId, AgrupacionId) → Agrupaciones(ProyectoId, Id)` que garantiza en la BD que
  incidencia y agrupación pertenecen al mismo proyecto (RF01.11).

### Relaciones

```
Personas 1 ──< Proyectos (ResponsableId)
Personas 1 ──< Proyectos (ArchivadoPorId, opcional)
EstadosProyecto 1 ──< Proyectos (EstadoId)
EstadosProyecto 1 ──< Proyectos (EstadoAlArchivarId, opcional)
Proyectos 1 ──< Agrupaciones (ProyectoId)
[futuro] Agrupaciones (ProyectoId, Id) 1 ──< Incidencias (ProyectoId, AgrupacionId opcional)
```

### Datos semilla (seed, idempotentes, insertar solo si no existe por `Nombre`/`Codigo`/`Email`)

`EstadosProyecto`:

| Nombre | Descripcion | Orden | EsInicial | EsFinal | Activo | Codigo |
|---|---|---|---|---|---|---|
| En planificación | Se está definiendo el alcance y organizando el trabajo. | 10 | 1 | 0 | 1 | NULL |
| Planificado | La planificación está definida y el proyecto está pendiente de inicio. | 20 | 0 | 0 | 1 | NULL |
| En curso | El proyecto se encuentra en ejecución. | 30 | 0 | 0 | 1 | NULL |
| Pausado | El proyecto está suspendido temporalmente. | 40 | 0 | 0 | 1 | NULL |
| Finalizado | El proyecto fue completado. | 50 | 0 | 1 | 1 | NULL |
| Cancelado | Se decidió no continuar con el proyecto. | 60 | 0 | 1 | 1 | NULL |
| Archivado | Proyecto archivado: solo lectura, conservado para consultas y reportes. | 9999 | 0 | 0 | 1 | `ARCHIVADO` |

(Orden de a 10 para facilitar intercalar estados nuevos.)

`Personas`:

| Nombre | Email | EsAdmin | Activo |
|---|---|---|---|
| Administrador | admin@gestionproyectos.local | 1 | 1 |

Scripts propuestos (el DBA puede ajustar la división, respetando el orden):
`V001__crea_tabla_versiones_esquema.sql`, `V002__crea_tabla_personas.sql`,
`V003__crea_tabla_estados_proyecto.sql`, `V004__crea_secuencia_y_tabla_proyectos.sql`,
`V005__crea_tabla_agrupaciones.sql`, `seed/S001__estados_proyecto_iniciales.sql`,
`seed/S002__persona_administrador.sql`.

---

## Contrato API

Headers comunes: `Content-Type: application/json`; `X-Usuario-Id: <int>` opcional (0.4).
Cualquier endpoint puede devolver `401 USUARIO_INVALIDO` si se envía un `X-Usuario-Id`
inválido, y `500` ante errores no controlados; no se repiten en cada fila.

### Resumen

| Método | Ruta | Request | Response | Códigos |
|---|---|---|---|---|
| GET | `/api/usuario-actual` | – | `UsuarioActualDto` | 200, 401 |
| GET | `/api/personas?texto=&incluirInactivas=false` | query | `PersonaDto[]` | 200 |
| GET | `/api/personas/{id}` | – | `PersonaDto` | 200, 404 |
| POST | `/api/personas` | `PersonaRequest` | `PersonaDto` + `Location` | 201, 400, 401, 403, 409 |
| PUT | `/api/personas/{id}` | `PersonaRequest` | `PersonaDto` | 200, 400, 401, 403, 404, 409 |
| POST | `/api/personas/{id}/desactivar` | – | `PersonaDto` | 200, 404, 409 |
| POST | `/api/personas/{id}/activar` | – | `PersonaDto` | 200, 404 |
| GET | `/api/estados-proyecto?soloActivos=false` | query | `EstadoProyectoDto[]` | 200 |
| GET | `/api/estados-proyecto/{id}` | – | `EstadoProyectoDto` | 200, 404 |
| POST | `/api/estados-proyecto` | `EstadoProyectoRequest` | `EstadoProyectoDto` + `Location` | 201, 400, 409 |
| PUT | `/api/estados-proyecto/{id}` | `EstadoProyectoRequest` | `EstadoProyectoDto` | 200, 400, 404, 409 |
| POST | `/api/estados-proyecto/{id}/marcar-inicial` | – | `EstadoProyectoDto` | 200, 404, 409 |
| POST | `/api/estados-proyecto/{id}/desactivar` | – | `EstadoProyectoDto` | 200, 404, 409 |
| POST | `/api/estados-proyecto/{id}/activar` | – | `EstadoProyectoDto` | 200, 404, 409 |
| GET | `/api/proyectos` | query (ver abajo) | `Paginado<ProyectoResumenDto>` | 200, 400 |
| GET | `/api/proyectos/{id}` | – | `ProyectoDto` | 200, 404 |
| POST | `/api/proyectos` | `CrearProyectoRequest` | `ProyectoDto` + `Location` | 201, 400 |
| PUT | `/api/proyectos/{id}` | `EditarProyectoRequest` | `ProyectoDto` | 200, 400, 404, 409 |
| POST | `/api/proyectos/{id}/archivar` | – | `ProyectoDto` | 200, 401, 403, 404, 409 |
| GET | `/api/proyectos/{proyectoId}/agrupaciones` | – | `AgrupacionDto[]` | 200, 404 |
| GET | `/api/proyectos/{proyectoId}/agrupaciones/{id}` | – | `AgrupacionDto` | 200, 404 |
| POST | `/api/proyectos/{proyectoId}/agrupaciones` | `AgrupacionRequest` | `AgrupacionDto` + `Location` | 201, 400, 404, 409 |
| PUT | `/api/proyectos/{proyectoId}/agrupaciones/{id}` | `AgrupacionRequest` | `AgrupacionDto` | 200, 400, 404, 409 |
| DELETE | `/api/proyectos/{proyectoId}/agrupaciones/{id}` | – | – | 204, 404, 409 |

### Usuario actual

`GET /api/usuario-actual`

Con `X-Usuario-Id: 1` válido → 200:
```json
{ "identificado": true, "personaId": 1, "nombre": "Administrador", "email": "admin@gestionproyectos.local", "esAdmin": true }
```
Sin header → 200:
```json
{ "identificado": false, "personaId": null, "nombre": null, "email": null, "esAdmin": false }
```
Header de persona inexistente/inactiva → 401 `USUARIO_INVALIDO`.

### Personas

`PersonaDto`:
```json
{ "id": 2, "nombre": "Ana Gómez", "email": "ana.gomez@empresa.com", "esAdmin": false, "activo": true,
  "fechaCreacion": "2026-10-09T13:00:00Z", "fechaModificacion": null }
```

`GET /api/personas` — `texto` (opcional) filtra por `Nombre` o `Email` que contengan el texto;
`incluirInactivas` (default `false`). Orden: `Nombre` asc. Sin paginación (catálogo chico; ver DA-07).

`POST /api/personas` — `PersonaRequest`:
```json
{ "nombre": "Ana Gómez", "email": "Ana.Gomez@empresa.com", "esAdmin": false }
```
→ 201 con `PersonaDto` (email guardado en minúsculas), header `Location: /api/personas/2`.
Errores: 400 `VALIDACION` (nombre vacío / >150; email vacío / >254 / formato inválido);
409 `PERSONA_EMAIL_DUPLICADO`; `esAdmin = true` sin usuario → 401 `USUARIO_NO_IDENTIFICADO`,
con usuario no admin → 403 `PERMISO_DENEGADO`.

`PUT /api/personas/{id}` — mismo body. Errores: los de POST + 404 `PERSONA_NO_ENCONTRADA`;
cambiar `esAdmin` (en cualquier sentido) exige admin (401/403); pasar `esAdmin` de `true` a
`false` al último admin activo → 409 `PERSONA_ULTIMO_ADMIN`. No modifica `activo`.

`POST /api/personas/{id}/desactivar` → 200 con `activo: false`. Idempotente (si ya estaba
inactiva devuelve 200). Si es el último admin activo → 409 `PERSONA_ULTIMO_ADMIN`.

`POST /api/personas/{id}/activar` → 200 con `activo: true`. Idempotente.

### Estados de proyecto

`EstadoProyectoDto`:
```json
{ "id": 1, "nombre": "En planificación", "descripcion": "Se está definiendo el alcance y organizando el trabajo.",
  "orden": 10, "esInicial": true, "esFinal": false, "activo": true,
  "esSistema": false, "codigo": null, "cantidadProyectos": 4 }
```
`esSistema` = `codigo != null`. `cantidadProyectos` = proyectos con ese `EstadoId` (informativo).

`GET /api/estados-proyecto` — `soloActivos` (default `false`). Devuelve todos, incluido el de
sistema, ordenados por `Orden` asc, `Nombre` asc.

`POST /api/estados-proyecto` — `EstadoProyectoRequest`:
```json
{ "nombre": "En revisión", "descripcion": "Esperando aprobación del cliente.", "orden": 35, "esFinal": false }
```
→ 201. Se crea con `activo = true`, `esInicial = false`, `codigo = null`.
Errores: 400 `VALIDACION` (nombre vacío / >50; descripcion >250; orden fuera de 1–9998);
409 `ESTADO_NOMBRE_DUPLICADO`.

`PUT /api/estados-proyecto/{id}` — mismo body. Errores: 400, 404 `ESTADO_NO_ENCONTRADO`,
409 `ESTADO_NOMBRE_DUPLICADO`, 409 `ESTADO_SISTEMA_NO_MODIFICABLE`,
409 `ESTADO_INICIAL_NO_FINAL` (si es el inicial y `esFinal = true`).

`POST /api/estados-proyecto/{id}/marcar-inicial` → 200 con el estado (`esInicial: true`); el
anterior inicial queda con `esInicial = false` (misma transacción). Si ya era inicial → 200
sin cambios. Errores: 404; 409 `ESTADO_SISTEMA_NO_MODIFICABLE`; 409 `ESTADO_NO_ELEGIBLE_INICIAL`
(inactivo o final).

`POST /api/estados-proyecto/{id}/desactivar` → 200 (idempotente). Errores: 404;
409 `ESTADO_SISTEMA_NO_MODIFICABLE`; 409 `ESTADO_INICIAL_NO_DESACTIVABLE`.

`POST /api/estados-proyecto/{id}/activar` → 200 (idempotente). Errores: 404;
409 `ESTADO_SISTEMA_NO_MODIFICABLE`.

### Proyectos

`GET /api/proyectos` — parámetros de query:

| Parámetro | Tipo | Default | Reglas |
|---|---|---|---|
| `texto` | string | – | Contiene, sobre `Codigo` o `Nombre`. Máx. 150 |
| `estadoId` | int | – | Filtra por `EstadoId` |
| `responsableId` | int | – | Filtra por `ResponsableId` |
| `archivados` | `excluir` \| `solo` \| `incluir` | `excluir` | Filtra por `FechaArchivado` |
| `pagina` | int | 1 | ≥ 1 |
| `tamanioPagina` | int | 20 | 1–100 |
| `ordenarPor` | `codigo` \| `nombre` \| `fechaInicio` \| `fechaObjetivo` \| `estado` \| `responsable` | `codigo` | Lista blanca |
| `direccion` | `asc` \| `desc` | `desc` | |

Valores fuera de rango o no permitidos → 400 `VALIDACION`. Desempate de orden siempre por `Id`.

Response 200 `Paginado<ProyectoResumenDto>`:
```json
{
  "items": [
    { "id": 3, "codigo": "PRJ-0003", "nombre": "Portal clientes",
      "responsable": { "id": 2, "nombre": "Ana Gómez", "activo": true },
      "fechaInicio": "2026-10-01", "fechaObjetivo": "2026-12-15",
      "estado": { "id": 3, "nombre": "En curso", "esFinal": false, "activo": true, "esSistema": false },
      "archivado": false }
  ],
  "pagina": 1, "tamanioPagina": 20, "total": 1
}
```

`GET /api/proyectos/{id}` → 200 `ProyectoDto`:
```json
{
  "id": 3, "codigo": "PRJ-0003", "nombre": "Portal clientes",
  "descripcion": "Nuevo portal de autogestión.",
  "responsable": { "id": 2, "nombre": "Ana Gómez", "activo": true },
  "fechaInicio": "2026-10-01", "fechaObjetivo": "2026-12-15",
  "estado": { "id": 3, "nombre": "En curso", "esFinal": false, "activo": true, "esSistema": false },
  "archivado": false,
  "fechaArchivado": null, "archivadoPor": null, "estadoAlArchivar": null,
  "cantidadAgrupaciones": 2,
  "fechaCreacion": "2026-10-09T13:05:00Z", "fechaModificacion": null
}
```
Para un archivado: `"archivado": true`, `"estado": {"id": 7, "nombre": "Archivado", ..., "esSistema": true}`,
`"fechaArchivado": "2026-11-02T10:00:00Z"`, `"archivadoPor": {"id": 1, "nombre": "Administrador", "activo": true}`,
`"estadoAlArchivar": {"id": 5, "nombre": "Finalizado", "esFinal": true, "activo": true, "esSistema": false}`.
404 `PROYECTO_NO_ENCONTRADO`.

`POST /api/proyectos` — `CrearProyectoRequest`:
```json
{ "nombre": "Portal clientes", "descripcion": "Nuevo portal de autogestión.",
  "responsableId": 2, "fechaInicio": "2026-10-01", "fechaObjetivo": "2026-12-15" }
```
→ 201 `ProyectoDto` con `codigo` generado y `estado` = estado inicial vigente;
`Location: /api/proyectos/3`. No se acepta `estadoId` en el alta (se ignora si viene).
Errores: 400 `VALIDACION` (obligatorios, largos, formato de fecha, `fechaObjetivo < fechaInicio`);
400 `RESPONSABLE_INVALIDO`.

`PUT /api/proyectos/{id}` — `EditarProyectoRequest`:
```json
{ "nombre": "Portal clientes v2", "descripcion": "Nuevo portal de autogestión.",
  "responsableId": 2, "fechaInicio": "2026-10-01", "fechaObjetivo": "2027-01-31", "estadoId": 3 }
```
→ 200 `ProyectoDto`. `codigo` no es editable. Errores: 400 `VALIDACION`;
400 `RESPONSABLE_INVALIDO`; 400 `ESTADO_INVALIDO` (incluye intentar asignar "Archivado");
404 `PROYECTO_NO_ENCONTRADO`; 409 `PROYECTO_ARCHIVADO`.

`POST /api/proyectos/{id}/archivar` (sin body) → 200 `ProyectoDto` archivado.
Errores: 401 `USUARIO_NO_IDENTIFICADO`; 403 `PERMISO_DENEGADO`; 404; 409 `PROYECTO_ARCHIVADO`
(ya archivado).

### Agrupaciones

`AgrupacionDto`:
```json
{ "id": 8, "proyectoId": 3, "nombre": "Módulo pagos", "descripcion": "Incidencias del circuito de pagos.",
  "fechaCreacion": "2026-10-09T13:10:00Z", "fechaModificacion": null }
```

`GET /api/proyectos/{proyectoId}/agrupaciones` → 200, ordenado por `Nombre` asc (también
para proyectos archivados). 404 `PROYECTO_NO_ENCONTRADO`.

`GET /api/proyectos/{proyectoId}/agrupaciones/{id}` → 200. 404 `PROYECTO_NO_ENCONTRADO` o
`AGRUPACION_NO_ENCONTRADA` (incluye agrupación existente de otro proyecto).

`POST /api/proyectos/{proyectoId}/agrupaciones` — `AgrupacionRequest`:
```json
{ "nombre": "Módulo pagos", "descripcion": "Incidencias del circuito de pagos." }
```
→ 201, `Location: /api/proyectos/3/agrupaciones/8`. Errores: 400 `VALIDACION`;
404 `PROYECTO_NO_ENCONTRADO`; 409 `PROYECTO_ARCHIVADO`; 409 `AGRUPACION_NOMBRE_DUPLICADO`.

`PUT /api/proyectos/{proyectoId}/agrupaciones/{id}` — mismo body → 200. Errores: los de POST
+ 404 `AGRUPACION_NO_ENCONTRADA`. Renombrar a su mismo nombre (incluso cambiando mayúsculas)
no es duplicado.

`DELETE /api/proyectos/{proyectoId}/agrupaciones/{id}` → 204. Errores: 404 (proyecto o
agrupación); 409 `PROYECTO_ARCHIVADO`.

---

## Reglas de negocio y validaciones

Personas
- **RN-01** Nombre obligatorio, 1–150 caracteres tras trim.
- **RN-02** Email obligatorio, máx. 254, formato válido; se normaliza a minúsculas y trim.
- **RN-03** El email es único entre todas las personas (activas e inactivas), sin distinguir
  mayúsculas.
- **RN-04** Las personas no se eliminan; se desactivan y se pueden reactivar. Una persona
  inactiva no puede asignarse como responsable ni usarse como usuario actual, pero los
  proyectos que ya la tienen como responsable la conservan.
- **RN-05** Crear una persona con `esAdmin = true` o cambiar el valor de `esAdmin` de una
  persona existente requiere que el usuario actual sea administrador.
- **RN-06** Debe existir siempre al menos un administrador activo: no se puede quitar
  `esAdmin` ni desactivar al último administrador activo.

Estados de proyecto
- **RN-07** Nombre obligatorio, 1–50 tras trim, único sin distinguir mayúsculas (incluye
  inactivos y el de sistema). Descripción opcional, máx. 250. Orden obligatorio 1–9998
  (9999 queda para el estado de sistema). Se permiten órdenes repetidos; se desempata por nombre.
- **RN-08** Los estados no se eliminan; solo se desactivan/reactivan.
- **RN-09** El estado de sistema "Archivado" (`Codigo = ARCHIVADO`) no puede editarse,
  desactivarse, activarse ni marcarse inicial, y no puede asignarse a un proyecto por edición;
  solo mediante la acción archivar.
- **RN-10** Existe exactamente un estado inicial. Marcar un estado como inicial desmarca el
  anterior en la misma transacción. No hay operación para "desmarcar" el inicial sin marcar otro.
- **RN-11** Solo puede marcarse inicial un estado activo, no final y no de sistema.
- **RN-12** El estado inicial no puede desactivarse ni marcarse como final.
- **RN-13** Desactivar un estado no modifica los proyectos que lo tienen: siguen en ese estado
  y se muestran con la marca "(inactivo)". Un estado inactivo no puede asignarse a un proyecto,
  salvo que el proyecto ya lo tenga (editar el proyecto sin cambiar su estado es válido).
  `EsFinal` es una marca de flujo de trabajo: en RF-001 es informativa (se muestra en
  pantallas y listados) y queda disponible para RF futuros (ver DA-09).

Proyectos
- **RN-14** Al crear un proyecto se asigna un código único e inmutable `PRJ-` + número de la
  secuencia `SQ_Proyectos_Numero` con relleno a 4 dígitos (`PRJ-0001`; a partir de 10000 sin
  relleno: `PRJ-10000`). El formateo vive en Application (función pura testeable).
- **RN-15** Nombre obligatorio 1–150 (no único); descripción obligatoria 1–2000; responsable,
  fecha de inicio y fecha objetivo obligatorios.
- **RN-16** `fechaObjetivo >= fechaInicio` (igualdad permitida). Se valida en el alta y en la
  edición. Sin restricción respecto de la fecha actual.
- **RN-17** El responsable debe existir y estar activo; excepción: en la edición se admite
  mantener el responsable actual aunque esté inactivo.
- **RN-18** Al crear, el estado es el estado inicial vigente; el request no lo define.
- **RN-19** En la edición, el `estadoId` debe existir, no ser de sistema y estar activo
  (salvo que sea el estado actual del proyecto). Se permite cualquier transición entre estados
  configurables.
- **RN-20** Solo un administrador puede archivar (401 si no hay usuario, 403 si no es admin).
- **RN-21** Archivar: en una transacción se guarda `EstadoAlArchivarId` = estado actual,
  `EstadoId` = estado `ARCHIVADO`, `FechaArchivado` = ahora (UTC, `TimeProvider`),
  `ArchivadoPorId` = usuario actual. Se puede archivar desde cualquier estado (ver DA-10).
- **RN-22** Un proyecto archivado es de solo lectura total: no se puede editar, re-archivar,
  ni crear/editar/eliminar sus agrupaciones (409 `PROYECTO_ARCHIVADO`). Futuras entidades
  asociadas (incidencias, etc.) deben respetar la misma regla usando el mismo guard.
- **RN-23** No se puede desarchivar (no existe endpoint ni acción).
- **RN-24** Los proyectos archivados se conservan y se consultan: detalle por id y listado con
  `archivados=solo|incluir`. Por defecto el listado los excluye.
- **RN-25** Los proyectos no se eliminan.

Agrupaciones
- **RN-26** Nombre obligatorio 1–100; descripción obligatoria 1–1000.
- **RN-27** Nombre único dentro del proyecto sin distinguir mayúsculas; puede repetirse en
  proyectos distintos.
- **RN-28** Una agrupación pertenece a un único proyecto y no se puede mover a otro
  (`ProyectoId` inmutable; no está en el request).
- **RN-29** Una agrupación solo se opera a través de su proyecto: si `{id}` no pertenece a
  `{proyectoId}` → 404 `AGRUPACION_NO_ENCONTRADA`.
- **RN-30** Eliminar una agrupación es físico y se ejecuta en transacción. Cuando existan
  incidencias, en la misma transacción se les pone `AgrupacionId = NULL` antes de borrar
  (no se eliminan incidencias). En RF-001 no hay incidencias, pero el servicio debe usar
  `IUnitOfWork` para que el paso se agregue sin cambiar el contrato.

Transversales
- **RN-31** Todos los textos de entrada se recortan (trim) antes de validar; un texto
  compuesto solo por espacios equivale a vacío.
- **RN-32** Las validaciones de formato del frontend replican RN-01, 02, 07, 15, 16, 26 con los
  mismos límites; el backend valida siempre (no confía en el frontend).

---

## Pantallas (Angular)

Estructura: `frontend/src/app/features/{proyectos,personas,estados-proyecto}` con
`*.routes.ts` lazy, `models/` (interfaces que reflejan los DTOs de esta spec), `services/`
(HTTP), `pages/`, `components/`. En `core/`: `UsuarioActualService`,
`usuarioActualInterceptor`, `errorInterceptor` (convierte `ProblemDetails` en mensajes),
`NotificacionService` (toasts simples). En `shared/`: `ConfirmacionDialogComponent`
(diálogo propio, sin librería), `PaginadorComponent`, `BadgeEstadoComponent`.

Manejo de errores común a todos los formularios:
- 400 con `errors`: se muestran junto a cada campo (mapeo por nombre de campo); errores sin
  campo, arriba del formulario.
- 400/409 con `codigo` de negocio: se muestra `detail` arriba del formulario.
- 404 al cargar: mensaje "No se encontró el recurso" y botón volver al listado.
- 401/403: toast con `detail`.
- Botón Guardar deshabilitado mientras el form es inválido o hay un envío en curso.

### Layout (`AppComponent` / `ShellComponent`)
- Barra superior: título de la app, menú (Proyectos, Personas, Configuración → Estados de
  proyecto) y selector **"Usuario actual"** (ver 0.4). Ruta por defecto: `/proyectos`.

### P1 – Listado de proyectos · ruta `/proyectos`
- Componentes: `ProyectosListadoPage`, `ProyectosFiltrosComponent`, `ProyectosTablaComponent`,
  `PaginadorComponent`.
- Filtros: Texto (código o nombre, máx. 150, búsqueda con debounce 300 ms), Estado (select con
  todos los estados, inactivos marcados "(inactivo)", incluye "Archivado"), Responsable (select
  de personas incluyendo inactivas), Archivados (select: "No archivados" (default),
  "Solo archivados", "Todos"). Botón "Limpiar filtros".
- Los filtros, página y orden se reflejan en los query params de la URL (permite volver al
  listado con el mismo estado).
- Tabla: Código, Nombre, Responsable, Fecha inicio, Fecha objetivo, Estado (badge; los
  finales con estilo diferenciado; "Archivado" con ícono/estilo de archivo). Encabezados
  ordenables según `ordenarPor`. Clic en fila → detalle.
- Acciones: "Nuevo proyecto" → `/proyectos/nuevo`.
- Vacío: "No hay proyectos que coincidan con los filtros."
- Fechas en formato `dd/MM/yyyy`.

### P2 – Alta de proyecto · ruta `/proyectos/nuevo`
- Componentes: `ProyectoFormPage` (modo alta), `ProyectoFormComponent` (presentación).
- Campos: Nombre (requerido, máx. 150), Descripción (textarea, requerida, máx. 2000, contador
  de caracteres), Responsable (select de personas **activas**, requerido), Fecha de inicio
  (date, requerida), Fecha objetivo (date, requerida). Validador de grupo
  `fechaObjetivoNoAnterior`: error "La fecha objetivo debe ser igual o posterior a la fecha de
  inicio." mostrado en Fecha objetivo.
- No hay campo Estado; texto informativo: "El proyecto se creará en estado <nombre del estado
  inicial>" (obtenido de `GET /api/estados-proyecto?soloActivos=true`).
- Acciones: Guardar → POST; éxito: toast "Proyecto <codigo> creado." y navegar a
  `/proyectos/{id}`. Cancelar → `/proyectos`.

### P3 – Detalle de proyecto · ruta `/proyectos/:id`
- Componentes: `ProyectoDetallePage`, `ProyectoDatosComponent`, `AgrupacionesListaComponent`.
- Muestra: código, nombre, descripción, responsable (con "(inactivo)" si corresponde), fechas,
  estado (badge), fecha de creación/modificación.
- Si `archivado`: banner "Proyecto archivado el dd/MM/yyyy por <nombre>. Estado al archivar:
  <estado>. Solo lectura." y se ocultan todas las acciones de modificación (Editar, Archivar,
  Nueva agrupación, Editar/Eliminar agrupación).
- Acciones (no archivado):
  - "Editar" → `/proyectos/:id/editar`.
  - "Archivar" (visible solo si el usuario actual es admin): abre
    `ConfirmacionDialogComponent` con "¿Archivar el proyecto <codigo> – <nombre>? El proyecto
    quedará en solo lectura y no podrá desarchivarse." → POST archivar; éxito: toast
    "Proyecto archivado." y recarga del detalle.
- Sección **Agrupaciones**: tabla Nombre, Descripción (truncada), acciones Editar / Eliminar;
  botón "Nueva agrupación". Vacío: "El proyecto no tiene agrupaciones."
  - Eliminar: confirmación "¿Eliminar la agrupación <nombre>? Las incidencias asociadas no se
    eliminan: quedarán sin agrupación." → DELETE; éxito: toast "Agrupación eliminada." y recarga
    de la lista.
- Volver → `/proyectos` (conservando query params previos).

### P4 – Edición de proyecto · ruta `/proyectos/:id/editar`
- Mismo `ProyectoFormComponent` en modo edición, con Código (solo lectura) y campo **Estado**
  (select requerido).
- Opciones de Responsable: personas activas + el responsable actual si está inactivo
  (marcado "(inactivo)").
- Opciones de Estado: estados activos no de sistema, en `orden`, + el estado actual si está
  inactivo (marcado "(inactivo)"). Nunca "Archivado".
- Si al cargar el proyecto está archivado: redirigir a `/proyectos/:id` con toast "El proyecto
  está archivado y no se puede editar."
- Guardar → PUT; éxito: toast "Proyecto actualizado." y navegar al detalle. Cancelar → detalle.

### P5 – Alta/edición de agrupación · rutas `/proyectos/:id/agrupaciones/nueva` y `/proyectos/:id/agrupaciones/:agrupacionId/editar`
- Componentes: `AgrupacionFormPage`, `AgrupacionFormComponent`.
- Encabezado con código y nombre del proyecto.
- Campos: Nombre (requerido, máx. 100), Descripción (textarea, requerida, máx. 1000).
- Si el proyecto está archivado: redirigir al detalle con toast "El proyecto está archivado y
  no se puede modificar."
- Guardar → POST/PUT; éxito: toast "Agrupación guardada." y volver al detalle del proyecto.
  409 `AGRUPACION_NOMBRE_DUPLICADO` se muestra en el campo Nombre.

### P6 – Personas · rutas `/personas`, `/personas/nueva`, `/personas/:id/editar`
- Listado (`PersonasListadoPage`): filtro texto, check "Incluir inactivas"; tabla Nombre,
  Email, Admin (Sí/No), Activo (Sí/No); acciones Editar, Desactivar/Activar (con confirmación
  al desactivar: "¿Desactivar a <nombre>? No podrá asignarse como responsable."). Botón
  "Nueva persona".
- Formulario (`PersonaFormPage`): Nombre (requerido, máx. 150), Email (requerido, máx. 254,
  `Validators.email`), Administrador (checkbox; **deshabilitado** si el usuario actual no es
  admin, con ayuda "Solo un administrador puede cambiar este valor").
- 409 `PERSONA_EMAIL_DUPLICADO` en el campo Email; 409 `PERSONA_ULTIMO_ADMIN` arriba del form o
  como toast en el listado.
- Tras desactivar a la persona seleccionada como usuario actual, el selector se limpia.

### P7 – Estados de proyecto · rutas `/configuracion/estados-proyecto`, `/configuracion/estados-proyecto/nuevo`, `/configuracion/estados-proyecto/:id/editar`
- Listado (`EstadosProyectoListadoPage`): tabla ordenada por Orden con columnas Orden, Nombre,
  Descripción, Inicial (marca), Final (marca), Activo, Sistema (marca), Proyectos (cantidad).
  Acciones por fila:
  - Editar (oculto en el de sistema).
  - "Marcar como inicial" (visible si activo, no final, no sistema y no es ya el inicial);
    confirmación "Los nuevos proyectos se crearán en estado <nombre>."
  - Desactivar (oculto en inicial y sistema); si `cantidadProyectos > 0` la confirmación
    avisa: "Hay N proyectos en este estado; lo conservarán, pero no podrá asignarse a otros."
  - Activar (en inactivos no de sistema).
  - Botón "Nuevo estado".
- Formulario (`EstadoProyectoFormPage`): Nombre (requerido, máx. 50), Descripción (máx. 250),
  Orden (requerido, entero 1–9998; en alta se sugiere el mayor orden de estados no de sistema
  + 10, tope 9998), Es final (checkbox; deshabilitado si el estado es el inicial, con ayuda
  "El estado inicial no puede ser final").

---

## Criterios de aceptación

Personas
- **CA-01** Dado un usuario admin, cuando crea la persona "Ana Gómez" / "Ana.Gomez@Empresa.com",
  entonces recibe 201 y la persona aparece en `/personas` con email `ana.gomez@empresa.com`.
  (POST /api/personas, P6)
- **CA-02** Dado que existe `ana.gomez@empresa.com`, cuando se crea otra persona con
  `ANA.GOMEZ@empresa.com`, entonces recibe 409 `PERSONA_EMAIL_DUPLICADO` y el form lo muestra
  en Email. (POST /api/personas, P6)
- **CA-03** Dado un usuario actual no admin, cuando intenta crear una persona con
  `esAdmin = true`, entonces recibe 403 `PERMISO_DENEGADO`; y en P6 el checkbox Administrador
  está deshabilitado. (POST /api/personas, P6)
- **CA-04** Dado que "Administrador" es el único admin activo, cuando se lo intenta desactivar o
  quitarle `esAdmin`, entonces recibe 409 `PERSONA_ULTIMO_ADMIN`.
  (POST /desactivar, PUT /api/personas/{id})
- **CA-05** Dada una persona inactiva, cuando se abre el alta de proyecto, entonces no figura en
  el select Responsable; y un POST con su id recibe 400 `RESPONSABLE_INVALIDO`. (P2, POST /api/proyectos)

Estados
- **CA-06** Dada la instalación inicial, cuando se consulta `GET /api/estados-proyecto`,
  entonces se obtienen los 6 estados del RF más "Archivado", con "En planificación" como único
  inicial y "Finalizado" y "Cancelado" como finales. (GET, P7)
- **CA-07** Cuando se crea el estado "En revisión" con orden 35, entonces aparece entre
  "En curso" y "Pausado" en P7 y en el select de estado de P4. (POST, P7, P4)
- **CA-08** Cuando se renombra "Pausado" a "En pausa", entonces los proyectos en ese estado
  muestran "En pausa" en listado y detalle. (PUT, P1, P3)
- **CA-09** Cuando se crea o renombra un estado con un nombre existente (sin importar
  mayúsculas), entonces recibe 409 `ESTADO_NOMBRE_DUPLICADO`. (POST/PUT)
- **CA-10** Cuando se marca "Planificado" como inicial, entonces "En planificación" deja de
  serlo y un proyecto nuevo se crea en "Planificado". (POST marcar-inicial, POST /api/proyectos)
- **CA-11** Cuando se intenta marcar como inicial un estado final, inactivo o "Archivado",
  entonces recibe 409 (`ESTADO_NO_ELEGIBLE_INICIAL` / `ESTADO_SISTEMA_NO_MODIFICABLE`) y la
  acción no se ofrece en P7. (POST marcar-inicial, P7)
- **CA-12** Cuando se intenta desactivar el estado inicial, entonces recibe 409
  `ESTADO_INICIAL_NO_DESACTIVABLE` y la acción no se ofrece en P7.
- **CA-13** Dado un proyecto en "Pausado", cuando se desactiva "Pausado", entonces el proyecto
  sigue en "Pausado (inactivo)"; puede editarse sin cambiar el estado; "Pausado" no se ofrece
  para otros proyectos; y un PUT de otro proyecto con ese `estadoId` recibe 400 `ESTADO_INVALIDO`.
  (POST desactivar, PUT proyectos, P4)
- **CA-14** Cuando se intenta editar, desactivar o activar "Archivado", entonces recibe 409
  `ESTADO_SISTEMA_NO_MODIFICABLE`. (PUT/POST estados)

Proyectos
- **CA-15** Cuando se crea un proyecto válido, entonces recibe 201 con código `PRJ-0001` (primer
  proyecto) y estado "En planificación"; un segundo proyecto recibe `PRJ-0002`. (POST, P2)
- **CA-16** Cuando se crea un proyecto con el mismo nombre que otro existente, entonces se crea
  sin error con un código distinto. (POST)
- **CA-17** Cuando se envía fecha objetivo anterior a la de inicio, entonces el form de P2/P4
  muestra el error y no permite guardar; y la API responde 400 `VALIDACION` con error en
  `fechaObjetivo`. Con fechas iguales, se acepta. (POST/PUT, P2, P4)
- **CA-18** Cuando se omite cualquier campo obligatorio (nombre, descripción, responsable,
  fechas), entonces el form lo marca y la API responde 400 `VALIDACION`. (POST/PUT, P2, P4)
- **CA-19** Cuando se edita nombre, responsable, fechas y estado de un proyecto no archivado,
  entonces recibe 200 y el detalle muestra los nuevos valores; el código no cambia. (PUT, P4, P3)
- **CA-20** Cuando se intenta asignar por PUT el estado "Archivado", entonces recibe 400
  `ESTADO_INVALIDO`; y "Archivado" no aparece en el select de P4. (PUT, P4)
- **CA-21** Dado un usuario actual admin, cuando archiva un proyecto en "Finalizado", entonces
  recibe 200, el proyecto queda en "Archivado" con `fechaArchivado`, `archivadoPor` y
  `estadoAlArchivar = Finalizado`, y P3 muestra el banner de solo lectura sin acciones. (POST archivar, P3)
- **CA-22** Dado un usuario no admin, cuando llama a archivar, entonces recibe 403
  `PERMISO_DENEGADO`; sin usuario, 401 `USUARIO_NO_IDENTIFICADO`; y el botón Archivar no se
  muestra en P3. (POST archivar, P3)
- **CA-23** Dado un proyecto archivado, cuando se intenta editarlo, re-archivarlo o crear,
  editar o eliminar una de sus agrupaciones, entonces recibe 409 `PROYECTO_ARCHIVADO` y no se
  modifica nada en la BD. (PUT, POST archivar, POST/PUT/DELETE agrupaciones)
- **CA-24** Dado un proyecto archivado, cuando se navega a `/proyectos/:id/editar` o a las
  rutas de agrupación, entonces se redirige al detalle con el mensaje correspondiente. (P4, P5)
- **CA-25** Dados proyectos archivados y no archivados, cuando se abre P1 sin filtros, entonces
  solo se listan los no archivados; con "Solo archivados" solo los archivados; con "Todos",
  ambos. (GET /api/proyectos, P1)
- **CA-26** Cuando se filtra por texto "prj-0003" o por parte del nombre, por estado o por
  responsable, entonces el listado y el `total` reflejan el filtro y la paginación respeta
  `tamanioPagina`. (GET /api/proyectos, P1)
- **CA-27** Cuando se piden parámetros inválidos (`tamanioPagina=500`, `ordenarPor=foo`,
  `archivados=x`), entonces recibe 400 `VALIDACION`. (GET /api/proyectos)
- **CA-28** Cuando se consulta un proyecto inexistente, entonces recibe 404
  `PROYECTO_NO_ENCONTRADO` y P3 muestra "No se encontró el recurso". (GET, P3)

Agrupaciones
- **CA-29** Dado un proyecto no archivado, cuando se crea la agrupación "Módulo pagos" con
  descripción, entonces recibe 201 y aparece en la sección Agrupaciones de P3. (POST, P5, P3)
- **CA-30** Cuando se crea o renombra una agrupación con un nombre ya usado en el mismo
  proyecto (sin importar mayúsculas), entonces recibe 409 `AGRUPACION_NOMBRE_DUPLICADO`; el
  mismo nombre en otro proyecto se acepta. (POST/PUT, P5)
- **CA-31** Cuando se elimina una agrupación confirmando el diálogo, entonces recibe 204 y deja
  de aparecer en P3. (DELETE, P3)
- **CA-32** Cuando se accede a una agrupación por un `proyectoId` que no es el suyo, entonces
  recibe 404 `AGRUPACION_NO_ENCONTRADA`. (GET/PUT/DELETE agrupaciones)
- **CA-33** Dado un proyecto archivado, cuando se consulta su lista de agrupaciones, entonces
  se obtiene 200 con las agrupaciones (solo lectura). (GET agrupaciones, P3)

Usuario actual
- **CA-34** Cuando se selecciona una persona en "Usuario actual", entonces todas las requests
  llevan `X-Usuario-Id` y la selección persiste al recargar la página; con "Sin usuario" no se
  envía el header. (Interceptor, layout)
- **CA-35** Cuando se envía `X-Usuario-Id` de una persona inactiva o inexistente, entonces la
  API responde 401 `USUARIO_INVALIDO` y el front limpia la selección. (GET /api/usuario-actual, layout)

---

## Casos de prueba unitarios sugeridos

Convención backend: `Metodo_Escenario_ResultadoEsperado`, AAA, NSubstitute, `FakeTimeProvider`.
Cada test referencia la RN/CA que cubre.

### Backend – Application (servicios y validadores)

`FormateadorCodigoProyecto` (función pura)
| Caso | Resultado |
|---|---|
| Formatear(1) | `PRJ-0001` (RN-14) |
| Formatear(9999) | `PRJ-9999` |
| Formatear(10000) | `PRJ-10000` |
| Formatear(0) o negativo | `ArgumentOutOfRangeException` |

`PersonaService`
| Caso | Resultado |
|---|---|
| Crear válido con email en mayúsculas y espacios | Guarda email trim + minúsculas (RN-02) |
| Crear con email existente (otra capitalización) | `ConflictoException PERSONA_EMAIL_DUPLICADO` (RN-03) |
| Crear con `esAdmin=true`, usuario no identificado | `UsuarioNoIdentificadoException` (RN-05) |
| Crear con `esAdmin=true`, usuario no admin | `PermisoDenegadoException` (RN-05) |
| Crear con `esAdmin=true`, usuario admin | Crea admin |
| Editar sin cambiar `esAdmin`, usuario no admin | OK (RN-05) |
| Editar quitando `esAdmin` al último admin activo | `ConflictoException PERSONA_ULTIMO_ADMIN` (RN-06) |
| Editar quitando `esAdmin` habiendo otro admin activo | OK |
| Desactivar último admin activo | `PERSONA_ULTIMO_ADMIN` (RN-06) |
| Desactivar persona ya inactiva | Devuelve persona sin error (idempotente) |
| Editar id inexistente | `EntidadNoEncontradaException PERSONA_NO_ENCONTRADA` |

`PersonaRequestValidator`
| Caso | Resultado |
|---|---|
| Nombre vacío / solo espacios / 151 caracteres | Error en `nombre` (RN-01, RN-31) |
| Email vacío / formato inválido / 255 caracteres | Error en `email` (RN-02) |
| Nombre de 150 y email válido | Sin errores (límite) |

`EstadoProyectoService`
| Caso | Resultado |
|---|---|
| Crear con nombre duplicado (otra capitalización) | `ESTADO_NOMBRE_DUPLICADO` (RN-07) |
| Crear válido | `activo=true`, `esInicial=false`, `codigo=null` |
| Editar estado de sistema | `ESTADO_SISTEMA_NO_MODIFICABLE` (RN-09) |
| Editar renombrando a su mismo nombre con otras mayúsculas | OK |
| Editar estado inicial con `esFinal=true` | `ESTADO_INICIAL_NO_FINAL` (RN-12) |
| MarcarInicial de estado activo no final | Desmarca el anterior y marca el nuevo en una transacción (verifica llamadas al repo y commit del `IUnitOfWork`) (RN-10) |
| MarcarInicial de estado ya inicial | Sin cambios, sin escritura |
| MarcarInicial de estado inactivo / final | `ESTADO_NO_ELEGIBLE_INICIAL` (RN-11) |
| MarcarInicial de estado sistema | `ESTADO_SISTEMA_NO_MODIFICABLE` (RN-09) |
| Desactivar estado inicial | `ESTADO_INICIAL_NO_DESACTIVABLE` (RN-12) |
| Desactivar estado con proyectos | OK, no modifica proyectos (RN-13) |
| Desactivar/activar estado de sistema | `ESTADO_SISTEMA_NO_MODIFICABLE` (RN-09) |
| Id inexistente en cualquier operación | `ESTADO_NO_ENCONTRADO` |

`EstadoProyectoRequestValidator`
| Caso | Resultado |
|---|---|
| Nombre vacío / 51 caracteres | Error `nombre` |
| Descripción 251 caracteres | Error `descripcion` |
| Orden 0 / 9999 | Error `orden`; 1 y 9998 válidos (RN-07) |

`ProyectoService`
| Caso | Resultado |
|---|---|
| Crear válido | Estado = inicial vigente; código formateado desde la secuencia; `FechaCreacion` = `FakeTimeProvider` (RN-14, RN-18) |
| Crear con responsable inexistente / inactivo | `ValidacionNegocioException RESPONSABLE_INVALIDO` (RN-17) |
| Crear con nombre igual a otro proyecto | OK, no consulta unicidad de nombre |
| Editar proyecto archivado | `ProyectoArchivadoException` y no llama a `ActualizarAsync` (RN-22) |
| Editar manteniendo responsable actual inactivo | OK (RN-17) |
| Editar cambiando a responsable inactivo | `RESPONSABLE_INVALIDO` |
| Editar manteniendo estado actual inactivo | OK (RN-13, RN-19) |
| Editar cambiando a estado inactivo | `ESTADO_INVALIDO` |
| Editar con estado "Archivado" | `ESTADO_INVALIDO` (RN-09, RN-19) |
| Editar con estado inexistente | `ESTADO_INVALIDO` |
| Editar id inexistente | `PROYECTO_NO_ENCONTRADO` |
| Archivar sin usuario | `UsuarioNoIdentificadoException` (RN-20) |
| Archivar usuario no admin | `PermisoDenegadoException` (RN-20) |
| Archivar admin, proyecto en "Finalizado" | `EstadoId`=Archivado, `EstadoAlArchivarId`=Finalizado, `FechaArchivado`=now, `ArchivadoPorId`=usuario (RN-21) |
| Archivar proyecto ya archivado | `ProyectoArchivadoException` (RN-22) |
| Listar con defaults | Pasa al repo `archivados=excluir`, página 1, tamaño 20, orden `codigo desc` (RN-24) |

`ProyectoRequestValidator` (crear y editar)
| Caso | Resultado |
|---|---|
| `fechaObjetivo` < `fechaInicio` | Error `fechaObjetivo` (RN-16) |
| `fechaObjetivo` == `fechaInicio` | Válido (límite) |
| Nombre vacío / 151; descripción vacía / 2001 | Errores (RN-15) |
| `responsableId` 0 o ausente; fechas ausentes | Errores (RN-15) |
| Editar sin `estadoId` | Error `estadoId` |

`ListarProyectosQueryValidator`
| Caso | Resultado |
|---|---|
| `pagina=0`, `tamanioPagina=0/101`, `ordenarPor=foo`, `direccion=x`, `archivados=x`, `texto` 151 | Error en el campo |
| `tamanioPagina=100` | Válido (límite) |

`AgrupacionService`
| Caso | Resultado |
|---|---|
| Crear en proyecto inexistente | `PROYECTO_NO_ENCONTRADO` |
| Crear en proyecto archivado | `ProyectoArchivadoException` (RN-22) |
| Crear con nombre duplicado en el proyecto (otra capitalización) | `AGRUPACION_NOMBRE_DUPLICADO` (RN-27) |
| Crear con nombre usado en otro proyecto | OK (RN-27) |
| Editar agrupación de otro proyecto | `AGRUPACION_NO_ENCONTRADA` (RN-29) |
| Editar renombrando a su mismo nombre | OK |
| Editar / eliminar en proyecto archivado | `ProyectoArchivadoException` (RN-22) |
| Eliminar válido | Llama a eliminar dentro de `IUnitOfWork` y hace commit (RN-30) |
| Eliminar inexistente | `AGRUPACION_NO_ENCONTRADA` |
| Listar en proyecto archivado | Devuelve la lista (CA-33) |

`AgrupacionRequestValidator`: nombre vacío/101, descripción vacía/1001 → errores (RN-26).

### Backend – Api (controllers con servicios mockeados y handler de errores)

| Caso | Resultado |
|---|---|
| POST proyectos válido | 201, header `Location` `/api/proyectos/{id}`, body `ProyectoDto` |
| GET proyecto: servicio lanza `EntidadNoEncontradaException` | 404 ProblemDetails con `codigo` |
| PUT proyecto: `ProyectoArchivadoException` | 409 `PROYECTO_ARCHIVADO` |
| Validación falla | 400 `ValidationProblemDetails` con `errors` en camelCase y `codigo=VALIDACION` |
| `ValidacionNegocioException RESPONSABLE_INVALIDO` | 400 con `codigo` |
| `UsuarioNoIdentificadoException` / `PermisoDenegadoException` | 401 / 403 |
| Excepción no controlada | 500 sin detalle interno |
| DELETE agrupación OK | 204 sin body |
| POST acciones (`archivar`, `desactivar`, `marcar-inicial`) | 200 con DTO |
| Endpoint archivar tiene `[Authorize(Policy="SoloAdministradores")]` | Verificado por reflexión |

Autenticación provisional (`ProvisionalAuthenticationHandler`, con lookup mockeado)
| Caso | Resultado |
|---|---|
| Sin header | `NoResult` (anónimo) |
| Header no numérico / persona inexistente / inactiva | `Fail` → 401 `USUARIO_INVALIDO` |
| Persona activa admin | Principal con `sub`, `name` y rol `Admin` |
| Persona activa no admin | Principal sin rol `Admin` |

`UsuarioActualDesdeClaims`: principal anónimo → `EstaIdentificado=false`, `EsAdmin=false`;
principal con rol Admin → `EsAdmin=true`, `PersonaId` correcto.

### Frontend (Angular)

Servicios HTTP (`HttpTestingController`)
| Caso | Resultado |
|---|---|
| `ProyectosService.listar(filtros)` | GET `/api/proyectos` con query params solo de filtros definidos |
| `ProyectosService.crear/editar/archivar` | POST/PUT/POST con URL y body exactos del contrato |
| `AgrupacionesService.eliminar(3, 8)` | DELETE `/api/proyectos/3/agrupaciones/8` |
| `EstadosProyectoService.marcarInicial(2)` | POST `/api/estados-proyecto/2/marcar-inicial` |
| `PersonasService.listar({incluirInactivas:true})` | GET con `incluirInactivas=true` |

Core
| Caso | Resultado |
|---|---|
| `usuarioActualInterceptor` con usuario seleccionado | Agrega `X-Usuario-Id` |
| `usuarioActualInterceptor` sin usuario | No agrega el header |
| `UsuarioActualService.seleccionar(id)` | Persiste en `localStorage`; al reiniciar lo recupera |
| `UsuarioActualService` ante 401 de `/api/usuario-actual` | Limpia selección |
| `errorInterceptor` con ProblemDetails 409 | Expone `codigo` y `detail` al llamador |
| Validador `fechaObjetivoNoAnterior` | Error si objetivo < inicio; válido si igual o posterior o si falta alguna |

Componentes (servicios mockeados)
| Caso | Resultado |
|---|---|
| `ProyectoFormComponent` alta: campos vacíos | Form inválido, Guardar deshabilitado (CA-18) |
| `ProyectoFormComponent`: nombre 151 / descripción 2001 | Errores `maxlength` |
| `ProyectoFormComponent`: fechas invertidas | Muestra mensaje de fecha objetivo (CA-17) |
| `ProyectoFormComponent` alta | No muestra campo Estado; muestra nombre del estado inicial |
| `ProyectoFormComponent` edición | Opciones de estado sin "Archivado" ni inactivos, salvo el actual inactivo (CA-13, CA-20) |
| `ProyectoFormComponent` edición | Responsable actual inactivo presente como opción "(inactivo)" |
| `ProyectoDetallePage` proyecto archivado | Banner visible; sin botones Editar/Archivar/Nueva agrupación/Eliminar (CA-21, CA-23) |
| `ProyectoDetallePage` usuario no admin | Sin botón Archivar (CA-22) |
| `ProyectoDetallePage` usuario admin, confirmar archivado | Llama `archivar` y recarga |
| `ProyectoDetallePage` cancelar diálogo eliminar agrupación | No llama `eliminar` |
| `ProyectoEditarPage` proyecto archivado | Redirige al detalle (CA-24) |
| `ProyectosListadoPage` | Filtro Archivados por defecto "No archivados"; cambios de filtro actualizan query params y vuelven a página 1 (CA-25) |
| `AgrupacionFormComponent` | Nombre requerido/100; descripción requerida/1000; 409 duplicado mostrado en Nombre (CA-30) |
| `PersonaFormComponent` usuario no admin | Checkbox Administrador deshabilitado (CA-03) |
| `EstadosProyectoListadoPage` | Sin acción Desactivar en el inicial ni en sistema; sin "Marcar como inicial" en finales/inactivos/sistema (CA-11, CA-12) |
| `EstadoProyectoFormComponent` estado inicial | Checkbox Es final deshabilitado |

---

## Impacto por agente

- **BD (`dba-sql`)**:
  - Crear `db/README.md` (base `GestionProyectos` en `(localdb)\MSSQLLocalDB`, cómo aplicar,
    construcciones específicas de SQL Server usadas: índices filtrados, `SEQUENCE`,
    `IDENTITY`, `SYSUTCDATETIME()`), `db/sqlserver/aplicar.ps1`.
  - Scripts V001–V005 y seeds S001–S002 según "Modelo de datos", con todas las constraints e
    índices nombrados. Verificar que la collation de la base sea `*_CI_AS`.
- **Backend (`dev-backend`)**:
  - Crear la solución y los 4 proyectos (0.1) con `<Nullable>enable</Nullable>`.
  - Domain: `Persona`, `EstadoProyecto`, `Proyecto`, `Agrupacion` con métodos de dominio
    (`Proyecto.Archivar(...)`, `Proyecto.AsegurarEditable()`), constante `CodigosEstado.Archivado`.
  - Application: DTOs (records) del contrato, validadores, servicios (`PersonaService`,
    `EstadoProyectoService`, `ProyectoService`, `AgrupacionService`), `FormateadorCodigoProyecto`,
    interfaces `IPersonaRepository`, `IEstadoProyectoRepository`, `IProyectoRepository`
    (incluye `ObtenerSiguienteNumeroAsync`), `IAgrupacionRepository`, `IUnitOfWork`,
    `IDbConnectionFactory`, `IUsuarioActual`, jerarquía de excepciones (0.3).
  - Infrastructure.SqlServer: repositorios Dapper, `AddInfraestructuraSqlServer`, listado con
    `OFFSET/FETCH` y `ORDER BY` armado solo desde lista blanca.
  - Api: controllers (`UsuarioActualController`, `PersonasController`,
    `EstadosProyectoController`, `ProyectosController`, `AgrupacionesController`),
    `IExceptionHandler` → ProblemDetails con `codigo`, esquema de autenticación provisional,
    política `SoloAdministradores`, CORS `http://localhost:4200` (exponer header `Location`),
    Swagger en desarrollo, serialización `DateOnly` como `yyyy-MM-dd`.
- **Frontend (`dev-frontend`)**:
  - Crear el proyecto Angular en `frontend/`, `environment.apiUrl`, layout con menú y selector
    de usuario actual, interceptores, shared components.
  - Features `proyectos` (P1–P5), `personas` (P6), `estados-proyecto` (P7) con lazy routes.
- **Tests (`dev-tests`)**:
  - Proyectos `tests/GestionProyectos.Application.Tests` y `tests/GestionProyectos.Api.Tests`
    (xUnit + NSubstitute + `Microsoft.Extensions.TimeProvider.Testing`).
  - Specs Angular según la sección anterior. Trazabilidad RN/CA en cada test.
  - Opcional (consultar al orquestador): tests de integración de repositorios contra LocalDB,
    en particular el listado con filtros y el swap de estado inicial con el índice filtrado.

---

## Impacto en RF futuros (Incidencias)

Restricciones que el RF de Incidencias **debe** respetar (derivadas de RF01.9–RF01.11 y de
las decisiones de este RF):

1. La tabla `Incidencias` tendrá `ProyectoId INT NOT NULL` (FK a `Proyectos`) y
   `AgrupacionId INT NULL`.
2. FK compuesta `FK_Incidencias_Agrupaciones` `(ProyectoId, AgrupacionId)` →
   `Agrupaciones(ProyectoId, Id)` (posible gracias a `UQ_Agrupaciones_ProyectoId_Id`). Con
   `AgrupacionId` nulo la FK no se evalúa, lo que permite incidencias sin agrupación
   (RF01.10) y garantiza en la BD que agrupación e incidencia son del mismo proyecto (RF01.11).
   Además se valida en el servicio para devolver un error de negocio claro.
3. **No** usar `ON DELETE SET NULL` en esa FK (pondría también `ProyectoId` en nulo). La
   eliminación de una agrupación debe, en la misma transacción de RN-30, ejecutar
   `UPDATE Incidencias SET AgrupacionId = NULL WHERE AgrupacionId = @id` antes del `DELETE`.
   El diálogo de P3 ya avisa que las incidencias quedan sin agrupación; se puede agregar la
   cantidad afectada.
4. La agrupación **no** es un tipo de incidencia (RF01.9): no debe relacionarse con un
   eventual catálogo de tipos ni condicionar campos/flujo de la incidencia.
5. Si una incidencia cambia de proyecto (si se permitiera), su `AgrupacionId` debe limpiarse o
   validarse contra el nuevo proyecto.
6. Las incidencias de un proyecto archivado son de solo lectura: reutilizar el guard de
   `Proyecto.AsegurarEditable()` / `ProyectoArchivadoException` (RN-22).
7. Con incidencias, agregar a `AgrupacionDto` la cantidad de incidencias y, en el listado de
   proyectos/agrupaciones, filtros por agrupación (incluyendo "sin agrupación").

Login real (futuro): reemplazar el esquema `"Provisional"` por el mecanismo elegido (OIDC/SSO,
cookies con usuario-contraseña + 2FA) emitiendo los mismos claims (`sub` = PersonaId, rol
`Admin`). `IUsuarioActual`, servicios, controllers y la política `SoloAdministradores` no
cambian. Probablemente se agregue una tabla de credenciales/identidades externas vinculada a
`Personas`.

---

## Decisiones abiertas

Requieren confirmación del usuario antes de implementar (se indica la propuesta por defecto
con la que se avanzaría si se aprueba la spec sin cambios):

- **DA-01** Nombre de la solución y de la base: propuesta `GestionProyectos`.
- **DA-02** Validación de DTOs con FluentValidation (propuesta) vs DataAnnotations.
- **DA-03** UI sin librería de componentes (propuesta) vs Angular Material.
- **DA-04 (decisión a confirmar, pedida explícitamente)** Mecanismo provisional de usuario
  actual: header `X-Usuario-Id` + selector "Usuario actual" en el front + `Personas.EsAdmin`,
  implementado como esquema de autenticación de ASP.NET Core reemplazable (0.4). Es
  falsificable: solo desarrollo/demo. Persona semilla "Administrador"
  (`admin@gestionproyectos.local`).
- **DA-05** Solo archivar y la gestión del flag `esAdmin` exigen administrador. ¿También deben
  exigirlo la configuración de estados (alta, edición, inicial, desactivar) y el ABM de personas?
  Propuesta: por ahora no (no fue pedido), pero la política ya existe y agregarla es trivial.
- **DA-06** Obligatoriedad: siguiendo literalmente RF01.2 y RF01.8, descripción, fecha de
  inicio y fecha objetivo del proyecto y descripción de la agrupación son **obligatorias**.
  ¿Se prefiere que alguna sea opcional (p. ej. fechas en "En planificación", descripción de
  agrupación)?
- **DA-07** Listado de personas sin paginación (catálogo chico). ¿Volumen esperado?
- **DA-08** Concurrencia: sin control optimista (gana la última escritura). ¿Se necesita
  detectar ediciones simultáneas (p. ej. columna de versión y 409)?
- **DA-09** Semántica de "estado final": en RF-001 solo es una marca informativa (no puede
  ser inicial). ¿Debe tener efectos, p. ej. impedir editar fechas, exigir motivo, o ser
  requisito para archivar?
- **DA-10** Archivado permitido desde cualquier estado (propuesta). Alternativa: solo desde
  estados finales.
- **DA-11** Transiciones de estado libres entre estados configurables (propuesta). ¿Se requiere
  un flujo con transiciones permitidas?
- **DA-12** Desactivar un estado con proyectos asignados: se permite y los proyectos lo
  conservan (propuesta). Alternativa: bloquear la desactivación mientras tenga proyectos no
  archivados.
- **DA-13** El estado de sistema "Archivado" no se puede renombrar (propuesta). ¿Debe poder
  renombrarse su etiqueta?
- **DA-14** Desactivación de personas responsables de proyectos activos: se permite
  (propuesta). ¿Debe advertirse o bloquearse?
- **DA-15** Auditoría: solo fechas de creación/modificación y datos de archivado. ¿Se quiere
  registrar quién creó/modificó cada registro (`CreadoPorId`/`ModificadoPorId`) desde ya?

## Preguntas abiertas

Ver "Decisiones abiertas" (DA-01 a DA-15). No hay otras preguntas pendientes.
