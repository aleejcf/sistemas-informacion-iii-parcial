Imports System.Windows.Threading

''' <summary>Inicio de sesión. La autenticación corre en segundo plano para que
''' la ventana no se congele, y tras tres intentos fallidos la cuenta se bloquea
''' 30 segundos: es la defensa mínima contra alguien probando contraseñas.</summary>
Public Class LoginWindow

    Private Const INTENTOS_PERMITIDOS As Integer = 3
    Private Const SEGUNDOS_BLOQUEO As Integer = 30

    Private intentosFallidos As Integer = 0
    Private segundosBloqueo As Integer = 0
    Private WithEvents temporizador As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1)}

    Private Sub LoginWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        TransicionVentana.EntradaEnCascada(panelFormulario)

        lblSaludo.Text = Formato.Saludo()
        lblFecha.Text = Formato.FechaLarga(DateTime.Now)
        txtUsuario.Focus()

        ' Aviso de Bloq Mayús mientras se escribe la contraseña
        AddHandler txtClave.PreviewKeyUp, Sub() RevisarMayusculas()
        AddHandler txtClave.GotKeyboardFocus, Sub() RevisarMayusculas()
        AddHandler txtClave.LostKeyboardFocus, Sub() lblCaps.Visibility = Visibility.Collapsed
        AddHandler txtClave.PreviewKeyDown,
            Sub(s As Object, ev As KeyEventArgs)
                If ev.Key = Key.Enter Then IniciarSesion()
            End Sub
    End Sub

    Private Sub RevisarMayusculas()
        lblCaps.Visibility = If(Keyboard.IsKeyToggled(Key.CapsLock), Visibility.Visible, Visibility.Collapsed)
    End Sub

    ' ---------- Inicio de sesión ----------

    Private Sub btnIngresar_Click(sender As Object, e As RoutedEventArgs) Handles btnIngresar.Click
        IniciarSesion()
    End Sub

    Private Sub txtUsuario_KeyDown(sender As Object, e As KeyEventArgs) Handles txtUsuario.KeyDown
        If e.Key = Key.Enter Then txtClave.Focus()
    End Sub

    Private Async Sub IniciarSesion()
        OcultarError()

        If String.IsNullOrWhiteSpace(txtUsuario.Text) OrElse String.IsNullOrWhiteSpace(txtClave.Password) Then
            MostrarError("Escribe tu usuario y contraseña.")
            TransicionVentana.Sacudir(panelFormulario)
            Return
        End If

        ' El botón muestra que está trabajando mientras se consulta la base de datos
        btnIngresar.IsEnabled = False
        btnIngresar.Content = "Ingresando…"

        Try
            Dim nombreUsuario = txtUsuario.Text
            Dim clave = txtClave.Password
            ' BCrypt es intencionalmente lento: si corriera en el hilo de la interfaz,
            ' la ventana se quedaría congelada en cada intento.
            Dim usuario = Await Task.Run(Function() AuthService.Autenticar(nombreUsuario, clave))

            If usuario Is Nothing Then
                intentosFallidos += 1
                If intentosFallidos >= INTENTOS_PERMITIDOS Then
                    BloquearTemporalmente()
                Else
                    MostrarError($"Usuario o contraseña incorrectos. Intento {intentosFallidos} de {INTENTOS_PERMITIDOS}.")
                    TransicionVentana.Sacudir(panelFormulario)
                End If
                txtClave.Clear()
                Return
            End If

            Sesion.UsuarioActual = usuario

            ' Las cuentas creadas por un Administrador traen clave temporal
            If usuario.DebeCambiarContrasena Then
                Dim cambio As New CambiarContrasenaObligatoriaWindow With {.Owner = Me}
                cambio.ShowDialog()
                If Sesion.UsuarioActual Is Nothing Then
                    ' Cerró la ventana sin cambiar la contraseña: no se le deja entrar
                    txtClave.Clear()
                    Return
                End If
            End If

            Dim principal As New MainWindow()
            principal.Show()
            Me.Close()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Inicio de sesión", ex))
        Finally
            btnIngresar.Content = "Ingresar"
            If segundosBloqueo <= 0 Then btnIngresar.IsEnabled = True
        End Try
    End Sub

    ' ---------- Bloqueo temporal ----------

    Private Sub BloquearTemporalmente()
        segundosBloqueo = SEGUNDOS_BLOQUEO
        btnIngresar.IsEnabled = False
        txtUsuario.IsEnabled = False
        txtClave.IsEnabled = False
        MostrarError($"Demasiados intentos fallidos. Espera {segundosBloqueo} segundos.")
        TransicionVentana.Sacudir(panelFormulario)
        Registro.Advertencia($"Cuenta bloqueada temporalmente tras {INTENTOS_PERMITIDOS} intentos: {txtUsuario.Text}")
        temporizador.Start()
    End Sub

    Private Sub temporizador_Tick(sender As Object, e As EventArgs) Handles temporizador.Tick
        segundosBloqueo -= 1
        If segundosBloqueo <= 0 Then
            temporizador.Stop()
            intentosFallidos = 0
            btnIngresar.IsEnabled = True
            txtUsuario.IsEnabled = True
            txtClave.IsEnabled = True
            OcultarError()
        Else
            MostrarError($"Demasiados intentos fallidos. Espera {segundosBloqueo} segundos.")
        End If
    End Sub

    ' ---------- Ventana sin marco ----------

    ''' <summary>La ventana no tiene barra de título propia, así que se arrastra
    ''' desde cualquier parte del fondo. DragMove solo es válido con el botón
    ''' izquierdo presionado; llamarlo en otro caso lanza excepción.</summary>
    Private Sub Ventana_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        If e.ChangedButton = MouseButton.Left AndAlso e.ButtonState = MouseButtonState.Pressed Then
            Try
                Me.DragMove()
            Catch
                ' Si el botón se soltó entre el evento y la llamada, no pasa nada
            End Try
        End If
    End Sub

    Private Sub btnCerrar_Click(sender As Object, e As RoutedEventArgs) Handles btnCerrar.Click
        Application.Current.Shutdown()
    End Sub

    ' ---------- Enlaces ----------

    Private Sub lnkRegistro_Click(sender As Object, e As RoutedEventArgs) Handles lnkRegistro.Click
        Dim registro As New RegisterWindow With {.Owner = Me}
        registro.ShowDialog()
    End Sub

    Private Sub lnkOlvide_Click(sender As Object, e As RoutedEventArgs) Handles lnkOlvide.Click
        Dim recuperar As New RecuperarClaveWindow With {.Owner = Me}
        recuperar.ShowDialog()
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
