-- ============================================================
-- Script 05 - Auditoría de inicios de sesión
-- Registro estructurado en BD (a diferencia del log de archivo),
-- consultable desde la nueva página "Auditoría"
-- Base de datos: parqueadero
-- ============================================================
USE parqueadero;
GO

IF OBJECT_ID('dbo.auditoria_login', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.auditoria_login (
        auditoria_id INT IDENTITY(1,1) PRIMARY KEY,
        usuario      VARCHAR(30)  NOT NULL,
        exito        BIT          NOT NULL,
        detalle      VARCHAR(100) NULL,
        equipo       VARCHAR(50)  NULL,
        fecha        DATETIME     NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_auditoria_login_usuario_fecha')
    CREATE NONCLUSTERED INDEX IX_auditoria_login_usuario_fecha
        ON dbo.auditoria_login (usuario, fecha DESC);
GO

PRINT 'Tabla auditoria_login creada correctamente.';
GO
