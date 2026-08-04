''' <summary>Autoservicio de la cuenta: configurar la pregunta de seguridad y
''' cambiar la contraseña sin depender de un administrador.</summary>
Public Class MiCuentaWindow

    Private Sub MiCuentaWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)

        If Sesion.UsuarioActual Is Nothing Then
            Me.Close()
            Return
        End If

        Dim u = Sesion.UsuarioActual
        lblNombre.Text = u.NombreCompleto
        lblEmail.Text = u.Email
        lblRol.Text = u.Rol.ToUpper()
        lblUsuario.Text = "@" & u.NombreUsuario
        lblIniciales.Text = Iniciales(u.NombreCompleto)

        cboPregunta.ItemsSource = AuthService.PreguntasSugeridas
        RevisarPregunta()
    End Sub

    Private Shared Function Iniciales(nombreCompleto As String) As String
        Dim partes = If(nombreCompleto, "").Split(" "c).Where(Function(p) p.Length > 0).ToArray()
        If partes.Length = 0 Then Return "?"
        If partes.Length = 1 Then Return partes(0).Substring(0, 1).ToUpper()
        Return (partes(0).Substring(0, 1) & partes(1).Substring(0, 1)).ToUpper()
    End Function

    Private Sub RevisarPregunta()
        Try
            Dim consulta = AuthService.ObtenerPregunta(Sesion.UsuarioActual.NombreUsuario)

            If consulta.Estado = AuthService.EstadoPregunta.Encontrada Then
                cboPregunta.Text = consulta.Pregunta
                pnlAviso.Visibility = Visibility.Collapsed
            Else
                lblAviso.Text = "Todavía no tienes pregunta de seguridad. Sin ella no podrás " &
                                "recuperar tu contraseña si la olvidas."
                pnlAviso.Visibility = Visibility.Visible
            End If
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Consultar la pregunta de seguridad", ex))
        End Try
    End Sub

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
            DialogoAlas.Show("Tu pregunta de seguridad quedó guardada.", "Guardado con éxito",
                             MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Guardar la pregunta de seguridad", ex))
        Finally
            btnGuardarPregunta.IsEnabled = True
            btnGuardarPregunta.Content = "Guardar pregunta"
        End Try
    End Sub

    Private Async Sub btnCambiarClave_Click(sender As Object, e As RoutedEventArgs) Handles btnCambiarClave.Click
        OcultarError()

        If txtNueva.Password <> txtNueva2.Password Then
            MostrarError("Las dos contraseñas nuevas no coinciden.")
            TransicionVentana.Sacudir(panelClave)
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
                TransicionVentana.Sacudir(panelClave)
                Return
            End If

            txtActual.Clear()
            txtNueva.Clear()
            txtNueva2.Clear()
            DialogoAlas.Show("Tu contraseña se cambió correctamente.", "Contraseña actualizada con éxito",
                             MessageBoxButton.OK, MessageBoxImage.Information)

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
