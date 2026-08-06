-- ============================================================================
--  ALAS Honduras — Sistema de Reserva de Vuelos
--  Script 09: recuperación de cuenta segura
--  Base de datos: dbreserva_vuelos
-- ============================================================================
--  La recuperación anterior tenía un agujero que se llevaba cualquier cuenta por
--  delante: la pantalla generaba el código y LO MOSTRABA. Bastaba con saber el
--  correo de alguien para pedirlo, leerlo y cambiarle la contraseña.
--
--  Se sustituye por dos caminos que sí verifican quién pide el cambio:
--
--   1. CÓDIGOS DE RESPALDO. Diez códigos de un solo uso que se entregan al
--      registrarse y se enseñan UNA sola vez. Se guardan con hash BCrypt, igual
--      que las contraseñas: ni leyendo la base se pueden recuperar. Es lo que
--      hacen Google, GitHub y Auth0, y lo que recomienda la guía de OWASP.
--      Funcionan sin internet y sin servidor de correo.
--
--   2. CÓDIGO POR CORREO. El de siempre, pero ahora sale de verdad hacia el
--      buzón del dueño. Si no hay servidor configurado, la vía no se ofrece:
--      antes que enseñar el código en pantalla, no hay vía.
--
--  Y como los dos se pueden intentar a ciegas, la cuenta lleva su propio
--  contador de intentos de recuperación, aparte del de inicio de sesión.
--
--  Idempotente.
-- ============================================================================

USE dbreserva_vuelos;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------- códigos de respaldo -----
IF OBJECT_ID('dbo.codigo_respaldo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.codigo_respaldo (
        idcodigo       INT IDENTITY(1,1) NOT NULL,
        usuario_id     INT           NOT NULL,
        -- Hash BCrypt del código, nunca el código. Si alguien se lleva la base,
        -- se lleva hashes que no le sirven para entrar.
        codigo_hash    VARCHAR(100)  NOT NULL,
        usado          BIT           NOT NULL CONSTRAINT DF_codigo_respaldo_usado DEFAULT 0,
        fecha_uso      DATETIME      NULL,
        fecha_creacion DATETIME      NOT NULL CONSTRAINT DF_codigo_respaldo_fecha DEFAULT GETDATE(),

        CONSTRAINT PK_codigo_respaldo PRIMARY KEY (idcodigo),
        CONSTRAINT FK_codigo_respaldo_usuario FOREIGN KEY (usuario_id)
            REFERENCES dbo.usuario (usuario_id) ON DELETE CASCADE
    );
END
GO

-- Al verificar un código hay que recorrer los que le quedan sin usar a esa
-- cuenta: es exactamente lo que este índice resuelve.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_codigo_respaldo_usuario')
BEGIN
    CREATE NONCLUSTERED INDEX IX_codigo_respaldo_usuario
        ON dbo.codigo_respaldo (usuario_id, usado);
END
GO

-- --------------------------------- freno a los intentos a ciegas -----
-- Aparte del bloqueo de inicio de sesión: quien adivina un código de respaldo no
-- está probando contraseñas, y mezclarlos dejaría a la persona sin poder entrar
-- por haber fallado recuperando.
IF COL_LENGTH('dbo.usuario', 'intentos_recuperacion') IS NULL
BEGIN
    ALTER TABLE dbo.usuario ADD intentos_recuperacion INT NOT NULL
        CONSTRAINT DF_usuario_intentos_recuperacion DEFAULT 0;
END
GO

IF COL_LENGTH('dbo.usuario', 'bloqueo_recuperacion_hasta') IS NULL
BEGIN
    ALTER TABLE dbo.usuario ADD bloqueo_recuperacion_hasta DATETIME NULL;
END
GO

-- ------------------------------------------------------ consultas -----
-- Cuántos códigos le quedan a una cuenta. La pantalla lo usa para decidir si
-- ofrecer esta vía, y el perfil para avisar cuando quedan pocos.
CREATE OR ALTER FUNCTION dbo.fn_codigos_disponibles (@usuario_id INT)
RETURNS INT
AS
BEGIN
    RETURN (SELECT COUNT(*) FROM dbo.codigo_respaldo
             WHERE usuario_id = @usuario_id AND usado = 0);
END
GO

-- Las cuentas que se quedaron sin red: ni pregunta, ni códigos. Es lo que un
-- Administrador necesita ver para avisarles antes de que se queden fuera.
CREATE OR ALTER VIEW dbo.v_cuenta_sin_recuperacion
AS
SELECT u.usuario_id, u.usuario, u.nombre_completo, u.email, u.rol,
       CASE WHEN u.pregunta_seguridad IS NULL OR u.respuesta_seguridad IS NULL
            THEN 0 ELSE 1 END                       AS tiene_pregunta,
       dbo.fn_codigos_disponibles(u.usuario_id)     AS codigos_disponibles
FROM dbo.usuario u
WHERE u.esta_activo = 1
  AND (u.pregunta_seguridad IS NULL OR u.respuesta_seguridad IS NULL)
  AND dbo.fn_codigos_disponibles(u.usuario_id) = 0;
GO

PRINT 'ALAS · 09_recuperacion_segura.sql aplicado correctamente.';
GO
