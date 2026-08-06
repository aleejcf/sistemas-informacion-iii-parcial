''' <summary>Recuperación de contraseña por dos caminos: la pregunta de seguridad
''' o un código de verificación enviado por correo. El asistente avanza de un paso
''' al siguiente solo cuando el anterior quedó verificado.
'''
''' El código se manda de verdad por SMTP (ver CorreoService). Si el equipo no
''' tiene configurada la cuenta de envío, o el correo falla, el código se muestra
''' en pantalla como respaldo: dejar al usuario sin forma de recuperar su cuenta
''' sería peor que enseñárselo.</summary>
Public Class RecuperarClaveWindow

    Private usuarioVerificado As String = ""
    Private emailCuenta As String = ""
    Private identidadConfirmada As Boolean = False
    ''' <summary>Verdadero cuando la verificación se hizo por código: en ese caso
    ''' la contraseña se cambia por correo y no por nombre de usuario.</summary>
    Private porCodigo As Boolean = False

    Private Sub Ventana_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        txtUsuario.Focus()
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
                    ' Sin pregunta configurada solo queda el código al correo. Si el
                    ' envío tampoco está configurado no hay camino automático, y hay
                    ' que decirlo en vez de dejar al usuario dando vueltas.
                    If Not CorreoService.HayConfiguracion() Then
                        MostrarError("Esta cuenta no tiene pregunta de seguridad y este equipo " &
                                     "no tiene configurado el envío de correo. Pídele a un " &
                                     "administrador que te genere una contraseña temporal.")
                        Return
                    End If

                    usuarioVerificado = txtUsuario.Text.Trim()
                    emailCuenta = consulta.Email
                    lblPaso.Text = "Esta cuenta no tiene pregunta de seguridad configurada. " &
                                   "Se usará un código de verificación."
                    IrAPasoCodigo()

                Case Else
                    usuarioVerificado = txtUsuario.Text.Trim()
                    emailCuenta = consulta.Email
                    lblPregunta.Text = consulta.Pregunta
                    lblPaso.Text = "Responde tu pregunta de seguridad."
                    pasoUsuario.Visibility = Visibility.Collapsed
                    pasoPregunta.Visibility = Visibility.Visible

                    ' El camino alterno solo se ofrece si de verdad se puede tomar
                    lnkUsarCorreo.IsEnabled = CorreoService.HayConfiguracion()
                    pnlUsarCorreo.Visibility = If(lnkUsarCorreo.IsEnabled,
                                                  Visibility.Visible, Visibility.Collapsed)

                    TransicionVentana.DeslizarEntrada(pasoPregunta)
                    txtRespuesta.Focus()
            End Select

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Buscar la pregunta de seguridad", ex))
        End Try
    End Sub

    ' ---------- Paso 2a: pregunta de seguridad ----------

    Private Async Sub btnVerificarRespuesta_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnVerificarRespuesta.Click
        OcultarError()

        If String.IsNullOrWhiteSpace(txtRespuesta.Text) Then
            MostrarError("Escribe la respuesta de tu pregunta de seguridad.")
            Return
        End If

        btnVerificarRespuesta.IsEnabled = False
        btnVerificarRespuesta.Content = "Verificando…"

        Try
            Dim usuario = usuarioVerificado
            Dim respuesta = txtRespuesta.Text
            ' Comparar el hash BCrypt es lento a propósito: fuera del hilo de la interfaz
            Dim correcta = Await Task.Run(Function() AuthService.VerificarRespuesta(usuario, respuesta))

            If Not correcta Then
                MostrarError("La respuesta no coincide con la que registraste.")
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            porCodigo = False
            IrAPasoClave()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Verificar la respuesta de seguridad", ex))
        Finally
            btnVerificarRespuesta.IsEnabled = True
            btnVerificarRespuesta.Content = "Verificar respuesta"
        End Try
    End Sub

    Private Sub lnkUsarCorreo_Click(sender As Object, e As RoutedEventArgs) Handles lnkUsarCorreo.Click
        OcultarError()
        IrAPasoCodigo()
    End Sub

    ' ---------- Paso 2b: código de verificación ----------

    Private Async Sub IrAPasoCodigo()
        pasoUsuario.Visibility = Visibility.Collapsed
        pasoPregunta.Visibility = Visibility.Collapsed
        pasoCodigo.Visibility = Visibility.Visible
        TransicionVentana.DeslizarEntrada(pasoCodigo)
        lblPaso.Text = "Escribe el código de verificación."

        Await GenerarYEnviarCodigo()
    End Sub

    ''' <summary>Genera el código y lo manda por correo.
    '''
    ''' El código NUNCA se muestra en pantalla. Enseñarlo aquí anularía su razón de
    ''' existir: cualquiera sentado frente a esta máquina escribiría un usuario, leería
    ''' el código y le cambiaría la contraseña a otra persona. Si el correo no sale, la
    ''' recuperación se detiene y el usuario tiene que reintentar o pedirle a un
    ''' administrador que le genere una contraseña temporal.</summary>
    Private Async Function GenerarYEnviarCodigo() As Task
        OcultarError()
        btnVerificarCodigo.IsEnabled = False
        lnkReenviar.IsEnabled = False
        lblAvisoCodigo.Text = "Generando el código…"

        Dim codigo As String = ""
        Dim nombre As String = ""

        Try
            Dim problema = AuthService.GenerarCodigoRecuperacion(emailCuenta, codigo, nombre)
            If problema IsNot Nothing Then
                MostrarError(problema)
                lblAvisoCodigo.Text = "No se pudo generar el código."
                Return
            End If

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Generar el código de recuperación", ex))
            lblAvisoCodigo.Text = "No se pudo generar el código."
            Return
        Finally
            lnkReenviar.IsEnabled = True
        End Try

        lblAvisoCodigo.Text = $"Enviando el código a {Enmascarar(emailCuenta)}…"

        ' Mandar un correo tarda segundos y es una operación de red: fuera del
        ' hilo de la interfaz para que la ventana no se congele.
        Dim destino = emailCuenta
        Dim paraQuien = nombre
        Dim clave = codigo
        Dim fallo = Await Task.Run(Function() CorreoService.EnviarCodigo(destino, paraQuien, clave))

        If fallo Is Nothing Then
            lblAvisoCodigo.Text = $"Se envió un código de 6 dígitos a {Enmascarar(emailCuenta)}. " &
                                  "Vence en 30 minutos."
            btnVerificarCodigo.IsEnabled = True
            txtCodigo.Focus()
        Else
            ' Sin correo entregado no hay código que escribir: se deja la casilla
            ' bloqueada para que quede claro que el paso no avanzó.
            lblAvisoCodigo.Text = "No se pudo entregar el código."
            MostrarError(fallo & vbCrLf &
                         "Puedes intentar de nuevo, o pedirle a un administrador que te " &
                         "genere una contraseña temporal desde la pantalla de Usuarios.")
        End If
    End Function

    Private Async Sub lnkReenviar_Click(sender As Object, e As RoutedEventArgs) Handles lnkReenviar.Click
        txtCodigo.Clear()
        Await GenerarYEnviarCodigo()
    End Sub

    Private Sub btnVerificarCodigo_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnVerificarCodigo.Click
        OcultarError()

        If String.IsNullOrWhiteSpace(txtCodigo.Text) Then
            MostrarError("Escribe el código de 6 dígitos.")
            Return
        End If

        Try
            If Not AuthService.VerificarCodigoRecuperacion(emailCuenta, txtCodigo.Text) Then
                MostrarError("El código no es correcto o ya venció.")
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            porCodigo = True
            IrAPasoClave()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Verificar el código de recuperación", ex))
        End Try
    End Sub

    ' ---------- Paso 3: nueva contraseña ----------

    Private Sub IrAPasoClave()
        identidadConfirmada = True
        pasoUsuario.Visibility = Visibility.Collapsed
        pasoPregunta.Visibility = Visibility.Collapsed
        pasoCodigo.Visibility = Visibility.Collapsed
        pasoClave.Visibility = Visibility.Visible
        TransicionVentana.DeslizarEntrada(pasoClave)
        lblPaso.Text = "Último paso: elige tu nueva contraseña."
        txtNueva.Focus()
    End Sub

    Private Async Sub btnGuardarClave_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnGuardarClave.Click
        OcultarError()

        ' El estado se revisa aquí también: la interfaz oculta el paso, pero quien
        ' decide si la identidad está confirmada es esta bandera, no la visibilidad.
        If Not identidadConfirmada Then
            MostrarError("Primero tienes que verificar tu identidad.")
            Return
        End If

        If txtNueva.Password <> txtNueva2.Password Then
            MostrarError("Las dos contraseñas no coinciden.")
            TransicionVentana.Sacudir(panelFormulario)
            Return
        End If

        btnGuardarClave.IsEnabled = False
        btnGuardarClave.Content = "Guardando…"

        Try
            Dim nueva = txtNueva.Password
            Dim usuario = usuarioVerificado
            Dim email = emailCuenta
            Dim usarCodigo = porCodigo

            Dim problema = Await Task.Run(
                Function()
                    Return If(usarCodigo,
                              AuthService.CambiarContrasenaPorEmail(email, nueva),
                              AuthService.CambiarContrasena(usuario, nueva))
                End Function)

            If problema IsNot Nothing Then
                MostrarError(problema)
                Return
            End If

            DialogoBiblioteca.Show("Tu contraseña se actualizó. Ya puedes iniciar sesión con ella.",
                                   "Contraseña recuperada con éxito", MessageBoxButton.OK,
                                   MessageBoxImage.Information)
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

    ' ---------- Auxiliares ----------

    ''' <summary>ju***@gmail.com — confirma al usuario cuál es su correo sin
    ''' revelarlo entero a quien esté mirando la pantalla.</summary>
    Private Shared Function Enmascarar(email As String) As String
        If String.IsNullOrWhiteSpace(email) Then Return "tu correo"

        Dim arroba = email.IndexOf("@"c)
        If arroba <= 2 Then Return email

        Return email.Substring(0, 2) & New String("*"c, Math.Min(4, arroba - 2)) & email.Substring(arroba)
    End Function

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
