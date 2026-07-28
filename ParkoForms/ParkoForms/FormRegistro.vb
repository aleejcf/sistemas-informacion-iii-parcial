Public Class FormRegistro
    Inherits Form

    Private txtNombre As TextBox
    Private txtEmail As TextBox
    Private txtUsuario As TextBox
    Private txtClave As TextBox
    Private txtConfirmar As TextBox
    Private lblInfoRol As Label
    Private lblError As Label

    Public Sub New()
        Me.Text = "Crear cuenta — PARKO Honduras"
        Me.ClientSize = New Size(448, 560)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.0F)

        Dim lblTitulo As New Label With {
            .Text = "Crear cuenta", .AutoSize = True, .Location = New Point(22, 16),
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }
        lblInfoRol = New Label With {
            .AutoSize = False, .Size = New Size(404, 34), .Location = New Point(24, 54),
            .ForeColor = TextoSuave, .Font = New Font("Segoe UI", 9.0F)
        }

        Me.Controls.Add(EtiquetaCampo("NOMBRE COMPLETO", 24, 96))
        txtNombre = CajaTexto(24, 114, 400)
        Me.Controls.Add(EtiquetaCampo("CORREO ELECTRÓNICO", 24, 152))
        txtEmail = CajaTexto(24, 170, 400)
        Me.Controls.Add(EtiquetaCampo("USUARIO", 24, 208))
        txtUsuario = CajaTexto(24, 226, 400)
        Me.Controls.Add(EtiquetaCampo("CONTRASEÑA (MÍNIMO 6, LETRAS Y NÚMEROS)", 24, 264))
        txtClave = CajaTexto(24, 282, 400)
        txtClave.UseSystemPasswordChar = True
        Me.Controls.Add(EtiquetaCampo("CONFIRMAR CONTRASEÑA", 24, 320))
        txtConfirmar = CajaTexto(24, 338, 400)
        txtConfirmar.UseSystemPasswordChar = True

        lblError = New Label With {
            .AutoSize = False, .Size = New Size(400, 36), .Location = New Point(24, 378),
            .ForeColor = Peligro, .Font = New Font("Segoe UI", 9.0F), .Visible = False
        }

        Dim btnRegistrar As New Button With {.Text = "Registrarme", .Size = New Size(400, 42),
            .Location = New Point(24, 420)}
        EstilizarBoton(btnRegistrar, VerdeBoton)
        AddHandler btnRegistrar.Click, AddressOf btnRegistrar_Click

        Dim btnCancelar As New Button With {.Text = "Cancelar", .Size = New Size(400, 36),
            .Location = New Point(24, 470)}
        EstilizarBoton(btnCancelar, Gris)
        AddHandler btnCancelar.Click, Sub() Me.Close()

        Me.Controls.AddRange({lblTitulo, lblInfoRol, txtNombre, txtEmail, txtUsuario,
                              txtClave, txtConfirmar, lblError, btnRegistrar, btnCancelar})
        Me.AcceptButton = btnRegistrar
        Me.CancelButton = btnCancelar
    End Sub

    Private Sub FormRegistro_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            If AuthService.HayUsuarios() Then
                lblInfoRol.Text = "Tu cuenta se creará con rol de Operador." & Environment.NewLine &
                                  "Un Administrador puede cambiarlo después."
            Else
                lblInfoRol.Text = "Eres el primer usuario del sistema:" & Environment.NewLine &
                                  "tu cuenta será de Administrador."
            End If
        Catch
            lblInfoRol.Text = ""
        End Try
    End Sub

    Private Sub btnRegistrar_Click(sender As Object, e As EventArgs)
        lblError.Visible = False

        If txtClave.Text <> txtConfirmar.Text Then
            MostrarError("Las contraseñas no coinciden.")
            Return
        End If

        Try
            ' Se consulta ANTES de registrar para saber qué rol le tocará
            Dim esPrimero As Boolean = Not AuthService.HayUsuarios()

            Dim mensajeError = AuthService.Registrar(txtNombre.Text, txtEmail.Text,
                                                     txtUsuario.Text, txtClave.Text)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            Dim rolAsignado = If(esPrimero, "Administrador", "Operador")
            DialogoParko.Show($"¡Cuenta creada con éxito, {txtNombre.Text.Trim()}!" & Environment.NewLine &
                              $"Tu rol asignado es: {rolAsignado}." & Environment.NewLine &
                              "Ya puedes iniciar sesión.", "Registro exitoso")
            Me.Close()

        Catch ex As Exception
            MostrarError("Error al registrar: " & ex.Message)
        End Try
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visible = True
    End Sub
End Class
