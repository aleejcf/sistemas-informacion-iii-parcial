-- ============================================================================
--  ALAS Honduras — Sistema de Reserva de Vuelos
--  Script 07: pago desde el portal e identidad de Google
--  Base de datos: dbreserva_vuelos
-- ============================================================================
--  Dos cosas:
--
--  1. El pasajero ahora paga desde el portal, como en cualquier aerolínea de hoy.
--     Se guarda la `referencia` que devuelve la pasarela de pago — el número que
--     el pasajero cita cuando reclama un cobro y por el que se rastrea la
--     transacción del lado del banco.
--
--  2. Una cuenta puede quedar ligada a una identidad de Google. Se guarda el
--     `sub` de Google (su identificador interno, que nunca cambia) y NO el correo:
--     una persona puede cambiar de correo en Google y seguir siendo la misma.
--
--  Idempotente.
-- ============================================================================

USE dbreserva_vuelos;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------- referencia de la pasarela -----
IF COL_LENGTH('dbo.pago', 'referencia') IS NULL
BEGIN
    ALTER TABLE dbo.pago ADD referencia VARCHAR(40) NULL;
END
GO

-- Deja constancia de por dónde entró el cobro: mostrador o portal del pasajero
IF COL_LENGTH('dbo.pago', 'canal') IS NULL
BEGIN
    ALTER TABLE dbo.pago ADD canal VARCHAR(20) NOT NULL DEFAULT 'Mostrador';
END
GO

-- ----------------------------------------------- identidad de Google -----
IF COL_LENGTH('dbo.usuario', 'google_id') IS NULL
BEGIN
    ALTER TABLE dbo.usuario ADD google_id VARCHAR(64) NULL;
END
GO

-- Una identidad de Google pertenece a una sola cuenta del sistema
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_usuario_google')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_usuario_google
        ON dbo.usuario (google_id)
        WHERE google_id IS NOT NULL;
END
GO

-- ------------------------------------------------------ consultas -----
-- Reservas del pasajero que todavía deben dinero: es lo que el portal le
-- muestra como "pendiente de pago".
CREATE OR ALTER PROCEDURE dbo.sp_saldos_del_pasajero
    @idpasajero CHAR(8)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.idreserva, r.codigo_reserva, r.fecha, r.boletos,
           r.costo, r.pagado, r.saldo
    FROM dbo.v_reserva_resumen r
    WHERE r.estado <> 'Cancelada'
      AND r.costo - r.pagado > 0
      AND (r.idpasajero = @idpasajero
           OR EXISTS (SELECT 1 FROM dbo.boleto b
                      WHERE b.idreserva = r.idreserva
                        AND b.idpasajero = @idpasajero
                        AND b.estado <> 'Cancelado'))
    ORDER BY r.fecha DESC;
END
GO

-- Cuánto se cobró por cada canal, para el panel de control
CREATE OR ALTER PROCEDURE dbo.sp_ingresos_por_canal
    @dias INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    SELECT canal,
           COUNT(*)               AS cobros,
           ISNULL(SUM(monto), 0)  AS total
    FROM dbo.pago
    WHERE fecha >= DATEADD(DAY, -@dias, GETDATE())
    GROUP BY canal
    ORDER BY total DESC;
END
GO

PRINT 'ALAS · 07_pagos_portal_y_google.sql aplicado correctamente.';
GO
