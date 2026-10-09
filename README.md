# Agentes para aplicación .NET + Angular

Equipo de subagentes de Claude Code para desarrollar una aplicación ASP.NET Core + Angular
con SQL Server (LocalDB en desarrollo) y scripts SQL puros.

## Instalación

Copiá la carpeta `.claude/` en la raíz del repo:

```
<repo>/
  .claude/
    agents/
      orquestador.md
      analista-requerimientos.md
      dba-sql.md
      dev-backend.md
      dev-frontend.md
      dev-tests.md
    skills/
      buenas-practicas-dotnet/SKILL.md   # usada por dev-backend y dev-tests
      git-main-basico/SKILL.md           # usada por el orquestador para commits
    settings.json
```

## Uso

El orquestador tiene que ser el **agente principal de la sesión**, porque un subagente no
puede lanzar otros subagentes. Dos formas:

- `settings.json` incluido: `"agent": "orquestador"` lo deja como agente por defecto del repo.
- O al iniciar: `claude --agent orquestador`.

Después, pasale un requerimiento funcional:

> RF: Necesitamos un ABM de clientes con nombre, CUIT y email. El CUIT no se puede repetir.

## Flujo

```
Usuario ─► orquestador
              ├─► analista-requerimientos ─► docs/specs/RF-XXX.md
              │        (checkpoint: el usuario aprueba la spec)
              ├─► dba-sql ─► db/sqlserver/scripts/V###__*.sql  (aplicados en LocalDB)
              ├─► dev-backend  ─┐ (en paralelo si el contrato API está cerrado)
              ├─► dev-frontend ─┘
              ├─► dev-tests ─► tests + reporte
              │        (si falla: vuelve al agente responsable, máx. 2 ciclos)
              └─► checklist + docs/avance.md + commit
```

## Decisiones de diseño

- **Spec como contrato**: los agentes no se hablan entre sí; todo pasa por la spec y el orquestador.
- **Capa de datos intercambiable**: interfaces en `Application`, Dapper en
  `Infrastructure.SqlServer`. Para cambiar de motor se agrega `Infrastructure.<Motor>` y
  `db/<motor>/scripts/`, y se cambia una línea de DI.
- **Solo el orquestador hace commits.**
- **Permisos de herramientas**: el analista no ejecuta comandos; el resto sí (build, sqlcmd, tests).

## Requisitos en la máquina

- .NET SDK, Node.js + Angular CLI, SQL Server LocalDB y `sqlcmd`.
- LocalDB solo funciona en Windows.
