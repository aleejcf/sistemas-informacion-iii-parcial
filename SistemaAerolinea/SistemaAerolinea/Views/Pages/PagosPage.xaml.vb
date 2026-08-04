Imports System.Data

''' <summary>Caja del mostrador: cobra el saldo de las reservas y consulta los
''' comprobantes ya emitidos. Admite abonos parciales, por eso el monto es
''' editable y no se fuerza a pagar el saldo completo.</summary>
Public Class PagosPage

    ' Mientras se repuebla la pantalla, los filtros del historial no deben disparar consultas
    Private cargando As Boolean

    Public Sub Cargar()
        cargando = True
        Try
            CargarIndicadores()
            CargarCombos()
            CargarReservasConSaldo()
        Finally
            cargando = False
        End Try

        CargarHistorial()
    End Sub

    ' ---------- Indicadores ----------

    Private Sub CargarIndicadores()
        Try
            Dim resumen = PagoService.ResumenDelDia()
            If resumen Is Nothing Then Return

            lblCobradoHoy.Text = Formato.Dinero(resumen("total"))
            lblCantidadCobros.Text = CInt(resumen("cantidad")).ToString()
            lblFacturadoHoy.Text = Formato.Dinero(resumen("facturado"))

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Consultar el resumen de caja del día", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- Cobrar ----------

    Private Sub CargarCombos()
        Try
            Dim metodos = CatalogoService.MetodosPagoParaCombo()
            cboMetodo.ItemsSource = metodos.DefaultView
            If metodos.Rows.Count > 0 Then cboMetodo.SelectedIndex = 0

            cboTipoComprobante.ItemsSource = New String() {Comprobante.FACTURA, Comprobante.RECIBO}
            cboTipoComprobante.SelectedIndex = 0

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Cargar los métodos de pago", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub CargarReservasConSaldo()
        Try
            Dim dt = PagoService.ReservasConSaldo()
            dgSaldos.ItemsSource = dt.DefaultView

            pnlSinSaldos.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblConteoSaldos.Text = If(dt.Rows.Count = 1, "1 reserva por cobrar",
                                      $"{dt.Rows.Count} reservas por cobrar")
            MostrarSeleccion()

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Cargar las reservas con saldo pendiente", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub dgSaldos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgSaldos.SelectionChanged
        MostrarSeleccion()
    End Sub

    ''' <summary>Vuelca la reserva elegida en el formulario y precarga el saldo
    ''' completo, que es lo que se cobra en la mayoría de los casos.</summary>
    Private Sub MostrarSeleccion()
        Dim fila = TryCast(dgSaldos.SelectedItem, DataRowView)

        If fila Is Nothing Then
            pnlReserva.Visibility = Visibility.Collapsed
            pnlSinReserva.Visibility = Visibility.Visible
            txtMonto.Clear()
            Return
        End If

        pnlSinReserva.Visibility = Visibility.Collapsed
        pnlReserva.Visibility = Visibility.Visible

        lblLocalizador.Text = fila("codigo_reserva").ToString()
        lblTitular.Text = If(IsDBNull(fila("titular")), "", fila("titular").ToString())
        lblDocumento.Text = "Documento: " & If(IsDBNull(fila("num_documento")), "—", fila("num_documento").ToString())
        lblTotalReserva.Text = Formato.Dinero(fila("costo"))
        lblPagadoReserva.Text = Formato.Dinero(fila("pagado"))
        lblSaldoReserva.Text = Formato.Dinero(fila("saldo"))

        txtMonto.Text = CDec(fila("saldo")).ToString("0.00")
    End Sub

    Private Sub btnCobrar_Click(sender As Object, e As RoutedEventArgs) Handles btnCobrar.Click
        Dim fila = TryCast(dgSaldos.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        If cboMetodo.SelectedValue Is Nothing Then
            DialogoAlas.Show("Elige el método de pago con el que se está cobrando.", "Falta el método de pago",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        If cboTipoComprobante.SelectedItem Is Nothing Then
            DialogoAlas.Show("Elige si se emite factura o recibo.", "Falta el comprobante",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim monto As Decimal
        If Not Decimal.TryParse(txtMonto.Text.Trim(), monto) OrElse monto <= 0 Then
            DialogoAlas.Show("Escribe un monto válido mayor que cero, por ejemplo 1500.00.", "Monto incorrecto",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            txtMonto.Focus()
            txtMonto.SelectAll()
            Return
        End If

        Dim codigo = fila("codigo_reserva").ToString()

        Try
            btnCobrar.IsEnabled = False

            Dim problema = PagoService.Registrar(CInt(fila("idreserva")),
                                                 CInt(cboMetodo.SelectedValue),
                                                 monto,
                                                 cboTipoComprobante.SelectedItem.ToString())
            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "No se pudo registrar el cobro",
                                 MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Cargar()
            DialogoAlas.Show($"Se registró un cobro de {Formato.Dinero(monto)} en la reserva {codigo}.",
                             "Cobro registrado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Registrar el cobro", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        Finally
            btnCobrar.IsEnabled = True
        End Try
    End Sub

    ' ---------- Historial ----------

    Private Sub CargarHistorial()
        Try
            Dim dt = PagoService.Listar(txtBuscar.Text.Trim(), dpDesde.SelectedDate, dpHasta.SelectedDate)
            dgHistorial.ItemsSource = dt.DefaultView

            pnlSinPagos.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblConteoHistorial.Text = If(dt.Rows.Count = 1, "1 cobro", $"{dt.Rows.Count} cobros")

            Dim total As Decimal = 0
            For Each pago As DataRow In dt.Rows
                total += CDec(pago("monto"))
            Next
            lblTotalHistorial.Text = Formato.Dinero(total)

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Consultar el historial de cobros", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        If cargando Then Return
        CargarHistorial()
    End Sub

    Private Sub dpDesde_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dpDesde.SelectedDateChanged
        If cargando Then Return
        CargarHistorial()
    End Sub

    Private Sub dpHasta_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dpHasta.SelectedDateChanged
        If cargando Then Return
        CargarHistorial()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        cargando = True
        txtBuscar.Clear()
        dpDesde.SelectedDate = Nothing
        dpHasta.SelectedDate = Nothing
        cargando = False

        CargarHistorial()
    End Sub
End Class
