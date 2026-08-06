-- ============================================================================
--  ALEJANDRÍA — Sistema de Biblioteca
--  Script 02: datos de arranque
--  Base de datos: db_biblioteca
-- ============================================================================
--  Se conservan los 20 libros y los 20 socios del II Parcial con sus mismas
--  llaves (L00001…L00020, U00001…U00020) y sus mismos datos. Lo que cambia es
--  cómo se guardan: el autor, la editorial y la categoría que antes eran texto
--  libre ahora son filas de catálogo, y el `stock` se convierte en ejemplares
--  físicos con código de barras.
--
--  Los préstamos SÍ se recrean con fechas relativas al día en que se ejecuta
--  el script. Los del II Parcial eran todos de marzo: si se copiaran tal cual,
--  cada préstamo "Activo" aparecería con cinco meses de mora y el sistema se
--  vería roto. Con fechas relativas el panel arranca con una operación viva:
--  préstamos al día, otros por vencer, algunos vencidos y multas de ambos tipos.
--
--  Idempotente: cada bloque revisa si ya hay datos antes de insertar.
-- ============================================================================

USE db_biblioteca;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------ categoria -----
-- El código Dewey es el que va impreso en el lomo y ordena la estantería.
IF NOT EXISTS (SELECT 1 FROM dbo.categoria)
INSERT INTO dbo.categoria (idcategoria, nombre, codigo_dewey, descripcion) VALUES
    (1,  'Programación',            '005', 'Lenguajes, algoritmos y desarrollo de software'),
    (2,  'Base de datos',           '005', 'Diseño, modelado y administración de datos'),
    (3,  'Redes',                   '004', 'Infraestructura, protocolos y comunicaciones'),
    (4,  'Ingeniería de Software',  '005', 'Metodologías, análisis y diseño de sistemas'),
    (5,  'Desarrollo Web',          '006', 'Tecnologías del lado del cliente y del servidor'),
    (6,  'Ofimática',               '005', 'Herramientas de productividad de oficina'),
    (7,  'Matemática',              '510', 'Álgebra, aritmética y lógica'),
    (8,  'Física',                  '530', 'Mecánica, ondas y electromagnetismo'),
    (9,  'Química',                 '540', 'Química general, orgánica e inorgánica'),
    (10, 'Historia',                '972', 'Historia de Honduras y Centroamérica'),
    (11, 'Geografía',               '910', 'Geografía física, política y económica'),
    (12, 'Idiomas',                 '420', 'Aprendizaje de lenguas extranjeras'),
    (13, 'Marketing',               '658', 'Mercadeo, publicidad y ventas'),
    (14, 'Contabilidad',            '657', 'Contabilidad financiera y de costos'),
    (15, 'Inteligencia Artificial', '006', 'Aprendizaje automático y sistemas inteligentes');
GO

-- ------------------------------------------------------------ editorial -----
IF NOT EXISTS (SELECT 1 FROM dbo.editorial)
INSERT INTO dbo.editorial (ideditorial, nombre, pais) VALUES
    (1,  'McGraw Hill',            'Estados Unidos'),
    (2,  'Pearson',                'Reino Unido'),
    (3,  'MIT Press',              'Estados Unidos'),
    (4,  'Cisco Press',            'Estados Unidos'),
    (5,  'Wiley',                  'Estados Unidos'),
    (6,  'Addison-Wesley',         'Estados Unidos'),
    (7,  'Microsoft Press',        'Estados Unidos'),
    (8,  'Publicaciones Cultural', 'México'),
    (9,  'Cengage Learning',       'Estados Unidos'),
    (10, 'Editorial Guaymuras',    'Honduras'),
    (11, 'Santillana',             'España'),
    (12, 'Cambridge University',   'Reino Unido');
GO

