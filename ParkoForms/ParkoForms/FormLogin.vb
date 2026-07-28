Public Class FormLogin
    Inherits Form

    Private txtUsuario As TextBox
    Private txtClave As TextBox
    Private lblError As Label
    Private btnIngresar As Button
    Private intentosFallidos As Integer = 0
    Private segundosBloqueo As Integer = 0
    Private WithEvents temporizador As New Timer With {.Interval = 1000}

    Public Sub New()
        Me.Text = "Iniciar sesión — PARKO Honduras"
        Me.ClientSize = New Size(900, 520)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.0F)

        ' ===== Panel izquierdo: marca PARKO =====
        Dim panelMarca As New Panel With {.Dock = DockStyle.Left, .Width = 400, .BackColor = AzulTecno}
        Dim pbLogo As New PictureBox With {
            .Image = CargarLogo(), .SizeMode = PictureBoxSizeMode.Zoom,
            .Size = New Size(180, 180), .Location = New Point(110, 70), .BackColor = Color.Transparent
        }
        Dim lblParko As New Label With {
            .Text = "PARKO", .AutoSize = False, .Size = New Size(400, 46),
            .Location = New Point(0, 270), .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = Color.White, .Font = New Font("Segoe UI", 26.0F, FontStyle.Bold),
            .BackColor = Color.Transparent
        }
        Dim lblHonduras As New Label With {
            .Text = "H O N D U R A S", .AutoSize = False, .Size = New Size(400, 24),
            .Location = New Point(0, 318), .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = Verde, .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .BackColor = Color.Transparent
        }
        Dim lblSlogan As New Label With {
            .Text = "Espacio inteligente, flujo constante.", .AutoSize = False,
            .Size = New Size(400, 22), .Location = New Point(0, 350),
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = Color.FromArgb(143, 176, 206),
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Italic), .BackColor = Color.Transparent
        }
        panelMarca.Controls.AddRange({pbLogo, lblParko, lblHonduras, lblSlogan})

        ' ===== Lado derecho: formulario =====
        Dim lblBienvenido As New Label With {
            .Text = "Bienvenido 👋", .AutoSize = True, .Location = New Point(487, 110),
            .Font = New Font("Segoe UI", 20.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }
        Dim lblSub As New Label With {
            .Text = "Inicia sesión para continuar", .AutoSize = True, .Location = New Point(490, 152),
            .Font = New Font("Segoe UI", 10.0F), .ForeColor = TextoSuave
        }

        Me.Controls.Add(EtiquetaCampo("USUARIO", 490, 200))
        txtUsuario = CajaTexto(490, 219, 320)
        Me.Controls.Add(EtiquetaCampo("CONTRASEÑA", 490, 262))
        txtClave = CajaTexto(490, 281, 320)
        txtClave.UseSystemPasswordChar = True

        lblError = New Label With {
            .AutoSize = False, .Size = New Size(320, 40), .Location = New Point(490, 320),
            .ForeColor = Peligro, .Font = New Font("Segoe UI", 9.0F), .Visible = False
        }

        btnIngresar = New Button With {.Text = "Ingresar", .Size = New Size(320, 42),
            .Location = New Point(490, 366)}
        EstilizarBoton(btnIngresar, Azul)
        AddHandler btnIngresar.Click, AddressOf btnIngresar_Click

        Dim linkRegistro As New LinkLabel With {
            .Text = "¿No tienes cuenta? Regístrate aquí", .AutoSize = False,
            .Size = New Size(320, 24), .Location = New Point(490, 422),
            .TextAlign = ContentAlignment.MiddleCenter, .LinkColor = Azul,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold), .LinkBehavior = LinkBehavior.NeverUnderline
        }
        AddHandler linkRegistro.LinkClicked, Sub()
                                                 Using registro As New FormRegistro()
                                                     registro.ShowDialog(Me)
                                                 End Using
                                             End Sub

        Me.Controls.AddRange({lblBienvenido, lblSub, txtUsuario, txtClave, lblError, btnIngresar, linkRegistro, panelMarca})
        Me.AcceptButton = btnIngresar
    End Sub

    Private Sub btnIngresar_Click(sender As Object, e As EventArgs)
        lblError.Visible = False

        If String.IsNullOrWhiteSpace(txtUsuario.Text) OrElse String.IsNullOrWhiteSpace(txtClave.Text) Then
            MostrarError("Escribe tu usuario y contraseña.")
            Return
        End If

        Try
            Dim usuario = AuthService.Autenticar(txtUsuario.Text, txtClave.Text)

            If usuario Is Nothing Then
                intentosFallidos += 1
                If intentosFallidos >= 3 Then
                    BloquearTemporalmente()
                Else
                    MostrarError($"Usuario o contraseña incorrectos. Intento {intentosFallidos} de 3.")
                End If
                txtClave.Clear()
                Return
            End If

            ' Sesión iniciada: abrir el menú principal
            Sesion.UsuarioActual = usuario
            Dim menu As New FormMenu()
            Me.Hide()
            menu.ShowDialog()

            If menu.CerradoPorLogout Then
                txtUsuario.Clear()
                txtClave.Clear()
                lblError.Visible = False
                Me.Show()
                txtUsuario.Focus()
            Else
                Me.Close()
            End If

        Catch ex As Exception
            MostrarError("No se pudo conectar a la base de datos. Verifica que SQL Server esté encendido.")
        End Try
    End Sub

    ' ---------- Bloqueo temporal tras 3 intentos fallidos ----------
    Private Sub BloquearTemporalmente()
        segundosBloqueo = 30
        btnIngresar.Enabled = False
        txtUsuario.Enabled = False
        txtClave.Enabled = False
        MostrarError($"Demasiados intentos fallidos. Espera {segundosBloqueo} segundos.")
        temporizador.Start()
    End Sub

    Private Sub temporizador_Tick(sender As Object, e As EventArgs) Handles temporizador.Tick
        segundosBloqueo -= 1
        If segundosBloqueo <= 0 Then
            temporizador.Stop()
            intentosFallidos = 0
            btnIngresar.Enabled = True
            txtUsuario.Enabled = True
            txtClave.Enabled = True
            lblError.Visible = False
        Else
            MostrarError($"Demasiados intentos fallidos. Espera {segundosBloqueo} segundos.")
        End If
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visible = True
    End Sub
End Class
