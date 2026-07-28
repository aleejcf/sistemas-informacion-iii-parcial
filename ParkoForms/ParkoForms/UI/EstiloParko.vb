''' <summary>Colores y estilos de la marca PARKO para toda la aplicación.</summary>
Module EstiloParko

    Public ReadOnly AzulTecno As Color = Color.FromArgb(10, 37, 64)      ' #0A2540
    Public ReadOnly AzulHover As Color = Color.FromArgb(18, 58, 95)
    Public ReadOnly Verde As Color = Color.FromArgb(0, 230, 118)         ' #00E676
    Public ReadOnly VerdeBoton As Color = Color.FromArgb(22, 163, 74)
    Public ReadOnly Azul As Color = Color.FromArgb(37, 99, 235)
    Public ReadOnly Gris As Color = Color.FromArgb(71, 85, 105)
    Public ReadOnly Peligro As Color = Color.FromArgb(220, 38, 38)
    Public ReadOnly Fondo As Color = Color.FromArgb(241, 245, 249)
    Public ReadOnly TextoOscuro As Color = Color.FromArgb(15, 23, 42)
    Public ReadOnly TextoSuave As Color = Color.FromArgb(100, 116, 139)
    Public ReadOnly Borde As Color = Color.FromArgb(226, 232, 240)

    Public Sub EstilizarBoton(b As Button, colorFondo As Color)
        b.FlatStyle = FlatStyle.Flat
        b.FlatAppearance.BorderSize = 0
        b.BackColor = colorFondo
        b.ForeColor = Color.White
        b.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        b.Cursor = Cursors.Hand
    End Sub

    Public Sub EstilizarBotonMenu(b As Button)
        b.FlatStyle = FlatStyle.Flat
        b.FlatAppearance.BorderSize = 0
        b.BackColor = AzulTecno
        b.FlatAppearance.MouseOverBackColor = AzulHover
        b.ForeColor = Color.White
        b.Font = New Font("Segoe UI", 11.0F)
        b.TextAlign = ContentAlignment.MiddleLeft
        b.Cursor = Cursors.Hand
    End Sub

    Public Sub EstilizarGrid(dg As DataGridView)
        dg.BackgroundColor = Color.White
        dg.BorderStyle = BorderStyle.None
        dg.EnableHeadersVisualStyles = False
        dg.ColumnHeadersDefaultCellStyle.BackColor = AzulTecno
        dg.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dg.ColumnHeadersDefaultCellStyle.SelectionBackColor = AzulTecno
        dg.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        dg.ColumnHeadersHeight = 34
        dg.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dg.RowHeadersVisible = False
        dg.AllowUserToAddRows = False
        dg.AllowUserToDeleteRows = False
        dg.ReadOnly = True
        dg.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dg.MultiSelect = False
        dg.RowTemplate.Height = 30
        dg.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)
        dg.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254)
        dg.DefaultCellStyle.SelectionForeColor = TextoOscuro
        dg.DefaultCellStyle.Font = New Font("Segoe UI", 9.5F)
        dg.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dg.GridColor = Borde
    End Sub

    Public Function CargarLogo() As Image
        Dim ruta = IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png")
        If IO.File.Exists(ruta) Then Return Image.FromFile(ruta)
        Return Nothing
    End Function

    Public Function EtiquetaCampo(texto As String, x As Integer, y As Integer) As Label
        Return New Label With {
            .Text = texto,
            .Location = New Point(x, y),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .ForeColor = TextoSuave
        }
    End Function

    Public Function CajaTexto(x As Integer, y As Integer, ancho As Integer) As TextBox
        Return New TextBox With {
            .Location = New Point(x, y),
            .Width = ancho,
            .Font = New Font("Segoe UI", 11.0F)
        }
    End Function
End Module
