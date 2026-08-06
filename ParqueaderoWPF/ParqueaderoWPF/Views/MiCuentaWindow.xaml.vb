''' <summary>Permite al usuario que ya inició sesión configurar su pregunta de seguridad
''' (necesaria para recuperar la contraseña) y cambiar su contraseña.</summary>
Public Class MiCuentaWindow

    Private Sub MiCuentaWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        Dim usuario = Sesion.UsuarioActual
        If usuario Is Nothing Then
            Me.Close()
            Return
        End If

        lblNombre.Text = usuario.NombreCompleto
        lblRol.Text = usuario.Rol.ToUpper()
        lblUsuario.Text = "@" & usuario.NombreUsuario

        cboPregunta.ItemsSource = New String() {
            "¿Cuál es el nombre de tu primera mascota?",
            "¿En qué ciudad naciste?",
            "¿Cuál es tu comida favorita?",
            "¿Cómo se llama tu mejor amigo de la infancia?",
            "¿Cuál es tu equipo favorito?"
        }

        MostrarEstadoDeLaPregunta()
    End Sub

    Private Sub MostrarEstadoDeLaPregunta()
        Try
            Dim consulta = AuthService.ObtenerPregunta(Sesion.UsuarioActual.NombreUsuario)
            If consulta.Estado = AuthService.EstadoPregunta.Encontrada Then
                lblEstadoPregunta.Text = "✔ Ya tienes una pregunta configurada. Puedes cambiarla cuando quieras."
                lblEstadoPregunta.Foreground = CType(FindResource("BrushExito"), Media.Brush)
                cboPregunta.Text = consulta.Pregunta
            Else
                lblEstadoPregunta.Text = "⚠ Aún no tienes pregunta de seguridad. Configúrala para poder " &
                                         "recuperar tu contraseña si algún día la olvidas."
                lblEstadoPregunta.Foreground = CType(FindResource("BrushPeligro"), Media.Brush)
            End If
        Catch ex As Exception
            lblEstadoPregunta.Text = MensajeError.Traducir("Consultar pregunta de seguridad", ex)
        End Try
    End Sub

    ' ---------- Pregunta de seguridad ----------
    Private Sub btnGuardarPregunta_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarPregunta.Click
        OcultarError()

        Try
            Dim mensajeError = AuthService.ConfigurarPregunta(Sesion.UsuarioActual.NombreUsuario,
                                                              cboPregunta.Text, txtRespuesta.Text)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            DialogoParko.Show("Pregunta de seguridad guardada." & Environment.NewLine &
                              "Ya puedes recuperar tu contraseña con ella si la olvidas.", "Éxito")
            txtRespuesta.Clear()
            MostrarEstadoDeLaPregunta()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Guardar pregunta de seguridad", ex))
        End Try
    End Sub

    ' ---------- Cambio de contraseña ----------
    Private Sub btnCambiarClave_Click(sender As Object, e As RoutedEventArgs) Handles btnCambiarClave.Click
        OcultarError()

        If txtNueva.Password <> txtConfirmar.Password Then
            MostrarError("Las contraseñas nuevas no coinciden.")
            Return
        End If

        Try
            Dim mensajeError = AuthService.CambiarContrasenaConActual(Sesion.UsuarioActual.NombreUsuario,
                                                                      txtActual.Password, txtNueva.Password)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            DialogoParko.Show("Tu contraseña se cambió correctamente.", "Éxito")
            txtActual.Clear()
            txtNueva.Clear()
            txtConfirmar.Clear()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Cambiar contraseña", ex))
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
