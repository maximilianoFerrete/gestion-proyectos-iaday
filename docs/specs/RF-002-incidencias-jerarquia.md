# RF-002 – Gestión y jerarquía de incidencias

> Versión: 1.1 (APROBADA por el usuario el 2026-10-09; decisiones DA-01 a DA-19 aprobadas con la propuesta por defecto) · Fecha: 2026-10-09
> Depende de RF-001 (aprobada e implementada). Respeta sus bases técnicas (0.1 a 0.4), su
> catálogo de errores y la sección "Impacto en RF futuros (Incidencias)".
> La numeración RN/CA/DA es propia de este RF. Cuando se cita una regla de RF-001 se escribe
> "RF-001 RN-xx".

## Resumen funcional

El usuario necesita registrar, consultar y editar **incidencias** dentro de un proyecto
(título, descripción, tipo, estado, prioridad, responsable, vencimiento y agrupación), con un
código legible por proyecto (`PRJ-0001-15`). Las incidencias se organizan en una **jerarquía
padre → hijas** (Épica → Historia de Usuario → Subtarea / Caso de Prueba / Bug), pero los
tipos, los niveles, las relaciones permitidas entre tipos, los estados (con **flujos de
trabajo** por tipo) y las prioridades son **configurables** por usuarios con un permiso
especial. La jerarquía debe ser consistente: sin ciclos, dentro del mismo proyecto, con
agrupación heredada del padre y vencimientos de hijas que no superen al del padre.

## Alcance

- Incluye:
  - Modelo mínimo de **permisos** (constantes + políticas), resuelto provisionalmente desde
    `Personas.EsAdmin` mediante una abstracción reemplazable (sección 0.1).
  - Configuración (con permiso `Configuracion.Incidencias`): niveles, tipos de incidencia,
    relaciones permitidas tipo padre → tipo hijo, catálogo de estados de incidencia, flujos de
    trabajo (estados incluidos, inicial, finales) y prioridades.
  - Incidencias: alta, consulta (listado paginado con filtros, detalle con padre e hijas, vista
    de árbol del proyecto), edición (incluye cambio de padre) y **cambio de tipo** (con permiso
    `Incidencias.CambiarTipo`).
  - Numeración por proyecto con control de concurrencia.
  - Herencia y propagación de agrupación; regla de vencimiento padre/hija.
  - Ajustes sobre código de RF-001: eliminación de agrupación (desasociación de incidencias),
    cantidad de incidencias en agrupaciones y proyectos, permisos del usuario actual, menú.
- No incluye:
  - Gestión de permisos/roles (ABM de roles, asignación de permisos a personas). Ver 0.1.
  - Eliminación de incidencias (no existe endpoint).
  - Mover incidencias entre proyectos.
  - Transiciones restringidas entre estados dentro de un flujo (ver DA-01).
  - Comentarios, adjuntos, historial/auditoría de cambios, notificaciones, estimaciones,
    vínculos no jerárquicos entre incidencias ("relacionada con", "bloquea", etc.).
  - Reglas automáticas por estado final (p. ej. cerrar el padre al cerrar las hijas).
  - Eliminación de niveles, tipos, estados, flujos, prioridades o relaciones.
  - Control de concurrencia optimista (se mantiene RF-001 DA-08: gana la última escritura).

---

## 0. Bases técnicas que agrega este RF

### 0.1 Permisos (mecanismo provisional, reemplazable)

Objetivo: exigir hoy un "permiso especial" sin tener gestión de roles, y que el día que exista
(roles y asignaciones en BD, claims del proveedor de identidad, etc.) **solo cambie el
resolutor**, no los servicios ni los controllers.

1. **Domain/Application**: clase estática `Permisos` (en `GestionProyectos.Application.Abstracciones`
   o en `Domain.Comun`, a criterio de backend, con un único lugar de definición):
   - `Permisos.ConfiguracionIncidencias = "Configuracion.Incidencias"`
   - `Permisos.IncidenciasCambiarTipo = "Incidencias.CambiarTipo"`
   - `Permisos.Todos` (lista con ambos).
2. **Application**:
   - `IUsuarioActual` agrega `bool TienePermiso(string permiso)` y
     `IReadOnlyCollection<string> Permisos { get; }`.
   - `UsuarioActualExtensions.AsegurarPermiso(this IUsuarioActual u, string permiso)`: lanza
     `UsuarioNoIdentificadoException` (401) si no está identificado y
     `PermisoDenegadoException` (403) si no tiene el permiso; devuelve `PersonaId`.
   - Abstracción `IResolutorPermisos` con
     `Task<IReadOnlyCollection<string>> ObtenerPermisosAsync(IdentidadUsuario identidad, CancellationToken ct)`.
   - Implementación provisional `ResolutorPermisosPorAdministrador`: si `identidad.EsAdmin`
     devuelve `Permisos.Todos`; si no, colección vacía. Registrada en DI como
     `IResolutorPermisos` (reemplazable).