-- ---------------------------------------------------------------- autor -----
IF NOT EXISTS (SELECT 1 FROM dbo.autor)
INSERT INTO dbo.autor (idautor, nombre, nacionalidad) VALUES
    (1,  'Luis Joyanes Aguilar', 'España'),
    (2,  'Ramez Elmasri',        'Estados Unidos'),
    (3,  'Thomas H. Cormen',     'Estados Unidos'),
    (4,  'Cisco Networking Academy', 'Estados Unidos'),
    (5,  'Roger S. Pressman',    'Estados Unidos'),
    (6,  'Jon Duckett',          'Reino Unido'),
    (7,  'Paul Deitel',          'Estados Unidos'),
    (8,  'Zed A. Shaw',          'Estados Unidos'),
    (9,  'Microsoft Corporation','Estados Unidos'),
    (10, 'Aurelio Baldor',       'Cuba'),
    (11, 'Raymond A. Serway',    'Estados Unidos'),
    (12, 'Raymond Chang',        'Estados Unidos'),
    (13, 'Longino Becerra',      'Honduras'),
    (14, 'Equipo Santillana',    'España'),
    (15, 'Cambridge Assessment', 'Reino Unido'),
    (16, 'Philip Kotler',        'Estados Unidos'),
    (17, 'Robert F. Meigs',      'Estados Unidos'),
    (18, 'Irving M. Copi',       'Estados Unidos'),
    (19, 'Stuart J. Russell',    'Reino Unido');
GO

-- ---------------------------------------------------------------- libro -----
-- Los mismos 20 títulos del II Parcial, con su idlibro y su año intactos.
-- Se agregan ISBN (prefijo 978-99926, el asignado a Honduras), edición e idioma.
IF NOT EXISTS (SELECT 1 FROM dbo.libro)
INSERT INTO dbo.libro (idlibro, isbn, titulo, idautor, ideditorial, idcategoria,
                       anio_publicacion, edicion, idioma, sinopsis) VALUES
    ('L00001', '978-99926-11-01-2', 'Programación en Pascal',   1,  1,  1, 2018, '4.ª edición', 'Español',
     'Fundamentos de la programación estructurada con Pascal: tipos, procedimientos y recursividad.'),
    ('L00002', '978-99926-11-02-9', 'Base de Datos SQL',        2,  2,  2, 2019, '7.ª edición', 'Español',
     'Modelo relacional, álgebra relacional, normalización y SQL desde cero.'),
    ('L00003', '978-99926-11-03-6', 'Algoritmos',               3,  3,  1, 2015, '3.ª edición', 'Español',
     'El texto clásico de análisis y diseño de algoritmos, con su tratamiento de la complejidad.'),
    ('L00004', '978-99926-11-04-3', 'Redes Cisco',              4,  4,  3, 2020, '2.ª edición', 'Español',
     'Modelo OSI, direccionamiento IP, enrutamiento y configuración de dispositivos Cisco.'),
    ('L00005', '978-99926-11-05-0', 'Ingeniería de Software',   5,  1,  4, 2016, '8.ª edición', 'Español',
     'Procesos de desarrollo, requisitos, diseño, pruebas y gestión de proyectos de software.'),
    ('L00006', '978-99926-11-06-7', 'HTML y CSS',               6,  5,  5, 2021, '1.ª edición', 'Español',
     'Maquetación web moderna explicada visualmente: estructura, estilos y diseño adaptable.'),
    ('L00007', '978-99926-11-07-4', 'Java Básico',              7,  2,  1, 2017, '10.ª edición','Español',
     'Programación orientada a objetos en Java: clases, herencia, colecciones y excepciones.'),
    ('L00008', '978-99926-11-08-1', 'Python Práctico',          8,  6,  1, 2022, '3.ª edición', 'Español',
     'Aprender Python haciendo ejercicios: sintaxis, estructuras de datos y automatización.'),
    ('L00009', '978-99926-11-09-8', 'Excel Profesional',        9,  7,  6, 2020, '1.ª edición', 'Español',
     'Fórmulas, tablas dinámicas, gráficos y automatización de reportes en Excel.'),
    ('L00010', '978-99926-11-10-4', 'Word Avanzado',            9,  7,  6, 2020, '1.ª edición', 'Español',
     'Estilos, plantillas, referencias cruzadas y documentos extensos con Word.'),
    ('L00011', '978-99926-11-11-1', 'Matemática Básica',       10,  8,  7, 2010, '2.ª edición', 'Español',
     'Aritmética y álgebra elemental con la colección de ejercicios que hizo célebre al texto.'),
    ('L00012', '978-99926-11-12-8', 'Física General',          11,  9,  8, 2014, '9.ª edición', 'Español',
     'Mecánica, termodinámica, ondas y electromagnetismo con problemas resueltos.'),
    ('L00013', '978-99926-11-13-5', 'Química Básica',          12,  1,  9, 2013, '11.ª edición','Español',
     'Estructura atómica, enlaces, estequiometría y reacciones químicas.'),
    ('L00014', '978-99926-11-14-2', 'Historia de Honduras',    13, 10, 10, 2012, '5.ª edición', 'Español',
     'Recorrido por la historia hondureña desde la época prehispánica hasta el siglo XX.'),
    ('L00015', '978-99926-11-15-9', 'Geografía Mundial',       14, 11, 11, 2011, '1.ª edición', 'Español',
     'Geografía física y humana del mundo con cartografía y datos comparados.'),
    ('L00016', '978-99926-11-16-6', 'Inglés Básico',           15, 12, 12, 2018, '4.ª edición', 'Inglés',
     'Curso introductorio de inglés con gramática, vocabulario y práctica auditiva.'),
    ('L00017', '978-99926-11-17-3', 'Marketing Digital',       16,  2, 13, 2021, '15.ª edición','Español',
     'Estrategia de mercadeo en canales digitales: contenido, métricas y publicidad en línea.'),
    ('L00018', '978-99926-11-18-0', 'Contabilidad General',    17,  1, 14, 2016, '11.ª edición','Español',
     'Ciclo contable, estados financieros y análisis de cuentas para principiantes.'),
    ('L00019', '978-99926-11-19-7', 'Lógica Matemática',       18,  2,  7, 2015, '14.ª edición','Español',
     'Lógica proposicional y de predicados, métodos de demostración y falacias.'),
    ('L00020', '978-99926-11-20-3', 'Inteligencia Artificial', 19,  2, 15, 2022, '4.ª edición', 'Español',
     'Agentes inteligentes, búsqueda, representación del conocimiento y aprendizaje automático.');
