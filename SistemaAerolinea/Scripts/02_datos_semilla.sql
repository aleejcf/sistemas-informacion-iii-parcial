-- ============================================================================
--  ALAS Honduras — Sistema de Reserva de Vuelos
--  Script 02: datos semilla
--  Base de datos: dbreserva_vuelos
-- ============================================================================
--  Los catálogos (países, aeropuertos, aerolíneas, aviones y pasajeros) son
--  los mismos del script "DB RESERVA.sql" del II Parcial, con las columnas
--  nuevas completadas. Lo que sí se genera aquí es lo que un sistema vivo
--  necesita y el script original no tenía:
--
--    · el mapa de asientos real de cada avión,
--    · una malla de vuelos relativa a la fecha de hoy (el sistema siempre
--      tiene vuelos de ayer, de hoy y de las próximas semanas),
--    · reservas, boletos y pagos de ejemplo para que el panel no salga vacío.
--
--  Idempotente: cada bloque solo inserta si su tabla está vacía.
-- ============================================================================

USE dbreserva_vuelos;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ---------------------------------------------------------------- pais -----
IF NOT EXISTS (SELECT 1 FROM dbo.pais)
INSERT INTO dbo.pais (idpais, nombre_pais) VALUES
('HN01','Honduras'),          ('GT01','Guatemala'),   ('SV01','El Salvador'),
('NI01','Nicaragua'),         ('CR01','Costa Rica'),  ('PA01','Panamá'),
('MX01','México'),            ('US01','Estados Unidos'), ('CO01','Colombia'),
('PE01','Perú'),              ('AR01','Argentina'),   ('CL01','Chile'),
('ES01','España'),            ('FR01','Francia'),     ('DE01','Alemania'),
('IT01','Italia'),            ('BR01','Brasil'),      ('EC01','Ecuador'),
('DO01','República Dominicana'), ('CA01','Canadá');
GO

-- --------------------------------------------------------- aeropuerto -----
IF NOT EXISTS (SELECT 1 FROM dbo.aeropuerto)
INSERT INTO dbo.aeropuerto (idaeropuerto, nombre, ciudad, iata, idpais) VALUES
('A0001','Toncontín',                'Tegucigalpa',        'TGU','HN01'),
('A0002','Palmerola',                'Comayagua',          'XPL','HN01'),
('A0003','Ramón Villeda Morales',    'San Pedro Sula',     'SAP','HN01'),
('A0004','Golosón',                  'La Ceiba',           'LCE','HN01'),
('A0005','Juan Manuel Gálvez',       'Roatán',             'RTB','HN01'),
('A0006','La Aurora',                'Ciudad de Guatemala','GUA','GT01'),
('A0007','Monseñor Romero',          'San Salvador',       'SAL','SV01'),
('A0008','Augusto C. Sandino',       'Managua',            'MGA','NI01'),
('A0009','Juan Santamaría',          'San José',           'SJO','CR01'),
('A0010','Tocumen',                  'Ciudad de Panamá',   'PTY','PA01'),
('A0011','Benito Juárez',            'Ciudad de México',   'MEX','MX01'),
('A0012','Miami International',      'Miami',              'MIA','US01'),
('A0013','Los Angeles International','Los Ángeles',        'LAX','US01'),
('A0014','El Dorado',                'Bogotá',             'BOG','CO01'),
('A0015','Jorge Chávez',             'Lima',               'LIM','PE01'),
('A0016','Ezeiza',                   'Buenos Aires',       'EZE','AR01'),
('A0017','Arturo Merino Benítez',    'Santiago',           'SCL','CL01'),
('A0018','Madrid Barajas',           'Madrid',             'MAD','ES01'),
('A0019','Charles de Gaulle',        'París',              'CDG','FR01'),
('A0020','Frankfurt Airport',        'Fráncfort',          'FRA','DE01');
GO

