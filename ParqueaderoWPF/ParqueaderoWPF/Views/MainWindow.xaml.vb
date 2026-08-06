Imports System.Globalization

Public Class MainWindow

    ' Las páginas se crean una sola vez y se reutilizan
    Private paginaDashboard As DashboardPage
    Private paginaClientes As ClientesPage
    Private paginaParqueaderos As ParqueaderosPage
    Private paginaVehiculos As VehiculosPage
    Private paginaMovimientos As MovimientosPage
    Private paginaUsuarios As UsuariosPage
    Private paginaAuditoria As AuditoriaPage

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        If Sesion.UsuarioActual Is Nothing Then
            Me.Close()
            Return
        End If

        lblNombreUsuario.Text = Sesion.UsuarioActual.NombreCompleto
        lblRol.Text = Sesion.UsuarioActual.Rol.ToUpper()
        rbUsuarios.Visibility = If(Permisos.PuedeGestionarUsuarios, Visibility.Visible, Visibility.Collapsed)
        rbAuditoria.Visibility = If(Permisos.PuedeVerAuditoria, Visibility.Visible, Visibility.Collapsed)

        Dim cultura = New CultureInfo("es-HN")
        lblFecha.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", cultura)

        RevisarPreguntaDeSeguridad()
        rbDashboard.IsChecked = True
    End Sub

    ''' <summary>Avisa con un ícono si la cuenta todavía no puede recuperar su contraseña.</summary>
    Private Sub RevisarPreguntaDeSeguridad()
        Try
            Dim configurada = AuthService.TienePreguntaConfigurada(Sesion.UsuarioActual.NombreUsuario)
            lblAvisoSeguridad.Visibility = If(configurada, Visibility.Collapsed, Visibility.Visible)
        Catch
            lblAvisoSeguridad.Visibility = Visibility.Collapsed
        End Try
    End Sub

    Private Sub btnMiCuenta_Click(sender As Object, e As RoutedEventArgs) Handles btnMiCuenta.Click
        Dim miCuenta As New MiCuentaWindow With {.Owner = Me}
        miCuenta.ShowDialog()
        RevisarPreguntaDeSeguridad()
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

    Private Sub rbUsuarios_Checked(sender As Object, e As RoutedEventArgs) Handles rbUsuarios.Checked
        If paginaUsuarios Is Nothing Then paginaUsuarios = New UsuariosPage()
        MostrarPagina(paginaUsuarios, "Usuarios")
    End Sub

    Private Sub rbAuditoria_Checked(sender As Object, e As RoutedEventArgs) Handles rbAuditoria.Checked
        If paginaAuditoria Is Nothing Then paginaAuditoria = New AuditoriaPage()
        MostrarPagina(paginaAuditoria, "Auditoría de inicios de sesión")
    End Sub

    Private Sub MostrarPagina(pagina As UserControl, titulo As String)
        Contenido.Content = pagina
        lblTituloPagina.Text = titulo
        TransicionVentana.FundirEntrada(Contenido)
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
