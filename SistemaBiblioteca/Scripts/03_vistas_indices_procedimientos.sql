-- ============================================================================
--  ALEJANDRÍA — Sistema de Biblioteca
--  Script 03: vistas, índices y procedimientos almacenados
--  Base de datos: db_biblioteca
-- ============================================================================
--  Las vistas son el contrato entre la base de datos y la aplicación: la capa
--  de servicios consulta `v_libro_detalle` y no tiene que saber que detrás hay
--  cuatro JOIN y dos subconsultas. Si mañana cambia el esquema, cambia la vista
--  y el código de VB.NET no se toca.
--
--  Idempotente: CREATE OR ALTER en todo.
-- ============================================================================

USE db_biblioteca;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
--  ÍNDICES
-- ============================================================================
--  Uno por cada columna sobre la que el sistema busca o filtra de verdad.

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_libro_titulo')
    CREATE NONCLUSTERED INDEX IX_libro_titulo ON dbo.libro (titulo)
        INCLUDE (idautor, idcategoria, anio_publicacion);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_libro_categoria')
    CREATE NONCLUSTERED INDEX IX_libro_categoria ON dbo.libro (idcategoria);
GO

-- El catálogo pregunta constantemente "¿cuántas copias disponibles tiene este
-- título?": ese conteo se resuelve entero dentro de este índice.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ejemplar_libro_estado')
    CREATE NONCLUSTERED INDEX IX_ejemplar_libro_estado ON dbo.ejemplar (idlibro, estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_socio_nombre')
    CREATE NONCLUSTERED INDEX IX_socio_nombre ON dbo.socio (apellido, nombre)
        INCLUDE (email, telefono, idtipo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_prestamo_socio')
    CREATE NONCLUSTERED INDEX IX_prestamo_socio ON dbo.prestamo (idsocio, estado)
        INCLUDE (fecha_prestamo, fecha_vencimiento);
GO

-- Los préstamos vencidos se buscan por fecha de vencimiento entre los activos:
-- el índice filtrado solo indexa esas filas, que son una fracción del total.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_prestamo_vencimiento')
    CREATE NONCLUSTERED INDEX IX_prestamo_vencimiento ON dbo.prestamo (fecha_vencimiento)
        INCLUDE (idsocio, codigo) WHERE estado = 'Activo';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_detalle_prestamo')
    CREATE NONCLUSTERED INDEX IX_detalle_prestamo ON dbo.detalle_prestamo (idprestamo)
        INCLUDE (idejemplar, fecha_devolucion);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_multa_socio')
    CREATE NONCLUSTERED INDEX IX_multa_socio ON dbo.multa (idsocio, estado)
        INCLUDE (monto, fecha_generada);
GO

-- ============================================================================
--  VISTAS
-- ============================================================================

-- ------------------------------------------------------ v_libro_detalle -----
-- El catálogo tal como lo ve el bibliotecario: la ficha del título más el
-- estado real de su acervo. Aquí es donde `stock` se vuelve a calcular, pero
-- ahora es un conteo verdadero y no un número que alguien tiene que recordar
-- actualizar a mano.
CREATE OR ALTER VIEW dbo.v_libro_detalle AS
SELECT
    l.idlibro,
    l.isbn,
    l.titulo,
    l.idautor,
    a.nombre                AS autor,
    a.nacionalidad          AS nacionalidad_autor,
    l.ideditorial,
    ISNULL(e.nombre, '—')   AS editorial,
    l.idcategoria,
    c.nombre                AS categoria,
    c.codigo_dewey,
    l.anio_publicacion,
    l.edicion,
    l.idioma,
    l.sinopsis,
    l.fecha_alta,
    acervo.total_ejemplares,
    acervo.disponibles,
    acervo.prestados,
    acervo.no_circulan,
    reservas.reservas_activas,
    -- Etiqueta de una sola palabra para la insignia del catálogo
    CASE
        WHEN acervo.total_ejemplares = 0 THEN 'Sin ejemplares'
        WHEN acervo.disponibles > 0      THEN 'Disponible'
        WHEN acervo.prestados > 0        THEN 'Prestado'
        ELSE 'No circula'
    END AS disponibilidad,
    -- Porcentaje del acervo que está afuera; alimenta la barra del catálogo
    CASE WHEN acervo.total_ejemplares = 0 THEN 0
         ELSE CAST(ROUND(acervo.prestados * 100.0 / acervo.total_ejemplares, 0) AS INT)
    END AS porcentaje_prestado,
    l.titulo + ' — ' + a.nombre AS etiqueta
FROM dbo.libro l
JOIN dbo.autor a      ON a.idautor     = l.idautor
JOIN dbo.categoria c  ON c.idcategoria = l.idcategoria
LEFT JOIN dbo.editorial e ON e.ideditorial = l.ideditorial
CROSS APPLY (
    SELECT
        COUNT(*)                                                        AS total_ejemplares,
        SUM(CASE WHEN ej.estado = 'Disponible' THEN 1 ELSE 0 END)       AS disponibles,
        SUM(CASE WHEN ej.estado = 'Prestado'   THEN 1 ELSE 0 END)       AS prestados,
        SUM(CASE WHEN ej.estado IN ('Reparación','Extraviado','Baja')
                 THEN 1 ELSE 0 END)                                     AS no_circulan
    FROM dbo.ejemplar ej WHERE ej.idlibro = l.idlibro
) AS acervo
CROSS APPLY (
    SELECT COUNT(*) AS reservas_activas
    FROM dbo.reserva r WHERE r.idlibro = l.idlibro AND r.estado = 'Activa'
) AS reservas;
GO

-- --------------------------------------------------- v_ejemplar_detalle -----
-- Cada copia física con su ficha y, si está prestada, con quién anda y hasta
-- cuándo. Es lo que se ve al abrir un título en el catálogo.
CREATE OR ALTER VIEW dbo.v_ejemplar_detalle AS
SELECT
    ej.idejemplar,
    ej.codigo_barras,
    ej.idlibro,
    l.titulo,
    a.nombre        AS autor,
    ej.ubicacion,
    ej.estado,
    ej.condicion,
    ej.fecha_adquisicion,
    p.codigo        AS prestamo_actual,
    p.idsocio,
    s.nombre + ' ' + s.apellido AS socio_actual,
    p.fecha_vencimiento,
    -- Días de mora de ESTE ejemplar; 0 si está en la estantería o al día
    CASE WHEN p.idprestamo IS NULL THEN 0
         ELSE CASE WHEN DATEDIFF(DAY, p.fecha_vencimiento, CAST(GETDATE() AS DATE)) > 0
                   THEN DATEDIFF(DAY, p.fecha_vencimiento, CAST(GETDATE() AS DATE))
                   ELSE 0 END
    END AS dias_retraso
FROM dbo.ejemplar ej
JOIN dbo.libro l ON l.idlibro = ej.idlibro
JOIN dbo.autor a ON a.idautor = l.idautor
-- El renglón de préstamo abierto, si lo hay. El índice único filtrado garantiza
-- que sea a lo sumo uno, así que el LEFT JOIN no puede duplicar filas.
LEFT JOIN dbo.detalle_prestamo d ON d.idejemplar = ej.idejemplar AND d.fecha_devolucion IS NULL
LEFT JOIN dbo.prestamo p ON p.idprestamo = d.idprestamo
LEFT JOIN dbo.socio s    ON s.idsocio    = p.idsocio;
GO

-- --------------------------------------------------- v_prestamo_detalle -----
-- La cabecera del préstamo con todo lo que el mostrador necesita saber de un
-- vistazo: de quién es, cuántos ejemplares lleva, cuántos ya volvieron y en qué
-- semáforo está.
CREATE OR ALTER VIEW dbo.v_prestamo_detalle AS
SELECT
    p.idprestamo,
    p.codigo,
    p.idsocio,
    s.nombre + ' ' + s.apellido AS socio,
    s.email,
    s.telefono,
    t.nombre        AS tipo_socio,
    t.multa_diaria,
    t.dias_prestamo,
    p.fecha_prestamo,
    p.fecha_vencimiento,
    p.fecha_devolucion,
    p.estado,
    p.renovaciones,
    p.usuario_registra,
    p.observacion,
    renglones.total_ejemplares,
    renglones.devueltos,
    renglones.pendientes,
    renglones.titulos,
    -- Días de mora: si ya se devolvió, los que se tardó de más; si sigue
    -- afuera, los que lleva vencido hasta hoy.
    CASE
        WHEN p.estado = 'Cancelado' THEN 0
        WHEN p.fecha_devolucion IS NOT NULL THEN
            CASE WHEN DATEDIFF(DAY, p.fecha_vencimiento, CAST(p.fecha_devolucion AS DATE)) > 0
                 THEN DATEDIFF(DAY, p.fecha_vencimiento, CAST(p.fecha_devolucion AS DATE))
                 ELSE 0 END
        ELSE
            CASE WHEN DATEDIFF(DAY, p.fecha_vencimiento, CAST(GETDATE() AS DATE)) > 0
                 THEN DATEDIFF(DAY, p.fecha_vencimiento, CAST(GETDATE() AS DATE))
                 ELSE 0 END
    END AS dias_retraso,
    -- Días que faltan para vencer (negativo si ya venció)
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), p.fecha_vencimiento) AS dias_restantes,
    -- Semáforo: es lo que colorea la lista y el panel
    CASE
        WHEN p.estado = 'Cancelado' THEN 'Cancelado'
        WHEN p.estado = 'Devuelto'  THEN 'Devuelto'
        WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), p.fecha_vencimiento) < 0 THEN 'Vencido'
        WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), p.fecha_vencimiento) <= 2 THEN 'Por vencer'
        ELSE 'Activo'
    END AS situacion,
    ISNULL(multas.monto_multa, 0) AS monto_multa
