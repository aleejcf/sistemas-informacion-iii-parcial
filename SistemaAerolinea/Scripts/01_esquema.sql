-- ============================================================================
--  ALAS Honduras — Sistema de Reserva de Vuelos
--  Script 01: esquema de la base de datos
--  Base de datos: dbreserva_vuelos
--  Autor: Alejandro Calderón — III Parcial
-- ============================================================================
--  Este esquema parte del script "DB RESERVA.sql" del II Parcial y lo lleva
--  a un modelo que un sistema real puede operar. Se conservan los nombres
--  originales de tablas y columnas; los cambios se explican donde ocurren.
--
--  Cambio de fondo: en el script original la tabla `vuelo` guardaba UN asiento,
--  UNA reserva y UNA tarifa por fila — o sea, cada fila era en realidad un
--  boleto, no un vuelo. Aquí se separan las dos ideas:
--
--      vuelo   = el trayecto programado (avión, origen, destino, horarios)
--      boleto  = un asiento de ese vuelo vendido a un pasajero
--
--  Esa separación es la que permite vender varios asientos del mismo vuelo,
--  calcular ocupación y evitar la doble venta de un asiento.
--
--  El script es idempotente: se puede volver a ejecutar sin error.
-- ============================================================================

IF DB_ID('dbreserva_vuelos') IS NULL
    CREATE DATABASE dbreserva_vuelos;
GO

USE dbreserva_vuelos;
GO

-- El índice único filtrado de dbo.boleto exige estas opciones. SqlClient (la
-- aplicación) ya las trae encendidas; sqlcmd no, por eso se fijan aquí.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
--  CATÁLOGOS
-- ============================================================================

-- TABLA: pais — igual que en el II Parcial
IF OBJECT_ID('dbo.pais', 'U') IS NULL
CREATE TABLE dbo.pais (
    idpais      CHAR(4)     NOT NULL PRIMARY KEY,
    nombre_pais VARCHAR(50) NOT NULL UNIQUE
);
GO

-- TABLA: aeropuerto — se agregan `ciudad` e `iata` (el código de 3 letras que
-- se imprime en el pase de abordar: TGU, SAP, MIA…)
IF OBJECT_ID('dbo.aeropuerto', 'U') IS NULL
CREATE TABLE dbo.aeropuerto (
    idaeropuerto CHAR(5)     NOT NULL PRIMARY KEY,
    nombre       VARCHAR(50) NOT NULL,
    ciudad       VARCHAR(40) NOT NULL,
    iata         CHAR(3)     NOT NULL UNIQUE,
    idpais       CHAR(4)     NOT NULL,
    CONSTRAINT FK_aeropuerto_pais FOREIGN KEY (idpais) REFERENCES dbo.pais (idpais)
);
GO

-- TABLA: aerolinea — se agrega `codigo` (prefijo de 2 letras del número de vuelo)
IF OBJECT_ID('dbo.aerolinea', 'U') IS NULL
CREATE TABLE dbo.aerolinea (
    idaerolinea INT         NOT NULL PRIMARY KEY,
    codigo      CHAR(2)     NOT NULL UNIQUE,
    rtn         VARCHAR(14) NOT NULL UNIQUE,
    nombre_aero VARCHAR(40) NOT NULL
);
GO

-- TABLA: avion — se agrega `asientos_por_fila` para poder dibujar el mapa de
-- asientos con la configuración real de cada aeronave (4, 6 u 8 por fila)
IF OBJECT_ID('dbo.avion', 'U') IS NULL
CREATE TABLE dbo.avion (
    idavion             CHAR(5)     NOT NULL PRIMARY KEY,
    idaerolinea         INT         NOT NULL,
    fabricante          VARCHAR(40) NULL,
    tipo                VARCHAR(30) NOT NULL,
    capacidad_pasajeros INT         NOT NULL,
    asientos_por_fila   INT         NOT NULL DEFAULT 6,
    CONSTRAINT FK_avion_aerolinea FOREIGN KEY (idaerolinea) REFERENCES dbo.aerolinea (idaerolinea),
    CONSTRAINT CK_avion_capacidad CHECK (capacidad_pasajeros BETWEEN 1 AND 600),
    CONSTRAINT CK_avion_fila      CHECK (asientos_por_fila IN (4, 6, 8))
);
GO

