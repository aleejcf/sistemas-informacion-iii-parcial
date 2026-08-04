-- ============================================================================
--  ALAS Honduras — Sistema de Reserva de Vuelos
--  Script 06: portal del pasajero
--  Base de datos: dbreserva_vuelos
-- ============================================================================
--  Hasta aquí el sistema era solo para el personal de la aerolínea. Este script
--  abre la puerta al pasajero: le permite tener cuenta propia y entrar a ver
--  únicamente lo suyo.
--
--  La idea de fondo: `usuario` es QUIEN ENTRA al sistema y `pasajero` es QUIEN
--  VIAJA. Son cosas distintas y siguen en tablas distintas — un agente entra pero
--  no viaja, y un pasajero puede viajar sin tener cuenta (lo registra el agente
--  en el mostrador). Lo que se agrega es el puente entre las dos.
--
--  Idempotente.
-- ============================================================================

USE dbreserva_vuelos;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ---------------------------------------------- el puente usuario→ficha -----
IF COL_LENGTH('dbo.usuario', 'idpasajero') IS NULL
BEGIN
    ALTER TABLE dbo.usuario ADD idpasajero CHAR(8) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_usuario_pasajero')
BEGIN
    ALTER TABLE dbo.usuario
        ADD CONSTRAINT FK_usuario_pasajero
            FOREIGN KEY (idpasajero) REFERENCES dbo.pasajero (idpasajero);
END
GO

-- Una persona no puede tener dos cuentas: si no, "mis vuelos" mostraría
-- lo mismo en las dos y cualquiera de ellas podría cancelar por la otra.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_usuario_pasajero')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_usuario_pasajero
        ON dbo.usuario (idpasajero)
        WHERE idpasajero IS NOT NULL;
END
GO

-- ------------------------------------------------------- el tercer rol -----
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_usuario_rol')
BEGIN
    ALTER TABLE dbo.usuario DROP CONSTRAINT CK_usuario_rol;
END
GO

ALTER TABLE dbo.usuario
    ADD CONSTRAINT CK_usuario_rol CHECK (rol IN ('Administrador', 'Agente', 'Pasajero'));
GO

-- Regla que la base de datos hace cumplir por su cuenta: una cuenta de pasajero
-- SIEMPRE apunta a su ficha, y una del personal NUNCA lo hace. Sin esto, un
-- error de programación podría dejar un pasajero sin datos de viajero, o un
-- administrador colgando de la ficha de otra persona.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_usuario_pasajero_coherente')
BEGIN
    ALTER TABLE dbo.usuario DROP CONSTRAINT CK_usuario_pasajero_coherente;
END
GO

ALTER TABLE dbo.usuario
    ADD CONSTRAINT CK_usuario_pasajero_coherente CHECK (
        (rol = 'Pasajero'  AND idpasajero IS NOT NULL) OR
        (rol <> 'Pasajero' AND idpasajero IS NULL)
    );
GO

-- ------------------------------------------------------- consultas -----
-- Las reservas de un pasajero concreto. Existe como procedimiento para que la
-- restricción viva en el servidor: la aplicación no puede "olvidarse" del filtro.
CREATE OR ALTER PROCEDURE dbo.sp_reservas_del_pasajero
    @idpasajero CHAR(8)
AS
BEGIN
    SET NOCOUNT ON;

    -- Se incluyen tanto las reservas donde es titular como aquellas en las que
    -- alguien más lo llevó de acompañante: en ambas tiene un asiento que le toca.
    SELECT r.*
    FROM dbo.v_reserva_resumen r
    WHERE r.idpasajero = @idpasajero
       OR EXISTS (SELECT 1 FROM dbo.boleto b
                  WHERE b.idreserva = r.idreserva
                    AND b.idpasajero = @idpasajero
                    AND b.estado <> 'Cancelado')
    ORDER BY r.fecha DESC;
END
GO

-- Los próximos vuelos de un pasajero, para el encabezado de "Mis vuelos"
CREATE OR ALTER PROCEDURE dbo.sp_proximos_vuelos_del_pasajero
    @idpasajero CHAR(8)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 5 b.idboleto, b.codigo_reserva, b.codigo_vuelo, b.fecha_salida,
           b.iata_origen, b.ciudad_origen, b.iata_destino, b.ciudad_destino,
           b.asiento, b.clase, b.estado, b.puerta, b.estado_vuelo
    FROM dbo.v_boleto_detalle b
    WHERE b.idpasajero = @idpasajero
      AND b.estado <> 'Cancelado'
      AND b.fecha_salida >= GETDATE()
    ORDER BY b.fecha_salida;
END
GO

PRINT 'ALAS · 06_portal_pasajero.sql aplicado correctamente.';
GO
