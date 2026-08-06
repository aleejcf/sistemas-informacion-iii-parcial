-- ============================================================================
--  ALEJANDRÍA — Sistema de Biblioteca
--  Script 04: cuentas del sistema y bitácora
--  Base de datos: db_biblioteca
-- ============================================================================
--  `usuario` guarda a quien OPERA el sistema (el personal de la biblioteca),
--  que no es lo mismo que `socio`, quien PIDE PRESTADO. En el script del
--  II Parcial la tabla `Usuarios` mezclaba ese único concepto; aquí son dos.
--
--  Las contraseñas se guardan con hash BCrypt generado por la aplicación; aquí
--  no se siembra ninguna cuenta porque T-SQL no puede calcular un hash BCrypt.
--  La regla de arranque la aplica AuthService: el PRIMER usuario que se registre
--  queda como Administrador y los siguientes como Bibliotecarios.
--
--  Idempotente.
-- ============================================================================

USE db_biblioteca;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------- usuario -----
IF OBJECT_ID('dbo.usuario', 'U') IS NULL
CREATE TABLE dbo.usuario (
    usuario_id              INT          IDENTITY(1,1) PRIMARY KEY,
    nombre_completo         VARCHAR(80)  NOT NULL,
    email                   VARCHAR(80)  NOT NULL UNIQUE,
    usuario                 VARCHAR(30)  NOT NULL UNIQUE,
    contrasena_hash         VARCHAR(100) NOT NULL,
    rol                     VARCHAR(20)  NOT NULL DEFAULT 'Bibliotecario',
    pregunta_seguridad      VARCHAR(100) NULL,
    respuesta_seguridad     VARCHAR(100) NULL,
    codigo_recuperacion     VARCHAR(6)   NULL,
    fecha_expiracion_codigo DATETIME     NULL,
    esta_activo             BIT          NOT NULL DEFAULT 1,
    debe_cambiar_contrasena BIT          NOT NULL DEFAULT 0,
    ultimo_acceso           DATETIME     NULL,
    fecha_creacion          DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_usuario_rol CHECK (rol IN ('Administrador', 'Bibliotecario'))
);
GO

-- ------------------------------------------------------------ bitacora -----
-- Registro de auditoría en la base de datos (a diferencia del log en archivo):
-- consultable desde la página "Bitácora" y con quién hizo qué sobre qué.
IF OBJECT_ID('dbo.bitacora', 'U') IS NULL
CREATE TABLE dbo.bitacora (
    idbitacora INT          IDENTITY(1,1) PRIMARY KEY,
    usuario    VARCHAR(30)  NOT NULL,
    accion     VARCHAR(40)  NOT NULL,
    entidad    VARCHAR(30)  NULL,
    detalle    VARCHAR(200) NULL,
    exito      BIT          NOT NULL DEFAULT 1,
    equipo     VARCHAR(50)  NULL,
    fecha      DATETIME     NOT NULL DEFAULT GETDATE()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_bitacora_fecha')
    CREATE NONCLUSTERED INDEX IX_bitacora_fecha
        ON dbo.bitacora (fecha DESC) INCLUDE (usuario, accion, exito);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_bitacora_usuario')
    CREATE NONCLUSTERED INDEX IX_bitacora_usuario
        ON dbo.bitacora (usuario, fecha DESC);
GO

-- Consulta de la bitácora con filtros opcionales
CREATE OR ALTER PROCEDURE dbo.sp_bitacora
    @usuario VARCHAR(30) = NULL,
    @accion  VARCHAR(40) = NULL,
    @desde   DATE        = NULL,
    @top     INT         = 300
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@top) idbitacora, usuario, accion, entidad, detalle, exito, equipo, fecha
    FROM dbo.bitacora
    WHERE (@usuario IS NULL OR usuario = @usuario)
      AND (@accion  IS NULL OR accion  = @accion)
      AND (@desde   IS NULL OR fecha  >= @desde)
    ORDER BY fecha DESC;
END
GO

PRINT 'ALEJANDRÍA · 04_sistema_login.sql aplicado correctamente.';
GO
