-- ============================================================
-- Script 04 - Recuperación de contraseña por código de correo
-- Alternativa a la pregunta de seguridad cuando no está configurada
-- Base de datos: parqueadero
-- ============================================================
USE parqueadero;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.usuario') AND name = 'codigo_recuperacion'
)
    ALTER TABLE dbo.usuario ADD codigo_recuperacion VARCHAR(6) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.usuario') AND name = 'fecha_expiracion_codigo'
)
    ALTER TABLE dbo.usuario ADD fecha_expiracion_codigo DATETIME NULL;
GO

PRINT 'Columnas de recuperación por correo agregadas correctamente.';
GO