-- ---------------------------------------------------------- aerolinea -----
IF NOT EXISTS (SELECT 1 FROM dbo.aerolinea)
INSERT INTO dbo.aerolinea (idaerolinea, codigo, rtn, nombre_aero) VALUES
( 1,'AV','08011999123456','Avianca Honduras'),
( 2,'CM','08011999123457','CM Airlines'),
( 3,'AA','08011999123458','American Airlines'),
( 4,'DL','08011999123459','Delta Airlines'),
( 5,'UA','08011999123460','United Airlines'),
( 6,'CO','08011999123461','Copa Airlines'),
( 7,'NK','08011999123462','Spirit Airlines'),
( 8,'Y4','08011999123463','Volaris'),
( 9,'AM','08011999123464','Aeromexico'),
(10,'LA','08011999123465','LATAM Airlines'),
(11,'UX','08011999123466','Air Europa'),
(12,'IB','08011999123467','Iberia'),
(13,'AF','08011999123468','Air France'),
(14,'LH','08011999123469','Lufthansa'),
(15,'AZ','08011999123470','Alitalia'),
(16,'AC','08011999123471','Air Canada'),
(17,'B6','08011999123472','JetBlue'),
(18,'F9','08011999123473','Frontier Airlines'),
(19,'H2','08011999123474','Sky Airline'),
(20,'P5','08011999123475','Wingo');
GO

-- -------------------------------------------------------------- avion -----
-- La configuración de cabina se deduce del tamaño de la aeronave:
-- regional 4 asientos por fila (A-D), pasillo único 6 (A-F), fuselaje ancho 8 (A-H).
IF NOT EXISTS (SELECT 1 FROM dbo.avion)
INSERT INTO dbo.avion (idavion, idaerolinea, fabricante, tipo, capacidad_pasajeros, asientos_por_fila)
SELECT idavion, idaerolinea, fabricante, tipo, capacidad,
       CASE WHEN capacidad <= 100 THEN 4 WHEN capacidad <= 200 THEN 6 ELSE 8 END
FROM (VALUES
    ('AV001', 1,'Airbus', 'A320', 180),
    ('AV002', 2,'ATR',    'ATR72', 70),
    ('AV003', 3,'Boeing', '737',  160),
    ('AV004', 4,'Boeing', '757',  200),
    ('AV005', 5,'Boeing', '767',  220),
    ('AV006', 6,'Boeing', '737',  150),
    ('AV007', 7,'Airbus', 'A321', 190),
    ('AV008', 8,'Airbus', 'A320', 180),
    ('AV009', 9,'Boeing', '737',  165),
    ('AV010',10,'Airbus', 'A319', 140),
    ('AV011',11,'Boeing', '787',  250),
    ('AV012',12,'Airbus', 'A330', 260),
    ('AV013',13,'Airbus', 'A320', 170),
    ('AV014',14,'Airbus', 'A340', 270),
    ('AV015',15,'Airbus', 'A321', 185),
    ('AV016',16,'Boeing', '777',  300),
    ('AV017',17,'Embraer','E190', 100),
    ('AV018',18,'Airbus', 'A320', 175),
    ('AV019',19,'Airbus', 'A320', 180),
    ('AV020',20,'Boeing', '737',  160)
) AS f(idavion, idaerolinea, fabricante, tipo, capacidad);
GO

-- ------------------------------------------------------------- tarifa -----
-- El precio final de un asiento sale de: precio_base del vuelo × multiplicador
-- de la clase. Cambiar el precio de una ruta es cambiar un solo número.
IF NOT EXISTS (SELECT 1 FROM dbo.tarifa)
INSERT INTO dbo.tarifa (idtarifa, clase, multiplicador, impuesto, equipaje_incluido_kg) VALUES
(1,'Económica',     1.00, 0.15, 20),
(2,'Ejecutiva',     2.60, 0.15, 35),
(3,'Primera Clase', 4.20, 0.15, 50);
GO

-- -------------------------------------------------------- metodo_pago -----
IF NOT EXISTS (SELECT 1 FROM dbo.metodo_pago)
INSERT INTO dbo.metodo_pago (idmetodopago, nombre) VALUES
(1,'Efectivo'), (2,'Tarjeta de crédito'), (3,'Tarjeta de débito'),
(4,'Transferencia bancaria'), (5,'Millas ALAS');
GO