FROM dbo.prestamo p
JOIN dbo.socio s      ON s.idsocio = p.idsocio
JOIN dbo.tipo_socio t ON t.idtipo  = s.idtipo
CROSS APPLY (
    SELECT
        COUNT(*)                                                          AS total_ejemplares,
        SUM(CASE WHEN d.fecha_devolucion IS NOT NULL THEN 1 ELSE 0 END)   AS devueltos,
        SUM(CASE WHEN d.fecha_devolucion IS NULL     THEN 1 ELSE 0 END)   AS pendientes,
        -- Los títulos del préstamo en una sola celda: "Algoritmos, Java Básico".
        -- STRING_AGG y no el viejo truco de FOR XML PATH: se lee mejor y no
        -- obliga a que QUOTED_IDENTIFIER esté encendido para poder consultar.
        (SELECT STRING_AGG(l2.titulo, ', ') WITHIN GROUP (ORDER BY l2.titulo)
           FROM dbo.detalle_prestamo d2
           JOIN dbo.ejemplar  e2 ON e2.idejemplar = d2.idejemplar
           JOIN dbo.libro     l2 ON l2.idlibro    = e2.idlibro
          WHERE d2.idprestamo = p.idprestamo) AS titulos
    FROM dbo.detalle_prestamo d WHERE d.idprestamo = p.idprestamo
) AS renglones
OUTER APPLY (
    SELECT SUM(m.monto) AS monto_multa
    FROM dbo.multa m WHERE m.idprestamo = p.idprestamo AND m.estado = 'Pendiente'
) AS multas;
GO

