Public Class RecuperarClaveWindow

    Private usuarioValidado As String = Nothing

    Private Sub RecuperarClaveWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        txtUsuario.Focus()
    End Sub

    ' ---------- Paso 1: buscar el usuario y su pregunta ----------
    Private Sub btnBuscar_Click(sender As Object, e As RoutedEventArgs) Handles btnBuscar.Click
        OcultarError()
        panelPregunta.Visibility = Visibility.Collapsed
        panelNueva.Visibility = Visibility.Collapsed
        usuarioValidado = Nothing

        If String.IsNullOrWhiteSpace(txtUsuario.Text) Then
            MostrarError("Escribe tu nombre de usuario.")
            Return
        End If

        Try
            Dim pregunta = AuthService.ObtenerPregunta(txtUsuario.Text)
            If pregunta Is Nothing Then
                MostrarError("No se encontró ese usuario, o su cuenta no tiene pregunta de seguridad configurada.")
                Return
            End If

            lblPregunta.Text = pregunta
            txtRespuesta.Clear()
            panelPregunta.Visibility = Visibility.Visible
            txtRespuesta.Focus()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("No se pudo conectar a la base de datos", ex))
        End Try
    End Sub

    ' ---------- Paso 2: verificar la respuesta ----------
    Private Sub btnVerificar_Click(sender As Object, e As RoutedEventArgs) Handles btnVerificar.Click
        OcultarError()

        Try
            If Not AuthService.VerificarRespuesta(txtUsuario.Text, txtRespuesta.Text) Then
                MostrarError("La respuesta no es correcta. Inténtalo de nuevo.")
                Return
            End If

            usuarioValidado = txtUsuario.Text.Trim()
            txtUsuario.IsEnabled = False
            btnBuscar.IsEnabled = False
            txtRespuesta.IsEnabled = False
            btnVerificar.IsEnabled = False
            panelNueva.Visibility = Visibility.Visible
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("No se pudo verificar", ex))
        End Try
    End Sub

    ' ---------- Paso 3: cambiar la contraseña ----------
    Private Sub btnCambiar_Click(sender As Object, e As RoutedEventArgs) Handles btnCambiar.Click
        OcultarError()

        If usuarioValidado Is Nothing Then Return

        If txtNueva.Password <> txtConfirmarNueva.Password Then
            MostrarError("Las contraseñas no coinciden.")
            Return
        End If

        Try
            Dim mensajeError = AuthService.CambiarContrasena(usuarioValidado, txtNueva.Password)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            DialogoParko.Show("¡Contraseña cambiada con éxito!" & Environment.NewLine &
                              "Ya puedes iniciar sesión con tu nueva contraseña.", "Recuperación exitosa")
            Me.Close()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("No se pudo cambiar la contraseña", ex))
        End Try
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
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
