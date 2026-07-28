-- ============================================================
-- Script 02 - Optimización profesional
-- Índices para consultas frecuentes + procedimientos almacenados
-- Base de datos: parqueadero
-- ============================================================
USE parqueadero;
GO

-- ---------- ÍNDICES ----------
-- Aceleran las búsquedas más frecuentes del sistema

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_movimiento_placa')
    CREATE NONCLUSTERED INDEX IX_movimiento_placa
        ON dbo.movimiento (placa) INCLUDE (fecha_salida);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_movimiento_fecha_salida')
    CREATE NONCLUSTERED INDEX IX_movimiento_fecha_salida
        ON dbo.movimiento (fecha_salida) INCLUDE (total, fecha_entrada);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_cliente_nombre')
    CREATE NONCLUSTERED INDEX IX_cliente_nombre ON dbo.cliente (nombre);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_vehiculo_placa')
    CREATE NONCLUSTERED INDEX IX_vehiculo_placa ON dbo.vehiculo (placa);
GO

-- ---------- PROCEDIMIENTOS ALMACENADOS ----------
-- Encapsulan la lógica de consulta en el servidor

CREATE OR ALTER PROCEDURE dbo.sp_registrar_entrada
    @placa       VARCHAR(10),
    @tipo        VARCHAR(20),
    @parqueadero NVARCHAR(5),
    @usuario     VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.movimiento (placa, tipo_vehiculo, codigo_parqueadero, usuario_registra)
    VALUES (UPPER(LTRIM(RTRIM(@placa))), @tipo, @parqueadero, @usuario);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_movimientos_abiertos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.movimiento_id, m.placa, m.tipo_vehiculo, m.codigo_parqueadero,
           m.fecha_entrada, t.valor_hora
    FROM dbo.movimiento m
    JOIN dbo.tarifa t ON t.tipo_vehiculo = m.tipo_vehiculo
    WHERE m.fecha_salida IS NULL
    ORDER BY m.fecha_entrada;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_movimientos_historial
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 100 movimiento_id, placa, tipo_vehiculo, codigo_parqueadero,
           fecha_entrada, fecha_salida, minutos, total, usuario_registra
    FROM dbo.movimiento
    WHERE fecha_salida IS NOT NULL
    ORDER BY fecha_salida DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ultimos_movimientos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10 placa, tipo_vehiculo, fecha_entrada, fecha_salida, total
    FROM dbo.movimiento
    ORDER BY movimiento_id DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_estadisticas_dashboard
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM dbo.movimiento WHERE fecha_salida IS NULL) AS vehiculos_dentro,
        (SELECT ISNULL(SUM(total), 0) FROM dbo.movimiento
         WHERE fecha_salida IS NOT NULL
           AND CONVERT(date, fecha_salida) = CONVERT(date, GETDATE())) AS ingresos_hoy,
        (SELECT COUNT(*) FROM dbo.cliente) AS total_clientes,
        (SELECT COUNT(*) FROM dbo.movimiento
         WHERE CONVERT(date, fecha_entrada) = CONVERT(date, GETDATE())) AS movimientos_hoy;
END
GO

PRINT 'Índices y procedimientos almacenados creados correctamente.';
GO
