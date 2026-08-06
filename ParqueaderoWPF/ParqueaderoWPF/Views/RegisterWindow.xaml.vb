Public Class RegisterWindow

    Private Sub RegisterWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        cboPregunta.ItemsSource = New String() {
            "¿Cuál es el nombre de tu primera mascota?",
            "¿En qué ciudad naciste?",
            "¿Cuál es tu comida favorita?",
            "¿Cómo se llama tu mejor amigo de la infancia?",
            "¿Cuál es tu equipo favorito?"
        }
        txtNombre.Focus()
    End Sub

    Private Sub btnRegistrar_Click(sender As Object, e As RoutedEventArgs) Handles btnRegistrar.Click
        lblError.Visibility = Visibility.Collapsed

        If txtClave.Password <> txtConfirmar.Password Then
            MostrarError("Las contraseñas no coinciden.")
            Return
        End If

        Try
            ' Se consulta ANTES de registrar para saber qué rol le tocará
            Dim esPrimero As Boolean = Not AuthService.HayUsuarios()

            Dim mensajeError = AuthService.Registrar(txtNombre.Text, txtEmail.Text,
                                                     txtUsuario.Text, txtClave.Password,
                                                     cboPregunta.Text, txtRespuesta.Text)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            Dim rolAsignado = If(esPrimero, "Administrador", "Operador")
            DialogoParko.Show($"¡Cuenta creada con éxito, {txtNombre.Text.Trim()}!" & Environment.NewLine &
                            $"Tu rol asignado es: {rolAsignado}." & Environment.NewLine &
                            "Ya puedes iniciar sesión.", "Registro exitoso",
                            MessageBoxButton.OK, MessageBoxImage.Information)
            Me.Close()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Error al registrar", ex))
        End Try
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        Me.Close()
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
    End Sub
End Class
