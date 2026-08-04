Imports System.Data

''' <summary>Registro de las personas que viajan. La lista de abajo muestra el
''' historial de vuelos del pasajero seleccionado, que es lo que primero pregunta
''' quien atiende en el mostrador.</summary>
Public Class PasajerosPage

    Private editando As Boolean = False
    Private historialAbierto As Boolean = True

    Private Sub PasajerosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If cboTipoDocumento.ItemsSource Is Nothing Then
            cboTipoDocumento.ItemsSource = PasajeroService.TiposDocumento
        End If

        If Not Permisos.PuedeEliminar Then
            btnEliminar.IsEnabled = False
            lblSinPermiso.Visibility = Visibility.Visible
        End If

        LimpiarFormulario()
    End Sub

    Public Sub Cargar()
        Try
            cboPais.DisplayMemberPath = "etiqueta"
            cboPais.SelectedValuePath = "idpais"
            If cboPais.ItemsSource Is Nothing Then
                cboPais.ItemsSource = CatalogoService.PaisesParaCombo().DefaultView
            End If

            CargarLista()
        Catch ex As Exception
            Avisar("Cargar los pasajeros", ex)
        End Try
    End Sub

    Private Sub CargarLista()
        Try
            Dim datos = PasajeroService.Listar(txtBuscar.Text)
            dgPasajeros.ItemsSource = datos.DefaultView

            pnlVacio.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblVacio.Text = If(String.IsNullOrWhiteSpace(txtBuscar.Text),
                               "Todavía no hay pasajeros registrados.",
                               "Ningún pasajero coincide con la búsqueda.")

            lblTotal.Text = datos.Rows.Count.ToString()
            lblConVuelos.Text = datos.Select("vuelos > 0").Length.ToString()
            lblPaises.Text = datos.DefaultView.ToTable(True, "idpais").Rows.Count.ToString()

        Catch ex As Exception
            Avisar("Cargar los pasajeros", ex)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        CargarLista()
    End Sub

    ' ---------- Selección e historial ----------

    Private Sub dgPasajeros_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgPasajeros.SelectionChanged

        Dim fila = TryCast(dgPasajeros.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editando = True
        lblModo.Text = "Editando pasajero"
        lblAlta.Text = "Modifica los datos y pulsa Guardar."

        txtCodigo.Text = fila("idpasajero").ToString()
        txtCodigo.IsEnabled = False
        txtNombre.Text = fila("nombre_p").ToString()
        txtApaterno.Text = fila("apaterno").ToString()
        txtAmaterno.Text = If(IsDBNull(fila("amaterno")), "", fila("amaterno").ToString())
        cboTipoDocumento.SelectedItem = fila("tipo_documento").ToString()
        txtDocumento.Text = fila("num_documento").ToString()
        dtpNacimiento.SelectedDate = If(IsDBNull(fila("fecha_nacimiento")), Nothing,
                                        CType(CDate(fila("fecha_nacimiento")), Date?))
        cboPais.SelectedValue = fila("idpais").ToString()
        txtTelefono.Text = If(IsDBNull(fila("telefono")), "", fila("telefono").ToString())
        txtCorreo.Text = fila("email").ToString()

        CargarHistorial(fila("idpasajero").ToString(), fila("nombre_completo").ToString())
    End Sub

    Private Sub CargarHistorial(idPasajero As String, nombre As String)
        Try
            lblPasajeroHistorial.Text = $"— {nombre}"
            Dim datos = PasajeroService.Historial(idPasajero)
            dgHistorial.ItemsSource = datos.DefaultView

            pnlHistorialVacio.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblHistorialVacio.Text = "Este pasajero todavía no ha volado con nosotros."

        Catch ex As Exception
            Avisar("Cargar el historial del pasajero", ex)
        End Try
    End Sub

    ''' <summary>El historial se puede plegar para dejarle toda la altura a la lista.</summary>
    Private Sub btnHistorial_Click(sender As Object, e As RoutedEventArgs) Handles btnHistorial.Click
        historialAbierto = Not historialAbierto

        If historialAbierto Then
            filaHistorial.Height = New GridLength(220)
            pnlHistorial.Visibility = Visibility.Visible
            lblFlecha.Text = "▾"
        Else
            filaHistorial.Height = GridLength.Auto
            pnlHistorial.Visibility = Visibility.Collapsed
            lblFlecha.Text = "▸"
        End If
    End Sub

    ' ---------- Alta, edición y baja ----------

    Private Sub btnSugerir_Click(sender As Object, e As RoutedEventArgs) Handles btnSugerir.Click
        Try
            txtCodigo.Text = PasajeroService.SiguienteCodigo()
        Catch ex As Exception
            Avisar("Sugerir el código del pasajero", ex)
        End Try
    End Sub

    Private Sub btnNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevo.Click
        LimpiarFormulario()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        If cboPais.SelectedValue Is Nothing Then
            DialogoAlas.Show("Selecciona el país del pasajero.", "Campos incompletos",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            Dim problema = PasajeroService.Guardar(
                txtCodigo.Text, txtNombre.Text, txtApaterno.Text, txtAmaterno.Text,
                If(cboTipoDocumento.SelectedItem Is Nothing, "", cboTipoDocumento.SelectedItem.ToString()),
                txtDocumento.Text, dtpNacimiento.SelectedDate, cboPais.SelectedValue.ToString(),
                txtTelefono.Text, txtCorreo.Text, editando)

            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "Revisa los datos", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            DialogoAlas.Show(If(editando, "Los datos del pasajero se actualizaron.",
                                          "El pasajero quedó registrado."),
                             "Guardado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            CargarLista()
            LimpiarFormulario()

        Catch ex As Exception
            Avisar("Guardar el pasajero", ex)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminar.Click
        If Not editando Then
            DialogoAlas.Show("Selecciona primero un pasajero de la lista.", "Sin selección",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim respuesta = DialogoAlas.Show(
            $"¿Eliminar a {txtNombre.Text} {txtApaterno.Text} ({txtCodigo.Text})?",
            "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = PasajeroService.Eliminar(txtCodigo.Text)
            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "No se pudo eliminar", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            DialogoAlas.Show("El pasajero se eliminó.", "Eliminado con éxito",
                             MessageBoxButton.OK, MessageBoxImage.Information)
            CargarLista()
            LimpiarFormulario()

        Catch ex As Exception
            Avisar("Eliminar el pasajero", ex)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editando = False
        lblModo.Text = "Nuevo pasajero"
        lblAlta.Text = "Completa los datos y pulsa Guardar."

        txtCodigo.Clear()
        txtCodigo.IsEnabled = True
        txtNombre.Clear()
        txtApaterno.Clear()
        txtAmaterno.Clear()
        cboTipoDocumento.SelectedIndex = 0
        txtDocumento.Clear()
        dtpNacimiento.SelectedDate = Nothing
        cboPais.SelectedIndex = -1
        txtTelefono.Clear()
        txtCorreo.Clear()

        dgPasajeros.SelectedItem = Nothing
        dgHistorial.ItemsSource = Nothing
        lblPasajeroHistorial.Text = ""
        pnlHistorialVacio.Visibility = Visibility.Visible
        lblHistorialVacio.Text = "Selecciona un pasajero para ver sus vuelos."

        txtCodigo.Focus()
    End Sub

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub
End Class
