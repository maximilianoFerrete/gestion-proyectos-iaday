-- Script: S002__persona_administrador.sql
-- Spec:   RF-001
-- Fecha:  2026-10-09
-- Desc:   Semilla de la persona Administrador (EsAdmin = 1), necesaria para RN-06 (al menos un
--         administrador activo). Idempotente: inserta solo si no existe por Email.

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.Personas WHERE Email = N'admin@gestionproyectos.local')
        INSERT INTO dbo.Personas (Nombre, Email, EsAdmin, Activo)
        VALUES (N'Administrador', N'admin@gestionproyectos.local', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.VersionesEsquema WHERE Version = N'S002')
        INSERT INTO dbo.VersionesEsquema (Version, Descripcion)
        VALUES (N'S002', N'seed_persona_administrador');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
