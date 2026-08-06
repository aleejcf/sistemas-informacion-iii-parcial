-- ============================================================
-- Script 03 - Gestión de usuarios por el Administrador
-- Agrega el cambio de contraseña obligatorio en el primer login
-- Base de datos: parqueadero
-- ============================================================
USE parqueadero;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.usuario') AND name = 'debe_cambiar_contrasena'
)
    ALTER TABLE dbo.usuario ADD debe_cambiar_contrasena BIT NOT NULL DEFAULT 0;
GO

PRINT 'Columna debe_cambiar_contrasena agregada correctamente.';
GO
