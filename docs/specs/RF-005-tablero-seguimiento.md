# RF-005 – Tablero de seguimiento

> Versión: 1.2.2 (1.2 APROBADA + ajuste de semilla + tarjetas no clicables en T1; decisiones DA-01 a DA-21 tomadas; sin preguntas abiertas) · Fecha: 2026-10-10
> Depende de RF-001 y RF-002 (aprobadas e implementadas). Respeta sus bases técnicas
> (RF-001 0.1 a 0.4, RF-002 0.1 a 0.4), sus catálogos de errores y sus convenciones de API.
> La numeración RN/CA/DA es propia de este RF. Cuando se cita una regla de otro RF se escribe
> "RF-001 RN-xx" / "RF-002 RN-xx".
>
> **Este RF modifica RF-002** en dos puntos, documentados aquí (sección "Cambios sobre
> RF-002"): (1) nuevo campo `Estimacion` en `Incidencias` (RF-002 lo tenía fuera de alcance) y
> (2) el cálculo de "vencida" pasa a hacerse en el servidor para todas las pantallas.

### Cambios respecto de v1.1

| Tema | v1.1 | v1.2 |
|---|---|---|
| Vistas de T2 (DA-16, opción A) | Resumen, **Lista**, Tablero, Jerarquía | **Resumen, Tablero, Jerarquía**. Se elimina la vista Lista; el Tablero es la única vista de incidencias en grilla del seguimiento |
| Filtros del Tablero | Solo ámbito y alcance (niveles/tipos) | Ámbito + alcance + filtros que tenía la Lista: **`indicador`**, **`estadoId`** y **`texto`** (aplicados en el servidor). Se combinan con AND (RN-21) |
| Destino de los clics del Resumen | Vista Lista con `indicador` / `estadoId` | **Vista Tablero** con `indicador` / `estadoId` (RN-24) |
| `GET .../seguimiento/tablero` | Query: `agrupacionId`, `sinAgrupacion`, `nivelId`, `tipoId` | + `indicador`, `estadoId`, `texto`; response agrega `filtros` (eco de los filtros aplicados) y `vencida` en cada ítem; `total` = ítems filtrados |
| Listado I1 de RF-002 (`GET .../incidencias`) | Extendido con `nivelId`, `indicador`, `ordenarPor=estimacion`, `estimacion`, `vencida` | **Se quitan `nivelId` e `indicador`** (solo los usaba la Lista de T2). Se mantienen `ordenarPor=estimacion`, `estimacion` y `vencida` (DA-14, DA-03) |
| Condición SQL compartida de indicador | Usada por el listado I1 | Usada por la grilla del tablero (0.2) |
| Orden "por Tipo" (DA-17) | Propuesta | **Confirmado**: nivel del tipo, nombre del tipo, Nro (RN-32) |
| Totales de estimación (DA-18) | Propuesta | **Confirmado: no** se suman horas estimadas en ningún lado (RN-35) |
| Configuración en proyecto archivado (DA-19) | Propuesta | **Confirmado**: 409 `PROYECTO_ARCHIVADO` (RN-31) |
| RN / CA | RN-21 y RN-24 sobre la Lista; CA-24 a CA-26 sobre el listado | RN-07, RN-20, RN-21, RN-24, RN-28, RN-33, RN-36 ajustadas; CA-24 a CA-26 reescritos sobre el Tablero; nuevos **CA-48** y **CA-49** |
| Decisiones | DA-16 a DA-19 abiertas | Todas tomadas; sin preguntas abiertas |

### Cambios v1.2 → ajuste semilla (v1.2.1)

| Tema | v1.2 | v1.2.1 |
|---|---|---|
| Ubicación de "Bloqueada" en el "Flujo general" (S004, DA-20 opción A) | `Orden` = máximo + 1 (quedaba después de "Hecho"); no se reordenan filas | **Inmediatamente antes del primer estado final**: toma su `Orden` y final(es) y posteriores corren +1 (1 Por hacer, 2 En progreso, 3 Bloqueada, 4 Hecho). Sin final: máximo + 1. Si ya estaba después del final se reubica; si ya está antes, no se toca. Excepción limitada a `FlujosIncidenciaEstados.Orden` del Flujo general. CA-37 ajustado |

### Cambios v1.2.1 → v1.2.2

| Tema | v1.2.1 | v1.2.2 |
|---|---|---|
| Clic en tarjetas de indicadores en T1 (DA-21 opción A) | `IndicadoresComponent` clicable también en T1, sin destino definido | En T1 las tarjetas son **solo informativas** (`clicable = false`); en T2 siguen clicables hacia el Tablero filtrado (sin cambios). Ajustados T1, `IndicadoresComponent` y casos de prueba frontend; nuevo **CA-50**. Sin cambios en API ni modelo de datos |

### Cambios respecto de v1.0

| Tema | v1.0 | v1.1 |
|---|---|---|
| Vista Tablero (T2) | Columnas por estado con tarjetas, máx. 50 por columna | **Grilla de incidencias** con columnas de datos (Tipo, Nro, Título, Estado, Estimación, Responsable), orden por defecto Tipo + Nro y orden por clic en cliente (DA-12) |
| Configuración del tablero | – | Columnas visibles **por proyecto** (Estado, Estimación, Responsable configurables), tabla nueva `ConfiguracionesTableroProyecto`, permiso nuevo `Proyectos.ConfigurarTablero` (DA-15) |
| Estimación | Fuera de alcance (RF-002) | `Incidencias.Estimacion DECIMAL(9,2) NULL` en horas; alta, edición, detalle, listado y tablero (DA-14) |
| "Hoy" / vencida | Fecha del servidor solo en seguimiento; I1/I2/I3 con fecha del navegador | Fecha de referencia del servidor (zona configurable) para **todo**; los DTO de incidencia devuelven `vencida` calculado (DA-03) |
| Semilla | Estados "En espera" y "Bloqueado" | Solo **"Bloqueada"** (categoría `BLOQUEADA`) en el "Flujo general" (DA-02) |
| Estado con categoría | No puede ser final | No puede ser **ni final ni inicial** (DA-06) |
| Endpoint `GET .../seguimiento/tablero` | `TableroEstadosDto` (columnas por estado) | **Reemplazado** por `TableroIncidenciasDto` (lista plana sin paginar) |
| Endpoints nuevos | – | `GET`/`PUT /api/proyectos/{p}/seguimiento/tablero/configuracion` |
| Decisiones | DA-01 a DA-13 abiertas | DA-01 a DA-15 tomadas; DA-16 a DA-19 abiertas |

## Resumen funcional

El usuario necesita **seguir el avance** de los proyectos sin recorrer incidencia por
incidencia: un tablero general con el resumen de todos los proyectos y, por cada proyecto o
agrupación, sus indicadores (cantidad por estado, vencidas, bloqueadas, en espera y sin
responsable), un tablero en forma de grilla con columnas de datos configurables por proyecto
(incluida la nueva **estimación en horas**), filtrable por indicador, estado y texto, y la
jerarquía expandible por niveles. Como la jerarquía mezcla niveles (Épica, Historia de Usuario,
Subtarea...), cada indicador debe **declarar qué niveles y tipos está contando**, y el usuario
debe poder elegirlos. Desde el tablero se navega al detalle de proyectos, agrupaciones e
incidencias.

## Alcance

- Incluye:
  - **Tablero general** (todos los proyectos): indicadores totales y una fila por proyecto con
    sus indicadores; expandible para ver las agrupaciones del proyecto.
  - **Seguimiento de un proyecto** o de **una agrupación** (o de las incidencias "sin
    agrupación" del proyecto), con tres vistas: Resumen (indicadores, **conteo por estado** y
    tabla por agrupación), Tablero (grilla con columnas de datos y filtros por indicador,
    estado y texto; destino de los clics del Resumen) y Jerarquía (DA-16).
  - Indicadores mínimos del RF: cantidad por estado, vencidas, bloqueadas, en espera, sin
    responsable (más total / abiertas / finalizadas como contexto). El requisito "tablero
    agrupado por estado" se cubre con el **conteo por estado** del Resumen (DA-12).
  - Filtro de **alcance** por niveles y tipos de incidencia, con leyenda visible del alcance y
    de la definición de cada indicador.
  - **Categoría de seguimiento** en el catálogo de estados de incidencia para determinar
    "bloqueada" y "en espera" (DA-01), con semilla del estado "Bloqueada" (DA-02).
  - **Configuración de columnas del tablero por proyecto**, con permiso nuevo
    `Proyectos.ConfigurarTablero` (DA-15).
  - **Estimación** (horas, decimal, opcional) en las incidencias: alta (I4), edición (I5),
    detalle (I3), listado (I1) y tablero (DA-14). **Cambio sobre RF-002.**
  - **Fecha de referencia del servidor** (zona horaria configurable) para "hoy" en seguimiento
    y en I1/I2/I3; la API devuelve `vencida` calculado (DA-03). **Cambio sobre RF-002.**
  - Extensiones de endpoints y pantallas de RF-002 (listado, árbol, detalle, alta/edición,
    estados, flujos) y de RF-001 (detalle de proyecto).
- No incluye:
  - Modificar incidencias desde el tablero (sin drag & drop, sin edición en línea). El tablero
    es de solo lectura; para modificar se navega a I3/I5 (DA-09).
  - Tablero tipo Kanban (carriles o columnas por estado, tarjetas). El tablero **no muestra las
    incidencias agrupadas** (DA-12).
  - Persistir el orden elegido en el tablero (ni en servidor, ni en `localStorage`, ni en la
    URL); configuración de columnas **por usuario**; reordenar columnas o agregar otras
    distintas de las seis definidas.
  - Agregación jerárquica ("rollup"): que una Épica cuente como bloqueada si una hija lo está,
    porcentaje de avance o **suma de estimaciones** de hijas. Cada incidencia cuenta por sí
    misma (RN-08, RN-35). No hay totales de horas estimadas en ninguna pantalla ni respuesta
    (DA-18).
  - Vista "Lista" dentro del seguimiento (eliminada en v1.2, DA-16). El listado I1 de RF-002
    sigue existiendo fuera del seguimiento, sin filtros por nivel ni por indicador.
  - Registro de horas consumidas / trabajo real, estimación en otras unidades (puntos, días).
  - Históricos, tendencias, burndown, gráficos temporales, exportación (PDF/Excel), tableros
    guardados por usuario, notificaciones/alertas.
  - Marca de bloqueo a nivel de incidencia (motivo, bloqueante) ni vínculos "bloquea a" entre
    incidencias.
  - Permisos para **consultar**: las consultas no exigen usuario (RN-25, DA-10). El único
    permiso nuevo es para modificar la configuración del tablero (RN-31).
  - Pantalla propia de "detalle de agrupación": el seguimiento por agrupación cumple ese rol
    (DA-08).
  - Estado semilla "En espera": no se crea; la categoría `EN_ESPERA` queda disponible para
    asignarla desde C3 (DA-02).

### Qué se reutiliza, qué se extiende y qué es nuevo

| Elemento | Situación | Detalle |
|---|---|---|
| `TiposIncidencia`, `NivelesIncidencia`, `FlujosIncidenciaEstados.EsFinal`, `Agrupaciones`, `Proyectos` | **Reutiliza** sin cambios | Fuente de los indicadores |
| `EstadosIncidencia` | **Extiende** | Nueva columna `Categoria` (BLOQUEADA / EN_ESPERA) |
| `Incidencias` | **Extiende** | Nueva columna `Estimacion` (cambio sobre RF-002) |
| `ConfiguracionesTableroProyecto` | **Nueva** | Columnas visibles del tablero por proyecto (1:1 opcional con `Proyectos`) |
| `Permisos` (RF-002 0.1) | **Extiende** | Nuevo permiso `Proyectos.ConfigurarTablero` |
| `GET /api/proyectos/{p}/incidencias` (listado I1) | **Extiende** (mínimo) | Orden `estimacion`; campos `estimacion` y `vencida`. Sin filtros nuevos (v1.2: se quitaron `nivelId` e `indicador`, DA-16) |
| `GET /api/proyectos/{p}/incidencias/arbol` (árbol I2) | **Extiende** | Filtros `agrupacionId` / `sinAgrupacion`; campo `vencida` |
| `GET /api/proyectos/{p}/incidencias/{id}` (detalle I3) | **Extiende** | Campos `estimacion` y `vencida` (también en `padre` e `hijas`) |
| `POST`/`PUT /api/proyectos/{p}/incidencias` (I4/I5) | **Extiende** | Campo `estimacion` en el request |
| `EstadoIncidenciaReferenciaDto` (en todos los DTO de incidencia) | **Extiende** | Nuevo campo `categoria` |
| `/api/estados-incidencia` (C3), `/api/flujos-incidencia` (C4) | **Extiende** | Campo `categoria`; regla RN-18 (ni final ni inicial) |
| `IncidenciasTablaComponent` (solo I1), `PaginadorComponent` (I1 y T1), `ArbolIncidenciasComponent`, `construirArbol`, `BadgeEstadoIncidenciaComponent`, `IncidenciaFormComponent`, `IncidenciaDatosComponent` | **Reutiliza / extiende** | Estimación, `vencida` del servidor, "expandir hasta nivel N", marcas de indicadores. T2 ya no usa `IncidenciasTablaComponent` (DA-16) |
| `estaVencida` + input `hoy` (fecha del navegador) en I1/I2 | **Se deja de usar** | Reemplazado por `vencida` de la API (RN-36) |
| P3 Detalle de proyecto (RF-001) | **Extiende** | Botón "Seguimiento" y enlace por agrupación |
| `GET /api/seguimiento/proyectos` | **Nuevo** | Tablero general |
| `GET /api/proyectos/{p}/seguimiento` | **Nuevo** | Resumen del proyecto / agrupación |
| `GET /api/proyectos/{p}/seguimiento/tablero` | **Nuevo** (contrato v1.2) | Grilla de incidencias del ámbito y alcance, filtrable por `indicador`, `estadoId` y `texto`, + columnas visibles |
| `GET`/`PUT /api/proyectos/{p}/seguimiento/tablero/configuracion` | **Nuevo** | Configuración de columnas del proyecto |
| Feature Angular `seguimiento` (T1, T2) | **Nueva** | |

### Cómo se determina cada indicador con el modelo (verificación)

| Concepto | ¿Existe en el modelo? | Determinación |
|---|---|---|
| Vencida | Sí: `Incidencias.FechaVencimiento DATE NULL` y `FlujosIncidenciaEstados.EsFinal` | Estado **no final** (en el flujo de su tipo) y `FechaVencimiento < fecha de referencia del servidor` (RN-11). Mismo criterio en todo el sistema (RN-36) |
| Bloqueada | Con este RF: `EstadosIncidencia.Categoria = BLOQUEADA` | Abierta y estado con categoría `BLOQUEADA` (RN-12, DA-01) |
| En espera | Con este RF: `EstadosIncidencia.Categoria = EN_ESPERA` | Abierta y estado con categoría `EN_ESPERA` (RN-13, DA-01). Sin estados con esa categoría vale 0 (DA-02) |
| Sin responsable | Sí: `Incidencias.ResponsableId INT NULL` | `ResponsableId IS NULL` y estado no final (RN-14, DA-04) |
| Relación incidencia–agrupación | Sí: `Incidencias.AgrupacionId` (FK compuesta con el proyecto), heredada del padre (RF-002 RN-28) | El ámbito "agrupación" son las incidencias con esa `AgrupacionId`; por la herencia, siempre jerarquías completas (RN-01) |
| Nivel de una incidencia | Sí: `TiposIncidencia.NivelId` → `NivelesIncidencia.Numero` | Filtro de alcance por nivel = por los tipos de ese nivel (RN-04) |
| Estado final | Sí, pero por flujo (`FlujosIncidenciaEstados.EsFinal`), no global | "Abierta" = estado no final en el flujo del tipo de la incidencia (RN-09) |

### Cambios sobre RF-002 (documentados en este RF)

| Cambio | Qué cambia en RF-002 | Dónde se detalla |
|---|---|---|
| **C-RF002-01 Estimación** | RF-002 "No incluye" listaba "estimaciones". Pasa a incluirse `Incidencias.Estimacion` (horas, `DECIMAL(9,2)`, opcional, `>= 0`). Afecta tabla `Incidencias`, `CrearIncidenciaRequest`, `EditarIncidenciaRequest`, `IncidenciaResumenDto` (listado), `IncidenciaDto` (detalle), validadores, I1, I3, I4, I5 | Modelo de datos, Contrato API ("Cambios en endpoints de RF-002"), RN-34/RN-35, Pantallas |
| **C-RF002-02 Vencida en servidor** | RF-002 DA-09 y las pantallas I1/I2/I3 calculaban "vencida" con la fecha del navegador (`estaVencida(fecha, esFinal, hoy)`). Pasa a calcularse en el backend con la fecha de referencia (0.1) y se devuelve como `vencida: boolean` en todos los DTO de incidencia; el frontend solo lo pinta | 0.1, Contrato API, RN-36, Pantallas |
| **C-RF002-03 Categoría del estado** | `EstadosIncidencia` agrega `Categoria`; RF-002 RN-14 (flujos) se extiende: un estado con categoría no puede ser inicial ni final (RN-18) | Modelo, Contrato, RN-17 a RN-19 |
| **C-RF002-04 Filtros y orden** | Listado: solo orden `estimacion` (v1.2 quita `nivelId` e `indicador`, que únicamente servían a la vista Lista eliminada; el filtro por indicador vive en `.../seguimiento/tablero`). Árbol: `agrupacionId`, `sinAgrupacion` (los usa la vista Jerarquía) | Contrato API |

---

## 0. Bases técnicas que agrega este RF

### 0.1 Fecha de referencia ("hoy") única para todo el sistema

"Hoy" (para vencidas) se calcula **siempre en el servidor** (DA-03):

- Application agrega `TimeProviderExtensions.HoyEn(this TimeProvider reloj, TimeZoneInfo zona)`
  → `DateOnly` (fecha actual en la zona indicada).
- Application agrega la abstracción `IFechaReferencia` con `DateOnly Hoy()`, implementada con
  `TimeProvider` + `OpcionesFechaReferencia` (`IOptions`). Se registra en DI y la usan
  **todos** los servicios que devuelven incidencias o indicadores.
- Configuración `FechaReferencia:ZonaHoraria` (id IANA, p. ej.
  `"America/Argentina/Buenos_Aires"`). Si no se configura, se usa
  `TimeProvider.LocalTimeZone` (zona del servidor). Un id inválido hace fallar el arranque
  (validación de opciones `ValidateOnStart`) con mensaje claro.
- Domain agrega la regla pura `ReglasVencimiento.EstaVencida(DateOnly? fechaVencimiento,
  bool esFinal, DateOnly fechaReferencia)` = `!esFinal && fechaVencimiento < fechaReferencia`
  (RN-11). Application la aplica al mapear **todos** los DTO de incidencia (listado, árbol,
  detalle, padre, hijas, tablero) y llena `vencida`.
- Las respuestas de seguimiento devuelven además `fechaReferencia` (`"yyyy-MM-dd"`) para la
  leyenda ("Datos al dd/MM/yyyy").
- La grilla del tablero con `indicador=vencidas` usa la misma fecha de referencia en SQL.
- El frontend **no** calcula "vencida" con la fecha del navegador en ninguna pantalla (RN-36).

### 0.2 Dónde viven las reglas de los indicadores

- Los repositorios devuelven **conteos crudos** agrupados por
  `(clave de grupo, EstadoId, EsFinal)`: `Cantidad`, `ConVencimientoAnterior` (vencimiento
  `< @FechaReferencia`) y `SinResponsable`. No aplican la regla "solo abiertas".
- Application arma los indicadores con una función pura (`CalculadoraIndicadores`) que aplica
  RN-09 a RN-16 combinando esas filas con la categoría y el carácter final de cada estado. Así
  las reglas se testean sin BD.
- Excepción en SQL: los filtros `indicador`, `estadoId` y `texto` de la grilla del tablero
  (RN-21) se aplican en la consulta. `indicador` usa un **único fragmento SQL**
  (`ConsultasSeguimiento.CondicionIndicador`, que replica RN-09 a RN-14) y un test de
  integración verifica que el tablero filtrado y el conteo del resumen coinciden (CA-25).
- El tablero general calcula los indicadores de **todos** los proyectos que cumplen el filtro y
  ordena/pagina en memoria en Application (volumen chico, DA-11).
- La grilla del tablero (T2) trae **todas** las incidencias del ámbito, alcance y filtros sin
  paginar; el ordenamiento por clic se hace en el **cliente** (DA-11, RN-33).

### 0.3 Consultas

- Agregados con `GROUP BY` y `SUM(CASE ...)`.
- Listas de ids (`tipoId`, `nivelId`) como parámetros expandidos por Dapper (`IN @TipoIds`),
  igual que RF-002.
- `ORDER BY` solo desde listas blancas. Orden por `estimacion`: nulos al final en ambas
  direcciones (`CASE WHEN Estimacion IS NULL THEN 1 ELSE 0 END`, portable).
- Upsert de la configuración del tablero con `UPDATE` y, si no afectó filas, `INSERT` dentro de
  la misma transacción (sin `MERGE`, por portabilidad).

---

## Modelo de datos

Esquema `dbo`, collation `*_CI_AS`, convenciones de nombres de RF-001/RF-002.

### Tabla `EstadosIncidencia` (se modifica)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| EstadosIncidencia | Categoria | NVARCHAR(20) | Sí | **Nueva**. Categoría de seguimiento del estado. `NULL` = sin categoría (valor de las filas existentes). `CK_EstadosIncidencia_Categoria`: `Categoria IS NULL OR Categoria IN (N'BLOQUEADA', N'EN_ESPERA')`. Editable (RN-17) |

Se modela como código de texto con `CHECK` (y no como dos `BIT`) porque las categorías son
excluyentes y el catálogo puede crecer sin cambiar la estructura; es portable. Invariante
mantenido por el backend (no expresable como CHECK): un estado con categoría no es **inicial
ni final** en ningún flujo (RN-18).

### Tabla `Incidencias` (se modifica – cambio sobre RF-002)

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| Incidencias | Estimacion | DECIMAL(9,2) | Sí | **Nueva**. Estimación en **horas**, hasta 2 decimales. `NULL` = sin estimar (valor de las filas existentes); `0` es un valor válido distinto de `NULL`. `CK_Incidencias_Estimacion`: `Estimacion IS NULL OR Estimacion >= 0`. Rango válido 0 – 9 999 999,99 (RN-34) |

### Tabla `ConfiguracionesTableroProyecto` (nueva)

Columnas visibles de la grilla del tablero de un proyecto. Relación 1:0..1 con `Proyectos`:
**la ausencia de fila significa configuración por defecto (todas visibles)** (RN-30). Se elige
tabla aparte en lugar de columnas en `Proyectos` para no tocar los DTO, consultas ni el alta
de proyectos de RF-001 y para representar naturalmente "sin configuración".

| Entidad | Campo | Tipo SQL | Nulo | Reglas |
|---|---|---|---|---|
| ConfiguracionesTableroProyecto | ProyectoId | INT | No | `PK_ConfiguracionesTableroProyecto`. `FK_ConfiguracionesTableroProyecto_Proyectos_ProyectoId` → `Proyectos(Id)`, `ON DELETE NO ACTION` |
| ConfiguracionesTableroProyecto | MostrarEstado | BIT | No | Columna "Estado" visible |
| ConfiguracionesTableroProyecto | MostrarEstimacion | BIT | No | Columna "Estimación" visible |
| ConfiguracionesTableroProyecto | MostrarResponsable | BIT | No | Columna "Responsable" visible |
| ConfiguracionesTableroProyecto | FechaModificacion | DATETIME2(0) | No | UTC, asignada por el backend en cada guardado |

Las columnas Tipo, Nro y Título no se persisten: son obligatorias (RN-29). No hay `DEFAULT` en
los `BIT`: el backend siempre informa los tres valores.

### Índice recomendado (rendimiento de los agregados)

- `IX_Incidencias_ProyectoId_EstadoId` sobre `(ProyectoId, EstadoId)`
  `INCLUDE (TipoId, AgrupacionId, ResponsableId, FechaVencimiento)`. Cubre los conteos por
  proyecto/agrupación sin leer la tabla base. `INCLUDE` existe también en PostgreSQL ≥ 11;
  documentarlo en `db/README.md`.

### Datos semilla (`S004`, idempotente; insertar solo lo que no existe por la clave indicada; no modifica filas existentes, salvo `FlujosIncidenciaEstados.Orden` del "Flujo general" según DA-20)

`EstadosIncidencia` (clave `Nombre`):

| Nombre | Descripcion | Orden | Activo | Categoria |
|---|---|---|---|---|
| Bloqueada | La incidencia no puede avanzar por un impedimento. | 24 | 1 | `BLOQUEADA` |

`FlujosIncidenciaEstados` del "Flujo general" (clave `(FlujoId, EstadoId)`): agregar
"Bloqueada" con `EsInicial = 0`, `EsFinal = 0`, ubicada **inmediatamente antes del primer
estado final** (DA-20). Sea `OrdenFinal` = `MIN(Orden)` de las filas del flujo con `EsFinal = 1`:

1. "Bloqueada" no está en el flujo y existe final: las filas del flujo con
   `Orden >= OrdenFinal` corren `Orden + 1` y se inserta "Bloqueada" con `Orden = OrdenFinal`.
   Con la semilla de RF-002 queda: 1 Por hacer (inicial), 2 En progreso, 3 Bloqueada,
   4 Hecho (final).
2. "Bloqueada" no está en el flujo y no hay estado final: se inserta con
   `Orden` = máximo orden actual del flujo + 1 (1 si el flujo está vacío).
3. "Bloqueada" ya está en el flujo con `Orden > OrdenFinal` (p. ej. versión previa de S004):
   las filas con `OrdenFinal <= Orden < Orden de Bloqueada` corren +1 y "Bloqueada" toma
   `Orden = OrdenFinal`.
4. "Bloqueada" ya está en el flujo con `Orden < OrdenFinal` (orden correcto o reordenado por un
   administrador), o no hay final: no se toca.

Excepción explícita aprobada por el usuario a "no modifica filas existentes", limitada a
`FlujosIncidenciaEstados.Orden` del "Flujo general"; no se modifican otras columnas ni otros
flujos. Si ya existe un estado "Bloqueada" se respeta tal cual; si el "Flujo general" no
existe, no se agrega a ningún flujo. Agregar estados a un flujo está permitido (RF-002 RN-15
solo restringe quitar).

**No** se crea estado "En espera" ni se asigna `EN_ESPERA` a ningún estado: el indicador "En
espera" vale 0 hasta que un administrador asigne esa categoría desde C3 (DA-02).

No se siembran filas en `ConfiguracionesTableroProyecto` (todos los proyectos quedan con la
configuración por defecto).

Scripts propuestos (continúan la numeración):
- `V010__agrega_categoria_a_estados_incidencia_e_indice_seguimiento.sql` (columna + CHECK con
  SQL dinámico como `V006`, e índice).
- `V011__agrega_estimacion_a_incidencias.sql` (columna + `CK_Incidencias_Estimacion`).
- `V012__crea_configuraciones_tablero_proyecto.sql`.
- `seed/S004__estado_bloqueada.sql`.

### Relaciones

```
Proyectos 1 ──o ConfiguracionesTableroProyecto (ProyectoId, 0..1)
```
El resto, sin cambios respecto de RF-002.

---

## Contrato API

Convenciones de RF-001 0.2/0.3 y RF-002: JSON camelCase, `DateOnly` `"yyyy-MM-dd"`, ids
repetibles en query (`nivelId=1&nivelId=2`), decimales como número JSON (`2.5`),
ProblemDetails con `codigo`. Header `X-Usuario-Id` opcional. Cualquier endpoint puede devolver
`401 USUARIO_INVALIDO` y `500`; no se repiten. **Ningún GET de este RF exige usuario ni
permiso** (RN-25); el `PUT` de configuración exige el permiso `Proyectos.ConfigurarTablero`
(RN-31).

### Resumen

| Método | Ruta | Request | Response | Códigos |
|---|---|---|---|---|
| GET | `/api/seguimiento/proyectos` | query (ver abajo) | `TableroProyectosDto` | 200, 400 |
| GET | `/api/proyectos/{proyectoId}/seguimiento` | query | `SeguimientoDto` | 200, 400, 404 |
| GET | `/api/proyectos/{proyectoId}/seguimiento/tablero` | query (ámbito, alcance, `indicador`, `estadoId`, `texto`) | `TableroIncidenciasDto` | 200, 400, 404 |
| GET | `/api/proyectos/{proyectoId}/seguimiento/tablero/configuracion` | – | `ConfiguracionTableroDto` | 200, 404 |
| PUT | `/api/proyectos/{proyectoId}/seguimiento/tablero/configuracion` | `ConfiguracionTableroRequest` | `ConfiguracionTableroDto` | 200, 400, 401, 403, 404, 409 |
| GET | `/api/proyectos/{proyectoId}/incidencias` (RF-002, **extiende**) | + `ordenarPor=estimacion` | `Paginado<IncidenciaResumenDto>` + `estimacion`, `vencida` | sin cambios (200, 400, 404) |
| GET | `/api/proyectos/{proyectoId}/incidencias/arbol` (RF-002, **extiende**) | + `agrupacionId`, `sinAgrupacion` | `NodoArbolIncidenciaDto[]` + `vencida` | 200, **400**, 404 |
| GET | `/api/proyectos/{proyectoId}/incidencias/{id}` (RF-002, **extiende**) | – | `IncidenciaDto` + `estimacion`, `vencida` (y `vencida` en `padre`/`hijas`) | sin cambios |
| POST/PUT | `/api/proyectos/{proyectoId}/incidencias[/{id}]` (RF-002, **extiende**) | + `estimacion` | `IncidenciaDto` | los de RF-002 (400 `VALIDACION` clave `estimacion`) |
| GET/POST/PUT | `/api/estados-incidencia[/{id}]` (RF-002, **extiende**) | `EstadoIncidenciaRequest` + `categoria` | `EstadoIncidenciaDto` + `categoria` | los de RF-002 + **409 `ESTADO_CATEGORIA_INCOMPATIBLE`** (PUT) |
| POST/PUT | `/api/flujos-incidencia[/{id}]` (RF-002, **nueva validación**) | sin cambios | sin cambios | los de RF-002 + **409 `ESTADO_CATEGORIA_INCOMPATIBLE`** |
| GET | `/api/usuario-actual` (RF-002) | – | `permisos` puede incluir `"Proyectos.ConfigurarTablero"` | sin cambios |
| Todos los que devuelven incidencias (RF-002) | | | `estado` agrega `categoria`; la incidencia agrega `vencida` | sin cambios |

**Eliminado respecto de v1.0**: el contrato `TableroEstadosDto` (columnas por estado con
tarjetas) y los parámetros `maxPorColumna`, `responsableId` y `sinResponsable` de
`.../seguimiento/tablero` (`texto` vuelve en v1.2 como filtro de la grilla).

**Eliminado respecto de v1.1** (DA-16): los parámetros `nivelId` e `indicador` del listado
`GET /api/proyectos/{proyectoId}/incidencias` (RF-002 queda solo con el orden `estimacion` y
los campos `estimacion`/`vencida`). El filtro por indicador pasa a `.../seguimiento/tablero`.

### Nuevos códigos de error (se agregan a `CodigosError`)

| Código | HTTP | Cuándo |
|---|---|---|
| `ESTADO_CATEGORIA_INCOMPATIBLE` | 409 | Asignar categoría a un estado que es **inicial o final** en algún flujo, o marcar como inicial o final en un flujo un estado con categoría (RN-18). `detail` nombra los flujos/estados |

Se reutilizan: `VALIDACION`, `PROYECTO_NO_ENCONTRADO`, `AGRUPACION_NO_ENCONTRADA`,
`PROYECTO_ARCHIVADO`, `USUARIO_INVALIDO`, `USUARIO_NO_IDENTIFICADO`, `PERMISO_DENEGADO`.

### Nuevo permiso (mecanismo de RF-002 0.1)

- `Permisos.ProyectosConfigurarTablero = "Proyectos.ConfigurarTablero"`, agregado a
  `Permisos.Todos`. Con el resolutor provisional (`ResolutorPermisosPorAdministrador`) lo
  tienen las personas activas con `EsAdmin = 1`.
- Política con el mismo nombre (`[Authorize(Policy = Permisos.ProyectosConfigurarTablero)]`)
  en el `PUT`; el servicio vuelve a verificar con `AsegurarPermiso`. 403 con `detail` = "La
  operación requiere el permiso 'Proyectos.ConfigurarTablero'.".
- `GET /api/usuario-actual` lo incluye en `permisos`; el frontend usa
  `tienePermiso('Proyectos.ConfigurarTablero')`.

### Parámetros comunes de alcance y ámbito

| Parámetro | Tipo | Default | Reglas |
|---|---|---|---|
| `nivelId` | int repetible | – (todos) | Ids de `NivelesIncidencia`. Inexistente → 400 `VALIDACION` (clave `nivelId`) (RN-05) |
| `tipoId` | int repetible | – (todos) | Ids de `TiposIncidencia` (activos o inactivos). Inexistente → 400 `VALIDACION` (clave `tipoId`) |
| `agrupacionId` | int | – | Solo endpoints de proyecto. Excluyente con `sinAgrupacion` (400). De otro proyecto o inexistente → 404 `AGRUPACION_NO_ENCONTRADA` (RN-01) |
| `sinAgrupacion` | bool | `false` | Solo endpoints de proyecto |

Si `nivelId` y `tipoId` vienen juntos y ningún tipo seleccionado pertenece a los niveles
seleccionados → 400 `VALIDACION` (clave `tipoId`: "Ninguno de los tipos seleccionados
pertenece a los niveles seleccionados.") (RN-05).

### DTOs compartidos

`AlcanceSeguimientoDto` (qué se contabiliza, RN-06):
```json
{
  "todosLosNiveles": false,
  "niveles": [ { "id": 2, "numero": 2, "nombre": "Nivel 2" } ],
  "todosLosTipos": true,
  "tipos": [ { "id": 2, "nombre": "Historia de Usuario", "activo": true, "nivelNumero": 2 } ]
}
```
- `niveles`: los seleccionados, o todos si no se filtró (orden `numero`).
- `tipos`: los **efectivamente contabilizados** (intersección de RN-04), orden
  `nivelNumero`, `nombre`.
- `todosLosNiveles` = no se envió `nivelId`. `todosLosTipos` = el conjunto efectivo es igual a
  todos los tipos de los niveles en `niveles` (aunque se hayan enviado `tipoId`).

`EstadoSeguimientoDto` (fila del conteo por estado):
```json
{ "id": 4, "nombre": "Bloqueada", "orden": 24, "activo": true, "categoria": "BLOQUEADA", "esFinal": false }
```
`esFinal` = el estado es final en **todos** los flujos de los tipos contabilizados que lo
incluyen (RN-15).

`IndicadoresDto`:
```json
{
  "total": 42, "abiertas": 30, "finalizadas": 12,
  "vencidas": 5, "bloqueadas": 2, "enEspera": 0, "sinResponsable": 7,
  "porEstado": [
    { "estado": { "id": 1, "nombre": "Por hacer",   "orden": 10, "activo": true, "categoria": null,        "esFinal": false }, "cantidad": 13 },
    { "estado": { "id": 2, "nombre": "En progreso", "orden": 20, "activo": true, "categoria": null,        "esFinal": false }, "cantidad": 15 },
    { "estado": { "id": 4, "nombre": "Bloqueada",   "orden": 24, "activo": true, "categoria": "BLOQUEADA", "esFinal": false }, "cantidad": 2 },
    { "estado": { "id": 3, "nombre": "Hecho",       "orden": 30, "activo": true, "categoria": null,        "esFinal": true  }, "cantidad": 12 }
  ]
}
```

`categoria` en `EstadoIncidenciaReferenciaDto` (RF-002, todos los DTO de incidencia):
```json
"estado": { "id": 4, "nombre": "Bloqueada", "activo": true, "esFinal": false, "categoria": "BLOQUEADA" }
```

### Tablero general

`GET /api/seguimiento/proyectos` — query:

| Parámetro | Tipo | Default | Reglas |
|---|---|---|---|
| `texto` | string | – | Contiene, sobre `Codigo` o `Nombre` del proyecto. Máx. 150 |
| `archivados` | `excluir` \| `solo` \| `incluir` | `excluir` | Igual que RF-001 |
| `nivelId`, `tipoId` | int repetible | – | Alcance (ver arriba) |
| `pagina` | int | 1 | ≥ 1 |
| `tamanioPagina` | int | 20 | 1–100 |
| `ordenarPor` | `codigo` \| `nombre` \| `total` \| `abiertas` \| `vencidas` \| `bloqueadas` \| `enEspera` \| `sinResponsable` | `codigo` | Lista blanca (sin distinguir mayúsculas, como RF-002) |
| `direccion` | `asc` \| `desc` | `desc` | Desempate siempre por `codigo` desc y luego `id` |

Response 200 `TableroProyectosDto`:
```json
{
  "fechaReferencia": "2026-10-10",
  "alcance": { "todosLosNiveles": true, "niveles": [ "..." ], "todosLosTipos": true, "tipos": [ "..." ] },
  "totales": { "total": 120, "abiertas": 85, "finalizadas": 35, "vencidas": 9, "bloqueadas": 4,
               "enEspera": 0, "sinResponsable": 14, "porEstado": [ "..." ] },
  "proyectos": {
    "items": [
      { "proyecto": { "id": 1, "codigo": "PRJ-0001", "nombre": "Portal clientes", "archivado": false,
                      "estado": { "id": 3, "nombre": "En curso", "esFinal": false, "activo": true, "esSistema": false },
                      "responsable": { "id": 2, "nombre": "Ana Gómez", "activo": true },
                      "fechaObjetivo": "2026-12-15" },
        "cantidadAgrupaciones": 2,
        "indicadores": { "total": 42, "abiertas": 30, "...": "..." } }
    ],
    "pagina": 1, "tamanioPagina": 20, "total": 3
  }
}
```
- `totales` suma **todos** los proyectos que cumplen `texto`/`archivados`, no solo la página
  (RN-27).
- Un proyecto sin incidencias en el alcance aparece con indicadores en cero (RN-02).
- Errores: 400 `VALIDACION` (parámetros inválidos, alcance inválido).

### Seguimiento de proyecto o agrupación

`GET /api/proyectos/{proyectoId}/seguimiento` — query: `agrupacionId`, `sinAgrupacion`,
`nivelId`, `tipoId`.

Response 200 `SeguimientoDto` (ámbito proyecto):
```json
{
  "fechaReferencia": "2026-10-10",
  "proyecto": { "id": 1, "codigo": "PRJ-0001", "nombre": "Portal clientes", "archivado": false },
  "ambito": "proyecto",
  "agrupacion": null,
  "alcance": { "...": "..." },
  "indicadores": { "total": 42, "abiertas": 30, "...": "..." },
  "agrupaciones": [
    { "agrupacion": { "id": 8, "nombre": "Módulo pagos" }, "indicadores": { "total": 30, "...": "..." } },
    { "agrupacion": { "id": 9, "nombre": "Módulo envíos" }, "indicadores": { "total": 0, "...": "..." } },
    { "agrupacion": null, "indicadores": { "total": 12, "...": "..." } }
  ]
}
```
- `ambito`: `"proyecto"` | `"agrupacion"` | `"sinAgrupacion"`.
- `agrupacion` (solo ámbito agrupación): `{ "id": 8, "nombre": "Módulo pagos", "descripcion": "Incidencias del circuito de pagos." }`.
- `agrupaciones`: solo en ámbito proyecto (en los otros, `[]`): todas las agrupaciones del
  proyecto por `nombre` asc (incluso con 0) y al final una fila `agrupacion: null` = "Sin
  agrupación" (RN-26).
- Funciona para proyectos archivados (RN-03).
- Errores: 400 `VALIDACION`; 404 `PROYECTO_NO_ENCONTRADO`; 404 `AGRUPACION_NO_ENCONTRADA`.

### Tablero (grilla de incidencias)

`GET /api/proyectos/{proyectoId}/seguimiento/tablero` — única vista de incidencias en grilla
del seguimiento (DA-16). Sin paginación ni ordenamiento por query (RN-20, RN-33). Query:

| Parámetro | Tipo | Default | Reglas |
|---|---|---|---|
| `agrupacionId`, `sinAgrupacion` | | | Ámbito; mismas reglas que el resumen (RN-01) |
| `nivelId`, `tipoId` | int repetible | – | Alcance; mismas reglas que el resumen (RN-04, RN-05) |
| `indicador` | `abiertas` \| `vencidas` \| `bloqueadas` \| `en-espera` \| `sin-responsable` | – (sin filtro) | Solo las incidencias que suman en ese indicador según RN-09 a RN-14, con la fecha de referencia de 0.1. Sin distinguir mayúsculas. Valor fuera de lista → 400 `VALIDACION` (clave `indicador`) |
| `estadoId` | int | – (sin filtro) | Solo las incidencias en ese estado. `<= 0` o estado inexistente en `EstadosIncidencia` → 400 `VALIDACION` (clave `estadoId`: "El estado no existe."). Un estado existente (activo o inactivo) sin incidencias en el ámbito/alcance devuelve `items` vacío |
| `texto` | string | – | Contiene (sin distinguir mayúsculas), sobre `Codigo` o `Titulo` de la incidencia, como el listado de RF-002. Se recorta; vacío = sin filtro. Más de 200 caracteres → 400 `VALIDACION` (clave `texto`) |

Los filtros `indicador`, `estadoId` y `texto` se combinan entre sí y con ámbito y alcance con
**AND** (RN-21). Combinaciones sin resultados (p. ej. `indicador=vencidas&estadoId=<Hecho>`)
no son error: devuelven `items` vacío.

Ejemplo: `GET /api/proyectos/1/seguimiento/tablero?nivelId=2&nivelId=3&indicador=bloqueadas`

Response 200 `TableroIncidenciasDto`:
```json
{
  "fechaReferencia": "2026-10-10",
  "proyecto": { "id": 1, "codigo": "PRJ-0001", "nombre": "Portal clientes", "archivado": false },
  "alcance": { "...": "..." },
  "filtros": {
    "indicador": "bloqueadas",
    "estado": null,
    "texto": null
  },
  "configuracion": { "proyectoId": 1, "personalizada": true, "mostrarEstado": true,
                     "mostrarEstimacion": false, "mostrarResponsable": true,
                     "fechaModificacion": "2026-10-10T13:00:00Z" },
  "total": 1,
  "items": [
    { "id": 15, "codigo": "PRJ-0001-15", "numero": 15, "titulo": "Pagar con tarjeta",
      "tipo": { "id": 2, "nombre": "Historia de Usuario", "activo": true, "nivelNumero": 2 },
      "estado": { "id": 4, "nombre": "Bloqueada", "orden": 24, "activo": true, "esFinal": false, "categoria": "BLOQUEADA" },
      "estimacion": 12.5,
      "vencida": true,
      "responsable": null }
  ]
}
```

Ejemplo sin filtros (`GET /api/proyectos/1/seguimiento/tablero`), extracto de `items`:
```json
[
  { "id": 3, "codigo": "PRJ-0001-3", "numero": 3, "titulo": "Checkout",
    "tipo": { "id": 1, "nombre": "Épica", "activo": true, "nivelNumero": 1 },
    "estado": { "id": 2, "nombre": "En progreso", "orden": 20, "activo": true, "esFinal": false, "categoria": null },
    "estimacion": null, "vencida": false,
    "responsable": { "id": 2, "nombre": "Ana Gómez", "activo": true } },
  { "id": 16, "codigo": "PRJ-0001-16", "numero": 16, "titulo": "Validar tarjeta",
    "tipo": { "id": 3, "nombre": "Subtarea", "activo": true, "nivelNumero": 3 },
    "estado": { "id": 1, "nombre": "Por hacer", "orden": 10, "activo": true, "esFinal": false, "categoria": null },
    "estimacion": 0, "vencida": false,
    "responsable": { "id": 5, "nombre": "Luis Pérez", "activo": false } }
]
```
y `"filtros": { "indicador": null, "estado": null, "texto": null }`.

- `filtros`: eco de los filtros aplicados, para chips y leyenda. `indicador` en minúsculas
  (valor canónico) o `null`; `estado` = `{ "id", "nombre", "activo", "categoria" }` del
  `estadoId` pedido o `null`; `texto` recortado o `null`.
- `items`: **todas** las incidencias del ámbito, alcance y filtros (abiertas y finalizadas,
  salvo que el `indicador` las restrinja), en el **orden por defecto** (RN-32). `total` =
  cantidad de `items`. Coherencia con el resumen del mismo ámbito, alcance y fecha (RN-20):
  sin filtros, `total = indicadores.total`; con `indicador=X`, `total` = indicador X; con
  `estadoId=E`, `total = porEstado[E].cantidad`.
- `estado` incluye `orden` (para ordenar en el cliente) y `esFinal` según el flujo del tipo.
- `vencida`: calculado en el servidor (RN-11, RN-36); se usa para la marca de alerta de la fila
  (no hay columna de vencimiento).
- El servidor devuelve siempre los datos de las seis columnas; el cliente oculta las que la
  `configuracion` marca como no visibles (RN-29). Los filtros no dependen de las columnas
  visibles (se puede filtrar por estado aunque la columna Estado esté oculta).
- Funciona para proyectos archivados.
- Errores: 400 `VALIDACION` (ámbito, alcance, `indicador`, `estadoId`, `texto`); 404
  `PROYECTO_NO_ENCONTRADO` / `AGRUPACION_NO_ENCONTRADA`.

Ejemplo 400:
```json
{ "type": "https://httpstatuses.io/400", "title": "Datos inválidos", "status": 400,
  "detail": "Hay errores de validación.", "instance": "/api/proyectos/1/seguimiento/tablero",
  "codigo": "VALIDACION", "errors": { "indicador": [ "El indicador 'foo' no es válido." ] },
  "traceId": "00-..." }
```

### Configuración del tablero

`GET /api/proyectos/{proyectoId}/seguimiento/tablero/configuracion` → 200
`ConfiguracionTableroDto`:
```json
{ "proyectoId": 1, "personalizada": false, "mostrarEstado": true,
  "mostrarEstimacion": true, "mostrarResponsable": true, "fechaModificacion": null }
```
- Sin fila en `ConfiguracionesTableroProyecto`: `personalizada = false`, los tres `mostrar*` en
  `true` y `fechaModificacion = null` (RN-30).
- No exige usuario. Funciona para proyectos archivados. 404 `PROYECTO_NO_ENCONTRADO`.

`PUT /api/proyectos/{proyectoId}/seguimiento/tablero/configuracion` — política
`Proyectos.ConfigurarTablero`. `ConfiguracionTableroRequest` (reemplazo completo):
```json
{ "mostrarEstado": true, "mostrarEstimacion": false, "mostrarResponsable": true }
```
→ 200 `ConfiguracionTableroDto` con `personalizada = true` y `fechaModificacion` = ahora (UTC).
Crea la fila si no existe o la actualiza (idempotente). Último en guardar gana (sin control de
concurrencia). Las columnas obligatorias no se envían; si vienen otros campos se ignoran.
Errores:
- 400 `VALIDACION`: algún `mostrar*` ausente o `null` (clave del campo).
- 401 `USUARIO_NO_IDENTIFICADO` (sin `X-Usuario-Id`); 403 `PERMISO_DENEGADO`.
- 404 `PROYECTO_NO_ENCONTRADO`.
- 409 `PROYECTO_ARCHIVADO` (proyecto archivado; confirmado en DA-19). Se reutiliza el guard
  `Proyecto.AsegurarEditable()` de RF-001 (RF-001 RN-22) y su `detail`.

Ejemplo 403:
```json
{ "type": "https://httpstatuses.io/403", "title": "Prohibido", "status": 403,
  "detail": "La operación requiere el permiso 'Proyectos.ConfigurarTablero'.",
  "instance": "/api/proyectos/1/seguimiento/tablero/configuracion",
  "codigo": "PERMISO_DENEGADO", "traceId": "00-..." }
```

### Cambios en endpoints de RF-002

**Listado** `GET /api/proyectos/{proyectoId}/incidencias` agrega (únicamente):

| Parámetro | Tipo | Default | Reglas |
|---|---|---|---|
| `ordenarPor` | + `estimacion` | | Se agrega a la lista blanca. Nulos al final en ambas direcciones; desempate por `Numero` |

No se agregan filtros al listado (v1.2, DA-16): `nivelId` e `indicador` de v1.1 se quitan
porque solo los usaba la vista Lista de T2, eliminada. Los demás filtros quedan como en
RF-002.

Cada ítem (`IncidenciaResumenDto`) agrega `"estimacion": 12.5` (o `null`) y
`"vencida": true|false` (RN-36):
```json
{ "id": 15, "codigo": "PRJ-0001-15", "numero": 15, "titulo": "Pagar con tarjeta",
  "tipo": { "...": "..." }, "estado": { "id": 4, "nombre": "Bloqueada", "activo": true, "esFinal": false, "categoria": "BLOQUEADA" },
  "prioridad": { "...": "..." }, "responsable": null,
  "fechaVencimiento": "2026-10-05", "vencida": true, "estimacion": 12.5,
  "agrupacion": { "id": 8, "nombre": "Módulo pagos" }, "padre": { "id": 3, "codigo": "PRJ-0001-3" },
  "cantidadHijas": 1, "fechaCreacion": "2026-10-09T14:00:00Z" }
```

**Árbol** `GET /api/proyectos/{proyectoId}/incidencias/arbol` agrega `agrupacionId` y
`sinAgrupacion` (excluyentes → 400 `VALIDACION`; agrupación de otro proyecto → lista vacía,
como el listado). Sin parámetros, el comportamiento es el de RF-002. Cada nodo agrega
`"vencida"`. (No se agrega `estimacion` al árbol.)

**Detalle** `GET /api/proyectos/{proyectoId}/incidencias/{id}`: `IncidenciaDto` agrega
`"estimacion"` y `"vencida"`; `IncidenciaReferenciaDto` (en `padre` e `hijas`) agrega
`"vencida"`.

**Alta** `POST /api/proyectos/{proyectoId}/incidencias` — `CrearIncidenciaRequest` agrega
`estimacion` (opcional; número o `null`):
```json
{ "tipoId": 3, "titulo": "Validar tarjeta", "descripcion": "Validar número y vencimiento.",
  "prioridadId": null, "responsableId": 2, "fechaVencimiento": "2026-11-20",
  "agrupacionId": null, "padreId": 15, "estimacion": 4.5 }
```
**Edición** `PUT /api/proyectos/{proyectoId}/incidencias/{id}` — `EditarIncidenciaRequest`
agrega `estimacion`. Como el PUT es reemplazo completo, **omitirla o enviar `null` deja la
incidencia sin estimación**.

Validación de `estimacion` (alta y edición) → 400 `VALIDACION` con clave `estimacion`:
- `< 0` → "La estimación no puede ser negativa."
- más de 2 decimales → "La estimación admite hasta 2 decimales."
- `> 9999999.99` → "La estimación no puede superar 9.999.999,99 horas."

Ejemplo 400:
```json
{ "type": "https://httpstatuses.io/400", "title": "Datos inválidos", "status": 400,
  "detail": "Hay errores de validación.", "instance": "/api/proyectos/1/incidencias",
  "codigo": "VALIDACION", "errors": { "estimacion": [ "La estimación no puede ser negativa." ] },
  "traceId": "00-..." }
```

**Estados** `EstadoIncidenciaRequest` agrega `categoria` (opcional; `null`, `"BLOQUEADA"`,
`"EN_ESPERA"`):
```json
{ "nombre": "Esperando cliente", "descripcion": "Aguarda respuesta del cliente.", "orden": 22, "categoria": "EN_ESPERA" }
```
Valor fuera de lista → 400 `VALIDACION` (clave `categoria`). En `PUT`, asignar categoría a un
estado **inicial o final** en algún flujo → 409 `ESTADO_CATEGORIA_INCOMPATIBLE`. En `POST` no
aplica (un estado nuevo no está en ningún flujo). `EstadoIncidenciaDto` agrega `"categoria"`.

**Flujos** `POST`/`PUT /api/flujos-incidencia`: marcar `esInicial = true` o `esFinal = true`
un estado con categoría → 409 `ESTADO_CATEGORIA_INCOMPATIBLE`.

Ejemplo 409:
```json
{ "type": "https://httpstatuses.io/409", "title": "Conflicto", "status": 409,
  "detail": "El estado 'Bloqueada' tiene categoría de seguimiento y no puede ser inicial ni final en el flujo 'Flujo general'.",
  "instance": "/api/flujos-incidencia/1", "codigo": "ESTADO_CATEGORIA_INCOMPATIBLE", "traceId": "00-..." }
```

---

## Reglas de negocio y validaciones

Ámbito
- **RN-01** El ámbito de seguimiento de un proyecto es: (a) **proyecto**: todas sus
  incidencias; (b) **agrupación**: las incidencias con `AgrupacionId` = la agrupación (por
  RF-002 RN-28 siempre son jerarquías completas); (c) **sin agrupación**: las incidencias con
  `AgrupacionId IS NULL`. `agrupacionId` y `sinAgrupacion` son excluyentes (400 `VALIDACION`).
  Una agrupación inexistente o de otro proyecto → 404 `AGRUPACION_NO_ENCONTRADA` en los
  endpoints de seguimiento.
- **RN-02** El tablero general incluye los proyectos que cumplen `texto` y `archivados`
  (default `excluir`), tengan o no incidencias; sin incidencias en el alcance, sus indicadores
  valen cero.
- **RN-03** El seguimiento de un proyecto archivado se puede consultar (solo lectura); se
  muestra el banner de archivado.

Alcance (niveles y tipos)
- **RN-04** Conjunto de tipos contabilizados = tipos (activos e inactivos) cuyo nivel está en
  los `nivelId` enviados (o todos si no se envían) **intersección** los `tipoId` enviados (o
  todos si no se envían). Solo se cuentan incidencias de esos tipos.
- **RN-05** `nivelId`/`tipoId` inexistentes, o una intersección vacía, → 400 `VALIDACION` en
  los endpoints de seguimiento (incluido `.../seguimiento/tablero`). El listado de RF-002 no
  recibe `nivelId` y su `tipoId` mantiene el comportamiento de RF-002.
- **RN-06** Por defecto se contabilizan **todos los niveles y todos los tipos** (DA-05). El
  alcance elegido vive en la URL (query params); no se recuerda entre sesiones. Toda respuesta
  de seguimiento incluye `alcance` con los niveles y tipos efectivos, y toda pantalla que
  muestre indicadores muestra la leyenda de alcance (RN-23).
- **RN-07** El alcance se aplica a indicadores, conteo por estado y tablero de la misma
  pantalla. La jerarquía muestra todas las incidencias del ámbito (para no romper el árbol),
  atenuando las que no están en el alcance. Los filtros propios del tablero (`indicador`,
  `estadoId`, `texto`, RN-21) solo afectan a la vista Tablero: no cambian los indicadores del
  Resumen ni la Jerarquía.
- **RN-08** Cada incidencia se cuenta por sí misma, una vez por indicador: no se acumulan
  hijas en el padre ni se excluyen padres con hijas. Una misma incidencia puede sumar en varios
  indicadores (p. ej. vencida, bloqueada y sin responsable).

Definiciones de indicadores
- **RN-09** Una incidencia está **finalizada** si su estado es final en el flujo de su tipo
  (`FlujosIncidenciaEstados.EsFinal`, como RF-002); si no, está **abierta**.
- **RN-10** La fecha de referencia ("hoy") es la fecha actual del servidor en la zona horaria
  configurada (`FechaReferencia:ZonaHoraria`; por defecto la zona del servidor) (0.1). Es la
  misma para seguimiento y para las pantallas de incidencias de RF-002; se devuelve en
  `fechaReferencia` en las respuestas de seguimiento.
- **RN-11** **Vencida**: abierta, con `FechaVencimiento` no nula y **anterior** a la fecha de
  referencia (la que vence hoy no está vencida).
- **RN-12** **Bloqueada**: abierta y su estado tiene `Categoria = BLOQUEADA`.
- **RN-13** **En espera**: abierta y su estado tiene `Categoria = EN_ESPERA`. Si ningún estado
  tiene esa categoría, el indicador vale 0.
- **RN-14** **Sin responsable**: abierta y `ResponsableId IS NULL`. Un responsable inactivo
  cuenta como asignado (DA-04).
- **RN-15** **Cantidad por estado**: cuenta todas las incidencias del alcance (abiertas y
  finalizadas) agrupadas por `EstadoId`. Se listan los estados **activos** incluidos en los
  flujos de los tipos contabilizados, más cualquier estado (aunque esté inactivo) con
  cantidad > 0; orden `EstadosIncidencia.Orden`, `Nombre`, `Id`. `esFinal` del estado = final
  en todos los flujos de los tipos contabilizados que lo incluyen. Es la vista "agrupada por
  estado" que pide el RF (DA-12).
- **RN-16** `total` = suma de `porEstado.cantidad` = `abiertas` + `finalizadas`.

Categoría de seguimiento
- **RN-17** Cada estado de incidencia tiene categoría opcional (`null`, `BLOQUEADA`,
  `EN_ESPERA`). Se asigna al crear o editar el estado, con el permiso
  `Configuracion.Incidencias` (RF-002 RN-01). Varios estados pueden tener la misma categoría.
- **RN-18** Un estado con categoría no puede ser **inicial ni final** en ningún flujo: se
  rechaza (409 `ESTADO_CATEGORIA_INCOMPATIBLE`) (a) asignar categoría a un estado que es
  inicial o final en algún flujo y (b) marcar como inicial o como final en un flujo a un estado
  con categoría (DA-06). Extiende RF-002 RN-14.
- **RN-19** Cambiar la categoría de un estado no modifica incidencias; los indicadores reflejan
  el cambio en la consulta siguiente.

Vistas
- **RN-20** Tablero: es la **única** vista de incidencias en grilla del seguimiento (T2 tiene
  las vistas Resumen, Tablero y Jerarquía; no hay vista Lista, DA-16). Muestra **todas** las
  incidencias del ámbito, alcance y filtros de RN-21 (abiertas y finalizadas, salvo que el
  filtro las restrinja), una fila por incidencia, sin agrupar por estado ni por ningún otro
  criterio y sin paginar. Para un mismo ámbito, alcance y fecha de referencia: sin filtros, la
  cantidad de filas es igual a `indicadores.total` del resumen; con `indicador=X`, igual al
  indicador X; con `estadoId=E`, igual a `porEstado[E].cantidad`.
- **RN-21** Filtros del tablero (aplicados en el servidor, combinados con AND entre sí y con
  ámbito y alcance): (a) `indicador` (`abiertas`, `vencidas`, `bloqueadas`, `en-espera`,
  `sin-responsable`): incidencias que suman en ese indicador según RN-09 a RN-14; (b)
  `estadoId`: incidencias en ese estado; debe existir en el catálogo (si no, 400 `VALIDACION`);
  (c) `texto`: contiene sobre código o título, máx. 200 caracteres. Todos son opcionales; sin
  ellos se ve todo el ámbito y alcance. Viven en la URL de T2 (query params) junto con el
  ámbito y el alcance, y se conservan al recargar.
- **RN-22** Jerarquía: árbol del ámbito (RF-002 I2 con `agrupacionId`/`sinAgrupacion`); se
  puede expandir/colapsar cada nodo, todo, o "hasta el nivel N": se expanden los nodos cuyo
  tipo tiene `nivelNumero < N` y se colapsan los demás.
- **RN-23** Toda vista con indicadores muestra: (a) la leyenda de alcance ("Se contabilizan:
  <niveles> · Tipos: <tipos>" o "todos los niveles y tipos"); (b) la definición de cada
  indicador con la fecha de referencia.
- **RN-24** Navegación: código de proyecto → P3; nombre de agrupación → T2 con esa agrupación;
  Nro de incidencia o fila del tablero, código de nodo de la jerarquía → I3; tarjeta de
  indicador del Resumen → vista **Tablero** con `indicador` (sin `estadoId` ni `texto`); la
  tarjeta "Total" → vista Tablero sin filtros; estado del conteo → vista **Tablero** con
  `estadoId` (sin `indicador` ni `texto`). Se conservan ámbito y alcance en todos los casos.

Seguridad y consistencia
- **RN-25** Las consultas (GET) de seguimiento y de configuración del tablero no exigen usuario
  ni permiso (coherente con RF-001 DA-05 y RF-002 DA-16). La única escritura de este RF es el
  `PUT` de configuración del tablero (RN-31).
- **RN-26** En ámbito proyecto, cada indicador del proyecto = suma del indicador en las filas
  de `agrupaciones` (incluida "Sin agrupación").
- **RN-27** `totales` del tablero general = suma de los indicadores de todos los proyectos que
  cumplen el filtro (todas las páginas).
- **RN-28** Validaciones de query (rangos, listas blancas, excluyentes, largo de texto,
  `indicador` fuera de lista, `estadoId` inexistente) → 400 `VALIDACION` con clave del
  parámetro; el frontend no envía combinaciones inválidas.

Columnas y orden del tablero
- **RN-29** Columnas del tablero, en este orden fijo: **Tipo**, **Nro**, **Título**, Estado,
  Estimación, Responsable. Tipo, Nro y Título son **obligatorias** (siempre visibles, no se
  pueden ocultar). Estado, Estimación y Responsable son configurables (mostrar/ocultar). Nro
  muestra `numero` de la incidencia (con el `codigo` como `title`).
- **RN-30** La configuración de columnas es **por proyecto** y aplica a todos los usuarios y a
  todos los ámbitos del proyecto (proyecto, cada agrupación, sin agrupación). Si el proyecto no
  tiene configuración guardada, todas las columnas son visibles.
- **RN-31** Modificar la configuración exige el permiso `Proyectos.ConfigurarTablero` (401
  `USUARIO_NO_IDENTIFICADO` sin usuario, 403 `PERMISO_DENEGADO` sin permiso). En un proyecto
  archivado se rechaza con 409 `PROYECTO_ARCHIVADO` (DA-19). Los tres valores son
  obligatorios en el request (400 `VALIDACION`).
- **RN-32** Orden por defecto del tablero: **Tipo** (nivel del tipo `nivelNumero` asc, luego
  nombre del tipo asc) y **Nro** (`numero` asc). Lo aplica el servidor en `items` y el cliente
  al restablecer (DA-17, confirmado por el usuario).
- **RN-33** El usuario ordena haciendo clic en el encabezado de cualquier columna visible:
  primer clic ascendente, siguiente descendente, y así alternando; un clic en otra columna
  empieza en ascendente. Botón "Orden predeterminado" vuelve a RN-32. El orden se calcula en el
  **cliente** y **no se persiste** (al recargar o cambiar de ámbito, alcance o filtros de
  RN-21 vuelve al de RN-32). Criterio por columna: Tipo = RN-32; Nro = `numero`; Título = alfabético sin
  distinguir mayúsculas ni acentos (`localeCompare` `es`, `sensitivity: 'base'`); Estado =
  `estado.orden`, luego `estado.nombre`; Estimación = numérico; Responsable = nombre. Valores
  nulos (sin estimación, sin responsable) siempre **al final**, en ambas direcciones.
  Desempate siempre por el orden de RN-32.

Estimación
- **RN-34** `Estimacion` es opcional, en horas, con hasta 2 decimales, `>= 0` y
  `<= 9999999.99`. `0` es válido y distinto de "sin estimación" (`null`). Se informa en alta y
  edición; en edición, no enviarla la deja en `null` (reemplazo completo).
- **RN-35** La estimación es propia de cada incidencia: no se valida contra padre ni hijas, no
  se suma ni se hereda en la jerarquía y no afecta indicadores. **No se calculan ni muestran
  totales de horas estimadas** en ninguna respuesta ni pantalla (tablero, resumen, tablero
  general, listado, jerarquía) (DA-18).

Fecha de referencia en incidencias
- **RN-36** Los DTO de incidencia de RF-002 (listado, árbol, detalle, padre, hijas) y los
  ítems del tablero informan `vencida` calculado en el servidor según RN-11 con la fecha de
  referencia de RN-10. Las pantallas I1, I2, I3 y las vistas Tablero y Jerarquía de T2 muestran
  la alerta de vencida usando ese campo y no la fecha del navegador. (La grilla del tablero no
  tiene columna de vencimiento: la alerta se muestra como marca en la celda Nro.)

---

## Pantallas (Angular)

Estructura nueva: `frontend/src/app/features/seguimiento/` (`seguimiento.routes.ts`,
`models/`, `services/`, `pages/`, `components/`, `utils/`).

Rutas (en `app.routes.ts`):
- `seguimiento` → T1 (lazy).
- `proyectos/:id/seguimiento` → T2 (lazy), declarada **antes** de `proyectos` (igual que
  `proyectos/:id/incidencias`).

Menú (shell): nuevo ítem "Seguimiento" (`/seguimiento`) después de "Proyectos". Visible para
todos.

Comunes a T1/T2:
- `LeyendaAlcanceComponent`: texto "Se contabilizan: todos los niveles y tipos de incidencia."
  o "Se contabilizan: Nivel 2 (Historia de Usuario) · Nivel 3 (Subtarea, Bug)." a partir de
  `alcance`; enlace "¿Qué cuenta cada indicador?" que despliega las definiciones:
  - Vencidas: "Abiertas con vencimiento anterior al dd/MM/yyyy."
  - Bloqueadas: "Abiertas en un estado de categoría Bloqueada (<nombres de estados>)."
  - En espera: "Abiertas en un estado de categoría En espera (<nombres>)."
  - Sin responsable: "Abiertas sin responsable asignado."
  - Por estado: "Todas las incidencias del alcance, incluidas las finalizadas."
  - Nota: "Cada incidencia se cuenta por sí misma (las épicas no suman a sus hijas)."
  Si no hay estados con alguna categoría: "No hay estados configurados como <Bloqueada / En
  espera>; el indicador siempre será 0." con enlace a C3 (si tiene permiso
  `Configuracion.Incidencias`). Con la semilla, este aviso aparece para "En espera".
- `FiltroAlcanceComponent`: multi-selección de Niveles (por número) y de Tipos agrupados por
  nivel (inactivos con "(inactivo)"); al elegir niveles, la lista de tipos se limita a esos
  niveles y se quitan los tipos que quedaron fuera. Botón "Todos". Valores en query params
  (`nivelId`, `tipoId` repetidos). Datos: `GET /api/niveles-incidencia`,
  `GET /api/tipos-incidencia`.
- `IndicadoresComponent`: tarjetas Total, Abiertas, Vencidas, Bloqueadas, En espera, Sin
  responsable; cada tarjeta muestra el valor, un subtítulo corto con el alcance ("Niveles 2–3")
  y `title` con la definición. Vencidas/Bloqueadas con estilo de alerta si > 0.
  Input `clicable: boolean` (default `true`): si es `true` cada tarjeta es un botón y emite
  `indicadorSeleccionado` (`total` para la tarjeta Total); si es `false` (modo informativo,
  usado en T1, DA-21) las tarjetas se renderizan como elementos no interactivos (sin cursor
  de enlace, sin `tabindex`/rol de botón) y no emiten eventos.
- `ConteoPorEstadoComponent`: barra horizontal apilada + tabla Estado (badge con ícono de
  categoría) / Cantidad / %; finales con estilo diferenciado.
- Estados de carga ("Cargando...") y error (mensaje de `detail`; 404 → "No se encontró el
  recurso").
- Fechas `dd/MM/yyyy`; la fecha de referencia se muestra: "Datos al dd/MM/yyyy".
- Estimación: `formatearEstimacion(valor)` → número con hasta 2 decimales en formato `es-AR`
  seguido de " h" (`12,5 h`, `0 h`); `null` → "—".

### T1 – Tablero general · ruta `/seguimiento`
- Componentes: `TableroGeneralPage`, `FiltroAlcanceComponent`, `LeyendaAlcanceComponent`,
  `IndicadoresComponent` (con `totales`, `clicable = false`: tarjetas solo informativas, sin
  navegación; DA-21, CA-50), `TablaProyectosSeguimientoComponent`, `PaginadorComponent`.
- Filtros (query params; cambios vuelven a página 1; texto con debounce 300 ms): Texto
  (código/nombre, máx. 150), Archivados ("No archivados" default / "Solo archivados" /
  "Todos"), alcance. Botón "Limpiar filtros".
- Tabla: Proyecto (código + nombre, enlace a P3), Estado del proyecto (badge), Responsable,
  Fecha objetivo, Agrupaciones, Total, Abiertas, Vencidas, Bloqueadas, En espera, Sin
  responsable, mini-barra por estado. Encabezados ordenables (`ordenarPor`). Acciones por
  fila: "Seguimiento" (→ T2) y expandir (▸) que carga
  `GET /api/proyectos/{id}/seguimiento` con el mismo alcance y muestra las filas de
  agrupaciones (nombre → T2 con `agrupacionId`; "Sin agrupación" → T2 con `sinAgrupacion=true`).
- Vacío: "No hay proyectos que coincidan con los filtros."

### T2 – Seguimiento de proyecto / agrupación · ruta `/proyectos/:id/seguimiento`
- Query params: `agrupacionId` | `sinAgrupacion=true`, `nivelId`, `tipoId`,
  `vista=resumen|tablero|jerarquia` (default `resumen`; cualquier otro valor, incluido el
  antiguo `lista`, se trata como `resumen`), y los filtros del tablero `indicador`, `estadoId`,
  `texto` (RN-21). El orden del tablero **no** va en la URL (RN-33).
- Componentes: `SeguimientoProyectoPage`, `SelectorAmbitoComponent`, `FiltroAlcanceComponent`,
  `LeyendaAlcanceComponent`, pestañas de vista **Resumen · Tablero · Jerarquía** (DA-16: no
  hay pestaña Lista).
- Al cambiar de pestaña se conservan ámbito, alcance y filtros del tablero en la URL; Resumen
  y Jerarquía ignoran los filtros del tablero (RN-07).
- Encabezado: código y nombre del proyecto (enlace a P3); banner de archivado si corresponde;
  selector de **Ámbito**: "Todo el proyecto", cada agrupación, "Sin agrupación". En ámbito
  agrupación se muestran nombre y descripción de la agrupación (rol de "detalle de agrupación",
  DA-08) y, si el proyecto no está archivado, enlace "Editar agrupación" (P5).
- Enlaces "Volver al tablero general" y "Ver proyecto".
- **Vista Resumen**: `IndicadoresComponent` (`clicable = true`: tarjetas clicables), `ConteoPorEstadoComponent`
  (estado clicable) y, en ámbito proyecto, `TablaAgrupacionesSeguimientoComponent`
  (Agrupación, Total, Abiertas, Vencidas, Bloqueadas, En espera, Sin responsable; nombre → T2
  con esa agrupación). Navegación (RN-24), conservando ámbito y alcance:
  - Tarjeta "Total" → `vista=tablero` sin `indicador`, `estadoId` ni `texto`.
  - Tarjetas Abiertas / Vencidas / Bloqueadas / En espera / Sin responsable → `vista=tablero`
    con `indicador=abiertas|vencidas|bloqueadas|en-espera|sin-responsable`, quitando
    `estadoId` y `texto`. Ej.: "Vencidas: 5" abre el Tablero con 5 filas.
  - Estado del conteo → `vista=tablero&estadoId=<id>`, quitando `indicador` y `texto`.
  - Las tarjetas y estados tienen `cursor: pointer`, rol de enlace y `title` "Ver en el
    tablero".
- **Vista Tablero** (DA-12, DA-15, DA-16) — única vista de incidencias en grilla:
  - Componentes: `FiltrosTableroComponent` (nuevo), `TablaTableroComponent` (presentación) y
    `ConfigurarColumnasDialogComponent`. Datos: `GET .../seguimiento/tablero` con ámbito +
    alcance + filtros (trae `items`, `filtros` y `configuracion`).
  - `FiltrosTableroComponent` (arriba de la grilla), valores en query params:
    - Texto: input "Buscar por código o título", máx. 200 (`maxlength`), debounce 300 ms.
    - Estado: select "Todos" + estados de `GET /api/estados-incidencia` ordenados por `orden`
      (inactivos con "(inactivo)").
    - Indicador: select Todos / Abiertas / Vencidas / Bloqueadas / En espera / Sin
      responsable.
    - Chips por filtro activo ("Indicador: Vencidas", "Estado: Bloqueada" — nombre tomado de
      `filtros.estado` — y "Texto: «pago»"), cada uno con botón para quitarlo; botón "Limpiar
      filtros" (quita los tres, conserva ámbito y alcance), deshabilitado si no hay filtros.
    - Cada cambio de filtro vuelve a pedir el tablero al servidor y restablece el orden
      predeterminado (RN-33).
  - Grilla con las columnas visibles según RN-29 / `configuracion`:
    - Tipo: nombre del tipo ("(inactivo)" si corresponde).
    - Nro: `numero` como enlace a I3, `title` = `codigo`; si `vencida = true`, ícono de alerta
      con `title` "Vencida" (RN-36).
    - Título: texto (truncado con `title` completo si excede el ancho).
    - Estado: `BadgeEstadoIncidenciaComponent` (finales diferenciados, ícono de categoría).
    - Estimación: `formatearEstimacion`, alineada a la derecha.
    - Responsable: nombre ("(inactivo)" si corresponde) o "Sin responsable".
  - Encabezados clicables con indicador de dirección (▲/▼) y `aria-sort`; ordenamiento en el
    cliente según RN-33 con la util `ordenarTablero`. Botón "Orden predeterminado" (deshabilitado
    si ya está en el orden por defecto).
  - Contador: "N incidencias" (`total`); con algún filtro activo, "N incidencias (filtradas)".
    Clic en la fila → I3.
  - Botón "Columnas" visible si `tienePermiso('Proyectos.ConfigurarTablero')` y el proyecto no
    está archivado. Abre `ConfigurarColumnasDialogComponent`: checks Tipo, Nro y Título
    marcados y deshabilitados con texto "(obligatoria)"; checks Estado, Estimación y
    Responsable editables; texto "La configuración aplica al tablero de todo el proyecto y a
    todos los usuarios."; botones "Restablecer" (marca los tres), "Cancelar" y "Guardar".
    Guardar → `PUT .../tablero/configuracion`; éxito: toast "Configuración del tablero
    guardada." y la grilla aplica la nueva configuración sin recargar los datos (si la columna
    por la que estaba ordenado quedó oculta, vuelve al orden predeterminado). Errores 401/403/409:
    mensaje de `detail` dentro del diálogo.
  - Vacío sin filtros: "No hay incidencias en este ámbito y alcance."; vacío con filtros: "No
    hay incidencias que coincidan con los filtros." con botón "Limpiar filtros".
  - Error 400 (p. ej. `estadoId` inexistente escrito a mano en la URL): mensaje de `detail` y
    botón "Limpiar filtros".
  - Sin drag & drop ni edición en línea. Sin totales de estimación (RN-35).
- **Vista Jerarquía**: reutiliza `ArbolIncidenciasComponent` sobre
  `GET .../incidencias/arbol` con el ámbito; agrega:
  - Botones "Expandir todo", "Colapsar todo" y "Expandir hasta nivel: [1] [2] [3]..." (un botón
    por nivel configurado, RN-22).
  - Marcas en cada nodo: vencida (campo `vencida` del nodo), bloqueada, en espera (por
    `estado.categoria`), sin responsable (íconos con `title`).
  - Input `tipoIdsEnAlcance`: los nodos fuera del alcance se muestran atenuados con nota en la
    leyenda: "Los elementos atenuados no se contabilizan con el alcance actual."
  - Vacío: "No hay incidencias en este ámbito."
- Errores: 404 de agrupación → mensaje "La agrupación no existe en este proyecto." y enlace a
  "Todo el proyecto".

### Cambios en P3 – Detalle de proyecto (RF-001/RF-002)
- Botón "Seguimiento" (→ `/proyectos/:id/seguimiento`) junto a "Incidencias" y "Árbol".
- Tabla de agrupaciones: acción "Seguimiento" por fila (→ T2 con `agrupacionId`).

### Cambios en C3 – Estados de incidencia (RF-002)
- Columna "Categoría de seguimiento" (—, "Bloqueada", "En espera").
- Formulario: select "Categoría de seguimiento" (Ninguna / Bloqueada / En espera) con ayuda
  "Se usa en el tablero de seguimiento. Un estado con categoría no puede ser inicial ni final
  de un flujo."; si el estado es inicial o final en algún flujo, el select está deshabilitado
  con esa ayuda. 409 `ESTADO_CATEGORIA_INCOMPATIBLE` arriba del formulario.

### Cambios en C4 – Flujos de trabajo (RF-002)
- Radio "Inicial" y check "Final" deshabilitados en estados con categoría (ayuda "Estado de
  categoría <Bloqueada/En espera>: no puede ser inicial ni final"). 409
  `ESTADO_CATEGORIA_INCOMPATIBLE` arriba del formulario.

### Cambios en I1 / I2 / I3 / I4 / I5 (RF-002)
- **Todas**: `BadgeEstadoIncidenciaComponent` muestra un ícono para las categorías (candado =
  bloqueada, reloj = en espera) con `title`.
- **Vencida desde el servidor (RN-36)**: `IncidenciasTablaComponent` (I1),
  `ArbolIncidenciasComponent` (I2 y vista Jerarquía) e `IncidenciaDatosComponent` /
  `IncidenciaHijasComponent` (I3) aplican el estilo de alerta cuando `vencida = true`. Se
  elimina el input `hoy` y el uso de `estaVencida` + fecha del navegador en esos componentes.
- **I1 – Listado**: nueva columna "Estimación" después de "Responsable"
  (`formatearEstimacion`, alineada a la derecha), ordenable (`ordenarPor=estimacion`). Sin
  filtros nuevos (no hay filtro por nivel ni por indicador en I1).
- **I3 – Detalle**: dato "Estimación: 12,5 h" o "Estimación: Sin estimar" (debajo de
  Vencimiento).
- **I4 – Alta** y **I5 – Edición** (`IncidenciaFormComponent`): campo opcional "Estimación
  (horas)", `input type="number"`, `min=0`, `step=0.25`, placeholder "Sin estimar".
  Validaciones de formulario: `min(0)` → "La estimación no puede ser negativa."; validador
  `maxDecimales(2)` → "La estimación admite hasta 2 decimales."; `max(9999999.99)` → "La
  estimación no puede superar 9.999.999,99 horas.". Vacío se envía como `null`. En I5 se
  precarga el valor actual y siempre se envía `estimacion` en el PUT. Errores 400 con clave
  `estimacion` se muestran bajo el campo.

---

## Criterios de aceptación

Datos de ejemplo usados (salvo indicación): fecha de referencia del servidor 10/10/2026;
proyecto `PRJ-0001` con agrupaciones "Pagos" y "Envíos"; S004 aplicado; un admin con todos los
permisos y una persona no admin.

Tablero general
- **CA-01** Dados tres proyectos no archivados y uno archivado, cuando se abre T1 sin filtros,
  entonces se listan los tres no archivados con sus indicadores y `totales` suma los tres; con
  "Todos" se incluye el archivado. (GET `/api/seguimiento/proyectos`, T1)
- **CA-02** Dado un proyecto sin incidencias, cuando se consulta el tablero general, entonces
  aparece con todos los indicadores en 0. (GET seguimiento/proyectos, T1)
- **CA-03** Dados 25 proyectos y `tamanioPagina=20`, cuando se pide la página 2, entonces se
  reciben 5 proyectos, `total` = 25 y `totales` incluye los 25. (GET seguimiento/proyectos)
- **CA-04** Cuando se ordena por `vencidas` desc, entonces el proyecto con más vencidas aparece
  primero y los empates se ordenan por código desc. (GET seguimiento/proyectos, T1)
- **CA-05** Cuando se piden parámetros inválidos (`ordenarPor=foo`, `tamanioPagina=101`,
  `archivados=x`, texto de 151), entonces se recibe 400 `VALIDACION`. (GET seguimiento/proyectos)
- **CA-06** Dado T1, cuando se expande la fila de `PRJ-0001`, entonces se ven las filas
  "Pagos", "Envíos" y "Sin agrupación" con sus indicadores, y sus nombres llevan a T2 con ese
  ámbito; el código del proyecto lleva a P3. (T1, GET seguimiento de proyecto)

Indicadores
- **CA-07** Dadas en `PRJ-0001` incidencias abiertas con vencimiento 09/10 y 10/10, y una
  finalizada con vencimiento 01/10, cuando se consulta el resumen, entonces `vencidas` = 1 (solo
  la del 09/10). (GET seguimiento, T2)
- **CA-08** Dado un estado "Esperando cliente" con categoría `EN_ESPERA` agregado al flujo, y
  dos incidencias en "Bloqueada", una en "Esperando cliente" y una en "Hecho" sin responsable,
  cuando se consulta el resumen, entonces `bloqueadas` = 2, `enEspera` = 1 y la de "Hecho" no
  suma en `sinResponsable`. (GET seguimiento, T2)
- **CA-09** Dada una incidencia abierta, vencida, en "Bloqueada" y sin responsable, cuando se
  consulta el resumen, entonces suma 1 en `vencidas`, `bloqueadas` y `sinResponsable`. (GET seguimiento)
- **CA-10** Cuando se consulta el resumen, entonces `porEstado` incluye todos los estados
  activos de los flujos de los tipos contabilizados (con 0 si no hay incidencias), ordenados por
  orden del catálogo, y `total` = suma de `porEstado` = `abiertas` + `finalizadas`. (GET seguimiento, T2)
- **CA-11** Dado un estado desactivado con incidencias, cuando se consulta el resumen, entonces
  aparece en `porEstado` con su cantidad y `activo = false`. (GET seguimiento)
- **CA-12** Dada la instalación con S004 (ningún estado con categoría `EN_ESPERA`), cuando se
  abre T2, entonces `enEspera` = 0 y la leyenda avisa que no hay estados configurados como En
  espera. (GET seguimiento, T2)
- **CA-13** Dada una Épica con dos HU hijas, todas abiertas y vencidas, cuando se consulta con
  alcance por defecto, entonces `vencidas` = 3; con `nivelId=2`, `vencidas` = 2. (GET seguimiento, T2)

Alcance
- **CA-14** Cuando se abre T1 o T2 sin filtro de alcance, entonces la leyenda dice "Se
  contabilizan: todos los niveles y tipos de incidencia." y `alcance.todosLosNiveles` y
  `todosLosTipos` son true. (GET seguimiento, T1, T2)
- **CA-15** Cuando se elige el nivel 3 y los tipos Subtarea y Bug, entonces los indicadores
  solo cuentan incidencias de esos tipos, `alcance.tipos` contiene exactamente esos dos y la
  leyenda los nombra; los filtros quedan en la URL y se conservan al recargar. (GET seguimiento, T2)
- **CA-16** Cuando se envía `nivelId=1&tipoId=<Subtarea>` (sin intersección), o un `nivelId`
  inexistente, entonces se recibe 400 `VALIDACION`; el `FiltroAlcanceComponent` no permite esa
  combinación. (GET seguimiento, GET tablero, T2)
- **CA-17** Dado un tipo inactivo con incidencias, cuando se consulta sin alcance, entonces sus
  incidencias se cuentan y el tipo figura en `alcance.tipos` con `activo = false`. (GET seguimiento)
- **CA-18** Dado un alcance de nivel 3, cuando se abre la vista Jerarquía, entonces el árbol
  muestra también Épicas e HU, atenuadas, con la nota de la leyenda. (T2 jerarquía)

Ámbito proyecto / agrupación
- **CA-19** Dado `PRJ-0001` con incidencias en "Pagos", "Envíos" y sin agrupación, cuando se
  consulta el seguimiento del proyecto, entonces cada indicador del proyecto es la suma de las
  tres filas de `agrupaciones`. (GET seguimiento, T2 resumen)
- **CA-20** Cuando se consulta con `agrupacionId` de "Pagos", entonces `ambito = agrupacion`,
  se informan nombre y descripción, los indicadores cuentan solo las incidencias de "Pagos" y
  `agrupaciones` es `[]`. (GET seguimiento, T2)
- **CA-21** Cuando se consulta con `agrupacionId` de otro proyecto, entonces se recibe 404
  `AGRUPACION_NO_ENCONTRADA`; con `agrupacionId` y `sinAgrupacion=true`, 400 `VALIDACION`.
  (GET seguimiento, GET tablero)
- **CA-22** Dado un proyecto archivado, cuando se abre T2, entonces responde 200, se muestra el
  banner de archivado y no aparecen "Editar agrupación" ni el botón "Columnas". (GET seguimiento, GET tablero, T2)
- **CA-23** Cuando se consulta el seguimiento de un proyecto inexistente, entonces se recibe 404
  `PROYECTO_NO_ENCONTRADO` y T2 muestra "No se encontró el recurso". (GET seguimiento, GET tablero, T2)

Tablero filtrado y jerarquía
- **CA-24** Dado el resumen de `PRJ-0001` con alcance nivel 2 y `vencidas` = 5, cuando se hace
  clic en la tarjeta "Vencidas", entonces se abre la vista Tablero con
  `vista=tablero&nivelId=2&indicador=vencidas`, la grilla muestra esas 5 incidencias (todas
  con la marca de vencida), el chip "Indicador: Vencidas" y el contador "5 incidencias
  (filtradas)"; al quitar el chip se ven todas las del ámbito y alcance; clic en el estado
  "En progreso" del conteo abre el Tablero con `estadoId` de "En progreso" y sin `indicador`;
  clic en una fila abre I3. (T2, GET tablero)
- **CA-25** Para un mismo ámbito, alcance y fecha de referencia, cuando se consulta el tablero
  con cada `indicador` (`abiertas`, `vencidas`, `bloqueadas`, `en-espera`, `sin-responsable`),
  entonces su `total` coincide con el indicador del resumen; con cada `estadoId`, con
  `porEstado[E].cantidad`; y sin filtros, con `indicadores.total`. (GET tablero, GET
  seguimiento; test de integración)
- **CA-26** Cuando se llama al tablero con `indicador=foo`, `estadoId=0`, `estadoId=9999`
  (inexistente) o `texto` de 201 caracteres, entonces se recibe 400 `VALIDACION` con la clave
  del parámetro; con `indicador=VENCIDAS` se acepta y `filtros.indicador` = `"vencidas"`.
  (GET tablero)
- **CA-27** Dado un árbol Épica (nivel 1) → HU (nivel 2) → Subtarea (nivel 3), cuando se pulsa
  "Expandir hasta nivel 2", entonces se ven Épica y HU pero no la Subtarea; con "nivel 3" se ve
  todo; "Colapsar todo" deja solo las raíces; cada nodo se expande/colapsa individualmente.
  (T2 jerarquía)
- **CA-28** Cuando se abre la jerarquía en ámbito "Pagos", entonces solo se ven las jerarquías
  de "Pagos" (completas) y cada código lleva a I3. (GET arbol con `agrupacionId`, T2)

Tablero (grilla)
- **CA-29** Dado `PRJ-0001` sin configuración de tablero, con una Épica #3, una HU #15 y una
  Subtarea #16 y otra HU #7, cuando se abre la vista Tablero, entonces se ve una grilla (sin
  columnas por estado ni tarjetas) con las columnas Tipo, Nro, Título, Estado, Estimación y
  Responsable, una fila por incidencia en el orden Épica #3, HU #7, HU #15, Subtarea #16, y la
  cantidad de filas es igual a `total` del resumen. (GET tablero, T2)
- **CA-30** Dado el tablero, cuando se hace clic en "Estimación", entonces las filas se ordenan
  por estimación ascendente con las "—" al final; un segundo clic ordena descendente con las
  "—" igualmente al final; "Orden predeterminado" vuelve a Tipo + Nro; al recargar la página
  el orden vuelve a Tipo + Nro y no se hizo ninguna llamada de escritura. (T2, util
  `ordenarTablero`)
- **CA-31** Dado el tablero, cuando se ordena por Estado, entonces las filas siguen el orden
  del catálogo de estados (`orden`) y no el alfabético; con Responsable, las "Sin responsable"
  quedan al final; con Título, "árbol" y "Arbol" se consideran iguales y desempatan por Tipo +
  Nro. (T2)
- **CA-32** Dado el admin en T2 de `PRJ-0001`, cuando abre "Columnas", desmarca Estimación y
  guarda, entonces el PUT responde 200 con `personalizada = true`, la grilla deja de mostrar
  Estimación y cualquier otro usuario (o sin usuario) que abra el tablero de `PRJ-0001` o de
  una de sus agrupaciones tampoco la ve; otro proyecto sigue mostrando todas las columnas.
  (PUT/GET configuración, GET tablero, T2)
- **CA-33** Dado el diálogo "Columnas", cuando se observa, entonces Tipo, Nro y Título están
  marcados y deshabilitados; y un PUT con `mostrarEstado: null` o sin `mostrarResponsable`
  responde 400 `VALIDACION`. (T2, PUT configuración)
- **CA-34** Dada la persona no admin, cuando abre el tablero, entonces no ve el botón
  "Columnas"; si llama al PUT recibe 403 `PERMISO_DENEGADO` con `detail` que nombra
  `Proyectos.ConfigurarTablero`, y sin `X-Usuario-Id` recibe 401 `USUARIO_NO_IDENTIFICADO`;
  la configuración no cambia. (PUT configuración, T2)
- **CA-35** Dado un proyecto archivado, cuando el admin llama al PUT de configuración, entonces
  recibe 409 `PROYECTO_ARCHIVADO`; el GET de configuración responde 200. (PUT/GET configuración)
- **CA-36** Dado un proyecto sin configuración, cuando se consulta el GET de configuración,
  entonces responde `personalizada = false`, los tres `mostrar*` en `true` y
  `fechaModificacion = null`. (GET configuración)

Configuración de categorías
- **CA-37** Dada la instalación con S004, cuando se consulta `GET /api/estados-incidencia`,
  entonces existe "Bloqueada" (`BLOQUEADA`), activa, incluida en el "Flujo general" como no
  inicial ni final e inmediatamente antes del estado final (orden del flujo: 1 Por hacer,
  2 En progreso, 3 Bloqueada, 4 Hecho; DA-20); si "Bloqueada" estaba después de "Hecho", al
  reejecutar S004 queda antes; si un administrador ya la ubicó antes del final, su orden no
  cambia; **no** existe un estado "En espera"; y "Por hacer"/"En progreso"/"Hecho"
  tienen categoría `null`. Reejecutar S004 no duplica nada. (GET estados, C3)
- **CA-38** Dado un admin, cuando asigna la categoría `EN_ESPERA` a un estado nuevo "Esperando
  cliente" (no inicial ni final), entonces las incidencias en ese estado pasan a contar en
  `enEspera` sin modificarse. Sin permiso → 403. (POST/PUT estados, C3, GET seguimiento)
- **CA-39** Cuando se asigna categoría a "Hecho" (final en el flujo general) o a "Por hacer"
  (inicial), o se marca como inicial o como final "Bloqueada" en un flujo, entonces se recibe
  409 `ESTADO_CATEGORIA_INCOMPATIBLE` y nada cambia; C3 y C4 deshabilitan esas opciones.
  (PUT estados, POST/PUT flujos, C3, C4)
- **CA-40** Cuando se consulta cualquier incidencia (listado, árbol, detalle, tablero),
  entonces `estado.categoria` viene informado y el badge muestra el ícono de la categoría.
  (GET incidencias, GET tablero, I1–I3, T2)

Estimación (cambio sobre RF-002)
- **CA-41** Cuando se crea una incidencia con estimación 2,5 en I4, entonces el POST envía
  `"estimacion": 2.5`, se guarda, I3 muestra "Estimación: 2,5 h", I1 y el tablero muestran
  "2,5 h"; creada sin estimación, I3 muestra "Sin estimar" y I1/tablero "—"; con 0 se muestra
  "0 h". (POST/GET incidencias, I1, I3, I4, T2)
- **CA-42** Cuando se envía `estimacion` -1, 1.234 o 10000000 en alta o edición, entonces se
  recibe 400 `VALIDACION` con clave `estimacion` y el mensaje correspondiente; el formulario
  I4/I5 muestra esos mensajes y no envía el request. (POST/PUT incidencias, I4, I5)
- **CA-43** Dada una incidencia con estimación 8, cuando en I5 se vacía el campo y se guarda,
  entonces el PUT envía `"estimacion": null` y la incidencia queda sin estimar. (PUT incidencias, I5)
- **CA-44** Cuando se ordena el listado por `estimacion` asc y desc, entonces las incidencias
  sin estimación quedan al final en ambos casos. (GET incidencias, I1)

Fecha de referencia del servidor (cambio sobre RF-002)
- **CA-45** Dada la zona configurada `America/Argentina/Buenos_Aires`, el servidor a las
  02:00 UTC del 11/10/2026 (23:00 del 10/10 en Argentina) y una incidencia abierta que vence
  el 10/10/2026, cuando un navegador con fecha 11/10 abre I1, I2, I3 y T2, entonces la
  incidencia **no** se marca vencida (`vencida = false`) en ninguna pantalla. (GET incidencias,
  arbol, detalle, seguimiento; I1, I2, I3, T2)

Navegación y permisos
- **CA-46** Dado el menú, cuando se elige "Seguimiento", entonces se abre T1; desde P3 el botón
  "Seguimiento" abre T2 del proyecto y la acción "Seguimiento" de una agrupación abre T2 con
  esa agrupación; el Nro en el tablero abre I3. (Shell, P3, T1, T2)
- **CA-47** Sin usuario seleccionado, cuando se consultan los GET de seguimiento, tablero y
  configuración del tablero, entonces responden 200. (GET seguimiento/*)

Filtros del tablero y vistas de T2 (DA-16)
- **CA-48** Dado el Tablero de `PRJ-0001` con `indicador=bloqueadas` (2 filas: #15 "Pagar con
  tarjeta" y #20 "Envío express") ordenado por Título, cuando se escribe "pago" en Texto,
  entonces el servidor devuelve solo #15 (AND entre filtros), `filtros.texto = "pago"`, el
  orden vuelve a Tipo + Nro y la URL contiene `indicador=bloqueadas&texto=pago`; al recargar
  se conservan los filtros; "Limpiar filtros" quita indicador, estado y texto pero conserva
  ámbito y alcance; con `indicador=vencidas&estadoId=<Hecho>` se ve "No hay incidencias que
  coincidan con los filtros." (GET tablero, T2)
- **CA-49** Cuando se abre T2, entonces las pestañas son solo Resumen, Tablero y Jerarquía (no
  existe "Lista"); la URL `/proyectos/1/seguimiento?vista=lista` abre la vista Resumen; con
  filtros de tablero en la URL, el Resumen y la Jerarquía muestran los mismos datos que sin
  ellos; y en ninguna vista se muestra un total de horas estimadas. (T2; RN-07, RN-20, RN-35)
- **CA-50** Dado T1 con totales de indicadores, cuando el usuario hace clic sobre cualquier
  tarjeta (Total, Abiertas, Vencidas, etc.), entonces no ocurre navegación ni cambian los
  filtros o la lista de proyectos, y las tarjetas no tienen cursor de enlace ni son
  enfocables como acción; en cambio, en la Vista Resumen de T2 el clic en una tarjeta abre el
  Tablero filtrado por ese indicador (CA-24). (T1, T2; DA-21)

---

## Casos de prueba unitarios sugeridos

Convención de RF-001 (`Metodo_Escenario_ResultadoEsperado`, AAA, NSubstitute,
`FakeTimeProvider`, trazabilidad RN/CA).

### Backend – Domain / Application (funciones puras)

`TimeProviderExtensions.HoyEn` / `FechaReferencia`
| Caso | Resultado |
|---|---|
| UTC 2026-10-10T02:00Z, zona UTC-3 | `2026-10-09` (RN-10) |
| UTC 2026-10-10T03:00Z, zona UTC-3 | `2026-10-10` (límite) |
| Zona nula | `ArgumentNullException` |
| `OpcionesFechaReferencia` sin zona | Usa `TimeProvider.LocalTimeZone` |
| `OpcionesFechaReferencia` con id inválido | Falla la validación de opciones |

`ReglasVencimiento.EstaVencida`
| Caso | Resultado |
|---|---|
| Fecha nula | false (RN-11) |
| Fecha igual a la de referencia, no final | false |
| Fecha anterior, no final | true |
| Fecha anterior, final | false |

`ReglasCategoriaEstado` (RN-18)
| Caso | Resultado |
|---|---|
| Estado con categoría, final en un flujo | Incompatible |
| Estado con categoría, inicial en un flujo | Incompatible |
| Estado con categoría, ni inicial ni final | Compatible |
| Estado sin categoría, inicial o final | Compatible |

`ResolutorAlcanceSeguimiento` (niveles y tipos del catálogo + ids pedidos → alcance o errores)
| Caso | Resultado |
|---|---|
| Sin `nivelId` ni `tipoId` | Todos los niveles y tipos; `todosLosNiveles`/`todosLosTipos` = true (RN-06) |
| `nivelId=3` | Tipos del nivel 3 (incluye inactivos); `todosLosTipos` = true (RN-04) |
| `nivelId=3`, `tipoId=Subtarea,Bug` | Solo esos dos; `todosLosTipos` = false (RN-04) |
| `tipoId` sin `nivelId` | Esos tipos; niveles = todos (RN-04) |
| `nivelId=1`, `tipoId=Subtarea` | Error `tipoId` intersección vacía (RN-05) |
| `nivelId=99` inexistente / `tipoId=99` inexistente | Error en la clave correspondiente (RN-05) |
| Ids repetidos | Se deduplican |
| Orden de salida | Niveles por número; tipos por nivel y nombre |

`CalculadoraIndicadores` (filas crudas + estados → `IndicadoresDto`)
| Caso | Resultado |
|---|---|
| Sin filas | Todo 0; `porEstado` con los estados en 0 (RN-15, CA-02) |
| Fila no final con `ConVencimientoAnterior=2` | `vencidas` += 2 (RN-11) |
| Fila final con `ConVencimientoAnterior=1` | No suma en `vencidas` (RN-11) |
| Fila no final, estado `BLOQUEADA`, cantidad 2 | `bloqueadas` = 2 (RN-12) |
| Fila no final, estado `EN_ESPERA` | `enEspera` (RN-13) |
| Ningún estado `EN_ESPERA` | `enEspera` = 0 (RN-13, CA-12) |
| Fila final con `SinResponsable=1` | No suma (RN-14) |
| Misma fila suma vencida + bloqueada + sin responsable | Suma en los tres (RN-08, CA-09) |
| Mismo estado final en un flujo y no final en otro (dos filas) | Cantidades correctas en abiertas/finalizadas; `esFinal` del estado = false (RN-15) |
| Estado inactivo con cantidad > 0 que no está en los flujos | Se agrega a `porEstado` (RN-15, CA-11) |
| Orden de `porEstado` | `Orden`, `Nombre`, `Id` (RN-15) |
| Invariante | `total` = Σ`porEstado` = `abiertas` + `finalizadas` (RN-16) |
| Sumar indicadores (`Sumar(a, b)`) | Suma campo a campo y por estado (RN-26, RN-27) |

`OrdenadorProyectosSeguimiento` / paginación en memoria
| Caso | Resultado |
|---|---|
| `ordenarPor=vencidas desc` con empate | Desempate por código desc, luego id (CA-04) |
| Página 2 de 25 con tamaño 20 | 5 ítems, `total` 25 (CA-03) |
| Página fuera de rango | `items` vacío, `total` correcto |

`OrdenTableroPorDefecto` (Application, orden de `items`)
| Caso | Resultado |
|---|---|
| Épica #3, HU #15, Subtarea #16, HU #7 | 3, 7, 15, 16 (RN-32, CA-29) |
| Dos tipos del mismo nivel | Por nombre del tipo, luego número |

Validadores
| Caso | Resultado |
|---|---|
| `ConsultaTableroProyectosQueryValidator`: `ordenarPor=foo`, `direccion=x`, `archivados=x`, `pagina=0`, `tamanioPagina=0/101`, texto 151 | Error en el campo (CA-05); `tamanioPagina=100` válido |
| `ConsultaSeguimientoQueryValidator` y `ConsultaTableroIncidenciasQueryValidator`: `agrupacionId` + `sinAgrupacion` | Error (RN-01) |
| `ConsultaTableroIncidenciasQueryValidator`: `indicador=foo`; `estadoId=0`; `texto` de 201 caracteres | Error en `indicador` / `estadoId` / `texto` (RN-21, RN-28, CA-26) |
| `ConsultaTableroIncidenciasQueryValidator`: `indicador` = cada valor de la lista (también en mayúsculas), `texto` de 200, `indicador` + `estadoId` + `texto` juntos | Válidos (RN-21) |
| `ListarIncidenciasQueryValidator` (modificado): `ordenarPor=estimacion` | Válido; el resto de reglas idénticas a RF-002 (regresión; no existen `nivelId` ni `indicador`) |
| `EstadoIncidenciaRequestValidator` (modificado): `categoria="OTRA"` | Error `categoria`; null / `BLOQUEADA` / `EN_ESPERA` válidos (RN-17) |
| `ConfiguracionTableroRequestValidator`: cada `mostrar*` null | Error en la clave (RN-31, CA-33); los tres informados válido |
| `CrearIncidenciaRequestValidator` / `EditarIncidenciaRequestValidator` (modificados): `estimacion` -0.01, 1.234, 10000000 | Error `estimacion` con el mensaje de cada caso (RN-34, CA-42) |
| Ídem: `estimacion` null, 0, 0.25, 2.5, 9999999.99 | Válidos (RN-34) |

### Backend – Application (servicios con repos mockeados)

`SeguimientoService.ObtenerTableroProyectosAsync`
| Caso | Resultado |
|---|---|
| Defaults | Repo de proyectos con `archivados=excluir`; orden `codigo desc`; página 1/20 (RN-02) |
| Fecha de referencia | Se pasa al repo de conteos `IFechaReferencia.Hoy()` y se devuelve en `fechaReferencia` (RN-10) |
| Proyecto sin filas de conteo | Indicadores en 0 (CA-02) |
| `totales` | Suma de todos los proyectos, no solo de la página (RN-27) |
| Alcance inválido | `ValidacionException` / 400 sin consultar conteos (RN-05) |

`SeguimientoService.ObtenerSeguimientoAsync`
| Caso | Resultado |
|---|---|
| Proyecto inexistente | `PROYECTO_NO_ENCONTRADO` (CA-23) |
| `agrupacionId` de otro proyecto | `AGRUPACION_NO_ENCONTRADA` (RN-01, CA-21) |
| Ámbito proyecto | Pide conteos agrupados por agrupación; `agrupaciones` = todas las del proyecto por nombre + fila "sin agrupación" al final; indicadores del proyecto = suma (RN-26) |
| Ámbito agrupación | Filtro con `AgrupacionId`; `agrupaciones` vacío; `agrupacion` con descripción (CA-20) |
| Ámbito sin agrupación | Filtro `AgrupacionId IS NULL`; `ambito = sinAgrupacion` |
| Proyecto archivado | Devuelve datos (RN-03) |
| Alcance | Pasa al repo solo los tipos efectivos (RN-04) |

`SeguimientoService.ObtenerTableroAsync`
| Caso | Resultado |
|---|---|
| Proyecto inexistente / agrupación de otro proyecto | `PROYECTO_NO_ENCONTRADO` / `AGRUPACION_NO_ENCONTRADA` (CA-21, CA-23) |
| Alcance | Pasa al repo solo los tipos efectivos y el ámbito (RN-07) |
| Filtros | Pasa al repo `indicador` (normalizado a minúsculas), `estadoId`, `texto` recortado y `IFechaReferencia.Hoy()` (RN-21, 0.1) |
| `texto` solo espacios | Se trata como sin filtro; `filtros.texto = null` |
| `estadoId` inexistente en el catálogo | `ValidacionException` clave `estadoId`, sin consultar incidencias (RN-21, CA-26) |
| `estadoId` existente | `filtros.estado` con id, nombre, activo y categoría |
| Sin filtros | `filtros` con los tres en `null` |
| `items` | En orden por defecto (RN-32); `total` = cantidad de ítems (RN-20); `vencida` calculado con `ReglasVencimiento` y la fecha de referencia (RN-36) |
| Repo de configuración devuelve null | `configuracion` por defecto: `personalizada=false`, todo visible (RN-30) |
| Repo de configuración devuelve fila | `configuracion` con esos valores |
| Proyecto archivado | Devuelve datos |

`ConfiguracionTableroService`
| Caso | Resultado |
|---|---|
| `ObtenerAsync` sin fila | Defaults (RN-30, CA-36) |
| `ObtenerAsync` proyecto inexistente | `PROYECTO_NO_ENCONTRADO` |
| `GuardarAsync` anónimo | `UsuarioNoIdentificadoException`, no llama al repo (RN-31) |
| `GuardarAsync` sin permiso | `PermisoDenegadoException`, no llama al repo (RN-31, CA-34) |
| `GuardarAsync` proyecto archivado | `PROYECTO_ARCHIVADO`, no llama al upsert (RN-31, CA-35) |
| `GuardarAsync` válido | Upsert con los tres valores y `FechaModificacion` = reloj; devuelve `personalizada=true` (CA-32) |
| `GuardarAsync` proyecto inexistente | `PROYECTO_NO_ENCONTRADO` |

`IncidenciaConsultaService` (modificado)
| Caso | Resultado |
|---|---|
| `ListarAsync` | Filtro idéntico al de RF-002 (regresión; sin `nivelId` ni `indicador`) |
| `ListarAsync` con `ordenarPor=estimacion` | Pasa el orden al repo (CA-44) |
| `ListarAsync` / `ObtenerArbolAsync` / `ObtenerAsync` | `vencida` calculado con `IFechaReferencia.Hoy()` (no con `DateTime.Now`); en detalle también en `padre` e `hijas` (RN-36, CA-45) |
| `ObtenerAsync` | `estimacion` mapeada (null y decimal) |
| `ObtenerArbolAsync` con `agrupacionId` + `sinAgrupacion` | Error de validación; sin parámetros → mismo llamado que RF-002 |

`IncidenciaService` (modificado)
| Caso | Resultado |
|---|---|
| Crear con `estimacion = 2.5` | Se persiste 2.5 (CA-41) |
| Crear sin `estimacion` | Se persiste null |
| Editar con `estimacion = null` teniendo 8 | Se persiste null (RN-34, CA-43) |
| Editar padre con estimación menor que la suma de hijas | OK, sin validación jerárquica (RN-35) |

`EstadoIncidenciaService` (modificado)
| Caso | Resultado |
|---|---|
| Crear con `categoria=EN_ESPERA` | Guarda la categoría (RN-17) |
| Editar asignando categoría a estado final en algún flujo | `ESTADO_CATEGORIA_INCOMPATIBLE`, sin actualizar (RN-18) |
| Editar asignando categoría a estado inicial en algún flujo | `ESTADO_CATEGORIA_INCOMPATIBLE`, sin actualizar (RN-18) |
| Editar quitando la categoría | OK |
| Editar cambiando categoría con incidencias | OK, no actualiza incidencias (RN-19) |
| Sin permiso | `PermisoDenegadoException` (RF-002 RN-01) |

`FlujoIncidenciaService` (modificado): marcar final un estado con categoría →
`ESTADO_CATEGORIA_INCOMPATIBLE`; marcar inicial un estado con categoría →
`ESTADO_CATEGORIA_INCOMPATIBLE` (RN-18); estado con categoría ni inicial ni final → OK.

`ResolutorPermisosPorAdministrador` (modificado): admin → incluye
`Proyectos.ConfigurarTablero`; no admin → vacío.

### Backend – Api

| Caso | Resultado |
|---|---|
| GET `/api/seguimiento/proyectos?nivelId=1&nivelId=2&tipoId=3` | El binding entrega listas `[1,2]` y `[3]` al servicio |
| GET seguimiento / tablero / configuración sin `X-Usuario-Id` | 200 (RN-25, CA-47); sin `[Authorize]` (reflexión) |
| PUT configuración: atributo `[Authorize(Policy = "Proyectos.ConfigurarTablero")]` | Presente (reflexión) |
| PUT configuración sin header | 401 `USUARIO_NO_IDENTIFICADO` (CA-34) |
| PUT configuración con persona no admin | 403 `PERMISO_DENEGADO`, `detail` nombra el permiso (CA-34) |
| PUT configuración, servicio lanza `PROYECTO_ARCHIVADO` | 409 con `codigo` (CA-35) |
| GET seguimiento, servicio lanza `AGRUPACION_NO_ENCONTRADA` | 404 ProblemDetails con `codigo` |
| GET tablero con `indicador=en-espera&estadoId=4&texto=pago` | Llegan los tres filtros al servicio |
| GET tablero, servicio lanza `ValidacionException` (clave `estadoId`) | 400 `VALIDACION` con `errors.estadoId` (CA-26) |
| PUT estado con `ConflictoException ESTADO_CATEGORIA_INCOMPATIBLE` | 409 con `codigo` |
| POST incidencia con `"estimacion": 2.5` | Llega `2.5m` al servicio |
| Serialización | `fechaReferencia` como `yyyy-MM-dd`; `categoria` y `estimacion` null serializados como `null`; `estimacion` 12.5 como número `12.5`; `vencida` booleano |

Integración contra LocalDB (recomendada; consultar al orquestador):
- Conteos de `ContarAsync` con incidencias de tipos con flujos distintos (EsFinal por flujo).
- CA-25 / RN-20: para cada `indicador` y cada `estadoId`, `total` del tablero = valor del
  resumen; sin filtros, `total` del tablero = `indicadores.total` (incluye incidencias de tipos
  con flujos distintos y un caso de vencimiento igual a la fecha de referencia).
- Filtro `texto` del tablero: coincidencia por código y por título sin distinguir mayúsculas.
- `CK_EstadosIncidencia_Categoria` y `CK_Incidencias_Estimacion` rechazan valores inválidos;
  S004 idempotente.
- Upsert de `ConfiguracionesTableroProyecto` (insert la primera vez, update después).
- Orden del listado por `estimacion` con nulos al final.

### Frontend (Angular)

Servicios (`HttpTestingController`)
| Caso | Resultado |
|---|---|
| `SeguimientoService.tableroProyectos({nivelIds:[1,2], ordenarPor:'vencidas'})` | GET `/api/seguimiento/proyectos?nivelId=1&nivelId=2&ordenarPor=vencidas` |
| `SeguimientoService.seguimiento(1, {sinAgrupacion:true})` | GET `/api/proyectos/1/seguimiento?sinAgrupacion=true` |
| `SeguimientoService.tablero(1, {agrupacionId:8, nivelIds:[3]})` | GET `/api/proyectos/1/seguimiento/tablero?agrupacionId=8&nivelId=3` |
| `SeguimientoService.tablero(1, {indicador:'vencidas', estadoId:4, texto:'pago'})` | GET `.../seguimiento/tablero?indicador=vencidas&estadoId=4&texto=pago`; filtros vacíos o `null` no se envían |
| `SeguimientoService.obtenerConfiguracionTablero(1)` | GET `/api/proyectos/1/seguimiento/tablero/configuracion` |
| `SeguimientoService.guardarConfiguracionTablero(1, {...})` | PUT misma URL con los tres booleanos |
| `IncidenciasService.listar(1, {ordenarPor:'estimacion'})` | Agrega `ordenarPor=estimacion`; sin otros parámetros nuevos respecto de RF-002 |
| `IncidenciasService.arbol(1, {agrupacionId:8})` | GET `.../arbol?agrupacionId=8`; sin filtros, URL de RF-002 |
| `IncidenciasService.crear/editar` | Body con `estimacion` (número o `null`) |
| `EstadosIncidenciaService.crear/editar` | Body con `categoria` |

Utilidades puras
| Caso | Resultado |
|---|---|
| `textoLeyendaAlcance(alcance)` todos | "Se contabilizan: todos los niveles y tipos de incidencia." (CA-14) |
| `textoLeyendaAlcance` nivel 3 con Subtarea y Bug | Nombra nivel y tipos (CA-15) |
| `tiposDisponibles(tipos, nivelIdsSeleccionados)` | Solo tipos de esos niveles; sin selección, todos |
| `depurarTiposSeleccionados` al quitar un nivel | Quita los tipos de ese nivel (CA-16) |
| `idsExpandidosHastaNivel(arbol, 2)` | Ids de nodos con `nivelNumero < 2` que tienen hijos (RN-22, CA-27) |
| `idsExpandidosHastaNivel(arbol, 1)` | Conjunto vacío |
| `filtrosSeguimientoDesdeQueryParams` / `aQueryParams` | Ida y vuelta sin pérdida (`nivelId` múltiples, `vista`, ámbito, `indicador`, `estadoId`, `texto`); no incluye orden del tablero |
| `filtrosSeguimientoDesdeQueryParams` con `vista=lista` o `vista=foo` | `vista = 'resumen'` (CA-49) |
| `navegacionDesdeResumen(filtros, {indicador:'vencidas'})` | `vista=tablero`, `indicador=vencidas`, sin `estadoId` ni `texto`; ámbito y alcance conservados (RN-24, CA-24) |
| `navegacionDesdeResumen(filtros, {estadoId:2})` | `vista=tablero`, `estadoId=2`, sin `indicador` ni `texto` (RN-24) |
| `navegacionDesdeResumen(filtros, {total:true})` | `vista=tablero` sin filtros del tablero (RN-24) |
| `ordenarTablero(items, null)` | Orden por defecto Tipo (nivel, nombre) + Nro (RN-32) |
| `ordenarTablero(items, {columna:'estimacion', direccion:'asc'/'desc'})` | Numérico; null al final en ambas direcciones (RN-33, CA-30) |
| `ordenarTablero` por `estado` | Por `estado.orden`, luego nombre (CA-31) |
| `ordenarTablero` por `responsable` | Por nombre; sin responsable al final (CA-31) |
| `ordenarTablero` por `titulo` | Sin distinguir mayúsculas ni acentos; empate por Tipo + Nro (CA-31) |
| `ordenarTablero` no muta el arreglo de entrada | Devuelve uno nuevo |
| `alternarOrden(actual, columna)` | Misma columna: asc → desc → asc; otra columna: asc |
| `columnasVisibles(configuracion)` | Siempre `tipo`, `numero`, `titulo` + las configurables visibles, en el orden de RN-29 |
| `formatearEstimacion` | 12.5 → "12,5 h"; 0 → "0 h"; 8 → "8 h"; null → "—" |
| Validador `maxDecimales(2)` | 1.23 válido; 1.234 error; vacío válido |

Componentes
| Caso | Resultado |
|---|---|
| `IndicadoresComponent` | Muestra 6 tarjetas; clic emite el indicador (`total` para la tarjeta Total) (CA-24); alerta si vencidas > 0 |
| `IndicadoresComponent` con `clicable = false` | Clic en cualquier tarjeta no emite `indicadorSeleccionado`; las tarjetas no tienen rol de botón ni `tabindex` (CA-50) |
| `IndicadoresComponent` sin input `clicable` | Default `true`: las tarjetas son botones y emiten al hacer clic (CA-24) |
| `TableroGeneralPage` | Pasa `clicable = false` a `IndicadoresComponent`; clic en una tarjeta no navega ni cambia query params (CA-50) |
| `LeyendaAlcanceComponent` sin estados de categoría EN_ESPERA | Muestra el aviso (CA-12) |
| `FiltroAlcanceComponent` | Al elegir nivel limita tipos; no permite intersección vacía (CA-16) |
| `ConteoPorEstadoComponent` | Filas en el orden recibido; clic emite `estadoId` |
| `TablaProyectosSeguimientoComponent` | Encabezados ordenables emiten `ordenarPor`; expandir pide el seguimiento del proyecto una sola vez (CA-06) |
| `TablaTableroComponent` todo visible | 6 columnas, sin agrupación por estado (CA-29) |
| `TablaTableroComponent` con `mostrarEstimacion=false` | No renderiza la columna Estimación; Tipo/Nro/Título siempre (CA-32) |
| `TablaTableroComponent` clic en encabezado | Reordena, actualiza `aria-sort`; "Orden predeterminado" restablece y queda deshabilitado (CA-30) |
| `TablaTableroComponent` Nro | Enlace a I3 con `title` = código (CA-46); ícono de vencida solo si `vencida = true` (RN-36) |
| `FiltrosTableroComponent` | Emite texto con debounce 300 ms; selects de estado e indicador emiten el valor; chips por filtro activo con botón quitar; "Limpiar filtros" emite los tres vacíos y está deshabilitado sin filtros (CA-48) |
| `ConfigurarColumnasDialogComponent` | Obligatorias marcadas y deshabilitadas; "Restablecer" marca las tres; Guardar emite los tres booleanos (CA-33) |
| `SeguimientoProyectoPage` vista tablero | Botón "Columnas" solo con permiso y proyecto no archivado (CA-22, CA-34); tras guardar aplica la configuración; si la columna ordenada quedó oculta vuelve al orden por defecto |
| `SeguimientoProyectoPage` | Cambiar ámbito/alcance/vista/filtros actualiza query params y resetea el orden del tablero; proyecto archivado → banner sin "Editar agrupación" (CA-22); 404 → mensaje (CA-23) |
| `SeguimientoProyectoPage` pestañas | Solo Resumen, Tablero y Jerarquía (CA-49) |
| `SeguimientoProyectoPage` clic en tarjeta "Vencidas" / en estado del conteo | Navega a `vista=tablero` con `indicador=vencidas` / `estadoId` (CA-24) |
| `SeguimientoProyectoPage` vista tablero con filtros en la URL | Llama al GET tablero con esos filtros; vacío con filtros muestra "No hay incidencias que coincidan con los filtros." (CA-48) |
| `TableroGeneralPage` | Filtro Archivados por defecto "No archivados"; cambios vuelven a página 1 (CA-01) |
| `ArbolIncidenciasComponent` (modificado) | Botones por nivel (CA-27); nodos fuera de alcance con clase atenuada (CA-18); alerta según `vencida` del nodo (CA-45) |
| `IncidenciasTablaComponent` (modificado) | Columna Estimación (CA-41); alerta según `vencida` y no según la fecha del navegador (CA-45) |
| `IncidenciaDatosComponent` / `IncidenciaHijasComponent` (modificados) | "Estimación: 2,5 h" / "Sin estimar" (CA-41); alerta según `vencida` |
| `IncidenciaFormComponent` (modificado) | Errores de estimación negativa, 3 decimales y máximo (CA-42); vacío envía `null` (CA-43); en edición precarga el valor |
| `ProyectoDetallePage` (modificado) | Botón "Seguimiento" y acción por agrupación con las rutas correctas (CA-46) |
| `EstadoIncidenciaFormComponent` (modificado) | Select categoría deshabilitado si el estado es inicial o final en algún flujo (CA-39) |
| `FlujoFormPage` (modificado) | Radio "Inicial" y check "Final" deshabilitados en estados con categoría (CA-39) |
| `BadgeEstadoIncidenciaComponent` | Ícono según categoría (CA-40) |

---

## Impacto por agente

- **BD (`dba-sql`)**:
  - `V010`: `ALTER TABLE dbo.EstadosIncidencia ADD Categoria NVARCHAR(20) NULL` +
    `CK_EstadosIncidencia_Categoria` (vía `EXEC` como en `V006`), e índice
    `IX_Incidencias_ProyectoId_EstadoId` con `INCLUDE`. Idempotente y transaccional.
  - `V011`: `ALTER TABLE dbo.Incidencias ADD Estimacion DECIMAL(9,2) NULL` +
    `CK_Incidencias_Estimacion` (`Estimacion IS NULL OR Estimacion >= 0`). Idempotente.
  - `V012`: tabla `ConfiguracionesTableroProyecto` (PK = `ProyectoId`, FK a `Proyectos`, tres
    `BIT NOT NULL`, `FechaModificacion DATETIME2(0) NOT NULL`). Idempotente.
  - `S004` idempotente: estado "Bloqueada" con categoría `BLOQUEADA` y su inclusión en el
    "Flujo general" al final (no inicial, no final; sin modificar filas existentes). **No**
    crea "En espera". UTF-8.
  - Actualizar `db/README.md` (CHECK de dominio, `INCLUDE`, upsert sin `MERGE`).
  - v1.2 no cambia el modelo de datos ni los scripts propuestos (DA-16 a DA-19 solo afectan
    contrato, reglas y pantallas).
- **Backend (`dev-backend`)**:
  - Domain: `EstadoIncidencia.Categoria` + constantes `CategoriasEstadoIncidencia`
    (`BLOQUEADA`, `EN_ESPERA`); regla pura RN-18 (inicial y final); `ReglasVencimiento`;
    `Incidencia.Estimacion` (`decimal?`); `CodigosError.EstadoCategoriaIncompatible`.
  - Application: `TimeProviderExtensions.HoyEn`; `IFechaReferencia` + implementación con
    `OpcionesFechaReferencia` (`ZonaHoraria`, `ValidateOnStart`); `Permisos.ProyectosConfigurarTablero`
    (en `Permisos.Todos`); carpeta `Seguimiento/` con DTOs del contrato, queries + validadores,
    `ResolutorAlcanceSeguimiento`, `CalculadoraIndicadores`, ordenamiento/paginación en memoria,
    orden por defecto del tablero, `ISeguimientoService`/`SeguimientoService`,
    `IConfiguracionTableroService`/`ConfiguracionTableroService`; interfaces
    `ISeguimientoRepository` (`ListarProyectosAsync`, `ContarAsync(filtro, agruparPor)`,
    `ListarEstadosDeFlujosAsync(tipoIds)`, `ListarIncidenciasTableroAsync(filtro)` con
    `FiltroTableroIncidencias` = ámbito + tipos efectivos + `Indicador`, `EstadoId`, `Texto`,
    `FechaReferencia`) e
    `IConfiguracionTableroRepository` (`ObtenerAsync`, `GuardarAsync` upsert).
  - **Cambios en código de RF-002**: `EstadoIncidenciaReferenciaDto` + `Categoria`;
    `IncidenciaResumenDto`, `NodoArbolIncidenciaDto`, `IncidenciaDto`, `IncidenciaReferenciaDto`
    + `Vencida` (calculado con `IFechaReferencia`); `IncidenciaResumenDto` e `IncidenciaDto` +
    `Estimacion`; `CrearIncidenciaRequest`/`EditarIncidenciaRequest` + `Estimacion` con sus
    validadores; `IncidenciaService` y `IncidenciaRepository` (INSERT/UPDATE/SELECT de
    `Estimacion`); `ListarIncidenciasQuery`/validador + orden `estimacion` (**sin** `NivelId` ni
    `Indicador`: se quitaron en v1.2); árbol con `agrupacionId`/`sinAgrupacion`;
    `EstadoIncidenciaRequest`/`Dto`/validador/servicio/repositorio + `Categoria`;
    `FlujoIncidenciaService` (RN-18 inicial y final).
  - Infrastructure.SqlServer: `SeguimientoRepository` + `ConsultasSeguimiento` (agregados con
    `GROUP BY`, `LEFT JOIN FlujosIncidenciaEstados` por el flujo del tipo como en RF-002;
    consulta de la grilla con filtros `estadoId`, `texto` (`LIKE` sobre código/título como
    RF-002) e `indicador` vía `CondicionIndicador`),
    `ConfiguracionTableroRepository` (UPDATE + INSERT), registro en DI.
  - Api: `SeguimientoController` (`api/seguimiento/proyectos`) y
    `SeguimientoProyectoController` (`api/proyectos/{proyectoId:int}/seguimiento`, acciones
    `tablero` con query `indicador`/`estadoId`/`texto`, `tablero/configuracion` GET y PUT con
    política); política
    `Proyectos.ConfigurarTablero` en `SeguridadExtensions`; binding de listas repetidas;
    configuración `FechaReferencia:ZonaHoraria` en `appsettings`.
- **Frontend (`dev-frontend`)**:
  - Feature `seguimiento` (T1, T2) con rutas lazy, modelos, `SeguimientoService`, componentes
    (incluidos `TablaTableroComponent`, `FiltrosTableroComponent` y
    `ConfigurarColumnasDialogComponent`) y utilidades de esta spec (`ordenarTablero`,
    `alternarOrden`, `columnasVisibles`, `navegacionDesdeResumen`); T2 con tres vistas
    (Resumen, Tablero, Jerarquía; **sin vista Lista**); ítem de menú "Seguimiento". Constante
    de permiso `Proyectos.ConfigurarTablero`.
  - **Cambios en RF-002**: `IncidenciasService.listar` (solo orden `estimacion`) y `arbol`
    (ámbito); modelos con `estimacion`, `vencida`,
    `EstadoIncidenciaReferencia.categoria`; `IncidenciasTablaComponent` (columna Estimación,
    alerta por `vencida`, sin input `hoy`); `ArbolIncidenciasComponent` (expandir hasta nivel,
    atenuado, marcas, alerta por `vencida`, sin input `hoy`) + util `idsExpandidosHastaNivel`;
    I3 (`IncidenciaDatosComponent`, `IncidenciaHijasComponent`); `IncidenciaFormComponent`
    (campo y validadores de estimación); util compartida `formatearEstimacion`;
    `BadgeEstadoIncidenciaComponent`; C3 (columna y select categoría), C4 (radio inicial y
    check final).
  - **Cambios en RF-001**: `ProyectoDetallePage` / `AgrupacionesListaComponent` (botón y acción
    "Seguimiento"); `app.routes.ts` (rutas antes de `proyectos`); `ShellComponent`.
- **Tests (`dev-tests`)**:
  - Casos de la sección anterior con trazabilidad RN/CA; regresión de RF-002 en listado, árbol,
    detalle, alta/edición, estados y flujos (DTOs con `categoria`, `vencida`, `estimacion`).
  - Ajustar tests de RF-002 que inyectaban `hoy` en `IncidenciasTablaComponent` /
    `ArbolIncidenciasComponent` para que usen `vencida` del modelo.
  - Recomendado: integración contra LocalDB para CA-25 y RN-20 (coherencia entre el tablero
    filtrado por `indicador`/`estadoId` y los indicadores del resumen), conteos con flujos
    distintos y upsert de configuración.
  - Ya no aplican los tests del listado con `indicador`/`nivelId` de v1.1 (quitados, DA-16).

---

## Decisiones tomadas

Respuestas del usuario a las decisiones de v1.0 (2026-10-10).

- **DA-01 Representación de "bloqueada" y "en espera"** → **Aprobada la propuesta**: categoría
  del estado (`EstadosIncidencia.Categoria`, valores `BLOQUEADA` / `EN_ESPERA`). Una
  incidencia está bloqueada/en espera cuando está abierta y su estado tiene esa categoría.
  (RN-12, RN-13, RN-17)
- **DA-02 Datos semilla** → El seed crea **solo** el estado "Bloqueada" (femenino, categoría
  `BLOQUEADA`) y lo agrega al "Flujo general". **No** se crea "En espera": la categoría
  `EN_ESPERA` queda disponible para que un administrador la asigne desde C3; el indicador "En
  espera" muestra 0 mientras ningún estado la tenga. (S004, RN-13, CA-12, CA-37)
- **DA-03 Zona horaria de "hoy"** → "Hoy" se calcula con la zona horaria del servidor,
  configurable (`FechaReferencia:ZonaHoraria`). **Se unifica**: I1, I2 e I3 de RF-002 dejan
  de usar la fecha del navegador; la API devuelve `vencida` calculado en el servidor en todos
  los DTO de incidencia (cambio C-RF002-02). (0.1, RN-10, RN-36, CA-45)
- **DA-04 Sin responsable** → **Aprobada la propuesta**: solo abiertas; un responsable inactivo
  cuenta como asignado. (RN-14)
- **DA-05 Alcance por defecto** → **Aprobada la propuesta**: todos los niveles y tipos, con
  leyenda y nota "las épicas no suman a sus hijas"; el alcance vive en la URL y no se recuerda
  entre sesiones. (RN-06)
- **DA-06 Estado con categoría** → Un estado con categoría no puede ser **ni final ni
  inicial** de un flujo; ambos casos devuelven 409 `ESTADO_CATEGORIA_INCOMPATIBLE`. (RN-18,
  CA-39)
- **DA-07 Rollup jerárquico** → **Aprobada la propuesta**: no hay rollup; cada incidencia
  cuenta por sí misma. (RN-08)
- **DA-08 Detalle de agrupación** → **Aprobada la propuesta**: T2 en ámbito agrupación cumple
  ese rol; sin pantalla propia.
- **DA-09 Tablero editable** → **Aprobada la propuesta**: solo lectura, sin drag & drop ni
  edición en línea.
- **DA-10 Permisos de consulta** → **Aprobada la propuesta**: consulta libre, sin usuario. (El
  permiso nuevo de DA-15 es solo para modificar la configuración del tablero.) (RN-25)
- **DA-11 Volumen** → No se esperan volúmenes grandes; cálculo y paginación en memoria son
  aceptables. En consecuencia, la grilla del tablero trae todas las incidencias del ámbito sin
  paginar y ordena en el cliente. (0.2, RN-20, RN-33)
- **DA-12 Vista Tablero** → **Cambio respecto de v1.0, confirmado por el usuario: "el tablero
  no muestra las incidencias agrupadas"**. El tablero es una grilla de incidencias del ámbito
  (con el alcance de niveles/tipos) con columnas de datos: Tipo, Nro, Título, Estado,
  Estimación, Responsable. Tipo, Nro y Título obligatorias; Estado, Estimación y Responsable
  configurables. Orden por defecto Tipo + Nro; ordenamiento por clic en el cliente, no
  persistido. Se eliminan las columnas por estado con tarjetas y el límite de 50 por columna.
  **Interpretación del requisito del RF "tablero agrupado por estado"**: queda cubierto por el
  **conteo por estado** de la vista Resumen (`porEstado`, RN-15), confirmado por el usuario.
  (RN-20, RN-29 a RN-33, CA-29 a CA-36)
- **DA-13 Indicadores adicionales** → **Aprobada la propuesta**: solo los mínimos del RF +
  total/abiertas/finalizadas.
- **DA-14 Estimación (nuevo)** → Campo `Incidencias.Estimacion`, numérico en horas con
  decimales: `DECIMAL(9,2)`, `>= 0`, opcional (`NULL`). Se carga en I4/I5 y se muestra en I3,
  I1 y el tablero. Cambio sobre RF-002 (que tenía "estimaciones" fuera de alcance)
  (C-RF002-01). (RN-34, RN-35, CA-41 a CA-44)
- **DA-15 Configuración de columnas (nuevo)** → Por proyecto, modificable por usuarios con el
  permiso nuevo `Proyectos.ConfigurarTablero` (mecanismo de RF-002 0.1; provisionalmente lo
  tienen los admin). Persistencia mínima propuesta: tabla 1:0..1
  `ConfiguracionesTableroProyecto`; sin fila = todas visibles. Endpoints GET/PUT
  `.../seguimiento/tablero/configuracion`. (RN-29 a RN-31, CA-32 a CA-36)

Respuestas del usuario a las decisiones de v1.1 (2026-10-10).

- **DA-16 Redundancia entre las vistas "Lista" y "Tablero"** → **Opción A – Unificar**. Se
  elimina la vista Lista de T2; el **Tablero** es la única vista de incidencias en grilla del
  seguimiento. Suma los filtros que tenía la Lista: `indicador` y `estadoId` (pedidos
  explícitamente) y `texto` (se mantiene porque sirve para buscar dentro de una grilla sin
  paginar). No se trasladan los filtros de I1 que la Lista no exponía (prioridad, responsable
  / sin responsable — este último queda cubierto por `indicador=sin-responsable`) ni la
  paginación ni las columnas extra de I1 (prioridad, vencimiento, agrupación, padre). Clic en
  un indicador del Resumen (p. ej. "Vencidas: 5") → Tablero filtrado por ese indicador; clic
  en un estado del conteo → Tablero filtrado por ese estado. I1 sigue existiendo fuera del
  seguimiento. Consecuencia sobre RF-002: los parámetros `nivelId` e `indicador` que v1.1
  agregaba al listado I1 ya no son necesarios y **se quitan** (se prefiere no modificar RF-002
  más de lo necesario); en el listado solo quedan el orden `estimacion` y los campos
  `estimacion`/`vencida`. (Contrato `.../seguimiento/tablero`, RN-07, RN-20, RN-21, RN-24,
  RN-28, RN-33, RN-36, T2, CA-24 a CA-26, CA-48, CA-49)
- **DA-17 Orden "por Tipo"** → **Confirmado**: nivel del tipo asc, luego nombre del tipo asc,
  luego Nro asc. (RN-32, CA-29)
- **DA-18 Totales de estimación** → **No**: no se suman horas estimadas en ningún lado (ni al
  pie del tablero ni como indicador). (RN-35, CA-49)
- **DA-19 Configurar el tablero de un proyecto archivado** → **Confirmado**: no se permite;
  el `PUT` de configuración responde 409 `PROYECTO_ARCHIVADO` y el botón "Columnas" no se
  muestra. Las consultas siguen funcionando. (RN-31, CA-22, CA-35)
- **DA-20 Ubicación de Bloqueada en el Flujo general** → **Opción A** (respuesta del
  usuario): "Bloqueada" se ubica inmediatamente antes del primer estado final, desplazando
  final(es) y posteriores +1 (1 Por hacer, 2 En progreso, 3 Bloqueada, 4 Hecho). Sin final:
  máximo + 1. Si ya está después del final se reubica; si ya está antes, no se toca.
  Excepción aprobada a "no modifica filas existentes", limitada a
  `FlujosIncidenciaEstados.Orden` del Flujo general. (S004, CA-37)
- **DA-21 Clic en tarjetas de indicadores en T1** → **Opción A** (respuesta del usuario): en
  T1 (totales de todos los proyectos) las tarjetas de `IndicadoresComponent` son **solo
  informativas, no clicables**; en T2 siguen siendo clicables y abren el Tablero filtrado
  (sin cambios). El componente compartido admite un modo no clicable (`clicable = false`)
  usado en T1. Alternativa descartada: B (filtrar la lista de proyectos de T1 por los que
  tienen ese indicador). (T1, `IndicadoresComponent`, CA-50)

## Preguntas abiertas

Ninguna. Todas las decisiones (DA-01 a DA-21) están tomadas.
