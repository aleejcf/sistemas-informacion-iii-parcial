Imports System.Globalization

Public Class MainWindow

    ' Las páginas se crean una sola vez y se reutilizan
    Private paginaDashboard As DashboardPage
    Private paginaClientes As ClientesPage
    Private paginaParqueaderos As ParqueaderosPage
    Private paginaVehiculos As VehiculosPage
    Private paginaMovimientos As MovimientosPage

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Sesion.UsuarioActual Is Nothing Then
            Me.Close()
            Return
        End If

        lblNombreUsuario.Text = Sesion.UsuarioActual.NombreCompleto
        lblRol.Text = Sesion.UsuarioActual.Rol.ToUpper()

        Dim cultura = New CultureInfo("es-HN")
        lblFecha.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", cultura)

        rbDashboard.IsChecked = True
    End Sub

    Private Sub rbDashboard_Checked(sender As Object, e As RoutedEventArgs) Handles rbDashboard.Checked
        If paginaDashboard Is Nothing Then paginaDashboard = New DashboardPage()
        MostrarPagina(paginaDashboard, "Dashboard")
        paginaDashboard.Cargar()
    End Sub

    Private Sub rbClientes_Checked(sender As Object, e As RoutedEventArgs) Handles rbClientes.Checked
        If paginaClientes Is Nothing Then paginaClientes = New ClientesPage()
        MostrarPagina(paginaClientes, "Clientes")
    End Sub

    Private Sub rbParqueaderos_Checked(sender As Object, e As RoutedEventArgs) Handles rbParqueaderos.Checked
        If paginaParqueaderos Is Nothing Then paginaParqueaderos = New ParqueaderosPage()
        MostrarPagina(paginaParqueaderos, "Parqueaderos")
    End Sub

    Private Sub rbVehiculos_Checked(sender As Object, e As RoutedEventArgs) Handles rbVehiculos.Checked
        If paginaVehiculos Is Nothing Then paginaVehiculos = New VehiculosPage()
        MostrarPagina(paginaVehiculos, "Vehículos")
    End Sub

    Private Sub rbMovimientos_Checked(sender As Object, e As RoutedEventArgs) Handles rbMovimientos.Checked
        If paginaMovimientos Is Nothing Then paginaMovimientos = New MovimientosPage()
        MostrarPagina(paginaMovimientos, "Entradas y salidas")
    End Sub

    Private Sub MostrarPagina(pagina As UserControl, titulo As String)
        Contenido.Content = pagina
        lblTituloPagina.Text = titulo
    End Sub

    Private Sub btnCerrarSesion_Click(sender As Object, e As RoutedEventArgs) Handles btnCerrarSesion.Click
        Dim respuesta = DialogoParko.Show("¿Seguro que deseas cerrar la sesión?", "Cerrar sesión",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta = MessageBoxResult.Yes Then
            Sesion.Cerrar()
            Dim login As New LoginWindow()
            login.Show()
            Me.Close()
        End If
    End Sub
End Class
