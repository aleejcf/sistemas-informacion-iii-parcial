Imports System.Data

''' <summary>Pantalla principal del pasajero: su próximo vuelo, sus reservas y
''' desde ahí el check-in y la descarga del pase de abordar.
'''
''' El aislamiento no depende de esta pantalla: ReservaService ya filtra por el
''' pasajero de la sesión. Aquí solo se decide qué se muestra.</summary>
Public Class MisVuelosPage

    Private idReservaActual As Integer = 0
    Private idBoletoProximo As Integer = 0

    Public Sub Cargar()
        Try
            CargarProximoVuelo()
            CargarReservas()
        Catch ex As Exception
            Avisar("Cargar tus vuelos", ex)
        End Try
    End Sub

    ' ---------- Próximo vuelo ----------

    Private Sub CargarProximoVuelo()
        Dim idPasajero = Sesion.IdPasajero
        If idPasajero Is Nothing Then
            pnlProximo.Visibility = Visibility.Collapsed
            Return
        End If

        Dim proximos = ReservaService.ProximosVuelosDe(idPasajero)
        If proximos.Rows.Count = 0 Then
            pnlProximo.Visibility = Visibility.Collapsed
            idBoletoProximo = 0
            Return
        End If

        Dim vuelo = proximos.Rows(0)
        idBoletoProximo = CInt(vuelo("idboleto"))

        lblOrigen.Text = vuelo("iata_origen").ToString()
        lblDestino.Text = vuelo("iata_destino").ToString()
        lblCiudades.Text = $"{vuelo("ciudad_origen")} → {vuelo("ciudad_destino")} · {vuelo("codigo_vuelo")}"
        lblAsiento.Text = vuelo("asiento").ToString()

        Dim salida = CDate(vuelo("fecha_salida"))
        lblCuandoSale.Text = $"{Formato.FechaLarga(salida)} a las {Formato.Hora(salida)}"
        lblFaltan.Text = CuantoFalta(salida)

        Dim estado = vuelo("estado").ToString()
        lblEstadoBoleto.Text = estado.ToUpper()
        lblEstadoBoleto.Foreground = EstadoAColorConverter.Trazo(estado)
        bdEstadoBoleto.Background = EstadoAColorFondoConverter.Relleno(estado)

        ' Con el check-in ya hecho lo que hace falta es el pase, no repetirlo
        btnCheckInRapido.Content = If(estado = "Emitido", "Hacer check-in", "⤓ Descargar mi pase")

        pnlProximo.Visibility = Visibility.Visible
        TransicionVentana.FundirEntrada(pnlProximo)
    End Sub

    ''' <summary>"Sale en 3 días" se entiende de un vistazo mucho mejor que una fecha.</summary>
    Private Shared Function CuantoFalta(salida As DateTime) As String
        Dim resta = salida - DateTime.Now
        If resta.TotalMinutes < 0 Then Return "El vuelo ya salió"
        If resta.TotalMinutes < 60 Then Return $"Sale en {CInt(resta.TotalMinutes)} minutos"
        If resta.TotalHours < 24 Then Return $"Sale en {CInt(resta.TotalHours)} horas"
        Dim dias = CInt(Math.Floor(resta.TotalDays))
        Return If(dias = 1, "Sale mañana", $"Sale en {dias} días")
    End Function

    Private Sub btnCheckInRapido_Click(sender As Object, e As RoutedEventArgs) Handles btnCheckInRapido.Click
        If idBoletoProximo = 0 Then Return

        Dim boleto = ReservaService.Boleto(idBoletoProximo)
        If boleto Is Nothing Then Return

        If boleto("estado").ToString() = "Emitido" Then
            HacerCheckIn(idBoletoProximo, CInt(boleto("idreserva")))
        Else
            AbrirPase(CInt(boleto("idreserva")), idBoletoProximo)
        End If
    End Sub

    ' ---------- Reservas ----------

    Private Sub CargarReservas()
        Dim datos = ReservaService.Listar()
        dgReservas.ItemsSource = datos.DefaultView
        lblVacio.Visibility = If(datos.DefaultView.Count = 0, Visibility.Visible, Visibility.Collapsed)

        If datos.DefaultView.Count = 0 Then LimpiarDetalle()
    End Sub

    Private Sub btnActualizar_Click(sender As Object, e As RoutedEventArgs) Handles btnActualizar.Click
        Cargar()
    End Sub

    Private Sub dgReservas_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgReservas.SelectionChanged

        Dim fila = TryCast(dgReservas.SelectedItem, DataRowView)
        If fila Is Nothing Then
            LimpiarDetalle()
            Return
        End If

        MostrarDetalle(CInt(fila("idreserva")))
    End Sub

    Private Sub MostrarDetalle(idReserva As Integer)
        Try
            Dim reserva = ReservaService.ObtenerPorId(idReserva)
            If reserva Is Nothing Then
                LimpiarDetalle()
                Return
            End If

            idReservaActual = idReserva

            lblPnr.Text = reserva("codigo_reserva").ToString()
            Dim saldo = CDec(reserva("saldo"))
            lblTotal.Text = Formato.Dinero(reserva("costo"))
            lblPagado.Text = Formato.Dinero(reserva("pagado"))
            lblSaldo.Text = Formato.Dinero(saldo)
            lblSaldo.Foreground = If(saldo > 0, TryFindResource("BrushPeligro"), TryFindResource("BrushTextoSuave"))
            pnlAvisoPago.Visibility = If(saldo > 0, Visibility.Visible, Visibility.Collapsed)

            ' Solo los boletos de esta persona: en una reserva familiar cada quien
            ' hace su propio check-in con su propio pase
            Dim boletos = ReservaService.Boletos(idReserva)
            boletos.DefaultView.RowFilter = $"idpasajero = '{Sesion.IdPasajero}'"
            dgBoletos.ItemsSource = boletos.DefaultView

            Dim cancelada = reserva("estado").ToString() = "Cancelada"
            btnCheckIn.IsEnabled = Not cancelada AndAlso saldo <= 0
            btnPase.IsEnabled = Not cancelada

            lblSinSeleccion.Visibility = Visibility.Collapsed
            pnlDetalle.Visibility = Visibility.Visible
            TransicionVentana.FundirEntrada(pnlDetalle)

        Catch ex As Exception
            Avisar("Abrir la reserva", ex)
        End Try
    End Sub

    Private Sub LimpiarDetalle()
        idReservaActual = 0
        dgBoletos.ItemsSource = Nothing
        pnlDetalle.Visibility = Visibility.Collapsed
        lblSinSeleccion.Visibility = Visibility.Visible
    End Sub

    ' ---------- Check-in y pase ----------

    Private Sub btnCheckIn_Click(sender As Object, e As RoutedEventArgs) Handles btnCheckIn.Click
        Dim fila = TryCast(dgBoletos.SelectedItem, DataRowView)
        If fila Is Nothing Then
            DialogoAlas.Show("Elige primero el boleto del vuelo al que quieres hacerle check-in.",
                             "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        HacerCheckIn(CInt(fila("idboleto")), idReservaActual)
    End Sub

    Private Sub HacerCheckIn(idBoleto As Integer, idReserva As Integer)
        Try
            Dim problema = ReservaService.HacerCheckIn(idBoleto)
            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "No se pudo hacer el check-in",
                                 MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Cargar()
            If idReserva > 0 Then MostrarDetalle(idReserva)

            Dim descargar = DialogoAlas.Show(
                "¡Listo! Ya hiciste tu check-in." & vbCrLf & vbCrLf &
                "¿Quieres descargar tu pase de abordar ahora?",
                "Check-in con éxito", MessageBoxButton.YesNo, MessageBoxImage.Question)

            If descargar = MessageBoxResult.Yes Then AbrirPase(idReserva, idBoleto)

        Catch ex As Exception
            Avisar("Hacer el check-in", ex)
        End Try
    End Sub

    ' ---------- Pago en línea ----------

    Private Sub btnPagar_Click(sender As Object, e As RoutedEventArgs) Handles btnPagar.Click
        If idReservaActual = 0 Then Return

        Try
            Dim ventana As New PagarWindow(idReservaActual) With {.Owner = Window.GetWindow(Me)}
            ventana.ShowDialog()

            If Not ventana.Pagado Then Return

            ' Tras pagar cambia el saldo y se habilita el check-in: hay que releer todo
            Dim id = idReservaActual
            Cargar()
            MostrarDetalle(id)

        Catch ex As Exception
            Avisar("Abrir el pago", ex)
        End Try
    End Sub

    Private Sub btnPase_Click(sender As Object, e As RoutedEventArgs) Handles btnPase.Click
        If idReservaActual = 0 Then Return

        Dim fila = TryCast(dgBoletos.SelectedItem, DataRowView)
        AbrirPase(idReservaActual, If(fila Is Nothing, 0, CInt(fila("idboleto"))))
    End Sub

    Private Sub AbrirPase(idReserva As Integer, idBoleto As Integer)
        Try
            Dim ventana = If(idBoleto > 0,
                             New PaseAbordarWindow(idReserva, idBoleto),
                             New PaseAbordarWindow(idReserva))
            ventana.Owner = Window.GetWindow(Me)
            ventana.ShowDialog()

        Catch ex As Exception
            Avisar("Abrir el pase de abordar", ex)
        End Try
    End Sub

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub
End Class