-- TABLA: asiento — antes era una lista suelta de 20 asientos compartida por
-- todos los aviones. Ahora cada asiento pertenece a un avión concreto y trae
-- su clase, que es lo que determina la tarifa que se le cobra.
IF OBJECT_ID('dbo.asiento', 'U') IS NULL
CREATE TABLE dbo.asiento (
    idasiento INT         IDENTITY(1,1) PRIMARY KEY,
    idavion   CHAR(5)     NOT NULL,
    fila      INT         NOT NULL,
    letra     CHAR(1)     NOT NULL,
    clase     VARCHAR(20) NOT NULL,
    CONSTRAINT FK_asiento_avion FOREIGN KEY (idavion) REFERENCES dbo.avion (idavion),
    CONSTRAINT UQ_asiento       UNIQUE (idavion, fila, letra)
);
GO

-- TABLA: tarifa — en el original guardaba 20 precios sueltos sin relación con
-- la ruta. Aquí una tarifa describe una CLASE: cuánto multiplica al precio base
-- del vuelo, qué impuesto lleva y cuánto equipaje incluye. Así un mismo vuelo
-- cobra distinto en Económica, Ejecutiva y Primera Clase, y cambiar el precio
-- de una ruta es cambiar un solo número.
IF OBJECT_ID('dbo.tarifa', 'U') IS NULL
CREATE TABLE dbo.tarifa (
    idtarifa             INT           NOT NULL PRIMARY KEY,
    clase                VARCHAR(20)   NOT NULL UNIQUE,
    multiplicador        DECIMAL(5,2)  NOT NULL,
    impuesto             DECIMAL(5,4)  NOT NULL,
    equipaje_incluido_kg INT           NOT NULL DEFAULT 20,
    CONSTRAINT CK_tarifa_multiplicador CHECK (multiplicador > 0),
    CONSTRAINT CK_tarifa_impuesto      CHECK (impuesto >= 0 AND impuesto < 1)
);
GO

-- TABLA: metodo_pago — nueva; en el original el medio de pago no existía
IF OBJECT_ID('dbo.metodo_pago', 'U') IS NULL
CREATE TABLE dbo.metodo_pago (
    idmetodopago INT         NOT NULL PRIMARY KEY,
    nombre       VARCHAR(30) NOT NULL UNIQUE
);
GO

-- ============================================================================
--  PASAJEROS
-- ============================================================================

-- TABLA: pasajero — igual que en el II Parcial, sin la columna `clave`:
-- las contraseñas del sistema viven en dbo.usuario con hash BCrypt
-- (script 04), no en texto plano junto a los datos del pasajero.
IF OBJECT_ID('dbo.pasajero', 'U') IS NULL
CREATE TABLE dbo.pasajero (
    idpasajero       CHAR(8)     NOT NULL PRIMARY KEY,
    nombre_p         VARCHAR(30) NOT NULL,
    apaterno         VARCHAR(30) NOT NULL,
    amaterno         VARCHAR(30) NULL,
    tipo_documento   VARCHAR(20) NOT NULL,
    num_documento    VARCHAR(20) NOT NULL UNIQUE,
    fecha_nacimiento DATE        NOT NULL,
    idpais           CHAR(4)     NOT NULL,
    telefono         VARCHAR(15) NULL,
    email            VARCHAR(50) NOT NULL UNIQUE,
    fecha_registro   DATETIME    NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_pasajero_pais FOREIGN KEY (idpais) REFERENCES dbo.pais (idpais),
    CONSTRAINT CK_pasajero_nacimiento CHECK (fecha_nacimiento < GETDATE())
);
GO

