Imports System.Data

''' <summary>La auditoría del sistema. A diferencia del log en archivo, que guarda
''' los detalles técnicos de los errores, esta bitácora registra las acciones del
''' negocio: quién prestó, quién cobró, quién condonó y quién intentó entrar sin
''' lograrlo.</summary>
Public Class BitacoraPage

    Private ocupado As Boolean = False

    Public Sub Cargar()
        CargarFiltros()
        CargarLista()
    End Sub

    Private Sub BitacoraPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
    End Sub

    ''' <summary>Los filtros se llenan con lo que de verdad hay en la bitácora, no
    ''' con una lista fija: así nunca ofrecen una opción que no daría resultados.</summary>
    Private Sub CargarFiltros()
        Try
            ocupado = True
            Dim usuario = cboUsuario.SelectedValue
            Dim accion = cboAccion.SelectedValue

            cboUsuario.ItemsSource = BitacoraService.Usuarios().DefaultView
            cboAccion.ItemsSource = BitacoraService.Acciones().DefaultView

            cboUsuario.SelectedValue = usuario
            cboAccion.SelectedValue = accion

        Catch ex As Exception
            Registro.Advertencia($"No se pudieron cargar los filtros de la bitácora: {ex.Message}")
        Finally
            ocupado = False
        End Try
    End Sub

    Private Sub CargarLista()
        Try
            Dim dt = BitacoraService.Listar(TryCast(cboUsuario.SelectedValue, String),
                                            TryCast(cboAccion.SelectedValue, String),
                                            dtpDesde.SelectedDate)

            ' `exito` es un bit; la tabla necesita una palabra que se pueda pintar
            ' con el mismo mapa de colores que el resto del sistema.
            dt.Columns.Add("resultado", GetType(String))
            For Each fila As DataRow In dt.Rows
                Dim exito = Not IsDBNull(fila("exito")) AndAlso CBool(fila("exito"))
                fila("resultado") = If(exito, "Devuelto", "Vencido")
            Next

            dgBitacora.ItemsSource = dt.DefaultView
            pnlVacio.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            lblConteo.Text = $"{dt.Rows.Count} registros (los más recientes primero)"

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar la bitácora", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub cboUsuario_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboUsuario.SelectionChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub cboAccion_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboAccion.SelectionChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub dtpDesde_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dtpDesde.SelectedDateChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        ocupado = True
        cboUsuario.SelectedIndex = -1
        cboAccion.SelectedIndex = -1
        dtpDesde.SelectedDate = Nothing
        ocupado = False
        CargarLista()
    End Sub

    Private Sub btnActualizar_Click(sender As Object, e As RoutedEventArgs) Handles btnActualizar.Click
        Cargar()
    End Sub
End Class
