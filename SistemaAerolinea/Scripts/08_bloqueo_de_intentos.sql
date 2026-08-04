-- ============================================================================
--  ALAS Honduras — Sistema de Reserva de Vuelos
--  Script 08: bloqueo temporal tras varios intentos fallidos
--  Base de datos: dbreserva_vuelos
-- ============================================================================
--  El bloqueo por intentos fallidos vivía en la ventana de inicio de sesión, y
--  ahí no protege de nada: cerrar y volver a abrir la ventana reiniciaba el
--  contador, y una segunda terminal ni siquiera se enteraba.
--
--  Al guardarlo en la cuenta, el castigo es de la cuenta y no de la pantalla:
--  vale para todas las terminales y sobrevive a cerrar el programa.
--
--    intentos_fallidos  cuántas veces seguidas se erró la contraseña
--    bloqueado_hasta    hasta cuándo no se acepta ni la contraseña correcta
--
--  Idempotente.
-- ============================================================================

USE dbreserva_vuelos;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- --------------------------------------------- contador de intentos -----
IF COL_LENGTH('dbo.usuario', 'intentos_fallidos') IS NULL
BEGIN
    ALTER TABLE dbo.usuario ADD intentos_fallidos INT NOT NULL DEFAULT 0;
END
GO

-- Momento hasta el que la cuenta no acepta intentos. NULL = sin bloqueo.
IF COL_LENGTH('dbo.usuario', 'bloqueado_hasta') IS NULL
BEGIN
    ALTER TABLE dbo.usuario ADD bloqueado_hasta DATETIME NULL;
END
GO

-- Las cuentas que ya existían arrancan limpias
UPDATE dbo.usuario
   SET intentos_fallidos = 0
 WHERE intentos_fallidos IS NULL;
GO

PRINT 'ALAS · 08_bloqueo_de_intentos.sql aplicado correctamente.';
GO
