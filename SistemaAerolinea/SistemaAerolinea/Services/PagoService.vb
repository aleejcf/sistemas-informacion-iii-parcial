Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Cobros y comprobantes. Una reserva puede pagarse al emitirla (lo hace
''' el asistente) o después desde esta pantalla; también admite pagos parciales.</summary>
Public Class PagoService

    Public Shared Function Listar(Optional filtro As String = "",
                                  Optional desde As Date? = Nothing,
                                  Optional hasta As Date? = Nothing) As DataTable
        Return Db.Consultar(
            "SELECT pg.idpago, pg.num_comprobante, pg.tipo_comprobante, pg.fecha, pg.monto,
                    pg.impuesto, pg.idreserva, r.codigo_reserva, mp.nombre AS metodo_pago,
                    p.nombre_p + ' ' + p.apaterno AS pasajero, pg.usuario_registra
             FROM pago pg
             JOIN reserva      r  ON r.idreserva     = pg.idreserva
             JOIN pasajero     p  ON p.idpasajero    = pg.idpasajero
             JOIN metodo_pago  mp ON mp.idmetodopago = pg.idmetodopago
             WHERE (@f = '' OR pg.num_comprobante LIKE @like OR r.codigo_reserva LIKE @like
                    OR p.nombre_p LIKE @like OR p.apaterno LIKE @like)
               AND (@d IS NULL OR CAST(pg.fecha AS DATE) >= @d)
               AND (@h IS NULL OR CAST(pg.fecha AS DATE) <= @h)
             ORDER BY pg.fecha DESC",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"),
            New SqlParameter("@d", If(desde.HasValue, CObj(desde.Value.Date), DBNull.Value)),
            New SqlParameter("@h", If(hasta.HasValue, CObj(hasta.Value.Date), DBNull.Value)))
    End Function

    ''' <summary>Reservas que todavía deben dinero, para la lista de cobro pendiente.</summary>
    Public Shared Function ReservasConSaldo() As DataTable
        Return Db.Consultar("SELECT idreserva, codigo_reserva, titular, num_documento,
                                    fecha, costo, pagado, saldo, boletos
                             FROM dbo.v_reserva_resumen
                             WHERE estado <> 'Cancelada' AND costo - pagado > 0
                             ORDER BY fecha DESC")
    End Function

    Public Shared Function Obtener(idPago As Integer) As DataRow
        Return Db.ConsultarFila(
            "SELECT pg.*, r.codigo_reserva, mp.nombre AS metodo_pago,
                    p.nombre_p + ' ' + p.apaterno + ISNULL(' ' + p.amaterno, '') AS pasajero,
                    p.num_documento, p.email
             FROM pago pg
             JOIN reserva     r  ON r.idreserva     = pg.idreserva
             JOIN pasajero    p  ON p.idpasajero    = pg.idpasajero
             JOIN metodo_pago mp ON mp.idmetodopago = pg.idmetodopago
             WHERE pg.idpago = @p",
            New SqlParameter("@p", idPago))
    End Function

    Public Const CANAL_MOSTRADOR As String = "Mostrador"
    Public Const CANAL_PORTAL As String = "Portal"

    ''' <summary>Cobro que hace el pasajero desde el portal, como en cualquier
    ''' aerolínea de hoy: se paga el saldo completo de una vez y la reserva queda
    ''' confirmada en el acto.
    '''
    ''' Nunca se piden ni se guardan datos de tarjeta. En un sistema real el pago
    ''' lo procesa una pasarela (Stripe, PayPal, un banco) y el comercio solo
    ''' recibe de vuelta la referencia de autorización: guardar el número de una
    ''' tarjeta obligaría a cumplir la norma PCI-DSS y es justo lo que las
    ''' pasarelas existen para evitar.
    '''
    ''' Devuelve Nothing si salió bien, o el mensaje de error. La referencia de la
    ''' operación se devuelve por parámetro.</summary>
    Public Shared Function PagarDesdeElPortal(idReserva As Integer, idMetodoPago As Integer,
                                              ByRef referencia As String) As String
        referencia = ""

        ' Un pasajero solo paga lo suyo. La comprobación va aquí y no en la
        ' pantalla: el identificador de una reserva es un número que se puede probar.
        If Not ReservaService.EsDelPasajeroActual(idReserva) Then
            Return "Esa reserva no está a tu nombre."
        End If

        Dim reserva = ReservaService.ObtenerPorId(idReserva)
        If reserva Is Nothing Then Return "No se encontró la reserva."

        Dim saldo = CDec(reserva("costo")) - CDec(reserva("pagado"))
        If saldo <= 0 Then Return "Esa reserva ya está pagada."

        Dim propia = ReferenciaPago.Generar()
        Dim problema = Registrar(idReserva, idMetodoPago, saldo, Comprobante.FACTURA,
                                 CANAL_PORTAL, propia)
        If problema IsNot Nothing Then Return problema

        referencia = propia
        Return Nothing
    End Function

    ''' <summary>Registra un cobro sobre una reserva. Si con este pago se cubre el
    ''' total, la reserva pasa a "Confirmada" en la misma transacción.
    ''' Devuelve Nothing si salió bien, o el mensaje de error.</summary>
    Public Shared Function Registrar(idReserva As Integer, idMetodoPago As Integer,
                                     monto As Decimal, tipoComprobante As String,
                                     Optional canal As String = CANAL_MOSTRADOR,
                                     Optional referencia As String = Nothing) As String

        If monto <= 0 Then Return "El monto debe ser mayor que cero."

        Dim reserva = ReservaService.ObtenerPorId(idReserva)
        If reserva Is Nothing Then Return "No se encontró la reserva."
        If reserva("estado").ToString() = "Cancelada" Then Return "Esa reserva está cancelada."

        Dim costo = CDec(reserva("costo"))
        Dim pagado = CDec(reserva("pagado"))
        Dim saldo = costo - pagado

        If saldo <= 0 Then Return "Esa reserva ya está pagada por completo."
        If monto > saldo Then
            Return $"El monto excede el saldo pendiente ({Formato.Dinero(saldo)})."
        End If

        Dim idPasajero = reserva("idpasajero").ToString()
        Dim impuestoProporcional = Math.Round(CDec(reserva("impuesto")) * (monto / costo), 2)
        Dim codigo = reserva("codigo_reserva").ToString()

        Db.EnTransaccion(
            Sub(cn, tx)
                Dim secuencia = CInt(Db.EscalarEn(cn, tx,
                    "SELECT COUNT(*) + 1 FROM pago WHERE idreserva = @r",
                    New SqlParameter("@r", idReserva)))

                Db.EjecutarEn(cn, tx,
                    "INSERT INTO pago (idreserva, idpasajero, idmetodopago, monto, impuesto,
                                       tipo_comprobante, num_comprobante, usuario_registra,
                                       canal, referencia)
                     VALUES (@r, @p, @m, @monto, @imp, @tipo, @num, @u, @canal, @ref)",
                    New SqlParameter("@r", idReserva),
                    New SqlParameter("@p", idPasajero),
                    New SqlParameter("@m", idMetodoPago),
                    New SqlParameter("@monto", monto),
                    New SqlParameter("@imp", impuestoProporcional),
                    New SqlParameter("@tipo", tipoComprobante),
                    New SqlParameter("@num", Comprobante.Numero(tipoComprobante, idReserva, secuencia)),
                    New SqlParameter("@u", Sesion.NombreUsuario),
                    New SqlParameter("@canal", canal),
                    New SqlParameter("@ref", Db.Opcional(referencia)))

                ' La reserva se confirma solo cuando queda saldada
                Db.EjecutarEn(cn, tx,
                    "UPDATE reserva SET estado = 'Confirmada'
                      WHERE idreserva = @r
                        AND estado <> 'Cancelada'
                        AND costo <= (SELECT ISNULL(SUM(monto), 0) FROM pago WHERE idreserva = @r)",
                    New SqlParameter("@r", idReserva))
            End Sub)

        Registro.Info($"Pago de {Formato.Dinero(monto)} registrado en la reserva {codigo}")
        BitacoraService.Registrar(BitacoraService.PAGO, "pago",
                                  $"{codigo} · {Formato.Dinero(monto)} · {tipoComprobante}")
        Return Nothing
    End Function

    ''' <summary>Totales del día para el encabezado de la pantalla de pagos.</summary>
    Public Shared Function ResumenDelDia() As DataRow
        Return Db.ConsultarFila(
            "SELECT ISNULL(SUM(monto), 0) AS total,
                    COUNT(*)              AS cantidad,
                    ISNULL(SUM(CASE WHEN tipo_comprobante = 'Factura' THEN monto ELSE 0 END), 0) AS facturado
             FROM pago
             WHERE CAST(fecha AS DATE) = CAST(GETDATE() AS DATE)")
    End Function

    ''' <summary>Los pagos hechos sobre una reserva concreta.</summary>
    Public Shared Function DeLaReserva(idReserva As Integer) As DataTable
        Return Db.Consultar("SELECT pg.idpago, pg.fecha, pg.monto, pg.tipo_comprobante,
                                    pg.num_comprobante, mp.nombre AS metodo_pago, pg.usuario_registra,
                                    pg.canal, pg.referencia
                             FROM pago pg
                             JOIN metodo_pago mp ON mp.idmetodopago = pg.idmetodopago
                             WHERE pg.idreserva = @r
                             ORDER BY pg.fecha",
                            New SqlParameter("@r", idReserva))
    End Function

    ''' <summary>Reservas del pasajero de la sesión que todavía deben dinero.</summary>
    Public Shared Function SaldosDelPasajero() As DataTable
        Dim idPasajero = Sesion.IdPasajero
        If idPasajero Is Nothing Then Return New DataTable()

        Return Db.Consultar("EXEC dbo.sp_saldos_del_pasajero @idpasajero = @p",
                            New SqlParameter("@p", idPasajero))
    End Function

    ''' <summary>Cuánto entró por cada canal: mostrador contra portal.</summary>
    Public Shared Function IngresosPorCanal(Optional dias As Integer = 30) As DataTable
        Return Db.Consultar("EXEC dbo.sp_ingresos_por_canal @dias = @d",
                            New SqlParameter("@d", dias))
    End Function

    ''' <summary>Los métodos de pago que se ofrecen en el portal. El efectivo se
    ''' queda fuera por razones obvias: nadie paga en efectivo por internet.</summary>
    Public Shared Function MetodosEnLinea() As DataTable
        Return Db.Consultar("SELECT idmetodopago, nombre AS etiqueta FROM metodo_pago
                             WHERE nombre <> 'Efectivo' ORDER BY idmetodopago")
    End Function
End Class
