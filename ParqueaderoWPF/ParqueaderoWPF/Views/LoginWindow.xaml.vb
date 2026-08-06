Imports System.IO
Imports System.Windows.Media.Animation
Imports System.Windows.Threading

Public Class LoginWindow

    Private intentosFallidos As Integer = 0
    Private segundosBloqueo As Integer = 0
    Private WithEvents temporizador As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1)}

    Private Sub LoginWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        IniciarVideoDeFondo()
        SaludoSegunLaHora()
        AnimarEntradaDelFormulario()
        txtUsuario.Focus()

        ' Aviso de Bloq Mayús en el campo de contraseña
        AddHandler txtClave.PreviewKeyUp, Sub() RevisarMayusculas()
        AddHandler txtClave.GotKeyboardFocus, Sub() RevisarMayusculas()
        AddHandler txtClave.LostKeyboardFocus, Sub() lblCaps.Visibility = Visibility.Collapsed
    End Sub

    ' ---------- Video de fondo del panel de marca ----------

    ''' <summary>Reproduce Resources\video_fondo.mp4 en loop, silenciado. Si el archivo
    ''' no existe o falla al cargar, no pasa nada: queda el navy sólido (BrushLateral) de fondo.</summary>
    Private Sub IniciarVideoDeFondo()
        Try
            Dim ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "video_fondo.mp4")
            If Not File.Exists(ruta) Then Return

            videoFondo.Source = New Uri(ruta, UriKind.Absolute)
            videoFondo.Play()
        Catch ex As Exception
            Registro.Advertencia($"No se pudo reproducir el video de fondo: {ex.Message}")
        End Try
    End Sub

    ''' <summary>MediaElement no tiene loop nativo: al terminar, se rebobina y se reproduce de nuevo.</summary>
    Private Sub videoFondo_MediaEnded(sender As Object, e As RoutedEventArgs) Handles videoFondo.MediaEnded
        videoFondo.Position = TimeSpan.Zero
        videoFondo.Play()
    End Sub

    ' ---------- Microinteracciones ----------

    ''' <summary>El saludo cambia según la hora del día.</summary>
    Private Sub SaludoSegunLaHora()
        Dim hora = DateTime.Now.Hour
        If hora >= 5 AndAlso hora < 12 Then
            lblSaludo.Text = "¡Buenos días!"
        ElseIf hora >= 12 AndAlso hora < 19 Then
            lblSaludo.Text = "¡Buenas tardes!"
        Else
            lblSaludo.Text = "¡Buenas noches!"
        End If
    End Sub

    ''' <summary>Los elementos del formulario entran en cascada (fundido + deslizamiento).</summary>
    Private Sub AnimarEntradaDelFormulario()
        Dim retraso = 0
        For Each hijo As UIElement In panelFormulario.Children
            AnimarEntrada(hijo, retraso)
            retraso += 70
        Next
    End Sub

    Private Sub AnimarEntrada(elemento As UIElement, retrasoMs As Integer)
        elemento.Opacity = 0
        Dim desplazamiento As New TranslateTransform(0, 18)
        elemento.RenderTransform = desplazamiento

        Dim aparecer As New DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(450)) With {
            .BeginTime = TimeSpan.FromMilliseconds(retrasoMs)
        }
        Dim subir As New DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(450)) With {
            .BeginTime = TimeSpan.FromMilliseconds(retrasoMs),
            .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
        }
        elemento.BeginAnimation(OpacityProperty, aparecer)
        desplazamiento.BeginAnimation(TranslateTransform.YProperty, subir)
    End Sub

    ''' <summary>Sacude el formulario cuando las credenciales fallan.</summary>
    Private Sub Sacudir(elemento As FrameworkElement)
        Dim desplazamiento As New TranslateTransform()
        elemento.RenderTransform = desplazamiento

        Dim sacudida As New DoubleAnimationUsingKeyFrames With {.Duration = TimeSpan.FromMilliseconds(420)}
        Dim valores = {0.0, -13.0, 11.0, -8.0, 6.0, -3.0, 0.0}
        For i = 0 To valores.Length - 1
            sacudida.KeyFrames.Add(New EasingDoubleKeyFrame(valores(i),
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 70))))
        Next
        desplazamiento.BeginAnimation(TranslateTransform.XProperty, sacudida)
    End Sub

    Private Sub RevisarMayusculas()
        lblCaps.Visibility = If(Keyboard.IsKeyToggled(Key.CapsLock),
                                Visibility.Visible, Visibility.Collapsed)
    End Sub

    ' ---------- Inicio de sesión ----------

    Private Sub btnIngresar_Click(sender As Object, e As RoutedEventArgs) Handles btnIngresar.Click
        IniciarSesion()
    End Sub

    Private Sub txtClave_KeyDown(sender As Object, e As KeyEventArgs) Handles txtClave.KeyDown
        If e.Key = Key.Enter Then IniciarSesion()
    End Sub

    Private Async Sub IniciarSesion()
        OcultarError()

        If String.IsNullOrWhiteSpace(txtUsuario.Text) OrElse String.IsNullOrWhiteSpace(txtClave.Password) Then
            MostrarError("Escribe tu usuario y contraseña.")
            Sacudir(panelFormulario)
            Return
        End If

        ' El botón muestra que está trabajando mientras se consulta la base de datos
        btnIngresar.IsEnabled = False
        btnIngresar.Content = "Ingresando…"

        Try
            Dim nombreUsuario = txtUsuario.Text
            Dim clave = txtClave.Password
            Dim usuario = Await Task.Run(Function() AuthService.Autenticar(nombreUsuario, clave))

            If usuario Is Nothing Then
                intentosFallidos += 1
                If intentosFallidos >= 3 Then
                    BloquearTemporalmente()
                Else
                    MostrarError($"Usuario o contraseña incorrectos. Intento {intentosFallidos} de 3.")
                    Sacudir(panelFormulario)
                End If
                txtClave.Clear()
                Return
            End If

            Sesion.UsuarioActual = usuario

            ' Cuentas creadas por un Administrador traen una contraseña temporal que hay que reemplazar
            If usuario.DebeCambiarContrasena Then
                Dim cambio As New CambiarContrasenaObligatoriaWindow With {.Owner = Me}
                cambio.ShowDialog()
                If Sesion.UsuarioActual Is Nothing Then
                    ' Cerró la ventana sin cambiar la contraseña: no se le deja entrar
                    Return
                End If
            End If

            ' Sesión iniciada: abrir la ventana principal
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

    ' ---------- Bloqueo temporal tras 3 intentos fallidos ----------
    Private Sub BloquearTemporalmente()
        segundosBloqueo = 30
        btnIngresar.IsEnabled = False
        txtUsuario.IsEnabled = False
        txtClave.IsEnabled = False
        MostrarError($"Demasiados intentos fallidos. Espera {segundosBloqueo} segundos.")
        Sacudir(panelFormulario)
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