GO

-- ------------------------------------------------------------- ejemplar -----
-- Aquí se traduce el `stock` del II Parcial: cada unidad de stock se convierte
-- en una copia física con su propio código de barras (L00001-01, L00001-02…).
-- Los 141 ejemplares se generan con una consulta en vez de 141 INSERT a mano.
IF NOT EXISTS (SELECT 1 FROM dbo.ejemplar)
BEGIN
    -- Tabla de números: sirve para repetir cada libro tantas veces como copias tenga
    ;WITH numeros AS (
        SELECT TOP (20) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects
    ),
    -- El stock exacto que traía cada libro en el script del II Parcial
    acervo (idlibro, copias) AS (
        SELECT * FROM (VALUES
            ('L00001', 10), ('L00002',  8), ('L00003',  5), ('L00004',  7),
            ('L00005',  6), ('L00006',  9), ('L00007', 10), ('L00008', 11),
            ('L00009', 12), ('L00010',  9), ('L00011',  6), ('L00012',  5),
            ('L00013',  4), ('L00014',  8), ('L00015',  7), ('L00016',  6),
            ('L00017',  5), ('L00018',  6), ('L00019',  4), ('L00020',  3)
        ) AS t (idlibro, copias)
    )
    INSERT INTO dbo.ejemplar (codigo_barras, idlibro, ubicacion, estado, condicion, fecha_adquisicion)
    SELECT
        a.idlibro + '-' + RIGHT('0' + CAST(n.n AS VARCHAR(2)), 2),
        a.idlibro,
        -- La ubicación sale del Dewey: los libros de un mismo tema van juntos
        'Estante ' + c.codigo_dewey + '-' + RIGHT('0' + CAST(((n.n - 1) / 4) + 1 AS VARCHAR(2)), 2),
        'Disponible',
        CASE
            WHEN n.n = 1          THEN 'Nuevo'
            WHEN n.n % 4 = 0      THEN 'Regular'
            ELSE 'Bueno'
        END,
        DATEADD(DAY, -(180 + n.n * 7), CAST(GETDATE() AS DATE))
    FROM acervo a
    JOIN numeros n ON n.n <= a.copias
    JOIN dbo.libro l ON l.idlibro = a.idlibro
    JOIN dbo.categoria c ON c.idcategoria = l.idcategoria;

    -- Dos copias entran al taller: el catálogo debe poder mostrar que existen
    -- pero no se pueden prestar.
    UPDATE dbo.ejemplar SET estado = 'Reparación', condicion = 'Deteriorado'
    WHERE codigo_barras IN ('L00011-06', 'L00013-04');
END
GO

