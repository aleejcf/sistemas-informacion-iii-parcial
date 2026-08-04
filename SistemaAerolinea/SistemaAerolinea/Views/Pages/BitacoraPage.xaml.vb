Imports System.Data

''' <summary>Consulta de la bitácora de auditoría. Los intentos fallidos de inicio
''' de sesión aparecen aquí: es lo primero que se revisa cuando alguien reporta un
''' acceso extraño.</summary>
Public Class BitacoraPage

    Private Const TODOS_USUARIOS As String = "Todos los usuarios"
    Private Const TODAS_ACCIONES As String = "Todas las acciones"

    Private combosListos As Boolean = False

    Public Sub Cargar()
        If Not combosListos Then LlenarCombos()
        Filtrar()
    End Sub

    Private Sub LlenarCombos()
        Try
            Dim usuarios As New List(Of String) From {TODOS_USUARIOS}
            For Each fila As DataRow In BitacoraService.Usuarios().Rows
                usuarios.Add(fila("usuario").ToString())
            Next
            cboUsuario.ItemsSource = usuarios
            cboUsuario.SelectedIndex = 0

            Dim acciones As New List(Of String) From {TODAS_ACCIONES}
            For Each fila As DataRow In BitacoraService.Acciones().Rows
                acciones.Add(fila("accion").ToString())
            Next
            cboAccion.ItemsSource = acciones
            cboAccion.SelectedIndex = 0

            combosListos = True

        Catch ex As Exception
            Avisar("Cargar los filtros de la bitácora", ex)
        End Try
    End Sub

    Private Sub Filtrar()
        Try
            Dim usuario As String = Nothing
            If cboUsuario.SelectedIndex > 0 Then usuario = cboUsuario.SelectedItem.ToString()

            Dim accion As String = Nothing
            If cboAccion.SelectedIndex > 0 Then accion = cboAccion.SelectedItem.ToString()

            Dim datos = BitacoraService.Listar(usuario, accion, dpDesde.SelectedDate)
            dgBitacora.ItemsSource = datos.DefaultView

            lblVacio.Visibility = If(datos.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

            Dim fallidos = datos.Select("exito = 0").Length
            lblRegistros.Text = datos.Rows.Count.ToString()
            lblCorrectos.Text = (datos.Rows.Count - fallidos).ToString()
            lblFallidos.Text = fallidos.ToString()

        Catch ex As Exception
            Avisar("Consultar la bitácora", ex)
        End Try
    End Sub

    Private Sub btnFiltrar_Click(sender As Object, e As RoutedEventArgs) Handles btnFiltrar.Click
        Filtrar()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        cboUsuario.SelectedIndex = 0
        cboAccion.SelectedIndex = 0
        dpDesde.SelectedDate = Nothing
        Filtrar()
    End Sub

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub
End Class
