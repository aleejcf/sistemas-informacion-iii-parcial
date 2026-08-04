Imports System.Windows.Threading

''' <summary>Ventana principal: menú lateral, encabezado y el área donde se
''' muestra la página activa. Las páginas se crean una sola vez y se reutilizan,
''' así cambiar de sección es instantáneo y no se pierde lo que se estaba haciendo.</summary>
Public Class MainWindow

    Private paginaPanel As PanelPage
    Private paginaItinerario As ItinerarioPage
    Private paginaReservar As ReservarPage
    Private paginaReservas As ReservasPage
    Private paginaPagos As PagosPage
    Private paginaVuelos As VuelosPage
    Private paginaPasajeros As PasajerosPage
    Private paginaCatalogos As CatalogosPage
    Private paginaUsuarios As UsuariosPage
    Private paginaBitacora As BitacoraPage
    Private paginaMisVuelos As MisVuelosPage
    Private paginaMiPerfil As MiPerfilPage

    Private WithEvents reloj As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1)}

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)

        If Sesion.UsuarioActual Is Nothing Then
            Me.Close()
            Return
        End If

        lblNombreUsuario.Text = Sesion.UsuarioActual.NombreCompleto
        lblRol.Text = Sesion.UsuarioActual.Rol.ToUpper()

        ArmarMenuSegunElRol()

        ActualizarReloj()
        reloj.Start()

        RevisarPreguntaDeSeguridad()

        ' Cada rol arranca en la pantalla que le importa
        If Permisos.EsPasajero Then
            rbMisVuelos.IsChecked = True
        Else
            rbPanel.IsChecked = True
        End If
    End Sub

    ''' <summary>Lo que un rol no puede usar ni siquiera aparece en el menú.
    ''' Esconder la opción no es la seguridad —esa la aplican los servicios—, pero
    ''' sí evita que alguien se topee con puertas cerradas.</summary>
    Private Sub ArmarMenuSegunElRol()
        Dim esPasajero = Permisos.EsPasajero
        Dim paraPasajero = If(esPasajero, Visibility.Visible, Visibility.Collapsed)
        Dim paraPersonal = If(esPasajero, Visibility.Collapsed, Visibility.Visible)

        ' Portal del pasajero
        lblSeccionViaje.Visibility = paraPasajero
        rbMisVuelos.Visibility = paraPasajero
        rbMiPerfil.Visibility = paraPasajero

        ' Operación y registros: solo el personal
        lblSeccionOperacion.Visibility = paraPersonal
        rbPanel.Visibility = paraPersonal
        rbItinerario.Visibility = paraPersonal
        lblSeccionRegistros.Visibility = paraPersonal
        rbVuelos.Visibility = paraPersonal
        rbPasajeros.Visibility = paraPersonal
        rbCatalogos.Visibility = paraPersonal
        rbReservas.Visibility = paraPersonal
        rbPagos.Visibility = paraPersonal

        ' Reservar lo usan los dos, pero el pasajero lo ve como "Reservar un vuelo"
        If esPasajero Then
            lblSeccionVentas.Text = "COMPRAR"
            rbReservar.Content = "🎫   Reservar un vuelo"
        End If

        ' Administración: solo un Administrador
        Dim visibleAdmin = If(Permisos.PuedeGestionarUsuarios, Visibility.Visible, Visibility.Collapsed)
        lblSeccionAdmin.Visibility = visibleAdmin
        rbUsuarios.Visibility = visibleAdmin
        rbBitacora.Visibility = If(Permisos.PuedeVerBitacora, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Sub rbMisVuelos_Checked(sender As Object, e As RoutedEventArgs) Handles rbMisVuelos.Checked
        If paginaMisVuelos Is Nothing Then paginaMisVuelos = New MisVuelosPage()
        MostrarPagina(paginaMisVuelos, "Mis vuelos", "Tus reservas, tu check-in y tu pase de abordar")
        paginaMisVuelos.Cargar()
    End Sub

    Private Sub rbMiPerfil_Checked(sender As Object, e As RoutedEventArgs) Handles rbMiPerfil.Checked
        If paginaMiPerfil Is Nothing Then paginaMiPerfil = New MiPerfilPage()
        MostrarPagina(paginaMiPerfil, "Mi perfil", "Tus datos de viajero y tu cuenta")
        paginaMiPerfil.Cargar()
    End Sub

    Private Sub reloj_Tick(sender As Object, e As EventArgs) Handles reloj.Tick
        ActualizarReloj()
    End Sub

    ''' <summary>En un mostrador de aerolínea la hora exacta importa: los vuelos
    ''' se abordan y se cierran por reloj.</summary>
    Private Sub ActualizarReloj()
        lblReloj.Text = DateTime.Now.ToString("HH:mm:ss")
        lblFecha.Text = Formato.FechaLarga(DateTime.Now)
    End Sub

    ''' <summary>Avisa con un ícono si la cuenta todavía no puede recuperar su
    ''' contraseña por no tener pregunta de seguridad configurada.</summary>
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

    ' ---------- Navegación ----------

    Private Sub rbPanel_Checked(sender As Object, e As RoutedEventArgs) Handles rbPanel.Checked
        If paginaPanel Is Nothing Then paginaPanel = New PanelPage()
        MostrarPagina(paginaPanel, "Panel de control", "Cómo va la operación de hoy")
        paginaPanel.Cargar()
    End Sub

    Private Sub rbItinerario_Checked(sender As Object, e As RoutedEventArgs) Handles rbItinerario.Checked
        If paginaItinerario Is Nothing Then paginaItinerario = New ItinerarioPage()
        MostrarPagina(paginaItinerario, "Llegadas y salidas", "Tablero de vuelos del día")
        paginaItinerario.Cargar()
    End Sub

    Private Sub rbReservar_Checked(sender As Object, e As RoutedEventArgs) Handles rbReservar.Checked
        If paginaReservar Is Nothing Then paginaReservar = New ReservarPage()
        MostrarPagina(paginaReservar, "Nueva reserva", "Buscar vuelo, elegir asiento y emitir los boletos")
    End Sub

    Private Sub rbReservas_Checked(sender As Object, e As RoutedEventArgs) Handles rbReservas.Checked
        If paginaReservas Is Nothing Then paginaReservas = New ReservasPage()
        MostrarPagina(paginaReservas, "Reservas", "Consultar, hacer check-in y cancelar")
        paginaReservas.Cargar()
    End Sub

    Private Sub rbPagos_Checked(sender As Object, e As RoutedEventArgs) Handles rbPagos.Checked
        If paginaPagos Is Nothing Then paginaPagos = New PagosPage()
        MostrarPagina(paginaPagos, "Pagos", "Cobros y comprobantes")
        paginaPagos.Cargar()
    End Sub

    Private Sub rbVuelos_Checked(sender As Object, e As RoutedEventArgs) Handles rbVuelos.Checked
        If paginaVuelos Is Nothing Then paginaVuelos = New VuelosPage()
        MostrarPagina(paginaVuelos, "Vuelos", "Programación de la malla de vuelos")
        paginaVuelos.Cargar()
    End Sub

    Private Sub rbPasajeros_Checked(sender As Object, e As RoutedEventArgs) Handles rbPasajeros.Checked
        If paginaPasajeros Is Nothing Then paginaPasajeros = New PasajerosPage()
        MostrarPagina(paginaPasajeros, "Pasajeros", "Datos de quienes viajan")
        paginaPasajeros.Cargar()
    End Sub

    Private Sub rbCatalogos_Checked(sender As Object, e As RoutedEventArgs) Handles rbCatalogos.Checked
        If paginaCatalogos Is Nothing Then paginaCatalogos = New CatalogosPage()
        MostrarPagina(paginaCatalogos, "Catálogos", "Países, aeropuertos, aerolíneas, aviones y tarifas")
        paginaCatalogos.Cargar()
    End Sub

    Private Sub rbUsuarios_Checked(sender As Object, e As RoutedEventArgs) Handles rbUsuarios.Checked
        If paginaUsuarios Is Nothing Then paginaUsuarios = New UsuariosPage()
        MostrarPagina(paginaUsuarios, "Usuarios", "Cuentas del personal del sistema")
        paginaUsuarios.Cargar()
    End Sub

    Private Sub rbBitacora_Checked(sender As Object, e As RoutedEventArgs) Handles rbBitacora.Checked
        If paginaBitacora Is Nothing Then paginaBitacora = New BitacoraPage()
        MostrarPagina(paginaBitacora, "Bitácora", "Auditoría: quién hizo qué y cuándo")
        paginaBitacora.Cargar()
    End Sub

    Private Sub MostrarPagina(pagina As UserControl, titulo As String, subtitulo As String)
        Contenido.Content = pagina
        lblTituloPagina.Text = titulo
        lblSubtituloPagina.Text = subtitulo
        TransicionVentana.FundirEntrada(Contenido)
    End Sub

    ''' <summary>Permite que una página mande a otra: al emitir una reserva se salta
    ''' a la lista de reservas, por ejemplo.</summary>
    Public Sub IrAReservas()
        rbReservas.IsChecked = True
        If paginaReservas IsNot Nothing Then paginaReservas.Cargar()
    End Sub

    ' ---------- Cierre de sesión ----------

    Private Sub btnCerrarSesion_Click(sender As Object, e As RoutedEventArgs) Handles btnCerrarSesion.Click
        Dim respuesta = DialogoAlas.Show("¿Seguro que deseas cerrar la sesión?", "Cerrar sesión",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        BitacoraService.Registrar(BitacoraService.CIERRE_SESION, "usuario")
        reloj.Stop()
        Sesion.Cerrar()

        Dim login As New LoginWindow()
        login.Show()
        Me.Close()
    End Sub
End Class
