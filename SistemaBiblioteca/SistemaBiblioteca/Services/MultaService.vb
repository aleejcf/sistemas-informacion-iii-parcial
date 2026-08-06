Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Multas: cobrarlas, condonarlas y saber cuánto debe cada socio.
''' Las genera PrestamoService al registrar una devolución tardía o dañada;
''' aquí solo se administran.</summary>
Public Class MultaService

    Public Shared ReadOnly Motivos As String() = {"Retraso", "Daño", "Extravío"}
    Public Shared ReadOnly Estados As String() = {"Pendiente", "Pagada", "Condonada"}

    Public Shared Function Listar(Optional filtro As String = "",
                                  Optional estado As String = Nothing,
                                  Optional motivo As String = Nothing,
                                  Optional desde As Date? = Nothing) As DataTable
        Return Db.Consultar(
            "SELECT m.idmulta, m.idprestamo, p.codigo, m.idsocio,
                    s.nombre + ' ' + s.apellido AS socio, s.email, t.nombre AS tipo_socio,
                    m.motivo, m.dias_retraso, m.monto, m.estado,
                    m.fecha_generada, m.fecha_pago, m.usuario_registra, m.observacion
             FROM multa m
             JOIN prestamo   p ON p.idprestamo = m.idprestamo
             JOIN socio      s ON s.idsocio    = m.idsocio
             JOIN tipo_socio t ON t.idtipo     = s.idtipo
             WHERE (@f = '' OR p.codigo LIKE @like OR m.idsocio LIKE @like
                    OR s.nombre LIKE @like OR s.apellido LIKE @like)
               AND (@e IS NULL OR m.estado = @e)
               AND (@m IS NULL OR m.motivo = @m)
               AND (@d IS NULL OR CAST(m.fecha_generada AS DATE) >= @d)
             ORDER BY CASE m.estado WHEN 'Pendiente' THEN 1 ELSE 2 END,
                      m.fecha_generada DESC",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"),
            New SqlParameter("@e", Db.Opcional(estado)),
            New SqlParameter("@m", Db.Opcional(motivo)),
            New SqlParameter("@d", If(desde.HasValue, CObj(desde.Value.Date), DBNull.Value)))
    End Function

    Public Shared Function Obtener(idMulta As Integer) As DataRow
        Return Db.ConsultarFila(
            "SELECT m.idmulta, m.idprestamo, p.codigo, m.idsocio,
                    s.nombre + ' ' + s.apellido AS socio,
                    m.motivo, m.dias_retraso, m.monto, m.estado, m.observacion
             FROM multa m
             JOIN prestamo p ON p.idprestamo = m.idprestamo
             JOIN socio    s ON s.idsocio    = m.idsocio
             WHERE m.idmulta = @m",
            New SqlParameter("@m", idMulta))
    End Function

    ''' <summary>Cobra la multa. Devuelve Nothing y, por referencia, el texto del
    ''' recibo; o el mensaje de error.</summary>
    Public Shared Function Pagar(idMulta As Integer, ByRef recibo As String) As String
        recibo = ""

        Dim fila = Obtener(idMulta)
        If fila Is Nothing Then Return "No se encontró la multa."
        If Db.Texto(fila, "estado") <> "Pendiente" Then
            Return $"Esta multa ya está {Db.Texto(fila, "estado").ToLower()}."
        End If

        ' El WHERE repite la condición de estado: si otro cajero la cobró entre
        ' la lectura y este UPDATE, aquí se afectan 0 filas y no se cobra dos veces.
        Dim filas = Db.Ejecutar(
            "UPDATE multa SET estado = 'Pagada', fecha_pago = GETDATE(), usuario_registra = @u
             WHERE idmulta = @m AND estado = 'Pendiente'",
            New SqlParameter("@m", idMulta),
            New SqlParameter("@u", Sesion.NombreUsuario))

        If filas = 0 Then Return "La multa ya había sido cobrada por otro usuario."

        Dim monto = Db.Monto(fila, "monto")
        BitacoraService.Registrar(BitacoraService.MULTA_PAGADA, "multa",
            $"{Db.Texto(fila, "codigo")} · {Db.Texto(fila, "socio")} · {Formato.Dinero(monto)}")

        recibo = Comprobante.Multa(Db.Texto(fila, "codigo"), Db.Texto(fila, "socio"),
                                   Db.Texto(fila, "motivo"), Db.Numero(fila, "dias_retraso"), monto)
        Return Nothing
    End Function

    ''' <summary>Perdona la multa. Es decisión de un Administrador y siempre exige
    ''' un motivo escrito: condonar sin justificación es un agujero de control.</summary>
    Public Shared Function Condonar(idMulta As Integer, motivo As String) As String
        If Not Permisos.PuedeCondonarMultas Then
            Return "Solo un administrador puede condonar multas."
        End If
        If String.IsNullOrWhiteSpace(motivo) Then
            Return "Escribe el motivo por el que se condona la multa."
        End If

        Dim fila = Obtener(idMulta)
        If fila Is Nothing Then Return "No se encontró la multa."
        If Db.Texto(fila, "estado") <> "Pendiente" Then
            Return $"Esta multa ya está {Db.Texto(fila, "estado").ToLower()}."
        End If

        Dim filas = Db.Ejecutar(
            "UPDATE multa SET estado = 'Condonada', fecha_pago = GETDATE(), usuario_registra = @u,
                    observacion = ISNULL(observacion + ' | ', '') + @o
             WHERE idmulta = @m AND estado = 'Pendiente'",
            New SqlParameter("@m", idMulta),
            New SqlParameter("@u", Sesion.NombreUsuario),
            New SqlParameter("@o", $"Condonada por {Sesion.NombreUsuario}: {motivo.Trim()}"))

        If filas = 0 Then Return "La multa ya había sido resuelta por otro usuario."

        BitacoraService.Registrar(BitacoraService.MULTA_CONDONADA, "multa",
            $"{Db.Texto(fila, "codigo")} · {Formato.Dinero(Db.Monto(fila, "monto"))} · {motivo.Trim()}")
        Return Nothing
    End Function

    ''' <summary>Cobra de un solo golpe todas las multas pendientes de un socio.
    ''' Es lo que pasa de verdad en el mostrador: el socio paga todo y se lleva
    ''' sus libros.</summary>
    Public Shared Function PagarTodasDelSocio(idSocio As String, ByRef total As Decimal,
                                              ByRef cantidad As Integer) As String
        total = 0
        cantidad = 0

        Dim pendientes = Db.Consultar(
            "SELECT idmulta, monto FROM multa WHERE idsocio = @s AND estado = 'Pendiente'",
            New SqlParameter("@s", idSocio))

        If pendientes.Rows.Count = 0 Then Return "Este socio no tiene multas pendientes."

        Dim sumado As Decimal = 0
        Dim contadas = 0

        Db.EnTransaccion(
            Sub(cn, tx)
                For Each fila As DataRow In pendientes.Rows
                    Dim filas = Db.EjecutarEn(cn, tx,
                        "UPDATE multa SET estado = 'Pagada', fecha_pago = GETDATE(), usuario_registra = @u
                         WHERE idmulta = @m AND estado = 'Pendiente'",
                        New SqlParameter("@m", CInt(fila("idmulta"))),
                        New SqlParameter("@u", Sesion.NombreUsuario))

                    If filas > 0 Then
                        sumado += CDec(fila("monto"))
                        contadas += 1
                    End If
                Next
            End Sub)

        If contadas = 0 Then Return "Las multas ya habían sido cobradas por otro usuario."

        total = sumado
        cantidad = contadas
        BitacoraService.Registrar(BitacoraService.MULTA_PAGADA, "multa",
                                  $"{idSocio} · {contadas} multas · {Formato.Dinero(sumado)}")
        Return Nothing
    End Function

    ''' <summary>Resumen de caja: cuánto se ha cobrado y cuánto falta por cobrar.</summary>
    Public Shared Function Resumen(Optional desde As Date? = Nothing) As DataRow
        Return Db.ConsultarFila(
            "SELECT
                 ISNULL(SUM(CASE WHEN estado = 'Pendiente' THEN monto ELSE 0 END), 0) AS por_cobrar,
                 ISNULL(SUM(CASE WHEN estado = 'Pagada'    THEN monto ELSE 0 END), 0) AS cobrado,
                 ISNULL(SUM(CASE WHEN estado = 'Condonada' THEN monto ELSE 0 END), 0) AS condonado,
                 SUM(CASE WHEN estado = 'Pendiente' THEN 1 ELSE 0 END)                AS pendientes,
                 COUNT(*)                                                             AS total
             FROM multa
             WHERE (@d IS NULL OR CAST(fecha_generada AS DATE) >= @d)",
            New SqlParameter("@d", If(desde.HasValue, CObj(desde.Value.Date), DBNull.Value)))
    End Function
End Class