-- ------------------------------------------------------------ asiento -----
-- Mapa de asientos real de cada avión, generado sin cursores: una tabla de
-- números se cruza con las letras de columna y con la capacidad de cada avión.
IF NOT EXISTS (SELECT 1 FROM dbo.asiento)
BEGIN
    WITH Numeros AS (
        SELECT TOP (600) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects
    ),
    Letras AS (
        SELECT * FROM (VALUES (1,'A'),(2,'B'),(3,'C'),(4,'D'),
                              (5,'E'),(6,'F'),(7,'G'),(8,'H')) AS L(pos, letra)
    ),
    Puestos AS (
        SELECT av.idavion,
               av.capacidad_pasajeros,
               ((n.n - 1) / av.asientos_por_fila) + 1 AS fila,
               l.letra
        FROM dbo.avion av
        JOIN Numeros n ON n.n <= av.capacidad_pasajeros
        JOIN Letras  l ON l.pos = ((n.n - 1) % av.asientos_por_fila) + 1
    )
    INSERT INTO dbo.asiento (idavion, fila, letra, clase)
    SELECT idavion, fila, letra,
           CASE
               -- Las aeronaves grandes llevan las tres clases; las regionales solo dos.
               WHEN capacidad_pasajeros >= 150 AND fila <= 2 THEN 'Primera Clase'
               WHEN fila <= CASE WHEN capacidad_pasajeros >= 150 THEN 5 ELSE 2 END THEN 'Ejecutiva'
               ELSE 'Económica'
           END
    FROM Puestos;
END
GO

-- ----------------------------------------------------------- pasajero -----
IF NOT EXISTS (SELECT 1 FROM dbo.pasajero)
INSERT INTO dbo.pasajero (idpasajero, nombre_p, apaterno, amaterno, tipo_documento,
                          num_documento, fecha_nacimiento, idpais, telefono, email) VALUES
('P0000001','Juan',    'Lopez',    'Martinez', 'DNI',       '0801199912345','1999-05-10','HN01','98765432','juan.lopez@gmail.com'),
('P0000002','Maria',   'Hernandez','Flores',   'DNI',       '0801199812345','1998-07-12','HN01','99887766','maria.h@gmail.com'),
('P0000003','Carlos',  'Mejia',    'Ramos',    'DNI',       '0801199712345','1997-02-20','HN01','91234567','carlos.m@gmail.com'),
('P0000004','Ana',     'Castro',   'Lopez',    'DNI',       '0801199612345','1996-09-18','HN01','92345678','ana.c@gmail.com'),
('P0000005','Luis',    'Pineda',   'Suazo',    'DNI',       '0801199512345','1995-11-25','HN01','93456789','luis.p@gmail.com'),
('P0000006','Jose',    'Ramirez',  'Cruz',     'DNI',       '0801199412345','1994-01-30','HN01','94567890','jose.r@gmail.com'),
('P0000007','Karla',   'Gomez',    'Rivas',    'DNI',       '0801199312345','1993-03-14','HN01','95678901','karla.g@gmail.com'),
('P0000008','Miguel',  'Alvarado', 'Mora',     'DNI',       '0801199212345','1992-06-22','HN01','96789012','miguel.a@gmail.com'),
('P0000009','Sofia',   'Torres',   'Vega',     'DNI',       '0801199112345','1991-08-19','HN01','97890123','sofia.t@gmail.com'),
('P0000010','Daniel',  'Flores',   'Castillo', 'DNI',       '0801199012345','1990-12-02','HN01','98901234','daniel.f@gmail.com'),
('P0000011','Andrea',  'Murillo',  'Santos',   'Pasaporte', 'E1234567',     '1995-04-05','GT01','90123456','andrea.m@gmail.com'),
('P0000012','Pedro',   'Diaz',     'Lopez',    'Pasaporte', 'E1234568',     '1989-07-11','SV01','90234567','pedro.d@gmail.com'),
('P0000013','Laura',   'Perez',    'Cruz',     'Pasaporte', 'E1234569',     '1993-10-09','CR01','90345678','laura.p@gmail.com'),
('P0000014','Fernando','Rojas',    'Diaz',     'Pasaporte', 'E1234570',     '1988-01-21','PA01','90456789','fernando.r@gmail.com'),
('P0000015','Valeria', 'Morales',  'Suarez',   'Pasaporte', 'E1234571',     '1996-02-17','MX01','90567890','valeria.m@gmail.com'),
('P0000016','Kevin',   'Castillo', 'Ruiz',     'Pasaporte', 'E1234572',     '1997-03-29','US01','90678901','kevin.c@gmail.com'),
('P0000017','Diana',   'Mendoza',  'Lopez',    'Pasaporte', 'E1234573',     '1994-06-13','CO01','90789012','diana.m@gmail.com'),
('P0000018','Jorge',   'Navarro',  'Perez',    'Pasaporte', 'E1234574',     '1992-09-08','PE01','90890123','jorge.n@gmail.com'),
('P0000019','Paola',   'Reyes',    'Flores',   'Pasaporte', 'E1234575',     '1991-11-27','ES01','90901234','paola.r@gmail.com'),
('P0000020','Ricardo', 'Suazo',    'Martinez', 'Pasaporte', 'E1234576',     '1987-05-16','CA01','91012345','ricardo.s@gmail.com');
GO

