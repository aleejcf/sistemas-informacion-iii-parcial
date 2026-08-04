-- ============================================================================
--  ALAS Honduras — Sistema de Reserva de Vuelos
--  Script 05: corrección de acentos
--  Base de datos: dbreserva_vuelos
-- ============================================================================
--  POR QUÉ EXISTE ESTE SCRIPT
--
--  Los archivos .sql de este proyecto están en UTF-8. Si se ejecutan con una
--  herramienta que los lee como Windows-1252, cada letra acentuada entra a la
--  base de datos convertida en dos caracteres: "Bogotá" se guarda como
--  "BogotÃ¡" y "Económica" como "EconÃ³mica".
--
--  Desde esta versión los scripts llevan marca de orden de bytes (BOM), con lo
--  que SSMS y sqlcmd los interpretan bien por sí solos. Este script repara las
--  bases que se crearon ANTES de esa corrección.
--
--  Es idempotente y no hace daño: sobre una base ya correcta reescribe los
--  mismos valores. Las filas se localizan por su llave, nunca por su texto,
--  así que funciona igual esté el texto corrupto o no.
--
--  IMPORTANTE: `asiento.clase` y `tarifa.clase` se corrigen juntas. Las une el
--  JOIN de dbo.sp_mapa_asientos, y si una quedara con acento y la otra sin él,
--  el mapa de asientos dejaría de encontrar precios.
-- ============================================================================

USE dbreserva_vuelos;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------- países -----
UPDATE dbo.pais SET nombre_pais = 'México'               WHERE idpais = 'MX01';
UPDATE dbo.pais SET nombre_pais = 'Panamá'               WHERE idpais = 'PA01';
UPDATE dbo.pais SET nombre_pais = 'Perú'                 WHERE idpais = 'PE01';
UPDATE dbo.pais SET nombre_pais = 'Canadá'               WHERE idpais = 'CA01';
UPDATE dbo.pais SET nombre_pais = 'España'               WHERE idpais = 'ES01';
UPDATE dbo.pais SET nombre_pais = 'República Dominicana' WHERE idpais = 'DO01';
GO

-- -------------------------------------------------------- aeropuertos -----
UPDATE dbo.aeropuerto SET nombre = 'Toncontín',              ciudad = 'Tegucigalpa'         WHERE idaeropuerto = 'A0001';
UPDATE dbo.aeropuerto SET nombre = 'Palmerola',              ciudad = 'Comayagua'           WHERE idaeropuerto = 'A0002';
UPDATE dbo.aeropuerto SET nombre = 'Ramón Villeda Morales',  ciudad = 'San Pedro Sula'      WHERE idaeropuerto = 'A0003';
UPDATE dbo.aeropuerto SET nombre = 'Golosón',                ciudad = 'La Ceiba'            WHERE idaeropuerto = 'A0004';
UPDATE dbo.aeropuerto SET nombre = 'Juan Manuel Gálvez',     ciudad = 'Roatán'              WHERE idaeropuerto = 'A0005';
UPDATE dbo.aeropuerto SET nombre = 'La Aurora',              ciudad = 'Ciudad de Guatemala' WHERE idaeropuerto = 'A0006';
UPDATE dbo.aeropuerto SET nombre = 'Monseñor Romero',        ciudad = 'San Salvador'        WHERE idaeropuerto = 'A0007';
UPDATE dbo.aeropuerto SET nombre = 'Augusto C. Sandino',     ciudad = 'Managua'             WHERE idaeropuerto = 'A0008';
UPDATE dbo.aeropuerto SET nombre = 'Juan Santamaría',        ciudad = 'San José'            WHERE idaeropuerto = 'A0009';
UPDATE dbo.aeropuerto SET nombre = 'Tocumen',                ciudad = 'Ciudad de Panamá'    WHERE idaeropuerto = 'A0010';
UPDATE dbo.aeropuerto SET nombre = 'Benito Juárez',          ciudad = 'Ciudad de México'    WHERE idaeropuerto = 'A0011';
UPDATE dbo.aeropuerto SET nombre = 'Miami International',    ciudad = 'Miami'               WHERE idaeropuerto = 'A0012';
UPDATE dbo.aeropuerto SET nombre = 'Los Angeles International', ciudad = 'Los Ángeles'      WHERE idaeropuerto = 'A0013';
UPDATE dbo.aeropuerto SET nombre = 'El Dorado',              ciudad = 'Bogotá'              WHERE idaeropuerto = 'A0014';
UPDATE dbo.aeropuerto SET nombre = 'Jorge Chávez',           ciudad = 'Lima'                WHERE idaeropuerto = 'A0015';
UPDATE dbo.aeropuerto SET nombre = 'Ezeiza',                 ciudad = 'Buenos Aires'        WHERE idaeropuerto = 'A0016';
UPDATE dbo.aeropuerto SET nombre = 'Arturo Merino Benítez',  ciudad = 'Santiago'            WHERE idaeropuerto = 'A0017';
UPDATE dbo.aeropuerto SET nombre = 'Madrid Barajas',         ciudad = 'Madrid'              WHERE idaeropuerto = 'A0018';
UPDATE dbo.aeropuerto SET nombre = 'Charles de Gaulle',      ciudad = 'París'               WHERE idaeropuerto = 'A0019';
UPDATE dbo.aeropuerto SET nombre = 'Frankfurt Airport',      ciudad = 'Fráncfort'           WHERE idaeropuerto = 'A0020';
GO

