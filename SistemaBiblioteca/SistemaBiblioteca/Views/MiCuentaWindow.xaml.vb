''' <summary>Autoservicio de la cuenta: configurar la pregunta de seguridad y
''' cambiar la contraseña. Existe para que un Bibliotecario no dependa del
''' Administrador por algo que puede resolver solo.</summary>
Public Class MiCuentaWindow

    Private Sub Ventana_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)

        If Sesion.UsuarioActual Is Nothing Then
            Me.Close()
            Return
        End If

        lblNombre.Text = Sesion.UsuarioActual.NombreCompleto
        lblEmail.Text = Sesion.UsuarioActual.Email
        lblRol.Text = Sesion.UsuarioActual.Rol.ToUpper()
        txtEmailActual.Text = Sesion.UsuarioActual.Email
        cboPregunta.ItemsSource = AuthService.PreguntasSugeridas

        RevisarPregunta()
    End Sub

    ''' <summary>Cambia el correo de la cuenta: el que recibe los códigos de
    ''' recuperación. El servicio exige la contraseña y avisa al correo anterior.</summary>
    Private Async Sub btnCambiarEmail_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnCambiarEmail.Click
        OcultarError()

        If Sesion.UsuarioActual Is Nothing Then Return

        btnCambiarEmail.IsEnabled = False
        btnCambiarEmail.Content = "Cambiando…"

        Try
            Dim usuario = Sesion.UsuarioActual.NombreUsuario
            Dim nuevo = txtEmailNuevo.Text
            Dim clave = txtClaveEmail.Password

            Dim problema = Await Task.Run(Function() AuthService.CambiarEmail(usuario, clave, nuevo))

            If problema IsNot Nothing Then
                MostrarError(problema)
                Return
            End If

            ' La sesión abierta lleva el correo viejo: sin refrescarla, la ventana
            ' seguiría enseñando el anterior hasta volver a entrar
            Sesion.UsuarioActual.Email = nuevo.Trim().ToLower()
            txtEmailActual.Text = Sesion.UsuarioActual.Email
            lblEmail.Text = Sesion.UsuarioActual.Email
            txtEmailNuevo.Clear()
            txtClaveEmail.Clear()

            DialogoBiblioteca.Show(
                "Listo. Desde ahora los códigos de recuperación llegarán a tu correo nuevo." & vbCrLf & vbCrLf &
                "Le avisamos del cambio a tu dirección anterior.",
                "Correo actualizado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Cambiar el correo", ex))
        Finally
            btnCambiarEmail.IsEnabled = True
            btnCambiarEmail.Content = "Cambiar correo"
        End Try
    End Sub

    Private Sub RevisarPregunta()
        Try
            Dim consulta = AuthService.ObtenerPregunta(Sesion.UsuarioActual.NombreUsuario)

            If consulta.Estado = AuthService.EstadoPregunta.Encontrada Then
                cboPregunta.Text = consulta.Pregunta
                lblAvisoPregunta.Text = "Ya tienes una pregunta configurada. Puedes cambiarla cuando quieras; " &
                                        "hay que volver a escribir la respuesta."
                pnlSinPregunta.Visibility = Visibility.Collapsed
            Else
                lblAvisoPregunta.Text = "Todavía no configuras tu pregunta de seguridad. " &
                                        "Sin ella no podrás recuperar tu contraseña si la olvidas."
                pnlSinPregunta.Visibility = Visibility.Visible
            End If

        Catch ex As Exception
            lblAvisoPregunta.Text = "No se pudo leer tu pregunta de seguridad."
            Registro.Advertencia($"No se pudo leer la pregunta de seguridad: {ex.Message}")
        End Try
    End Sub

    ' ---------- Pregunta de seguridad ----------

    Private Async Sub btnGuardarPregunta_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnGuardarPregunta.Click
        OcultarError()

        btnGuardarPregunta.IsEnabled = False
        btnGuardarPregunta.Content = "Guardando…"

        Try
            Dim usuario = Sesion.UsuarioActual.NombreUsuario
            Dim pregunta = cboPregunta.Text
            Dim respuesta = txtRespuesta.Text

            Dim problema = Await Task.Run(Function() AuthService.ConfigurarPregunta(usuario, pregunta, respuesta))

            If problema IsNot Nothing Then
                MostrarError(problema)
                Return
            End If

            txtRespuesta.Clear()
            RevisarPregunta()
            DialogoBiblioteca.Show("Tu pregunta de seguridad quedó guardada.",
                                   "Guardado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Guardar la pregunta de seguridad", ex))
        Finally
            btnGuardarPregunta.IsEnabled = True
            btnGuardarPregunta.Content = "Guardar pregunta"
        End Try
    End Sub

    ' ---------- Cambio de contraseña ----------

    Private Async Sub btnCambiarClave_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnCambiarClave.Click
        OcultarError()

        If String.IsNullOrWhiteSpace(txtActual.Password) Then
            MostrarError("Escribe tu contraseña actual.")
            Return
        End If
        If txtNueva.Password <> txtNueva2.Password Then
            MostrarError("Las dos contraseñas nuevas no coinciden.")
            TransicionVentana.Sacudir(panelFormulario)
            Return
        End If

        btnCambiarClave.IsEnabled = False
        btnCambiarClave.Content = "Cambiando…"

        Try
            Dim usuario = Sesion.UsuarioActual.NombreUsuario
            Dim actual = txtActual.Password
            Dim nueva = txtNueva.Password

            Dim problema = Await Task.Run(Function() AuthService.CambiarContrasenaConActual(usuario, actual, nueva))

            If problema IsNot Nothing Then
                MostrarError(problema)
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            txtActual.Clear()
            txtNueva.Clear()
            txtNueva2.Clear()
            DialogoBiblioteca.Show("Tu contraseña se cambió correctamente.",
                                   "Contraseña actualizada con éxito", MessageBoxButton.OK,
                                   MessageBoxImage.Information)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Cambiar la contraseña", ex))
        Finally
            btnCambiarClave.IsEnabled = True
            btnCambiarClave.Content = "Cambiar contraseña"
        End Try
    End Sub

    Private Sub btnCerrar_Click(sender As Object, e As RoutedEventArgs) Handles btnCerrar.Click
        Me.Close()
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
