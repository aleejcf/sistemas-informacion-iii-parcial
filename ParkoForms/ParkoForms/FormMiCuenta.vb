''' <summary>Permite al usuario que ya inició sesión configurar su pregunta de seguridad
''' (necesaria para recuperar la contraseña) y cambiar su contraseña.</summary>
Public Class FormMiCuenta
    Inherits Form

    Private cboPregunta As ComboBox
    Private txtRespuesta As TextBox
    Private txtActual, txtNueva, txtConfirmar As CajaClaveParko
    Private lblEstadoPregunta As Label
    Private lblError As Label

    Public Sub New()
        Me.Text = "Mi cuenta — PARKO Honduras"
        Me.ClientSize = New Size(470, 640)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Fondo
        Me.Font = New Font("Segoe UI", 9.0F)

        ' ===== Datos del usuario =====
        Dim panelUsuario As New Panel With {
            .Location = New Point(16, 16), .Size = New Size(438, 74), .BackColor = Color.White
        }
        Dim lblNombre As New Label With {
            .Text = Sesion.UsuarioActual.NombreCompleto, .AutoSize = True, .Location = New Point(14, 12),
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }
        Dim lblRol As New Label With {
            .Text = " " & Sesion.UsuarioActual.Rol.ToUpper() & " ", .AutoSize = True,
            .Location = New Point(16, 44), .BackColor = Verde, .ForeColor = AzulTecno,
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold)
        }
        Dim lblUsuario As New Label With {
            .Text = "@" & Sesion.UsuarioActual.NombreUsuario, .AutoSize = True,
            .Location = New Point(100, 45), .ForeColor = TextoSuave, .Font = New Font("Segoe UI", 9.0F)
        }
        panelUsuario.Controls.AddRange({lblNombre, lblRol, lblUsuario})

        ' ===== Pregunta de seguridad =====
        Dim panelPregunta As New Panel With {
            .Location = New Point(16, 102), .Size = New Size(438, 216), .BackColor = Color.White
        }
        Dim lblTituloPregunta As New Label With {
            .Text = "Pregunta de seguridad", .AutoSize = True, .Location = New Point(14, 12),
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }
        lblEstadoPregunta = New Label With {
            .AutoSize = False, .Size = New Size(410, 34), .Location = New Point(16, 38),
            .Font = New Font("Segoe UI", 8.5F)
        }
        panelPregunta.Controls.Add(EtiquetaCampo("PREGUNTA", 16, 76))
        cboPregunta = New ComboBox With {
            .Location = New Point(16, 94), .Width = 406,
            .Font = New Font("Segoe UI", 10.0F), .DropDownStyle = ComboBoxStyle.DropDown
        }
        cboPregunta.Items.AddRange({
            "¿Cuál es el nombre de tu primera mascota?",
            "¿En qué ciudad naciste?",
            "¿Cuál es tu comida favorita?",
            "¿Cómo se llama tu mejor amigo de la infancia?",
            "¿Cuál es tu equipo favorito?"
        })
        panelPregunta.Controls.Add(EtiquetaCampo("RESPUESTA", 16, 128))
        txtRespuesta = CajaTexto(16, 146, 406)

        Dim btnGuardarPregunta As New Button With {
            .Text = "Guardar pregunta de seguridad", .Size = New Size(406, 36), .Location = New Point(16, 174)
        }
        EstilizarBoton(btnGuardarPregunta, VerdeBoton)
        AddHandler btnGuardarPregunta.Click, AddressOf btnGuardarPregunta_Click

        panelPregunta.Controls.AddRange({lblTituloPregunta, lblEstadoPregunta, cboPregunta,
                                         txtRespuesta, btnGuardarPregunta})

        ' ===== Cambio de contraseña =====
        Dim panelClave As New Panel With {
            .Location = New Point(16, 330), .Size = New Size(438, 214), .BackColor = Color.White
        }
        Dim lblTituloClave As New Label With {
            .Text = "Cambiar mi contraseña", .AutoSize = True, .Location = New Point(14, 12),
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }
        panelClave.Controls.Add(EtiquetaCampo("CONTRASEÑA ACTUAL", 16, 42))
        txtActual = New CajaClaveParko With {.Location = New Point(16, 60), .Width = 406}
        panelClave.Controls.Add(EtiquetaCampo("NUEVA CONTRASEÑA (MÍNIMO 6, LETRAS Y NÚMEROS)", 16, 94))
        txtNueva = New CajaClaveParko With {.Location = New Point(16, 112), .Width = 406}
        panelClave.Controls.Add(EtiquetaCampo("CONFIRMAR NUEVA CONTRASEÑA", 16, 140))
        txtConfirmar = New CajaClaveParko With {.Location = New Point(16, 158), .Width = 406}

        Dim btnCambiarClave As New Button With {
            .Text = "Cambiar contraseña", .Size = New Size(406, 34), .Location = New Point(16, 172)
        }
        EstilizarBoton(btnCambiarClave, Azul)
        AddHandler btnCambiarClave.Click, AddressOf btnCambiarClave_Click

        panelClave.Controls.AddRange({lblTituloClave, txtActual, txtNueva, txtConfirmar, btnCambiarClave})
        btnCambiarClave.Location = New Point(16, 180)

        lblError = New Label With {
            .AutoSize = False, .Size = New Size(438, 34), .Location = New Point(16, 552),
            .ForeColor = Peligro, .Font = New Font("Segoe UI", 9.0F), .Visible = False
        }

        Dim btnCerrar As New Button With {.Text = "Cerrar", .Size = New Size(438, 34),
            .Location = New Point(16, 592)}
        EstilizarBoton(btnCerrar, Gris)
        AddHandler btnCerrar.Click, Sub() Me.Close()

        Me.Controls.AddRange({panelUsuario, panelPregunta, panelClave, lblError, btnCerrar})
        Me.CancelButton = btnCerrar
    End Sub

    Private Sub FormMiCuenta_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        MostrarEstadoDeLaPregunta()
    End Sub

    Private Sub MostrarEstadoDeLaPregunta()
        Try
            Dim consulta = AuthService.ObtenerPregunta(Sesion.UsuarioActual.NombreUsuario)
            If consulta.Estado = AuthService.EstadoPregunta.Encontrada Then
                lblEstadoPregunta.Text = "✔ Ya tienes una pregunta configurada. Puedes cambiarla cuando quieras."
                lblEstadoPregunta.ForeColor = VerdeBoton
                cboPregunta.Text = consulta.Pregunta
            Else
                lblEstadoPregunta.Text = "⚠ Aún no tienes pregunta de seguridad. Configúrala para poder " &
                                         "recuperar tu contraseña si la olvidas."
                lblEstadoPregunta.ForeColor = Peligro
            End If
        Catch ex As Exception
            lblEstadoPregunta.Text = MensajeError.Traducir("Consultar pregunta de seguridad", ex)
        End Try
    End Sub

    Private Sub btnGuardarPregunta_Click(sender As Object, e As EventArgs)
        lblError.Visible = False

        Try
            Dim mensajeError = AuthService.ConfigurarPregunta(Sesion.UsuarioActual.NombreUsuario,
                                                              cboPregunta.Text, txtRespuesta.Text)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            DialogoParko.Show("Pregunta de seguridad guardada." & Environment.NewLine &
                              "Ya puedes recuperar tu contraseña con ella si la olvidas.", "Éxito")
            txtRespuesta.Clear()
            MostrarEstadoDeLaPregunta()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Guardar pregunta de seguridad", ex))
        End Try
    End Sub

    Private Sub btnCambiarClave_Click(sender As Object, e As EventArgs)
        lblError.Visible = False

        If txtNueva.Password <> txtConfirmar.Password Then
            MostrarError("Las contraseñas nuevas no coinciden.")
            Return
        End If

        Try
            Dim mensajeError = AuthService.CambiarContrasenaConActual(Sesion.UsuarioActual.NombreUsuario,
                                                                      txtActual.Password, txtNueva.Password)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            DialogoParko.Show("Tu contraseña se cambió correctamente.", "Éxito")
            txtActual.Clear()
            txtNueva.Clear()
            txtConfirmar.Clear()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Cambiar contraseña", ex))
        End Try
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visible = True
    End Sub
End Class