-- ----------------------------------------------------------- tipo_socio -----
-- Las reglas del préstamo, en datos y no en código: cuántos ejemplares puede
-- tener afuera cada tipo de socio, por cuántos días y cuánto se le cobra por
-- cada día de retraso.
IF NOT EXISTS (SELECT 1 FROM dbo.tipo_socio)
INSERT INTO dbo.tipo_socio (idtipo, nombre, max_prestamos, dias_prestamo, multa_diaria) VALUES
    (1, 'Estudiante',    3, 7,  5.00),
    (2, 'Docente',       6, 21, 3.00),
    (3, 'Investigador',  8, 30, 3.00),
    (4, 'Público', 2, 5,  8.00);
GO

-- ---------------------------------------------------------------- socio -----
-- Los mismos 20 socios del II Parcial: mismo id, nombre, apellido, teléfono,
-- correo, dirección y fecha de registro. Se les asigna tipo e identidad.
IF NOT EXISTS (SELECT 1 FROM dbo.socio)
INSERT INTO dbo.socio (idsocio, nombre, apellido, identidad, telefono, email, direccion, idtipo, fecha_registro) VALUES
    ('U00001','Juan','Lopez',        '0501199000101','98765432','juan@gmail.com',    'San Pedro Sula', 1,'2026-03-01'),
    ('U00002','Maria','Hernandez',   '0501199000202','99887766','maria@gmail.com',   'El Progreso',    1,'2026-03-02'),
    ('U00003','Carlos','Mejia',      '0501198500303','91234567','carlos@gmail.com',  'La Ceiba',       2,'2026-03-03'),
    ('U00004','Ana','Castro',        '0501199100404','92345678','ana@gmail.com',     'Choloma',        1,'2026-03-04'),
    ('U00005','Luis','Pineda',       '0501198800505','93456789','luis@gmail.com',    'Villanueva',     4,'2026-03-05'),
    ('U00006','Jose','Ramirez',      '0501199200606','94567890','jose@gmail.com',    'Tela',           1,'2026-03-06'),
    ('U00007','Karla','Gomez',       '0501198700707','95678901','karla@gmail.com',   'Puerto Cortes',  2,'2026-03-07'),
    ('U00008','Miguel','Alvarado',   '0501199300808','96789012','miguel@gmail.com',  'Danli',          1,'2026-03-08'),
    ('U00009','Sofia','Torres',      '0501199400909','97890123','sofia@gmail.com',   'Tegucigalpa',    3,'2026-03-09'),
    ('U00010','Daniel','Flores',     '0501199001010','98901234','daniel@gmail.com',  'Comayagua',      1,'2026-03-10'),
    ('U00011','Andrea','Murillo',    '0501198901111','90123456','andrea@gmail.com',  'Santa Rosa',     2,'2026-03-11'),
    ('U00012','Pedro','Diaz',        '0501199101212','90234567','pedro@gmail.com',   'Juticalpa',      4,'2026-03-12'),
    ('U00013','Laura','Perez',       '0501199201313','90345678','laura@gmail.com',   'Siguatepeque',   1,'2026-03-13'),
    ('U00014','Fernando','Rojas',    '0501198601414','90456789','fernando@gmail.com','Yoro',           2,'2026-03-14'),
    ('U00015','Valeria','Morales',   '0501199301515','90567890','valeria@gmail.com', 'La Lima',        1,'2026-03-15'),
    ('U00016','Kevin','Castillo',    '0501199401616','90678901','kevin@gmail.com',   'Trujillo',       1,'2026-03-16'),
    ('U00017','Diana','Mendoza',     '0501198801717','90789012','diana@gmail.com',   'Olanchito',      3,'2026-03-17'),
    ('U00018','Jorge','Navarro',     '0501199001818','90890123','jorge@gmail.com',   'Copan',          4,'2026-03-18'),
    ('U00019','Paola','Reyes',       '0501199201919','90901234','paola@gmail.com',   'Gracias',        1,'2026-03-19'),
    ('U00020','Ricardo','Suazo',     '0501198702020','91012345','ricardo@gmail.com', 'Intibuca',       2,'2026-03-20');
GO

