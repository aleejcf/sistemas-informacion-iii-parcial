-- ============================================================================
--  ALEJANDRÍA — Sistema de Biblioteca
--  Script 05: venta de ejemplares dados de baja
--  Base de datos: db_biblioteca
-- ============================================================================
--  Una biblioteca no vende lo que presta: vende lo que ya sacó de circulación
--  (duplicados, copias deterioradas). Por eso esto no es "vender un libro
--  cualquiera del catálogo" -eso mezclaría venta con préstamo en la misma
--  ficha-, sino un paso más después de dar de baja un ejemplar, que ya
--  existía como estado desde el script 01 pero sin ninguna acción que lo
--  usara todavía.
--
--  `venta_ejemplar` copia `idlibro` y `codigo_barras` al momento de vender
--  -no solo el idejemplar- a propósito: si el título se edita o el ejemplar
--  algún día se purga, el recibo de la venta no debe cambiar retroactivamente
--  ni perder de qué se trataba.
--
--  Idempotente.
-- ============================================================================

USE db_biblioteca;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 'Vendido' es estado terminal: no hay "Prestado" ni "Reparación" después de
-- vendido. Se agrega al CHECK existente en vez de crear una columna aparte
-- (como "esta_vendido") para no tener dos lugares donde un ejemplar pueda
-- decir cosas contradictorias sobre sí mismo.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ejemplar_estado')
    ALTER TABLE dbo.ejemplar DROP CONSTRAINT CK_ejemplar_estado;
GO

ALTER TABLE dbo.ejemplar ADD CONSTRAINT CK_ejemplar_estado CHECK (estado IN
    ('Disponible', 'Prestado', 'Reparación', 'Extraviado', 'Baja', 'Vendido'));
GO

-- ------------------------------------------------------- venta_ejemplar -----
IF OBJECT_ID('dbo.venta_ejemplar', 'U') IS NULL
CREATE TABLE dbo.venta_ejemplar (
    idventa          INT          IDENTITY(1,1) PRIMARY KEY,
    idejemplar       INT          NOT NULL UNIQUE,  -- un ejemplar se vende una sola vez
    idlibro          CHAR(6)      NOT NULL,
    codigo_barras    VARCHAR(16)  NOT NULL,
    precio           DECIMAL(8,2) NOT NULL,
    comprador        VARCHAR(80)  NULL,
    usuario_registra VARCHAR(30)  NULL,
    fecha_venta      DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_venta_ejemplar_ejemplar FOREIGN KEY (idejemplar) REFERENCES dbo.ejemplar (idejemplar),
    CONSTRAINT FK_venta_ejemplar_libro    FOREIGN KEY (idlibro)    REFERENCES dbo.libro (idlibro),
    CONSTRAINT CK_venta_ejemplar_precio   CHECK (precio > 0)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_venta_ejemplar_fecha')
    CREATE NONCLUSTERED INDEX IX_venta_ejemplar_fecha
        ON dbo.venta_ejemplar (fecha_venta DESC);
GO

-- --------------------------------------------------- v_venta_detalle -----
-- La venta con la ficha del título, para no repetir el JOIN en cada consulta.
CREATE OR ALTER VIEW dbo.v_venta_detalle AS
SELECT
    v.idventa,
    v.idejemplar,
    v.codigo_barras,
    v.idlibro,
    l.titulo,
    a.nombre AS autor,
    v.precio,
    v.comprador,
    v.usuario_registra,
    v.fecha_venta
FROM dbo.venta_ejemplar v
JOIN dbo.libro l ON l.idlibro = v.idlibro
JOIN dbo.autor a ON a.idautor = l.idautor;
GO

PRINT 'ALEJANDRÍA · 05_venta_ejemplares.sql aplicado correctamente.';
GO
