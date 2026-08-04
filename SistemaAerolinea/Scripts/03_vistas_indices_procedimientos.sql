-- ============================================================================
--  ALAS Honduras — Sistema de Reserva de Vuelos
--  Script 03: índices, vistas y procedimientos almacenados
--  Base de datos: dbreserva_vuelos
-- ============================================================================
--  Los índices aceleran las tres búsquedas que el sistema hace todo el tiempo:
--  vuelos por ruta y fecha, asientos ocupados de un vuelo, y boletos de una
--  reserva. Las vistas concentran los JOIN que de otro modo se repetirían en
--  cada servicio de VB, y los procedimientos dejan en el servidor la lógica de
--  consulta del panel de control y del buscador de vuelos.
--
--  Idempotente.
-- ============================================================================

USE dbreserva_vuelos;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
--  ÍNDICES
-- ============================================================================

-- Buscar vuelos por ruta y día: la consulta más usada del sistema
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_vuelo_ruta_fecha')
    CREATE NONCLUSTERED INDEX IX_vuelo_ruta_fecha
        ON dbo.vuelo (idaeropuerto_origen, idaeropuerto_destino, fecha_salida)
        INCLUDE (estado, precio_base, idavion);
GO

-- Tablero de llegadas y salidas del día
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_vuelo_fecha_salida')
    CREATE NONCLUSTERED INDEX IX_vuelo_fecha_salida
        ON dbo.vuelo (fecha_salida) INCLUDE (estado, codigo_vuelo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_vuelo_fecha_llegada')
    CREATE NONCLUSTERED INDEX IX_vuelo_fecha_llegada
        ON dbo.vuelo (fecha_llegada) INCLUDE (estado, codigo_vuelo);
GO

-- Saber qué asientos de un vuelo ya están vendidos (mapa de asientos)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_boleto_vuelo_estado')
    CREATE NONCLUSTERED INDEX IX_boleto_vuelo_estado
        ON dbo.boleto (idvuelo, estado) INCLUDE (idasiento, idpasajero);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_boleto_reserva')
    CREATE NONCLUSTERED INDEX IX_boleto_reserva ON dbo.boleto (idreserva);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_boleto_pasajero')
    CREATE NONCLUSTERED INDEX IX_boleto_pasajero ON dbo.boleto (idpasajero);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_reserva_pasajero')
    CREATE NONCLUSTERED INDEX IX_reserva_pasajero ON dbo.reserva (idpasajero);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_reserva_fecha')
    CREATE NONCLUSTERED INDEX IX_reserva_fecha ON dbo.reserva (fecha DESC) INCLUDE (estado, costo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_pago_fecha')
    CREATE NONCLUSTERED INDEX IX_pago_fecha ON dbo.pago (fecha DESC) INCLUDE (monto, idreserva);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_pago_reserva')
    CREATE NONCLUSTERED INDEX IX_pago_reserva ON dbo.pago (idreserva);
GO

-- Búsqueda de pasajeros por apellido o documento
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_pasajero_nombre')
    CREATE NONCLUSTERED INDEX IX_pasajero_nombre ON dbo.pasajero (apaterno, nombre_p);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_asiento_avion')
    CREATE NONCLUSTERED INDEX IX_asiento_avion ON dbo.asiento (idavion, fila, letra) INCLUDE (clase);
GO

-- ============================================================================
--  VISTAS
--  Concentran los JOIN que de otro modo se repetirían en cada servicio.
-- ============================================================================

-- Un vuelo con todo lo que la interfaz necesita mostrar de él, incluida la
-- ocupación calculada contra los boletos vivos.
CREATE OR ALTER VIEW dbo.v_vuelo_detalle AS
SELECT
    v.idvuelo,
    v.codigo_vuelo,
    v.idaerolinea,
    al.nombre_aero,
    v.idavion,
    av.tipo                AS tipo_avion,
    av.fabricante,
    av.capacidad_pasajeros,
    av.asientos_por_fila,
    v.idaeropuerto_origen,
    ao.iata                AS iata_origen,
    ao.ciudad              AS ciudad_origen,
    ao.nombre              AS aeropuerto_origen,
    v.idaeropuerto_destino,
    ad.iata                AS iata_destino,
    ad.ciudad              AS ciudad_destino,
    ad.nombre              AS aeropuerto_destino,
    v.fecha_salida,
    v.fecha_llegada,
    DATEDIFF(MINUTE, v.fecha_salida, v.fecha_llegada) AS duracion_minutos,
    v.precio_base,
    v.estado,
    v.puerta,
    -- La flecha se escribe con NCHAR y no como literal: el carácter → no existe
    -- en Windows-1252, así que dentro de un literal VARCHAR se perdería (queda '?')
    -- por más que el archivo se lea con la codificación correcta.
    ao.iata + N' ' + NCHAR(8594) + N' ' + ad.iata AS ruta,
    ISNULL(oc.vendidos, 0)                        AS asientos_vendidos,
    av.capacidad_pasajeros - ISNULL(oc.vendidos, 0) AS asientos_disponibles,
    CAST(ISNULL(oc.vendidos, 0) * 100.0 / NULLIF(av.capacidad_pasajeros, 0) AS DECIMAL(5,2)) AS ocupacion
FROM dbo.vuelo v
JOIN dbo.aerolinea  al ON al.idaerolinea  = v.idaerolinea
JOIN dbo.avion      av ON av.idavion      = v.idavion
JOIN dbo.aeropuerto ao ON ao.idaeropuerto = v.idaeropuerto_origen
JOIN dbo.aeropuerto ad ON ad.idaeropuerto = v.idaeropuerto_destino
OUTER APPLY (
    SELECT COUNT(*) AS vendidos
    FROM dbo.boleto b
    WHERE b.idvuelo = v.idvuelo AND b.estado <> 'Cancelado'
) oc;
GO

-- Un boleto con el nombre del pasajero, el asiento y el vuelo: es lo que se
-- imprime en el pase de abordar y lo que se lista en el detalle de la reserva.
CREATE OR ALTER VIEW dbo.v_boleto_detalle AS
SELECT
    b.idboleto,
    b.idreserva,
    r.codigo_reserva,
    b.idvuelo,
    v.codigo_vuelo,
    v.fecha_salida,
    v.fecha_llegada,
    v.estado          AS estado_vuelo,
    v.puerta,
    ao.iata           AS iata_origen,
    ao.ciudad         AS ciudad_origen,
    ad.iata           AS iata_destino,
    ad.ciudad         AS ciudad_destino,
    al.nombre_aero,
    b.idpasajero,
    p.nombre_p + ' ' + p.apaterno + ISNULL(' ' + p.amaterno, '') AS pasajero,
    p.num_documento,
    b.idasiento,
    CAST(a.fila AS VARCHAR(3)) + a.letra AS asiento,
    a.clase,
    t.equipaje_incluido_kg,
    b.precio,
    b.impuesto,
    b.total,
    b.estado,
    b.fecha_checkin
FROM dbo.boleto     b
JOIN dbo.reserva    r  ON r.idreserva     = b.idreserva
JOIN dbo.vuelo      v  ON v.idvuelo       = b.idvuelo
JOIN dbo.aerolinea  al ON al.idaerolinea  = v.idaerolinea
JOIN dbo.aeropuerto ao ON ao.idaeropuerto = v.idaeropuerto_origen
JOIN dbo.aeropuerto ad ON ad.idaeropuerto = v.idaeropuerto_destino
JOIN dbo.pasajero   p  ON p.idpasajero    = b.idpasajero
JOIN dbo.asiento    a  ON a.idasiento     = b.idasiento
JOIN dbo.tarifa     t  ON t.idtarifa      = b.idtarifa;
GO

-- Una reserva con su titular, cuántos boletos tiene y cuánto lleva pagado.
CREATE OR ALTER VIEW dbo.v_reserva_resumen AS
SELECT
    r.idreserva,
    r.codigo_reserva,
    r.idpasajero,
    p.nombre_p + ' ' + p.apaterno + ISNULL(' ' + p.amaterno, '') AS titular,
    p.num_documento,
    p.email,
    p.telefono,
    r.fecha,
    r.estado,
    r.subtotal,
    r.impuesto,
    r.costo,
    r.observacion,
    r.usuario_registra,
    ISNULL(bo.boletos, 0)  AS boletos,
    ISNULL(pg.pagado, 0)   AS pagado,
    r.costo - ISNULL(pg.pagado, 0) AS saldo,
    bo.itinerario
FROM dbo.reserva  r
JOIN dbo.pasajero p ON p.idpasajero = r.idpasajero
OUTER APPLY (
    SELECT COUNT(*) AS boletos,
           MAX(v.codigo_vuelo) AS itinerario
    FROM dbo.boleto b
    JOIN dbo.vuelo  v ON v.idvuelo = b.idvuelo
    WHERE b.idreserva = r.idreserva AND b.estado <> 'Cancelado'
) bo
OUTER APPLY (
    SELECT SUM(pa.monto) AS pagado
    FROM dbo.pago pa
    WHERE pa.idreserva = r.idreserva
) pg;
GO

-- ============================================================================
--  PROCEDIMIENTOS ALMACENADOS
-- ============================================================================

-- Buscador de vuelos: origen, destino y fecha son opcionales; solo devuelve
-- vuelos que todavía tienen asientos y que no están cancelados.
CREATE OR ALTER PROCEDURE dbo.sp_buscar_vuelos
    @origen  CHAR(5) = NULL,
    @destino CHAR(5) = NULL,
    @fecha   DATE    = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.v_vuelo_detalle
    WHERE (@origen  IS NULL OR idaeropuerto_origen  = @origen)
      AND (@destino IS NULL OR idaeropuerto_destino = @destino)
      AND (@fecha   IS NULL OR CAST(fecha_salida AS DATE) = @fecha)
      AND estado NOT IN ('Cancelado', 'Aterrizado')
      AND fecha_salida > GETDATE()
      AND asientos_disponibles > 0
    ORDER BY fecha_salida;
END
GO

-- Mapa de asientos de un vuelo: todos los asientos del avión, marcando cuáles
-- están ocupados y con qué precio se vende cada clase en ESTE vuelo.
CREATE OR ALTER PROCEDURE dbo.sp_mapa_asientos
    @idvuelo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.idasiento,
        a.fila,
        a.letra,
        CAST(a.fila AS VARCHAR(3)) + a.letra AS etiqueta,
        a.clase,
        t.idtarifa,
        ROUND(v.precio_base * t.multiplicador, 2)                     AS precio,
        ROUND(v.precio_base * t.multiplicador * t.impuesto, 2)        AS impuesto,
        ROUND(v.precio_base * t.multiplicador * (1 + t.impuesto), 2)  AS total,
        t.equipaje_incluido_kg,
        av.asientos_por_fila,
        CAST(CASE WHEN b.idboleto IS NULL THEN 0 ELSE 1 END AS BIT)   AS ocupado
    FROM dbo.vuelo   v
    JOIN dbo.avion   av ON av.idavion = v.idavion
    JOIN dbo.asiento a  ON a.idavion  = v.idavion
    JOIN dbo.tarifa  t  ON t.clase    = a.clase
    LEFT JOIN dbo.boleto b
           ON b.idvuelo = v.idvuelo AND b.idasiento = a.idasiento AND b.estado <> 'Cancelado'
    WHERE v.idvuelo = @idvuelo
    ORDER BY a.fila, a.letra;
END
GO

-- Tablero de llegadas y salidas de un día (por omisión, hoy)
CREATE OR ALTER PROCEDURE dbo.sp_itinerario
    @fecha DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @fecha = ISNULL(@fecha, CAST(GETDATE() AS DATE));

    SELECT *
    FROM dbo.v_vuelo_detalle
    WHERE CAST(fecha_salida  AS DATE) = @fecha
       OR CAST(fecha_llegada AS DATE) = @fecha
    ORDER BY fecha_salida;
END
GO

-- Indicadores del panel de control: una sola fila con todo lo del encabezado
CREATE OR ALTER PROCEDURE dbo.sp_panel_estadisticas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.vuelo
          WHERE CAST(fecha_salida AS DATE) = CAST(GETDATE() AS DATE))            AS vuelos_hoy,

        (SELECT COUNT(*) FROM dbo.vuelo
          WHERE fecha_salida BETWEEN GETDATE() AND DATEADD(HOUR, 24, GETDATE())
            AND estado NOT IN ('Cancelado','Aterrizado'))                        AS proximas_salidas,

        (SELECT COUNT(*) FROM dbo.reserva
          WHERE CAST(fecha AS DATE) = CAST(GETDATE() AS DATE)
            AND estado <> 'Cancelada')                                           AS reservas_hoy,

        (SELECT COUNT(*) FROM dbo.boleto b
           JOIN dbo.vuelo v ON v.idvuelo = b.idvuelo
          WHERE CAST(v.fecha_salida AS DATE) = CAST(GETDATE() AS DATE)
            AND b.estado <> 'Cancelado')                                         AS pasajeros_hoy,

        (SELECT ISNULL(SUM(monto), 0) FROM dbo.pago
          WHERE CAST(fecha AS DATE) = CAST(GETDATE() AS DATE))                   AS ingresos_hoy,

        (SELECT ISNULL(SUM(monto), 0) FROM dbo.pago
          WHERE fecha >= DATEADD(DAY, -30, GETDATE()))                           AS ingresos_mes,

        (SELECT COUNT(*) FROM dbo.reserva WHERE estado = 'Pendiente de pago')    AS reservas_pendientes,

        (SELECT COUNT(*) FROM dbo.vuelo
          WHERE estado IN ('Retrasado','Cancelado')
            AND fecha_salida >= CAST(GETDATE() AS DATE))                         AS alertas,

        (SELECT COUNT(*) FROM dbo.pasajero)                                      AS total_pasajeros,

        (SELECT CAST(ISNULL(AVG(ocupacion), 0) AS DECIMAL(5,2))
           FROM dbo.v_vuelo_detalle
          WHERE CAST(fecha_salida AS DATE) = CAST(GETDATE() AS DATE))            AS ocupacion_promedio_hoy;
END
GO

-- Ingresos por día para el gráfico del panel. Se genera la serie completa de
-- días para que los días sin ventas aparezcan en cero y no se salten en la barra.
CREATE OR ALTER PROCEDURE dbo.sp_panel_ingresos
    @dias INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Serie AS (
        SELECT TOP (@dias)
               CAST(DATEADD(DAY, -(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1), GETDATE()) AS DATE) AS dia
        FROM sys.all_objects
    )
    SELECT s.dia,
           FORMAT(s.dia, 'ddd dd')          AS etiqueta,
           ISNULL(SUM(p.monto), 0)          AS ingresos,
           COUNT(p.idpago)                  AS pagos
    FROM Serie s
    LEFT JOIN dbo.pago p ON CAST(p.fecha AS DATE) = s.dia
    GROUP BY s.dia
    ORDER BY s.dia;
END
GO

-- Rutas más vendidas: alimenta el bloque "rutas destacadas" del panel
CREATE OR ALTER PROCEDURE dbo.sp_panel_rutas_top
    @top INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@top)
           vd.ruta,
           vd.ciudad_origen + N' ' + NCHAR(8594) + N' ' + vd.ciudad_destino AS descripcion,
           COUNT(b.idboleto)        AS boletos,
           ISNULL(SUM(b.total), 0)  AS ingresos
    FROM dbo.v_vuelo_detalle vd
    JOIN dbo.boleto b ON b.idvuelo = vd.idvuelo AND b.estado <> 'Cancelado'
    GROUP BY vd.ruta, vd.ciudad_origen, vd.ciudad_destino
    ORDER BY COUNT(b.idboleto) DESC;
END
GO

-- Lista de pasajeros de un vuelo (para el embarque)
CREATE OR ALTER PROCEDURE dbo.sp_manifiesto_vuelo
    @idvuelo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idboleto, codigo_reserva, pasajero, num_documento, asiento, clase, estado, fecha_checkin
    FROM dbo.v_boleto_detalle
    WHERE idvuelo = @idvuelo AND estado <> 'Cancelado'
    ORDER BY asiento;
END
GO

PRINT 'ALAS · 03_vistas_indices_procedimientos.sql aplicado correctamente.';
GO
