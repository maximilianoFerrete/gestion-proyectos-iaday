---
name: typescript
description: >
  TypeScript estricto con tipado fuerte, calidad de código y sin duplicación.
  Trigger: crear interfaces, tipos, DTOs, refactorizar tipado, calidad de código TypeScript
license: MIT
metadata:
  author: tu-proyecto
  version: '1.1'
  scope: [root]
  auto_invoke:
    - 'Crear interface'
    - 'Crear tipo'
    - 'Crear DTO'
    - 'Refactorizar tipado'
    - 'Agregar tipos'
    - 'Evitar duplicación de tipos'
    - 'Mejorar calidad de código TypeScript'
allowed-tools: Read, Edit, Write, Glob, Grep
---

# TypeScript Skill

## Critical Rules

### ALWAYS

- Tipar explícitamente parámetros de funciones y retornos públicos
- Usar `class-validator` decoradores en DTOs
- Usar `class-transformer` para transformaciones
- Nombrar DTOs: `{action}-{entity}.dto.ts`
- Preferir utility types (`Omit`, `Pick`, `Partial`, `Record`) antes de duplicar tipos
- Extraer tipos compartidos a `src/types/` o `src/common/types/`
- Usar type guards (`value is T`) en lugar de casteos con `as`
- Usar `readonly` en propiedades que no deben mutar
- Tipar el `catch` como `unknown` y narrowing con type guard
- Preferir `const` sobre `let`, nunca `var`
- Mantener funciones con 1 responsabilidad (< 30 líneas como guía)
- Usar `satisfies` para validar literales sin perder el tipo exacto (TS 4.9+)

### NEVER

- Usar `any` sin justificación documentada
- Usar `as` para forzar tipos (preferir type guards o narrowing)
- Duplicar definiciones de tipo — derivarlas del tipo base
- Usar `object` o `{}` como tipo (usar `Record<string, unknown>` o interfaz)
- Usar `Function` como tipo (usar firma específica: `() => void`)
- Mutar parámetros de funciones directamente
- Dejar variables sin tipo cuando no es inferible
- Omitir validación en DTOs expuestos a usuarios
- Crear catch vacíos o con `catch (e: any)`

---

## DTO Pattern

```typescript
// dto/create-{entity}.dto.ts
import {
  IsString,
  IsNotEmpty,
  IsOptional,
  IsEmail,
  IsInt,
  Min,
} from 'class-validator';
import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class CreateEntityDto {
  @ApiProperty({ description: 'Nombre del entity', example: 'Mi Entity' })
  @IsString()
  @IsNotEmpty()
  name: string;

  @ApiPropertyOptional({ description: 'Descripción opcional' })
  @IsString()
  @IsOptional()
  description?: string;

  @ApiProperty({
    description: 'Email de contacto',
    example: 'test@example.com',
  })
  @IsEmail()
  @IsNotEmpty()
  email: string;

  @ApiProperty({ description: 'Cantidad', minimum: 1 })
  @IsInt()
  @Min(1)
  quantity: number;
}
```

---

## Update DTO Pattern (Partial — sin duplicar campos)

```typescript
// dto/update-{entity}.dto.ts
import { PartialType } from '@nestjs/swagger';
import { CreateEntityDto } from './create-entity.dto';

// Hereda todos los campos de Create pero los hace opcionales
export class UpdateEntityDto extends PartialType(CreateEntityDto) {}
```

---

## Interface & Type Pattern

```typescript
// interfaces/{entity}.interface.ts
export interface Entity {
  readonly id: string;        // readonly: no debe mutar
  name: string;
  description?: string;
  createdAt: Date;
  updatedAt: Date;
}

// Extender sin duplicar campos
export interface EntityWithRelations extends Entity {
  user: User;
  items: Item[];
}
```

---

## Utility Types — Anti-Duplicación

Derivar tipos del tipo base en vez de redefinirlos:

```typescript
// ❌ MAL — duplica campos
type UserResponse = {
  id: string;
  name: string;
  email: string;
};

// ✅ BIEN — deriva del tipo base
type UserResponse     = Pick<User, 'id' | 'name' | 'email'>;
type UserInput        = Omit<User, 'id' | 'createdAt' | 'updatedAt'>;
type PartialUser      = Partial<User>;
type RequiredUser     = Required<User>;
type ReadonlyUser     = Readonly<User>;

// Record: mapas tipados (evita duplicar la estructura)
type RolePermissions = Record<UserRole, string[]>;
const permissions: RolePermissions = {
  ADMIN: ['read', 'write', 'delete'],
  USER:  ['read'],
};

// Inferir tipos de funciones existentes (evita redeclarar)
type ServiceReturn  = ReturnType<typeof userService.findOne>;
type ServiceParams  = Parameters<typeof userService.create>[0];

// satisfies: valida el literal sin perder el tipo exacto
const config = {
  timeout: 3000,
  retries: 3,
} satisfies Partial<AppConfig>;
```

---

## Generics — Reutilización sin Duplicación

### Respuesta paginada genérica

```typescript
// common/types/pagination.types.ts
export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  limit: number;
}

// Uso — no duplicar la estructura en cada módulo
type UserList  = PaginatedResponse<User>;
type EventList = PaginatedResponse<Event>;
```

### Resultado genérico (Success / Error)