-- -------------------------------------------------------------- vuelo -----
-- Malla de vuelos: 14 rutas × 14 días alrededor de hoy. Al ser relativa a
-- GETDATE(), el sistema siempre tiene vuelos pasados (historial), vuelos de
-- hoy (tablero de llegadas y salidas) y vuelos futuros (para reservar).
IF NOT EXISTS (SELECT 1 FROM dbo.vuelo)
BEGIN
    DECLARE @hoy DATE = CAST(GETDATE() AS DATE);

    ;WITH Rutas AS (
        SELECT * FROM (VALUES
            -- aerolínea, avión, origen, destino, número, hora salida (min), duración (min), precio base
            ( 1,'AV001','A0001','A0006', 101,  6*60+15,  75, 1850.00),
            ( 1,'AV001','A0006','A0001', 102,  9*60+00,  75, 1850.00),
            ( 2,'AV002','A0003','A0005', 201,  7*60+30,  55, 1250.00),
            ( 2,'AV002','A0005','A0003', 202,  9*60+30,  55, 1250.00),
            ( 6,'AV006','A0001','A0010', 301, 11*60+20, 140, 3400.00),
            ( 3,'AV003','A0003','A0012', 401,  8*60+45, 155, 4200.00),
            ( 3,'AV003','A0012','A0003', 402, 13*60+10, 160, 4200.00),
            ( 5,'AV005','A0002','A0013', 501, 10*60+00, 320, 7900.00),
            ( 9,'AV009','A0001','A0011', 601, 15*60+40, 165, 3900.00),
            (10,'AV010','A0003','A0014', 701, 12*60+15, 175, 4600.00),
            (12,'AV012','A0009','A0018', 801, 18*60+30, 600,12500.00),
            (17,'AV017','A0001','A0004', 901, 16*60+20,  45,  980.00),
            (17,'AV017','A0004','A0001', 902, 17*60+40,  45,  980.00),
            ( 7,'AV007','A0003','A0007',1001, 14*60+05,  60, 1650.00)
        ) AS R(idaerolinea, idavion, origen, destino, numero, min_salida, duracion, precio_base)
    ),
    Dias AS (
        SELECT TOP (14) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 4 AS desfase
        FROM sys.all_objects
    )
    INSERT INTO dbo.vuelo (codigo_vuelo, idaerolinea, idavion, idaeropuerto_origen,
                           idaeropuerto_destino, fecha_salida, fecha_llegada,
                           precio_base, estado, puerta)
    SELECT
        al.codigo + CAST(r.numero AS VARCHAR(4)) + '-' + FORMAT(DATEADD(DAY, d.desfase, @hoy), 'ddMM'),
        r.idaerolinea, r.idavion, r.origen, r.destino,
        DATEADD(MINUTE, r.min_salida, CAST(DATEADD(DAY, d.desfase, @hoy) AS DATETIME)),
        DATEADD(MINUTE, r.min_salida + r.duracion, CAST(DATEADD(DAY, d.desfase, @hoy) AS DATETIME)),
        r.precio_base,
        'Programado',
        CHAR(65 + (r.numero % 4)) + CAST((r.numero % 8) + 1 AS VARCHAR(1))
    FROM Rutas r
    JOIN Dias  d ON 1 = 1
    JOIN dbo.aerolinea al ON al.idaerolinea = r.idaerolinea;

    -- El estado se deduce del reloj: lo que ya llegó está aterrizado, lo que
    -- está en el aire va "En vuelo", lo próximo a salir queda "Abordando".
    UPDATE dbo.vuelo SET estado =
        CASE
            WHEN fecha_llegada < GETDATE()                                        THEN 'Aterrizado'
            WHEN fecha_salida  <= GETDATE()                                       THEN 'En vuelo'
            WHEN fecha_salida  <= DATEADD(MINUTE, 45, GETDATE())                  THEN 'Abordando'
            ELSE 'Programado'
        END;

    -- Un par de retrasos y una cancelación para que el tablero tenga alertas
    UPDATE dbo.vuelo SET estado = 'Retrasado'
    WHERE estado = 'Programado' AND idvuelo % 17 = 0;

    UPDATE dbo.vuelo SET estado = 'Cancelado'
    WHERE estado = 'Programado' AND idvuelo % 41 = 0;
