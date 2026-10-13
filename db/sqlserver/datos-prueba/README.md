# Datos de prueba (solo desarrollo local)

Scripts para cargar y borrar datos de prueba en LocalDB (`(localdb)\MSSQLLocalDB`, base
`GestionProyectos`). **No** forman parte del esquema ni de la semilla: `aplicar.ps1` no los
ejecuta y no se registran en `dbo.VersionesEsquema`. Requieren los `V###` y `S###` aplicados.

| Archivo | Qué hace |
|---|---|
| `D001__proyectos_e_incidencias_prueba.sql` | 9 personas `@prueba.local`, 12 proyectos `[PRUEBA] …` (uno archivado), 2–5 agrupaciones por proyecto y entre 100 y 990 incidencias por proyecto (5590 en total) con jerarquía válida. Lee la configuración de la base. Si ya hay proyectos `[PRUEBA] ` no inserta nada. |
| `D900__borra_datos_prueba.sql` | Borra solo los proyectos `[PRUEBA] ` con sus incidencias, agrupaciones y configuración de tablero, y las personas `@prueba.local` no referenciadas por otros datos. |

Desde la raíz del repo (mismos flags que `../README.md`):

```powershell
# Cargar
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\datos-prueba\D001__proyectos_e_incidencias_prueba.sql

# Borrar
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GestionProyectos -b -I -f 65001 -i db\sqlserver\datos-prueba\D900__borra_datos_prueba.sql
```

Para regenerarlos: ejecutar `D900` y luego `D001`. Los códigos de proyecto salen de
`SQ_Proyectos_Numero`, así que cada recarga usa códigos nuevos (como en la app).
