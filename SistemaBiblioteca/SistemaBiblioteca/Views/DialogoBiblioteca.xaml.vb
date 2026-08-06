''' <summary>Diálogo propio de ALEJANDRÍA que reemplaza al MessageBox de Windows.
''' Mantiene la misma firma que MessageBox.Show para poder usarse igual, y añade
''' dos modos más: uno con "dato copiable" para folios y claves temporales, y otro
''' con el comprobante completo del préstamo o de la multa.</summary>
Public Class DialogoBiblioteca

    Private resultado As MessageBoxResult = MessageBoxResult.None
    Private esPregunta As Boolean = False

    Public Overloads Shared Function Show(mensaje As String) As MessageBoxResult
        Return Show(mensaje, "Biblioteca Alejandría", MessageBoxButton.OK, MessageBoxImage.Information)
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
        Return Mostrar(mensaje, titulo, botones, icono, Nothing, Nothing)
    End Function

    ''' <summary>Variante con un dato destacado y botón de copiar: se usa para el
    ''' folio de un préstamo recién registrado o una contraseña temporal.</summary>
    Public Shared Function MostrarConDato(mensaje As String, titulo As String, dato As String,
                                          Optional icono As MessageBoxImage = MessageBoxImage.Information) As MessageBoxResult
        Return Mostrar(mensaje, titulo, MessageBoxButton.OK, icono, dato, Nothing)
    End Function

    ''' <summary>Variante con el comprobante completo, listo para copiar y pegar
    ''' donde se vaya a imprimir.</summary>
    Public Shared Function MostrarComprobante(mensaje As String, titulo As String, dato As String,
                                              comprobante As String) As MessageBoxResult
        Return Mostrar(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information,
                       dato, comprobante)
    End Function

    Private Shared Function Mostrar(mensaje As String, titulo As String,
                                    botones As MessageBoxButton, icono As MessageBoxImage,
                                    dato As String, comprobante As String) As MessageBoxResult
        Dim dialogo As New DialogoBiblioteca()
        dialogo.lblTitulo.Text = titulo
        dialogo.lblMensaje.Text = mensaje

        Select Case icono
            Case MessageBoxImage.Error
                dialogo.lblIcono.Text = "❌"
            Case MessageBoxImage.Warning
                dialogo.lblIcono.Text = "⚠️"
            Case MessageBoxImage.Question
                dialogo.lblIcono.Text = "❓"
            Case Else
                ' Los mensajes de éxito llevan check verde; el resto, informativo
                If titulo.IndexOf("éxito", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   titulo.IndexOf("exito", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   titulo.IndexOf("list", StringComparison.OrdinalIgnoreCase) = 0 Then
                    dialogo.lblIcono.Text = "✅"
                Else
                    dialogo.lblIcono.Text = "ℹ️"
                End If
        End Select

        If Not String.IsNullOrWhiteSpace(dato) Then
            dialogo.lblDato.Text = dato
            dialogo.pnlDato.Visibility = Visibility.Visible
        End If

        If Not String.IsNullOrWhiteSpace(comprobante) Then
            dialogo.lblComprobante.Text = comprobante
            dialogo.pnlComprobante.Visibility = Visibility.Visible
        End If

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

    Private Sub btnCopiar_Click(sender As Object, e As RoutedEventArgs) Handles btnCopiar.Click
        btnCopiar.Content = Copiar(lblDato.Text, "¡Copiado!")
    End Sub

    Private Sub btnCopiarComprobante_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnCopiarComprobante.Click
        btnCopiarComprobante.Content = Copiar(lblComprobante.Text, "¡Comprobante copiado!")
    End Sub

    Private Shared Function Copiar(texto As String, mensajeExito As String) As String
        Try
            Clipboard.SetText(texto)
            Return mensajeExito
        Catch
            ' El portapapeles puede estar ocupado por otra aplicación: no es motivo de error
            Return "No se pudo copiar"
        End Try
    End Function
End Class
