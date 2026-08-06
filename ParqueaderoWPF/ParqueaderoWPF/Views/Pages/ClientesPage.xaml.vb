Imports System.Data

Public Class ClientesPage

    ' True cuando el formulario muestra un cliente existente (el código no se puede cambiar)
    Private editando As Boolean = False

    Private Sub ClientesPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Not Permisos.PuedeEliminar Then
            btnEliminar.IsEnabled = False
            lblSoloAdmin.Visibility = Visibility.Visible
        End If
        CargarCombos()
        CargarLista()
        LimpiarFormulario()
    End Sub

    Private Sub CargarCombos()
        Try
            cboParqueadero.ItemsSource = ParqueaderoService.ParaCombo().DefaultView
            cboTipo.ItemsSource = MovimientoService.Tarifas().DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar los combos", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub CargarLista(Optional filtro As String = "")
        Try
            dgClientes.ItemsSource = ClienteService.Listar(filtro).DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar clientes", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- Búsqueda en vivo ----------
    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        CargarLista(txtBuscar.Text)
    End Sub

    Private Sub btnLimpiarBusqueda_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiarBusqueda.Click
        txtBuscar.Clear()
    End Sub

    ' ---------- Selección ----------
    Private Sub dgClientes_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgClientes.SelectionChanged

        Dim fila = TryCast(dgClientes.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editando = True
        txtCodigo.Text = fila("codigo_cliente").ToString()
        cboParqueadero.SelectedValue = fila("codigo_parqueadero").ToString()
        txtNombre.Text = fila("nombre").ToString()
        txtCedula.Text = fila("cedula").ToString()
        txtCelular.Text = fila("celular").ToString()
        cboTipo.SelectedValue = fila("tipo_vehiculo").ToString()
    End Sub

    ' ---------- Botones ----------
    Private Sub btnNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevo.Click
        LimpiarFormulario()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        If String.IsNullOrWhiteSpace(txtCodigo.Text) OrElse String.IsNullOrWhiteSpace(txtNombre.Text) OrElse
           cboParqueadero.SelectedValue Is Nothing OrElse cboTipo.SelectedValue Is Nothing Then
            DialogoParko.Show("Completa al menos: código, nombre, parqueadero y tipo de vehículo.",
                            "Campos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            If editando Then
                ClienteService.Actualizar(txtCodigo.Text, cboParqueadero.SelectedValue.ToString(),
                                          txtNombre.Text, txtCelular.Text, txtCedula.Text,
                                          cboTipo.SelectedValue.ToString())
                DialogoParko.Show("Cliente actualizado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information)
            Else
                If ClienteService.Existe(txtCodigo.Text) Then
                    DialogoParko.Show("Ya existe un cliente con ese código.", "Código duplicado",
                                    MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If
                ClienteService.Insertar(txtCodigo.Text, cboParqueadero.SelectedValue.ToString(),
                                        txtNombre.Text, txtCelular.Text, txtCedula.Text,
                                        cboTipo.SelectedValue.ToString())
                DialogoParko.Show("Cliente registrado correctamente.", "Éxito",
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
            DialogoParko.Show("Selecciona primero un cliente de la lista.", "Sin selección",
                            MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim respuesta = DialogoParko.Show($"¿Eliminar al cliente '{txtNombre.Text}'?", "Confirmar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        Try
            ClienteService.Eliminar(txtCodigo.Text)
            DialogoParko.Show("Cliente eliminado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            Registro.Error_("Eliminar cliente", ex)
            DialogoParko.Show("No se pudo eliminar. Es posible que el cliente tenga vehículos registrados.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editando = False
        ' El sistema asigna el código automáticamente
        Try
            txtCodigo.Text = ClienteService.SiguienteCodigo()
        Catch
            txtCodigo.Text = ""
        End Try
        txtNombre.Clear()
        txtCedula.Clear()
        txtCelular.Clear()
        cboParqueadero.SelectedIndex = -1
        cboTipo.SelectedIndex = -1
        dgClientes.SelectedItem = Nothing
        txtNombre.Focus()
    End Sub
End Class
