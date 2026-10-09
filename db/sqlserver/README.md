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
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\seed\S001__estados_proyecto_iniciales.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\seed\S002__persona_administrador.sql
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
| `S001` | 7 estados (inicial "En planificación"; finales Finalizado/Cancelado; sistema Archivado = `ARCHIVADO`) |
| `S002` | Persona Administrador (`admin@gestionproyectos.local`, `EsAdmin = 1`) |

Recrear desde cero (pierde todos los datos):
`sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -Q "ALTER DATABASE GestionProyectos SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE GestionProyectos"`
y volver a ejecutar `aplicar.ps1`.