3. **Api**:
   - `ProvisionalAuthenticationHandler`, después de construir la identidad, llama a
     `IResolutorPermisos` y agrega un claim `permiso` por cada permiso
     (constante `EsquemaProvisional.ClaimPermiso = "permiso"`).
   - `UsuarioActualDesdeClaims` implementa `Permisos`/`TienePermiso` leyendo los claims
     `permiso` (no conoce el resolutor ni el header).
   - Una política por permiso, con el **mismo nombre que el permiso**:
     `AddPolicy(Permisos.X, p => p.RequireAuthenticatedUser().RequireClaim("permiso", Permisos.X))`.
     Se aplican con `[Authorize(Policy = Permisos.ConfiguracionIncidencias)]`.
   - `ManejadorResultadoAutorizacion`: el mensaje del 403 deja de ser fijo ("requiere un
     usuario administrador") y pasa a depender de la política: si la política exige el claim
     `permiso`, `detail` = `"La operación requiere el permiso 'Configuracion.Incidencias'."`;
     para `SoloAdministradores` se mantiene el mensaje actual. Códigos sin cambios
     (`401 USUARIO_NO_IDENTIFICADO`, `403 PERMISO_DENEGADO`).
   - Los servicios vuelven a verificar con `AsegurarPermiso` (defensa en profundidad, igual
     que RF-001 0.4).
4. **`GET /api/usuario-actual`** agrega `permisos: string[]` (vacío si anónimo). El frontend
   expone `UsuarioActualService.tienePermiso(permiso): boolean` (computed) y oculta acciones
   sin permiso (la autorización real la hace el backend).

Impacto futuro: cuando exista la gestión de permisos se crearán tablas del estilo `Roles`,
`RolesPermisos`, `PersonasRoles` y una implementación `ResolutorPermisosSqlServer` (o claims
emitidos por el IdP). Las constantes, políticas, `IUsuarioActual`, servicios y controllers no
cambian. Con login real (RF-001 0.4), el nuevo esquema de autenticación debe emitir también
los claims `permiso` (o se usa un `IClaimsTransformation` que llame al resolutor).

### 0.2 ProblemDetails con extensiones

Algunos errores de este RF informan una lista de incidencias afectadas. Se agrega a
`ExcepcionDominio` una propiedad opcional `IReadOnlyDictionary<string, object?> Extensiones`
(vacía por defecto) que `FabricaProblemas` copia a `ProblemDetails.Extensions`. Uso en este RF:
`"incidencias": ["PRJ-0001-16", "PRJ-0001-17"]`. Los códigos existentes no cambian.

### 0.3 Concurrencia de escrituras sobre incidencias

Toda operación que **escribe** incidencias de un proyecto (alta, edición, cambio de tipo)
se ejecuta en **una transacción** (`IUnitOfWork`) que como primer paso **bloquea la fila del
proyecto**:

- Alta: `UPDATE dbo.Proyectos SET UltimoNumeroIncidencia = UltimoNumeroIncidencia + 1
  OUTPUT INSERTED.UltimoNumeroIncidencia, INSERTED.Codigo, INSERTED.FechaArchivado
  WHERE Id = @ProyectoId` (el `UPDATE` toma un bloqueo exclusivo de fila hasta el commit).
- Edición / cambio de tipo: `SELECT Id, Codigo, FechaArchivado FROM dbo.Proyectos
  WITH (UPDLOCK, ROWLOCK) WHERE Id = @ProyectoId`.

Efectos: (a) la numeración por proyecto no se repite ni tiene huecos (un rollback revierte el
incremento); (b) las validaciones de jerarquía (ciclos, relaciones, vencimientos, propagación
de agrupación) se evalúan sin que otra transacción cambie la jerarquía del mismo proyecto en
paralelo; (c) la verificación de "proyecto archivado" es consistente. Serializa las escrituras
de incidencias **por proyecto**, aceptable para el volumen esperado (ver DA-13).
Portabilidad: en PostgreSQL equivale a `UPDATE ... RETURNING` y `SELECT ... FOR UPDATE`.

`AgrupacionService.EliminarAsync` (RF-001) también bloquea la fila del proyecto con
`UPDLOCK` antes de desasociar y borrar.

### 0.4 Consultas jerárquicas

Ancestros y descendientes se obtienen con **CTE recursivas** en los repositorios
(`WITH ... UNION ALL ...`, `OPTION (MAXRECURSION 100)`); las **reglas** (ciclo, relación
permitida, vencimiento, herencia) se evalúan en Application sobre los datos devueltos, para
poder testearlas sin BD. Las CTE recursivas son SQL estándar (`WITH RECURSIVE` en PostgreSQL).

---

## Modelo de datos

Esquema `dbo`, collation `*_CI_AS` (RF-001). Fechas de auditoría asignadas desde el backend
con `TimeProvider` (UTC); `DEFAULT` en BD solo de respaldo. Todas las FK con
`ON DELETE NO ACTION`. Nombres de constraints según la convención de RF-001
(`PK_`, `FK_<Tabla>_<TablaRef>_<Columna>`, `UQ_`, `UX_` (único filtrado), `IX_`, `CK_`, `DF_`).

### Tabla `Proyectos` (se modifica)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| Proyectos | UltimoNumeroIncidencia | INT | No | **Nueva**. `DF_Proyectos_UltimoNumeroIncidencia` = 0 (también para filas existentes). `CK_Proyectos_UltimoNumeroIncidencia`: `>= 0`. Solo la modifica el alta de incidencias (0.3). No se expone en la API |

### Tabla `NivelesIncidencia` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| NivelesIncidencia | Id | INT IDENTITY(1,1) | No | `PK_NivelesIncidencia` |
| NivelesIncidencia | Numero | INT | No | Nivel de referencia (1 = más alto). `CK_NivelesIncidencia_Numero`: `BETWEEN 1 AND 99`. `UQ_NivelesIncidencia_Numero`. **Inmutable** |
| NivelesIncidencia | Nombre | NVARCHAR(50) | No | Trim, 1–50. `UQ_NivelesIncidencia_Nombre` |
| NivelesIncidencia | Descripcion | NVARCHAR(250) | Sí | |
| NivelesIncidencia | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |
| NivelesIncidencia | FechaModificacion | DATETIME2(0) | Sí | |

### Tabla `EstadosIncidencia` (nueva, catálogo)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| EstadosIncidencia | Id | INT IDENTITY(1,1) | No | `PK_EstadosIncidencia` |
| EstadosIncidencia | Nombre | NVARCHAR(50) | No | Trim, 1–50. `UQ_EstadosIncidencia_Nombre` |
| EstadosIncidencia | Descripcion | NVARCHAR(250) | Sí | |
| EstadosIncidencia | Orden | INT | No | Orden de presentación en catálogos y filtros. `CK_EstadosIncidencia_Orden`: `BETWEEN 1 AND 9999` |
| EstadosIncidencia | Activo | BIT | No | `DF_...` = 1 |
| EstadosIncidencia | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |
| EstadosIncidencia | FechaModificacion | DATETIME2(0) | Sí | |

Sin `EsInicial`/`EsFinal`: esas marcas pertenecen al estado **dentro de un flujo**.

### Tabla `FlujosIncidencia` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| FlujosIncidencia | Id | INT IDENTITY(1,1) | No | `PK_FlujosIncidencia` |
| FlujosIncidencia | Nombre | NVARCHAR(100) | No | Trim, 1–100. `UQ_FlujosIncidencia_Nombre` |
| FlujosIncidencia | Descripcion | NVARCHAR(250) | Sí | |
| FlujosIncidencia | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |
| FlujosIncidencia | FechaModificacion | DATETIME2(0) | Sí | |

Sin `Activo`: los flujos no se desactivan ni eliminan (DA-14).

### Tabla `FlujosIncidenciaEstados` (nueva, estados de un flujo)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| FlujosIncidenciaEstados | FlujoId | INT | No | `FK_FlujosIncidenciaEstados_FlujosIncidencia_FlujoId` |
| FlujosIncidenciaEstados | EstadoId | INT | No | `FK_FlujosIncidenciaEstados_EstadosIncidencia_EstadoId` |
| FlujosIncidenciaEstados | Orden | INT | No | Posición dentro del flujo (1..n). `CK_FlujosIncidenciaEstados_Orden`: `>= 1` |
| FlujosIncidenciaEstados | EsInicial | BIT | No | `DF_...` = 0 |
| FlujosIncidenciaEstados | EsFinal | BIT | No | `DF_...` = 0 |

Constraints e índices:
- `PK_FlujosIncidenciaEstados` sobre `(FlujoId, EstadoId)` (un estado aparece una vez por flujo).
- `UX_FlujosIncidenciaEstados_FlujoId_EsInicial`: índice único **filtrado** sobre `(FlujoId)
  WHERE EsInicial = 1` → como máximo un inicial por flujo; "exactamente uno" lo garantiza el
  backend (RN-14).
- `CK_FlujosIncidenciaEstados_InicialNoFinal`: `NOT (EsInicial = 1 AND EsFinal = 1)`.
- `IX_FlujosIncidenciaEstados_EstadoId` (índice de FK).

### Tabla `TiposIncidencia` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| TiposIncidencia | Id | INT IDENTITY(1,1) | No | `PK_TiposIncidencia` |
| TiposIncidencia | Nombre | NVARCHAR(50) | No | Trim, 1–50. `UQ_TiposIncidencia_Nombre` |
| TiposIncidencia | Descripcion | NVARCHAR(250) | Sí | |
| TiposIncidencia | NivelId | INT | No | `FK_TiposIncidencia_NivelesIncidencia_NivelId`. **Inmutable** (DA-03) |
| TiposIncidencia | FlujoId | INT | No | `FK_TiposIncidencia_FlujosIncidencia_FlujoId`. Editable con RN-12 |
| TiposIncidencia | Activo | BIT | No | `DF_...` = 1 |
| TiposIncidencia | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |
| TiposIncidencia | FechaModificacion | DATETIME2(0) | Sí | |

Índices: `IX_TiposIncidencia_NivelId`, `IX_TiposIncidencia_FlujoId`.

### Tabla `RelacionesTiposIncidencia` (nueva, relaciones permitidas)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| RelacionesTiposIncidencia | Id | INT IDENTITY(1,1) | No | `PK_RelacionesTiposIncidencia` |
| RelacionesTiposIncidencia | TipoPadreId | INT | No | `FK_RelacionesTiposIncidencia_TiposIncidencia_TipoPadreId` |
| RelacionesTiposIncidencia | TipoHijoId | INT | No | `FK_RelacionesTiposIncidencia_TiposIncidencia_TipoHijoId` |
| RelacionesTiposIncidencia | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |

Constraints e índices:
- `UQ_RelacionesTiposIncidencia_TipoPadreId_TipoHijoId` (también índice de la FK padre).
- `CK_RelacionesTiposIncidencia_TiposDistintos`: `TipoPadreId <> TipoHijoId`.
- `IX_RelacionesTiposIncidencia_TipoHijoId`.
- Sin `FechaModificacion`: las filas son **inmutables** (solo `INSERT`; RN-09).
- La regla "nivel del padre < nivel del hijo" (RN-08) la valida el backend (no expresable
  como CHECK sin funciones).

### Tabla `PrioridadesIncidencia` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| PrioridadesIncidencia | Id | INT IDENTITY(1,1) | No | `PK_PrioridadesIncidencia` |
| PrioridadesIncidencia | Nombre | NVARCHAR(50) | No | Trim, 1–50. `UQ_PrioridadesIncidencia_Nombre` |
| PrioridadesIncidencia | Descripcion | NVARCHAR(250) | Sí | |
| PrioridadesIncidencia | Orden | INT | No | Orden de presentación (de menor a mayor urgencia). `CK_PrioridadesIncidencia_Orden`: `BETWEEN 1 AND 9999` |
| PrioridadesIncidencia | EsPredeterminada | BIT | No | `DF_...` = 0 |
| PrioridadesIncidencia | Activo | BIT | No | `DF_...` = 1 |
| PrioridadesIncidencia | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()` |
| PrioridadesIncidencia | FechaModificacion | DATETIME2(0) | Sí | |

Constraints: `UX_PrioridadesIncidencia_EsPredeterminada` (único filtrado `(EsPredeterminada)
WHERE EsPredeterminada = 1`), `CK_PrioridadesIncidencia_PredeterminadaActiva`:
`EsPredeterminada = 0 OR Activo = 1`.

### Tabla `Incidencias` (nueva)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| Incidencias | Id | INT IDENTITY(1,1) | No | `PK_Incidencias` |
| Incidencias | ProyectoId | INT | No | `FK_Incidencias_Proyectos_ProyectoId`. **Inmutable** (RN-35) |
| Incidencias | Numero | INT | No | Correlativo por proyecto (RN-18). `CK_Incidencias_Numero`: `>= 1`. Inmutable |
| Incidencias | Codigo | NVARCHAR(30) | No | `<Proyectos.Codigo>-<Numero>`, ej. `PRJ-0001-15`. Inmutable. `UQ_Incidencias_Codigo` |
| Incidencias | Titulo | NVARCHAR(200) | No | Trim, 1–200 |
| Incidencias | Descripcion | NVARCHAR(4000) | No | Trim, 1–4000 (DA-06) |
| Incidencias | TipoId | INT | No | `FK_Incidencias_TiposIncidencia_TipoId` |
| Incidencias | EstadoId | INT | No | `FK_Incidencias_EstadosIncidencia_EstadoId`. Debe pertenecer al flujo del tipo (RN-24) |
| Incidencias | PrioridadId | INT | No | `FK_Incidencias_PrioridadesIncidencia_PrioridadId` |
| Incidencias | ResponsableId | INT | Sí | `FK_Incidencias_Personas_ResponsableId` |
| Incidencias | FechaVencimiento | DATE | Sí | |
| Incidencias | AgrupacionId | INT | Sí | Ver FK compuesta |
| Incidencias | PadreId | INT | Sí | Ver FK compuesta |
| Incidencias | FechaCreacion | DATETIME2(0) | No | `DF_...` = `SYSUTCDATETIME()`. Asignada por el sistema |
| Incidencias | FechaModificacion | DATETIME2(0) | Sí | |

Constraints e índices:
- `UQ_Incidencias_ProyectoId_Numero` sobre `(ProyectoId, Numero)` (sirve también como índice
  de la FK a `Proyectos` y para ordenar el listado por número).
- `UQ_Incidencias_ProyectoId_Id` sobre `(ProyectoId, Id)`: destino de la FK compuesta del padre.
- `FK_Incidencias_Agrupaciones` `(ProyectoId, AgrupacionId)` → `Agrupaciones(ProyectoId, Id)`
  (contra `UQ_Agrupaciones_ProyectoId_Id`, según RF-001). **Sin `ON DELETE SET NULL`**.
- `FK_Incidencias_Incidencias_Padre` `(ProyectoId, PadreId)` → `Incidencias(ProyectoId, Id)`:
  garantiza en la BD que padre e hija son del mismo proyecto. Con `PadreId` nulo la FK no se
  evalúa.
- `CK_Incidencias_PadreDistinto`: `PadreId IS NULL OR PadreId <> Id`.
- `IX_Incidencias_ProyectoId_AgrupacionId` (FK agrupación, filtro por agrupación),
  `IX_Incidencias_ProyectoId_PadreId` (FK padre), `IX_Incidencias_PadreId` (CTE de
  descendientes), `IX_Incidencias_TipoId`, `IX_Incidencias_EstadoId`,
  `IX_Incidencias_PrioridadId`, `IX_Incidencias_ResponsableId`.
- Invariantes mantenidos por el backend (no expresables como CHECK): estado dentro del flujo
  del tipo (RN-24); agrupación igual a la del padre (RN-28); vencimiento hija ≤ padre (RN-31);
  sin ciclos (RN-26); relación de tipos permitida (RN-25).

### Relaciones

```
NivelesIncidencia 1 ──< TiposIncidencia (NivelId)
FlujosIncidencia  1 ──< TiposIncidencia (FlujoId)          (un flujo compartido por varios tipos)
FlujosIncidencia  1 ──< FlujosIncidenciaEstados >── 1 EstadosIncidencia
TiposIncidencia   1 ──< RelacionesTiposIncidencia (TipoPadreId / TipoHijoId)
Proyectos 1 ──< Incidencias (ProyectoId)
Agrupaciones (ProyectoId, Id) 1 ──< Incidencias (ProyectoId, AgrupacionId opcional)
Incidencias (ProyectoId, Id)  1 ──< Incidencias (ProyectoId, PadreId opcional)
TiposIncidencia / EstadosIncidencia / PrioridadesIncidencia 1 ──< Incidencias
Personas 1 ──< Incidencias (ResponsableId opcional)
```

### Datos semilla (`S003`, idempotente; insertar solo lo que no existe por la clave indicada; no modifica filas existentes)

`NivelesIncidencia` (clave `Numero`):

| Numero | Nombre | Descripcion |
|---|---|---|
| 1 | Nivel 1 | Agrupa historias de usuario (p. ej. Épica). |
| 2 | Nivel 2 | Trabajo funcional que puede incluir subtareas, casos de prueba y bugs (p. ej. Historia de Usuario). |
| 3 | Nivel 3 | Tareas, pruebas y defectos que dependen de una historia de usuario. |

`EstadosIncidencia` (clave `Nombre`): Por hacer (10), En progreso (20), Hecho (30); todos activos.

`FlujosIncidencia` (clave `Nombre`): **Flujo general** – "Flujo por defecto: Por hacer → En
progreso → Hecho." con `FlujosIncidenciaEstados` (clave `(FlujoId, EstadoId)`):

| Estado | Orden | EsInicial | EsFinal |
|---|---|---|---|
| Por hacer | 1 | 1 | 0 |
| En progreso | 2 | 0 | 0 |
| Hecho | 3 | 0 | 1 |

(La marca inicial se inserta solo si el flujo no tiene ya un inicial.)

`TiposIncidencia` (clave `Nombre`; nivel buscado por `Numero`, flujo "Flujo general"):

| Nombre | Nivel | Descripcion |
|---|---|---|
| Épica | 1 | Agrupa historias de usuario. |
| Historia de Usuario | 2 | Funcionalidad desde la perspectiva del usuario. |
| Subtarea | 3 | Tarea de una historia de usuario. |
| Caso de Prueba | 3 | Caso de prueba de una historia de usuario. |
| Bug | 3 | Defecto detectado. |

`RelacionesTiposIncidencia` (clave par de nombres): Épica → Historia de Usuario; Historia de
Usuario → Subtarea; Historia de Usuario → Caso de Prueba; Historia de Usuario → Bug.

`PrioridadesIncidencia` (clave `Nombre`): Baja (10), Media (20, **predeterminada**),
Alta (30), Crítica (40); todas activas. La marca predeterminada se inserta solo si no existe
ya una predeterminada.

Scripts propuestos (continúan la numeración de RF-001; el DBA puede ajustar la división
respetando el orden de dependencias):
- `V006__agrega_contador_incidencias_a_proyectos.sql`
- `V007__crea_tablas_estados_flujos_prioridades_incidencia.sql` (`EstadosIncidencia`,
  `FlujosIncidencia`, `FlujosIncidenciaEstados`, `PrioridadesIncidencia`)
- `V008__crea_tablas_niveles_tipos_relaciones_incidencia.sql` (`NivelesIncidencia`,
  `TiposIncidencia`, `RelacionesTiposIncidencia`)
- `V009__crea_tabla_incidencias.sql`
- `seed/S003__configuracion_incidencias_inicial.sql`

---

## Contrato API

Convenciones de RF-001 0.2/0.3: JSON camelCase, `DateOnly` `"yyyy-MM-dd"`, `DATETIME2` UTC con
`Z`, trim de textos, opcionales vacíos → `null`, acciones como `POST /recurso/{id}/<accion>`
que devuelven `200` con el recurso. Headers comunes: `X-Usuario-Id` opcional. Cualquier
endpoint puede devolver `401 USUARIO_INVALIDO` y `500`; no se repiten.

**Permisos**: todos los `POST`/`PUT` de configuración exigen la política
`Configuracion.Incidencias`; `cambiar-tipo` exige `Incidencias.CambiarTipo`. Sin usuario →
`401 USUARIO_NO_IDENTIFICADO`; sin permiso → `403 PERMISO_DENEGADO`. Los `GET` y las demás
operaciones sobre incidencias no exigen usuario (coherente con RF-001 DA-05; ver DA-16).

### Resumen

| Método | Ruta | Request | Response | Códigos |
|---|---|---|---|---|
| GET | `/api/usuario-actual` | – | `UsuarioActualDto` (+`permisos`) | 200, 401 |
| GET | `/api/niveles-incidencia` | – | `NivelIncidenciaDto[]` | 200 |
| GET | `/api/niveles-incidencia/{id}` | – | `NivelIncidenciaDto` | 200, 404 |
| POST | `/api/niveles-incidencia` | `CrearNivelIncidenciaRequest` | `NivelIncidenciaDto` + `Location` | 201, 400, 401, 403, 409 |
| PUT | `/api/niveles-incidencia/{id}` | `EditarNivelIncidenciaRequest` | `NivelIncidenciaDto` | 200, 400, 401, 403, 404, 409 |
| GET | `/api/tipos-incidencia?soloActivos=false&nivelId=` | query | `TipoIncidenciaDto[]` | 200 |
| GET | `/api/tipos-incidencia/{id}` | – | `TipoIncidenciaDto` | 200, 404 |
| POST | `/api/tipos-incidencia` | `CrearTipoIncidenciaRequest` | `TipoIncidenciaDto` + `Location` | 201, 400, 401, 403, 409 |
| PUT | `/api/tipos-incidencia/{id}` | `EditarTipoIncidenciaRequest` | `TipoIncidenciaDto` | 200, 400, 401, 403, 404, 409 |
| POST | `/api/tipos-incidencia/{id}/desactivar` | – | `TipoIncidenciaDto` | 200, 401, 403, 404 |
| POST | `/api/tipos-incidencia/{id}/activar` | – | `TipoIncidenciaDto` | 200, 401, 403, 404 |
| GET | `/api/relaciones-tipos-incidencia?tipoPadreId=&tipoHijoId=` | query | `RelacionTipoIncidenciaDto[]` | 200 |
| POST | `/api/relaciones-tipos-incidencia` | `CrearRelacionTipoIncidenciaRequest` | `RelacionTipoIncidenciaDto` + `Location` | 201, 400, 401, 403, 409 |
| GET | `/api/estados-incidencia?soloActivos=false` | query | `EstadoIncidenciaDto[]` | 200 |
| GET | `/api/estados-incidencia/{id}` | – | `EstadoIncidenciaDto` | 200, 404 |
| POST | `/api/estados-incidencia` | `EstadoIncidenciaRequest` | `EstadoIncidenciaDto` + `Location` | 201, 400, 401, 403, 409 |
| PUT | `/api/estados-incidencia/{id}` | `EstadoIncidenciaRequest` | `EstadoIncidenciaDto` | 200, 400, 401, 403, 404, 409 |
| POST | `/api/estados-incidencia/{id}/desactivar` | – | `EstadoIncidenciaDto` | 200, 401, 403, 404, 409 |
| POST | `/api/estados-incidencia/{id}/activar` | – | `EstadoIncidenciaDto` | 200, 401, 403, 404 |
| GET | `/api/flujos-incidencia` | – | `FlujoIncidenciaDto[]` | 200 |
| GET | `/api/flujos-incidencia/{id}` | – | `FlujoIncidenciaDto` | 200, 404 |
| POST | `/api/flujos-incidencia` | `FlujoIncidenciaRequest` | `FlujoIncidenciaDto` + `Location` | 201, 400, 401, 403, 409 |
| PUT | `/api/flujos-incidencia/{id}` | `FlujoIncidenciaRequest` | `FlujoIncidenciaDto` | 200, 400, 401, 403, 404, 409 |
| GET | `/api/prioridades-incidencia?soloActivas=false` | query | `PrioridadIncidenciaDto[]` | 200 |
| GET | `/api/prioridades-incidencia/{id}` | – | `PrioridadIncidenciaDto` | 200, 404 |
| POST | `/api/prioridades-incidencia` | `PrioridadIncidenciaRequest` | `PrioridadIncidenciaDto` + `Location` | 201, 400, 401, 403, 409 |
| PUT | `/api/prioridades-incidencia/{id}` | `PrioridadIncidenciaRequest` | `PrioridadIncidenciaDto` | 200, 400, 401, 403, 404, 409 |
| POST | `/api/prioridades-incidencia/{id}/marcar-predeterminada` | – | `PrioridadIncidenciaDto` | 200, 401, 403, 404, 409 |
| POST | `/api/prioridades-incidencia/{id}/desactivar` | – | `PrioridadIncidenciaDto` | 200, 401, 403, 404, 409 |
| POST | `/api/prioridades-incidencia/{id}/activar` | – | `PrioridadIncidenciaDto` | 200, 401, 403, 404 |
| GET | `/api/proyectos/{proyectoId}/incidencias` | query (ver abajo) | `Paginado<IncidenciaResumenDto>` | 200, 400, 404 |
| GET | `/api/proyectos/{proyectoId}/incidencias/arbol` | – | `NodoArbolIncidenciaDto[]` | 200, 404 |
| GET | `/api/proyectos/{proyectoId}/incidencias/{id}` | – | `IncidenciaDto` | 200, 404 |
| POST | `/api/proyectos/{proyectoId}/incidencias` | `CrearIncidenciaRequest` | `IncidenciaDto` + `Location` | 201, 400, 404, 409 |
| PUT | `/api/proyectos/{proyectoId}/incidencias/{id}` | `EditarIncidenciaRequest` | `IncidenciaDto` | 200, 400, 404, 409 |
| POST | `/api/proyectos/{proyectoId}/incidencias/{id}/cambiar-tipo` | `CambiarTipoIncidenciaRequest` | `IncidenciaDto` | 200, 400, 401, 403, 404, 409 |
| DELETE | `/api/proyectos/{proyectoId}/agrupaciones/{id}` (RF-001, **cambia comportamiento**) | – | – | 204, 404, 409 |
| GET | `/api/proyectos/{proyectoId}/agrupaciones[/{id}]` (RF-001, **agrega campo**) | – | `AgrupacionDto` + `cantidadIncidencias` | sin cambios |
| GET | `/api/proyectos/{id}` (RF-001, **agrega campo**) | – | `ProyectoDto` + `cantidadIncidencias` | sin cambios |

No existen `DELETE` de incidencias, niveles, tipos, relaciones, estados, flujos ni
prioridades, ni `PUT` de relaciones. `DELETE /api/proyectos/{p}/incidencias/{id}` responde
`405` (la plantilla existe para GET/PUT); `PUT`/`DELETE /api/relaciones-tipos-incidencia/{id}`
responde `404` (la plantilla no existe).

### Nuevos códigos de error (se agregan a `CodigosError`)

| Código | HTTP | Cuándo |
|---|---|---|
| `NIVEL_NO_ENCONTRADO` | 404 | |
| `TIPO_INCIDENCIA_NO_ENCONTRADO` | 404 | |
| `ESTADO_INCIDENCIA_NO_ENCONTRADO` | 404 | |
| `FLUJO_NO_ENCONTRADO` | 404 | |
| `PRIORIDAD_NO_ENCONTRADA` | 404 | |
| `INCIDENCIA_NO_ENCONTRADA` | 404 | Inexistente o de otro proyecto que el de la URL |
| `NIVEL_INVALIDO` | 400 | `nivelId` del body inexistente |
| `FLUJO_INVALIDO` | 400 | `flujoId` del body inexistente |
| `TIPO_INVALIDO` | 400 | `tipoId`/`tipoPadreId`/`tipoHijoId` del body inexistente o inactivo |
| `ESTADO_INCIDENCIA_INVALIDO` | 400 | `estadoId` inexistente, inactivo (y distinto del actual) o fuera del flujo del tipo; en flujos, estado inexistente o inactivo agregado al flujo |
| `PRIORIDAD_INVALIDA` | 400 | `prioridadId` inexistente o inactiva (y distinta de la actual) |
| `AGRUPACION_INVALIDA` | 400 | `agrupacionId` del body inexistente o de otro proyecto |
| `PADRE_INVALIDO` | 400 | `padreId` inexistente o de otro proyecto |
| `RELACION_NO_PERMITIDA` | 400 | No existe la relación tipo padre → tipo hijo requerida |
| `RELACION_NIVELES_INVALIDOS` | 400 | Al crear una relación, el nivel del tipo padre no es menor que el del hijo |
| `AGRUPACION_HEREDADA` | 400 | Se envía `agrupacionId` distinto del heredado en una incidencia con padre |
| `VENCIMIENTO_POSTERIOR_AL_PADRE` | 400 | Vencimiento de la incidencia mayor que el de su padre |
| `NIVEL_NOMBRE_DUPLICADO` / `NIVEL_NUMERO_DUPLICADO` | 409 | |
| `TIPO_NOMBRE_DUPLICADO` | 409 | |
| `RELACION_DUPLICADA` | 409 | Par tipo padre → tipo hijo ya existente |
| `ESTADO_INCIDENCIA_NOMBRE_DUPLICADO` | 409 | |
| `FLUJO_NOMBRE_DUPLICADO` | 409 | |
| `PRIORIDAD_NOMBRE_DUPLICADO` | 409 | |
| `ESTADO_INCIDENCIA_INICIAL_EN_FLUJO` | 409 | Desactivar un estado que es inicial de algún flujo |
| `FLUJO_ESTADO_EN_USO` | 409 | Quitar de un flujo un estado en el que hay incidencias de tipos que usan el flujo |
| `TIPO_FLUJO_INCOMPATIBLE` | 409 | Cambiar el flujo de un tipo con incidencias en estados que el nuevo flujo no incluye |
| `PRIORIDAD_PREDETERMINADA_NO_DESACTIVABLE` | 409 | |
| `PRIORIDAD_NO_ELEGIBLE_PREDETERMINADA` | 409 | Marcar predeterminada una prioridad inactiva |
| `RELACION_CIRCULAR` | 409 | El padre elegido es la propia incidencia o uno de sus descendientes |
| `TIPO_INCOMPATIBLE_CON_HIJAS` | 409 | Cambiar el tipo y alguna hija no tiene relación permitida con el nuevo tipo. Extensión `incidencias` |
| `VENCIMIENTO_ANTERIOR_A_HIJAS` | 409 | Adelantar el vencimiento por debajo del de alguna hija. Extensión `incidencias` |

Se reutilizan de RF-001: `VALIDACION`, `RESPONSABLE_INVALIDO`, `USUARIO_NO_IDENTIFICADO`,
`USUARIO_INVALIDO`, `PERMISO_DENEGADO`, `PROYECTO_NO_ENCONTRADO`, `PROYECTO_ARCHIVADO`,
`AGRUPACION_NO_ENCONTRADA`.

### Usuario actual (modificado)

`GET /api/usuario-actual` con admin → 200:
```json
{ "identificado": true, "personaId": 1, "nombre": "Administrador", "email": "admin@gestionproyectos.local",
  "esAdmin": true, "permisos": ["Configuracion.Incidencias", "Incidencias.CambiarTipo"] }
```
Sin header: `"permisos": []`. Persona no admin: `"permisos": []`.

### Niveles

`NivelIncidenciaDto`:
```json
{ "id": 2, "numero": 2, "nombre": "Nivel 2", "descripcion": "Trabajo funcional...",
  "cantidadTipos": 1, "fechaCreacion": "2026-10-09T13:00:00Z", "fechaModificacion": null }
```
`GET /api/niveles-incidencia` → ordenado por `numero` asc.

`POST` — `CrearNivelIncidenciaRequest`:
```json
{ "numero": 4, "nombre": "Nivel 4", "descripcion": "Subdivisión de subtareas." }
```
→ 201, `Location: /api/niveles-incidencia/4`. Errores: 400 `VALIDACION` (numero fuera de
1–99, nombre vacío/>50, descripcion >250); 409 `NIVEL_NUMERO_DUPLICADO`,
`NIVEL_NOMBRE_DUPLICADO`; 401/403.

`PUT /{id}` — `EditarNivelIncidenciaRequest` `{ "nombre": "...", "descripcion": "..." }`
(`numero` no editable; si viene se ignora). Errores: los de POST + 404 `NIVEL_NO_ENCONTRADO`.

### Tipos de incidencia

`TipoIncidenciaDto`:
```json
{ "id": 2, "nombre": "Historia de Usuario", "descripcion": "Funcionalidad desde la perspectiva del usuario.",
  "nivel": { "id": 2, "numero": 2, "nombre": "Nivel 2" },
  "flujo": { "id": 1, "nombre": "Flujo general" },
  "activo": true, "cantidadIncidencias": 12,
  "fechaCreacion": "2026-10-09T13:00:00Z", "fechaModificacion": null }
```
`GET /api/tipos-incidencia` — `soloActivos` (default `false`), `nivelId` opcional. Orden:
`nivel.numero` asc, `nombre` asc.

`POST` — `CrearTipoIncidenciaRequest`:
```json
{ "nombre": "Spike", "descripcion": "Investigación acotada.", "nivelId": 3, "flujoId": 1 }
```
→ 201 (`activo = true`). Errores: 400 `VALIDACION`; 400 `NIVEL_INVALIDO`; 400 `FLUJO_INVALIDO`;
409 `TIPO_NOMBRE_DUPLICADO`; 401/403.

`PUT /{id}` — `EditarTipoIncidenciaRequest`:
```json
{ "nombre": "Spike técnico", "descripcion": "Investigación acotada.", "flujoId": 2 }
```
(`nivelId` no editable; si viene se ignora.) Errores: los de POST (sin `NIVEL_INVALIDO`) +
404 `TIPO_INCIDENCIA_NO_ENCONTRADO`; 409 `TIPO_FLUJO_INCOMPATIBLE` (RN-12).

`POST /{id}/desactivar` | `/{id}/activar` → 200, idempotentes. 404 si no existe.

### Relaciones permitidas

`RelacionTipoIncidenciaDto`:
```json
{ "id": 3, "tipoPadre": { "id": 2, "nombre": "Historia de Usuario", "activo": true, "nivelNumero": 2 },
  "tipoHijo": { "id": 4, "nombre": "Caso de Prueba", "activo": true, "nivelNumero": 3 },
  "fechaCreacion": "2026-10-09T13:00:00Z" }
```
`GET` — filtros opcionales `tipoPadreId`, `tipoHijoId`. Orden: nivel del padre, nombre del
padre, nombre del hijo.

`POST` — `CrearRelacionTipoIncidenciaRequest`:
```json
{ "tipoPadreId": 2, "tipoHijoId": 6 }
```
→ 201, `Location: /api/relaciones-tipos-incidencia/7` (no hay `GET /{id}`; el `Location` es
informativo, ver DA-02). Errores: 400 `VALIDACION` (ids ausentes o iguales); 400
`TIPO_INVALIDO` (inexistente o inactivo); 400 `RELACION_NIVELES_INVALIDOS`;
409 `RELACION_DUPLICADA`; 401/403.

### Estados de incidencia

`EstadoIncidenciaDto`:
```json
{ "id": 2, "nombre": "En progreso", "descripcion": null, "orden": 20, "activo": true,
  "cantidadIncidencias": 5,
  "flujos": [ { "id": 1, "nombre": "Flujo general", "esInicial": false, "esFinal": false } ] }
```
`GET` — `soloActivos` (default `false`); orden `orden` asc, `nombre` asc.

`POST` / `PUT /{id}` — `EstadoIncidenciaRequest`:
```json
{ "nombre": "En revisión", "descripcion": "Esperando revisión de código.", "orden": 25 }
```
→ 201 (activo) / 200. Errores: 400 `VALIDACION` (nombre vacío/>50, descripcion >250, orden
fuera de 1–9999); 409 `ESTADO_INCIDENCIA_NOMBRE_DUPLICADO`; 404 (PUT); 401/403.

`POST /{id}/desactivar` → 200 (idempotente). 409 `ESTADO_INCIDENCIA_INICIAL_EN_FLUJO` si es
inicial en algún flujo (`detail` nombra los flujos). `POST /{id}/activar` → 200 (idempotente).

### Flujos de trabajo

`FlujoIncidenciaDto`:
```json
{ "id": 1, "nombre": "Flujo general", "descripcion": "Flujo por defecto: Por hacer → En progreso → Hecho.",
  "estados": [
    { "estadoId": 1, "nombre": "Por hacer",   "activo": true, "orden": 1, "esInicial": true,  "esFinal": false },
    { "estadoId": 2, "nombre": "En progreso", "activo": true, "orden": 2, "esInicial": false, "esFinal": false },
    { "estadoId": 3, "nombre": "Hecho",       "activo": true, "orden": 3, "esInicial": false, "esFinal": true }
  ],
  "tipos": [ { "id": 1, "nombre": "Épica", "activo": true }, { "id": 2, "nombre": "Historia de Usuario", "activo": true } ],
  "fechaCreacion": "2026-10-09T13:00:00Z", "fechaModificacion": null }
```
`GET` → ordenado por `nombre`.

`POST` / `PUT /{id}` — `FlujoIncidenciaRequest` (el `orden` es la posición en el array, desde 1;
el `PUT` reemplaza la composición completa):
```json
{ "nombre": "Flujo con revisión", "descripcion": null,
  "estados": [
    { "estadoId": 1, "esInicial": true,  "esFinal": false },
    { "estadoId": 2, "esInicial": false, "esFinal": false },
    { "estadoId": 4, "esInicial": false, "esFinal": false },
    { "estadoId": 3, "esInicial": false, "esFinal": true }
  ] }
```
Errores: 400 `VALIDACION` (nombre vacío/>100; descripcion >250; `estados` vacío; `estadoId`
repetido; cantidad de `esInicial = true` distinta de 1; un estado inicial y final a la vez —
errores con clave `estados`); 400 `ESTADO_INCIDENCIA_INVALIDO` (estado inexistente; estado
inactivo **agregado** al flujo o marcado inicial); 409 `FLUJO_NOMBRE_DUPLICADO`;
409 `FLUJO_ESTADO_EN_USO` (PUT, RN-15); 404 `FLUJO_NO_ENCONTRADO` (PUT); 401/403.

### Prioridades

`PrioridadIncidenciaDto`:
```json
{ "id": 2, "nombre": "Media", "descripcion": null, "orden": 20, "esPredeterminada": true,
  "activo": true, "cantidadIncidencias": 30 }
```
`GET` — `soloActivas` (default `false`); orden `orden` asc, `nombre` asc.

`POST` / `PUT /{id}` — `PrioridadIncidenciaRequest` `{ "nombre": "Bloqueante", "descripcion": null, "orden": 50 }`
→ 201 (activa, no predeterminada) / 200. Errores: 400 `VALIDACION` (nombre vacío/>50,
descripcion >250, orden fuera de 1–9999); 409 `PRIORIDAD_NOMBRE_DUPLICADO`; 404 (PUT); 401/403.

`POST /{id}/marcar-predeterminada` → 200; desmarca la anterior en la misma transacción; si ya
lo era, 200 sin cambios. 409 `PRIORIDAD_NO_ELEGIBLE_PREDETERMINADA` si está inactiva.

`POST /{id}/desactivar` → 200 (idempotente); 409 `PRIORIDAD_PREDETERMINADA_NO_DESACTIVABLE`.
`POST /{id}/activar` → 200 (idempotente).

### Incidencias

Referencias usadas en los DTOs:
- `TipoReferenciaDto` `{ "id", "nombre", "activo", "nivelNumero" }`
- `EstadoIncidenciaReferenciaDto` `{ "id", "nombre", "activo", "esFinal" }` (`esFinal` según el
  flujo del tipo de la incidencia)
- `PrioridadReferenciaDto` `{ "id", "nombre", "orden", "activo" }`
- `PersonaReferenciaDto` (RF-001) `{ "id", "nombre", "activo" }`
- `AgrupacionReferenciaDto` `{ "id", "nombre" }`
- `IncidenciaReferenciaDto` `{ "id", "codigo", "titulo", "tipo", "estado", "responsable", "fechaVencimiento" }`

`GET /api/proyectos/{proyectoId}/incidencias` — query:

| Parámetro | Tipo | Default | Reglas |
|---|---|---|---|
| `texto` | string | – | Contiene, sobre `Codigo` o `Titulo`. Máx. 200 |
| `tipoId` | int (repetible: `tipoId=1&tipoId=2`) | – | Filtra por cualquiera de los tipos |
| `estadoId` | int | – | |
| `prioridadId` | int | – | |
| `responsableId` | int | – | Excluyente con `sinResponsable` |
| `sinResponsable` | bool | `false` | `ResponsableId IS NULL` |
| `agrupacionId` | int | – | Excluyente con `sinAgrupacion` |
| `sinAgrupacion` | bool | `false` | `AgrupacionId IS NULL` |
| `pagina` | int | 1 | ≥ 1 |
| `tamanioPagina` | int | 20 | 1–100 |
| `ordenarPor` | `numero` \| `titulo` \| `tipo` \| `estado` \| `prioridad` \| `responsable` \| `fechaVencimiento` \| `fechaCreacion` | `numero` | Lista blanca |
| `direccion` | `asc` \| `desc` | `desc` | |

Combinaciones excluyentes o valores fuera de rango → 400 `VALIDACION`. Desempate por `Numero`.
Proyecto inexistente → 404 `PROYECTO_NO_ENCONTRADO`. Funciona para proyectos archivados.

Response 200:
```json
{
  "items": [
    { "id": 15, "codigo": "PRJ-0001-15", "numero": 15, "titulo": "Pagar con tarjeta",
      "tipo": { "id": 2, "nombre": "Historia de Usuario", "activo": true, "nivelNumero": 2 },
      "estado": { "id": 2, "nombre": "En progreso", "activo": true, "esFinal": false },
      "prioridad": { "id": 3, "nombre": "Alta", "orden": 30, "activo": true },
      "responsable": { "id": 2, "nombre": "Ana Gómez", "activo": true },
      "fechaVencimiento": "2026-11-30",
      "agrupacion": { "id": 8, "nombre": "Módulo pagos" },
      "padre": { "id": 3, "codigo": "PRJ-0001-3" },
      "cantidadHijas": 4,
      "fechaCreacion": "2026-10-09T14:00:00Z" }
  ],
  "pagina": 1, "tamanioPagina": 20, "total": 1
}
```

`GET /api/proyectos/{proyectoId}/incidencias/arbol` → 200, lista **plana** de todas las
incidencias del proyecto ordenada por `numero` asc; el frontend arma el árbol con `padreId`:
```json
[
  { "id": 3, "codigo": "PRJ-0001-3", "titulo": "Checkout", "padreId": null,
    "tipo": { "id": 1, "nombre": "Épica", "activo": true, "nivelNumero": 1 },
    "estado": { "id": 2, "nombre": "En progreso", "activo": true, "esFinal": false },
    "prioridad": { "id": 2, "nombre": "Media", "orden": 20, "activo": true },
    "responsable": null, "fechaVencimiento": "2026-12-15", "agrupacion": { "id": 8, "nombre": "Módulo pagos" } },
  { "id": 15, "codigo": "PRJ-0001-15", "titulo": "Pagar con tarjeta", "padreId": 3, "...": "..." }
]
```
404 `PROYECTO_NO_ENCONTRADO`.

`GET /api/proyectos/{proyectoId}/incidencias/{id}` → 200 `IncidenciaDto`:
```json
{
  "id": 15, "codigo": "PRJ-0001-15", "numero": 15,
  "proyecto": { "id": 1, "codigo": "PRJ-0001", "nombre": "Portal clientes", "archivado": false },
  "titulo": "Pagar con tarjeta", "descripcion": "Como cliente quiero pagar con tarjeta...",
  "tipo": { "id": 2, "nombre": "Historia de Usuario", "activo": true, "nivelNumero": 2 },
  "estado": { "id": 2, "nombre": "En progreso", "activo": true, "esFinal": false },
  "prioridad": { "id": 3, "nombre": "Alta", "orden": 30, "activo": true },
  "responsable": { "id": 2, "nombre": "Ana Gómez", "activo": true },
  "fechaVencimiento": "2026-11-30",
  "agrupacion": { "id": 8, "nombre": "Módulo pagos" },
  "agrupacionHeredada": true,
  "padre": { "id": 3, "codigo": "PRJ-0001-3", "titulo": "Checkout",
             "tipo": { "id": 1, "nombre": "Épica", "activo": true, "nivelNumero": 1 },
             "estado": { "id": 2, "nombre": "En progreso", "activo": true, "esFinal": false },
             "responsable": null, "fechaVencimiento": "2026-12-15" },
  "hijas": [
    { "id": 16, "codigo": "PRJ-0001-16", "titulo": "Validar tarjeta",
      "tipo": { "id": 3, "nombre": "Subtarea", "activo": true, "nivelNumero": 3 },
      "estado": { "id": 1, "nombre": "Por hacer", "activo": true, "esFinal": false },
      "responsable": null, "fechaVencimiento": "2026-11-20" }
  ],
  "tiposHijaPermitidos": [ { "id": 3, "nombre": "Subtarea" }, { "id": 4, "nombre": "Caso de Prueba" }, { "id": 5, "nombre": "Bug" } ],
  "fechaCreacion": "2026-10-09T14:00:00Z", "fechaModificacion": null
}
```
`agrupacionHeredada` = `padre != null`. `hijas` = hijas directas ordenadas por `numero`.
`tiposHijaPermitidos` = tipos **activos** con relación desde el tipo de la incidencia.
404 `PROYECTO_NO_ENCONTRADO` / `INCIDENCIA_NO_ENCONTRADA` (incluye incidencia de otro proyecto).

`POST /api/proyectos/{proyectoId}/incidencias` — `CrearIncidenciaRequest`:
```json
{ "tipoId": 3, "titulo": "Validar tarjeta", "descripcion": "Validar número y vencimiento.",
  "prioridadId": null, "responsableId": 2, "fechaVencimiento": "2026-11-20",
  "agrupacionId": null, "padreId": 15 }
```
→ 201 `IncidenciaDto`, `Location: /api/proyectos/1/incidencias/16`. El sistema asigna
`numero`, `codigo`, `estado` (inicial del flujo del tipo), `fechaCreacion`, y la prioridad
predeterminada si `prioridadId` es `null`; con padre, la agrupación es la del padre. No se
acepta `estadoId` (se ignora si viene).
Errores: 400 `VALIDACION` (tipoId ausente; título vacío/>200; descripción vacía/>4000; fecha
inválida); 400 `TIPO_INVALIDO`; 400 `PRIORIDAD_INVALIDA`; 400 `RESPONSABLE_INVALIDO`;
400 `AGRUPACION_INVALIDA`; 400 `PADRE_INVALIDO`; 400 `RELACION_NO_PERMITIDA`;
400 `AGRUPACION_HEREDADA`; 400 `VENCIMIENTO_POSTERIOR_AL_PADRE`; 404 `PROYECTO_NO_ENCONTRADO`;
409 `PROYECTO_ARCHIVADO`.

`PUT /api/proyectos/{proyectoId}/incidencias/{id}` — `EditarIncidenciaRequest` (reemplazo
completo; `tipoId` no se acepta, se ignora si viene):
```json
{ "titulo": "Pagar con tarjeta de crédito", "descripcion": "Como cliente quiero pagar con tarjeta...",
  "estadoId": 3, "prioridadId": 3, "responsableId": null, "fechaVencimiento": "2026-11-25",
  "agrupacionId": null, "padreId": 3 }
```
→ 200 `IncidenciaDto`. Errores: 400 `VALIDACION` (título, descripción, `estadoId` y
`prioridadId` obligatorios); 400 `ESTADO_INCIDENCIA_INVALIDO`; 400 `PRIORIDAD_INVALIDA`;
400 `RESPONSABLE_INVALIDO`; 400 `AGRUPACION_INVALIDA`; 400 `AGRUPACION_HEREDADA`;
400 `PADRE_INVALIDO`; 400 `RELACION_NO_PERMITIDA`; 400 `VENCIMIENTO_POSTERIOR_AL_PADRE`;
404 `PROYECTO_NO_ENCONTRADO` / `INCIDENCIA_NO_ENCONTRADA`; 409 `PROYECTO_ARCHIVADO`;
409 `RELACION_CIRCULAR`; 409 `VENCIMIENTO_ANTERIOR_A_HIJAS`.

Ejemplo 409 `VENCIMIENTO_ANTERIOR_A_HIJAS`:
```json
{ "type": "https://httpstatuses.io/409", "title": "Conflicto", "status": 409,
  "detail": "El vencimiento 2026-11-10 es anterior al de las hijas PRJ-0001-16 (2026-11-20).",
  "instance": "/api/proyectos/1/incidencias/15", "codigo": "VENCIMIENTO_ANTERIOR_A_HIJAS",
  "incidencias": ["PRJ-0001-16"], "traceId": "00-..." }
```

`POST /api/proyectos/{proyectoId}/incidencias/{id}/cambiar-tipo` — política
`Incidencias.CambiarTipo`. `CambiarTipoIncidenciaRequest`:
```json
{ "tipoId": 5, "estadoId": null }
```
`estadoId` opcional (RN-23). → 200 `IncidenciaDto`. Si `tipoId` es el actual → 200 sin cambios.
Errores: 400 `VALIDACION` (`tipoId` ausente); 400 `TIPO_INVALIDO`; 400
`ESTADO_INCIDENCIA_INVALIDO`; 400 `RELACION_NO_PERMITIDA` (con el tipo del padre); 401/403;
404; 409 `PROYECTO_ARCHIVADO`; 409 `TIPO_INCOMPATIBLE_CON_HIJAS` (extensión `incidencias`).

### Cambios en endpoints de RF-001

- `DELETE /api/proyectos/{proyectoId}/agrupaciones/{id}`: mismo contrato (204/404/409); ahora,
  en la misma transacción y antes del `DELETE`, ejecuta
  `UPDATE Incidencias SET AgrupacionId = NULL, FechaModificacion = @ahora
  WHERE ProyectoId = @proyectoId AND AgrupacionId = @id` (RN-30). Por la herencia (RN-28), esto
  desasocia jerarquías completas.
- `AgrupacionDto` agrega `"cantidadIncidencias": 7`.
- `ProyectoDto` agrega `"cantidadIncidencias": 42`.

---

## Reglas de negocio y validaciones

Permisos
- **RN-01** Crear o editar niveles, tipos (incluye activar/desactivar), relaciones, estados de
  incidencia (incluye activar/desactivar), flujos y prioridades (incluye predeterminada y
  activar/desactivar) exige el permiso `Configuracion.Incidencias` (401 sin usuario, 403 sin
  permiso). Las consultas no exigen permiso.
- **RN-02** Cambiar el tipo de una incidencia exige el permiso `Incidencias.CambiarTipo`.
- **RN-03** Provisionalmente, una persona activa con `EsAdmin = 1` tiene todos los permisos y
  una persona no admin no tiene ninguno (resolutor reemplazable, 0.1).

Niveles
- **RN-04** Número obligatorio 1–99, único e inmutable. Nombre obligatorio 1–50 único (sin
  distinguir mayúsculas). Descripción opcional ≤ 250. Los niveles no se eliminan ni desactivan.

Tipos de incidencia
- **RN-05** Nombre obligatorio 1–50 único (sin distinguir mayúsculas, incluye inactivos);
  descripción opcional ≤ 250; nivel y flujo obligatorios y existentes.
- **RN-06** Cada tipo pertenece a un único nivel, fijado al crear (inmutable).
- **RN-07** Los tipos no se eliminan; se desactivan/reactivan. Un tipo inactivo no puede
  usarse para crear incidencias, como destino de un cambio de tipo ni en relaciones nuevas.
  Las incidencias existentes de ese tipo lo conservan y siguen editables; las relaciones
  existentes se conservan.

Relaciones permitidas
- **RN-08** Una relación es un par (tipo padre, tipo hijo) de tipos distintos, ambos activos al
  crearla, donde el número de nivel del padre es **menor** que el del hijo (DA-04). El par es
  único.
- **RN-09** Las relaciones se pueden agregar, pero no modificar ni eliminar (DA-02).

Estados y flujos
- **RN-10** Estado de incidencia: nombre obligatorio 1–50 único (sin distinguir mayúsculas);
  descripción ≤ 250; orden 1–9999 (repetibles, desempate por nombre). No se eliminan; se
  desactivan/reactivan.
- **RN-11** Un estado que es inicial en algún flujo no se puede desactivar. Desactivar un
  estado no modifica las incidencias que lo tienen ni los flujos que lo incluyen; un estado
  inactivo no puede asignarse a una incidencia (salvo que ya lo tenga), ni agregarse a un flujo,
  ni marcarse inicial.
- **RN-12** Cada tipo tiene exactamente un flujo; un flujo puede compartirse entre varios tipos.
  Cambiar el flujo de un tipo solo se permite si **todas** las incidencias de ese tipo están en
  estados incluidos en el nuevo flujo (si no, 409 `TIPO_FLUJO_INCOMPATIBLE`; `detail` indica la
  cantidad de incidencias afectadas por estado).
- **RN-13** Flujo: nombre obligatorio 1–100 único; descripción ≤ 250; al menos un estado; sin
  estados repetidos; el orden es la posición en la lista.
- **RN-14** Cada flujo tiene **exactamente un** estado inicial, activo y no final. Puede tener
  cero o más estados finales. La marca final es informativa en este RF (DA-09).
- **RN-15** No se puede quitar de un flujo un estado en el que haya incidencias cuyo tipo usa
  ese flujo (409 `FLUJO_ESTADO_EN_USO`). Cambiar el inicial o las marcas finales no modifica
  incidencias existentes; el nuevo inicial aplica a las incidencias creadas después.
- **RN-16** Dentro del flujo de su tipo, una incidencia puede pasar de cualquier estado a
  cualquier otro estado del flujo (transiciones libres, DA-01).

Prioridades
- **RN-17** Nombre obligatorio 1–50 único; descripción ≤ 250; orden 1–9999. Existe exactamente
  una predeterminada, activa; marcar otra desmarca la anterior en la misma transacción; la
  predeterminada no se puede desactivar. Desactivar una prioridad no modifica las incidencias
  que la tienen; una prioridad inactiva no puede asignarse (salvo que la incidencia ya la tenga).
  No se eliminan.

Incidencias: identificación y datos
- **RN-18** Al crear una incidencia se le asigna `Numero` = siguiente correlativo del proyecto
  (1, 2, 3...) y `Codigo` = `<CodigoProyecto>-<Numero>` sin relleno (`PRJ-0001-15`). Ambos son
  únicos e inmutables. La numeración es segura ante concurrencia (0.3). El formateo es una
  función pura en Application (`FormateadorCodigoIncidencia`).
- **RN-19** Título obligatorio 1–200; descripción obligatoria 1–4000 (DA-06); tipo obligatorio;
  responsable y fecha de vencimiento opcionales. `FechaCreacion` la asigna el sistema
  (`TimeProvider`, UTC); `FechaModificacion` en cada edición.
- **RN-20** El estado inicial de una incidencia es el estado inicial del flujo de su tipo; el
  request de alta no lo define.
- **RN-21** La prioridad es obligatoria; en el alta, si no se envía, se usa la predeterminada.
  Debe existir y estar activa (salvo la actual en la edición).
- **RN-22** El responsable es opcional; si se informa, debe ser una persona existente y activa
  (salvo el responsable actual en la edición, aunque esté inactivo).
- **RN-23** En la edición, `estadoId` debe pertenecer al flujo del tipo de la incidencia y
  estar activo (salvo que sea el actual). En el cambio de tipo: si `estadoId` se informa, debe
  pertenecer al flujo del **nuevo** tipo y estar activo (o ser el actual); si no se informa, se
  conserva el estado actual cuando el nuevo flujo lo incluye y, si no, se asigna el estado
  inicial del nuevo flujo.
- **RN-24** Invariante: el estado de una incidencia siempre pertenece al flujo de su tipo
  (mantenido por RN-12, RN-15, RN-20, RN-23).

Incidencias: jerarquía
- **RN-25** Una incidencia puede tener como máximo un padre; el padre es opcional para todos los
  tipos. Si tiene padre, debe existir el par (tipo del padre → tipo de la hija) en las
  relaciones permitidas. Se valida al crear, al cambiar de padre y al cambiar el tipo (contra
  el padre y contra **cada** hija directa: si alguna hija no es compatible → 409
  `TIPO_INCOMPATIBLE_CON_HIJAS` con la lista de códigos).
- **RN-26** Una incidencia no puede ser su propio padre ni tener como padre a uno de sus
  descendientes (409 `RELACION_CIRCULAR`). Se valida en Application con los ancestros del padre
  propuesto (CTE recursiva en el repositorio).
- **RN-27** Padre e hija pertenecen al mismo proyecto (400 `PADRE_INVALIDO` si el padre no es del
  proyecto de la URL; garantizado además por la FK compuesta).
- **RN-28** Agrupación heredada: una incidencia con padre tiene siempre la misma agrupación que
  su padre (incluido "sin agrupación"). En el request, una incidencia con padre debe enviar
  `agrupacionId` nulo o igual a la del padre; cualquier otro valor → 400 `AGRUPACION_HEREDADA`.
  Solo las incidencias sin padre eligen agrupación, que debe ser del mismo proyecto (400
  `AGRUPACION_INVALIDA`).
- **RN-29** Propagación de agrupación (misma transacción):
  - Al cambiar la agrupación de una incidencia sin padre, se actualizan todos sus descendientes.
  - Al crear con padre o al asignar/cambiar el padre, la incidencia y todos sus descendientes
    toman la agrupación del nuevo padre.
  - Al quitar el padre, la incidencia (y sus descendientes) conservan la agrupación que tenían;
    en el mismo request se puede elegir otra (que se propaga).
- **RN-30** Al eliminar una agrupación (RF-001 RN-30), en la misma transacción se ponen en
  `NULL` las `AgrupacionId` de todas las incidencias del proyecto con esa agrupación (por RN-28
  son jerarquías completas) y luego se borra la agrupación.
- **RN-31** Vencimiento: si el padre tiene fecha de vencimiento y la hija también, la de la hija
  debe ser **menor o igual** (400 `VENCIMIENTO_POSTERIOR_AL_PADRE`). Una hija sin vencimiento
  es válida aunque el padre tenga vencimiento; la regla compara solo con el padre directo
  (DA-05). Se valida al crear, al editar el vencimiento y al cambiar de padre (contra el nuevo
  padre).
- **RN-32** Si se modifica el vencimiento de una incidencia a una fecha anterior al vencimiento
  de alguna de sus hijas directas, la operación se rechaza (409 `VENCIMIENTO_ANTERIOR_A_HIJAS`)
  listando esas hijas; no se modifica ninguna fecha automáticamente. Quitar el vencimiento del
  padre siempre es válido.
- **RN-33** No hay profundidad máxima explícita: RN-08 hace que cada nivel de la jerarquía tenga
  un número de nivel mayor que su padre, por lo que la profundidad queda acotada por la
  cantidad de niveles (≤ 99). Las CTE usan `MAXRECURSION 100` como resguardo.

Incidencias: ciclo de vida
- **RN-34** Las incidencias no se eliminan (no hay endpoint).
- **RN-35** Una incidencia pertenece a un único proyecto y no puede moverse a otro (`ProyectoId`
  no está en ningún request; se toma de la URL).
- **RN-36** Las incidencias de un proyecto archivado son de solo lectura: alta, edición y
  cambio de tipo → 409 `PROYECTO_ARCHIVADO` (guard `Proyecto.AsegurarEditable()`, RF-001 RN-22).
  Las consultas (listado, árbol, detalle) funcionan.
- **RN-37** Una incidencia solo se opera a través de su proyecto: si `{id}` no pertenece a
  `{proyectoId}` → 404 `INCIDENCIA_NO_ENCONTRADA`.
- **RN-38** Todas las escrituras de incidencias de un proyecto se ejecutan en una transacción
  que bloquea la fila del proyecto (0.3); si cualquier paso falla (incluida la propagación) no
  se persiste nada.

Transversales
- **RN-39** Textos con trim (RF-001 RN-31). Unicidad de nombres sin distinguir mayúsculas. Las
  validaciones de formato del frontend replican RN-04, 05, 10, 13, 17, 19 y RN-31 (con el
  vencimiento del padre cargado); el backend valida siempre.

---

## Pantallas (Angular)

Estructura nueva:
- `frontend/src/app/features/incidencias/` (`incidencias.routes.ts`, `models/`, `services/`,
  `pages/`, `components/`, `validators/`, `utils/`).
- `frontend/src/app/features/configuracion-incidencias/` (niveles/tipos, relaciones, estados,
  flujos, prioridades).

Rutas (en `app.routes.ts`, la entrada `proyectos/:id/incidencias` se declara **antes** de
`proyectos` para que el lazy de proyectos no la capture):
- `proyectos/:id/incidencias` → `incidencias.routes.ts`
- `configuracion/incidencias` → `configuracion-incidencias.routes.ts`

Menú (shell): Configuración agrega "Niveles y tipos", "Relaciones de tipos", "Estados de
incidencia", "Flujos de trabajo", "Prioridades". Visibles para todos (las pantallas son
consultables); las acciones de alta/edición se muestran solo si
`usuarioActual.tienePermiso('Configuracion.Incidencias')`.

Manejo de errores común: el de RF-001. `RELACION_CIRCULAR`, `AGRUPACION_HEREDADA`,
`VENCIMIENTO_*`, `RELACION_NO_PERMITIDA`, `PADRE_INVALIDO` se muestran junto al campo
correspondiente (Padre, Agrupación, Vencimiento, Tipo); `TIPO_INCOMPATIBLE_CON_HIJAS` y
`VENCIMIENTO_ANTERIOR_A_HIJAS` muestran `detail` y la lista `incidencias` como enlaces.

Proyecto archivado: en todas las pantallas de incidencias se muestra el banner "Proyecto
archivado: solo lectura." y se ocultan Nueva incidencia, Editar, Crear hija y Cambiar tipo;
las rutas de alta/edición redirigen al listado/detalle con toast "El proyecto está archivado y
no se puede modificar."

### Cambios en P3 – Detalle de proyecto (RF-001)
- Botones "Incidencias" (`/proyectos/:id/incidencias`) y "Árbol" (`/proyectos/:id/incidencias/arbol`)
  y el total `cantidadIncidencias`.
- Tabla de agrupaciones: columna "Incidencias" (`cantidadIncidencias`).
- Diálogo de eliminar agrupación: "¿Eliminar la agrupación <nombre>? Las N incidencias asociadas
  no se eliminan: quedarán sin agrupación (incluidas sus jerarquías)." (si N = 0, el texto de RF-001).

### I1 – Listado de incidencias · ruta `/proyectos/:id/incidencias`
- Componentes: `IncidenciasListadoPage`, `IncidenciasFiltrosComponent`,
  `IncidenciasTablaComponent`, `PaginadorComponent`.
- Encabezado: código y nombre del proyecto; enlaces "Volver al proyecto" y "Ver árbol".
- Filtros (en query params, cambios vuelven a página 1, texto con debounce 300 ms):
  Texto (código o título, máx. 200), Tipo (multi-selección, todos los tipos con "(inactivo)"),
  Estado (todos los estados con "(inactivo)"), Prioridad, Responsable (opciones "Todos",
  "Sin responsable" y personas incluidas inactivas), Agrupación (opciones "Todas",
  "Sin agrupación" y agrupaciones del proyecto). Botón "Limpiar filtros".
- Tabla: Código, Título, Tipo, Estado (badge; finales con estilo diferenciado), Prioridad,
  Responsable, Vencimiento (`dd/MM/yyyy`; vencida y estado no final → estilo de alerta),
  Agrupación, Padre (código, enlace). Encabezados ordenables. Clic en fila → detalle.
- Acción "Nueva incidencia" → `/proyectos/:id/incidencias/nueva`.
- Vacío: "No hay incidencias que coincidan con los filtros."

### I2 – Árbol de incidencias · ruta `/proyectos/:id/incidencias/arbol`
- Componentes: `IncidenciasArbolPage`, `ArbolIncidenciasComponent` (recursivo, presentación),
  util pura `construirArbol(nodos)` (raíces = `padreId` nulo; hijos ordenados por número).
- Cada nodo: botón expandir/colapsar (si tiene hijos), código (enlace al detalle), título, tipo,
  estado (badge), responsable, vencimiento. Botones "Expandir todo" / "Colapsar todo".
  Inicialmente se muestran expandidas las raíces (primer nivel visible con sus hijas).
- Vacío: "El proyecto no tiene incidencias."

### I3 – Detalle de incidencia · ruta `/proyectos/:id/incidencias/:incidenciaId`
- Componentes: `IncidenciaDetallePage`, `IncidenciaDatosComponent`, `IncidenciaHijasComponent`.
- Muestra: código, título, descripción, tipo (nivel), estado, prioridad, responsable
  ("(inactivo)" si corresponde), vencimiento, agrupación (con nota "heredada de <código padre>"
  si `agrupacionHeredada`), fechas de creación/modificación.
- Sección **Padre**: código + título + tipo + estado (enlace) o "Sin padre".
- Sección **Hijas**: tabla Código, Título, Tipo, Estado, Responsable, Vencimiento (enlaces).
  Vacío: "No tiene incidencias hijas."
- Acciones (proyecto no archivado):
  - "Editar" → `/proyectos/:id/incidencias/:incidenciaId/editar`.
  - "Crear hija" (visible si `tiposHijaPermitidos` no está vacío) →
    `/proyectos/:id/incidencias/nueva?padreId=<id>`.
  - "Cambiar tipo" (visible si `tienePermiso('Incidencias.CambiarTipo')`): abre
    `CambiarTipoDialogComponent` con select Tipo (activos, distinto del actual) y select Estado
    (estados activos del flujo del tipo elegido; preseleccionado: el actual si está en el nuevo
    flujo, si no el inicial del nuevo flujo, RN-23). Confirmar → POST `cambiar-tipo`; éxito:
    toast "Tipo actualizado." y recarga.
- Volver → listado (conservando query params).

### I4 – Alta de incidencia · ruta `/proyectos/:id/incidencias/nueva[?padreId=]`
- Componentes: `IncidenciaFormPage` (modo alta), `IncidenciaFormComponent` (presentación).
- Campos:
  - Padre (opcional): buscador por código/título sobre `GET .../incidencias?texto=` (si viene
    `padreId` en la URL queda preseleccionado). Al elegir padre: se muestra su vencimiento y su
    agrupación.
  - Tipo (requerido): tipos activos; si hay padre, solo los tipos hijos permitidos del tipo del
    padre (`GET /api/relaciones-tipos-incidencia?tipoPadreId=`). Si al cambiar el padre el tipo
    elegido deja de ser válido, se limpia.
  - Título (requerido, máx. 200). Descripción (textarea, requerida, máx. 4000, contador).
  - Prioridad (requerida; prioridades activas por `orden`; preseleccionada la predeterminada).
  - Responsable (opcional; personas activas; opción "Sin responsable").
  - Fecha de vencimiento (opcional). Validador `vencimientoNoPosteriorAlPadre`: error "El
    vencimiento no puede ser posterior al del padre (dd/MM/yyyy)." si ambos tienen valor.
  - Agrupación (opcional, agrupaciones del proyecto): **deshabilitada** si hay padre, mostrando
    la del padre con ayuda "Se hereda del padre".
- Sin campo Estado; texto informativo "Se creará en estado <inicial del flujo del tipo>".
- Guardar → POST; éxito: toast "Incidencia <codigo> creada." y navegar al detalle.
  Cancelar → detalle del padre si vino `padreId`, si no, listado.

### I5 – Edición de incidencia · ruta `/proyectos/:id/incidencias/:incidenciaId/editar`
- Mismo `IncidenciaFormComponent` en modo edición: Código y Tipo en solo lectura (el cambio de
  tipo se hace desde I3).
- Estado (requerido): estados activos del flujo del tipo + el actual si está inactivo
  ("(inactivo)"), en el orden del flujo.
- Prioridad: activas + la actual si está inactiva. Responsable: activas + el actual si está
  inactivo.
- Padre: buscador; candidatos filtrados a incidencias cuyos tipos tienen relación hacia el tipo
  de la incidencia (`GET /api/relaciones-tipos-incidencia?tipoHijoId=` y luego
  `GET .../incidencias?tipoId=..&texto=`), excluyendo la propia incidencia. Opción "Sin padre".
  Aviso al cambiar el padre: "La incidencia y sus N hijas tomarán la agrupación del nuevo padre."
- Agrupación: editable solo sin padre; si tiene hijas y cambia, aviso "Se aplicará también a
  todas sus incidencias descendientes."
- Vencimiento: mismo validador contra el padre elegido.
- Guardar → PUT; éxito: toast "Incidencia actualizada." y navegar al detalle.

### C1 – Niveles y tipos · ruta `/configuracion/incidencias/tipos`
- `NivelesTiposPage`: por cada nivel (orden por número) un bloque "Nivel N – Nombre" con sus
  tipos (Nombre, Descripción, Flujo, Activo, Incidencias). Acciones (con permiso): "Nuevo
  nivel", "Editar nivel", "Nuevo tipo" (en el nivel), "Editar tipo", "Desactivar"/"Activar".
- `NivelFormDialog`: Número (requerido, 1–99, solo en alta), Nombre (requerido, 50),
  Descripción (250).
- `TipoFormPage` (`/configuracion/incidencias/tipos/nuevo?nivelId=`, `/.../tipos/:id/editar`):
  Nombre (requerido, 50), Descripción (250), Nivel (select requerido; solo lectura en edición
  con ayuda "El nivel no se puede cambiar"), Flujo (select requerido). 409
  `TIPO_FLUJO_INCOMPATIBLE` arriba del form.
- Confirmación al desactivar: "Las N incidencias de este tipo lo conservan, pero no podrán
  crearse nuevas incidencias de este tipo."

### C2 – Relaciones permitidas · ruta `/configuracion/incidencias/relaciones`
- `RelacionesPage`: tabla Tipo padre (nivel), Tipo hijo (nivel), Fecha de creación; filtros por
  tipo padre / tipo hijo. Sin acciones de editar ni eliminar; leyenda "Las relaciones no se
  pueden modificar ni eliminar una vez creadas."
- Acción (con permiso) "Nueva relación": formulario con Tipo padre y Tipo hijo (activos); el
  select de hijo solo ofrece tipos de nivel mayor que el del padre y sin relación existente.
  Confirmación: "La relación <padre> → <hijo> no podrá modificarse ni eliminarse."

### C3 – Estados de incidencia · ruta `/configuracion/incidencias/estados`
- Listado: Orden, Nombre, Descripción, Activo, Flujos (nombres; marca "inicial"/"final"),
  Incidencias. Acciones (con permiso): Nuevo, Editar, Desactivar (oculto si es inicial en algún
  flujo; confirmación con cantidad de incidencias), Activar.
- Formulario: Nombre (requerido, 50), Descripción (250), Orden (requerido, 1–9999; en alta
  sugerido el mayor + 10, tope 9999).

### C4 – Flujos de trabajo · rutas `/configuracion/incidencias/flujos`, `/nuevo`, `/:id/editar`
- Listado: Nombre, Estados (en orden, con marcas), Tipos que lo usan. Acciones (con permiso):
  Nuevo, Editar.
- Formulario `FlujoFormPage`: Nombre (requerido, 100), Descripción (250), lista ordenable de
  estados (agregar desde estados activos no incluidos, quitar, subir/bajar), radio "Inicial"
  (exactamente uno; deshabilitado en estados inactivos), check "Final" (deshabilitado en el
  inicial). Validador de grupo: al menos un estado y exactamente un inicial. Aviso si el flujo
  es compartido: "Este flujo lo usan los tipos: X, Y." 409 `FLUJO_ESTADO_EN_USO` arriba del form.

### C5 – Prioridades · ruta `/configuracion/incidencias/prioridades`
- Listado: Orden, Nombre, Descripción, Predeterminada, Activo, Incidencias. Acciones (con
  permiso): Nueva, Editar, "Marcar como predeterminada" (activas no predeterminadas),
  Desactivar (oculto en la predeterminada), Activar.
- Formulario: Nombre (requerido, 50), Descripción (250), Orden (requerido, 1–9999).

---

## Criterios de aceptación

Configuración y permisos
- **CA-01** Dada la instalación con S003, cuando se consultan niveles, tipos, relaciones,
  estados, flujos y prioridades, entonces se obtienen 3 niveles, los 5 tipos en sus niveles,
  las 4 relaciones del RF, los estados Por hacer/En progreso/Hecho, el "Flujo general" (inicial
  Por hacer, final Hecho) asignado a los 5 tipos y las prioridades Baja/Media/Alta/Crítica con
  Media predeterminada. (GET configuración, C1–C5)
- **CA-02** Dado un usuario no admin, cuando intenta crear un tipo, una relación, un estado, un
  flujo o una prioridad, entonces recibe 403 `PERMISO_DENEGADO`; sin usuario, 401
  `USUARIO_NO_IDENTIFICADO`; y C1–C5 no muestran acciones de alta/edición. (POST/PUT config, C1–C5)
- **CA-03** Dado un admin, cuando consulta `GET /api/usuario-actual`, entonces `permisos`
  contiene `Configuracion.Incidencias` e `Incidencias.CambiarTipo`; un no admin recibe `[]`.
- **CA-04** Dado un admin, cuando crea el tipo "Spike" en el nivel 3 con el flujo general y la
  relación Historia de Usuario → Spike, entonces se puede crear una incidencia Spike hija de
  una HU. (POST tipos, POST relaciones, POST incidencias, C1, C2, I4)
- **CA-05** Cuando se intenta crear la relación Subtarea → Épica (nivel 3 → 1) o Bug → Subtarea
  (mismo nivel), entonces recibe 400 `RELACION_NIVELES_INVALIDOS`; y una relación ya existente,
  409 `RELACION_DUPLICADA`. (POST relaciones, C2)
- **CA-06** Cuando se intenta `PUT` o `DELETE` sobre `/api/relaciones-tipos-incidencia/{id}`,
  entonces recibe 404 (no existe la operación) y la relación no cambia; y C2 no ofrece editar ni
  eliminar. (C2)
- **CA-07** Cuando se crea un flujo sin estado inicial, con dos iniciales o con un estado inicial
  y final a la vez, entonces recibe 400 `VALIDACION` en `estados` y el form de C4 no permite
  guardar. (POST/PUT flujos, C4)
- **CA-08** Dadas incidencias en "En progreso" de tipos que usan el flujo general, cuando se
  quita "En progreso" del flujo, entonces recibe 409 `FLUJO_ESTADO_EN_USO` y el flujo no cambia.
  (PUT flujos, C4)
- **CA-09** Dado un tipo con incidencias en "En progreso", cuando se le asigna un flujo que no
  incluye ese estado, entonces recibe 409 `TIPO_FLUJO_INCOMPATIBLE`. (PUT tipos, C1)
- **CA-10** Cuando se desactiva "Por hacer" (inicial del flujo general), entonces recibe 409
  `ESTADO_INCIDENCIA_INICIAL_EN_FLUJO`; al desactivar "En progreso", las incidencias en ese
  estado lo conservan ("(inactivo)") y no se ofrece para otras. (POST desactivar estados, C3, I5)
- **CA-11** Cuando se marca "Alta" como predeterminada, entonces "Media" deja de serlo y una
  incidencia nueva sin `prioridadId` se crea con "Alta"; desactivar la predeterminada → 409
  `PRIORIDAD_PREDETERMINADA_NO_DESACTIVABLE`. (POST prioridades, POST incidencias, C5)
- **CA-12** Dado un tipo desactivado, cuando se intenta crear una incidencia de ese tipo,
  entonces recibe 400 `TIPO_INVALIDO`; las incidencias existentes del tipo siguen editables.
  (POST/PUT incidencias, I4)

Alta, consulta y edición
- **CA-13** Dado el proyecto `PRJ-0001` sin incidencias, cuando se crean dos incidencias,
  entonces reciben `PRJ-0001-1` y `PRJ-0001-2`; en `PRJ-0002` la primera es `PRJ-0002-1`.
  (POST incidencias, I4)
- **CA-14** Cuando se crean en paralelo 20 incidencias en el mismo proyecto, entonces se obtienen
  los números 1 a 20 sin repetidos ni huecos. (POST incidencias; test de integración)
- **CA-15** Cuando se crea una incidencia de tipo Historia de Usuario, entonces su estado es
  "Por hacer", su prioridad la predeterminada y `fechaCreacion` la asigna el sistema. (POST, I3)
- **CA-16** Cuando se omite título, descripción o tipo, o se excede un largo, entonces I4
  marca el campo y la API responde 400 `VALIDACION`. (POST/PUT, I4, I5)
- **CA-17** Cuando se edita estado, prioridad, responsable y vencimiento de una incidencia,
  entonces el detalle muestra los nuevos valores y no cambia ninguna otra incidencia de la
  jerarquía. (PUT, I5, I3)
- **CA-18** Cuando se envía un `estadoId` que no pertenece al flujo del tipo, entonces recibe
  400 `ESTADO_INCIDENCIA_INVALIDO`; el select de I5 solo ofrece estados del flujo. (PUT, I5)
- **CA-19** Cuando se filtra el listado por tipo, estado, prioridad, responsable (incluido "Sin
  responsable"), agrupación (incluido "Sin agrupación") y texto, entonces los ítems y `total`
  respetan el filtro; `responsableId` con `sinResponsable=true` → 400 `VALIDACION`.
  (GET incidencias, I1)
- **CA-20** Cuando se consulta una incidencia por un `proyectoId` que no es el suyo, entonces
  recibe 404 `INCIDENCIA_NO_ENCONTRADA`. (GET/PUT/cambiar-tipo)
- **CA-21** Dado un proyecto archivado, cuando se intenta crear, editar o cambiar el tipo de una
  incidencia, entonces recibe 409 `PROYECTO_ARCHIVADO` y nada cambia; el listado, el árbol y el
  detalle responden 200 y las pantallas no muestran acciones de modificación. (POST/PUT/
  cambiar-tipo, I1–I5)
- **CA-22** No existe forma de eliminar una incidencia: `DELETE` → 405 y no hay acción en I1/I3.

Jerarquía
- **CA-23** Dada la HU `PRJ-0001-15`, cuando desde I3 se usa "Crear hija", entonces I4 abre con
  el padre preseleccionado, el select Tipo solo ofrece Subtarea, Caso de Prueba y Bug, y la
  agrupación aparece deshabilitada con la del padre. (I3, I4)
- **CA-24** Cuando se crea una Épica con padre HU (relación inexistente), entonces recibe 400
  `RELACION_NO_PERMITIDA`. (POST)
- **CA-25** Cuando se crea una Subtarea sin padre, entonces se crea correctamente (padre
  opcional). (POST)
- **CA-26** Cuando se edita una incidencia poniendo como padre a sí misma o a una de sus
  descendientes, entonces recibe 409 `RELACION_CIRCULAR`. (PUT, I5)
- **CA-27** Cuando se indica como padre una incidencia de otro proyecto, entonces recibe 400
  `PADRE_INVALIDO`. (POST/PUT)
- **CA-28** Dada una Épica en la agrupación "Pagos" con HU y subtareas, cuando se cambia la
  agrupación de la Épica a "Envíos", entonces la Épica y todos sus descendientes quedan en
  "Envíos". (PUT, I5, I3)
- **CA-29** Cuando se envía `agrupacionId` distinto al del padre en una incidencia con padre,
  entonces recibe 400 `AGRUPACION_HEREDADA`; en I4/I5 el campo está deshabilitado. (POST/PUT, I4, I5)
- **CA-30** Dada una HU (con dos subtareas) bajo una Épica en "Pagos", cuando se la mueve bajo
  otra Épica en "Envíos", entonces la HU y sus subtareas quedan en "Envíos"; cuando luego se le
  quita el padre, conserva "Envíos". (PUT)
- **CA-31** Dado un padre con vencimiento 30/11, cuando se crea o edita una hija con vencimiento
  01/12, entonces recibe 400 `VENCIMIENTO_POSTERIOR_AL_PADRE` y el form lo marca; con 30/11 o
  sin vencimiento, se acepta. (POST/PUT, I4, I5)
- **CA-32** Dado un padre con vencimiento 30/11 e hijas con 20/11 y 25/11, cuando se cambia el
  vencimiento del padre a 22/11, entonces recibe 409 `VENCIMIENTO_ANTERIOR_A_HIJAS` con
  `incidencias` = la hija del 25/11; nada cambia. Quitar el vencimiento del padre se acepta. (PUT, I5)
- **CA-33** Dada una incidencia con vencimiento 10/12, cuando se la mueve bajo un padre con
  vencimiento 30/11 sin ajustar su fecha, entonces recibe 400 `VENCIMIENTO_POSTERIOR_AL_PADRE`.
  (PUT)
- **CA-34** Dado un proyecto con jerarquías, cuando se abre I2, entonces se ven las incidencias
  sin padre como raíces y sus hijas anidadas en orden de número; expandir/colapsar funciona.
  (GET arbol, I2)
- **CA-35** Dado I3 de una HU con 3 hijas, cuando se carga, entonces se ven su padre (Épica) y
  las 3 hijas con enlaces. (GET incidencia, I3)

Cambio de tipo
- **CA-36** Dado un usuario sin `Incidencias.CambiarTipo`, cuando llama a `cambiar-tipo`,
  entonces recibe 403 (401 sin usuario) y I3 no muestra "Cambiar tipo". (POST cambiar-tipo, I3)
- **CA-37** Dado un admin y una Subtarea hija de una HU, cuando la cambia a Bug, entonces recibe
  200 y conserva su estado (ambos tipos usan el flujo general). Al cambiarla a Épica recibe 400
  `RELACION_NO_PERMITIDA` (HU → Épica no existe). (POST cambiar-tipo, I3)
- **CA-38** Dada una HU con una hija Caso de Prueba, cuando se la cambia a un tipo de nivel 2 sin
  relación hacia Caso de Prueba, entonces recibe 409 `TIPO_INCOMPATIBLE_CON_HIJAS` listando la
  hija. (POST cambiar-tipo)
- **CA-39** Dado un tipo destino con otro flujo que no incluye el estado actual, cuando se cambia
  el tipo sin `estadoId`, entonces la incidencia queda en el estado inicial del nuevo flujo; con
  un `estadoId` de ese flujo queda en ese estado; con uno ajeno, 400 `ESTADO_INCIDENCIA_INVALIDO`.
  (POST cambiar-tipo, diálogo I3)

Integración con RF-001
- **CA-40** Dada la agrupación "Pagos" con una Épica, su HU y dos subtareas, cuando se elimina la
  agrupación, entonces recibe 204 y las cuatro incidencias quedan sin agrupación (no se
  eliminan); el diálogo de P3 informa la cantidad. (DELETE agrupaciones, P3)
- **CA-41** Cuando se consulta el detalle de proyecto y sus agrupaciones, entonces muestran
  `cantidadIncidencias`. (GET proyecto, GET agrupaciones, P3)

---

## Casos de prueba unitarios sugeridos

Convención de RF-001 (`Metodo_Escenario_ResultadoEsperado`, AAA, NSubstitute,
`FakeTimeProvider`, trazabilidad RN/CA).

### Backend – Application / Domain (funciones puras)

`FormateadorCodigoIncidencia`
| Caso | Resultado |
|---|---|
| Formatear("PRJ-0001", 15) | `PRJ-0001-15` (RN-18) |
| Formatear("PRJ-10000", 1) | `PRJ-10000-1` |
| Número 0 o negativo; código vacío | `ArgumentOutOfRangeException` / `ArgumentException` |

`ReglasJerarquia` (o métodos de dominio equivalentes)
| Caso | Resultado |
|---|---|
| Padre propuesto = la propia incidencia | `RELACION_CIRCULAR` (RN-26) |
| Ancestros del padre propuesto contienen la incidencia | `RELACION_CIRCULAR` (RN-26) |
| Ancestros sin la incidencia | OK |
| Par (tipo padre, tipo hijo) no está en relaciones | `RELACION_NO_PERMITIDA` (RN-25) |
| Vencimiento hija > padre | `VENCIMIENTO_POSTERIOR_AL_PADRE` (RN-31) |
| Vencimiento hija == padre; hija null; padre null | OK (límites, RN-31) |
| Nuevo vencimiento < el de alguna hija | `VENCIMIENTO_ANTERIOR_A_HIJAS` con códigos de esas hijas (RN-32) |
| Nuevo vencimiento null con hijas con fecha | OK (RN-32) |
| Resolver estado al cambiar tipo: actual en nuevo flujo, sin estadoId | Conserva (RN-23) |
| Resolver estado: actual fuera del nuevo flujo, sin estadoId | Inicial del nuevo flujo (RN-23) |
| Resolver estado: estadoId fuera del nuevo flujo | `ESTADO_INCIDENCIA_INVALIDO` (RN-23) |

`ValidadorComposicionFlujo` (o validador FluentValidation de `FlujoIncidenciaRequest`)
| Caso | Resultado |
|---|---|
| Lista vacía | Error `estados` (RN-13) |
| Estado repetido | Error `estados` (RN-13) |
| 0 o 2 iniciales | Error `estados` (RN-14) |
| Inicial y final a la vez | Error `estados` (RN-14) |
| Un inicial, cero finales | Válido (RN-14) |
| Nombre vacío / 101 | Error `nombre` |

### Backend – Application (servicios con repos mockeados)

`IncidenciaService.CrearAsync`
| Caso | Resultado |
|---|---|
| Válido sin padre | Número desde el contador del repo, código formateado, estado = inicial del flujo del tipo, `FechaCreacion` = reloj, dentro de `IUnitOfWork` con commit (RN-18, RN-20, RN-38) |
| `prioridadId` null | Usa la predeterminada (RN-21) |
| Prioridad inactiva | `PRIORIDAD_INVALIDA` (RN-21) |
| Tipo inexistente / inactivo | `TIPO_INVALIDO` (RN-07) |
| Responsable inactivo | `RESPONSABLE_INVALIDO` (RN-22) |
| Responsable null | OK (RN-22) |
| Agrupación de otro proyecto | `AGRUPACION_INVALIDA` (RN-28) |
| Padre de otro proyecto / inexistente | `PADRE_INVALIDO` (RN-27) |
| Padre con relación no permitida | `RELACION_NO_PERMITIDA` (RN-25) |
| Con padre y `agrupacionId` null | Hereda la del padre (RN-28, RN-29) |
| Con padre y `agrupacionId` distinto | `AGRUPACION_HEREDADA` (RN-28) |
| Vencimiento posterior al del padre | `VENCIMIENTO_POSTERIOR_AL_PADRE` (RN-31) |
| Proyecto archivado | `ProyectoArchivadoException`, sin insertar (RN-36) |
| Proyecto inexistente | `PROYECTO_NO_ENCONTRADO` |
| Error en el insert | `RevertirAsync` llamado (RN-38) |

`IncidenciaService.EditarAsync`
| Caso | Resultado |
|---|---|
| Incidencia de otro proyecto | `INCIDENCIA_NO_ENCONTRADA` (RN-37) |
| Estado fuera del flujo del tipo | `ESTADO_INCIDENCIA_INVALIDO` (RN-23) |
| Mantener estado actual inactivo | OK (RN-11) |
| Cambiar a estado inactivo | `ESTADO_INCIDENCIA_INVALIDO` (RN-11) |
| Mantener prioridad / responsable actual inactivos | OK (RN-17, RN-22) |
| Cambio a padre = sí misma / descendiente | `RELACION_CIRCULAR` (RN-26) |
| Cambio de padre válido | Llama a propagar agrupación del nuevo padre a la incidencia y descendientes (RN-29) |
| Quitar padre sin enviar agrupación | Conserva la agrupación actual, no propaga cambios (RN-29) |
| Sin padre, cambio de agrupación | Propaga a descendientes (RN-29) |
| Con padre, `agrupacionId` distinto del padre | `AGRUPACION_HEREDADA` (RN-28) |
| Adelantar vencimiento debajo de una hija | `VENCIMIENTO_ANTERIOR_A_HIJAS`, sin actualizar (RN-32) |
| Mover bajo padre con vencimiento menor | `VENCIMIENTO_POSTERIOR_AL_PADRE` (RN-31) |
| Proyecto archivado | `ProyectoArchivadoException` (RN-36) |
| Request con `tipoId` distinto | Ignorado, el tipo no cambia (RN-02) |

`IncidenciaService.CambiarTipoAsync`
| Caso | Resultado |
|---|---|
| Usuario sin permiso / no identificado | `PermisoDenegadoException` / `UsuarioNoIdentificadoException` (RN-02) |
| Mismo tipo | Sin escritura, devuelve DTO |
| Tipo inactivo | `TIPO_INVALIDO` (RN-07) |
| Sin relación con el tipo del padre | `RELACION_NO_PERMITIDA` (RN-25) |
| Alguna hija incompatible | `TIPO_INCOMPATIBLE_CON_HIJAS` con códigos (RN-25) |
| Estado resuelto según RN-23 (3 escenarios) | Conserva / inicial / error |
| Proyecto archivado | `ProyectoArchivadoException` (RN-36) |

`IncidenciaService.ListarAsync` / `ListarQueryValidator`
| Caso | Resultado |
|---|---|
| Defaults | página 1, 20, `numero desc` |
| `responsableId` + `sinResponsable` / `agrupacionId` + `sinAgrupacion` | Error de validación (CA-19) |
| `tamanioPagina` 101, `ordenarPor=foo`, texto 201 | Errores; 100 válido |
| Proyecto archivado | Devuelve la lista (RN-36) |

`TipoIncidenciaService`
| Caso | Resultado |
|---|---|
| Crear sin permiso | `PermisoDenegadoException` (RN-01) |
| Crear nombre duplicado (otra capitalización) | `TIPO_NOMBRE_DUPLICADO` (RN-05) |
| Crear con nivel / flujo inexistente | `NIVEL_INVALIDO` / `FLUJO_INVALIDO` |
| Editar con `nivelId` distinto | Nivel no cambia (RN-06) |
| Cambiar flujo con incidencias en estados ausentes | `TIPO_FLUJO_INCOMPATIBLE` (RN-12) |
| Cambiar flujo compatible | OK |
| Desactivar con incidencias | OK, no modifica incidencias (RN-07) |

`RelacionTipoIncidenciaService`
| Caso | Resultado |
|---|---|
| Nivel padre ≥ nivel hijo | `RELACION_NIVELES_INVALIDOS` (RN-08) |
| Tipo inactivo / inexistente | `TIPO_INVALIDO` (RN-08) |
| Par existente | `RELACION_DUPLICADA` (RN-08) |
| Mismo tipo | Error de validación (RN-08) |
| Válida | Inserta; el servicio no expone métodos de editar/eliminar (RN-09) |

`EstadoIncidenciaService`
| Caso | Resultado |
|---|---|
| Nombre duplicado | `ESTADO_INCIDENCIA_NOMBRE_DUPLICADO` (RN-10) |
| Desactivar estado inicial de un flujo | `ESTADO_INCIDENCIA_INICIAL_EN_FLUJO` (RN-11) |
| Desactivar estado con incidencias | OK, no modifica incidencias (RN-11) |
| Desactivar / activar ya en ese estado | Idempotente |

`FlujoIncidenciaService`
| Caso | Resultado |
|---|---|
| Crear con estado inactivo | `ESTADO_INCIDENCIA_INVALIDO` (RN-11) |
| Editar conservando un estado inactivo ya incluido (no inicial) | OK (RN-11) |
| Editar quitando estado en uso | `FLUJO_ESTADO_EN_USO` (RN-15) |
| Editar cambiando el inicial | OK, no modifica incidencias (RN-15) |
| Nombre duplicado | `FLUJO_NOMBRE_DUPLICADO` (RN-13) |
| Sin permiso | `PermisoDenegadoException` (RN-01) |

`PrioridadIncidenciaService`
| Caso | Resultado |
|---|---|
| Marcar predeterminada | Desmarca la anterior en transacción (RN-17) |
| Marcar predeterminada inactiva | `PRIORIDAD_NO_ELEGIBLE_PREDETERMINADA` |
| Desactivar predeterminada | `PRIORIDAD_PREDETERMINADA_NO_DESACTIVABLE` |
| Nombre duplicado | `PRIORIDAD_NOMBRE_DUPLICADO` |

`NivelIncidenciaService`: número duplicado → `NIVEL_NUMERO_DUPLICADO`; número 0/100 → error;
editar no cambia `Numero` (RN-04); sin permiso → 403.

`AgrupacionService.EliminarAsync` (modificado)
| Caso | Resultado |
|---|---|
| Eliminar válida | Dentro de la transacción llama primero a `DesasociarAgrupacionAsync(proyectoId, id, ahora)` y luego a `EliminarAsync` (orden verificado con `Received.InOrder`) (RN-30) |
| Falla el delete | Revierte; no hay desasociación persistida |

`ResolutorPermisosPorAdministrador`: admin → ambos permisos; no admin → vacío (RN-03).
`UsuarioActualExtensions.AsegurarPermiso`: anónimo → 401; sin permiso → 403; con permiso →
`PersonaId`.

### Backend – Api

| Caso | Resultado |
|---|---|
| POST incidencia válido | 201, `Location` `/api/proyectos/{p}/incidencias/{id}` |
| `ConflictoException` con extensión `incidencias` | 409 ProblemDetails con `codigo` y `incidencias` (0.2) |
| Endpoints POST/PUT de configuración | `[Authorize(Policy = "Configuracion.Incidencias")]` (reflexión) |
| `cambiar-tipo` | `[Authorize(Policy = "Incidencias.CambiarTipo")]` (reflexión) |
| Política de permiso fallida, usuario no admin | 403 `PERMISO_DENEGADO`, `detail` menciona el permiso |
| Sin usuario en endpoint con política | 401 `USUARIO_NO_IDENTIFICADO` |
| `ProvisionalAuthenticationHandler` con admin | Principal con claims `permiso` de ambos permisos |
| `UsuarioActualDesdeClaims.TienePermiso` | true solo para claims presentes |
| GET usuario-actual | Incluye `permisos` |
| DELETE incidencia | 405 |
| PUT / DELETE relación por id | 404 |

Integración opcional contra LocalDB (consultar al orquestador; recomendada para CA-14):
contador concurrente, CTE de ancestros/descendientes, FK compuestas (padre y agrupación de otro
proyecto rechazados por la BD), índices filtrados de inicial por flujo y prioridad
predeterminada, desasociación al eliminar agrupación.

### Frontend (Angular)

Servicios (`HttpTestingController`)
| Caso | Resultado |
|---|---|
| `IncidenciasService.listar(1, {tipoIds:[1,2], sinResponsable:true})` | GET `/api/proyectos/1/incidencias?tipoId=1&tipoId=2&sinResponsable=true` |
| `IncidenciasService.crear/editar/cambiarTipo/arbol/obtener` | URL y body exactos del contrato |
| Servicios de configuración (`tipos`, `relaciones`, `estados`, `flujos`, `prioridades`, `niveles`) | URLs exactas; relaciones sin métodos de editar/eliminar |
| `UsuarioActualService.tienePermiso` | true/false según `permisos` |

Utilidades y validadores
| Caso | Resultado |
|---|---|
| `construirArbol` con nodos desordenados | Raíces por número; hijos anidados por número (CA-34) |
| `construirArbol` con `padreId` inexistente en la lista | El nodo se trata como raíz (defensivo) |
| `vencimientoNoPosteriorAlPadre` | Error si hija > padre; válido si igual, si alguno es nulo (RN-31) |
| `resolverEstadoAlCambiarTipo` (preselección del diálogo) | Actual si está en el nuevo flujo; si no, inicial (RN-23) |
| Validador de flujo (C4) | Error sin estados o con ≠ 1 inicial (CA-07) |

Componentes
| Caso | Resultado |
|---|---|
| `IncidenciaFormComponent` alta con `padreId` | Padre preseleccionado; Tipo limitado a hijos permitidos; Agrupación deshabilitada con la del padre (CA-23, CA-29) |
| `IncidenciaFormComponent` sin padre | Agrupación habilitada; tipos activos |
| `IncidenciaFormComponent` campos vacíos / largos excedidos | Form inválido; Guardar deshabilitado (CA-16) |
| `IncidenciaFormComponent` edición | Tipo solo lectura; estados del flujo + actual inactivo (CA-18, CA-10) |
| `IncidenciaFormComponent` 400 `VENCIMIENTO_POSTERIOR_AL_PADRE` | Error en Vencimiento |
| `IncidenciaDetallePage` proyecto archivado | Banner, sin Editar/Crear hija/Cambiar tipo (CA-21) |
| `IncidenciaDetallePage` sin permiso CambiarTipo | Sin botón Cambiar tipo (CA-36) |
| `IncidenciaDetallePage` sin `tiposHijaPermitidos` | Sin botón Crear hija |
| `IncidenciaDetallePage` 409 con `incidencias` | Muestra los códigos como enlaces |
| `IncidenciasListadoPage` | Filtros a query params y vuelta a página 1; "Sin agrupación" envía `sinAgrupacion=true` (CA-19) |
| `ArbolIncidenciasComponent` | Expandir/colapsar nodo y "Expandir todo" (CA-34) |
| `RelacionesPage` | Sin acciones de editar/eliminar; select hijo solo niveles mayores (CA-05, CA-06) |
| Páginas de configuración sin permiso | Sin acciones de alta/edición (CA-02) |
| `PrioridadesPage` | Sin Desactivar en predeterminada; sin "Marcar predeterminada" en inactivas |
| `EstadosIncidenciaPage` | Sin Desactivar en estados iniciales de algún flujo |
| `ProyectoDetallePage` (modificado) | Botones Incidencias/Árbol; diálogo de eliminar agrupación con cantidad (CA-40) |

---

## Impacto por agente

- **BD (`dba-sql`)**:
  - `V006`: `ALTER TABLE Proyectos ADD UltimoNumeroIncidencia INT NOT NULL CONSTRAINT
    DF_Proyectos_UltimoNumeroIncidencia DEFAULT 0` + `CK_Proyectos_UltimoNumeroIncidencia`.
  - `V007`–`V009` según "Modelo de datos", con todas las constraints nombradas, FK compuestas
    (`FK_Incidencias_Agrupaciones`, `FK_Incidencias_Incidencias_Padre`), índices filtrados y sin
    `ON DELETE SET NULL`. Scripts idempotentes y transaccionales como V001–V005.
  - `S003` idempotente (orden: niveles, estados, flujo y estados del flujo, tipos, relaciones,
    prioridades), archivo UTF-8.
  - Actualizar `db/README.md`: nuevos índices filtrados, FK compuestas autorreferenciales, CTE
    recursivas, y el patrón `UPDATE ... OUTPUT` / `UPDLOCK` (equivalencias PostgreSQL).
- **Backend (`dev-backend`)**:
  - Domain: entidades `NivelIncidencia`, `TipoIncidencia`, `RelacionTipoIncidencia`,
    `EstadoIncidencia`, `FlujoIncidencia` (+ `EstadoEnFlujo`), `PrioridadIncidencia`,
    `Incidencia` (métodos `CambiarPadre`, `CambiarTipo`, `Actualizar`); reglas puras de
    jerarquía (RN-25, 26, 28, 31, 32) y resolución de estado (RN-23); constantes nuevas en
    `CodigosError`; `ExcepcionDominio.Extensiones` (0.2).
  - Application: `Permisos`, `IUsuarioActual.TienePermiso/Permisos`, `AsegurarPermiso`,
    `IResolutorPermisos` + `ResolutorPermisosPorAdministrador`; DTOs/requests/validadores del
    contrato; servicios `NivelIncidenciaService`, `TipoIncidenciaService`,
    `RelacionTipoIncidenciaService`, `EstadoIncidenciaService`, `FlujoIncidenciaService`,
    `PrioridadIncidenciaService`, `IncidenciaService`; `FormateadorCodigoIncidencia`;
    repositorios (interfaces) incluyendo en `IIncidenciaRepository`:
    `ObtenerSiguienteNumeroAsync(proyectoId)` (UPDATE ... OUTPUT), `BloquearProyectoAsync`,
    `ObtenerIdsAncestrosAsync(id)`, `ActualizarAgrupacionDescendientesAsync(id, agrupacionId, ahora)`,
    `DesasociarAgrupacionAsync(proyectoId, agrupacionId, ahora)`, `ListarHijasAsync(id)`,
    `ContarPorTipoYEstadoAsync` (RN-12/RN-15), listado paginado y árbol.
    `UsuarioActualDto` agrega `Permisos`.
  - **Cambios en código de RF-001**: `AgrupacionService.EliminarAsync` (inyectar
    `IIncidenciaRepository`, bloquear proyecto, desasociar y borrar en la misma transacción;
    eliminar el comentario "RF futuro"); `AgrupacionDto` y `ProyectoDto` +
    `CantidadIncidencias` (consultas de `AgrupacionRepository`/`ConsultasProyecto`);
    `IUsuarioActual`/`UsuarioActualDesdeClaims`/`ProvisionalAuthenticationHandler`
    (claims `permiso`); `EsquemaProvisional.ClaimPermiso`; `SeguridadExtensions` (una política
    por permiso); `ManejadorResultadoAutorizacion` (mensaje 403 según política);
    `FabricaProblemas` (extensiones); `UsuarioActualService`/`UsuarioActualController`
    (permisos). `ProyectoRepository` no cambia el INSERT (usa el DEFAULT del contador).
  - Infrastructure.SqlServer: repositorios Dapper nuevos, CTE recursivas con `MAXRECURSION 100`,
    `ORDER BY` desde lista blanca, filtros `tipoId` múltiples parametrizados.
  - Api: controllers `NivelesIncidenciaController`, `TiposIncidenciaController`,
    `RelacionesTiposIncidenciaController`, `EstadosIncidenciaController`,
    `FlujosIncidenciaController`, `PrioridadesIncidenciaController`, `IncidenciasController`
    (ruta `api/proyectos/{proyectoId:int}/incidencias`; la ruta `arbol` debe declararse para no
    chocar con `{id:int}`, ya resuelto por la restricción `:int`).
- **Frontend (`dev-frontend`)**:
  - Features `incidencias` (I1–I5) y `configuracion-incidencias` (C1–C5), rutas lazy, menú.
  - Core: `UsuarioActualDto.permisos`, `UsuarioActualService.tienePermiso`, constantes de
    permisos; `convertir-error` expone extensiones (`incidencias`).
  - **Cambios en RF-001**: `ProyectoDetallePage` (botones, cantidad, diálogo de agrupación),
    `AgrupacionesListaComponent` (columna Incidencias), modelos `ProyectoDto`/`AgrupacionDto`,
    `app.routes.ts`, `ShellComponent`.
- **Tests (`dev-tests`)**:
  - Casos de la sección anterior con trazabilidad RN/CA; actualizar tests de RF-001 afectados
    (`AgrupacionService.Eliminar`, `UsuarioActualDesdeClaims`, handler provisional,
    `ManejadorResultadoAutorizacion`, `ProyectoDetallePage`, `UsuarioActualService`).
  - Recomendado: integración contra LocalDB para CA-14 (concurrencia de numeración) y CTE.

---

## Decisiones abiertas

Propuesta por defecto entre paréntesis; se avanza con ella si se aprueba la spec sin cambios.

- **DA-01 Transiciones del flujo**: ¿el flujo define transiciones permitidas entre estados?
  (Propuesta: no; transiciones libres entre los estados del flujo, RN-16. Si se requieren, se
  agrega una tabla `FlujosIncidenciaTransiciones (FlujoId, EstadoOrigenId, EstadoDestinoId)`.)
- **DA-02 Relaciones inmutables**: (Propuesta: se pueden agregar; no se modifican, eliminan ni
  desactivan, RN-09.) ¿Hace falta poder "dar de baja" una relación para nuevas incidencias sin
  afectar las existentes? No se expone `GET /{id}` de relaciones.
- **DA-03 Qué se edita de tipos y niveles**: (Propuesta: tipo → nombre, descripción, flujo
  (RN-12) y activo; **nivel inmutable**. Nivel → nombre y descripción; número inmutable; sin
  desactivar ni eliminar.) Alternativa: permitir cambiar el nivel de un tipo sin relaciones ni
  incidencias.
- **DA-04 Niveles y relaciones**: (Propuesta: una relación exige nivel del padre **menor** que
  el del hijo, RN-08; no se permiten relaciones dentro del mismo nivel ni hacia arriba. Esto
  acota la profundidad.) Alternativa: permitir cualquier par (incluso mismo tipo), confiando
  solo en la validación de ciclos.
- **DA-05 Vencimiento**: (Propuesta: hija sin vencimiento válida aunque el padre tenga; se
  compara solo con el padre directo, RN-31; adelantar el padre por debajo de una hija se
  rechaza, RN-32.) Alternativas: exigir vencimiento en la hija cuando el padre lo tiene;
  comparar con el ancestro más cercano con vencimiento; ajustar automáticamente las hijas.
- **DA-06 Descripción de incidencia**: (Propuesta: obligatoria, 1–4000, lectura literal del RF
  como en RF-001 DA-06.) ¿Opcional (p. ej. para subtareas)? ¿Mayor largo (`NVARCHAR(MAX)`)?
- **DA-07 Permisos provisionales**: (Propuesta: `EsAdmin` ⇒ todos los permisos, 0.1.) ¿Se prefiere
  otra regla transitoria (p. ej. lista de personas en configuración)?
- **DA-08 Cambio de flujo de un tipo**: (Propuesta: solo si todas sus incidencias están en
  estados del nuevo flujo, RN-12.) Alternativa: permitir con un mapeo de estados.
- **DA-09 Estado final**: (Propuesta: marca informativa; badge diferenciado y alerta de
  vencida solo en no finales.) ¿Debe tener efectos (impedir editar, exigir hijas finales para
  cerrar el padre)?
- **DA-10 Auditoría**: (Propuesta: sin `CreadoPorId`/"informante", coherente con RF-001
  DA-15.) ¿Se requiere registrar quién creó la incidencia?
- **DA-11 Árbol**: (Propuesta: árbol completo del proyecto en una sola llamada, sin paginación
  ni filtros.) ¿Volumen esperado por proyecto? Alternativa: filtrar por agrupación o carga
  diferida por nodo.
- **DA-12 Estado al cambiar de tipo**: (Propuesta: RN-23 — conservar si el nuevo flujo lo
  incluye; si no, inicial del nuevo flujo; el usuario puede elegir otro estado del nuevo flujo
  en el diálogo.)
- **DA-13 Concurrencia**: (Propuesta: bloqueo de la fila del proyecto en cada escritura de
  incidencias, 0.3; sin control optimista.) ¿Se esperan muchos usuarios escribiendo en el mismo
  proyecto a la vez?
- **DA-14 Flujos**: (Propuesta: no se desactivan ni eliminan.)
- **DA-15 Formato del código**: (Propuesta: número sin relleno, `PRJ-0001-15`, tal como el
  ejemplo.) ¿Relleno fijo (p. ej. `PRJ-0001-0015`)?
- **DA-16 Usuario en operaciones de incidencias**: (Propuesta: crear/editar incidencias no exige
  usuario identificado, coherente con RF-001 DA-05; solo cambiar tipo y configurar exigen
  permiso.)
- **DA-17 Agrupación en hijas**: (Propuesta: rechazar con `AGRUPACION_HEREDADA` si se envía un
  valor distinto al del padre, RN-28.) Alternativa: ignorar silenciosamente el valor enviado.
- **DA-18 Restricciones de fecha de vencimiento**: (Propuesta: ninguna respecto de la fecha
  actual ni de las fechas del proyecto.) ¿Debe ser ≤ `FechaObjetivo` del proyecto?
- **DA-19 Desactivar estados**: (Propuesta: un estado inicial de algún flujo no se puede
  desactivar; los demás sí, y las incidencias los conservan, RN-11.) Alternativa: bloquear la
  desactivación mientras haya incidencias en ese estado.

## Preguntas abiertas

Ver "Decisiones abiertas" (DA-01 a DA-19). No hay otras preguntas pendientes.
