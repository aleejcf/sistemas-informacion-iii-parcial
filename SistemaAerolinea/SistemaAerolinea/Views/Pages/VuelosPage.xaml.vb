Imports System.Data

''' <summary>Alta y mantenimiento de la malla de vuelos: a la izquierda la lista
''' con sus filtros y a la derecha el formulario del vuelo seleccionado.</summary>
Public Class VuelosPage

    Private preparado As Boolean = False
    Private editandoId As Integer = 0

    ' Los aviones se traen una sola vez y se filtran en memoria por aerolínea:
    ' así elegir aerolínea no obliga a volver a consultar la base de datos.
    Private dtAviones As DataTable

    ' Evita que llenar los combos y limpiar los filtros dispare consultas de más
    Private ocupado As Boolean = False

    Public Sub Cargar()
        Preparar()
        CargarCombos()
        LimpiarFormulario()
        CargarLista()
    End Sub

    Private Sub VuelosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True

        ocupado = True
        cboEstado.ItemsSource = VueloService.Estados
        cboEstadoVuelo.ItemsSource = VueloService.Estados
        cboHoraSalida.ItemsSource = Horas()
        cboHoraLlegada.ItemsSource = Horas()
        cboMinutoSalida.ItemsSource = Minutos()
        cboMinutoLlegada.ItemsSource = Minutos()
        ocupado = False

        btnEliminar.IsEnabled = Permisos.PuedeEliminar
        lblSinPermiso.Visibility = If(Permisos.PuedeEliminar, Visibility.Collapsed, Visibility.Visible)
    End Sub

    Private Shared Function Horas() As List(Of String)
        Dim lista As New List(Of String)
        For h = 0 To 23
            lista.Add(h.ToString("00"))
        Next
        Return lista
    End Function

    Private Shared Function Minutos() As List(Of String)
        Dim lista As New List(Of String)
        For m = 0 To 55 Step 5
            lista.Add(m.ToString("00"))
        Next
        Return lista
    End Function

    ' ---------- Datos ----------

    Private Sub CargarCombos()
        Try
            ocupado = True

            Dim aerolineaElegida = cboAerolinea.SelectedValue
            cboAerolinea.ItemsSource = CatalogoService.AerolineasParaCombo().DefaultView
            cboAerolinea.SelectedValue = aerolineaElegida

            Dim origenElegido = cboOrigen.SelectedValue
            Dim destinoElegido = cboDestino.SelectedValue
            cboOrigen.ItemsSource = CatalogoService.AeropuertosParaCombo().DefaultView
            cboDestino.ItemsSource = CatalogoService.AeropuertosParaCombo().DefaultView
            cboOrigen.SelectedValue = origenElegido
            cboDestino.SelectedValue = destinoElegido

            dtAviones = CatalogoService.AvionesParaCombo()
            FiltrarAviones()

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Cargar los catálogos de vuelos", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        Finally
            ocupado = False
        End Try
    End Sub

    Private Sub CargarLista()
        Try
            Dim dt = VueloService.Listar(txtBuscar.Text,
                                         TryCast(cboEstado.SelectedItem, String),
                                         dtpDesde.SelectedDate)
            dgVuelos.ItemsSource = dt.DefaultView
            pnlVacio.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            ActualizarIndicadores(dt)

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Consultar la malla de vuelos", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ActualizarIndicadores(dt As DataTable)
        Dim programados = 0, enOperacion = 0
        Dim sumaOcupacion As Decimal = 0

        For Each fila As DataRow In dt.Rows
            Dim estado = fila("estado").ToString()
            If estado = "Programado" Then programados += 1
            If estado = "Abordando" OrElse estado = "En vuelo" Then enOperacion += 1
            If Not IsDBNull(fila("ocupacion")) Then sumaOcupacion += CDec(fila("ocupacion"))
        Next

        lblIndTotal.Text = dt.Rows.Count.ToString()
        lblIndProgramados.Text = programados.ToString()
        lblIndOperacion.Text = enOperacion.ToString()
        lblIndOcupacion.Text = If(dt.Rows.Count = 0, "0%",
                                  Math.Round(sumaOcupacion / dt.Rows.Count).ToString() & "%")
    End Sub

    ''' <summary>Un avión solo puede volar para su propia aerolínea, así que la
    ''' lista se recorta con la aerolínea elegida en vez de mostrarlos todos.</summary>
    Private Sub FiltrarAviones()
        If dtAviones Is Nothing Then Return

        Dim avionElegido = If(cboAvion.SelectedValue Is Nothing, "", cboAvion.SelectedValue.ToString())
        Dim vista As New DataView(dtAviones)

        If cboAerolinea.SelectedValue IsNot Nothing Then
            vista.RowFilter = "idaerolinea = " & CInt(cboAerolinea.SelectedValue)
            cboAvion.Tag = "Elige el avión"
        Else
            vista.RowFilter = "1 = 0"
            cboAvion.Tag = "Elige primero la aerolínea"
        End If

        cboAvion.ItemsSource = vista
        If avionElegido <> "" Then cboAvion.SelectedValue = avionElegido
    End Sub

    ' ---------- Filtros ----------

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub cboEstado_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboEstado.SelectionChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub dtpDesde_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dtpDesde.SelectedDateChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        ocupado = True
        txtBuscar.Clear()
        cboEstado.SelectedIndex = -1
        dtpDesde.SelectedDate = Nothing
        ocupado = False
        CargarLista()
    End Sub

    ' ---------- Formulario ----------

    Private Sub dgVuelos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgVuelos.SelectionChanged
        Dim fila = TryCast(dgVuelos.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editandoId = CInt(fila("idvuelo"))
        txtCodigo.Text = fila("codigo_vuelo").ToString()
        cboAerolinea.SelectedValue = CInt(fila("idaerolinea"))
        cboAvion.SelectedValue = fila("idavion").ToString()
        cboOrigen.SelectedValue = fila("idaeropuerto_origen").ToString()
        cboDestino.SelectedValue = fila("idaeropuerto_destino").ToString()

        PonerFechaHora(dtpSalida, cboHoraSalida, cboMinutoSalida, CDate(fila("fecha_salida")))
        PonerFechaHora(dtpLlegada, cboHoraLlegada, cboMinutoLlegada, CDate(fila("fecha_llegada")))

        txtPrecio.Text = CDec(fila("precio_base")).ToString("0.00")
        cboEstadoVuelo.SelectedItem = fila("estado").ToString()
        txtPuerta.Text = If(IsDBNull(fila("puerta")), "", fila("puerta").ToString())

        lblTituloFormulario.Text = "Editar vuelo"
        lblSubtituloFormulario.Text = fila("codigo_vuelo").ToString() & " · " & fila("ruta").ToString()
        RevisarBoletosVendidos()
    End Sub

    ''' <summary>Cambiar el horario o el avión de un vuelo con pasajeros ya emitidos
    ''' tiene consecuencias reales, por eso se avisa antes de tocar nada.</summary>
    Private Sub RevisarBoletosVendidos()
        Try
            Dim vendidos = VueloService.BoletosVendidos(editandoId)
            If vendidos > 0 Then
                lblAvisoBoletos.Text = $"Este vuelo tiene {vendidos} boletos vendidos. " &
                                       "Cambiar el horario o el avión afecta a pasajeros con reserva."
                pnlAvisoBoletos.Visibility = Visibility.Visible
            Else
                pnlAvisoBoletos.Visibility = Visibility.Collapsed
            End If
        Catch ex As Exception
            pnlAvisoBoletos.Visibility = Visibility.Collapsed
        End Try
    End Sub

    Private Sub cboAerolinea_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboAerolinea.SelectionChanged
        FiltrarAviones()
    End Sub

    Private Sub btnSugerir_Click(sender As Object, e As RoutedEventArgs) Handles btnSugerir.Click
        If cboAerolinea.SelectedValue Is Nothing Then
            DialogoAlas.Show("Elige primero la aerolínea para poder sugerir el código.", "Falta la aerolínea",
                             MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If

        Try
            Dim fecha = If(dtpSalida.SelectedDate.HasValue, dtpSalida.SelectedDate.Value, Date.Today)
            Dim sugerido = VueloService.SugerirCodigo(CInt(cboAerolinea.SelectedValue), fecha)

            If String.IsNullOrWhiteSpace(sugerido) Then
                DialogoAlas.Show("No se pudo generar un código para esa aerolínea.", "Sin sugerencia",
                                 MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            txtCodigo.Text = sugerido

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Sugerir el código del vuelo", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevo.Click
        dgVuelos.SelectedItem = Nothing
        LimpiarFormulario()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        If String.IsNullOrWhiteSpace(txtCodigo.Text) Then
            Avisar("Escribe el código del vuelo.")
            Return
        End If
        If cboAerolinea.SelectedValue Is Nothing OrElse cboAvion.SelectedValue Is Nothing Then
            Avisar("Elige la aerolínea y el avión del vuelo.")
            Return
        End If
        If cboOrigen.SelectedValue Is Nothing OrElse cboDestino.SelectedValue Is Nothing Then
            Avisar("Elige el aeropuerto de origen y el de destino.")
            Return
        End If

        Dim salida = ArmarFechaHora(dtpSalida, cboHoraSalida, cboMinutoSalida)
        Dim llegada = ArmarFechaHora(dtpLlegada, cboHoraLlegada, cboMinutoLlegada)
        If Not salida.HasValue OrElse Not llegada.HasValue Then
            Avisar("Completa la fecha y la hora de salida y de llegada.")
            Return
        End If

        Dim precio As Decimal
        If Not Decimal.TryParse(txtPrecio.Text.Trim(), precio) OrElse precio <= 0 Then
            Avisar("El precio base debe ser un número mayor que cero.")
            Return
        End If

        If cboEstadoVuelo.SelectedItem Is Nothing Then
            Avisar("Elige el estado del vuelo.")
            Return
        End If

        Try
            Dim problema As String
            If editandoId = 0 Then
                problema = VueloService.Crear(txtCodigo.Text.Trim(),
                                              CInt(cboAerolinea.SelectedValue),
                                              cboAvion.SelectedValue.ToString(),
                                              cboOrigen.SelectedValue.ToString(),
                                              cboDestino.SelectedValue.ToString(),
                                              salida.Value, llegada.Value, precio,
                                              cboEstadoVuelo.SelectedItem.ToString(),
                                              txtPuerta.Text.Trim())
            Else
                problema = VueloService.Actualizar(editandoId, txtCodigo.Text.Trim(),
                                                   CInt(cboAerolinea.SelectedValue),
                                                   cboAvion.SelectedValue.ToString(),
                                                   cboOrigen.SelectedValue.ToString(),
                                                   cboDestino.SelectedValue.ToString(),
                                                   salida.Value, llegada.Value, precio,
                                                   cboEstadoVuelo.SelectedItem.ToString(),
                                                   txtPuerta.Text.Trim())
            End If

            ' El formulario se conserva tal cual para que el usuario corrija el dato señalado
            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "No se pudo guardar", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim codigo = txtCodigo.Text.Trim().ToUpper()
            CargarLista()
            dgVuelos.SelectedItem = Nothing
            LimpiarFormulario()
            DialogoAlas.Show($"El vuelo {codigo} quedó guardado.", "Guardado con éxito",
                             MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Guardar el vuelo", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminar.Click
        If editandoId = 0 Then
            Avisar("Selecciona primero un vuelo de la lista.")
            Return
        End If

        Dim codigo = txtCodigo.Text.Trim().ToUpper()
        If DialogoAlas.Show($"¿Eliminar el vuelo {codigo}? Esta acción no se puede deshacer.", "Confirmar",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = VueloService.Eliminar(editandoId)
            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "No se pudo eliminar", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            dgVuelos.SelectedItem = Nothing
            LimpiarFormulario()
            DialogoAlas.Show($"El vuelo {codigo} se eliminó de la malla.", "Eliminado con éxito",
                             MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Eliminar el vuelo", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- Auxiliares ----------

    Private Sub LimpiarFormulario()
        editandoId = 0
        txtCodigo.Clear()
        txtPrecio.Clear()
        txtPuerta.Clear()
        cboAerolinea.SelectedIndex = -1
        cboAvion.SelectedIndex = -1
        cboOrigen.SelectedIndex = -1
        cboDestino.SelectedIndex = -1
        dtpSalida.SelectedDate = Nothing
        dtpLlegada.SelectedDate = Nothing
        cboHoraSalida.SelectedIndex = -1
        cboMinutoSalida.SelectedIndex = -1
        cboHoraLlegada.SelectedIndex = -1
        cboMinutoLlegada.SelectedIndex = -1
        cboEstadoVuelo.SelectedItem = "Programado"

        pnlAvisoBoletos.Visibility = Visibility.Collapsed
        lblTituloFormulario.Text = "Nuevo vuelo"
        lblSubtituloFormulario.Text = "Programa un vuelo en la malla"
    End Sub

    Private Shared Sub PonerFechaHora(selector As DatePicker, cboHora As ComboBox,
                                      cboMinuto As ComboBox, valor As DateTime)
        selector.SelectedDate = valor.Date
        cboHora.SelectedItem = valor.ToString("HH")
        ' Los minutos van de cinco en cinco: se acerca al múltiplo anterior
        cboMinuto.SelectedItem = ((valor.Minute \ 5) * 5).ToString("00")
    End Sub

    Private Shared Function ArmarFechaHora(selector As DatePicker, cboHora As ComboBox,
                                           cboMinuto As ComboBox) As DateTime?
        If Not selector.SelectedDate.HasValue Then Return Nothing
        If cboHora.SelectedItem Is Nothing OrElse cboMinuto.SelectedItem Is Nothing Then Return Nothing

        Return selector.SelectedDate.Value.Date.
            AddHours(CInt(cboHora.SelectedItem)).
            AddMinutes(CInt(cboMinuto.SelectedItem))
    End Function

    Private Shared Sub Avisar(mensaje As String)
        DialogoAlas.Show(mensaje, "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning)
    End Sub
End Class
