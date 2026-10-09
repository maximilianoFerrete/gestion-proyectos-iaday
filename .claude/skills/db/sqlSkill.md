---
name: sql
description: >
  SQL moderno con CTEs, window functions, índices y buenas prácticas de rendimiento.
  Trigger: escribir query SQL, optimizar query, crear índice, CTE, window function, transacción SQL, query PostgreSQL
license: MIT
metadata:
  author: flock-engineering
  version: '1.0'
  scope: [root]
  auto_invoke:
    - 'Escribir query SQL'
    - 'Optimizar query'
    - 'Crear índice SQL'
    - 'CTE SQL'
    - 'Window function'
    - 'Transacción SQL'
    - 'Query PostgreSQL'
    - 'Agregar paginación SQL'
allowed-tools: Read, Edit, Write, Glob, Grep
---

# SQL Skill

## Critical Rules

### ALWAYS

- Usar **CTEs** (`WITH`) para queries complejas en vez de subqueries anidadas
- Formatear SQL con keywords en MAYÚSCULAS y columnas/tablas en minúsculas
- Agregar índices en columnas usadas en `WHERE`, `JOIN` y `ORDER BY` frecuentes
- Usar paginación con `LIMIT` + `OFFSET` o cursor-based para listas grandes
- Wrappear operaciones multi-tabla en **transacciones** explícitas
- Usar `RETURNING` en `INSERT`/`UPDATE`/`DELETE` para evitar re-queries
- Nombrar índices descriptivamente: `idx_{tabla}_{columna(s)}`
- Preferir `EXISTS` sobre `COUNT(*)` para verificar existencia
- Usar `COALESCE` para valores por defecto en nullables
- Filtrar por `deleted_at IS NULL` en soft-delete siempre

### NEVER

- Usar `SELECT *` en producción — listar columnas explícitamente
- Hacer N+1 queries — resolver con JOINs o batch loads
- Concatenar strings para construir queries — usar parámetros (`$1`, `?`)
- Olvidar índices en FKs — siempre indexar claves foráneas
- Usar `OFFSET` alto para paginación de listas muy grandes — usar cursor
- Dejar transacciones abiertas sin commit/rollback
- Usar `DISTINCT` para ocultar un JOIN mal hecho

---

## Query Base con CTE

```sql
-- ✅ CTE para legibilidad y reutilización
WITH active_users AS (
  SELECT
    u.id,
    u.name,
    u.email,
    u.role,
    u.created_at
  FROM users u
  WHERE
    u.deleted_at IS NULL
    AND u.is_active = true
),
user_order_stats AS (
  SELECT
    o.user_id,
    COUNT(*)        AS total_orders,
    SUM(o.total)    AS lifetime_value,
    MAX(o.created_at) AS last_order_at
  FROM orders o
  WHERE o.status != 'CANCELLED'
  GROUP BY o.user_id
)
SELECT
  au.id,
  au.name,
  au.email,
  COALESCE(uos.total_orders, 0)    AS total_orders,
  COALESCE(uos.lifetime_value, 0)  AS lifetime_value,
  uos.last_order_at
FROM active_users au
LEFT JOIN user_order_stats uos ON uos.user_id = au.id
ORDER BY uos.lifetime_value DESC NULLS LAST;
```

---

## Window Functions

```sql
-- ROW_NUMBER: ranking por partición (sin duplicados)
SELECT
  id,
  name,
  department,
  salary,
  ROW_NUMBER() OVER (
    PARTITION BY department
    ORDER BY salary DESC
  ) AS rank_in_dept
FROM employees;

-- RANK / DENSE_RANK: permite empates
SELECT
  name,
  salary,
  RANK()       OVER (ORDER BY salary DESC) AS rank_with_gaps,
  DENSE_RANK() OVER (ORDER BY salary DESC) AS rank_no_gaps
FROM employees;

-- LAG / LEAD: comparar con fila anterior/siguiente
SELECT
  user_id,
  amount,
  created_at,
  LAG(amount)  OVER (PARTITION BY user_id ORDER BY created_at) AS prev_amount,
  LEAD(amount) OVER (PARTITION BY user_id ORDER BY created_at) AS next_amount,
  amount - LAG(amount) OVER (PARTITION BY user_id ORDER BY created_at) AS delta
FROM transactions;

-- SUM acumulativa
SELECT
  created_at::date AS day,
  daily_revenue,
  SUM(daily_revenue) OVER (ORDER BY created_at::date) AS cumulative_revenue
FROM daily_stats;
```

---

## Paginación

