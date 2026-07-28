''' <summary>Diálogo personalizado PARKO que reemplaza al MessageBox de Windows.
''' Uso: DialogoParko.Show("mensaje", "título", MessageBoxButtons.OK, MessageBoxIcon.Information)</summary>
Public Class DialogoParko
    Inherits Form

    Private resultado As DialogResult = DialogResult.Cancel

    Public Overloads Shared Function Show(mensaje As String,
                                          Optional titulo As String = "PARKO",
                                          Optional botones As MessageBoxButtons = MessageBoxButtons.OK,
                                          Optional icono As MessageBoxIcon = MessageBoxIcon.Information) As DialogResult
        Using dialogo As New DialogoParko(mensaje, titulo, botones, icono)
            dialogo.ShowDialog()
            Return dialogo.resultado
        End Using
    End Function

    Private Sub New(mensaje As String, titulo As String,
                    botones As MessageBoxButtons, icono As MessageBoxIcon)
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.ShowInTaskbar = False
        Me.Width = 440

        ' ----- Encabezado con la marca -----
        Dim encabezado As New Panel With {.Dock = DockStyle.Top, .Height = 44, .BackColor = AzulTecno}
        Dim pbLogo As New PictureBox With {
            .Image = CargarLogo(), .SizeMode = PictureBoxSizeMode.Zoom,
            .Size = New Size(26, 26), .Location = New Point(14, 9), .BackColor = Color.Transparent
        }
        Dim lblTitulo As New Label With {
            .Text = titulo, .AutoSize = True, .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold), .Location = New Point(48, 11),
            .BackColor = Color.Transparent
        }
        encabezado.Controls.Add(pbLogo)
        encabezado.Controls.Add(lblTitulo)

        ' ----- Icono según el tipo -----
        Dim textoIcono As String
        Select Case icono
            Case MessageBoxIcon.Error
                textoIcono = "❌"
            Case MessageBoxIcon.Warning
                textoIcono = "⚠️"
            Case MessageBoxIcon.Question
                textoIcono = "❓"
            Case Else
                If titulo.IndexOf("éxito", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   titulo.IndexOf("exito", StringComparison.OrdinalIgnoreCase) >= 0 Then
                    textoIcono = "✅"
                Else
                    textoIcono = "ℹ️"
                End If
        End Select
        Dim lblIcono As New Label With {
            .Text = textoIcono, .Font = New Font("Segoe UI Emoji", 22.0F),
            .AutoSize = True, .Location = New Point(20, 62)
        }

        ' ----- Mensaje (alto calculado según el texto) -----
        Dim fuenteMensaje As New Font("Segoe UI", 10.5F)
        Dim tamano = TextRenderer.MeasureText(mensaje, fuenteMensaje,
            New Size(320, Integer.MaxValue), TextFormatFlags.WordBreak)
        Dim lblMensaje As New Label With {
            .Text = mensaje, .Font = fuenteMensaje, .ForeColor = TextoOscuro,
            .Location = New Point(80, 62), .Size = New Size(330, tamano.Height + 6),
            .AutoSize = False
        }

        ' ----- Botones -----
        Dim yBotones = 62 + Math.Max(tamano.Height + 6, 44) + 18
        Dim btnSi As New Button With {.Size = New Size(110, 36)}
        EstilizarBoton(btnSi, VerdeBoton)

        If botones = MessageBoxButtons.YesNo Then
            btnSi.Text = "Sí"
            btnSi.Location = New Point(Me.Width - 240, yBotones)
            Dim btnNo As New Button With {.Text = "No", .Size = New Size(100, 36),
                .Location = New Point(Me.Width - 120, yBotones)}
            EstilizarBoton(btnNo, Gris)
            AddHandler btnNo.Click, Sub()
                                        resultado = DialogResult.No
                                        Me.Close()
                                    End Sub
            AddHandler btnSi.Click, Sub()
                                        resultado = DialogResult.Yes
                                        Me.Close()
                                    End Sub
            Me.Controls.Add(btnNo)
            Me.CancelButton = btnNo
        Else
            btnSi.Text = "Aceptar"
            btnSi.Location = New Point(Me.Width - 130, yBotones)
            AddHandler btnSi.Click, Sub()
                                        resultado = DialogResult.OK
                                        Me.Close()
                                    End Sub
            Me.CancelButton = btnSi
        End If

        Me.Controls.Add(lblIcono)
        Me.Controls.Add(lblMensaje)
        Me.Controls.Add(btnSi)
        Me.Controls.Add(encabezado)
        Me.AcceptButton = btnSi
        Me.Height = yBotones + 36 + 18

        ' Borde fino alrededor del diálogo
        AddHandler Me.Paint, Sub(s, e)
                                 ControlPaint.DrawBorder(e.Graphics, Me.ClientRectangle,
                                                         AzulTecno, ButtonBorderStyle.Solid)
                             End Sub
    End Sub
End Class
