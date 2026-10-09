-- Script: crear_base.sql
-- Spec:   RF-001
-- Fecha:  2026-10-09
-- Desc:   Crea la base GestionProyectos si no existe (ejecutar conectado a master) y verifica
--         que la collation sea case-insensitive (*_CI_*), requisito del modelo de datos.
--         No se versiona en VersionesEsquema: es previo a V001.
-- Uso:    sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -b -I -f 65001 -i db\sqlserver\crear_base.sql

SET NOCOUNT ON;

IF DB_ID(N'GestionProyectos') IS NULL
BEGIN
    -- Sin COLLATE explicito: se usa la collation por defecto de la instancia (spec: debe ser *_CI_AS).
    CREATE DATABASE GestionProyectos;
    PRINT N'Base GestionProyectos creada.';
END
ELSE
    PRINT N'Base GestionProyectos ya existe.';
GO

DECLARE @collation NVARCHAR(128) = CONVERT(NVARCHAR(128), DATABASEPROPERTYEX(N'GestionProyectos', 'Collation'));
IF @collation NOT LIKE N'%[_]CI[_]%'
    THROW 50001, N'La collation de GestionProyectos no es case-insensitive (*_CI_*). Revisar la instancia.', 1;
PRINT N'Collation de GestionProyectos: ' + @collation;
GO