-- ------------------------------------------------------ v_socio_detalle -----
-- La ficha del socio con su solvencia. `puede_prestar` es la regla de negocio
-- convertida en columna: sin ella, cada pantalla tendría que recordar las tres
-- condiciones y alguna se olvidaría de una.
CREATE OR ALTER VIEW dbo.v_socio_detalle AS
SELECT
    s.idsocio,
    s.nombre,
    s.apellido,
    s.nombre + ' ' + s.apellido AS nombre_completo,
    s.identidad,
    s.telefono,
    s.email,
    s.direccion,
    s.idtipo,
    t.nombre        AS tipo_socio,
    t.max_prestamos,
    t.dias_prestamo,
    t.multa_diaria,
    s.fecha_registro,
    s.esta_activo,
    circulacion.prestamos_activos,
    circulacion.ejemplares_afuera,
    circulacion.prestamos_vencidos,
    circulacion.historico,
    ISNULL(deuda.multas_pendientes, 0) AS multas_pendientes,
    ISNULL(deuda.monto_adeudado, 0)    AS monto_adeudado,
    -- Cupo que le queda: cuántos ejemplares más puede llevarse hoy
    CASE WHEN t.max_prestamos - circulacion.ejemplares_afuera < 0 THEN 0
         ELSE t.max_prestamos - circulacion.ejemplares_afuera END AS cupo_disponible,
    -- La regla completa: activo, sin mora y sin deuda
    CASE WHEN s.esta_activo = 1
          AND circulacion.prestamos_vencidos = 0
          AND ISNULL(deuda.monto_adeudado, 0) = 0
          AND circulacion.ejemplares_afuera < t.max_prestamos
         THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS puede_prestar,
    -- La misma regla dicha en palabras, para pintarla como insignia sin que la
    -- interfaz tenga que traducir un booleano
    CASE WHEN s.esta_activo = 0 THEN 'Inactivo'
         WHEN circulacion.prestamos_vencidos > 0
           OR ISNULL(deuda.monto_adeudado, 0) > 0
           OR circulacion.ejemplares_afuera >= t.max_prestamos THEN 'Con bloqueo'
         ELSE 'Al día' END AS situacion,
    s.idsocio + ' · ' + s.nombre + ' ' + s.apellido AS etiqueta