```sql
-- Cursor-based (recomendado para listas grandes)
-- Primera página
SELECT id, name, created_at
FROM users
WHERE deleted_at IS NULL
ORDER BY created_at DESC, id DESC
LIMIT 20;

-- Página siguiente (usando el último cursor)
SELECT id, name, created_at
FROM users
WHERE
  deleted_at IS NULL
  AND (created_at, id) < (:last_created_at, :last_id)  -- cursor compuesto
ORDER BY created_at DESC, id DESC
LIMIT 20;

-- OFFSET (solo para listas pequeñas / búsquedas con total count)
SELECT
  id, name, email
FROM users
WHERE deleted_at IS NULL
ORDER BY name ASC
LIMIT  :page_size
OFFSET (:page - 1) * :page_size;

-- Total para paginación con OFFSET
SELECT COUNT(*) AS total
FROM users
WHERE deleted_at IS NULL;
```

---

## Índices

```sql
-- Índice simple (columna de búsqueda frecuente)
CREATE INDEX idx_users_email ON users(email);

-- Índice compuesto (orden importa: columnas de mayor selectividad primero)
CREATE INDEX idx_orders_user_status ON orders(user_id, status);

-- Índice parcial (solo filas relevantes → más pequeño y rápido)
CREATE INDEX idx_users_active ON users(email)
WHERE deleted_at IS NULL AND is_active = true;

-- Índice para búsqueda de texto (PostgreSQL)
CREATE INDEX idx_products_name_search ON products USING gin(to_tsvector('spanish', name));

-- Verificar uso de índices
EXPLAIN ANALYZE
SELECT * FROM users WHERE email = 'test@flock.com';
```

---

## Transacciones

```sql
-- Patrón básico
BEGIN;

INSERT INTO orders (user_id, total, status)
VALUES ($1, $2, 'PENDING')
RETURNING id INTO _order_id;

INSERT INTO order_items (order_id, product_id, quantity, price)
SELECT _order_id, product_id, quantity, price
FROM unnest($3::order_item_input[]);

UPDATE inventory
SET reserved = reserved + item.quantity
FROM (SELECT product_id, quantity FROM order_items WHERE order_id = _order_id) item
WHERE inventory.product_id = item.product_id;

COMMIT;

-- Con manejo de error (en función PL/pgSQL)
CREATE OR REPLACE FUNCTION create_order(
  p_user_id   UUID,
  p_items     JSONB
) RETURNS UUID AS $$
DECLARE
  v_order_id UUID;
BEGIN
  INSERT INTO orders (user_id, status)
  VALUES (p_user_id, 'PENDING')
  RETURNING id INTO v_order_id;

  -- ... más operaciones ...

  RETURN v_order_id;
EXCEPTION
  WHEN OTHERS THEN
    RAISE; -- re-lanza, el caller hace ROLLBACK
END;
$$ LANGUAGE plpgsql;
```

---

## Upsert (INSERT ON CONFLICT)

```sql
-- Insertar o actualizar si ya existe
INSERT INTO user_preferences (user_id, key, value, updated_at)
VALUES ($1, $2, $3, NOW())
ON CONFLICT (user_id, key)
DO UPDATE SET
  value      = EXCLUDED.value,
  updated_at = EXCLUDED.updated_at
RETURNING *;

-- Insertar solo si no existe (ignorar conflicto)
INSERT INTO user_roles (user_id, role)
VALUES ($1, $2)
ON CONFLICT (user_id, role) DO NOTHING;
```

---

## RETURNING — Evitar Re-queries

```sql
-- ✅ Obtener el registro creado en el mismo INSERT
INSERT INTO users (name, email, role)
VALUES ($1, $2, $3)
RETURNING id, name, email, created_at;

-- ✅ Obtener el registro actualizado
UPDATE users
SET
  name       = $1,
  updated_at = NOW()
WHERE id = $2
RETURNING id, name, email, updated_at;

-- ✅ Soft delete con RETURNING
UPDATE users
SET deleted_at = NOW()
WHERE id = $1
RETURNING id, name;
```

---

## Búsqueda de Texto (PostgreSQL)

```sql
-- Full text search
SELECT id, name, description
FROM products
WHERE to_tsvector('spanish', name || ' ' || COALESCE(description, ''))
    @@ plainto_tsquery('spanish', $1)
ORDER BY ts_rank(
  to_tsvector('spanish', name || ' ' || COALESCE(description, '')),
  plainto_tsquery('spanish', $1)
) DESC;

-- ILIKE para búsqueda simple (sin índice FTS)
SELECT id, name FROM products
WHERE name ILIKE '%' || $1 || '%';
```

---

## Diagnóstico de Performance

```sql
-- Ver queries lentas
SELECT query, calls, mean_exec_time, total_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 20;

-- Ver índices no usados
SELECT schemaname, tablename, indexname, idx_scan
FROM pg_stat_user_indexes
WHERE idx_scan = 0
ORDER BY schemaname, tablename;

-- Ver tablas con más sequential scans (candidatos a índice)
SELECT relname, seq_scan, idx_scan
FROM pg_stat_user_tables
ORDER BY seq_scan DESC;

-- Analizar plan de ejecución
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT ...;
```