-- --------------------------------------------------- métodos de pago -----
UPDATE dbo.metodo_pago SET nombre = 'Efectivo'               WHERE idmetodopago = 1;
UPDATE dbo.metodo_pago SET nombre = 'Tarjeta de crédito'     WHERE idmetodopago = 2;
UPDATE dbo.metodo_pago SET nombre = 'Tarjeta de débito'      WHERE idmetodopago = 3;
UPDATE dbo.metodo_pago SET nombre = 'Transferencia bancaria' WHERE idmetodopago = 4;
UPDATE dbo.metodo_pago SET nombre = 'Millas ALAS'            WHERE idmetodopago = 5;
GO

-- -------------------------------------------- clases: tarifa y asiento -----
-- El orden importa: primero los asientos (que se localizan por el texto viejo)
-- y después la tarifa, que se localiza por su identificador.
UPDATE dbo.asiento SET clase = 'Económica'
 WHERE clase <> 'Ejecutiva' AND clase <> 'Primera Clase';
GO

UPDATE dbo.tarifa SET clase = 'Económica'     WHERE idtarifa = 1;
UPDATE dbo.tarifa SET clase = 'Ejecutiva'     WHERE idtarifa = 2;
UPDATE dbo.tarifa SET clase = 'Primera Clase' WHERE idtarifa = 3;
GO

-- ------------------------------------------------------ comprobación -----
-- Ninguna fila debería contener ya la secuencia 'Ã', que es la huella de la
-- corrupción. Si el conteo no da cero, algo quedó sin corregir.
SELECT 'pais'       AS tabla, COUNT(*) AS filas_con_problema FROM dbo.pais       WHERE nombre_pais LIKE '%Ã%'
UNION ALL SELECT 'aeropuerto.nombre', COUNT(*) FROM dbo.aeropuerto WHERE nombre  LIKE '%Ã%'
UNION ALL SELECT 'aeropuerto.ciudad', COUNT(*) FROM dbo.aeropuerto WHERE ciudad  LIKE '%Ã%'
UNION ALL SELECT 'tarifa',            COUNT(*) FROM dbo.tarifa     WHERE clase   LIKE '%Ã%'
UNION ALL SELECT 'asiento',           COUNT(*) FROM dbo.asiento    WHERE clase   LIKE '%Ã%'
UNION ALL SELECT 'metodo_pago',       COUNT(*) FROM dbo.metodo_pago WHERE nombre LIKE '%Ã%'
-- La flecha de la columna `ruta` vive dentro de la vista, no en una tabla:
-- si aparece corrupta, hay que volver a ejecutar 03_vistas_indices_procedimientos.sql
UNION ALL SELECT 'v_vuelo_detalle.ruta', COUNT(*) FROM dbo.v_vuelo_detalle WHERE ruta LIKE '%â%';
GO

PRINT 'ALAS · 05_corregir_acentos.sql aplicado correctamente.';
GO
