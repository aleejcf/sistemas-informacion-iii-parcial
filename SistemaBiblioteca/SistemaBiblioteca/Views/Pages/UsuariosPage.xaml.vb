Imports System.Data

''' <summary>Administración de las cuentas del personal: crear, cambiar el rol,
''' activar o desactivar y restablecer contraseñas. Solo llega aquí un
''' Administrador; el menú lateral ni siquiera le muestra la sección a los demás.</summary>
Public Class UsuariosPage

    Private preparado As Boolean = False
    Private usuarioIdActual As Integer = 0
    Private estaActivo As Boolean = True

    ' ======================= CICLO DE VIDA =======================

    Public Sub Cargar()
        Preparar()
        LimpiarDetalle()
        CargarLista()
    End Sub

    Private Sub UsuariosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True

        cboRol.ItemsSource = AuthService.Roles
        cboNuevoRol.ItemsSource = AuthService.Roles
        cboNuevoRol.SelectedItem = "Bibliotecario"
    End Sub

    ' ======================= LISTA =======================

    Private Sub CargarLista()
        Try
            dgUsuarios.ItemsSource = UsuarioService.Listar(txtBuscar.Text).DefaultView
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar las cuentas", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        CargarLista()
    End Sub

    ' ======================= DETALLE =======================

    Private Sub dgUsuarios_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgUsuarios.SelectionChanged
        Dim vista = TryCast(dgUsuarios.SelectedItem, DataRowView)
        If vista Is Nothing Then
            LimpiarDetalle()
            Return
        End If

        Dim fila = vista.Row
        usuarioIdActual = Db.Numero(fila, "usuario_id")
        estaActivo = Not IsDBNull(fila("esta_activo")) AndAlso CBool(fila("esta_activo"))

        pnlSinSeleccion.Visibility = Visibility.Collapsed
        pnlCuenta.Visibility = Visibility.Visible

        lblNombre.Text = Db.Texto(fila, "nombre_completo")
        lblEmail.Text = Db.Texto(fila, "email")
        lblUsuario.Text = Db.Texto(fila, "usuario")
        cboRol.SelectedItem = Db.Texto(fila, "rol")

        Dim tienePregunta = Db.Numero(fila, "tiene_pregunta") = 1
        pnlAvisoPregunta.Visibility = If(tienePregunta, Visibility.Collapsed, Visibility.Visible)

        btnActivar.Content = If(estaActivo, "Desactivar cuenta", "Activar cuenta")
        MostrarExplicacionRol()
    End Sub

    Private Sub cboRol_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboRol.SelectionChanged
        MostrarExplicacionRol()
    End Sub

    ''' <summary>Dice qué puede hacer cada rol, en vez de dejar que el nombre lo
    ''' sugiera. Quien reparte permisos debe saber exactamente qué está dando.</summary>
    Private Sub MostrarExplicacionRol()
        Select Case TryCast(cboRol.SelectedItem, String)
            Case "Administrador"
                lblExplicacionRol.Text = "Control total: elimina registros, edita los catálogos y la política " &
                                         "de préstamo, condona multas, autoriza préstamos bloqueados y ve la bitácora."
            Case "Bibliotecario"
                lblExplicacionRol.Text = "Atiende el mostrador: presta, devuelve, cobra multas y registra socios " &
                                         "y libros. No elimina registros ni cambia la política de préstamo."
            Case Else
                lblExplicacionRol.Text = ""
        End Select
    End Sub

    Private Sub LimpiarDetalle()
        usuarioIdActual = 0
        pnlCuenta.Visibility = Visibility.Collapsed
        pnlSinSeleccion.Visibility = Visibility.Visible
    End Sub

    ' ======================= ACCIONES SOBRE LA CUENTA =======================

    Private Sub btnGuardarRol_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarRol.Click
        If usuarioIdActual = 0 Then Return

        Dim rol = TryCast(cboRol.SelectedItem, String)
        If rol Is Nothing Then
            DialogoBiblioteca.Show("Elige el rol de la cuenta.", "Faltan datos",
                                   MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            Dim problema = UsuarioService.CambiarRol(usuarioIdActual, rol)
            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo cambiar el rol",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            DialogoBiblioteca.Show($"La cuenta {lblUsuario.Text} ahora es {rol}.",
                                   "Rol actualizado con éxito", MessageBoxButton.OK,
                                   MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cambiar el rol", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnActivar_Click(sender As Object, e As RoutedEventArgs) Handles btnActivar.Click
        If usuarioIdActual = 0 Then Return

        Dim accion = If(estaActivo, "desactivar", "activar")
        If DialogoBiblioteca.Show($"¿{accion.Substring(0, 1).ToUpper()}{accion.Substring(1)} " &
                                  $"la cuenta {lblUsuario.Text}?",
                                  "Confirmar", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = UsuarioService.CambiarEstado(usuarioIdActual, Not estaActivo)
            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, $"No se pudo {accion} la cuenta",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            LimpiarDetalle()

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cambiar el estado de la cuenta", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnRestablecer_Click(sender As Object, e As RoutedEventArgs) Handles btnRestablecer.Click
        If usuarioIdActual = 0 Then Return

        If DialogoBiblioteca.Show($"Se le generará una contraseña temporal a {lblUsuario.Text}. " &
                                  "La actual dejará de servir y tendrá que cambiarla al entrar. ¿Continuar?",
                                  "Restablecer contraseña", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim clave As String = ""
            Dim problema = UsuarioService.RestablecerContrasena(usuarioIdActual, clave)

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo restablecer",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            DialogoBiblioteca.MostrarConDato(
                $"Entrégale esta contraseña temporal a {lblNombre.Text}. " &
                "El sistema le pedirá cambiarla la primera vez que inicie sesión.",
                "Contraseña restablecida con éxito", clave)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Restablecer la contraseña", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminar.Click
        If usuarioIdActual = 0 Then Return

        If DialogoBiblioteca.Show($"¿Eliminar la cuenta {lblUsuario.Text}? Esta acción no se puede deshacer.",
                                  "Confirmar", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = UsuarioService.Eliminar(usuarioIdActual)
            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo eliminar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            LimpiarDetalle()
            DialogoBiblioteca.Show("La cuenta se eliminó.", "Eliminada con éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Eliminar la cuenta", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ======================= ALTA =======================

    Private Async Sub btnCrear_Click(sender As Object, e As RoutedEventArgs) Handles btnCrear.Click
        lblErrorAlta.Visibility = Visibility.Collapsed

        Dim rol = TryCast(cboNuevoRol.SelectedItem, String)
        If rol Is Nothing Then
            MostrarErrorAlta("Elige el rol de la nueva cuenta.")
            Return
        End If

        btnCrear.IsEnabled = False
        btnCrear.Content = "Creando…"

        Try
            Dim nombre = txtNuevoNombre.Text
            Dim email = txtNuevoEmail.Text
            Dim usuario = txtNuevoUsuario.Text
            Dim clave As String = ""

            ' El hash BCrypt es lento a propósito: fuera del hilo de la interfaz
            Dim resultado = Await Task.Run(
                Function()
                    Dim temporal As String = ""
                    Dim problema = AuthService.CrearPorAdministrador(nombre, email, usuario, rol, temporal)
                    Return Tuple.Create(problema, temporal)
                End Function)

            If resultado.Item1 IsNot Nothing Then
                MostrarErrorAlta(resultado.Item1)
                Return
            End If

            clave = resultado.Item2
            txtNuevoNombre.Clear()
            txtNuevoEmail.Clear()
            txtNuevoUsuario.Clear()
            cboNuevoRol.SelectedItem = "Bibliotecario"
            CargarLista()

            DialogoBiblioteca.MostrarConDato(
                $"La cuenta '{usuario.Trim()}' quedó creada como {rol}. " &
                "Entrégale esta contraseña temporal; el sistema le pedirá cambiarla al entrar.",
                "Cuenta creada con éxito", clave)

        Catch ex As Exception
            MostrarErrorAlta(MensajeError.Traducir("Crear la cuenta", ex))
        Finally
            btnCrear.IsEnabled = True
            btnCrear.Content = "Crear cuenta"
        End Try
    End Sub

    Private Sub MostrarErrorAlta(mensaje As String)
        lblErrorAlta.Text = mensaje
        lblErrorAlta.Visibility = Visibility.Visible
        TransicionVentana.Sacudir(panelAlta)
    End Sub
End Class
