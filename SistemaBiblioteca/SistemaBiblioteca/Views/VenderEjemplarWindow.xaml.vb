''' <summary>Registra la venta de un ejemplar ya dado de baja. Solo pide el precio
''' y, si se sabe, quién lo compró — el resto (código, título) lo trae la propia
''' ventana para que quien vende confirme que está vendiendo lo que cree.</summary>
Public Class VenderEjemplarWindow

    Public Property IdEjemplar As Integer
    Public Property Titulo As String
    Public Property CodigoBarras As String

    Private Sub Ventana_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        lblTitulo.Text = Titulo
        lblCodigo.Text = CodigoBarras
        txtPrecio.Focus()
    End Sub

    Private Sub btnVender_Click(sender As Object, e As RoutedEventArgs) Handles btnVender.Click
        OcultarError()

        Dim precio As Decimal
        If Not Decimal.TryParse(txtPrecio.Text.Trim(), precio) OrElse precio <= 0 Then
            MostrarError("Escribe un precio válido, mayor que cero.")
            TransicionVentana.Sacudir(panelFormulario)
            Return
        End If

        Try
            Dim problema = LibroService.VenderEjemplar(IdEjemplar, precio, txtComprador.Text)
            If problema IsNot Nothing Then
                MostrarError(problema)
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            Me.DialogResult = True
            Me.Close()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Vender el ejemplar", ex))
        End Try
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
