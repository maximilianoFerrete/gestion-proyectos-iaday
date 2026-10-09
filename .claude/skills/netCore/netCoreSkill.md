---
name: netcore
description: >
  .NET/C# moderno con calidad de código, primary constructors, null safety y patrones idiomáticos.
  Trigger: crear clase C#, crear servicio .NET, refactorizar C#, mejorar calidad .NET, escribir código NetCore
license: MIT
metadata:
  author: tu-proyecto
  version: '1.0'
  scope: [root]
  auto_invoke:
    - 'Crear clase C#'
    - 'Crear servicio .NET'
    - 'Refactorizar C#'
    - 'Mejorar calidad .NET'
    - 'Escribir código NetCore'
    - 'Crear controller'
    - 'Crear endpoint'
allowed-tools: Read, Edit, Write, Glob, Grep, Bash
---

# NetCore Skill

## Critical Rules

### ALWAYS

- Usar **primary constructors** (C# 12) para inyección de dependencias — sin campos privados repetitivos
- Usar **`ILogger<T>`** vía inyección por constructor — nunca `Console.WriteLine` en producción
- Usar **`record`** para DTOs inmutables — conciso, con equals/hashCode/toString automáticos
- Habilitar **nullable reference types** (`#nullable enable`) — eliminar null bugs silenciosos
- Usar **`ArgumentNullException.ThrowIfNull`** para guardar cláusulas de parámetros
- Aplicar Single Responsibility: clases < 200 líneas, métodos < 20 líneas
- Usar **LINQ** para procesar colecciones en vez de loops imperativos
- Usar **pattern matching** con `is`, `switch` expressions y `when` guards (C# 8+)
- Usar **`async/await`** correctamente — nunca `.Result` ni `.Wait()` en async contexts
- Definir **jerarquía de excepciones propia** — nunca `catch (Exception e)` silencioso
- Extraer constantes nombradas — sin magic numbers ni magic strings en la lógica
- Usar generics para evitar duplicación: `PaginatedResponse<T>`, `Result<T>`
- Preferir composición sobre herencia

### NEVER

- Usar `Console.WriteLine` para logging en producción — usar `ILogger<T>`
- Usar `Thread.Sleep` — usar `await Task.Delay`
- Usar `.Result` o `.Wait()` en contextos async — causa deadlocks
- Retornar `null` desde métodos que prometen un objeto — lanzar excepción o usar `Result<T>`
- Capturar `Exception` con catch vacío o solo con `Console.WriteLine`
- Crear clases con más de una responsabilidad (God classes)
- Usar estado mutable estático sin thread-safety
- Duplicar lógica entre clases — extraer a base, interfaz o utility
- Usar `var` para tipos no obvios — preferir tipo explícito cuando mejora legibilidad

---

## Primary Constructors — Inyección sin Boilerplate (C# 12)

```csharp
// ❌ MAL — boilerplate repetitivo con campo privado
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }
}

// ✅ BIEN — primary constructor elimina el boilerplate
public class UserService(IUserRepository userRepository, ILogger<UserService> logger)
{
    public async Task<UserResponse> GetByIdAsync(int id)
    {
        var user = await userRepository.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(nameof(User), id);
        logger.LogInformation("User retrieved id={Id}", id);
        return UserResponse.From(user);
    }
}
```

---

## ILogger<T> — Logging Estructurado

```csharp
// ❌ MAL — Console.WriteLine (no structured, no levels, no sink)
Console.WriteLine($"User created: {user.Id}");

// ✅ BIEN — ILogger<T> con message templates estructurados
public class UserService(ILogger<UserService> logger)
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        logger.LogInformation("Creating user email={Email}", request.Email);

        // ... lógica

        logger.LogInformation("User created id={Id} email={Email}", user.Id, user.Email);
        return UserResponse.From(user);
    }
}

// Niveles de log:
logger.LogDebug("Debug info: {Data}", data);
logger.LogInformation("User {Id} logged in", userId);
logger.LogWarning("Retry attempt {Attempt} for {Operation}", attempt, operation);
logger.LogError(ex, "Failed to process order {OrderId}", orderId);
logger.LogCritical("Database connection lost");
```

---

## Record — DTO Inmutable

```csharp
// ✅ BIEN — record es conciso, inmutable y genera equals/hashCode/toString
public record CreateUserRequest(
    [Required] string Name,
    [EmailAddress] string Email,
    UserRole Role
);

public record UserResponse(int Id, string Name, string Email, UserRole Role)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Name, user.Email, user.Role);
}

// Record con validación adicional:
public record PaginationRequest(int Page = 1, int PageSize = 20)
{
    public int Page { get; init; } = Page > 0 ? Page : 1;
    public int PageSize { get; init; } = PageSize is > 0 and <= 100 ? PageSize : 20;
}
```

---

## Nullable Reference Types — Null Safety

```csharp
// Habilitar en el proyecto completo (en .csproj):
// <Nullable>enable</Nullable>

// O en el archivo:
#nullable enable

// ❌ MAL — null implícito que el compilador no detecta
public User GetById(int id) => _repo.FindById(id); // puede retornar null

// ✅ BIEN — null explícito con ? o lanzar excepción
public User? FindById(int id) => _repo.FindById(id); // puede ser null
public User GetById(int id) =>                        // nunca null
    _repo.FindById(id) ?? throw new EntityNotFoundException(nameof(User), id);

// ArgumentNullException.ThrowIfNull — guard clause idiomática
public void Process(Order order)
{
    ArgumentNullException.ThrowIfNull(order);
    ArgumentNullException.ThrowIfNull(order.Items, nameof(order.Items));
    // lógica principal sin anidación
}
```

---

## LINQ — Colecciones sin Loops Imperativos

```csharp
// ❌ MAL — loop imperativo
var activeEmails = new List<string>();
foreach (var user in users)
{
    if (user.IsActive)
        activeEmails.Add(user.Email.ToLower());
}

// ✅ BIEN — LINQ declarativo
var activeEmails = users
    .Where(u => u.IsActive)
    .Select(u => u.Email.ToLower())
    .ToList();

// Agrupar:
var byRole = users.GroupBy(u => u.Role)
                  .ToDictionary(g => g.Key, g => g.ToList());

// Proyección con tipo anónimo o record:
var summary = users
    .Where(u => u.IsActive)
    .Select(u => new { u.Id, u.Email, u.Role })
    .ToList();

// Primer elemento o excepción:
var admin = users.FirstOrDefault(u => u.Role == UserRole.Admin)
    ?? throw new EntityNotFoundException("Admin user not found");

// Any / All:
bool hasAdmins = users.Any(u => u.Role == UserRole.Admin);
bool allActive = users.All(u => u.IsActive);
```

---

## Pattern Matching — Sin Casteos (C# 8+)

```csharp
// ❌ MAL — casteo explícito
if (shape is Circle) { var c = (Circle)shape; Console.WriteLine(c.Radius); }

// ✅ BIEN — pattern matching con is
if (shape is Circle circle) { logger.LogInformation("Radio: {R}", circle.Radius); }

// ✅ MEJOR — switch expression con pattern matching (C# 8+)
string description = shape switch
{
    Circle    c when c.Radius > 10 => $"Círculo grande (r={c.Radius})",
    Circle    c                    => $"Círculo pequeño (r={c.Radius})",
    Rectangle { Width: var w, Height: var h } => $"Rectángulo {w}x{h}",
    _                              => "Forma desconocida"
};

// Deconstruct pattern en if:
if (result is Result<User>.Success { Value: var user })
{
    return UserResponse.From(user);
}
```

---

## Service Pattern (ASP.NET Core + primary constructor)

```csharp
public class UserService(
    IUserRepository userRepository,
    ILogger<UserService> logger)
{
    public async Task<UserResponse> GetByIdAsync(int id)
    {
        var user = await userRepository.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(nameof(User), id);
        return UserResponse.From(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await userRepository.ExistsByEmailAsync(request.Email))
            throw new ConflictException($"Email {request.Email} already in use", "EMAIL_CONFLICT");

        var user = new User { Name = request.Name, Email = request.Email, Role = request.Role };
        await userRepository.AddAsync(user);

        logger.LogInformation("User created id={Id} email={Email}", user.Id, user.Email);
        return UserResponse.From(user);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(nameof(User), id);

        user.Name  = request.Name;
        user.Email = request.Email;
        await userRepository.UpdateAsync(user);

        logger.LogInformation("User updated id={Id}", id);
        return UserResponse.From(user);
    }
}
```

---

## Exception Hierarchy — Sin Capturar Genéricos

```csharp
// Jerarquía propia — evita lanzar/capturar Exception genérico
public abstract class DomainException(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}

public class EntityNotFoundException(string entity, object id)
    : DomainException($"{entity} with id {id} not found", $"{entity.ToUpper()}_NOT_FOUND");

public class ConflictException(string message, string code) : DomainException(message, code);

public class ValidationException(string message) : DomainException(message, "VALIDATION_ERROR");

// ❌ MAL
try { RiskyOperation(); } catch (Exception e) { Console.WriteLine(e); }

// ✅ BIEN
try
{
    await RiskyOperationAsync();
}
catch (SpecificException ex)
{
    logger.LogError(ex, "Error procesando operación: {Message}", ex.Message);
    throw new DomainException("Error en operación X", "OP_X_ERROR");
}
```

---

## Guard Clauses — Inputs al Inicio

```csharp
// ❌ MAL — lógica principal enterrada en niveles de if anidados
public void ProcessOrder(Order order)
{
    if (order != null)
    {
        if (order.Items != null && order.Items.Any())
        {
            if (order.Total > 0) { /* lógica */ }
        }
    }
}

// ✅ BIEN — fail fast con guard clauses
public void ProcessOrder(Order order)
{
    ArgumentNullException.ThrowIfNull(order);

    if (!order.Items.Any())
        throw new ValidationException("Order must have at least one item");

    if (order.Total <= 0)
        throw new ValidationException("Order total must be positive");

    // lógica principal sin anidación
}
```

---

## Generics — Sin Duplicación

### Respuesta paginada genérica

```csharp
// ✅ Una sola definición — usada en todos los módulos
public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PaginatedResponse<T> From(IEnumerable<T> source, int page, int pageSize, int totalCount) =>
        new(source.ToList().AsReadOnly(), page, pageSize, totalCount);
}
```

### Resultado genérico (Success / Failure)

```csharp
// Discriminated union type-safe
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string Message, string Code) : Result<T>;

    public static Result<T> Ok(T value)                         => new Success(value);
    public static Result<T> Fail(string message, string code)   => new Failure(message, code);

    public bool IsSuccess => this is Success;
}

// Uso con pattern matching:
var result = await userService.TryCreateAsync(request);
return result switch
{
    Result<User>.Success s => Ok(UserResponse.From(s.Value)),
    Result<User>.Failure f => BadRequest(new { f.Message, f.Code }),
    _ => throw new UnreachableException()
};
```

---

## Async/Await — Sin Deadlocks

```csharp
// ❌ MAL — .Result y .Wait() causan deadlocks en ASP.NET Core
var user = userRepository.GetByIdAsync(id).Result;
userRepository.SaveAsync(user).Wait();

// ✅ BIEN — propagar async hasta el controller
public async Task<UserResponse> GetByIdAsync(int id)
{
    var user = await userRepository.GetByIdAsync(id)
        ?? throw new EntityNotFoundException(nameof(User), id);
    return UserResponse.From(user);
}

// ConfigureAwait(false) — en librerías (no en ASP.NET Core controllers)
var data = await httpClient.GetStringAsync(url).ConfigureAwait(false);

// Paralelo cuando no hay dependencias:
var (users, roles) = await (
    userRepository.GetAllAsync(),
    roleRepository.GetAllAsync()
).WhenAll();

// Cancelation token — propagar siempre
public async Task<List<User>> SearchAsync(string query, CancellationToken ct = default)
{
    return await userRepository.SearchAsync(query, ct);
}
```

---

## Commands

```bash
# Crear proyecto
dotnet new webapi -n MyProject

# Compilar
dotnet build

# Tests
dotnet test

# Tests con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Análisis estático (Roslyn Analyzers — incluidos en .NET 5+)
dotnet build /p:TreatWarningsAsErrors=true

# Linter / formato
dotnet format

# Ejecutar aplicación
dotnet run

# Publicar
dotnet publish -c Release -o ./publish

# Migrations (EF Core)
dotnet ef migrations add NombreMigracion
dotnet ef database update
```

---

## Project-Specific Notes

Adaptá esta sección a las convenciones de tu proyecto:

- **Versión**: .NET 8+ (LTS recomendado) o .NET 9+
- **Framework**: ASP.NET Core Web API / Minimal APIs / gRPC
- **ORM**: Entity Framework Core / Dapper / NHibernate
- **Testing**: xUnit + Moq + FluentAssertions
- **Nullable**: habilitar `<Nullable>enable</Nullable>` en el `.csproj`
- **Primary constructors**: disponibles desde C# 12 (.NET 8+)
- **Analyzers incluidos**: Microsoft.CodeAnalysis.NetAnalyzers (incluido en .NET 5+)

### Paquetes NuGet recomendados

```xml
<!-- Testing -->
<PackageReference Include="xunit" Version="2.9.*" />
<PackageReference Include="Moq" Version="4.20.*" />
<PackageReference Include="FluentAssertions" Version="6.12.*" />

<!-- EF Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />

<!-- Validación -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />

<!-- Logging estructurado (opcional — Serilog) -->
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
```
