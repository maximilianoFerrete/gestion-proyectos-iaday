---
name: git-main-basico
description: Flujo Git simple con una sola rama (main) para proyectos .NET chicos de una persona, como prototipos o el challenge del IA Day: commit y push por cada cambio que funcione.
---

# Git básico: una sola rama (main)

## Cuándo usarla

Proyectos chicos de una persona, como un prototipo o el challenge del IA Day.

## Cómo trabajar

1. **Al empezar**, revisá que el proyecto esté en Git y conectado a GitHub:
   - `git status` → si falla, no hay repo.
   - `git remote -v` → si está vacío, no hay GitHub.
   - Si falta algo, **ofrecé** crearlo (no lo hagas sin preguntar):
     - `git init -b main`
     - `dotnet new gitignore` (genera el .gitignore estándar de .NET)
     - `gh repo create <nombre> --private --source=. --remote=origin --push`
2. **Trabajá siempre en la rama `main`.** Si `git branch --show-current` devuelve otra rama, avisame antes de seguir. No crees ramas nuevas.
3. **Antes de cada commit, mostrame qué archivos cambiaron:** `git status --short` y `git diff --stat`.
4. **Cada cambio que funcione: un commit** con un mensaje claro en castellano.
   - "Funciona" = `dotnet build` sin errores y, si hay tests, `dotnet test` en verde.
   - Si el build o los tests fallan, no commitees: avisame.
5. **Después de cada commit, subilo a GitHub:** `git push origin main`.

## Reglas

- **Nunca subas `.env`, claves ni contraseñas.** Antes del primer commit (y si aparecen archivos nuevos de config), revisá que el `.gitignore` incluya al menos:
  - `.env`, `*.env`
  - `bin/`, `obj/`, `.vs/`, `*.user`
  - `appsettings.Development.json` / `appsettings.*.local.json` si tienen secretos
  - `*.pfx`, `*.key`, `secrets.json`
  - Si ves una connection string, API key o password en un archivo trackeado, frená y avisame. Sugerí `dotnet user-secrets` o variables de entorno.
- **Antes de borrar archivos o pisar cambios, preguntame.** Esto incluye `git checkout -- <archivo>`, `git restore`, `git reset --hard` y `git clean`.
- **Nada de force push ni de reescribir la historia:** prohibido `git push --force`, `--force-with-lease`, `git rebase`, `git commit --amend` sobre commits ya pusheados.
- **Si algo se rompe, ofrecé volver al último commit que funcionaba.** Mostrame `git log --oneline -5`, proponé el commit y usá `git revert` (no `reset`) para no reescribir la historia. Esperá mi confirmación.

## Mensajes de commit

- Verbo en presente (tercera persona) + qué cambió.
- Cortos, una línea, sin punto final.
- Ejemplos:
  - "Agrega formulario de contacto"
  - "Corrige validación de email en el registro"
  - "Agrega endpoint GET /productos"
  - "Actualiza paquetes NuGet"

## Al terminar

- Resumime qué se subió: lista de commits de la sesión (`git log --oneline origin/main` de los nuevos) y archivos principales tocados.
- Pasame el link al repo (`gh repo view --json url -q .url`).
