-- ============================================================================
--  ALEJANDRÍA — Sistema de Biblioteca
--  Script 01: esquema de la base de datos
--  Base de datos: db_biblioteca
--  Autor: Alejandro Calderón — III Parcial
-- ============================================================================
--  Este esquema parte del script "DB BIBLIOTECA.sql" del II Parcial y lo lleva
--  a un modelo que una biblioteca real puede operar. Se conservan las llaves
--  originales (L00001, U00001) y los nombres de columna que ya existían; cada
--  cambio se explica donde ocurre.
--
--  Los tres cambios de fondo:
--
--  1) `Libros.stock` era un solo número. Un número no se puede prestar: lo que
--     se presta es UN LIBRO FÍSICO de la estantería. Se separan las dos ideas:
--
--         libro    = la obra catalogada (título, autor, editorial, categoría)
--         ejemplar = cada copia física de esa obra, con su código de barras,
--                    su ubicación en la estantería y su estado
--
--     Esa separación es la que permite saber cuántas copias hay disponibles,
--     cuál copia tiene cada socio y evitar prestar dos veces la misma.
--
--  2) `Prestamos.fecha_devolucion` era ambigua: en los datos del II Parcial
--     guardaba la fecha en que el libro DEBÍA volver, aun en préstamos con
--     estado 'Activo'. Se separan:
--
--         fecha_vencimiento = cuándo debe volver  (lo que antes era fecha_devolucion)
--         fecha_devolucion  = cuándo volvió de verdad (NULL mientras está afuera)
--
--     Sin esa separación no se puede calcular un solo día de retraso.
--
--  3) La tabla `Usuarios` del original guardaba a los SOCIOS de la biblioteca.
--     Ahora el sistema tiene además cuentas de acceso para el personal, así que
--     `Usuarios` se llama `socio` (quien pide prestado) y `usuario` queda para
--     quien OPERA el sistema. Son dos cosas distintas y ahora tienen dos tablas.
--
--  El script es idempotente: se puede volver a ejecutar sin error.
-- ============================================================================

IF DB_ID('db_biblioteca') IS NULL
    CREATE DATABASE db_biblioteca;
GO

USE db_biblioteca;
GO

-- El índice único filtrado de dbo.detalle_prestamo exige estas opciones.
-- SqlClient (la aplicación) ya las trae encendidas; sqlcmd no, por eso se fijan aquí.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
--  CATÁLOGOS DE AUTORIDAD
-- ============================================================================
--  En el II Parcial el autor, la editorial y la categoría eran texto libre
--  dentro de `Libros`. Escribir "McGraw Hill" y "Mc Graw Hill" creaba dos
--  editoriales distintas y ningún filtro servía. Ahora cada uno es una tabla:
--  se escribe una sola vez y se elige de una lista.

-- TABLA: categoria
IF OBJECT_ID('dbo.categoria', 'U') IS NULL
CREATE TABLE dbo.categoria (
    idcategoria   INT          NOT NULL PRIMARY KEY,
    nombre        VARCHAR(40)  NOT NULL UNIQUE,
    -- Clasificación decimal Dewey: el número que va en el lomo del libro
    codigo_dewey  CHAR(3)      NULL,
    descripcion   VARCHAR(120) NULL
);
GO

-- TABLA: editorial
IF OBJECT_ID('dbo.editorial', 'U') IS NULL
CREATE TABLE dbo.editorial (
    ideditorial INT         NOT NULL PRIMARY KEY,
    nombre      VARCHAR(60) NOT NULL UNIQUE,
    pais        VARCHAR(40) NULL
);
GO

-- TABLA: autor
IF OBJECT_ID('dbo.autor', 'U') IS NULL
CREATE TABLE dbo.autor (
    idautor      INT         NOT NULL PRIMARY KEY,
    nombre       VARCHAR(60) NOT NULL UNIQUE,
    nacionalidad VARCHAR(40) NULL
);
GO

-- ============================================================================
--  ACERVO
-- ============================================================================

