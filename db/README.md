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
| Índices únicos **filtrados** (`CREATE UNIQUE INDEX ... WHERE ...`) | `UX_EstadosProyecto_EsInicial` (máx. un estado inicial), `UX_EstadosProyecto_Codigo` | PostgreSQL: índices parciales (misma sintaxis `WHERE`). Requieren `SET QUOTED_IDENTIFIER ON` y `ANSI_NULLS ON` para crear y para hacer DML (SqlClient los usa por defecto; con `sqlcmd` usar `-I`) |
| `CREATE SEQUENCE dbo.SQ_Proyectos_Numero` / `NEXT VALUE FOR` | Código `PRJ-0001` (RN-14) | Estándar SQL; PostgreSQL: `nextval('sq_proyectos_numero')` |
| `SYSUTCDATETIME()` | `DF_*_FechaCreacion`, `DF_VersionesEsquema_FechaAplicacion` | PostgreSQL: `(now() AT TIME ZONE 'utc')` |
| `DATETIME2(0)` | Fechas con hora (UTC) | PostgreSQL: `timestamp(0)` |
| Collation `*_CI_AS` | Unicidad de nombres/emails sin distinguir mayúsculas (`UQ_Personas_Email`, `UQ_EstadosProyecto_Nombre`, `UQ_Agrupaciones_ProyectoId_Nombre`) | PostgreSQL: `citext` o índice único sobre `lower(col)` |
| `NVARCHAR(n)` con literales `N'...'` | Textos | PostgreSQL: `varchar(n)` |
| `OBJECT_ID`, `sys.indexes`, `TRY/CATCH`, `THROW`, `XACT_STATE()` | Idempotencia y transacciones de los scripts | PostgreSQL: `IF NOT EXISTS` en DDL, bloques `DO $$ ... $$` |
| `OUTPUT INSERTED.*`, `OFFSET ... FETCH NEXT` | Consultas del backend (no en scripts) | PostgreSQL: `RETURNING`, `LIMIT/OFFSET` |
