-- ============================================================
-- Script III Parcial - Sistema Parqueadero WPF
-- Crea las tablas de usuarios (login), tarifas y movimientos
-- Base de datos: parqueadero
-- ============================================================
USE parqueadero;
GO

-- ---------- TABLA USUARIO (login / registro / roles) ----------
IF OBJECT_ID('dbo.usuario', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.usuario (
        usuario_id        INT IDENTITY(1,1) PRIMARY KEY,
        nombre_completo   VARCHAR(80)  NOT NULL,
        email             VARCHAR(80)  NOT NULL UNIQUE,
        usuario           VARCHAR(30)  NOT NULL UNIQUE,
        contrasena_hash   VARCHAR(100) NOT NULL,
        rol               VARCHAR(20)  NOT NULL DEFAULT 'Operador',  -- Administrador / Operador
        pregunta_seguridad  VARCHAR(100) NULL,
        respuesta_seguridad VARCHAR(100) NULL,
        esta_activo       BIT          NOT NULL DEFAULT 1,
        intentos_fallidos INT          NOT NULL DEFAULT 0,
        fecha_creacion    DATETIME     NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ---------- TABLA TARIFA (valor por hora segun tipo de vehiculo) ----------
IF OBJECT_ID('dbo.tarifa', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tarifa (
        tipo_vehiculo VARCHAR(20)   NOT NULL PRIMARY KEY,
        valor_hora    DECIMAL(10,2) NOT NULL
    );
    INSERT INTO dbo.tarifa (tipo_vehiculo, valor_hora) VALUES
        ('carro',     30.00),
        ('moto',      15.00),
        ('camioneta', 40.00),
        ('bus',       50.00);
END
GO

-- ---------- TABLA MOVIMIENTO (entradas y salidas con cobro) ----------
IF OBJECT_ID('dbo.movimiento', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.movimiento (
        movimiento_id      INT IDENTITY(1,1) PRIMARY KEY,
        placa              VARCHAR(10)  NOT NULL,
        tipo_vehiculo      VARCHAR(20)  NOT NULL,
        codigo_parqueadero NVARCHAR(5)  NOT NULL,
        fecha_entrada      DATETIME     NOT NULL DEFAULT GETDATE(),
        fecha_salida       DATETIME     NULL,
        minutos            INT          NULL,
        total              DECIMAL(10,2) NULL,
        usuario_registra   VARCHAR(30)  NULL,
        CONSTRAINT FK_movimiento_parqueadero
            FOREIGN KEY (codigo_parqueadero) REFERENCES dbo.parqueadero (codigo_parqueadero),
        CONSTRAINT FK_movimiento_tarifa
            FOREIGN KEY (tipo_vehiculo) REFERENCES dbo.tarifa (tipo_vehiculo)
    );
END
GO

PRINT 'Tablas del sistema creadas correctamente.';
GO
