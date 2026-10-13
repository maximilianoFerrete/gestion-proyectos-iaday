# SQL Server – GestionProyectos

Instancia de desarrollo: `(localdb)\MSSQLLocalDB` · Base: `GestionProyectos`.
Convenciones y construcciones específicas de SQL Server: ver [`../README.md`](../README.md).

## Opción 1: script automático

```powershell
.\db\sqlserver\aplicar.ps1                     # crea la base si falta, aplica V pendientes y seeds
.\db\sqlserver\aplicar.ps1 -OmitirSeed
.\db\sqlserver\aplicar.ps1 -Servidor "(localdb)\MSSQLLocalDB" -Base GestionProyectos
```

Omite los `V###` ya registrados en `dbo.VersionesEsquema`; los seeds se ejecutan siempre
(son idempotentes). Corta ante el primer error.

## Opción 2: manual con sqlcmd (en este orden, desde la raíz del repo)

Flags: `-b` corta ante error (obligatorio), `-I` activa `QUOTED_IDENTIFIER` (índices
filtrados), `-f 65001` lee los archivos como UTF-8 (acentos de la semilla).

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d master           -b -I -f 65001 -i db\sqlserver\crear_base.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V001__crea_tabla_versiones_esquema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V002__crea_tabla_personas.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V003__crea_tabla_estados_proyecto.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V004__crea_secuencia_y_tabla_proyectos.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V005__crea_tabla_agrupaciones.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V006__agrega_contador_incidencias_a_proyectos.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V007__crea_tablas_estados_flujos_prioridades_incidencia.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V008__crea_tablas_niveles_tipos_relaciones_incidencia.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V009__crea_tabla_incidencias.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V010__agrega_categoria_a_estados_incidencia_e_indice_seguimiento.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V011__agrega_estimacion_a_incidencias.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\scripts\V012__crea_configuraciones_tablero_proyecto.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\seed\S001__estados_proyecto_iniciales.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\seed\S002__persona_administrador.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\seed\S003__configuracion_incidencias_inicial.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\seed\S004__estado_bloqueada.sql
```

Verificación:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -W -Q "SELECT * FROM dbo.VersionesEsquema ORDER BY Version"
```

## Contenido

| Archivo | Crea |
|---|---|
| `crear_base.sql` | Base `GestionProyectos` (si no existe) + validación de collation `*_CI_*` |
| `V001` | `VersionesEsquema` |
| `V002` | `Personas` (`UQ_Personas_Email`) |
| `V003` | `EstadosProyecto` (CHECKs, `UX_EstadosProyecto_EsInicial`, `UX_EstadosProyecto_Codigo` filtrados) |
| `V004` | Secuencia `SQ_Proyectos_Numero` + `Proyectos` (4 FKs, `CK_Proyectos_Fechas`, `CK_Proyectos_Archivado`, índices `IX_Proyectos_*`) |
| `V005` | `Agrupaciones` (`UQ_Agrupaciones_ProyectoId_Nombre`, `UQ_Agrupaciones_ProyectoId_Id`) |
| `V006` | `Proyectos.UltimoNumeroIncidencia` (`DF_Proyectos_UltimoNumeroIncidencia` = 0, `CK_Proyectos_UltimoNumeroIncidencia` >= 0): contador de numeración de incidencias |
| `V007` | `EstadosIncidencia`, `FlujosIncidencia`, `FlujosIncidenciaEstados` (PK `(FlujoId, EstadoId)`, `UX_FlujosIncidenciaEstados_FlujoId_EsInicial` filtrado, `CK_..._InicialNoFinal`), `PrioridadesIncidencia` (`UX_PrioridadesIncidencia_EsPredeterminada` filtrado, `CK_..._PredeterminadaActiva`) |
| `V008` | `NivelesIncidencia` (`UQ_..._Numero`, `UQ_..._Nombre`), `TiposIncidencia` (FK a nivel y flujo), `RelacionesTiposIncidencia` (`UQ_..._TipoPadreId_TipoHijoId`, `CK_..._TiposDistintos`) |
| `V009` | `Incidencias` (`UQ_Incidencias_ProyectoId_Numero`, `UQ_Incidencias_Codigo`, `UQ_Incidencias_ProyectoId_Id`, FK compuestas `FK_Incidencias_Agrupaciones` y `FK_Incidencias_Incidencias_Padre`, `CK_Incidencias_PadreDistinto`, índices `IX_Incidencias_*`) |
| `V010` | `EstadosIncidencia.Categoria NVARCHAR(20) NULL` (`CK_EstadosIncidencia_Categoria`: `NULL`, `BLOQUEADA`, `EN_ESPERA`) e índice `IX_Incidencias_ProyectoId_EstadoId` `(ProyectoId, EstadoId) INCLUDE (TipoId, AgrupacionId, ResponsableId, FechaVencimiento)` (RF-005) |
| `V011` | `Incidencias.Estimacion DECIMAL(9,2) NULL` (horas; `CK_Incidencias_Estimacion` >= 0) (RF-005, cambio sobre RF-002) |
| `V012` | `ConfiguracionesTableroProyecto` (PK = FK `ProyectoId` → `Proyectos`, 1:0..1; `MostrarEstado`, `MostrarEstimacion`, `MostrarResponsable` BIT sin default; `FechaModificacion`). Sin fila = todas las columnas visibles (RF-005) |
| `S001` | 7 estados (inicial "En planificación"; finales Finalizado/Cancelado; sistema Archivado = `ARCHIVADO`) |
| `S002` | Persona Administrador (`admin@gestionproyectos.local`, `EsAdmin = 1`) |
| `S003` | Configuración de incidencias: niveles 1–3; estados Por hacer/En progreso/Hecho; "Flujo general" (Por hacer inicial → En progreso → Hecho final); tipos Épica (N1), Historia de Usuario (N2), Subtarea/Caso de Prueba/Bug (N3) con "Flujo general"; relaciones Épica→HU, HU→Subtarea/Caso de Prueba/Bug; prioridades Baja/Media (predeterminada)/Alta/Crítica |
| `S004` | Estado de incidencia "Bloqueada" (Orden 24, categoría `BLOQUEADA`) agregado al "Flujo general" al final (ni inicial ni final, `Orden` = máx. + 1). No crea "En espera" (RF-005 DA-02) |

Recrear desde cero (pierde todos los datos):
`sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -Q "ALTER DATABASE GestionProyectos SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE GestionProyectos"`
y volver a ejecutar `aplicar.ps1`.
