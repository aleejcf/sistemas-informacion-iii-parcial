Public Class RecuperarClaveWindow

    Private Enum Paso
        Usuario
        Pregunta
        SinPregunta
        CodigoCorreo
        Nueva
    End Enum

    Private pasoActual As Paso = Paso.Usuario
    Private usuarioValidado As String = Nothing
    Private emailDeLaCuenta As String = Nothing
    Private preguntaGuardada As String = Nothing
    Private tienePreguntaConfigurada As Boolean = False

    Private Sub RecuperarClaveWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        txtUsuario.Focus()
    End Sub

    ' ---------- Enter avanza el paso activo (no hay un solo botón "default": cada paso tiene el suyo) ----------
    Private Sub txtUsuario_KeyDown(sender As Object, e As KeyEventArgs) Handles txtUsuario.KeyDown
        If e.Key = Key.Enter Then btnBuscar_Click(sender, e)
    End Sub

    Private Sub txtRespuesta_KeyDown(sender As Object, e As KeyEventArgs) Handles txtRespuesta.KeyDown
        If e.Key = Key.Enter Then btnVerificar_Click(sender, e)
    End Sub

    Private Sub txtCodigoCorreo_KeyDown(sender As Object, e As KeyEventArgs) Handles txtCodigoCorreo.KeyDown
        If e.Key = Key.Enter Then btnVerificarCodigoCorreo_Click(sender, e)
    End Sub

    Private Sub txtConfirmarNueva_KeyDown(sender As Object, e As KeyEventArgs) Handles txtConfirmarNueva.KeyDown
        If e.Key = Key.Enter Then btnCambiar_Click(sender, e)
    End Sub

    ''' <summary>Muestra solo el panel del paso indicado y ajusta el botón inferior:
    ''' "Cancelar" cierra la ventana (paso 1 o ya terminado), "Volver" regresa un paso.</summary>
    Private Sub MostrarPaso(paso As Paso)
        panelPregunta.Visibility = If(paso = Paso.Pregunta, Visibility.Visible, Visibility.Collapsed)
        panelSinPregunta.Visibility = If(paso = Paso.SinPregunta, Visibility.Visible, Visibility.Collapsed)
        panelCodigoCorreo.Visibility = If(paso = Paso.CodigoCorreo, Visibility.Visible, Visibility.Collapsed)
        panelNueva.Visibility = If(paso = Paso.Nueva, Visibility.Visible, Visibility.Collapsed)

        pasoActual = paso
        btnCancelar.Content = If(paso = Paso.Usuario OrElse paso = Paso.Nueva, "Cancelar", "Volver")
    End Sub

    Private Sub VolverAlPaso1()
        txtUsuario.IsEnabled = True
        btnBuscar.IsEnabled = True
        usuarioValidado = Nothing
        MostrarPaso(Paso.Usuario)
        txtUsuario.Focus()
    End Sub

    ' ---------- Paso 1: buscar el usuario y decidir el camino de recuperación ----------
    Private Sub btnBuscar_Click(sender As Object, e As RoutedEventArgs) Handles btnBuscar.Click
        OcultarError()
        usuarioValidado = Nothing
        emailDeLaCuenta = Nothing
        preguntaGuardada = Nothing

        If String.IsNullOrWhiteSpace(txtUsuario.Text) Then
            MostrarError("Escribe tu nombre de usuario.")
            Return
        End If

        Try
            Dim consulta = AuthService.ObtenerPregunta(txtUsuario.Text)

            Select Case consulta.Estado
                Case AuthService.EstadoPregunta.UsuarioNoExiste
                    MostrarError("No existe un usuario con ese nombre. Revisa que esté bien escrito.")
                    Return

                Case AuthService.EstadoPregunta.SinConfigurar
                    emailDeLaCuenta = consulta.Email
                    tienePreguntaConfigurada = False
                    lblSinPregunta.Text = $"Esta cuenta no tiene pregunta de seguridad configurada. " &
                                          $"Podemos enviarte un código a {OcultarEmail(consulta.Email)} para verificar que eres tú."
                    MostrarPaso(Paso.SinPregunta)
                    Return
            End Select

            emailDeLaCuenta = consulta.Email
            preguntaGuardada = consulta.Pregunta
            tienePreguntaConfigurada = True
            lblPregunta.Text = consulta.Pregunta
            txtRespuesta.Clear()
            MostrarPaso(Paso.Pregunta)
            txtRespuesta.Focus()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("No se pudo conectar a la base de datos", ex))
        End Try
    End Sub

    ' ---------- Paso 2a: verificar la respuesta de seguridad ----------
    Private Sub btnVerificar_Click(sender As Object, e As RoutedEventArgs) Handles btnVerificar.Click
        OcultarError()

        Try
            If Not AuthService.VerificarRespuesta(txtUsuario.Text, txtRespuesta.Text) Then
                MostrarError("La respuesta no es correcta. Inténtalo de nuevo.")
                Return
            End If

            IrAPasoFinal()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("No se pudo verificar", ex))
        End Try
    End Sub

    ''' <summary>Aunque la cuenta sí tenga pregunta de seguridad, se le puede ofrecer
    ''' igual el código por correo (por si tampoco recuerda la respuesta).</summary>
    Private Sub lnkPreferirCorreo_Click(sender As Object, e As RoutedEventArgs) Handles lnkPreferirCorreo.Click
        OcultarError()
        lblSinPregunta.Text = $"Podemos enviarte un código a {OcultarEmail(emailDeLaCuenta)} para verificar que eres tú."
        MostrarPaso(Paso.SinPregunta)
    End Sub

    ' ---------- Paso 2b: enviar el código por correo ----------
    Private Async Sub btnEnviarCodigoCorreo_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnEnviarCodigoCorreo.Click
        Await EnviarCodigoPorCorreo()
    End Sub

    Private Async Sub lnkReenviarCodigo_Click(sender As Object, e As RoutedEventArgs) Handles lnkReenviarCodigo.Click
        Await EnviarCodigoPorCorreo()
    End Sub

    Private Async Function EnviarCodigoPorCorreo() As Task
        OcultarError()
        btnEnviarCodigoCorreo.IsEnabled = False
        lnkReenviarCodigo.IsEnabled = False
        Try
            Dim email = emailDeLaCuenta
            Dim codigo As String = ""
            Dim nombre As String = ""
            Dim mensajeError = Await Task.Run(Function() AuthService.GenerarCodigoRecuperacionPorEmail(
                                              email, codigo, nombre))
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            Await EmailService.EnviarCodigoRecuperacion(email, nombre, codigo)

            lblCodigoEnviadoA.Text = $"Te enviamos un código a {OcultarEmail(email)}. Vence en 30 minutos."
            MostrarPaso(Paso.CodigoCorreo)
            txtCodigoCorreo.Clear()
            txtCodigoCorreo.Focus()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("No se pudo enviar el código", ex))
        Finally
            btnEnviarCodigoCorreo.IsEnabled = True
            lnkReenviarCodigo.IsEnabled = True
        End Try
    End Function

    ' ---------- Paso 2c: verificar el código de correo ----------
    Private Sub btnVerificarCodigoCorreo_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnVerificarCodigoCorreo.Click
        OcultarError()

        If String.IsNullOrWhiteSpace(txtCodigoCorreo.Text) Then
            MostrarError("Escribe el código que recibiste.")
            Return
        End If

        Try
            If Not AuthService.VerificarCodigoRecuperacion(emailDeLaCuenta, txtCodigoCorreo.Text) Then
                MostrarError("El código no es válido o ya venció. Solicita uno nuevo.")
                Return
            End If

            IrAPasoFinal()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("No se pudo verificar el código", ex))
        End Try
    End Sub

    ''' <summary>Los dos caminos (pregunta de seguridad o código de correo) llegan aquí igual.</summary>
    Private Sub IrAPasoFinal()
        usuarioValidado = txtUsuario.Text.Trim()
        txtUsuario.IsEnabled = False
        btnBuscar.IsEnabled = False
        MostrarPaso(Paso.Nueva)
        txtNueva.Focus()
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
            ' Se cambia por correo (limpia también el código de recuperación) porque es el dato
            ' que siempre tenemos, sin importar cuál de los dos caminos se usó para verificar la identidad.
            Dim mensajeError = AuthService.CambiarContrasenaPorEmail(emailDeLaCuenta, txtNueva.Password)
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

    ''' <summary>En el paso 1 (o ya con la contraseña cambiada) cierra la ventana;
    ''' en los pasos intermedios retrocede uno, sin perder lo ya escrito.</summary>
    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        Select Case pasoActual
            Case Paso.Usuario, Paso.Nueva
                Me.Close()

            Case Paso.Pregunta
                VolverAlPaso1()

            Case Paso.SinPregunta
                OcultarError()
                If tienePreguntaConfigurada Then
                    lblPregunta.Text = preguntaGuardada
                    txtRespuesta.Clear()
                    MostrarPaso(Paso.Pregunta)
                Else
                    VolverAlPaso1()
                End If

            Case Paso.CodigoCorreo
                OcultarError()
                MostrarPaso(Paso.SinPregunta)
        End Select
    End Sub

    ''' <summary>Oculta la mayor parte del correo para mostrarlo en pantalla sin exponerlo completo.</summary>
    Private Shared Function OcultarEmail(email As String) As String
        Dim arroba = email.IndexOf("@"c)
        If arroba <= 1 Then Return email
        Return email.Substring(0, 2) & "***" & email.Substring(arroba)
    End Function

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
