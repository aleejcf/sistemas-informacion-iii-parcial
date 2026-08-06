''' <summary>Página de solo lectura (admin) con los últimos 200 intentos de inicio de sesión.
''' No hay botón de eliminar a propósito: un registro de auditoría no debería poder borrarse.</summary>
Public Class AuditoriaPage

    Private Sub AuditoriaPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        CargarLista()
    End Sub

    Private Sub CargarLista(Optional filtro As String = "")
        Try
            dgAuditoria.ItemsSource = AuditoriaService.Listar(filtro).DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar la auditoría", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        CargarLista(txtBuscar.Text)
    End Sub

    Private Sub btnLimpiarBusqueda_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiarBusqueda.Click
        txtBuscar.Clear()
    End Sub
End Class
