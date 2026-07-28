''' <summary>Recuperación de contraseña en tres pasos:
''' buscar el usuario, responder su pregunta de seguridad y definir la contraseña nueva.</summary>
Public Class FormRecuperarClave
    Inherits Form

    Private txtUsuario As TextBox
    Private btnBuscar As Button
    Private lblPregunta As Label
    Private txtRespuesta As TextBox
    Private btnVerificar As Button
    Private txtNueva As CajaClaveParko
    Private txtConfirmar As CajaClaveParko
    Private btnCambiar As Button
    Private lblError As Label
    Private panelPregunta As Panel
    Private panelNueva As Panel
    Private usuarioValidado As String = Nothing

    Public Sub New()
        Me.Text = "Recuperar contraseña — PARKO Honduras"
        Me.ClientSize = New Size(448, 520)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.0F)

        Dim lblTitulo As New Label With {
            .Text = "Recuperar contraseña", .AutoSize = True, .Location = New Point(22, 16),
            .Font = New Font("Segoe UI", 17.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }
        Dim lblSub As New Label With {
            .Text = "Responde tu pregunta de seguridad para crear una contraseña nueva.",
            .AutoSize = False, .Size = New Size(404, 20), .Location = New Point(24, 52),
            .ForeColor = TextoSuave, .Font = New Font("Segoe UI", 9.0F)
        }

        ' ----- Paso 1: buscar el usuario -----
        Me.Controls.Add(EtiquetaCampo("USUARIO", 24, 84))
        txtUsuario = CajaTexto(24, 102, 300)
        btnBuscar = New Button With {.Text = "Buscar", .Size = New Size(96, 30), .Location = New Point(330, 102)}
        EstilizarBoton(btnBuscar, Azul)
        AddHandler btnBuscar.Click, AddressOf btnBuscar_Click

        ' ----- Paso 2: pregunta de seguridad -----
        panelPregunta = New Panel With {
            .Location = New Point(0, 142), .Size = New Size(448, 130),
            .BackColor = Color.White, .Visible = False
        }
        panelPregunta.Controls.Add(EtiquetaCampo("TU PREGUNTA DE SEGURIDAD", 24, 4))
        lblPregunta = New Label With {
            .AutoSize = False, .Size = New Size(400, 36), .Location = New Point(24, 22),
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold), .ForeColor = TextoOscuro
        }
        panelPregunta.Controls.Add(lblPregunta)
        panelPregunta.Controls.Add(EtiquetaCampo("RESPUESTA", 24, 58))
        txtRespuesta = CajaTexto(24, 76, 400)
        panelPregunta.Controls.Add(txtRespuesta)
        btnVerificar = New Button With {.Text = "Verificar respuesta", .Size = New Size(400, 34),
            .Location = New Point(24, 110)}
        EstilizarBoton(btnVerificar, Azul)
        AddHandler btnVerificar.Click, AddressOf btnVerificar_Click
        panelPregunta.Controls.Add(btnVerificar)

        ' ----- Paso 3: contraseña nueva -----
        panelNueva = New Panel With {
            .Location = New Point(0, 278), .Size = New Size(448, 140),
            .BackColor = Color.White, .Visible = False
        }
        panelNueva.Controls.Add(EtiquetaCampo("NUEVA CONTRASEÑA (MÍNIMO 6, LETRAS Y NÚMEROS)", 24, 4))
        txtNueva = New CajaClaveParko With {.Location = New Point(24, 22), .Width = 400}
        panelNueva.Controls.Add(txtNueva)
        panelNueva.Controls.Add(EtiquetaCampo("CONFIRMAR NUEVA CONTRASEÑA", 24, 58))
        txtConfirmar = New CajaClaveParko With {.Location = New Point(24, 76), .Width = 400}
        panelNueva.Controls.Add(txtConfirmar)
        btnCambiar = New Button With {.Text = "Cambiar contraseña", .Size = New Size(400, 36),
            .Location = New Point(24, 112)}
        EstilizarBoton(btnCambiar, VerdeBoton)
        AddHandler btnCambiar.Click, AddressOf btnCambiar_Click
        panelNueva.Controls.Add(btnCambiar)

        lblError = New Label With {
            .AutoSize = False, .Size = New Size(400, 36), .Location = New Point(24, 424),
            .ForeColor = Peligro, .Font = New Font("Segoe UI", 9.0F), .Visible = False
        }

        Dim btnCancelar As New Button With {.Text = "Cancelar", .Size = New Size(400, 34),
            .Location = New Point(24, 468)}
        EstilizarBoton(btnCancelar, Gris)
        AddHandler btnCancelar.Click, Sub() Me.Close()

        Me.Controls.AddRange({lblTitulo, lblSub, txtUsuario, btnBuscar,
                              panelPregunta, panelNueva, lblError, btnCancelar})
        Me.AcceptButton = btnBuscar
        Me.CancelButton = btnCancelar
    End Sub

    ' ---------- Paso 1 ----------
    Private Sub btnBuscar_Click(sender As Object, e As EventArgs)
        lblError.Visible = False
        panelPregunta.Visible = False
        panelNueva.Visible = False
        usuarioValidado = Nothing

        If String.IsNullOrWhiteSpace(txtUsuario.Text) Then
            MostrarError("Escribe tu nombre de usuario.")
            Return
        End If

        Try
            Dim consulta = AuthService.ObtenerPregunta(txtUsuario.Text)

            Select Case consulta.Estado
                Case AuthService.EstadoPregunta.UsuarioNoExiste
                    MostrarError("No existe un usuario con ese nombre. Revisa que esté bien escrito.")
                    Return

                Case AuthService.EstadoPregunta.SinConfigurar
                    MostrarError("Esta cuenta todavía no tiene pregunta de seguridad. Inicia sesión y " &
                                 "ve a «Mi cuenta» para configurarla.")
                    Return
            End Select

            lblPregunta.Text = consulta.Pregunta
            txtRespuesta.Clear()
            panelPregunta.Visible = True
            txtRespuesta.Focus()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Buscar pregunta de seguridad", ex))
        End Try
    End Sub

    ' ---------- Paso 2 ----------
    Private Sub btnVerificar_Click(sender As Object, e As EventArgs)
        lblError.Visible = False

        Try
            If Not AuthService.VerificarRespuesta(txtUsuario.Text, txtRespuesta.Text) Then
                MostrarError("La respuesta no es correcta. Inténtalo de nuevo.")
                Return
            End If

            usuarioValidado = txtUsuario.Text.Trim()
            txtUsuario.Enabled = False
            btnBuscar.Enabled = False
            txtRespuesta.Enabled = False
            btnVerificar.Enabled = False
            panelNueva.Visible = True
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Verificar respuesta", ex))
        End Try
    End Sub

    ' ---------- Paso 3 ----------
    Private Sub btnCambiar_Click(sender As Object, e As EventArgs)
        lblError.Visible = False
        If usuarioValidado Is Nothing Then Return

        If txtNueva.Password <> txtConfirmar.Password Then
            MostrarError("Las contraseñas no coinciden.")
            Return
        End If

        Try
            Dim mensajeError = AuthService.CambiarContrasena(usuarioValidado, txtNueva.Password)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            DialogoParko.Show("¡Contraseña cambiada con éxito!" & Environment.NewLine &
                              "Ya puedes iniciar sesión con tu nueva contraseña.", "Recuperación exitosa")
            Me.Close()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Cambiar contraseña", ex))
        End Try
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visible = True
    End Sub
End Class