FROM dbo.socio s
JOIN dbo.tipo_socio t ON t.idtipo = s.idtipo
CROSS APPLY (
    SELECT
        SUM(CASE WHEN p.estado = 'Activo' THEN 1 ELSE 0 END) AS prestamos_activos,
        SUM(CASE WHEN p.estado = 'Activo'
                  AND p.fecha_vencimiento < CAST(GETDATE() AS DATE)
                 THEN 1 ELSE 0 END)                          AS prestamos_vencidos,
        COUNT(*)                                             AS historico,
        ISNULL((SELECT COUNT(*)
                  FROM dbo.detalle_prestamo d
                  JOIN dbo.prestamo p2 ON p2.idprestamo = d.idprestamo
                 WHERE p2.idsocio = s.idsocio AND d.fecha_devolucion IS NULL), 0) AS ejemplares_afuera
    FROM dbo.prestamo p WHERE p.idsocio = s.idsocio
) AS circulacion
OUTER APPLY (
    SELECT COUNT(*) AS multas_pendientes, SUM(m.monto) AS monto_adeudado
    FROM dbo.multa m WHERE m.idsocio = s.idsocio AND m.estado = 'Pendiente'
) AS deuda;
GO

-- ============================================================================
--  PROCEDIMIENTOS ALMACENADOS
-- ============================================================================

-- ----------------------------------------------------------- sp_panel -------
-- Todos los indicadores del panel en una sola ida a la base de datos. Hacer
-- ocho consultas sueltas desde la aplicación costaría ocho viajes de red.
CREATE OR ALTER PROCEDURE dbo.sp_panel
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hoy DATE = CAST(GETDATE() AS DATE);

    SELECT
        (SELECT COUNT(*) FROM dbo.libro)                                        AS titulos,
        (SELECT COUNT(*) FROM dbo.ejemplar)                                     AS ejemplares,
        (SELECT COUNT(*) FROM dbo.ejemplar WHERE estado = 'Disponible')         AS disponibles,
        (SELECT COUNT(*) FROM dbo.ejemplar WHERE estado = 'Prestado')           AS prestados,
        (SELECT COUNT(*) FROM dbo.socio   WHERE esta_activo = 1)                AS socios,
        (SELECT COUNT(*) FROM dbo.prestamo WHERE estado = 'Activo')             AS prestamos_activos,
        (SELECT COUNT(*) FROM dbo.prestamo
          WHERE estado = 'Activo' AND fecha_vencimiento < @hoy)                 AS prestamos_vencidos,
        (SELECT COUNT(*) FROM dbo.prestamo
          WHERE estado = 'Activo' AND fecha_vencimiento = @hoy)                 AS vencen_hoy,
        (SELECT COUNT(*) FROM dbo.prestamo
          WHERE CAST(fecha_prestamo AS DATE) = @hoy)                            AS prestamos_del_dia,
        (SELECT COUNT(*) FROM dbo.prestamo
          WHERE CAST(fecha_devolucion AS DATE) = @hoy)                          AS devoluciones_del_dia,
        (SELECT ISNULL(SUM(monto), 0) FROM dbo.multa WHERE estado = 'Pendiente')AS multas_por_cobrar,
        (SELECT ISNULL(SUM(monto), 0) FROM dbo.multa
          WHERE estado = 'Pagada' AND CAST(fecha_pago AS DATE) = @hoy)          AS cobrado_hoy,
        (SELECT COUNT(*) FROM dbo.reserva WHERE estado = 'Activa')              AS reservas_activas;
END
GO

-- ------------------------------------------------- sp_libros_mas_prestados --
CREATE OR ALTER PROCEDURE dbo.sp_libros_mas_prestados
    @top   INT = 8,
    @dias  INT = 180
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@top)
        l.idlibro,
        l.titulo,
        a.nombre AS autor,
        c.nombre AS categoria,
        COUNT(*) AS veces_prestado
    FROM dbo.detalle_prestamo d
    JOIN dbo.prestamo  p ON p.idprestamo = d.idprestamo
    JOIN dbo.ejemplar ej ON ej.idejemplar = d.idejemplar
    JOIN dbo.libro     l ON l.idlibro = ej.idlibro
    JOIN dbo.autor     a ON a.idautor = l.idautor
    JOIN dbo.categoria c ON c.idcategoria = l.idcategoria
    WHERE p.fecha_prestamo >= DATEADD(DAY, -@dias, GETDATE())
    GROUP BY l.idlibro, l.titulo, a.nombre, c.nombre
    ORDER BY COUNT(*) DESC, l.titulo;
