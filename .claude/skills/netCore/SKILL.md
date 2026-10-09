---
name: buenas-practicas-dotnet
description: Buenas prácticas para escribir, revisar y testear código en proyectos .NET modernos (.NET Core / .NET 5+), no .NET Framework. Usar al escribir código C#, validarlo o crear tests unitarios.
---

# Buenas prácticas .NET

## Cuándo usarla

Proyectos .NET de cualquier versión (.NET Core 3.1, .NET 5 en adelante), **excepto .NET Framework**.
Si el proyecto apunta a .NET Framework (`<TargetFramework>net4x</TargetFramework>` o `packages.config`), avisame y no apliques esta skill.

## Cómo trabajar

1. **Antes de escribir código**, revisá el proyecto (`.csproj`, estructura de carpetas, código existente) y aplicá las buenas prácticas de Microsoft que se resumen abajo.
2. **Respetá SOLID** en todo el código nuevo o modificado.
3. **Adaptá las prácticas a la arquitectura del sistema** (capas, Clean Architecture, minimal APIs, etc.). Si la arquitectura existente contradice una práctica, seguí la del proyecto y avisame en vez de reestructurar por tu cuenta.
4. **Al validar o revisar código**, usá estas reglas como checklist y reportá cada incumplimiento con archivo y línea.
5. **Al escribir tests unitarios**, seguí la sección "Tests unitarios".

## Reglas

- **Programación orientada a objetos siempre:** encapsulamiento, abstracción, herencia solo cuando hay una relación "es un" real (preferí composición), polimorfismo mediante interfaces.
- **Comentario en cada clase e interfaz** con fecha de creación y de última modificación (ver formato abajo). Al modificar una clase, actualizá la fecha y agregá una línea con el cambio.
- **Los comentarios en métodos o funciones no pueden superar los 100 caracteres.** Si hace falta explicar más, el método probablemente hace demasiado: dividilo.

### Formato de comentario en clases e interfaces

```csharp
/// <summary>
/// Gestiona el alta y consulta de clientes.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-15 - Agrega validación de CUIT
/// </remarks>
public class ClienteService : IClienteService
```

### Comentarios en métodos (máx. 100 caracteres)

```csharp
/// <summary>Obtiene el cliente por id o null si no existe.</summary>
public async Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct)
```

## SOLID aplicado

- **S – Responsabilidad única:** una clase, un motivo para cambiar. Controllers finos; la lógica va en servicios.
- **O – Abierto/cerrado:** extender con nuevas implementaciones de una interfaz, no con `if`/`switch` por tipo.
- **L – Sustitución de Liskov:** una implementación no debe lanzar `NotImplementedException` ni cambiar el contrato.
- **I – Segregación de interfaces:** interfaces chicas y específicas en lugar de una "`IService`" gigante.
- **D – Inversión de dependencias:** depender de abstracciones e inyectarlas por constructor con el contenedor DI de .NET.

## Prácticas de Microsoft (resumen)

### Estilo y nombres (C# coding conventions)

- `PascalCase` para clases, métodos, propiedades y constantes; `camelCase` para variables y parámetros; `_camelCase` para campos privados.
- Interfaces con prefijo `I` (`IClienteRepository`). Métodos async con sufijo `Async`.
- Usar `var` solo cuando el tipo es obvio por el lado derecho.
- File-scoped namespaces y `using` implícitos cuando la versión lo permita.
- Respetar el `.editorconfig` del repo si existe; si no hay, proponer uno.

### Nullabilidad y tipos

- `<Nullable>enable</Nullable>` activado; no silenciar warnings con `!` sin justificación.
- Preferir `record` para DTOs inmutables y objetos de valor.
- Validar argumentos públicos con `ArgumentNullException.ThrowIfNull(...)` (.NET 6+).

### Async

- Async de punta a punta: nunca `.Result`, `.Wait()` ni `GetAwaiter().GetResult()`.
- Propagar `CancellationToken` en métodos async de I/O.
- Evitar `async void` (solo en event handlers).

### Inyección de dependencias y configuración

- Registrar servicios con el ciclo de vida correcto (`Singleton`, `Scoped`, `Transient`); no inyectar un `Scoped` en un `Singleton`.
- Configuración con el patrón Options (`IOptions<T>`), no leyendo `IConfiguration` en todos lados.
- Secretos fuera del código y de `appsettings.json`: `dotnet user-secrets`, variables de entorno o Key Vault.
- `HttpClient` siempre vía `IHttpClientFactory`.

### Errores y logging

- No usar excepciones para control de flujo. Capturar solo lo que se puede manejar.
- Nunca `catch (Exception) { }` vacío. Re-lanzar con `throw;`, no `throw ex;`.
- Manejo global de errores con middleware / `IExceptionHandler` (ASP.NET Core 8+) y `ProblemDetails`.
- Logging con `ILogger<T>` y mensajes estructurados: `_logger.LogInformation("Cliente {Id} creado", id)`.
- No loguear datos sensibles.

### Recursos y rendimiento

- `using` / `await using` para todo lo que implemente `IDisposable` / `IAsyncDisposable`.
- EF Core: `AsNoTracking()` en consultas de solo lectura, proyectar con `Select` y evitar N+1.
- No materializar colecciones grandes sin necesidad (`ToList()` prematuro).

## Tests unitarios

- **Framework:** usar el que ya tenga el proyecto (xUnit, NUnit o MSTest). Si no hay, proponer xUnit.
- **Un proyecto de tests por proyecto testeado:** `MiApp.Servicios` → `MiApp.Servicios.Tests`.
- **Nombre del test:** `Metodo_Escenario_ResultadoEsperado`. Ej.: `ObtenerPorId_IdInexistente_DevuelveNull`.
- **Patrón AAA** (Arrange, Act, Assert), separado visualmente.
- **Un comportamiento por test.** Sin lógica (`if`, `for`) dentro del test.
- **Aislar dependencias** con mocks/fakes (Moq o NSubstitute, según el proyecto). Sin base de datos, red ni archivos reales.
- **Tests deterministas:** no depender de `DateTime.Now` ni del orden de ejecución; inyectar `TimeProvider` (.NET 8+) o una abstracción.
- **Cubrir casos borde:** nulls, colecciones vacías, límites y excepciones esperadas.
- Usar `[Theory]` / `[InlineData]` (o equivalente) para variantes del mismo caso.
- Correr `dotnet test` después de escribir los tests y reportar el resultado.

```csharp
[Fact]
public async Task ObtenerPorIdAsync_IdInexistente_DevuelveNull()
{
    // Arrange
    var repo = Substitute.For<IClienteRepository>();
    repo.BuscarAsync(99, Arg.Any<CancellationToken>()).Returns((Cliente?)null);
    var sut = new ClienteService(repo);

    // Act
    var resultado = await sut.ObtenerPorIdAsync(99, CancellationToken.None);

    // Assert
    Assert.Null(resultado);
}
```

## Al validar código: checklist

- [ ] El proyecto no es .NET Framework.
- [ ] Clases e interfaces con comentario y fechas de creación y modificación.
- [ ] Ningún comentario de método supera 100 caracteres.
- [ ] SOLID y POO respetados (sin clases "dios", dependencias por interfaz).
- [ ] Nombres según las convenciones de C#.
- [ ] Async correcto, con `CancellationToken` y sin bloqueos.
- [ ] Sin secretos en el código ni en la configuración versionada.
- [ ] Manejo de errores y logging estructurado.
- [ ] Tests unitarios nuevos o actualizados y `dotnet test` en verde.

Reportá el resultado como lista: ✅ cumple / ❌ no cumple (archivo:línea + sugerencia).
