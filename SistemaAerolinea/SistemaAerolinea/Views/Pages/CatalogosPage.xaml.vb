Imports System.Data

''' <summary>Mantenimiento de los catálogos del negocio en seis pestañas.
''' Las seis siguen el mismo patrón (lista + formulario), pero el manejo de errores
''' y la comprobación de permisos están centralizados para no repetirlos.</summary>
Public Class CatalogosPage

    Private editandoPais As Boolean = False
    Private editandoAeropuerto As Boolean = False
    Private editandoAerolinea As Boolean = False
    Private editandoAvion As Boolean = False
    Private editandoMetodo As Boolean = False
    Private idTarifaActual As Integer = 0

    Private Sub CatalogosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If cboAsientosPorFila.ItemsSource Is Nothing Then
            cboAsientosPorFila.ItemsSource = New Integer() {4, 6, 8}
            cboAsientosPorFila.SelectedIndex = 1
        End If

        ' Para un Agente los catálogos son de solo lectura: son configuración del
        ' negocio y tocarlos afecta a los vuelos y a los precios ya publicados.
        If Not Permisos.PuedeEditarCatalogos Then
            pnlSoloLectura.Visibility = Visibility.Visible
            For Each boton In {btnGuardarPais, btnEliminarPais,
                               btnGuardarAeropuerto, btnEliminarAeropuerto,
                               btnGuardarAerolinea, btnEliminarAerolinea,
                               btnGuardarAvion, btnEliminarAvion,
                               btnGuardarTarifa,
                               btnGuardarMetodo, btnEliminarMetodo}
                boton.IsEnabled = False
            Next
        End If
    End Sub

    Public Sub Cargar()
        CargarPaises()
        CargarAeropuertos()
        CargarAerolineas()
        CargarAviones()
        CargarTarifas()
        CargarMetodos()
        CargarCombos()
    End Sub

    Private Sub CargarCombos()
        Try
            cboPaisAeropuerto.ItemsSource = CatalogoService.PaisesParaCombo().DefaultView
            cboAerolineaAvion.ItemsSource = CatalogoService.AerolineasParaCombo().DefaultView
        Catch ex As Exception
            Avisar("Cargar las listas de apoyo", ex)
        End Try
    End Sub

    Private Sub tabCatalogos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles tabCatalogos.SelectionChanged
        ' Solo interesa el cambio de pestaña, no el de los controles que hay dentro
        If Not IsLoaded OrElse Not ReferenceEquals(e.OriginalSource, tabCatalogos) Then Return

        Select Case tabCatalogos.SelectedIndex
            Case 0 : CargarPaises()
            Case 1 : CargarAeropuertos()
            Case 2 : CargarAerolineas()
            Case 3 : CargarAviones()
            Case 4 : CargarTarifas()
            Case 5 : CargarMetodos()
        End Select
    End Sub

    ' ════════════════════════════ PAÍSES ════════════════════════════

    Private Sub CargarPaises()
        Try
            Dim datos = CatalogoService.ListarPaises(txtBuscarPais.Text)
            dgPaises.ItemsSource = datos.DefaultView
            pnlVacioPaises.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblTotalPaises.Text = $"{datos.Rows.Count} país(es)"
        Catch ex As Exception
            Avisar("Cargar los países", ex)
        End Try
    End Sub

    Private Sub txtBuscarPais_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarPais.TextChanged
        CargarPaises()
    End Sub

    Private Sub dgPaises_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgPaises.SelectionChanged

        Dim fila = TryCast(dgPaises.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editandoPais = True
        lblModoPais.Text = "Editando país"
        txtIdPais.Text = fila("idpais").ToString()
        txtIdPais.IsEnabled = False
        txtNombrePais.Text = fila("nombre_pais").ToString()

        lblUsoPais.Text = $"{fila("aeropuertos")} aeropuerto(s) y {fila("pasajeros")} pasajero(s) dependen de este país."
        pnlUsoPais.Visibility = Visibility.Visible
    End Sub

    Private Sub btnNuevoPais_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevoPais.Click
        LimpiarPais()
    End Sub

    Private Sub btnGuardarPais_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarPais.Click
        If String.IsNullOrWhiteSpace(txtIdPais.Text) OrElse String.IsNullOrWhiteSpace(txtNombrePais.Text) Then
            Advertir("Escribe el código y el nombre del país.")
            Return
        End If

        Try
            If Not editandoPais AndAlso CatalogoService.ExistePais(txtIdPais.Text) Then
                Advertir("Ya existe un país con ese código.")
                Return
            End If

            CatalogoService.GuardarPais(txtIdPais.Text, txtNombrePais.Text, editandoPais)
            Exito("El país se guardó.")
            CargarPaises()
            CargarCombos()
            LimpiarPais()
        Catch ex As Exception
            Avisar("Guardar el país", ex)
        End Try
    End Sub

    Private Sub btnEliminarPais_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminarPais.Click
        If Not editandoPais Then
            Advertir("Selecciona primero un país de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar el país {txtNombrePais.Text}?") Then Return

        Try
            CatalogoService.EliminarPais(txtIdPais.Text)
            Exito("El país se eliminó.")
            CargarPaises()
            CargarCombos()
            LimpiarPais()
        Catch ex As Exception
            Avisar("Eliminar el país", ex)
        End Try
    End Sub

    Private Sub LimpiarPais()
        editandoPais = False
        lblModoPais.Text = "Nuevo país"
        txtIdPais.Clear()
        txtIdPais.IsEnabled = True
        txtNombrePais.Clear()
        pnlUsoPais.Visibility = Visibility.Collapsed
        dgPaises.SelectedItem = Nothing
        txtIdPais.Focus()
    End Sub

    ' ══════════════════════════ AEROPUERTOS ══════════════════════════

    Private Sub CargarAeropuertos()
        Try
            Dim datos = CatalogoService.ListarAeropuertos(txtBuscarAeropuerto.Text)
            dgAeropuertos.ItemsSource = datos.DefaultView
            pnlVacioAeropuertos.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblTotalAeropuertos.Text = $"{datos.Rows.Count} aeropuerto(s)"
        Catch ex As Exception
            Avisar("Cargar los aeropuertos", ex)
        End Try
    End Sub

    Private Sub txtBuscarAeropuerto_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarAeropuerto.TextChanged
        CargarAeropuertos()
    End Sub

    Private Sub dgAeropuertos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgAeropuertos.SelectionChanged

        Dim fila = TryCast(dgAeropuertos.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editandoAeropuerto = True
        lblModoAeropuerto.Text = "Editando aeropuerto"
        txtIdAeropuerto.Text = fila("idaeropuerto").ToString()
        txtIdAeropuerto.IsEnabled = False
        txtIata.Text = fila("iata").ToString()
        txtNombreAeropuerto.Text = fila("nombre").ToString()
        txtCiudad.Text = fila("ciudad").ToString()
        cboPaisAeropuerto.SelectedValue = fila("idpais").ToString()

        lblUsoAeropuerto.Text = $"{fila("vuelos")} vuelo(s) usan este aeropuerto."
        pnlUsoAeropuerto.Visibility = Visibility.Visible
    End Sub

    Private Sub btnNuevoAeropuerto_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevoAeropuerto.Click
        LimpiarAeropuerto()
    End Sub

    Private Sub btnGuardarAeropuerto_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarAeropuerto.Click
        If String.IsNullOrWhiteSpace(txtIdAeropuerto.Text) OrElse
           String.IsNullOrWhiteSpace(txtNombreAeropuerto.Text) OrElse
           String.IsNullOrWhiteSpace(txtCiudad.Text) OrElse cboPaisAeropuerto.SelectedValue Is Nothing Then
            Advertir("Completa el código, el nombre, la ciudad y el país.")
            Return
        End If

        If Not Validador.EsIataValido(txtIata.Text) Then
            Advertir("El código IATA debe tener exactamente 3 letras (por ejemplo TGU).")
            Return
        End If

        Try
            If Not editandoAeropuerto AndAlso CatalogoService.ExisteAeropuerto(txtIdAeropuerto.Text) Then
                Advertir("Ya existe un aeropuerto con ese código.")
                Return
            End If

            CatalogoService.GuardarAeropuerto(txtIdAeropuerto.Text, txtNombreAeropuerto.Text,
                                              txtCiudad.Text, txtIata.Text,
                                              cboPaisAeropuerto.SelectedValue.ToString(), editandoAeropuerto)
            Exito("El aeropuerto se guardó.")
            CargarAeropuertos()
            LimpiarAeropuerto()
        Catch ex As Exception
            Avisar("Guardar el aeropuerto", ex)
        End Try
    End Sub

    Private Sub btnEliminarAeropuerto_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminarAeropuerto.Click
        If Not editandoAeropuerto Then
            Advertir("Selecciona primero un aeropuerto de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar el aeropuerto {txtNombreAeropuerto.Text}?") Then Return

        Try
            CatalogoService.EliminarAeropuerto(txtIdAeropuerto.Text)
            Exito("El aeropuerto se eliminó.")
            CargarAeropuertos()
            LimpiarAeropuerto()
        Catch ex As Exception
            Avisar("Eliminar el aeropuerto", ex)
        End Try
    End Sub

    Private Sub LimpiarAeropuerto()
        editandoAeropuerto = False
        lblModoAeropuerto.Text = "Nuevo aeropuerto"
        txtIdAeropuerto.Clear()
        txtIdAeropuerto.IsEnabled = True
        txtIata.Clear()
        txtNombreAeropuerto.Clear()
        txtCiudad.Clear()
        cboPaisAeropuerto.SelectedIndex = -1
        pnlUsoAeropuerto.Visibility = Visibility.Collapsed
        dgAeropuertos.SelectedItem = Nothing
    End Sub

    ' ══════════════════════════ AEROLÍNEAS ══════════════════════════

    Private Sub CargarAerolineas()
        Try
            Dim datos = CatalogoService.ListarAerolineas(txtBuscarAerolinea.Text)
            dgAerolineas.ItemsSource = datos.DefaultView
            pnlVacioAerolineas.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblTotalAerolineas.Text = $"{datos.Rows.Count} aerolínea(s)"
        Catch ex As Exception
            Avisar("Cargar las aerolíneas", ex)
        End Try
    End Sub

    Private Sub txtBuscarAerolinea_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarAerolinea.TextChanged
        CargarAerolineas()
    End Sub

    Private Sub dgAerolineas_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgAerolineas.SelectionChanged

        Dim fila = TryCast(dgAerolineas.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editandoAerolinea = True
        lblModoAerolinea.Text = "Editando aerolínea"
        txtIdAerolinea.Text = fila("idaerolinea").ToString()
        txtIdAerolinea.IsEnabled = False
        txtCodigoAerolinea.Text = fila("codigo").ToString()
        txtNombreAerolinea.Text = fila("nombre_aero").ToString()
        txtRtn.Text = fila("rtn").ToString()

        lblUsoAerolinea.Text = $"{fila("aviones")} avión(es) y {fila("vuelos")} vuelo(s) pertenecen a esta aerolínea."
        pnlUsoAerolinea.Visibility = Visibility.Visible
    End Sub

    Private Sub btnNuevaAerolinea_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevaAerolinea.Click
        LimpiarAerolinea()
        Try
            txtIdAerolinea.Text = CatalogoService.SiguienteIdAerolinea().ToString()
        Catch ex As Exception
            Avisar("Sugerir el identificador de la aerolínea", ex)
        End Try
    End Sub

    Private Sub btnGuardarAerolinea_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarAerolinea.Click
        Dim id As Integer
        If Not Integer.TryParse(txtIdAerolinea.Text.Trim(), id) Then
            Advertir("El identificador de la aerolínea debe ser un número.")
            Return
        End If
        If txtCodigoAerolinea.Text.Trim().Length <> 2 Then
            Advertir("El código de la aerolínea debe tener exactamente 2 caracteres (por ejemplo AV).")
            Return
        End If
        If String.IsNullOrWhiteSpace(txtNombreAerolinea.Text) OrElse String.IsNullOrWhiteSpace(txtRtn.Text) Then
            Advertir("Escribe el nombre y el RTN de la aerolínea.")
            Return
        End If

        Try
            If Not editandoAerolinea AndAlso CatalogoService.ExisteAerolinea(id) Then
                Advertir("Ya existe una aerolínea con ese identificador.")
                Return
            End If

            CatalogoService.GuardarAerolinea(id, txtCodigoAerolinea.Text, txtNombreAerolinea.Text,
                                             txtRtn.Text, editandoAerolinea)
            Exito("La aerolínea se guardó.")
            CargarAerolineas()
            CargarCombos()
            LimpiarAerolinea()
        Catch ex As Exception
            Avisar("Guardar la aerolínea", ex)
        End Try
    End Sub

    Private Sub btnEliminarAerolinea_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminarAerolinea.Click
        If Not editandoAerolinea Then
            Advertir("Selecciona primero una aerolínea de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar la aerolínea {txtNombreAerolinea.Text}?") Then Return

        Try
            CatalogoService.EliminarAerolinea(CInt(txtIdAerolinea.Text))
            Exito("La aerolínea se eliminó.")
            CargarAerolineas()
            CargarCombos()
            LimpiarAerolinea()
        Catch ex As Exception
            Avisar("Eliminar la aerolínea", ex)
        End Try
    End Sub

    Private Sub LimpiarAerolinea()
        editandoAerolinea = False
        lblModoAerolinea.Text = "Nueva aerolínea"
        txtIdAerolinea.Clear()
        txtIdAerolinea.IsEnabled = True
        txtCodigoAerolinea.Clear()
        txtNombreAerolinea.Clear()
        txtRtn.Clear()
        pnlUsoAerolinea.Visibility = Visibility.Collapsed
        dgAerolineas.SelectedItem = Nothing
    End Sub

    ' ════════════════════════════ AVIONES ════════════════════════════

    Private Sub CargarAviones()
        Try
            Dim datos = CatalogoService.ListarAviones(txtBuscarAvion.Text)
            dgAviones.ItemsSource = datos.DefaultView
            pnlVacioAviones.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblTotalAviones.Text = $"{datos.Rows.Count} avión(es)"
        Catch ex As Exception
            Avisar("Cargar los aviones", ex)
        End Try
    End Sub

    Private Sub txtBuscarAvion_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarAvion.TextChanged
        CargarAviones()
    End Sub

    Private Sub dgAviones_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgAviones.SelectionChanged

        Dim fila = TryCast(dgAviones.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editandoAvion = True
        lblModoAvion.Text = "Editando avión"
        txtIdAvion.Text = fila("idavion").ToString()
        txtIdAvion.IsEnabled = False
        cboAerolineaAvion.SelectedValue = CInt(fila("idaerolinea"))
        txtFabricante.Text = If(IsDBNull(fila("fabricante")), "", fila("fabricante").ToString())
        txtTipoAvion.Text = fila("tipo").ToString()
        txtCapacidad.Text = fila("capacidad_pasajeros").ToString()
        cboAsientosPorFila.SelectedItem = CInt(fila("asientos_por_fila"))

        ' La cabina ya está construida: cambiarla dejaría boletos sin asiento válido
        txtCapacidad.IsEnabled = False
        cboAsientosPorFila.IsEnabled = False
        pnlAvisoAvion.Visibility = Visibility.Visible

        lblUsoAvion.Text = $"{fila("asientos")} asiento(s) generados · {fila("vuelos")} vuelo(s) programados."
        pnlUsoAvion.Visibility = Visibility.Visible
    End Sub

    Private Sub btnNuevoAvion_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevoAvion.Click
        LimpiarAvion()
    End Sub

    Private Sub btnGuardarAvion_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarAvion.Click
        If String.IsNullOrWhiteSpace(txtIdAvion.Text) OrElse String.IsNullOrWhiteSpace(txtTipoAvion.Text) OrElse
           cboAerolineaAvion.SelectedValue Is Nothing Then
            Advertir("Completa la matrícula, la aerolínea y el tipo de aeronave.")
            Return
        End If

        Try
            If editandoAvion Then
                CatalogoService.ActualizarAvion(txtIdAvion.Text, CInt(cboAerolineaAvion.SelectedValue),
                                                txtFabricante.Text, txtTipoAvion.Text)
                Exito("El avión se actualizó.")
            Else
                Dim capacidad As Integer
                If Not Integer.TryParse(txtCapacidad.Text.Trim(), capacidad) OrElse
                   capacidad < 1 OrElse capacidad > 600 Then
                    Advertir("La capacidad debe ser un número entre 1 y 600.")
                    Return
                End If
                If CatalogoService.ExisteAvion(txtIdAvion.Text) Then
                    Advertir("Ya existe un avión con esa matrícula.")
                    Return
                End If

                CatalogoService.CrearAvion(txtIdAvion.Text, CInt(cboAerolineaAvion.SelectedValue),
                                           txtFabricante.Text, txtTipoAvion.Text, capacidad,
                                           CInt(cboAsientosPorFila.SelectedItem))
                Exito($"El avión se registró y se le generaron sus {capacidad} asientos.")
            End If

            CargarAviones()
            LimpiarAvion()
        Catch ex As Exception
            Avisar("Guardar el avión", ex)
        End Try
    End Sub

    Private Sub btnEliminarAvion_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminarAvion.Click
        If Not editandoAvion Then
            Advertir("Selecciona primero un avión de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar el avión {txtIdAvion.Text} y todo su mapa de asientos?") Then Return

        Try
            CatalogoService.EliminarAvion(txtIdAvion.Text)
            Exito("El avión se eliminó.")
            CargarAviones()
            LimpiarAvion()
        Catch ex As Exception
            Avisar("Eliminar el avión", ex)
        End Try
    End Sub

    Private Sub LimpiarAvion()
        editandoAvion = False
        lblModoAvion.Text = "Nuevo avión"
        txtIdAvion.Clear()
        txtIdAvion.IsEnabled = True
        cboAerolineaAvion.SelectedIndex = -1
        txtFabricante.Clear()
        txtTipoAvion.Clear()
        txtCapacidad.Clear()
        txtCapacidad.IsEnabled = True
        cboAsientosPorFila.SelectedIndex = 1
        cboAsientosPorFila.IsEnabled = True
        pnlAvisoAvion.Visibility = Visibility.Collapsed
        pnlUsoAvion.Visibility = Visibility.Collapsed
        lblNotaAvion.Text = "Al guardar se genera automáticamente el mapa de asientos completo de la aeronave."
        dgAviones.SelectedItem = Nothing
    End Sub

    ' ════════════════════════════ TARIFAS ════════════════════════════

    Private Sub CargarTarifas()
        Try
            Dim datos = CatalogoService.ListarTarifas()
            dgTarifas.ItemsSource = datos.DefaultView
            pnlVacioTarifas.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
        Catch ex As Exception
            Avisar("Cargar las tarifas", ex)
        End Try
    End Sub

    Private Sub dgTarifas_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgTarifas.SelectionChanged

        Dim fila = TryCast(dgTarifas.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        idTarifaActual = CInt(fila("idtarifa"))
        lblModoTarifa.Text = $"Editando la tarifa de {fila("clase")}"
        txtClaseTarifa.Text = fila("clase").ToString()
        txtMultiplicador.Text = CDec(fila("multiplicador")).ToString("0.00")
        txtImpuestoTarifa.Text = CDec(fila("impuesto_pct")).ToString("0.##")
        txtEquipaje.Text = fila("equipaje_incluido_kg").ToString()

        lblUsoTarifa.Text = $"{fila("asientos")} asiento(s) de esta clase · {fila("boletos")} boleto(s) vendidos."
    End Sub

    Private Sub btnGuardarTarifa_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarTarifa.Click
        If idTarifaActual = 0 Then
            Advertir("Selecciona primero una tarifa de la lista.")
            Return
        End If

        Dim multiplicador, impuestoPct As Decimal
        Dim equipaje As Integer

        If Not Decimal.TryParse(txtMultiplicador.Text.Trim(), multiplicador) OrElse multiplicador <= 0 Then
            Advertir("El multiplicador debe ser un número mayor que cero (por ejemplo 2.60).")
            Return
        End If
        If Not Decimal.TryParse(txtImpuestoTarifa.Text.Trim(), impuestoPct) OrElse
           impuestoPct < 0 OrElse impuestoPct >= 100 Then
            Advertir("El impuesto debe ser un porcentaje entre 0 y 99 (por ejemplo 15).")
            Return
        End If
        If Not Integer.TryParse(txtEquipaje.Text.Trim(), equipaje) OrElse equipaje < 0 Then
            Advertir("El equipaje incluido debe ser un número de kilogramos.")
            Return
        End If

        Try
            ' En la base de datos el impuesto se guarda como fracción (0.15), no como 15
            CatalogoService.ActualizarTarifa(idTarifaActual, multiplicador, impuestoPct / 100D, equipaje)
            Exito("La tarifa se actualizó. Los boletos ya emitidos conservan su precio original.")
            CargarTarifas()
        Catch ex As Exception
            Avisar("Guardar la tarifa", ex)
        End Try
    End Sub

    ' ════════════════════════ MÉTODOS DE PAGO ════════════════════════

    Private Sub CargarMetodos()
        Try
            Dim datos = CatalogoService.ListarMetodosPago()
            dgMetodos.ItemsSource = datos.DefaultView
            pnlVacioMetodos.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblTotalMetodos.Text = $"{datos.Rows.Count} método(s)"
        Catch ex As Exception
            Avisar("Cargar los métodos de pago", ex)
        End Try
    End Sub

    Private Sub dgMetodos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgMetodos.SelectionChanged

        Dim fila = TryCast(dgMetodos.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editandoMetodo = True
        lblModoMetodo.Text = "Editando método de pago"
        txtNombreMetodo.Text = fila("nombre").ToString()
        txtNombreMetodo.Tag = CInt(fila("idmetodopago"))

        lblUsoMetodo.Text = $"{fila("pagos")} pago(s) · {Formato.Dinero(fila("recaudado"))} recaudados."
        pnlUsoMetodo.Visibility = Visibility.Visible
    End Sub

    Private Sub btnNuevoMetodo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevoMetodo.Click
        LimpiarMetodo()
    End Sub

    Private Sub btnGuardarMetodo_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarMetodo.Click
        If String.IsNullOrWhiteSpace(txtNombreMetodo.Text) Then
            Advertir("Escribe el nombre del método de pago.")
            Return
        End If

        Try
            Dim id = If(editandoMetodo AndAlso txtNombreMetodo.Tag IsNot Nothing, CInt(txtNombreMetodo.Tag), 0)
            CatalogoService.GuardarMetodoPago(id, txtNombreMetodo.Text, editandoMetodo)
            Exito("El método de pago se guardó.")
            CargarMetodos()
            LimpiarMetodo()
        Catch ex As Exception
            Avisar("Guardar el método de pago", ex)
        End Try
    End Sub

    Private Sub btnEliminarMetodo_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminarMetodo.Click
        If Not editandoMetodo OrElse txtNombreMetodo.Tag Is Nothing Then
            Advertir("Selecciona primero un método de pago de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar el método de pago {txtNombreMetodo.Text}?") Then Return

        Try
            CatalogoService.EliminarMetodoPago(CInt(txtNombreMetodo.Tag))
            Exito("El método de pago se eliminó.")
            CargarMetodos()
            LimpiarMetodo()
        Catch ex As Exception
            Avisar("Eliminar el método de pago", ex)
        End Try
    End Sub

    Private Sub LimpiarMetodo()
        editandoMetodo = False
        lblModoMetodo.Text = "Nuevo método de pago"
        txtNombreMetodo.Clear()
        txtNombreMetodo.Tag = Nothing
        pnlUsoMetodo.Visibility = Visibility.Collapsed
        dgMetodos.SelectedItem = Nothing
    End Sub

    ' ═══════════════════ MENSAJES COMPARTIDOS ═══════════════════

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub

    Private Sub Advertir(mensaje As String)
        DialogoAlas.Show(mensaje, "Revisa los datos", MessageBoxButton.OK, MessageBoxImage.Warning)
    End Sub

    Private Sub Exito(mensaje As String)
        DialogoAlas.Show(mensaje, "Guardado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Private Function Confirmar(pregunta As String) As Boolean
        Return DialogoAlas.Show(pregunta, "Confirmar",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes
    End Function
End Class
