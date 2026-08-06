''' <summary>Alta de cuentas del sistema. El primer usuario que se registra queda
''' como Administrador (alguien tiene que poder crear a los demás); a partir de
''' ahí todos entran como Bibliotecarios.</summary>
Public Class RegisterWindow

    Private Sub RegisterWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        cboPregunta.ItemsSource = AuthService.PreguntasSugeridas

        Try
            lblAvisoRol.Text = If(AuthService.HayUsuarios(),
                "Rol:  Bibliotecario",
                "Rol:  Administrador")
        Catch
            lblAvisoRol.Text = ""
        End Try

        AddHandler txtClave.ClaveCambiada, AddressOf MedirFuerza
        txtNombre.Focus()
    End Sub

    ''' <summary>Medidor de fuerza de la contraseña. No bloquea nada: solo le dice
    ''' al usuario, mientras escribe, qué tan buena es la que está eligiendo.</summary>
    Private Sub MedirFuerza(sender As Object, e As EventArgs)
        Dim clave = txtClave.Password
        Dim puntos = 0

        If clave.Length >= 6 Then puntos += 1
        If clave.Length >= 10 Then puntos += 1
        If clave.Any(AddressOf Char.IsDigit) AndAlso clave.Any(AddressOf Char.IsLetter) Then puntos += 1
        If clave.Any(Function(c) Not Char.IsLetterOrDigit(c)) Then puntos += 1

        barraFuerza.Value = puntos * 25

        If clave.Length = 0 Then
            lblFuerza.Text = "Mínimo 6 caracteres, combinando letras y números."
            lblFuerza.Foreground = TryFindResource("BrushTextoSuave")
            barraFuerza.Foreground = TryFindResource("BrushPrimario")
            Return
        End If

        Select Case puntos
            Case 0, 1
                lblFuerza.Text = "Contraseña débil"
                lblFuerza.Foreground = TryFindResource("BrushPeligro")
                barraFuerza.Foreground = TryFindResource("BrushPeligro")
            Case 2
                lblFuerza.Text = "Contraseña aceptable"
                lblFuerza.Foreground = TryFindResource("BrushAdvertencia")
                barraFuerza.Foreground = TryFindResource("BrushAdvertencia")
            Case Else
                lblFuerza.Text = "Contraseña fuerte"
                lblFuerza.Foreground = TryFindResource("BrushExito")
                barraFuerza.Foreground = TryFindResource("BrushExito")
        End Select
    End Sub

    Private Async Sub btnCrear_Click(sender As Object, e As RoutedEventArgs) Handles btnCrear.Click
        OcultarError()

        If txtClave.Password <> txtClave2.Password Then
            MostrarError("Las dos contraseñas no coinciden.")
            Return
        End If

        btnCrear.IsEnabled = False
        btnCrear.Content = "Creando…"

        Try
            Dim nombre = txtNombre.Text
            Dim email = txtEmail.Text
            Dim usuario = txtUsuario.Text
            Dim clave = txtClave.Password
            Dim pregunta = cboPregunta.Text
            Dim respuesta = txtRespuesta.Text

            ' El hash BCrypt es lento a propósito: se calcula fuera del hilo de la interfaz
            Dim problema = Await Task.Run(Function() AuthService.Registrar(
                nombre, email, usuario, clave, pregunta, respuesta))

            If problema IsNot Nothing Then
                MostrarError(problema)
                Return
            End If

            DialogoBiblioteca.Show($"La cuenta '{usuario.Trim()}' se creó correctamente. Ya puedes iniciar sesión.",
                                   "Cuenta creada con éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            Me.Close()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Registro de cuenta", ex))
        Finally
            btnCrear.IsEnabled = True
            btnCrear.Content = "Crear cuenta"
        End Try
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        Me.Close()
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
        TransicionVentana.Sacudir(panelFormulario)
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
