Imports System.Data

''' <summary>Gestión de las cuentas del personal. Las reglas que impiden dejar al
''' sistema sin administradores viven en UsuarioService: aquí solo se muestran los
''' mensajes que devuelve.</summary>
Public Class UsuariosPage

    Private idUsuarioSel As Integer = 0
    Private activoSel As Boolean = False

    Private Sub UsuariosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If cboRolNuevo.ItemsSource Is Nothing Then
            ' Dar de alta solo crea personal; el combo de cambiar rol sí lista los
            ' tres, o al seleccionar a un pasajero se quedaría en blanco
            cboRolNuevo.ItemsSource = UsuarioService.RolesDelPersonal
            cboRolNuevo.SelectedIndex = 1
            cboRolSel.ItemsSource = UsuarioService.Roles
        End If
    End Sub

    Public Sub Cargar()
        Try
            Dim datos = UsuarioService.Listar(txtBuscar.Text)
            dgUsuarios.ItemsSource = datos.DefaultView

            lblVacio.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblTotal.Text = $"{datos.Rows.Count} cuenta(s) · " &
                            $"{datos.Select("rol = 'Administrador'").Length} administrador(es)"

            LimpiarSeleccion()

        Catch ex As Exception
            Avisar("Cargar las cuentas", ex)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        Cargar()
    End Sub

    ' ---------- Selección ----------

    Private Sub dgUsuarios_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgUsuarios.SelectionChanged

        Dim fila = TryCast(dgUsuarios.SelectedItem, DataRowView)
        If fila Is Nothing Then
            LimpiarSeleccion()
            Return
        End If

        idUsuarioSel = CInt(fila("usuario_id"))
        activoSel = CBool(fila("esta_activo"))

        lblNombreSel.Text = fila("nombre_completo").ToString()
        lblUsuarioSel.Text = $"@{fila("usuario")} · {fila("rol")} · {fila("estado")}"
        cboRolSel.SelectedItem = fila("rol").ToString()

        ' Sobre la propia cuenta hay acciones que el servicio va a rechazar:
        ' avisarlo antes evita que el administrador se lleve la sorpresa.
        Dim esPropia = Sesion.UsuarioActual IsNot Nothing AndAlso
                       Sesion.UsuarioActual.UsuarioID = idUsuarioSel
        lblEsTuCuenta.Visibility = If(esPropia, Visibility.Visible, Visibility.Collapsed)

        btnActivar.Content = If(activoSel, "Desactivar cuenta", "Activar cuenta")

        lblSinSeleccion.Visibility = Visibility.Collapsed
        pnlAcciones.Visibility = Visibility.Visible
    End Sub

    Private Sub LimpiarSeleccion()
        idUsuarioSel = 0
        pnlAcciones.Visibility = Visibility.Collapsed
        lblSinSeleccion.Visibility = Visibility.Visible
    End Sub

    ' ---------- Alta ----------

    Private Async Sub btnCrear_Click(sender As Object, e As RoutedEventArgs) Handles btnCrear.Click
        If cboRolNuevo.SelectedItem Is Nothing Then
            Advertir("Elige el rol de la cuenta nueva.")
            Return
        End If

        btnCrear.IsEnabled = False
        btnCrear.Content = "Creando…"

        Try
            Dim nombre = txtNombre.Text
            Dim email = txtEmail.Text
            Dim usuario = txtUsuario.Text
            Dim rol = cboRolNuevo.SelectedItem.ToString()
            Dim clave As String = ""

            ' El hash BCrypt tarda a propósito: se calcula fuera del hilo de la interfaz
            Dim resultado = Await Task.Run(
                Function()
                    Dim temporal As String = ""
                    Dim problema = AuthService.CrearPorAdministrador(nombre, email, usuario, rol, temporal)
                    Return Tuple.Create(problema, temporal)
                End Function)

            If resultado.Item1 IsNot Nothing Then
                Advertir(resultado.Item1)
                Return
            End If
            clave = resultado.Item2

            DialogoAlas.MostrarConDato(
                $"La cuenta '{usuario.Trim()}' se creó con rol {rol}." & vbCrLf & vbCrLf &
                "Esta es su contraseña temporal. Entrégasela a su dueño: el sistema le pedirá " &
                "cambiarla en su primer inicio de sesión.",
                "Cuenta creada con éxito", clave)

            txtNombre.Clear()
            txtEmail.Clear()
            txtUsuario.Clear()
            Cargar()

        Catch ex As Exception
            Avisar("Crear la cuenta", ex)
        Finally
            btnCrear.IsEnabled = True
            btnCrear.Content = "➕ Crear cuenta"
        End Try
    End Sub

    ' ---------- Acciones sobre la cuenta ----------

    Private Sub btnCambiarRol_Click(sender As Object, e As RoutedEventArgs) Handles btnCambiarRol.Click
        If idUsuarioSel = 0 OrElse cboRolSel.SelectedItem Is Nothing Then Return

        Dim rol = cboRolSel.SelectedItem.ToString()
        If Not Confirmar($"¿Cambiar el rol de {lblNombreSel.Text} a {rol}?") Then Return

        Try
            Dim problema = UsuarioService.CambiarRol(idUsuarioSel, rol)
            If problema IsNot Nothing Then
                Advertir(problema)
                Return
            End If
            Exito($"La cuenta quedó con el rol {rol}.")
            Cargar()
        Catch ex As Exception
            Avisar("Cambiar el rol", ex)
        End Try
    End Sub

    Private Sub btnActivar_Click(sender As Object, e As RoutedEventArgs) Handles btnActivar.Click
        If idUsuarioSel = 0 Then Return

        Dim accion = If(activoSel, "desactivar", "activar")
        If Not Confirmar($"¿Seguro que quieres {accion} la cuenta de {lblNombreSel.Text}?") Then Return

        Try
            Dim problema = UsuarioService.CambiarEstado(idUsuarioSel, Not activoSel)
            If problema IsNot Nothing Then
                Advertir(problema)
                Return
            End If
            Exito($"La cuenta se {If(activoSel, "desactivó", "activó")}.")
            Cargar()
        Catch ex As Exception
            Avisar("Cambiar el estado de la cuenta", ex)
        End Try
    End Sub

    Private Async Sub btnRestablecer_Click(sender As Object, e As RoutedEventArgs) Handles btnRestablecer.Click
        If idUsuarioSel = 0 Then Return
        If Not Confirmar($"¿Restablecer la contraseña de {lblNombreSel.Text}?" & vbCrLf &
                         "Se generará una contraseña temporal y la actual dejará de servir.") Then Return

        btnRestablecer.IsEnabled = False

        Try
            Dim id = idUsuarioSel
            Dim resultado = Await Task.Run(
                Function()
                    Dim temporal As String = ""
                    Dim problema = UsuarioService.RestablecerContrasena(id, temporal)
                    Return Tuple.Create(problema, temporal)
                End Function)

            If resultado.Item1 IsNot Nothing Then
                Advertir(resultado.Item1)
                Return
            End If

            DialogoAlas.MostrarConDato(
                "Contraseña temporal generada. Entrégasela a su dueño: el sistema le pedirá " &
                "cambiarla en su próximo inicio de sesión.",
                "Contraseña restablecida con éxito", resultado.Item2)
            Cargar()

        Catch ex As Exception
            Avisar("Restablecer la contraseña", ex)
        Finally
            btnRestablecer.IsEnabled = True
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminar.Click
        If idUsuarioSel = 0 Then Return
        If Not Confirmar($"¿Eliminar definitivamente la cuenta de {lblNombreSel.Text}?") Then Return

        Try
            Dim problema = UsuarioService.Eliminar(idUsuarioSel)
            If problema IsNot Nothing Then
                Advertir(problema)
                Return
            End If
            Exito("La cuenta se eliminó.")
            Cargar()
        Catch ex As Exception
            Avisar("Eliminar la cuenta", ex)
        End Try
    End Sub

    ' ---------- Mensajes ----------

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub

    Private Sub Advertir(mensaje As String)
        DialogoAlas.Show(mensaje, "No se pudo completar", MessageBoxButton.OK, MessageBoxImage.Warning)
    End Sub

    Private Sub Exito(mensaje As String)
        DialogoAlas.Show(mensaje, "Hecho con éxito", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Private Function Confirmar(pregunta As String) As Boolean
        Return DialogoAlas.Show(pregunta, "Confirmar",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes
    End Function
End Class
