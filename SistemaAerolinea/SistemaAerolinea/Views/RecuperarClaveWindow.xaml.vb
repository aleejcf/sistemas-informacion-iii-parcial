''' <summary>Recuperación de contraseña por dos caminos: la pregunta de seguridad
''' o un código de un solo uso asociado al correo de la cuenta. El segundo existe
''' porque una cuenta sin pregunta configurada quedaría sin forma de recuperarse.</summary>
Public Class RecuperarClaveWindow

    ''' <summary>Usuario ya verificado. Mientras esté vacío no se puede cambiar nada:
    ''' es la garantía de que el paso 3 solo se alcanza tras superar el paso 2.</summary>
    Private usuarioVerificado As String = ""
    Private emailVerificado As String = ""

    Private Sub RecuperarClaveWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        MarcarPaso(1)
        txtUsuario.Focus()
    End Sub

    ' ---------- Indicador de pasos ----------

    Private Sub MarcarPaso(numero As Integer)
        Dim activo = TryFindResource("BrushPrimario")
        Dim inactivo = TryFindResource("BrushTextoSuave")

        paso1.Background = If(numero >= 1, TryFindResource("BrushPrimarioSuave"), TryFindResource("BrushBorde"))
        paso2.Background = If(numero >= 2, TryFindResource("BrushPrimarioSuave"), TryFindResource("BrushBorde"))
        paso3.Background = If(numero >= 3, TryFindResource("BrushPrimarioSuave"), TryFindResource("BrushBorde"))

        txtPaso1.Foreground = If(numero >= 1, activo, inactivo)
        txtPaso2.Foreground = If(numero >= 2, activo, inactivo)
        txtPaso3.Foreground = If(numero >= 3, activo, inactivo)
    End Sub

    Private Sub MostrarPanel(panel As StackPanel)
        pnlPaso1.Visibility = Visibility.Collapsed
        pnlPregunta.Visibility = Visibility.Collapsed
        pnlCorreo.Visibility = Visibility.Collapsed
        pnlNuevaClave.Visibility = Visibility.Collapsed
        panel.Visibility = Visibility.Visible
        TransicionVentana.FundirEntrada(panel)
    End Sub

    ' ---------- Paso 1: identificar la cuenta ----------

    Private Sub btnBuscar_Click(sender As Object, e As RoutedEventArgs) Handles btnBuscar.Click
        OcultarError()

        If String.IsNullOrWhiteSpace(txtUsuario.Text) Then
            MostrarError("Escribe tu nombre de usuario.")
            Return
        End If

        Try
            Dim consulta = AuthService.ObtenerPregunta(txtUsuario.Text)

            Select Case consulta.Estado
                Case AuthService.EstadoPregunta.UsuarioNoExiste
                    MostrarError("No existe una cuenta activa con ese usuario.")

                Case AuthService.EstadoPregunta.SinConfigurar
                    MostrarError("Esa cuenta no tiene pregunta de seguridad configurada. " &
                                 "Recupérala con el código que se envía a tu correo.")
                    txtEmail.Text = consulta.Email
                    MostrarPanel(pnlCorreo)
                    MarcarPaso(2)

                Case AuthService.EstadoPregunta.Encontrada
                    usuarioVerificado = ""
                    emailVerificado = consulta.Email
                    lblPregunta.Text = consulta.Pregunta
                    MostrarPanel(pnlPregunta)
                    MarcarPaso(2)
                    txtRespuesta.Focus()
            End Select

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Buscar la pregunta de seguridad", ex))
        End Try
    End Sub

    Private Sub lnkPorCorreo_Click(sender As Object, e As RoutedEventArgs) Handles lnkPorCorreo.Click
        OcultarError()
        MostrarPanel(pnlCorreo)
        MarcarPaso(2)
        txtEmail.Focus()
    End Sub

    ' ---------- Paso 2A: pregunta de seguridad ----------

    Private Async Sub btnVerificarRespuesta_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnVerificarRespuesta.Click
        OcultarError()

        If String.IsNullOrWhiteSpace(txtRespuesta.Text) Then
            MostrarError("Escribe tu respuesta.")
            Return
        End If

        btnVerificarRespuesta.IsEnabled = False
        btnVerificarRespuesta.Content = "Verificando…"

        Try
            Dim usuario = txtUsuario.Text
            Dim respuesta = txtRespuesta.Text
            Dim correcta = Await Task.Run(Function() AuthService.VerificarRespuesta(usuario, respuesta))

            If Not correcta Then
                MostrarError("La respuesta no coincide. Inténtalo de nuevo.")
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            usuarioVerificado = usuario.Trim()
            emailVerificado = ""
            lblCuentaVerificada.Text = $"Cuenta verificada: {usuarioVerificado}. Escribe tu nueva contraseña."
            MostrarPanel(pnlNuevaClave)
            MarcarPaso(3)
            txtNueva.Focus()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Verificar la respuesta de seguridad", ex))
        Finally
            btnVerificarRespuesta.IsEnabled = True
            btnVerificarRespuesta.Content = "Verificar respuesta"
        End Try
    End Sub

    ' ---------- Paso 2B: código al correo ----------

    Private Sub btnEnviarCodigo_Click(sender As Object, e As RoutedEventArgs) Handles btnEnviarCodigo.Click
        OcultarError()

        If Not Validador.EsEmailValido(txtEmail.Text) Then
            MostrarError("Escribe un correo electrónico válido.")
            Return
        End If

        Try
            Dim codigo As String = ""
            Dim nombre As String = ""
            Dim problema = AuthService.GenerarCodigoRecuperacion(txtEmail.Text, codigo, nombre)

            If problema IsNot Nothing Then
                MostrarError(problema)
                Return
            End If

            txtCodigo.IsEnabled = True
            btnVerificarCodigo.IsEnabled = True
            txtCodigo.Focus()

            ' El sistema no tiene servidor de correo configurado, así que el código
            ' se muestra en pantalla. En producción, aquí iría el envío por SMTP.
            DialogoAlas.MostrarConDato(
                $"Código de recuperación para {nombre}." & vbCrLf & vbCrLf &
                "Vence en 30 minutos y solo se puede usar una vez. " &
                "(En un sistema en producción este código llegaría por correo electrónico.)",
                "Código generado", codigo)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Generar el código de recuperación", ex))
        End Try
    End Sub

    Private Sub btnVerificarCodigo_Click(sender As Object, e As RoutedEventArgs) Handles btnVerificarCodigo.Click
        OcultarError()

        Try
            If Not AuthService.VerificarCodigoRecuperacion(txtEmail.Text, txtCodigo.Text) Then
                MostrarError("El código no es correcto o ya venció. Genera uno nuevo.")
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            usuarioVerificado = ""
            emailVerificado = txtEmail.Text.Trim().ToLower()
            lblCuentaVerificada.Text = $"Código verificado para {emailVerificado}. Escribe tu nueva contraseña."
            MostrarPanel(pnlNuevaClave)
            MarcarPaso(3)
            txtNueva.Focus()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Verificar el código de recuperación", ex))
        End Try
    End Sub

    ' ---------- Paso 3: nueva contraseña ----------

    Private Async Sub btnGuardarClave_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarClave.Click
        OcultarError()

        ' Sin verificación previa no se llega hasta aquí, pero se comprueba igual:
        ' la validación no puede depender solo de que un panel esté visible.
        If String.IsNullOrWhiteSpace(usuarioVerificado) AndAlso String.IsNullOrWhiteSpace(emailVerificado) Then
            MostrarError("Primero tienes que verificar tu identidad.")
            Return
        End If

        If txtNueva.Password <> txtNueva2.Password Then
            MostrarError("Las dos contraseñas no coinciden.")
            Return
        End If

        btnGuardarClave.IsEnabled = False
        btnGuardarClave.Content = "Guardando…"

        Try
            Dim nueva = txtNueva.Password
            Dim usuario = usuarioVerificado
            Dim email = emailVerificado

            Dim problema = Await Task.Run(
                Function()
                    If Not String.IsNullOrWhiteSpace(usuario) Then
                        Return AuthService.CambiarContrasena(usuario, nueva)
                    End If
                    Return AuthService.CambiarContrasenaPorEmail(email, nueva)
                End Function)

            If problema IsNot Nothing Then
                MostrarError(problema)
                Return
            End If

            DialogoAlas.Show("Tu contraseña se cambió correctamente. Ya puedes iniciar sesión.",
                             "Contraseña actualizada con éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            Me.Close()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Guardar la nueva contraseña", ex))
        Finally
            btnGuardarClave.IsEnabled = True
            btnGuardarClave.Content = "Guardar contraseña"
        End Try
    End Sub

    Private Sub btnCerrar_Click(sender As Object, e As RoutedEventArgs) Handles btnCerrar.Click
        Me.Close()
    End Sub

    ' ---------- Mensajes ----------

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
