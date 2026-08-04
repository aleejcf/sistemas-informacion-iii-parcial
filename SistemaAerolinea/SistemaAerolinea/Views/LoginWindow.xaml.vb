Imports System.Windows.Threading

''' <summary>Inicio de sesión. La autenticación corre en segundo plano para que
''' la ventana no se congele.
'''
''' El bloqueo tras varios intentos fallidos lo cuenta y lo decide AuthService
''' sobre la cuenta; aquí solo se muestra la cuenta atrás. Cerrar esta ventana y
''' volver a abrirla no lo reinicia, que era justo el agujero de tenerlo aquí.</summary>
Public Class LoginWindow

    Private segundosBloqueo As Integer = 0
    Private WithEvents temporizador As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1)}

    Private Sub LoginWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        TransicionVentana.EntradaEnCascada(panelFormulario)

        lblSaludo.Text = Formato.Saludo()
        lblFechaMarca.Text = Formato.FechaLarga(DateTime.Now)
        txtUsuario.Focus()

        ' El botón de Google solo tiene sentido si hay credenciales configuradas
        pnlGoogle.Visibility = If(GoogleAuthService.EstaDisponible(),
                                  Visibility.Visible, Visibility.Collapsed)

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
            Dim acceso = Await Task.Run(Function() AuthService.Autenticar(nombreUsuario, clave))

            If Not acceso.Exitoso Then
                txtClave.Clear()
                If acceso.SegundosBloqueo > 0 Then
                    BloquearTemporalmente(acceso.SegundosBloqueo)
                Else
                    MostrarError(acceso.Mensaje)
                    TransicionVentana.Sacudir(panelFormulario)
                End If
                Return
            End If

            Dim usuario = acceso.Cuenta
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

    ' ---------- Acceso con Google ----------

    Private Async Sub btnGoogle_Click(sender As Object, e As RoutedEventArgs) Handles btnGoogle.Click
        OcultarError()

        btnGoogle.IsEnabled = False
        btnIngresar.IsEnabled = False

        Try
            MostrarAviso("Te abrimos el navegador para que entres con Google…")

            Dim resultado = Await GoogleAuthService.IniciarSesionAsync()

            If Not resultado.Exitoso Then
                MostrarError(resultado.Mensaje)
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            Dim acceso = Await Task.Run(Function() AuthService.AutenticarConGoogle(resultado.Identidad))

            If acceso.NecesitaRegistro Then
                OcultarError()
                RegistrarConGoogle(resultado.Identidad)
                Return
            End If

            If acceso.Cuenta Is Nothing Then
                MostrarError(If(acceso.Mensaje, "No se pudo entrar con Google."))
                Return
            End If

            EntrarConLaCuenta(acceso.Cuenta)

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Inicio de sesión con Google", ex))
        Finally
            btnGoogle.IsEnabled = True
            btnIngresar.IsEnabled = segundosBloqueo <= 0
        End Try
    End Sub

    ''' <summary>Google confirma quién es la persona, pero no sabe su número de
    ''' documento ni su fecha de nacimiento, y sin eso no se puede emitir un
    ''' boleto. Por eso la primera vez se le pide completar sus datos de viajero.</summary>
    Private Sub RegistrarConGoogle(identidad As GoogleAuthService.IdentidadGoogle)
        DialogoAlas.Show(
            $"Bienvenido, {identidad.Nombre}." & vbCrLf & vbCrLf &
            "Es la primera vez que entras con esa cuenta de Google. Completa tus datos de " &
            "viajero una sola vez y quedarás registrado.",
            "Casi listo", MessageBoxButton.OK, MessageBoxImage.Information)

        Dim registro As New RegisterWindow(identidad) With {.Owner = Me}
        registro.ShowDialog()

        If Not registro.SeRegistro Then Return

        ' Ya tiene cuenta: se entra sin volver a pasar por Google
        Dim acceso = AuthService.AutenticarConGoogle(identidad)
        If acceso.Cuenta IsNot Nothing Then EntrarConLaCuenta(acceso.Cuenta)
    End Sub

    ''' <summary>Pasos comunes a los dos caminos de acceso.</summary>
    Private Sub EntrarConLaCuenta(cuenta As Usuario)
        Sesion.UsuarioActual = cuenta

        If cuenta.DebeCambiarContrasena Then
            Dim cambio As New CambiarContrasenaObligatoriaWindow With {.Owner = Me}
            cambio.ShowDialog()
            If Sesion.UsuarioActual Is Nothing Then Return
        End If

        Dim principal As New MainWindow()
        principal.Show()
        Me.Close()
    End Sub

    Private Sub MostrarAviso(mensaje As String)
        lblError.Text = mensaje
        lblError.Foreground = TryFindResource("BrushTextoSuave")
        lblError.Visibility = Visibility.Visible
    End Sub

    ' ---------- Bloqueo temporal ----------

    ''' <summary>Refleja el bloqueo que ya aplicó el servicio. Deshabilitar los
    ''' campos es cortesía visual: aunque se saltara, la cuenta sigue bloqueada
    ''' en la base de datos.</summary>
    Private Sub BloquearTemporalmente(segundos As Integer)
        segundosBloqueo = segundos
        btnIngresar.IsEnabled = False
        txtUsuario.IsEnabled = False
        txtClave.IsEnabled = False
        MostrarError($"Demasiados intentos fallidos. Espera {segundosBloqueo} segundos.")
        TransicionVentana.Sacudir(panelFormulario)
        temporizador.Start()
    End Sub

    Private Sub temporizador_Tick(sender As Object, e As EventArgs) Handles temporizador.Tick
        segundosBloqueo -= 1
        If segundosBloqueo <= 0 Then
            temporizador.Stop()
            btnIngresar.IsEnabled = True
            txtUsuario.IsEnabled = True
            txtClave.IsEnabled = True
            OcultarError()
        Else
            MostrarError($"Demasiados intentos fallidos. Espera {segundosBloqueo} segundos.")
        End If
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
        lblError.Foreground = TryFindResource("BrushPeligro")
        lblError.Visibility = Visibility.Visible
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
