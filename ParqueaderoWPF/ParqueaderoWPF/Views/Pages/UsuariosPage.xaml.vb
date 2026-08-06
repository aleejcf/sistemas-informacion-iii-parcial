Imports System.Data

''' <summary>Página exclusiva del Administrador: da de alta cuentas con una contraseña
''' temporal (el usuario la cambia en su primer login) y administra rol/estado de las existentes.</summary>
Public Class UsuariosPage

    Private editando As Boolean = False
    Private usuarioIdSeleccionado As Integer
    Private estaActivoSeleccionado As Boolean

    Private Sub UsuariosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        cboRol.ItemsSource = New String() {"Operador", "Administrador"}
        CargarLista()
        LimpiarFormulario()
    End Sub

    Private Sub CargarLista(Optional filtro As String = "")
        Try
            dgUsuarios.ItemsSource = UsuarioService.Listar(filtro).DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar usuarios", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        CargarLista(txtBuscar.Text)
    End Sub

    Private Sub btnLimpiarBusqueda_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiarBusqueda.Click
        txtBuscar.Clear()
    End Sub

    ' ---------- Selección: pasa a modo "gestionar" ----------
    Private Sub dgUsuarios_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgUsuarios.SelectionChanged

        Dim fila = TryCast(dgUsuarios.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        editando = True
        usuarioIdSeleccionado = CInt(fila("usuario_id"))
        estaActivoSeleccionado = CBool(fila("esta_activo"))

        lblTituloForm.Text = "Gestionar usuario"
        txtNombre.Text = fila("nombre_completo").ToString()
        txtEmail.Text = fila("email").ToString()
        txtUsuario.Text = fila("usuario").ToString()
        txtNombre.IsEnabled = False
        txtEmail.IsEnabled = False
        txtUsuario.IsEnabled = False
        cboRol.SelectedItem = fila("rol").ToString()

        lblAyuda.Visibility = Visibility.Collapsed
        btnCrear.Visibility = Visibility.Collapsed
        btnGuardarRol.Visibility = Visibility.Visible
        btnActivarDesactivar.Visibility = Visibility.Visible
        btnActivarDesactivar.Content = If(estaActivoSeleccionado, "Desactivar", "Activar")
        btnNuevo.Visibility = Visibility.Visible
    End Sub

    Private Sub btnNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevo.Click
        LimpiarFormulario()
    End Sub

    ' ---------- Alta de usuario ----------
    Private Sub btnCrear_Click(sender As Object, e As RoutedEventArgs) Handles btnCrear.Click
        If cboRol.SelectedItem Is Nothing Then
            DialogoParko.Show("Selecciona un rol.", "Falta el rol", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            Dim claveTemporal As String = ""
            Dim mensajeError = AuthService.CrearPorAdministrador(txtNombre.Text, txtEmail.Text, txtUsuario.Text,
                                                                  cboRol.SelectedItem.ToString(), claveTemporal)
            If mensajeError IsNot Nothing Then
                DialogoParko.Show(mensajeError, "No se pudo crear el usuario",
                                MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            DialogoParko.Show($"Usuario '{txtUsuario.Text.Trim()}' creado." & Environment.NewLine & Environment.NewLine &
                              $"Contraseña temporal: {claveTemporal}" & Environment.NewLine & Environment.NewLine &
                              "Compártela con el usuario; deberá cambiarla al iniciar sesión por primera vez.",
                              "Usuario creado", MessageBoxButton.OK, MessageBoxImage.Information)
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al crear el usuario", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- Cambiar rol ----------
    Private Sub btnGuardarRol_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarRol.Click
        If cboRol.SelectedItem Is Nothing Then Return
        Dim nuevoRol = cboRol.SelectedItem.ToString()

        If usuarioIdSeleccionado = Sesion.UsuarioActual.UsuarioID AndAlso nuevoRol <> "Administrador" Then
            DialogoParko.Show("No puedes quitarte a ti mismo el rol de Administrador.", "Acción no permitida",
                            MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            UsuarioService.CambiarRol(usuarioIdSeleccionado, nuevoRol)
            DialogoParko.Show("Rol actualizado correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information)
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cambiar el rol", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- Activar / desactivar ----------
    Private Sub btnActivarDesactivar_Click(sender As Object, e As RoutedEventArgs) Handles btnActivarDesactivar.Click
        If usuarioIdSeleccionado = Sesion.UsuarioActual.UsuarioID Then
            DialogoParko.Show("No puedes desactivar tu propia cuenta.", "Acción no permitida",
                            MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim nuevoEstado = Not estaActivoSeleccionado
        Dim accion = If(nuevoEstado, "activar", "desactivar")
        Dim respuesta = DialogoParko.Show($"¿Seguro que deseas {accion} a '{txtNombre.Text}'?", "Confirmar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        Try
            UsuarioService.CambiarActivo(usuarioIdSeleccionado, nuevoEstado)
            DialogoParko.Show($"Usuario {If(nuevoEstado, "activado", "desactivado")} correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information)
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cambiar el estado", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editando = False
        lblTituloForm.Text = "Nuevo usuario"
        txtNombre.Clear()
        txtEmail.Clear()
        txtUsuario.Clear()
        txtNombre.IsEnabled = True
        txtEmail.IsEnabled = True
        txtUsuario.IsEnabled = True
        cboRol.SelectedIndex = -1
        dgUsuarios.SelectedItem = Nothing

        lblAyuda.Visibility = Visibility.Visible
        btnCrear.Visibility = Visibility.Visible
        btnGuardarRol.Visibility = Visibility.Collapsed
        btnActivarDesactivar.Visibility = Visibility.Collapsed
        btnNuevo.Visibility = Visibility.Collapsed
        txtNombre.Focus()
    End Sub
End Class
