# Base de datos

Scripts SQL puros versionados (sin migraciones de EF Core). Un directorio por motor:
`db/<motor>/` con la misma numeración. Motor actual: **SQL Server** (`db/sqlserver/`).

## Desarrollo (LocalDB)

| Dato | Valor |
|---|---|
| Instancia | `(localdb)\MSSQLLocalDB` |
| Base | `GestionProyectos` |
| Collation | la de la instancia; debe ser case-insensitive (`*_CI_AS`, hoy `SQL_Latin1_General_CP1_CI_AS`). `crear_base.sql` lo valida |
| Cadena de conexión (backend) | `Server=(localdb)\MSSQLLocalDB;Database=GestionProyectos;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=False` |

Cómo crear la base y aplicar los scripts: ver [`sqlserver/README.md`](sqlserver/README.md).
Atajo: `.\db\sqlserver\aplicar.ps1`.

## Convenciones

- `scripts/V###__descripcion.sql`: DDL. Nunca se modifica un script ya aplicado; los cambios
  van en un script nuevo. Idempotentes (`IF OBJECT_ID(...) IS NULL`, `IF NOT EXISTS`) y en una
  transacción con `TRY/CATCH` + `ROLLBACK`.
- `seed/S###__descripcion.sql`: datos iniciales, idempotentes (insertan solo lo que falta;
  no pisan cambios del usuario).
- Cada script registra su versión (`V001`, `S001`, ...) en `dbo.VersionesEsquema`.
- Nombres: tablas en plural PascalCase, PK `Id`, FK `<Entidad>Id`, constraints con prefijo
  `PK_`, `FK_`, `UQ_`, `UX_` (índice único filtrado), `IX_`, `CK_`, `DF_`, secuencias `SQ_`.
- Sin stored procedures, triggers ni funciones: la lógica vive en el backend.

## Construcciones específicas de SQL Server (a traducir si se cambia de motor)

| Construcción | Dónde | Equivalente en otros motores |
|---|---|---|
| `INT IDENTITY(1,1)` | PK de todas las tablas | PostgreSQL: `INT GENERATED ALWAYS AS IDENTITY` |
| Índices únicos **filtrados** (`CREATE UNIQUE INDEX ... WHERE ...`) | `UX_EstadosProyecto_EsInicial` (máx. un estado inicial), `UX_EstadosProyecto_Codigo`, `UX_FlujosIncidenciaEstados_FlujoId_EsInicial` (máx. un inicial **por flujo**), `UX_PrioridadesIncidencia_EsPredeterminada` (máx. una prioridad predeterminada) | PostgreSQL: índices parciales (misma sintaxis `WHERE`). Requieren `SET QUOTED_IDENTIFIER ON` y `ANSI_NULLS ON` para crear y para hacer DML (SqlClient los usa por defecto; con `sqlcmd` usar `-I`). Violación: error 2601 (no 2627) |
| FK **compuestas** contra `UNIQUE (ProyectoId, Id)` | `FK_Incidencias_Agrupaciones` `(ProyectoId, AgrupacionId)` → `Agrupaciones (ProyectoId, Id)`; `FK_Incidencias_Incidencias_Padre` `(ProyectoId, PadreId)` → `Incidencias (ProyectoId, Id)` (**autorreferencial**): la BD garantiza que agrupación y padre son del mismo proyecto. Con la columna opcional en `NULL` la FK no se evalúa (`MATCH SIMPLE`) | Estándar SQL; PostgreSQL igual (`MATCH SIMPLE` por defecto). La autorreferencial se crea con `ALTER TABLE` después de la `UNIQUE` destino |
| CTE recursivas `WITH x AS (ancla UNION ALL recursivo) ... OPTION (MAXRECURSION 100)` | Ancestros/descendientes de incidencias (backend, RF-002 0.4); índice `IX_Incidencias_PadreId` | PostgreSQL: `WITH RECURSIVE`; sin `OPTION (MAXRECURSION)` (limitar con una columna de profundidad `WHERE Profundidad < 100`) |
| Contador por proyecto con `UPDATE ... OUTPUT INSERTED.*` (bloqueo exclusivo de fila hasta el commit) y `SELECT ... WITH (UPDLOCK, ROWLOCK)` | `Proyectos.UltimoNumeroIncidencia` (numeración de incidencias, RF-002 0.3) | PostgreSQL: `UPDATE ... RETURNING` y `SELECT ... FOR UPDATE` |
| `CREATE SEQUENCE dbo.SQ_Proyectos_Numero` / `NEXT VALUE FOR` | Código `PRJ-0001` (RN-14) | Estándar SQL; PostgreSQL: `nextval('sq_proyectos_numero')` |
| `SYSUTCDATETIME()` | `DF_*_FechaCreacion`, `DF_VersionesEsquema_FechaAplicacion` | PostgreSQL: `(now() AT TIME ZONE 'utc')` |
| `DATETIME2(0)` | Fechas con hora (UTC) | PostgreSQL: `timestamp(0)` |
| Collation `*_CI_AS` | Unicidad de nombres/emails sin distinguir mayúsculas (`UQ_Personas_Email`, `UQ_EstadosProyecto_Nombre`, `UQ_Agrupaciones_ProyectoId_Nombre`, `UQ_NivelesIncidencia_Nombre`, `UQ_TiposIncidencia_Nombre`, `UQ_EstadosIncidencia_Nombre`, `UQ_FlujosIncidencia_Nombre`, `UQ_PrioridadesIncidencia_Nombre`) | PostgreSQL: `citext` o índice único sobre `lower(col)` |
| `NVARCHAR(n)` con literales `N'...'` | Textos | PostgreSQL: `varchar(n)` |
| `OBJECT_ID`, `COL_LENGTH`, `sys.indexes`, `sys.check_constraints`, `sys.foreign_keys`, `TRY/CATCH`, `THROW`, `XACT_STATE()` | Idempotencia y transacciones de los scripts | PostgreSQL: `IF NOT EXISTS` en DDL, bloques `DO $$ ... $$` |
| `EXEC (N'ALTER TABLE ... ADD CONSTRAINT CK_...')` (SQL dinámico) | `V006`: CHECK sobre una columna agregada en el mismo lote (evita "Invalid column name" en compilación) | PostgreSQL: no hace falta (DDL transaccional en un bloque `DO`) |
| `OUTPUT INSERTED.*`, `OFFSET ... FETCH NEXT` | Consultas del backend (no en scripts) | PostgreSQL: `RETURNING`, `LIMIT/OFFSET` |

## Integridad: qué garantiza la BD y qué el backend (RF-002)

- **BD**: unicidades, CHECKs, FK simples y compuestas (padre y agrupación del mismo proyecto),
  máximo un estado inicial por flujo y máximo una prioridad predeterminada (índices filtrados).
- **Backend** (no expresable sin triggers/funciones, que están excluidos por convención):
  exactamente un inicial por flujo, nivel del padre < nivel del hijo en relaciones de tipos,
  estado de la incidencia dentro del flujo de su tipo, relación de tipos permitida, ausencia de
  ciclos, agrupación heredada del padre, vencimiento hija ≤ padre y desasociación de
  incidencias al eliminar una agrupación (la FK no tiene `ON DELETE SET NULL`).
