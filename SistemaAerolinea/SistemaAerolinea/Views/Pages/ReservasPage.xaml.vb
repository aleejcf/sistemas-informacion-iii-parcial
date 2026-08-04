Imports System.Data

''' <summary>Consulta de reservas emitidas: ver sus boletos, hacer el check-in de
''' un pasajero, reimprimir los pases de abordar y cancelar la reserva completa.</summary>
Public Class ReservasPage

    Private idReservaActual As Integer = 0

    Private Sub ReservasPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If cboEstado.ItemsSource Is Nothing Then
            cboEstado.ItemsSource = New String() {"Todos los estados", "Pendiente de pago",
                                                  "Confirmada", "Cancelada"}
            cboEstado.SelectedIndex = 0
        End If

        If Not Permisos.PuedeCancelarReservas Then
            btnCancelar.IsEnabled = False
            lblSinPermiso.Visibility = Visibility.Visible
        End If
    End Sub

    ''' <summary>Recarga la lista respetando el texto de búsqueda y el filtro de estado.</summary>
    Public Sub Cargar()
        Try
            Dim estado As String = Nothing
            If cboEstado.SelectedIndex > 0 Then estado = cboEstado.SelectedItem.ToString()

            Dim datos = ReservaService.Listar(txtBuscar.Text, estado)
            dgReservas.ItemsSource = datos.DefaultView
            lblVacio.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

            ' La reserva abierta puede haber desaparecido del filtro
            If datos.Rows.Count = 0 Then LimpiarDetalle()

        Catch ex As Exception
            Avisar("Cargar las reservas", ex)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        Cargar()
    End Sub

    Private Sub cboEstado_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboEstado.SelectionChanged
        If IsLoaded Then Cargar()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        txtBuscar.Clear()
        cboEstado.SelectedIndex = 0
    End Sub

    ' ---------- Detalle ----------

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
            lblTitular.Text = reserva("titular").ToString()
            lblContacto.Text = $"{reserva("num_documento")} · {reserva("email")}"

            Dim saldo = CDec(reserva("saldo"))
            lblTotal.Text = Formato.Dinero(reserva("costo"))
            lblPagado.Text = Formato.Dinero(reserva("pagado"))
            lblSaldo.Text = Formato.Dinero(saldo)
            ' Un saldo pendiente tiene que saltar a la vista: es dinero sin cobrar
            lblSaldo.Foreground = If(saldo > 0, TryFindResource("BrushPeligro"), TryFindResource("BrushTextoSuave"))

            If IsDBNull(reserva("observacion")) OrElse
               String.IsNullOrWhiteSpace(reserva("observacion").ToString()) Then
                pnlObservacion.Visibility = Visibility.Collapsed
            Else
                lblObservacion.Text = reserva("observacion").ToString()
                pnlObservacion.Visibility = Visibility.Visible
            End If

            dgBoletos.ItemsSource = ReservaService.Boletos(idReserva).DefaultView

            Dim cancelada = reserva("estado").ToString() = "Cancelada"
            btnCheckIn.IsEnabled = Not cancelada
            btnCancelar.IsEnabled = Permisos.PuedeCancelarReservas AndAlso Not cancelada

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

    ' ---------- Acciones ----------

    Private Sub btnCheckIn_Click(sender As Object, e As RoutedEventArgs) Handles btnCheckIn.Click
        Dim fila = TryCast(dgBoletos.SelectedItem, DataRowView)
        If fila Is Nothing Then
            DialogoAlas.Show("Selecciona primero el boleto del pasajero que hace el check-in.",
                             "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            Dim problema = ReservaService.HacerCheckIn(CInt(fila("idboleto")))
            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "No se pudo hacer el check-in",
                                 MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim idBoleto = CInt(fila("idboleto"))
            MostrarDetalle(idReservaActual)

            ' Con el check-in hecho ya existe pase: ofrecerlo aquí evita que el
            ' agente tenga que acordarse de ir a buscarlo
            Dim descargar = DialogoAlas.Show(
                $"Check-in realizado para {fila("pasajero")}, asiento {fila("asiento")}." & vbCrLf & vbCrLf &
                "¿Quieres descargar su pase de abordar?",
                "Check-in con éxito", MessageBoxButton.YesNo, MessageBoxImage.Question)

            If descargar = MessageBoxResult.Yes Then AbrirPases(idBoleto)

        Catch ex As Exception
            Avisar("Hacer el check-in", ex)
        End Try
    End Sub

    Private Sub btnPases_Click(sender As Object, e As RoutedEventArgs) Handles btnPases.Click
        If idReservaActual = 0 Then Return

        ' Con un boleto seleccionado se descarga solo ese; si no, todos los de la reserva
        Dim fila = TryCast(dgBoletos.SelectedItem, DataRowView)
        AbrirPases(If(fila Is Nothing, 0, CInt(fila("idboleto"))))
    End Sub

    Private Sub AbrirPases(idBoleto As Integer)
        Try
            Dim ventana = If(idBoleto > 0,
                             New PaseAbordarWindow(idReservaActual, idBoleto),
                             New PaseAbordarWindow(idReservaActual))
            ventana.Owner = Window.GetWindow(Me)
            ventana.ShowDialog()

        Catch ex As Exception
            Avisar("Abrir los pases de abordar", ex)
        End Try
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        If idReservaActual = 0 Then Return

        Dim respuesta = DialogoAlas.Show(
            $"¿Cancelar la reserva {lblPnr.Text} de {lblTitular.Text}?" & vbCrLf & vbCrLf &
            "Sus boletos quedarán cancelados y los asientos volverán a estar disponibles. " &
            "El historial y los pagos ya registrados se conservan.",
            "Confirmar cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = ReservaService.Cancelar(idReservaActual, "Cancelada desde la pantalla de Reservas")
            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "No se pudo cancelar", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            DialogoAlas.Show("La reserva quedó cancelada.", "Cancelación con éxito",
                             MessageBoxButton.OK, MessageBoxImage.Information)
            Dim id = idReservaActual
            Cargar()
            MostrarDetalle(id)

        Catch ex As Exception
            Avisar("Cancelar la reserva", ex)
        End Try
    End Sub

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub
End Class
