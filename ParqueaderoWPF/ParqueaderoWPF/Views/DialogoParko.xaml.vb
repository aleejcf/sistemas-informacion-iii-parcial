''' <summary>Diálogo personalizado de PARKO que reemplaza al MessageBox de Windows.
''' Tiene la misma firma que MessageBox.Show para poder usarse igual.</summary>
Public Class DialogoParko

    Private resultado As MessageBoxResult = MessageBoxResult.None
    Private esPregunta As Boolean = False

    Public Overloads Shared Function Show(mensaje As String) As MessageBoxResult
        Return Show(mensaje, "PARKO", MessageBoxButton.OK, MessageBoxImage.Information)
    End Function

    Public Overloads Shared Function Show(mensaje As String, titulo As String) As MessageBoxResult
        Return Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information)
    End Function

    Public Overloads Shared Function Show(mensaje As String, titulo As String,
                                          botones As MessageBoxButton) As MessageBoxResult
        Return Show(mensaje, titulo, botones, MessageBoxImage.Information)
    End Function

    Public Overloads Shared Function Show(mensaje As String, titulo As String,
                                          botones As MessageBoxButton,
                                          icono As MessageBoxImage) As MessageBoxResult
        Dim dialogo As New DialogoParko()
        dialogo.lblTitulo.Text = titulo
        dialogo.lblMensaje.Text = mensaje

        ' Icono según el tipo de mensaje
        Select Case icono
            Case MessageBoxImage.Error
                dialogo.lblIcono.Text = "❌"
            Case MessageBoxImage.Warning
                dialogo.lblIcono.Text = "⚠️"
            Case MessageBoxImage.Question
                dialogo.lblIcono.Text = "❓"
            Case Else
                ' Mensajes informativos: check verde si es de éxito
                If titulo.IndexOf("éxito", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   titulo.IndexOf("exito", StringComparison.OrdinalIgnoreCase) >= 0 Then
                    dialogo.lblIcono.Text = "✅"
                Else
                    dialogo.lblIcono.Text = "ℹ️"
                End If
        End Select

        ' Botones
        If botones = MessageBoxButton.YesNo Then
            dialogo.esPregunta = True
            dialogo.btnNo.Visibility = Visibility.Visible
            dialogo.btnSi.Content = "Sí"
        End If

        ' Centrar sobre la ventana activa
        Dim activa = Application.Current.Windows.OfType(Of Window)().
            FirstOrDefault(Function(w) w.IsActive AndAlso w.IsLoaded)
        If activa IsNot Nothing Then
            dialogo.Owner = activa
            dialogo.WindowStartupLocation = WindowStartupLocation.CenterOwner
        End If

        dialogo.ShowDialog()
        Return dialogo.resultado
    End Function

    Private Sub btnSi_Click(sender As Object, e As RoutedEventArgs) Handles btnSi.Click
        resultado = If(esPregunta, MessageBoxResult.Yes, MessageBoxResult.OK)
        Me.Close()
    End Sub

    Private Sub btnNo_Click(sender As Object, e As RoutedEventArgs) Handles btnNo.Click
        resultado = MessageBoxResult.No
        Me.Close()
    End Sub
End Class
