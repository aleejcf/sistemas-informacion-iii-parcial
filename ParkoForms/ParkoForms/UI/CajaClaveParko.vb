Imports System.Drawing.Drawing2D

''' <summary>Caja de contraseña con un ojo dibujado a la derecha para mostrarla u ocultarla.
''' El icono es vectorial: ojo abierto cuando está oculta, ojo tachado cuando está visible.</summary>
Public Class CajaClaveParko
    Inherits UserControl

    Private ReadOnly caja As TextBox
    Private ReadOnly zonaOjo As Panel
    Private mostrando As Boolean = False
    Private ReadOnly ayuda As New ToolTip()

    <ComponentModel.DesignerSerializationVisibility(ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property Password As String
        Get
            Return caja.Text
        End Get
        Set(value As String)
            caja.Text = value
        End Set
    End Property

    Public Sub New()
        Me.Height = 30
        Me.Width = 320
        Me.BackColor = Color.White
        Me.BorderStyle = BorderStyle.FixedSingle
        Me.Padding = New Padding(1)

        caja = New TextBox With {
            .BorderStyle = BorderStyle.None,
            .Font = New Font("Segoe UI", 11.0F),
            .UseSystemPasswordChar = True,
            .Dock = DockStyle.Fill
        }

        zonaOjo = New Panel With {
            .Width = 34,
            .Dock = DockStyle.Right,
            .Cursor = Cursors.Hand,
            .BackColor = Color.White
        }
        AddHandler zonaOjo.Paint, AddressOf DibujarOjo
        AddHandler zonaOjo.Click, AddressOf AlternarVisibilidad
        ayuda.SetToolTip(zonaOjo, "Mostrar contraseña")

        ' Un contenedor con margen deja el texto separado del borde
        Dim contenedor As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(9, 6, 0, 0)}
        contenedor.Controls.Add(caja)

        Me.Controls.Add(contenedor)
        Me.Controls.Add(zonaOjo)
    End Sub

    Private Sub AlternarVisibilidad(sender As Object, e As EventArgs)
        mostrando = Not mostrando
        caja.UseSystemPasswordChar = Not mostrando
        ayuda.SetToolTip(zonaOjo, If(mostrando, "Ocultar contraseña", "Mostrar contraseña"))
        zonaOjo.Invalidate()
        caja.Focus()
        caja.SelectionStart = caja.Text.Length
    End Sub

    ''' <summary>Dibuja el ojo: contorno almendrado, pupila y, si está visible, la línea diagonal.</summary>
    Private Sub DibujarOjo(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim cx = zonaOjo.Width / 2.0F
        Dim cy = zonaOjo.Height / 2.0F
        Dim ancho = 9.0F
        Dim alto = 6.0F

        Using lapiz As New Pen(Color.FromArgb(100, 116, 139), 1.6F)
            lapiz.StartCap = LineCap.Round
            lapiz.EndCap = LineCap.Round

            ' Contorno del ojo: dos curvas que se juntan en las esquinas
            Using contorno As New GraphicsPath()
                contorno.AddBezier(cx - ancho, cy, cx - ancho / 2, cy - alto, cx + ancho / 2, cy - alto, cx + ancho, cy)
                contorno.AddBezier(cx + ancho, cy, cx + ancho / 2, cy + alto, cx - ancho / 2, cy + alto, cx - ancho, cy)
                g.DrawPath(lapiz, contorno)
            End Using

            ' Pupila
            Using relleno As New SolidBrush(Color.FromArgb(100, 116, 139))
                g.FillEllipse(relleno, cx - 2.4F, cy - 2.4F, 4.8F, 4.8F)
            End Using

            ' Línea diagonal cuando la contraseña está visible
            If mostrando Then
                g.DrawLine(lapiz, cx - ancho + 1, cy + alto, cx + ancho - 1, cy - alto)
            End If
        End Using
    End Sub

    Public Overloads Sub Clear()
        caja.Clear()
    End Sub
End Class