-- TABLA: libro — la OBRA catalogada. Ya no guarda `stock`: cuántas copias hay
-- se responde contando ejemplares, que es el dato que de verdad existe.
IF OBJECT_ID('dbo.libro', 'U') IS NULL
CREATE TABLE dbo.libro (
    idlibro          CHAR(6)      NOT NULL PRIMARY KEY,   -- L00001, igual que el original
    isbn             VARCHAR(17)  NULL UNIQUE,            -- 978-84-376-0494-7
    titulo           VARCHAR(120) NOT NULL,
    idautor          INT          NOT NULL,
    ideditorial      INT          NULL,
    idcategoria      INT          NOT NULL,
    anio_publicacion INT          NULL,
    edicion          VARCHAR(20)  NULL,
    idioma           VARCHAR(20)  NOT NULL DEFAULT 'Español',
    sinopsis         VARCHAR(600) NULL,
    fecha_alta       DATE         NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_libro_autor     FOREIGN KEY (idautor)     REFERENCES dbo.autor (idautor),
    CONSTRAINT FK_libro_editorial FOREIGN KEY (ideditorial) REFERENCES dbo.editorial (ideditorial),
    CONSTRAINT FK_libro_categoria FOREIGN KEY (idcategoria) REFERENCES dbo.categoria (idcategoria),
    CONSTRAINT CK_libro_anio      CHECK (anio_publicacion IS NULL
                                         OR anio_publicacion BETWEEN 1450 AND YEAR(GETDATE()) + 1)
);
GO

-- TABLA: ejemplar — la copia física. Es la tabla nueva más importante del
-- esquema: convierte `stock = 10` en diez objetos con identidad propia, cada
-- uno con su código de barras y su estado.
--
--   Disponible  → en la estantería, se puede prestar
--   Prestado    → afuera, con un socio
--   Reparación  → dañado, temporalmente fuera de circulación
--   Extraviado  → no apareció; se cobra al socio y no se puede prestar
--   Baja        → descartado del acervo
IF OBJECT_ID('dbo.ejemplar', 'U') IS NULL
CREATE TABLE dbo.ejemplar (
    idejemplar        INT         IDENTITY(1,1) PRIMARY KEY,
    codigo_barras     VARCHAR(16) NOT NULL UNIQUE,   -- L00001-01
    idlibro           CHAR(6)     NOT NULL,
    ubicacion         VARCHAR(30) NULL,              -- Estante A-3
    estado            VARCHAR(15) NOT NULL DEFAULT 'Disponible',
    condicion         VARCHAR(15) NOT NULL DEFAULT 'Bueno',
    fecha_adquisicion DATE        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_ejemplar_libro  FOREIGN KEY (idlibro) REFERENCES dbo.libro (idlibro),
    CONSTRAINT CK_ejemplar_estado CHECK (estado IN
        ('Disponible', 'Prestado', 'Reparación', 'Extraviado', 'Baja')),
    CONSTRAINT CK_ejemplar_condicion CHECK (condicion IN
        ('Nuevo', 'Bueno', 'Regular', 'Deteriorado'))
);
GO

-- ============================================================================
--  SOCIOS
-- ============================================================================

-- TABLA: tipo_socio — nueva. En el original todos los socios eran iguales, así
-- que las reglas del préstamo (cuántos libros, por cuántos días, cuánto se
-- multa) no vivían en ninguna parte. Aquí viven en una fila: cambiar la política
-- de préstamo de los estudiantes es cambiar un número, no tocar el código.
IF OBJECT_ID('dbo.tipo_socio', 'U') IS NULL
CREATE TABLE dbo.tipo_socio (
    idtipo        INT          NOT NULL PRIMARY KEY,
    nombre        VARCHAR(20)  NOT NULL UNIQUE,
    max_prestamos INT          NOT NULL,          -- ejemplares simultáneos
    dias_prestamo INT          NOT NULL,          -- plazo de devolución
    multa_diaria  DECIMAL(6,2) NOT NULL,          -- lempiras por día de retraso
    CONSTRAINT CK_tipo_max   CHECK (max_prestamos BETWEEN 1 AND 20),
    CONSTRAINT CK_tipo_dias  CHECK (dias_prestamo BETWEEN 1 AND 90),
    CONSTRAINT CK_tipo_multa CHECK (multa_diaria >= 0)
);
GO