END
GO

-- ------------------------------------------ reservas, boletos y pagos -----
-- 40 reservas de ejemplo repartidas en los últimos 10 días, para que el panel
-- de control, el gráfico de ingresos y el historial tengan datos reales.
IF NOT EXISTS (SELECT 1 FROM dbo.reserva)
BEGIN
    DECLARE @abc      CHAR(32) = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    DECLARE @i        INT = 1;
    DECLARE @maximo   INT = 40;
    DECLARE @vueltas  INT = 0;   -- freno de seguridad: el bucle nunca es infinito
    DECLARE @idvuelo  INT, @idavion CHAR(5), @precioBase DECIMAL(10,2);
    -- @pnr es VARCHAR y no CHAR: un CHAR(6) se rellena con espacios y la
    -- concatenación letra por letra se truncaría siempre al mismo valor.
    DECLARE @pasajero CHAR(8), @pnr VARCHAR(6), @idreserva INT;
    DECLARE @asientos INT, @k INT, @idasiento INT, @idtarifa INT;
    DECLARE @precio DECIMAL(10,2), @imp DECIMAL(10,2);
    DECLARE @sub DECIMAL(10,2), @impTotal DECIMAL(10,2);
    DECLARE @fecha DATETIME;

    WHILE @i <= @maximo AND @vueltas < 500
    BEGIN
        SET @vueltas += 1;

        -- Un vuelo distinto en cada vuelta, de los que ya salieron o salen pronto
        SELECT TOP 1 @idvuelo = v.idvuelo, @idavion = v.idavion, @precioBase = v.precio_base
        FROM dbo.vuelo v
        WHERE v.estado <> 'Cancelado'
        ORDER BY ABS(CHECKSUM(v.idvuelo * 7919 + @i * 104729)) % 1000;

        SET @pasajero = 'P' + RIGHT('0000000' + CAST(((@i * 3) % 20) + 1 AS VARCHAR(7)), 7);
        SET @fecha    = DATEADD(HOUR, -(@i * 6), GETDATE());

        -- Localizador de 6 caracteres, sin letras que se confundan con números
        SET @pnr = '';
        SET @k = 1;
        WHILE @k <= 6
        BEGIN
            SET @pnr = @pnr + SUBSTRING(@abc, ABS(CHECKSUM(NEWID())) % 32 + 1, 1);
            SET @k += 1;
        END

        IF EXISTS (SELECT 1 FROM dbo.reserva WHERE codigo_reserva = @pnr)
        BEGIN
            CONTINUE;   -- colisión de PNR: se vuelve a intentar
        END

        INSERT INTO dbo.reserva (codigo_reserva, idpasajero, fecha, estado,
                                 subtotal, impuesto, costo, observacion, usuario_registra)
        VALUES (@pnr, @pasajero, @fecha, 'Pendiente de pago', 0, 0, 0, NULL, 'semilla');

        SET @idreserva = SCOPE_IDENTITY();
        SET @sub = 0;
        SET @impTotal = 0;

        -- Una o dos personas por reserva
        SET @asientos = CASE WHEN @i % 3 = 0 THEN 2 ELSE 1 END;
        SET @k = 1;
        WHILE @k <= @asientos
        BEGIN
            SELECT TOP 1 @idasiento = a.idasiento,
                         @idtarifa  = t.idtarifa,
                         @precio    = ROUND(@precioBase * t.multiplicador, 2),
                         @imp       = ROUND(@precioBase * t.multiplicador * t.impuesto, 2)
            FROM dbo.asiento a
            JOIN dbo.tarifa  t ON t.clase = a.clase
            WHERE a.idavion = @idavion
              AND NOT EXISTS (SELECT 1 FROM dbo.boleto b
                              WHERE b.idvuelo = @idvuelo AND b.idasiento = a.idasiento
                                AND b.estado <> 'Cancelado')
            ORDER BY a.fila, a.letra;

            IF @@ROWCOUNT = 0 BREAK;   -- vuelo lleno

            INSERT INTO dbo.boleto (idreserva, idvuelo, idasiento, idpasajero, idtarifa,
                                    precio, impuesto, total, estado)
            VALUES (@idreserva, @idvuelo, @idasiento, @pasajero, @idtarifa,
                    @precio, @imp, @precio + @imp, 'Emitido');

            SET @sub      = @sub + @precio;
            SET @impTotal = @impTotal + @imp;
            SET @k += 1;
        END

        UPDATE dbo.reserva
        SET subtotal = @sub, impuesto = @impTotal, costo = @sub + @impTotal
        WHERE idreserva = @idreserva;

        -- 4 de cada 5 reservas ya están pagadas
        IF @i % 5 <> 0 AND @sub > 0
        BEGIN
            INSERT INTO dbo.pago (idreserva, idpasajero, idmetodopago, fecha, monto, impuesto,
                                  tipo_comprobante, num_comprobante, usuario_registra)
            VALUES (@idreserva, @pasajero, (@i % 5) + 1, @fecha, @sub + @impTotal, @impTotal,
                    CASE WHEN @i % 4 = 0 THEN 'Recibo' ELSE 'Factura' END,
                    CASE WHEN @i % 4 = 0 THEN 'R001-' ELSE 'F001-' END
                        + RIGHT('0000' + CAST(@i AS VARCHAR(4)), 4),
                    'semilla');

            UPDATE dbo.reserva SET estado = 'Confirmada' WHERE idreserva = @idreserva;
        END

        SET @i += 1;
    END

    -- Los pasajeros de vuelos que ya despegaron aparecen como abordados
    UPDATE b SET estado = 'Abordado', fecha_checkin = DATEADD(HOUR, -2, v.fecha_salida)
    FROM dbo.boleto b
    JOIN dbo.vuelo  v ON v.idvuelo = b.idvuelo
    JOIN dbo.reserva r ON r.idreserva = b.idreserva
    WHERE v.fecha_salida < GETDATE() AND r.estado = 'Confirmada' AND b.estado = 'Emitido';
END
GO

PRINT 'ALAS · 02_datos_semilla.sql aplicado correctamente.';
GO

SELECT 'pais' AS tabla, COUNT(*) AS filas FROM dbo.pais
UNION ALL SELECT 'aeropuerto',  COUNT(*) FROM dbo.aeropuerto
UNION ALL SELECT 'aerolinea',   COUNT(*) FROM dbo.aerolinea
UNION ALL SELECT 'avion',       COUNT(*) FROM dbo.avion
UNION ALL SELECT 'asiento',     COUNT(*) FROM dbo.asiento
UNION ALL SELECT 'tarifa',      COUNT(*) FROM dbo.tarifa
UNION ALL SELECT 'pasajero',    COUNT(*) FROM dbo.pasajero
UNION ALL SELECT 'vuelo',       COUNT(*) FROM dbo.vuelo
UNION ALL SELECT 'reserva',     COUNT(*) FROM dbo.reserva
UNION ALL SELECT 'boleto',      COUNT(*) FROM dbo.boleto
UNION ALL SELECT 'pago',        COUNT(*) FROM dbo.pago;
GO