END
GO

-- ----------------------------------------------------- sp_prestamos_vencidos
-- La lista de cobranza: quién debe qué libro, desde cuándo y cuánto lleva
-- acumulado de mora al día de hoy.
CREATE OR ALTER PROCEDURE dbo.sp_prestamos_vencidos
    @dias_minimos INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        v.idprestamo, v.codigo, v.idsocio, v.socio, v.email, v.telefono,
        v.tipo_socio, v.fecha_prestamo, v.fecha_vencimiento,
        v.dias_retraso, v.pendientes, v.titulos,
        -- Mora proyectada si devolviera hoy: días × tarifa × ejemplares afuera
        CAST(v.dias_retraso * v.multa_diaria * v.pendientes AS DECIMAL(10,2)) AS mora_estimada
    FROM dbo.v_prestamo_detalle v
    WHERE v.estado = 'Activo' AND v.dias_retraso >= @dias_minimos
    ORDER BY v.dias_retraso DESC, v.socio;
END
GO

-- ------------------------------------------------- sp_estado_cuenta_socio ---
-- Todo lo que hay que saber de un socio antes de prestarle: su ficha, lo que
-- tiene afuera y lo que debe.
CREATE OR ALTER PROCEDURE dbo.sp_estado_cuenta_socio
    @idsocio CHAR(6)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) Ficha y solvencia
    SELECT * FROM dbo.v_socio_detalle WHERE idsocio = @idsocio;

    -- 2) Ejemplares que tiene afuera ahora mismo
    SELECT
        d.iddetalle, p.codigo, ej.codigo_barras, l.titulo, a.nombre AS autor,
        p.fecha_prestamo, p.fecha_vencimiento,
        CASE WHEN DATEDIFF(DAY, p.fecha_vencimiento, CAST(GETDATE() AS DATE)) > 0
             THEN DATEDIFF(DAY, p.fecha_vencimiento, CAST(GETDATE() AS DATE))
             ELSE 0 END AS dias_retraso
    FROM dbo.detalle_prestamo d
    JOIN dbo.prestamo  p ON p.idprestamo  = d.idprestamo
    JOIN dbo.ejemplar ej ON ej.idejemplar = d.idejemplar
    JOIN dbo.libro     l ON l.idlibro     = ej.idlibro
    JOIN dbo.autor     a ON a.idautor     = l.idautor
    WHERE p.idsocio = @idsocio AND d.fecha_devolucion IS NULL
    ORDER BY p.fecha_vencimiento;

    -- 3) Multas pendientes
    SELECT m.idmulta, p.codigo, m.motivo, m.dias_retraso, m.monto,
           m.fecha_generada, m.observacion
    FROM dbo.multa m
    JOIN dbo.prestamo p ON p.idprestamo = m.idprestamo
    WHERE m.idsocio = @idsocio AND m.estado = 'Pendiente'
    ORDER BY m.fecha_generada DESC;
END
GO

-- ------------------------------------------------------ sp_movimiento_mes ---
-- Préstamos y devoluciones por día de los últimos N días: alimenta la gráfica
-- de barras del panel.
CREATE OR ALTER PROCEDURE dbo.sp_movimiento_diario
    @dias INT = 14
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH dias AS (
        SELECT CAST(GETDATE() AS DATE) AS d, 0 AS n
        UNION ALL
        SELECT DATEADD(DAY, -1, d), n + 1 FROM dias WHERE n < @dias - 1
    )
    SELECT
        dias.d AS fecha,
        (SELECT COUNT(*) FROM dbo.prestamo p
          WHERE CAST(p.fecha_prestamo AS DATE) = dias.d)   AS prestamos,
        (SELECT COUNT(*) FROM dbo.prestamo p
          WHERE CAST(p.fecha_devolucion AS DATE) = dias.d) AS devoluciones
    FROM dias
    ORDER BY dias.d
    OPTION (MAXRECURSION 400);
END
GO

PRINT 'ALEJANDRÍA · 03_vistas_indices_procedimientos.sql aplicado correctamente.';
GO
