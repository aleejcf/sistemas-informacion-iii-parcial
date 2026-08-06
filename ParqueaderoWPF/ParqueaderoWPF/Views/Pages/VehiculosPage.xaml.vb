Imports System.Data

Public Class VehiculosPage

    Private editando As Boolean = False

    Private Sub VehiculosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Not Permisos.PuedeEliminar Then
            btnEliminar.IsEnabled = False
            lblSoloAdmin.Visibility = Visibility.Visible
        End If
        cboEstado.ItemsSource = New String() {"excelente", "bueno", "rayado", "golpeado"}
        CargarCombos()
        CargarLista()
    End Sub

    Private Sub CargarCombos()
        Try
            cboParqueadero.ItemsSource = ParqueaderoService.ParaCombo().DefaultView
            cboCliente.ItemsSource = ClienteService.ParaCombo().DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar los combos", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub CargarLista(Optional filtro As String = "")
        Try
            dgVehiculos.ItemsSource = VehiculoService.Listar(filtro).DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar vehículos", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        CargarLista(txtBuscar.Text)
    End Sub

    Private Sub btnLimpiarBusqueda_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiarBusqueda.Click
        txtBuscar.Clear()
    End Sub

    Private Sub dgVehiculos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgVehiculos.SelectionChanged

        Dim fila = TryCast(dgVehiculos.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editando = True
        txtCodigo.Text = fila("codigo_vehiculo").ToString()
        txtCodigo.IsEnabled = False
        txtPlaca.Text = fila("placa").ToString()
        cboParqueadero.SelectedValue = fila("codigo_parqueadero").ToString()
        cboCliente.SelectedValue = fila("codigo_cliente").ToString()
        If IsDBNull(fila("fecha_ingreso")) Then
            dpFecha.SelectedDate = Nothing
        Else
            dpFecha.SelectedDate = CDate(fila("fecha_ingreso"))
        End If
        txtMarca.Text = fila("marca").ToString()
        txtModelo.Text = fila("modelo").ToString()
        cboEstado.Text = fila("estado").ToString()
    End Sub

    Private Sub btnNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevo.Click
        LimpiarFormulario()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        If String.IsNullOrWhiteSpace(txtCodigo.Text) OrElse String.IsNullOrWhiteSpace(txtPlaca.Text) OrElse
           String.IsNullOrWhiteSpace(txtModelo.Text) OrElse String.IsNullOrWhiteSpace(cboEstado.Text) OrElse
           cboParqueadero.SelectedValue Is Nothing OrElse cboCliente.SelectedValue Is Nothing Then
            DialogoParko.Show("Completa: código, placa, modelo, estado, parqueadero y cliente.",
                            "Campos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            If editando Then
                VehiculoService.Actualizar(txtCodigo.Text, cboParqueadero.SelectedValue.ToString(),
                                           cboCliente.SelectedValue.ToString(), dpFecha.SelectedDate,
                                           txtMarca.Text, txtModelo.Text, cboEstado.Text, txtPlaca.Text)
                DialogoParko.Show("Vehículo actualizado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information)
            Else
                If VehiculoService.Existe(txtCodigo.Text) Then
                    DialogoParko.Show("Ya existe un vehículo con ese código.", "Código duplicado",
                                    MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If
                VehiculoService.Insertar(txtCodigo.Text, cboParqueadero.SelectedValue.ToString(),
                                         cboCliente.SelectedValue.ToString(), dpFecha.SelectedDate,
                                         txtMarca.Text, txtModelo.Text, cboEstado.Text, txtPlaca.Text)
                DialogoParko.Show("Vehículo registrado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information)
            End If
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al guardar", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminar.Click
        If Not editando Then
            DialogoParko.Show("Selecciona primero un vehículo de la lista.", "Sin selección",
                            MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim respuesta = DialogoParko.Show($"¿Eliminar el vehículo con placa '{txtPlaca.Text}'?", "Confirmar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        Try
            VehiculoService.Eliminar(txtCodigo.Text)
            DialogoParko.Show("Vehículo eliminado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al eliminar", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editando = False
        txtCodigo.Clear()
        txtCodigo.IsEnabled = True
        txtPlaca.Clear()
        cboParqueadero.SelectedIndex = -1
        cboCliente.SelectedIndex = -1
        dpFecha.SelectedDate = Nothing
        txtMarca.Clear()
        txtModelo.Clear()
        cboEstado.SelectedIndex = -1
        cboEstado.Text = ""
        dgVehiculos.SelectedItem = Nothing
        txtCodigo.Focus()
    End Sub
End Class