-- ================================ CIRCULACIÓN ===============================
--  Los 20 préstamos del II Parcial, recreados con fechas relativas al día de
--  ejecución para que el sistema arranque con una operación creíble:
--
--      12 devueltos (3 de ellos con retraso → multa pagada o pendiente)
--       4 activos al día
--       2 activos que vencen en los próximos días
--       2 activos ya vencidos → aparecen en rojo en el panel
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.prestamo)
BEGIN
    -- Plan de la siembra. `dias_atras` es hace cuántos días se hizo el préstamo
    -- y `retraso` cuántos días tarde se devolvió (NULL = todavía no vuelve).
    DECLARE @cab TABLE (
        n                 INT PRIMARY KEY,
        idsocio           CHAR(6),
        dias_atras        INT,
        retraso           INT NULL,
        fecha_prestamo    DATETIME,
        fecha_vencimiento DATE,
        fecha_devolucion  DATETIME NULL,
        estado            VARCHAR(15)
    );

    INSERT INTO @cab (n, idsocio, dias_atras, retraso) VALUES
        -- Devueltos a tiempo
        ( 1, 'U00001', 96, 0), ( 2, 'U00002', 88, 0), ( 3, 'U00003', 80, 0),
        ( 4, 'U00004', 74, 0), ( 5, 'U00006', 66, 0), ( 6, 'U00007', 58, 0),
        ( 7, 'U00009', 50, 0), ( 8, 'U00010', 44, 0), ( 9, 'U00013', 36, 0),
        -- Devueltos con retraso → generan multa
        (10, 'U00005', 40, 6), (11, 'U00012', 30, 4), (12, 'U00015', 22, 9),
        -- Activos, dentro del plazo
        (13, 'U00008',  2, NULL), (14, 'U00011',  6, NULL),
        (15, 'U00017',  9, NULL), (16, 'U00020',  5, NULL),
        -- Activos, por vencer en los próximos días
        (17, 'U00016',  6, NULL), (18, 'U00014', 19, NULL),
        -- Activos ya vencidos → mora corriendo
        (19, 'U00019', 14, NULL), (20, 'U00018', 12, NULL);

    -- El plazo y, por tanto, la fecha de vencimiento salen del tipo de socio
    -- El CAST a DATETIME es obligatorio: DATEADD no acepta la parte HOUR sobre
    -- un valor DATE, y restar días a CAST(GETDATE() AS DATE) devuelve DATE.
    UPDATE c
       SET fecha_prestamo    = DATEADD(HOUR, 10,
                                       CAST(DATEADD(DAY, -c.dias_atras,
                                                    CAST(GETDATE() AS DATE)) AS DATETIME)),
           fecha_vencimiento = DATEADD(DAY, t.dias_prestamo,
                                       DATEADD(DAY, -c.dias_atras, CAST(GETDATE() AS DATE))),
           estado            = CASE WHEN c.retraso IS NULL THEN 'Activo' ELSE 'Devuelto' END
      FROM @cab c
      JOIN dbo.socio s      ON s.idsocio = c.idsocio
      JOIN dbo.tipo_socio t ON t.idtipo  = s.idtipo;

    UPDATE @cab
       SET fecha_devolucion = DATEADD(HOUR, 11, DATEADD(DAY, retraso, CAST(fecha_vencimiento AS DATETIME)))
     WHERE retraso IS NOT NULL;

    INSERT INTO dbo.prestamo (codigo, idsocio, fecha_prestamo, fecha_vencimiento,
                              fecha_devolucion, estado, usuario_registra, observacion)
    SELECT 'PR-' + RIGHT('000000' + CAST(n AS VARCHAR(6)), 6),
           idsocio, fecha_prestamo, fecha_vencimiento, fecha_devolucion, estado,
           'sistema',
           CASE WHEN retraso > 0 THEN 'Devuelto con ' + CAST(retraso AS VARCHAR(3)) + ' días de retraso.'
                ELSE NULL END
    FROM @cab
    ORDER BY n;

    -- Qué títulos se llevó cada préstamo. Varios renglones del mismo `n` = un
    -- socio que se llevó varios libros de una sola vez.
    DECLARE @det TABLE (n INT, idlibro CHAR(6));
    INSERT INTO @det (n, idlibro) VALUES
        ( 1,'L00001'), ( 1,'L00007'),
        ( 2,'L00002'),
        ( 3,'L00003'), ( 3,'L00005'), ( 3,'L00019'),
        ( 4,'L00004'),
        ( 5,'L00006'), ( 5,'L00008'),
        ( 6,'L00007'), ( 6,'L00020'),
        ( 7,'L00009'),
        ( 8,'L00010'), ( 8,'L00011'),
        ( 9,'L00012'),
        (10,'L00013'),
        (11,'L00014'), (11,'L00015'),
        (12,'L00016'),
        (13,'L00017'), (13,'L00001'),
        (14,'L00018'), (14,'L00002'),
        (15,'L00019'), (15,'L00020'), (15,'L00003'),
        (16,'L00006'),
        (17,'L00008'),
        (18,'L00014'), (18,'L00009'),
        (19,'L00020'), (19,'L00005'),
        (20,'L00011');

    -- A cada renglón se le asigna una copia física distinta del mismo título:
    -- el k-ésimo renglón que pide L00007 se lleva el k-ésimo ejemplar de L00007.
    ;WITH pedido AS (
        SELECT d.n, d.idlibro,
               ROW_NUMBER() OVER (PARTITION BY d.idlibro ORDER BY d.n) AS k
        FROM @det d
    ),
    copias AS (
        SELECT e.idejemplar, e.idlibro,
               ROW_NUMBER() OVER (PARTITION BY e.idlibro ORDER BY e.idejemplar) AS k
        FROM dbo.ejemplar e
        WHERE e.estado = 'Disponible'
    )
    INSERT INTO dbo.detalle_prestamo (idprestamo, idejemplar, fecha_devolucion, condicion_devolucion)
    SELECT p.idprestamo, co.idejemplar, c.fecha_devolucion,
           CASE WHEN c.fecha_devolucion IS NULL THEN NULL
                WHEN c.retraso >= 9 THEN 'Regular'
                ELSE 'Bueno' END
    FROM pedido pe
    JOIN copias co ON co.idlibro = pe.idlibro AND co.k = pe.k
    JOIN @cab c    ON c.n = pe.n
    JOIN dbo.prestamo p ON p.codigo = 'PR-' + RIGHT('000000' + CAST(c.n AS VARCHAR(6)), 6);

    -- Los ejemplares que siguen afuera quedan marcados como prestados. Es el
    -- mismo estado que pone la aplicación al registrar un préstamo real.
    UPDATE e
       SET estado = 'Prestado'
      FROM dbo.ejemplar e
      JOIN dbo.detalle_prestamo d ON d.idejemplar = e.idejemplar
     WHERE d.fecha_devolucion IS NULL;

    -- ------------------------------------------------------------- multas ---
    -- Multa por cada préstamo devuelto tarde: días de mora × tarifa del tipo de
    -- socio. Dos ya se pagaron y una queda pendiente, para que la página de
    -- multas arranque con ambos casos.
    INSERT INTO dbo.multa (idprestamo, idsocio, motivo, dias_retraso, monto,
                           estado, fecha_generada, fecha_pago, usuario_registra, observacion)
    SELECT p.idprestamo, p.idsocio, 'Retraso', c.retraso,
           c.retraso * t.multa_diaria * ejemplares.total,
           CASE WHEN c.n = 12 THEN 'Pendiente' ELSE 'Pagada' END,
           p.fecha_devolucion,
           CASE WHEN c.n = 12 THEN NULL ELSE DATEADD(HOUR, 1, p.fecha_devolucion) END,
           'sistema',
           'Multa generada al registrar la devolución.'
    FROM @cab c
    JOIN dbo.prestamo p   ON p.codigo = 'PR-' + RIGHT('000000' + CAST(c.n AS VARCHAR(6)), 6)
    JOIN dbo.socio s      ON s.idsocio = p.idsocio
    JOIN dbo.tipo_socio t ON t.idtipo  = s.idtipo
    CROSS APPLY (SELECT COUNT(*) AS total FROM dbo.detalle_prestamo d
                  WHERE d.idprestamo = p.idprestamo) AS ejemplares
    WHERE c.retraso > 0;
END
GO

-- --------------------------------------------------------------- reserva -----
-- Una reserva de arranque sobre el título con menos copias del acervo.
IF NOT EXISTS (SELECT 1 FROM dbo.reserva)
INSERT INTO dbo.reserva (idlibro, idsocio, fecha_reserva, fecha_expira, estado)
VALUES ('L00020', 'U00004', DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, 5, CAST(GETDATE() AS DATE)), 'Activa');
GO

PRINT 'ALEJANDRÍA · 02_datos_semilla.sql aplicado correctamente.';
GO