-- ============================================================================
--  OPERACIÓN: VUELOS, RESERVAS Y BOLETOS
-- ============================================================================

-- TABLA: vuelo — ahora sí es un vuelo: una aeronave que sale de un aeropuerto
-- y llega a otro en una fecha y hora determinadas.
IF OBJECT_ID('dbo.vuelo', 'U') IS NULL
CREATE TABLE dbo.vuelo (
    idvuelo              INT           IDENTITY(1,1) PRIMARY KEY,
    codigo_vuelo         VARCHAR(12)   NOT NULL UNIQUE,
    idaerolinea          INT           NOT NULL,
    idavion              CHAR(5)       NOT NULL,
    idaeropuerto_origen  CHAR(5)       NOT NULL,
    idaeropuerto_destino CHAR(5)       NOT NULL,
    fecha_salida         DATETIME      NOT NULL,
    fecha_llegada        DATETIME      NOT NULL,
    precio_base          DECIMAL(10,2) NOT NULL,
    estado               VARCHAR(20)   NOT NULL DEFAULT 'Programado',
    puerta               VARCHAR(5)    NULL,
    CONSTRAINT FK_vuelo_aerolinea FOREIGN KEY (idaerolinea)          REFERENCES dbo.aerolinea (idaerolinea),
    CONSTRAINT FK_vuelo_avion     FOREIGN KEY (idavion)              REFERENCES dbo.avion (idavion),
    CONSTRAINT FK_vuelo_origen    FOREIGN KEY (idaeropuerto_origen)  REFERENCES dbo.aeropuerto (idaeropuerto),
    CONSTRAINT FK_vuelo_destino   FOREIGN KEY (idaeropuerto_destino) REFERENCES dbo.aeropuerto (idaeropuerto),
    CONSTRAINT CK_vuelo_estado    CHECK (estado IN ('Programado','Abordando','En vuelo','Aterrizado','Retrasado','Cancelado')),
    CONSTRAINT CK_vuelo_horarios  CHECK (fecha_llegada > fecha_salida),
    CONSTRAINT CK_vuelo_ruta      CHECK (idaeropuerto_origen <> idaeropuerto_destino),
    CONSTRAINT CK_vuelo_precio    CHECK (precio_base > 0)
);
GO

-- TABLA: reserva — conserva costo, fecha y observación del original, y agrega
-- el `codigo_reserva`: el localizador de 6 caracteres que toda aerolínea le da
-- al pasajero (el PNR). El desglose se guarda porque una factura no puede
-- cambiar de monto si mañana cambia el precio de la ruta.
IF OBJECT_ID('dbo.reserva', 'U') IS NULL
CREATE TABLE dbo.reserva (
    idreserva        INT           IDENTITY(1,1) PRIMARY KEY,
    codigo_reserva   CHAR(6)       NOT NULL UNIQUE,
    idpasajero       CHAR(8)       NOT NULL,     -- titular de la reserva
    fecha            DATETIME      NOT NULL DEFAULT GETDATE(),
    estado           VARCHAR(20)   NOT NULL DEFAULT 'Pendiente de pago',
    subtotal         DECIMAL(10,2) NOT NULL DEFAULT 0,
    impuesto         DECIMAL(10,2) NOT NULL DEFAULT 0,
    costo            DECIMAL(10,2) NOT NULL DEFAULT 0,   -- total a pagar
    observacion      VARCHAR(200)  NULL,
    usuario_registra VARCHAR(30)   NULL,
    CONSTRAINT FK_reserva_pasajero FOREIGN KEY (idpasajero) REFERENCES dbo.pasajero (idpasajero),
    CONSTRAINT CK_reserva_estado   CHECK (estado IN ('Pendiente de pago','Confirmada','Cancelada')),
    CONSTRAINT CK_reserva_montos   CHECK (subtotal >= 0 AND impuesto >= 0 AND costo >= 0)
);
GO

