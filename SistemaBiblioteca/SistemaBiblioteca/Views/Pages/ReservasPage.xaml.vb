Imports System.Data

''' <summary>La fila de espera de los títulos agotados. Las reservas se crean
''' desde el catálogo; aquí se ven todas juntas y se cancelan las que ya no hacen
''' falta.</summary>
Public Class ReservasPage

    Public Sub Cargar()
        ' Antes de mostrar la lista se cierran las que ya expiraron, para que no
        ' aparezcan como activas cuando ya no lo son
        ReservaService.CaducarVencidas()
        CargarLista()
    End Sub

    Private Sub ReservasPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
    End Sub

    Private Sub CargarLista()
        Try
            Dim dt = ReservaService.Listar(soloActivas:=chkHistorial.IsChecked <> True)
            dgReservas.ItemsSource = dt.DefaultView
            pnlVacio.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar las reservas", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub chkHistorial_Changed(sender As Object, e As RoutedEventArgs) _
        Handles chkHistorial.Checked, chkHistorial.Unchecked
        CargarLista()
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        Dim vista = TryCast(dgReservas.SelectedItem, DataRowView)
        If vista Is Nothing Then
            DialogoBiblioteca.Show("Selecciona una reserva de la lista.", "Falta elegir",
                                   MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim titulo = Db.Texto(vista.Row, "titulo")
        Dim socio = Db.Texto(vista.Row, "socio")

        If DialogoBiblioteca.Show($"¿Cancelar la reserva de «{titulo}» a nombre de {socio}?",
                                  "Cancelar reserva", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = ReservaService.Cancelar(Db.Numero(vista.Row, "idreserva"))

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo cancelar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            DialogoBiblioteca.Show("La reserva quedó cancelada.", "Cancelada con éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cancelar la reserva", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub
End Class
