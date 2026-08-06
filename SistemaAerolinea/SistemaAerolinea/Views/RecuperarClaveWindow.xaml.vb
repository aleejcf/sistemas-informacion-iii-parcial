''' <summary>Recuperación de contraseña por tres caminos, y los tres verifican de
''' verdad quién está pidiendo el cambio:
'''
'''   · La PREGUNTA DE SEGURIDAD, comparada contra su hash BCrypt.
'''   · Un CÓDIGO DE RESPALDO de un solo uso, de los que se entregan al registrarse.
'''   · Un CÓDIGO POR CORREO, que sale hacia el buzón registrado en la cuenta.
'''
''' Lo que había antes generaba el código y LO MOSTRABA en pantalla cuando no
''' había servidor de correo. Eso no era recuperar una cuenta: bastaba con saber
''' el correo de alguien para pedir su código, leerlo aquí mismo y cambiarle la
''' contraseña. Ese camino ya no existe. Si no hay correo configurado, la vía del
''' correo no se ofrece —y quedan las otras dos, que no dependen de nada externo.</summary>
Public Class RecuperarClaveWindow

    ''' <summary>Cuenta ya verificada. Mientras esté vacío no se puede cambiar nada:
    ''' es la garantía de que el paso 3 solo se alcanza tras superar el paso 2.</summary>
    Private usuarioVerificado As String = ""

    ''' <summary>Datos de la cuenta que se está recuperando, hallados en el paso 1.
    ''' El correo sale de la base y nunca de lo que teclee quien está delante.</summary>
    Private usuarioBuscado As String = ""
    Private correoDeLaCuenta As String = ""

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
        pnlMetodos.Visibility = Visibility.Collapsed
        pnlPregunta.Visibility = Visibility.Collapsed
        pnlRespaldo.Visibility = Visibility.Collapsed
        pnlCorreo.Visibility = Visibility.Collapsed
        pnlNuevaClave.Visibility = Visibility.Collapsed
        panel.Visibility = Visibility.Visible
        TransicionVentana.FundirEntrada(panel)
    End Sub

    ' ---------- Paso 1: identificar la cuenta y ver qué vías tiene ----------

    Private Async Sub btnBuscar_Click(sender As Object, e As RoutedEventArgs) Handles btnBuscar.Click
        OcultarError()

        If String.IsNullOrWhiteSpace(txtUsuario.Text) Then
            MostrarError("Escribe tu nombre de usuario.")
            Return
        End If

        btnBuscar.IsEnabled = False
        btnBuscar.Content = "Buscando…"

        Try
            Dim usuario = txtUsuario.Text.Trim()

            Dim consulta = Await Task.Run(Function() AuthService.ObtenerPregunta(usuario))

            If consulta.Estado = AuthService.EstadoPregunta.UsuarioNoExiste Then
                MostrarError("No existe una cuenta activa con ese usuario.")
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            usuarioBuscado = usuario
            correoDeLaCuenta = If(consulta.Email, "")

            Dim tienePregunta = consulta.Estado = AuthService.EstadoPregunta.Encontrada
            Dim codigos = Await Task.Run(Function() RecuperacionService.DisponiblesPara(usuario))
            Dim hayCorreo = CorreoService.EstaDisponible() AndAlso correoDeLaCuenta.Length > 0

            PrepararMetodos(tienePregunta, consulta.Pregunta, codigos, hayCorreo)

            MostrarPanel(pnlMetodos)
            MarcarPaso(2)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Buscar la cuenta", ex))
        Finally
            btnBuscar.IsEnabled = True
            btnBuscar.Content = "Continuar"
        End Try
    End Sub

    ''' <summary>Deja cada vía habilitada o no según lo que la cuenta tenga de
    ''' verdad, y lo explica: una opción apagada sin motivo solo desconcierta.</summary>
    Private Sub PrepararMetodos(tienePregunta As Boolean, pregunta As String,
                                codigosDisponibles As Integer, hayCorreo As Boolean)

        btnViaPregunta.IsEnabled = tienePregunta
        lblEstadoPregunta.Text = If(tienePregunta,
                                    pregunta,
                                    "Esta cuenta no tiene pregunta configurada.")

        btnViaRespaldo.IsEnabled = codigosDisponibles > 0
        lblEstadoRespaldo.Text = If(codigosDisponibles > 0,
                                    $"Te quedan {codigosDisponibles} código(s) sin usar.",
                                    "Esta cuenta no tiene códigos de respaldo sin usar.")

        btnViaCorreo.IsEnabled = hayCorreo
        If hayCorreo Then
            lblEstadoCorreo.Text = $"Se enviará a {Formato.CorreoOculto(correoDeLaCuenta)}"
        ElseIf correoDeLaCuenta.Length = 0 Then
            lblEstadoCorreo.Text = "Esta cuenta no tiene correo registrado."
        Else
            ' Se dice claro en vez de dejar el botón apagado sin explicación
            lblEstadoCorreo.Text = "Este equipo no tiene configurado el envío de correo."
        End If

        pnlSinVias.Visibility = If(tienePregunta OrElse codigosDisponibles > 0 OrElse hayCorreo,
                                   Visibility.Collapsed, Visibility.Visible)
    End Sub

    Private Sub btnViaPregunta_Click(sender As Object, e As RoutedEventArgs) Handles btnViaPregunta.Click
        OcultarError()
        lblPregunta.Text = lblEstadoPregunta.Text
        MostrarPanel(pnlPregunta)
        txtRespuesta.Focus()
    End Sub

    Private Sub btnViaRespaldo_Click(sender As Object, e As RoutedEventArgs) Handles btnViaRespaldo.Click
        OcultarError()
        MostrarPanel(pnlRespaldo)
        txtRespaldo.Focus()
    End Sub

    Private Sub btnViaCorreo_Click(sender As Object, e As RoutedEventArgs) Handles btnViaCorreo.Click
        OcultarError()
        lblCorreoTapado.Text = Formato.CorreoOculto(correoDeLaCuenta)
        MostrarPanel(pnlCorreo)
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
            Dim usuario = usuarioBuscado
            Dim respuesta = txtRespuesta.Text
            Dim correcta = Await Task.Run(Function() AuthService.VerificarRespuesta(usuario, respuesta))

            If Not correcta Then
                MostrarError("La respuesta no coincide. Inténtalo de nuevo.")
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            DarPorVerificada("Respondiste bien tu pregunta de seguridad.")

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Verificar la respuesta de seguridad", ex))
        Finally
            btnVerificarRespuesta.IsEnabled = True
            btnVerificarRespuesta.Content = "Verificar respuesta"
        End Try
    End Sub

    ' ---------- Paso 2C: código de respaldo ----------

    Private Async Sub btnVerificarRespaldo_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnVerificarRespaldo.Click
        OcultarError()

        btnVerificarRespaldo.IsEnabled = False
        btnVerificarRespaldo.Content = "Verificando…"

        Try
            Dim usuario = usuarioBuscado
            Dim codigo = txtRespaldo.Text
            Dim resultado = Await Task.Run(Function() RecuperacionService.Canjear(usuario, codigo))

            If Not resultado.Valido Then
                MostrarError(resultado.Mensaje)
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            Dim aviso = If(resultado.Restantes = 0,
                           "Era tu último código de respaldo. Genera unos nuevos desde Mi cuenta.",
                           $"Código aceptado. Te quedan {resultado.Restantes} sin usar.")
            DarPorVerificada(aviso)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Verificar el código de respaldo", ex))
        Finally
            btnVerificarRespaldo.IsEnabled = True
            btnVerificarRespaldo.Content = "Verificar código"
        End Try
    End Sub

    ' ---------- Paso 2B: código por correo ----------

    Private Async Sub btnEnviarCodigo_Click(sender As Object, e As RoutedEventArgs) Handles btnEnviarCodigo.Click
        OcultarError()

        btnEnviarCodigo.IsEnabled = False
        btnEnviarCodigo.Content = "Enviando…"

        Try
            Dim correo = correoDeLaCuenta

            ' El código se genera y se manda dentro del mismo Task: en ningún punto
            ' vuelve a la interfaz, que es justo lo que se quería evitar
            Dim resultado = Await Task.Run(
                Function()
                    Dim codigo As String = ""
                    Dim nombre As String = ""
                    Dim problema = AuthService.GenerarCodigoRecuperacion(correo, codigo, nombre)
                    If problema IsNot Nothing Then Return problema

                    Return CorreoService.EnviarCodigo(correo, nombre, codigo)
                End Function)

            If resultado IsNot Nothing Then
                MostrarError(resultado)
                Return
            End If

            txtCodigo.IsEnabled = True
            btnVerificarCodigo.IsEnabled = True
            txtCodigo.Focus()

            DialogoAlas.Show(
                $"Te enviamos un código a {Formato.CorreoOculto(correo)}." & vbCrLf & vbCrLf &
                "Vence en 30 minutos y solo se puede usar una vez. Si no lo ves, " &
                "revisa la carpeta de correo no deseado.",
                "Código enviado", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Enviar el código de recuperación", ex))
        Finally
            btnEnviarCodigo.IsEnabled = True
            btnEnviarCodigo.Content = "Enviarme el código"
        End Try
    End Sub

    Private Sub btnVerificarCodigo_Click(sender As Object, e As RoutedEventArgs) Handles btnVerificarCodigo.Click
        OcultarError()

        Try
            If Not AuthService.VerificarCodigoRecuperacion(correoDeLaCuenta, txtCodigo.Text) Then
                MostrarError("El código no es correcto o ya venció. Pide uno nuevo.")
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            DarPorVerificada("Código verificado.")

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Verificar el código de recuperación", ex))
        End Try
    End Sub

    ''' <summary>Punto único por el que se llega al paso 3. Que las tres vías pasen
    ''' por aquí es lo que hace fácil comprobar que ninguna se lo salta.</summary>
    Private Sub DarPorVerificada(aviso As String)
        usuarioVerificado = usuarioBuscado
        lblCuentaVerificada.Text = $"{aviso} Escribe tu nueva contraseña."
        MostrarPanel(pnlNuevaClave)
        MarcarPaso(3)
        txtNueva.Focus()
    End Sub

    ' ---------- Paso 3: nueva contraseña ----------

    Private Async Sub btnGuardarClave_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardarClave.Click
        OcultarError()

        ' Sin verificación previa no se llega hasta aquí, pero se comprueba igual:
        ' la validación no puede depender solo de que un panel esté visible.
        If String.IsNullOrWhiteSpace(usuarioVerificado) Then
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

            Dim problema = Await Task.Run(Function() AuthService.CambiarContrasena(usuario, nueva))

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