-- TABLA: boleto — nueva, y es el corazón del sistema. Un boleto es UN asiento
-- de UN vuelo vendido a UN pasajero dentro de una reserva.
IF OBJECT_ID('dbo.boleto', 'U') IS NULL
CREATE TABLE dbo.boleto (
    idboleto      INT           IDENTITY(1,1) PRIMARY KEY,
    idreserva     INT           NOT NULL,
    idvuelo       INT           NOT NULL,
    idasiento     INT           NOT NULL,
    idpasajero    CHAR(8)       NOT NULL,
    idtarifa      INT           NOT NULL,
    precio        DECIMAL(10,2) NOT NULL,
    impuesto      DECIMAL(10,2) NOT NULL,
    total         DECIMAL(10,2) NOT NULL,
    estado        VARCHAR(20)   NOT NULL DEFAULT 'Emitido',
    fecha_checkin DATETIME      NULL,
    CONSTRAINT FK_boleto_reserva  FOREIGN KEY (idreserva)  REFERENCES dbo.reserva (idreserva),
    CONSTRAINT FK_boleto_vuelo    FOREIGN KEY (idvuelo)    REFERENCES dbo.vuelo (idvuelo),
    CONSTRAINT FK_boleto_asiento  FOREIGN KEY (idasiento)  REFERENCES dbo.asiento (idasiento),
    CONSTRAINT FK_boleto_pasajero FOREIGN KEY (idpasajero) REFERENCES dbo.pasajero (idpasajero),
    CONSTRAINT FK_boleto_tarifa   FOREIGN KEY (idtarifa)   REFERENCES dbo.tarifa (idtarifa),
    CONSTRAINT CK_boleto_estado   CHECK (estado IN ('Emitido','Check-in','Abordado','Cancelado'))
);
GO

-- Regla de oro del negocio, aplicada por la base de datos y no por la interfaz:
-- un asiento de un vuelo no se puede vender dos veces. El índice es FILTRADO
-- (WHERE estado <> 'Cancelado') para que al cancelar un boleto el asiento
-- vuelva a quedar libre sin tener que borrar el histórico.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_boleto_vuelo_asiento')
    CREATE UNIQUE NONCLUSTERED INDEX UQ_boleto_vuelo_asiento
        ON dbo.boleto (idvuelo, idasiento)
        WHERE estado <> 'Cancelado';
GO

-- TABLA: pago — conserva las columnas del original y agrega el método de pago
IF OBJECT_ID('dbo.pago', 'U') IS NULL
CREATE TABLE dbo.pago (
    idpago           INT           IDENTITY(1,1) PRIMARY KEY,
    idreserva        INT           NOT NULL,
    idpasajero       CHAR(8)       NOT NULL,
    idmetodopago     INT           NOT NULL,
    fecha            DATETIME      NOT NULL DEFAULT GETDATE(),
    monto            DECIMAL(10,2) NOT NULL,
    impuesto         DECIMAL(10,2) NOT NULL,
    tipo_comprobante VARCHAR(20)   NOT NULL,
    num_comprobante  VARCHAR(15)   NOT NULL UNIQUE,
    usuario_registra VARCHAR(30)   NULL,
    CONSTRAINT FK_pago_reserva  FOREIGN KEY (idreserva)    REFERENCES dbo.reserva (idreserva),
    CONSTRAINT FK_pago_pasajero FOREIGN KEY (idpasajero)   REFERENCES dbo.pasajero (idpasajero),
    CONSTRAINT FK_pago_metodo   FOREIGN KEY (idmetodopago) REFERENCES dbo.metodo_pago (idmetodopago),
    CONSTRAINT CK_pago_monto    CHECK (monto > 0),
    CONSTRAINT CK_pago_tipo     CHECK (tipo_comprobante IN ('Factura','Recibo'))
);
GO

PRINT 'ALAS · 01_esquema.sql aplicado correctamente.';
GO
