Imports System.Data

Public Class MovimientosPage

    Private Sub MovimientosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        CargarCombos()
        CargarTablas()
    End Sub

    Private Sub CargarCombos()
        Try
            cboTipo.ItemsSource = MovimientoService.Tarifas().DefaultView
            cboParqueadero.ItemsSource = ParqueaderoService.ParaCombo().DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar los combos", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub CargarTablas()
        Try
            Dim abiertos = MovimientoService.Abiertos()
            dgAbiertos.ItemsSource = abiertos.DefaultView
            lblCantidadDentro.Text = $"({abiertos.Rows.Count})"
            dgHistorial.ItemsSource = MovimientoService.Historial().DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar movimientos", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- ENTRADA ----------
    Private Sub btnEntrada_Click(sender As Object, e As RoutedEventArgs) Handles btnEntrada.Click
        If String.IsNullOrWhiteSpace(txtPlaca.Text) OrElse cboTipo.SelectedValue Is Nothing OrElse
           cboParqueadero.SelectedValue Is Nothing Then
            DialogoParko.Show("Completa la placa, el tipo de vehículo y el parqueadero.",
                            "Campos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            If MovimientoService.TieneEntradaAbierta(txtPlaca.Text) Then
                DialogoParko.Show($"El vehículo con placa {txtPlaca.Text.Trim().ToUpper()} ya está dentro del parqueadero.",
                                "Entrada duplicada", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            MovimientoService.RegistrarEntrada(txtPlaca.Text, cboTipo.SelectedValue.ToString(),
                                               cboParqueadero.SelectedValue.ToString(),
                                               Sesion.UsuarioActual.NombreUsuario)
            txtPlaca.Clear()
            CargarTablas()
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al registrar la entrada", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- SALIDA ----------
    Private Sub btnSalida_Click(sender As Object, e As RoutedEventArgs) Handles btnSalida.Click
        Dim fila = TryCast(dgAbiertos.SelectedItem, DataRowView)
        If fila Is Nothing Then
            DialogoParko.Show("Selecciona primero un vehículo de la lista de vehículos dentro.",
                            "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim placa = fila("placa").ToString()
        Dim respuesta = DialogoParko.Show($"¿Registrar la salida del vehículo {placa}?", "Confirmar salida",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        Try
            Dim resultado = MovimientoService.RegistrarSalida(CInt(fila("movimiento_id")))
            If resultado Is Nothing Then
                DialogoParko.Show("No se encontró el movimiento. Actualiza la lista.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            Dim tiempo = TimeSpan.FromMinutes(resultado.Minutos)
            Dim ticket =
                "════════ TICKET DE SALIDA ════════" & Environment.NewLine & Environment.NewLine &
                $"  Placa:      {resultado.Placa}" & Environment.NewLine &
                $"  Entrada:    {resultado.Entrada:dd/MM/yyyy hh:mm tt}" & Environment.NewLine &
                $"  Salida:     {resultado.Salida:dd/MM/yyyy hh:mm tt}" & Environment.NewLine &
                $"  Tiempo:     {CInt(Math.Floor(tiempo.TotalHours))} h {tiempo.Minutes} min" & Environment.NewLine &
                $"  Tarifa:     L {resultado.ValorHora:N2} por hora" & Environment.NewLine &
                $"  Horas cobradas: {resultado.HorasCobradas}" & Environment.NewLine &
                "──────────────────────────────────" & Environment.NewLine &
                $"  TOTAL A PAGAR:  L {resultado.Total:N2}" & Environment.NewLine &
                "══════════════════════════════════"

            DialogoParko.Show(ticket, "Salida registrada", MessageBoxButton.OK, MessageBoxImage.Information)
            CargarTablas()
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al registrar la salida", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub
End Class
