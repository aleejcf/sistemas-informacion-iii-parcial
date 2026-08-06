Imports System.Data

''' <summary>Multas: cobrarlas y, cuando hay motivo, condonarlas. Las multas no
''' se crean aquí — las genera el sistema al registrar una devolución tardía,
''' dañada o incompleta; esta página solo las administra.</summary>
Public Class MultasPage

    Private preparado As Boolean = False
    Private ocupado As Boolean = False
    Private idMultaActual As Integer = 0
    Private estadoActual As String = ""

    ' ======================= CICLO DE VIDA =======================

    Public Sub Cargar()
        Preparar()
        LimpiarDetalle()
        CargarLista()
        CargarResumen()
    End Sub

    Private Sub MultasPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True

        ocupado = True
        cboEstado.ItemsSource = MultaService.Estados
        cboMotivo.ItemsSource = MultaService.Motivos
        ocupado = False

        Dim puede = Permisos.PuedeCondonarMultas
        btnCondonar.IsEnabled = puede
        txtMotivoCondonar.IsEnabled = puede
        lblSoloAdmin.Visibility = If(puede, Visibility.Collapsed, Visibility.Visible)
    End Sub

    ' ======================= LISTA =======================

    Private Sub CargarLista()
        Try
            Dim dt = MultaService.Listar(txtBuscar.Text,
                                         TryCast(cboEstado.SelectedItem, String),
                                         TryCast(cboMotivo.SelectedItem, String))
            dgMultas.ItemsSource = dt.DefaultView
            pnlVacio.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar las multas", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub CargarResumen()
        Try
            Dim fila = MultaService.Resumen()
            If fila Is Nothing Then Return

            lblPorCobrar.Text = Formato.Dinero(Db.Monto(fila, "por_cobrar"))
            lblCobrado.Text = Formato.Dinero(Db.Monto(fila, "cobrado"))
            lblCondonado.Text = Formato.Dinero(Db.Monto(fila, "condonado"))
            lblPendientes.Text = Db.Numero(fila, "pendientes").ToString()

        Catch ex As Exception
            Registro.Advertencia($"No se pudo cargar el resumen de multas: {ex.Message}")
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub cboEstado_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboEstado.SelectionChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub cboMotivo_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboMotivo.SelectionChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        ocupado = True
        txtBuscar.Clear()
        cboEstado.SelectedIndex = -1
        cboMotivo.SelectedIndex = -1
        ocupado = False
        CargarLista()
    End Sub

    ' ======================= DETALLE =======================

    Private Sub dgMultas_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgMultas.SelectionChanged
        Dim vista = TryCast(dgMultas.SelectedItem, DataRowView)
        If vista Is Nothing Then
            LimpiarDetalle()
            Return
        End If

        Dim fila = vista.Row
        idMultaActual = Db.Numero(fila, "idmulta")
        estadoActual = Db.Texto(fila, "estado")

        pnlSinSeleccion.Visibility = Visibility.Collapsed
        pnlDetalle.Visibility = Visibility.Visible

        lblFolio.Text = Db.Texto(fila, "codigo")
        lblSocio.Text = Db.Texto(fila, "socio")
        lblMonto.Text = Formato.Dinero(Db.Monto(fila, "monto"))
        lblMotivo.Text = Db.Texto(fila, "motivo")

        Dim dias = Db.Numero(fila, "dias_retraso")
        lblDias.Text = If(dias > 0, $"{dias} días", "—")

        lblEstado.Text = estadoActual.ToUpper()
        lblEstado.Foreground = EstadoAColorConverter.Trazo(estadoActual)
        pnlEstado.Background = EstadoAColorFondoConverter.Relleno(estadoActual)

        Dim observacion = Db.Texto(fila, "observacion")
        lblObservacion.Text = If(observacion = "", "Sin detalle registrado.", observacion)

        ' Una multa ya resuelta no se vuelve a cobrar ni a condonar
        Dim pendiente = estadoActual = "Pendiente"
        btnCobrar.IsEnabled = pendiente
        btnCondonar.IsEnabled = pendiente AndAlso Permisos.PuedeCondonarMultas
        txtMotivoCondonar.IsEnabled = btnCondonar.IsEnabled

        Dim visibleCondonar = If(pendiente, Visibility.Visible, Visibility.Collapsed)
        lblEtiquetaCondonar.Visibility = visibleCondonar
        txtMotivoCondonar.Visibility = visibleCondonar

        TransicionVentana.FundirEntrada(pnlDetalle)
    End Sub

    Private Sub LimpiarDetalle()
        idMultaActual = 0
        estadoActual = ""
        txtMotivoCondonar.Clear()
        pnlDetalle.Visibility = Visibility.Collapsed
        pnlSinSeleccion.Visibility = Visibility.Visible
    End Sub

    ' ======================= ACCIONES =======================

    Private Sub btnCobrar_Click(sender As Object, e As RoutedEventArgs) Handles btnCobrar.Click
        If idMultaActual = 0 Then Return

        If DialogoBiblioteca.Show($"¿Registrar el cobro de {lblMonto.Text} a {lblSocio.Text}?",
                                  "Cobrar multa", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim recibo As String = ""
            Dim problema = MultaService.Pagar(idMultaActual, recibo)

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo cobrar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                CargarLista()
                Return
            End If

            Dim monto = lblMonto.Text
            CargarLista()
            CargarResumen()
            LimpiarDetalle()

            DialogoBiblioteca.MostrarComprobante(
                "El cobro quedó registrado. Entrégale el recibo al socio.",
                "Multa cobrada con éxito", monto, recibo)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cobrar la multa", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnCondonar_Click(sender As Object, e As RoutedEventArgs) Handles btnCondonar.Click
        If idMultaActual = 0 Then Return

        If String.IsNullOrWhiteSpace(txtMotivoCondonar.Text) Then
            DialogoBiblioteca.Show("Escribe el motivo por el que se condona la multa. " &
                                   "Perdonar dinero sin justificación deja el control sin rastro.",
                                   "Falta el motivo", MessageBoxButton.OK, MessageBoxImage.Warning)
            txtMotivoCondonar.Focus()
            Return
        End If

        If DialogoBiblioteca.Show($"¿Condonar los {lblMonto.Text} de {lblSocio.Text}? " &
                                  "Quedará registrado en la bitácora con tu nombre.",
                                  "Condonar multa", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = MultaService.Condonar(idMultaActual, txtMotivoCondonar.Text)

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo condonar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            CargarResumen()
            LimpiarDetalle()
            DialogoBiblioteca.Show("La multa quedó condonada.", "Condonada con éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Condonar la multa", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub
End Class
