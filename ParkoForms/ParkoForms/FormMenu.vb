Public Class FormMenu
    Inherits Form

    <ComponentModel.DesignerSerializationVisibility(ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property CerradoPorLogout As Boolean = False

    Public Sub New()
        Me.Text = "PARKO Honduras — Sistema de Parqueadero"
        Me.ClientSize = New Size(1000, 620)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Fondo
        Me.Font = New Font("Segoe UI", 9.0F)

        ' ===== Menú lateral =====
        Dim panelLateral As New Panel With {.Dock = DockStyle.Left, .Width = 260, .BackColor = AzulTecno}

        Dim pbLogo As New PictureBox With {
            .Image = CargarLogo(), .SizeMode = PictureBoxSizeMode.Zoom,
            .Size = New Size(90, 90), .Location = New Point(85, 22), .BackColor = Color.Transparent
        }
        Dim lblParko As New Label With {
            .Text = "PARKO", .AutoSize = False, .Size = New Size(260, 34),
            .Location = New Point(0, 116), .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = Color.White, .Font = New Font("Segoe UI", 17.0F, FontStyle.Bold),
            .BackColor = Color.Transparent
        }
        Dim lblSlogan As New Label With {
            .Text = "Espacio inteligente, flujo constante.", .AutoSize = False,
            .Size = New Size(260, 20), .Location = New Point(0, 150),
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = Color.FromArgb(143, 176, 206),
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Italic), .BackColor = Color.Transparent
        }

        ' Tarjeta del usuario conectado
        Dim panelUsuario As New Panel With {
            .Location = New Point(15, 184), .Size = New Size(230, 62), .BackColor = AzulHover
        }
        Dim lblNombre As New Label With {
            .Text = Sesion.UsuarioActual.NombreCompleto, .AutoSize = False,
            .Size = New Size(210, 22), .Location = New Point(10, 8),
            .ForeColor = Color.White, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .AutoEllipsis = True
        }
        Dim lblRol As New Label With {
            .Text = " " & Sesion.UsuarioActual.Rol.ToUpper() & " ", .AutoSize = True,
            .Location = New Point(10, 32), .BackColor = Verde, .ForeColor = AzulTecno,
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold)
        }
        panelUsuario.Controls.AddRange({lblNombre, lblRol})

        ' Botones de navegación
        Dim btnClientes = BotonMenu("👥   Clientes", 270)
        AddHandler btnClientes.Click, Sub()
                                          Using f As New FormClientes() : f.ShowDialog(Me) : End Using
                                      End Sub
        Dim btnParqueaderos = BotonMenu("🏢   Parqueaderos", 324)
        AddHandler btnParqueaderos.Click, Sub()
                                              Using f As New FormParqueaderos() : f.ShowDialog(Me) : End Using
                                          End Sub
        Dim btnVehiculos = BotonMenu("🚗   Vehículos", 378)
        AddHandler btnVehiculos.Click, Sub()
                                           Using f As New FormVehiculos() : f.ShowDialog(Me) : End Using
                                       End Sub

        Dim btnCerrarSesion As New Button With {
            .Text = "⟵   Cerrar sesión", .Size = New Size(230, 42), .Location = New Point(15, 556)
        }
        EstilizarBoton(btnCerrarSesion, Peligro)
        AddHandler btnCerrarSesion.Click, AddressOf btnCerrarSesion_Click

        panelLateral.Controls.AddRange({pbLogo, lblParko, lblSlogan, panelUsuario,
                                        btnClientes, btnParqueaderos, btnVehiculos, btnCerrarSesion})

        ' ===== Panel de bienvenida =====
        Dim pbGrande As New PictureBox With {
            .Image = CargarLogo(), .SizeMode = PictureBoxSizeMode.Zoom,
            .Size = New Size(220, 220), .Location = New Point(460, 120), .BackColor = Fondo
        }
        Dim lblBienvenida As New Label With {
            .Text = $"¡Bienvenido, {Sesion.UsuarioActual.NombreCompleto}!", .AutoSize = False,
            .Size = New Size(740, 40), .Location = New Point(260, 360),
            .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = TextoOscuro,
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold)
        }
        Dim lblIndicacion As New Label With {
            .Text = "Usa el menú de la izquierda para gestionar clientes, parqueaderos y vehículos.",
            .AutoSize = False, .Size = New Size(740, 26), .Location = New Point(260, 402),
            .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = TextoSuave,
            .Font = New Font("Segoe UI", 10.5F)
        }

        Me.Controls.AddRange({pbGrande, lblBienvenida, lblIndicacion, panelLateral})
    End Sub

    Private Function BotonMenu(texto As String, y As Integer) As Button
        Dim b As New Button With {.Text = texto, .Size = New Size(230, 44), .Location = New Point(15, y)}
        EstilizarBotonMenu(b)
        Return b
    End Function

    Private Sub btnCerrarSesion_Click(sender As Object, e As EventArgs)
        If DialogoParko.Show("¿Seguro que deseas cerrar la sesión?", "Cerrar sesión",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Sesion.Cerrar()
            CerradoPorLogout = True
            Me.Close()
        End If
    End Sub
End Class
