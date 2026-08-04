Imports System.Data

''' <summary>Tablero de llegadas y salidas del día. Es la pantalla de la operación:
''' desde aquí se marca un retraso, se abre el embarque o se cancela un vuelo, y se
''' consulta el manifiesto de pasajeros.</summary>
Public Class ItinerarioPage

    Private idVueloActual As Integer = 0

    Private Sub ItinerarioPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If cboEstado.ItemsSource Is Nothing Then
            Dim filtros As New List(Of String) From {"Todos los estados"}
            filtros.AddRange(VueloService.Estados)
            cboEstado.ItemsSource = filtros
            cboEstado.SelectedIndex = 0

            cboEstadoVuelo.ItemsSource = VueloService.Estados
        End If

        If dpFecha.SelectedDate Is Nothing Then dpFecha.SelectedDate = Date.Today

        If Not Permisos.PuedeEditarCatalogos Then
            btnCambiarEstado.IsEnabled = False
            cboEstadoVuelo.IsEnabled = False
            lblSinPermiso.Visibility = Visibility.Visible
        End If
    End Sub

    Public Sub Cargar()
        Try
            Dim fecha = If(dpFecha.SelectedDate.HasValue, dpFecha.SelectedDate.Value, Date.Today)
            lblFechaLarga.Text = Formato.FechaLarga(fecha)

            Dim datos = VueloService.Itinerario(fecha)

            ' El filtro por estado se aplica sobre la vista, sin volver a la base de datos
            Dim vista = datos.DefaultView
            If cboEstado.SelectedIndex > 0 Then
                vista.RowFilter = $"estado = '{cboEstado.SelectedItem}'"
            Else
                vista.RowFilter = ""
            End If

            dgTablero.ItemsSource = vista

            pnlSinVuelos.Visibility = If(vista.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblSinVuelos.Text = If(cboEstado.SelectedIndex > 0,
                                   "Ningún vuelo de este día tiene ese estado.",
                                   "No hay vuelos programados para este día.")

            lblTotalVuelos.Text = datos.Rows.Count.ToString()
            lblEnHora.Text = datos.Select("estado IN ('Programado','Abordando','En vuelo','Aterrizado')").Length.ToString()
            lblRetrasados.Text = datos.Select("estado = 'Retrasado'").Length.ToString()
            lblCancelados.Text = datos.Select("estado = 'Cancelado'").Length.ToString()
            lblResumen.Text = $"{vista.Count} vuelo(s) en pantalla"

            LimpiarDetalle()

        Catch ex As Exception
            Avisar("Cargar el itinerario", ex)
        End Try
    End Sub

    ' ---------- Navegación por fecha ----------

    Private Sub dpFecha_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dpFecha.SelectedDateChanged
        If IsLoaded Then Cargar()
    End Sub

    Private Sub cboEstado_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboEstado.SelectionChanged
        If IsLoaded Then Cargar()
    End Sub

    Private Sub btnDiaAnterior_Click(sender As Object, e As RoutedEventArgs) Handles btnDiaAnterior.Click
        dpFecha.SelectedDate = If(dpFecha.SelectedDate.HasValue, dpFecha.SelectedDate.Value, Date.Today).AddDays(-1)
    End Sub

    Private Sub btnHoy_Click(sender As Object, e As RoutedEventArgs) Handles btnHoy.Click
        dpFecha.SelectedDate = Date.Today
    End Sub

    Private Sub btnDiaSiguiente_Click(sender As Object, e As RoutedEventArgs) Handles btnDiaSiguiente.Click
        dpFecha.SelectedDate = If(dpFecha.SelectedDate.HasValue, dpFecha.SelectedDate.Value, Date.Today).AddDays(1)
    End Sub

    Private Sub btnActualizar_Click(sender As Object, e As RoutedEventArgs) Handles btnActualizar.Click
        Cargar()
    End Sub

    ' ---------- Ficha del vuelo ----------

    Private Sub dgTablero_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgTablero.SelectionChanged

        Dim fila = TryCast(dgTablero.SelectedItem, DataRowView)
        If fila Is Nothing Then
            LimpiarDetalle()
            Return
        End If

        Try
            idVueloActual = CInt(fila("idvuelo"))

            Dim estado = fila("estado").ToString()
            lblEstadoVuelo.Text = estado.ToUpper()
            lblEstadoVuelo.Foreground = EstadoAColorConverter.Trazo(estado)
            bdEstadoVuelo.Background = EstadoAColorFondoConverter.Relleno(estado)

            lblCodigoVuelo.Text = fila("codigo_vuelo").ToString()
            lblAerolinea.Text = fila("nombre_aero").ToString()
            lblFechaSalida.Text = Formato.FechaLarga(CDate(fila("fecha_salida")))

            lblIataOrigen.Text = fila("iata_origen").ToString()
            lblCiudadOrigen.Text = fila("ciudad_origen").ToString()
            lblHoraSalida.Text = Formato.Hora(CDate(fila("fecha_salida")))

            lblIataDestino.Text = fila("iata_destino").ToString()
            lblCiudadDestino.Text = fila("ciudad_destino").ToString()
            lblHoraLlegada.Text = Formato.Hora(CDate(fila("fecha_llegada")))

            lblDuracion.Text = Formato.Duracion(CInt(fila("duracion_minutos")))
            lblAvion.Text = $"{fila("idavion")} · {fila("tipo_avion")}"
            lblPuerta.Text = If(IsDBNull(fila("puerta")) OrElse
                                String.IsNullOrWhiteSpace(fila("puerta").ToString()),
                                "—", fila("puerta").ToString())

            Dim vendidos = CInt(fila("asientos_vendidos"))
            Dim capacidad = CInt(fila("capacidad_pasajeros"))
            lblOcupacion.Text = $"{vendidos} / {capacidad}  ({CDec(fila("ocupacion")):N1} %)"
            barOcupacion.Value = CDbl(fila("ocupacion"))

            cboEstadoVuelo.SelectedItem = estado

            dgManifiesto.ItemsSource = ReservaService.Manifiesto(idVueloActual).DefaultView
            lblConteoPasajeros.Text = $"{vendidos} pasajero(s)"
            pnlSinPasajeros.Visibility = If(vendidos = 0, Visibility.Visible, Visibility.Collapsed)

            pnlSinSeleccion.Visibility = Visibility.Collapsed
            pnlDetalle.Visibility = Visibility.Visible
            TransicionVentana.FundirEntrada(pnlDetalle)

        Catch ex As Exception
            Avisar("Abrir la ficha del vuelo", ex)
        End Try
    End Sub

    Private Sub LimpiarDetalle()
        idVueloActual = 0
        dgManifiesto.ItemsSource = Nothing
        pnlDetalle.Visibility = Visibility.Collapsed
        pnlSinSeleccion.Visibility = Visibility.Visible
    End Sub

    ' ---------- Cambio de estado ----------

    Private Sub btnCambiarEstado_Click(sender As Object, e As RoutedEventArgs) Handles btnCambiarEstado.Click
        If idVueloActual = 0 OrElse cboEstadoVuelo.SelectedItem Is Nothing Then Return

        Dim nuevo = cboEstadoVuelo.SelectedItem.ToString()

        ' Cancelar un vuelo arrastra a todos sus pasajeros: hay que decirlo claro
        If nuevo = "Cancelado" Then
            Dim aviso = DialogoAlas.Show(
                $"¿Cancelar el vuelo {lblCodigoVuelo.Text}?" & vbCrLf & vbCrLf &
                $"Se cancelarán también los boletos de sus {lblConteoPasajeros.Text.Replace(" pasajero(s)", "")} " &
                "pasajeros y sus asientos volverán a quedar libres.",
                "Confirmar cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question)
            If aviso <> MessageBoxResult.Yes Then Return
        End If

        Try
            Dim problema = VueloService.CambiarEstado(idVueloActual, nuevo)
            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "No se pudo cambiar el estado",
                                 MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            DialogoAlas.Show($"El vuelo {lblCodigoVuelo.Text} quedó como '{nuevo}'.",
                             "Estado actualizado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            Cargar()

        Catch ex As Exception
            Avisar("Cambiar el estado del vuelo", ex)
        End Try
    End Sub

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub
End Class
