Public Class DashboardPage

    Public Sub Cargar()
        Try
            Dim estadisticas = MovimientoService.Estadisticas()
            lblDentro.Text = estadisticas("vehiculos_dentro").ToString()
            lblIngresos.Text = "L " & CDec(estadisticas("ingresos_hoy")).ToString("N2")
            lblClientes.Text = estadisticas("total_clientes").ToString()
            lblMovsHoy.Text = estadisticas("movimientos_hoy").ToString()
            dgUltimos.ItemsSource = MovimientoService.UltimosMovimientos().DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar el dashboard", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnActualizar_Click(sender As Object, e As RoutedEventArgs) Handles btnActualizar.Click
        Cargar()
    End Sub
End Class