```typescript
// common/types/result.types.ts
export type ApiResult<T> =
  | { success: true;  data: T }
  | { success: false; error: string; code: string };

// Uso con discriminated union (TypeScript narrowing automático)
function handleResult<T>(result: ApiResult<T>): T {
  if (!result.success) {
    throw new AppError(result.error, result.code);
  }
  return result.data;  // TypeScript sabe que data existe aquí
}
```

### Función genérica con constraint

```typescript
// Buscar por id sin duplicar la lógica para cada entidad
function findById<T extends { id: string }>(
  items: T[],
  id: string,
): T | undefined {
  return items.find((item) => item.id === id);
}

// Repositorio base genérico
interface BaseRepository<T, CreateDto, UpdateDto = Partial<CreateDto>> {
  findAll(): Promise<T[]>;
  findById(id: string): Promise<T | null>;
  create(dto: CreateDto): Promise<T>;
  update(id: string, dto: UpdateDto): Promise<T>;
  softDelete(id: string): Promise<void>;
}
```

---

## Type Guards & Narrowing

En lugar de casteos con `as`, usar type guards:

```typescript
// ❌ MAL
const user = response as User;

// ✅ BIEN — type guard explícito
function isUser(value: unknown): value is User {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    typeof (value as Record<string, unknown>).id === 'string' &&
    'email' in value
  );
}

// Discriminated union con narrowing automático
type Shape =
  | { kind: 'circle';    radius: number }
  | { kind: 'rectangle'; width: number; height: number };

function area(shape: Shape): number {
  switch (shape.kind) {
    case 'circle':    return Math.PI * shape.radius ** 2;
    case 'rectangle': return shape.width * shape.height;
    // TypeScript detecta si falta un case (exhaustiveness check)
  }
}

// Narrowing con instanceof
function processError(error: unknown): never {
  if (error instanceof AppError) throw error;
  if (error instanceof Error)    throw new AppError(error.message, 'UNKNOWN');
  throw new AppError('Error desconocido', 'UNKNOWN');
}
```

---

## Error Typing

```typescript
// errors/app-error.ts
export class AppError extends Error {
  constructor(
    message: string,
    public readonly code: string,
    public readonly statusCode: number = 500,
  ) {
    super(message);
    this.name = 'AppError';
    // Mantiene el stack trace correcto en V8
    Error.captureStackTrace(this, this.constructor);
  }
}

export function isAppError(error: unknown): error is AppError {
  return error instanceof AppError;
}

// ❌ MAL
try {
  await riskyOperation();
} catch (e: any) {    // any en catch — nunca
  console.error(e.message);
}

// ✅ BIEN
try {
  await riskyOperation();
} catch (error: unknown) {
  if (isAppError(error)) throw error;                        // Re-throw conocidos
  if (error instanceof Error) throw new AppError(error.message, 'OP_ERROR');
  throw new AppError('Error inesperado', 'UNKNOWN');
}
```

---

## Barrel Exports — Tipos Centralizados

Centralizar tipos para evitar importaciones dispersas y duplicadas:

```typescript
// src/types/index.ts  ← punto central de tipos compartidos
export type { User, UserRole, UserStatus }   from './user.types';
export type { Event, EventStatus }           from './event.types';
export type { PaginatedResponse, ApiResult } from './common.types';
export type { AppError }                     from '../errors/app-error';

// En cualquier archivo — importar desde el barrel
import type { User, PaginatedResponse, ApiResult } from '@/types';
```

---

## Common Validators

```typescript
// Strings
@IsString()
@IsNotEmpty()
@MinLength(3)
@MaxLength(100)

// Numbers
@IsInt()
@IsNumber()
@Min(0)
@Max(1000)

// Email / UUID
@IsEmail()
@IsUUID()

// Enum
@IsEnum(MyEnum)

// Array
@IsArray()
@ArrayMinSize(1)

// Nested
@ValidateNested()
@Type(() => NestedDto)

// Optional
@IsOptional()

// Date
@IsDateString()
@IsISO8601()
```

---

## Swagger Decorators

```typescript
// Required field
@ApiProperty({
  description: 'Descripción del campo',
  example: 'valor de ejemplo',
})

// Optional field
@ApiPropertyOptional({
  description: 'Campo opcional',
  example: 'valor opcional',
})

// Enum field
@ApiProperty({ enum: MyEnum, enumName: 'MyEnum' })

// Array field
@ApiProperty({ type: [ItemDto], description: 'Lista de items' })
```

---

## Commands

```bash
# Verificar tipado sin compilar
npx tsc --noEmit

# Verificar tipado en modo watch
npx tsc --noEmit --watch

# Compilar el proyecto
npm run build

# Linting con ESLint
npm run lint

# Linting con auto-fix
npm run lint -- --fix

# Detectar archivos con errores de tipo
npx tsc --noEmit 2>&1 | grep "error TS"
```

---

## Project Config

El proyecto usa:

- `tsconfig.json` con `strict: true`
- ES2023 target
- Path aliases: `@/*` → `src/*` (si configurado)
- Decorators habilitados (`experimentalDecorators: true`, `emitDecoratorMetadata: true`)

### Reglas ESLint recomendadas para calidad

```json
{
  "@typescript-eslint/no-explicit-any": "error",
  "@typescript-eslint/no-unsafe-assignment": "error",
  "@typescript-eslint/no-unnecessary-type-assertion": "error",
  "@typescript-eslint/prefer-nullish-coalescing": "warn",
  "@typescript-eslint/prefer-optional-chain": "warn",
  "@typescript-eslint/consistent-type-imports": "warn",
  "no-duplicate-imports": "error"
}
```
