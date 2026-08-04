Imports System.Windows.Media

''' <summary>Registro público. La primera cuenta del sistema queda como
''' Administrador porque alguien tiene que poder crear a los demás; a partir de
''' ahí, quien se registra aquí es un PASAJERO. Las cuentas del personal las crea
''' un Administrador desde la pantalla de Usuarios: en una aerolínea nadie se da
''' de alta como agente por su cuenta.</summary>
Public Class RegisterWindow

    Private esPrimeraCuenta As Boolean = False
    Private ReadOnly identidadGoogle As GoogleAuthService.IdentidadGoogle

    ''' <summary>Queda en True si la cuenta se llegó a crear, para que quien abrió
    ''' esta ventana sepa si puede continuar.</summary>
    Public Property SeRegistro As Boolean = False

    Public Sub New()
        InitializeComponent()
    End Sub

    ''' <summary>Registro guiado tras entrar con Google: el nombre y el correo ya
    ''' vienen confirmados por Google y no hace falta inventar una contraseña.</summary>
    Public Sub New(identidad As GoogleAuthService.IdentidadGoogle)
        InitializeComponent()
        identidadGoogle = identidad
    End Sub

    Private ReadOnly Property ConGoogle As Boolean
        Get
            Return identidadGoogle IsNot Nothing
        End Get
    End Property

    Private Sub RegisterWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        cboPregunta.ItemsSource = AuthService.PreguntasSugeridas

        Try
            esPrimeraCuenta = AuthService.EsPrimeraCuenta() AndAlso Not ConGoogle

            If ConGoogle Then
                PrepararRegistroConGoogle()
            ElseIf esPrimeraCuenta Then
                Me.Title = "Crear la cuenta de administrador — ALAS Honduras"
                lblAvisoRol.Text = "Eres la primera cuenta del sistema: quedarás como Administrador."
            Else
                lblAvisoRol.Text = "Te registrarás como pasajero: podrás comprar boletos, " &
                                   "hacer tu check-in y descargar tu pase de abordar."
                pnlDatosPasajero.Visibility = Visibility.Visible
                cboTipoDocumento.ItemsSource = PasajeroService.TiposDocumento
                cboTipoDocumento.SelectedIndex = 0
                cboPais.ItemsSource = CatalogoService.PaisesParaCombo().DefaultView
            End If

        Catch ex As Exception
            lblAvisoRol.Text = "Completa tus datos para crear la cuenta."
            Registro.Error_("Preparar el registro", ex)
        End Try

        AddHandler txtClave.ClaveCambiada, AddressOf MedirFuerza
        If ConGoogle Then txtNombrePila.Focus() Else txtNombre.Focus()
    End Sub

    ''' <summary>Google ya confirmó el nombre y el correo, y de la contraseña se
    ''' encarga él: aquí solo faltan los datos de viajero, que Google no conoce.</summary>
    Private Sub PrepararRegistroConGoogle()
        Me.Title = "Completa tu registro — ALAS Honduras"
        lblAvisoRol.Text = $"Entraste con Google como {identidadGoogle.Email}. " &
                           "Solo faltan tus datos de viajero; los necesitamos para poder emitir tus boletos."

        txtNombre.Text = If(identidadGoogle.Nombre, "")
        txtEmail.Text = If(identidadGoogle.Email, "")
        txtNombre.IsEnabled = False
        txtEmail.IsEnabled = False
        txtUsuario.Text = AuthService.SugerirUsuarioDesdeEmail(identidadGoogle.Email)

        ' Sin contraseña ni pregunta: la identidad la respalda Google
        pnlClaves.Visibility = Visibility.Collapsed
        pnlSeguridad.Visibility = Visibility.Collapsed

        pnlDatosPasajero.Visibility = Visibility.Visible
        cboTipoDocumento.ItemsSource = PasajeroService.TiposDocumento
        cboTipoDocumento.SelectedIndex = 0
        cboPais.ItemsSource = CatalogoService.PaisesParaCombo().DefaultView

        ' El nombre de Google suele venir como "Nombre Apellido": se reparte
        Dim partes = If(identidadGoogle.Nombre, "").Split(" "c).
                     Where(Function(p) p.Length > 0).ToArray()
        If partes.Length > 0 Then txtNombrePila.Text = partes(0)
        If partes.Length > 1 Then txtApaterno.Text = partes(1)
        If partes.Length > 2 Then txtAmaterno.Text = String.Join(" ", partes.Skip(2))

        btnCrear.Content = "Completar registro"
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

        If Not ConGoogle AndAlso txtClave.Password <> txtClave2.Password Then
            MostrarError("Las dos contraseñas no coinciden.")
            Return
        End If

        If Not esPrimeraCuenta AndAlso cboPais.SelectedValue Is Nothing Then
            MostrarError("Selecciona tu país.")
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

            Dim datos As AuthService.DatosPasajero = Nothing
            If Not esPrimeraCuenta Then
                datos = New AuthService.DatosPasajero With {
                    .Nombre = txtNombrePila.Text,
                    .ApellidoPaterno = txtApaterno.Text,
                    .ApellidoMaterno = txtAmaterno.Text,
                    .TipoDocumento = If(cboTipoDocumento.SelectedItem Is Nothing, "",
                                        cboTipoDocumento.SelectedItem.ToString()),
                    .NumDocumento = txtDocumento.Text,
                    .FechaNacimiento = dpNacimiento.SelectedDate,
                    .IdPais = cboPais.SelectedValue.ToString(),
                    .Telefono = txtTelefono.Text
                }
            End If

            Dim primera = esPrimeraCuenta
            Dim google = If(ConGoogle, identidadGoogle.GoogleId, Nothing)

            ' El hash BCrypt es lento a propósito: se calcula fuera del hilo de la interfaz
            Dim problema = Await Task.Run(
                Function()
                    If primera Then
                        Return AuthService.RegistrarAdministradorInicial(
                            nombre, email, usuario, clave, pregunta, respuesta)
                    End If
                    Return AuthService.RegistrarPasajero(
                        nombre, email, usuario, clave, pregunta, respuesta, datos, google)
                End Function)

            If problema IsNot Nothing Then
                MostrarError(problema)
                Return
            End If

            SeRegistro = True

            DialogoAlas.Show(
                If(ConGoogle,
                   $"¡Listo! Tu cuenta quedó ligada a Google. Ya puedes reservar tus vuelos.",
                   $"La cuenta '{usuario.Trim()}' se creó correctamente. Ya puedes iniciar sesión."),
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