-- TABLA: socio — era `Usuarios` en el II Parcial. Conserva sus columnas
-- (nombre, apellido, telefono, email, direccion, fecha_registro) y suma el tipo
-- de socio, la identidad y la baja lógica.
IF OBJECT_ID('dbo.socio', 'U') IS NULL
CREATE TABLE dbo.socio (
    idsocio        CHAR(6)      NOT NULL PRIMARY KEY,   -- U00001, igual que el original
    nombre         VARCHAR(40)  NOT NULL,
    apellido       VARCHAR(40)  NOT NULL,
    identidad      VARCHAR(15)  NULL UNIQUE,            -- número de identidad
    telefono       VARCHAR(15)  NULL,
    email          VARCHAR(60)  NOT NULL UNIQUE,
    direccion      VARCHAR(100) NULL,
    idtipo         INT          NOT NULL,
    fecha_registro DATE         NOT NULL DEFAULT GETDATE(),
    -- Baja lógica: un socio con historial de préstamos no se borra, se inactiva
    esta_activo    BIT          NOT NULL DEFAULT 1,
    CONSTRAINT FK_socio_tipo FOREIGN KEY (idtipo) REFERENCES dbo.tipo_socio (idtipo)
);
GO

-- ============================================================================
--  CIRCULACIÓN
-- ============================================================================

-- TABLA: prestamo — la cabecera. Un socio se lleva varios ejemplares en una
-- sola visita: por eso cabecera y detalle, tal como en el II Parcial.
IF OBJECT_ID('dbo.prestamo', 'U') IS NULL
CREATE TABLE dbo.prestamo (
    idprestamo        INT          IDENTITY(1,1) PRIMARY KEY,
    -- Folio que se le dice al socio; el IDENTITY es interno
    codigo            VARCHAR(12)  NOT NULL UNIQUE,      -- PR-000001
    idsocio           CHAR(6)      NOT NULL,
    fecha_prestamo    DATETIME     NOT NULL DEFAULT GETDATE(),
    fecha_vencimiento DATE         NOT NULL,             -- antes se llamaba fecha_devolucion
    fecha_devolucion  DATETIME     NULL,                 -- devolución REAL, NULL si sigue afuera
    estado            VARCHAR(15)  NOT NULL DEFAULT 'Activo',
    -- Cuántas veces se le ha extendido el plazo. Sin este contador, "renovar"
    -- sería un préstamo eterno: el socio pediría una prórroga cada semana y el
    -- libro nunca volvería a la estantería.
    renovaciones      INT          NOT NULL DEFAULT 0,
    usuario_registra  VARCHAR(30)  NULL,                 -- quién lo atendió en el mostrador
    observacion       VARCHAR(200) NULL,
    CONSTRAINT FK_prestamo_socio  FOREIGN KEY (idsocio) REFERENCES dbo.socio (idsocio),
    CONSTRAINT CK_prestamo_estado CHECK (estado IN ('Activo', 'Devuelto', 'Cancelado')),
    CONSTRAINT CK_prestamo_plazo  CHECK (fecha_vencimiento >= CAST(fecha_prestamo AS DATE))
);
GO

-- TABLA: detalle_prestamo — cada renglón es UN EJEMPLAR concreto prestado.
-- En el original apuntaba a `idLibro` con una `cantidad`, que es como pedir
-- "3 unidades del libro L00003" sin decir cuáles: al devolverlos no había forma
-- de saber qué copia volvió ni en qué estado.
IF OBJECT_ID('dbo.detalle_prestamo', 'U') IS NULL
CREATE TABLE dbo.detalle_prestamo (
    iddetalle            INT         IDENTITY(1,1) PRIMARY KEY,
    idprestamo           INT         NOT NULL,
    idejemplar           INT         NOT NULL,
    -- Cada ejemplar puede devolverse por separado: alguien trae dos de los tres
    -- libros hoy y el tercero la otra semana.
    fecha_devolucion     DATETIME    NULL,
    condicion_devolucion VARCHAR(15) NULL,
    CONSTRAINT FK_detalle_prestamo FOREIGN KEY (idprestamo) REFERENCES dbo.prestamo (idprestamo),
    CONSTRAINT FK_detalle_ejemplar FOREIGN KEY (idejemplar) REFERENCES dbo.ejemplar (idejemplar),
    CONSTRAINT CK_detalle_condicion CHECK (condicion_devolucion IS NULL
        OR condicion_devolucion IN ('Nuevo', 'Bueno', 'Regular', 'Deteriorado', 'Extraviado'))
);
GO

-- La regla más importante del sistema, y la impone la base de datos, no la
-- aplicación: UN EJEMPLAR NO PUEDE ESTAR EN DOS PRÉSTAMOS ABIERTOS A LA VEZ.
--
-- El índice es único pero FILTRADO por `fecha_devolucion IS NULL`: solo mira los
-- renglones todavía afuera. Cuando el libro vuelve, el renglón deja de contar y
-- el ejemplar se puede volver a prestar. Si dos bibliotecarios intentan prestar
-- la misma copia al mismo tiempo, el segundo choca contra este índice.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ejemplar_en_prestamo')
    CREATE UNIQUE NONCLUSTERED INDEX UQ_ejemplar_en_prestamo
        ON dbo.detalle_prestamo (idejemplar)
        WHERE fecha_devolucion IS NULL;
GO

-- TABLA: multa — nueva. Sin ella, un préstamo vencido no tiene consecuencia y
-- la fecha de vencimiento es decorativa.
--
--   Retraso   → días de mora × multa_diaria del tipo de socio
--   Daño      → el ejemplar volvió deteriorado
--   Extravío  → el ejemplar no volvió
IF OBJECT_ID('dbo.multa', 'U') IS NULL
CREATE TABLE dbo.multa (
    idmulta          INT          IDENTITY(1,1) PRIMARY KEY,
    idprestamo       INT          NOT NULL,
    idsocio          CHAR(6)      NOT NULL,
    motivo           VARCHAR(15)  NOT NULL,
    dias_retraso     INT          NOT NULL DEFAULT 0,
    monto            DECIMAL(8,2) NOT NULL,
    estado           VARCHAR(15)  NOT NULL DEFAULT 'Pendiente',
    fecha_generada   DATETIME     NOT NULL DEFAULT GETDATE(),
    fecha_pago       DATETIME     NULL,
    usuario_registra VARCHAR(30)  NULL,
    observacion      VARCHAR(200) NULL,
    CONSTRAINT FK_multa_prestamo FOREIGN KEY (idprestamo) REFERENCES dbo.prestamo (idprestamo),
    CONSTRAINT FK_multa_socio    FOREIGN KEY (idsocio)    REFERENCES dbo.socio (idsocio),
    CONSTRAINT CK_multa_motivo   CHECK (motivo IN ('Retraso', 'Daño', 'Extravío')),
    CONSTRAINT CK_multa_estado   CHECK (estado IN ('Pendiente', 'Pagada', 'Condonada')),
    CONSTRAINT CK_multa_monto    CHECK (monto >= 0)
);
GO

-- TABLA: reserva — nueva. Cuando todas las copias de un título están prestadas,
-- el socio se apunta en la fila de espera en vez de volver a preguntar cada día.
IF OBJECT_ID('dbo.reserva', 'U') IS NULL
CREATE TABLE dbo.reserva (
    idreserva     INT         IDENTITY(1,1) PRIMARY KEY,
    idlibro       CHAR(6)     NOT NULL,
    idsocio       CHAR(6)     NOT NULL,
    fecha_reserva DATETIME    NOT NULL DEFAULT GETDATE(),
    fecha_expira  DATE        NOT NULL,
    estado        VARCHAR(15) NOT NULL DEFAULT 'Activa',
    CONSTRAINT FK_reserva_libro  FOREIGN KEY (idlibro) REFERENCES dbo.libro (idlibro),
    CONSTRAINT FK_reserva_socio  FOREIGN KEY (idsocio) REFERENCES dbo.socio (idsocio),
    CONSTRAINT CK_reserva_estado CHECK (estado IN ('Activa', 'Atendida', 'Vencida', 'Cancelada'))
);
GO

-- Un socio no puede reservar dos veces el mismo título mientras la primera
-- reserva siga activa. Otra vez: índice único filtrado.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_reserva_activa')
    CREATE UNIQUE NONCLUSTERED INDEX UQ_reserva_activa
        ON dbo.reserva (idlibro, idsocio)
        WHERE estado = 'Activa';
GO

PRINT 'ALEJANDRÍA · 01_esquema.sql aplicado correctamente.';
GO
