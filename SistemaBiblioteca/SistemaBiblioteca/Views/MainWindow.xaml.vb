Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Windows.Media.Animation
Imports System.Windows.Threading

''' <summary>Ventana principal. La navegación está repartida en dos capas: el riel
''' de la izquierda dice A DÓNDE ir y la barra de arriba trae las herramientas que
''' sirven desde cualquier pantalla (buscador global, aviso de mora, reloj y la
''' cuenta conectada).
'''
''' Las páginas se crean una sola vez y se reutilizan, así cambiar de sección es
''' instantáneo y no se pierde lo que se estaba haciendo.</summary>
Public Class MainWindow
    Implements INotifyPropertyChanged

    ' ---------- Estado del riel ----------

    Private Const ANCHO_PLEGADO As Double = 76
    Private Const ANCHO_DESPLEGADO As Double = 236

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private expandido As Boolean = False

    ''' <summary>Si el riel muestra los nombres de las secciones o solo sus iconos.
    ''' Las etiquetas del menú se atan a esta propiedad desde el XAML, por eso la
    ''' ventana avisa cuando cambia.</summary>
    Public Property MenuExpandido As Boolean
        Get
            Return expandido
        End Get
        Set(value As Boolean)
            If expandido = value Then Return
            expandido = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(MenuExpandido)))
        End Set
    End Property

    ' ---------- Páginas ----------

    Private paginaPanel As PanelPage
    Private paginaPrestar As PrestarPage
    Private paginaPrestamos As PrestamosPage
    Private paginaMultas As MultasPage
    Private paginaCatalogo As CatalogoPage
    Private paginaLibros As LibrosPage
    Private paginaCatalogos As CatalogosPage
    Private paginaSocios As SociosPage
    Private paginaReservas As ReservasPage
    Private paginaUsuarios As UsuariosPage
    Private paginaBitacora As BitacoraPage

    Private WithEvents reloj As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1)}
    ' El aviso de mora se refresca cada dos minutos: la mora no cambia de un segundo a otro
    Private WithEvents vigilanteMora As New DispatcherTimer With {.Interval = TimeSpan.FromMinutes(2)}

    ' ======================= ARRANQUE =======================

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)

        If Sesion.UsuarioActual Is Nothing Then
            Me.Close()
            Return
        End If

        MostrarCuenta()

        ' Las secciones de administración ni siquiera se muestran a un Bibliotecario.
        ' Se oculta el panel entero, no cada elemento: al rótulo no se le puede tocar
        ' la visibilidad desde aquí sin romper el plegado del riel.
        rbUsuarios.Visibility = If(Permisos.PuedeGestionarUsuarios, Visibility.Visible, Visibility.Collapsed)
        rbBitacora.Visibility = If(Permisos.PuedeVerBitacora, Visibility.Visible, Visibility.Collapsed)

        Dim hayAdmin = Permisos.PuedeGestionarUsuarios OrElse Permisos.PuedeVerBitacora
        pnlAdmin.Visibility = If(hayAdmin, Visibility.Visible, Visibility.Collapsed)

        ActualizarReloj()
        reloj.Start()

        ' Las reservas que ya expiraron se cierran al abrir el sistema: mantienen
        ' limpia la fila de espera sin necesidad de un trabajo programado.
        ReservaService.CaducarVencidas()

        RevisarMora()
        vigilanteMora.Start()

        RevisarPreguntaDeSeguridad()
        rbPanel.IsChecked = True
    End Sub

    Private Sub MostrarCuenta()
        Dim usuario = Sesion.UsuarioActual

        lblNombreUsuario.Text = usuario.NombreCompleto
        lblRol.Text = usuario.Rol
        lblIniciales.Text = Iniciales(usuario.NombreCompleto)
        lblMenuNombre.Text = usuario.NombreCompleto
        lblMenuEmail.Text = usuario.Email
    End Sub

    ''' <summary>Las dos primeras iniciales del nombre, para el círculo del chip.
    ''' Si solo hay una palabra, se usa su primera letra.</summary>
    Private Shared Function Iniciales(nombreCompleto As String) As String
        If String.IsNullOrWhiteSpace(nombreCompleto) Then Return "?"

        Dim partes = nombreCompleto.Trim().Split(" "c).Where(Function(p) p <> "").ToArray()
        If partes.Length = 1 Then Return partes(0).Substring(0, 1).ToUpper()

        Return (partes(0).Substring(0, 1) & partes(1).Substring(0, 1)).ToUpper()
    End Function

    ' ======================= RIEL =======================

    Private Sub btnMenu_Click(sender As Object, e As RoutedEventArgs) Handles btnMenu.Click
        MenuExpandido = Not MenuExpandido
        lblIconoMenu.Text = If(MenuExpandido, "⟨", "☰")

        ' El ancho se anima; las etiquetas aparecen o desaparecen solas porque
        ' están atadas a MenuExpandido.
        Dim destino = If(MenuExpandido, ANCHO_DESPLEGADO, ANCHO_PLEGADO)
        Dim animacion As New DoubleAnimation(riel.ActualWidth, destino,
                                             TimeSpan.FromMilliseconds(180)) With {
            .EasingFunction = New QuadraticEase With {.EasingMode = EasingMode.EaseOut}
        }
        riel.BeginAnimation(FrameworkElement.WidthProperty, animacion)
    End Sub

    ' ======================= RELOJ Y MORA =======================

    Private Sub reloj_Tick(sender As Object, e As EventArgs) Handles reloj.Tick
        ActualizarReloj()
    End Sub

    ''' <summary>En una biblioteca la fecha importa más que la hora: de ella
    ''' dependen los vencimientos y la mora. Aun así se muestra el reloj porque
    ''' el mostrador anota la hora de cada movimiento.</summary>
    Private Sub ActualizarReloj()
        lblReloj.Text = DateTime.Now.ToString("HH:mm:ss")
        lblFecha.Text = Formato.FechaLarga(DateTime.Now)
    End Sub

    Private Sub vigilanteMora_Tick(sender As Object, e As EventArgs) Handles vigilanteMora.Tick
        RevisarMora()
    End Sub

    ''' <summary>Mantiene a la vista, desde cualquier pantalla, cuántos préstamos
    ''' están vencidos. Es el número que una biblioteca no puede permitirse olvidar.</summary>
    Public Sub RevisarMora()
        Try
            Dim indicadores = PanelService.Indicadores()
            Dim vencidos = Db.Numero(indicadores, "prestamos_vencidos")

            If vencidos > 0 Then
                lblAvisoVencidos.Text = If(vencidos = 1,
                                           "⚠  1 préstamo vencido",
                                           $"⚠  {vencidos} préstamos vencidos")
                pnlAvisoVencidos.Visibility = Visibility.Visible
            Else
                pnlAvisoVencidos.Visibility = Visibility.Collapsed
            End If

        Catch ex As Exception
            ' Un aviso que no se pudo calcular no debe interrumpir el trabajo
            pnlAvisoVencidos.Visibility = Visibility.Collapsed
            Registro.Advertencia($"No se pudo revisar la mora: {ex.Message}")
        End Try
    End Sub

    Private Sub pnlAvisoVencidos_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) _
        Handles pnlAvisoVencidos.MouseLeftButtonUp
        IrAPrestamos("Vencido")
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

    ' ======================= BUSCADOR GLOBAL =======================

    Private Sub txtBuscarGlobal_KeyDown(sender As Object, e As KeyEventArgs) Handles txtBuscarGlobal.KeyDown
        If e.Key <> Key.Enter Then Return
        Buscar(txtBuscarGlobal.Text)
    End Sub

    ''' <summary>Un solo campo para todo: según la forma de lo que se escribe, el
    ''' sistema decide a qué pantalla llevar. Un folio PR- va a préstamos, un
    ''' código U00001 a socios, y cualquier otra cosa se busca en el catálogo,
    ''' que es lo que más se pregunta en un mostrador.</summary>
    Private Sub Buscar(texto As String)
        Dim consulta = If(texto, "").Trim()
        If consulta = "" Then Return

        If Regex.IsMatch(consulta, "^PR-?\d+", RegexOptions.IgnoreCase) Then
            IrAPrestamos(filtro:=consulta)

        ElseIf Validador.EsIdSocioValido(consulta) Then
            IrASocios(consulta)

        Else
            IrACatalogo(consulta)
        End If

        txtBuscarGlobal.Clear()
    End Sub

    ' ======================= CUENTA =======================

    Private Sub btnCuenta_Click(sender As Object, e As RoutedEventArgs) Handles btnCuenta.Click
        menuCuenta.IsOpen = Not menuCuenta.IsOpen
    End Sub

    Private Sub btnMiCuenta_Click(sender As Object, e As RoutedEventArgs) Handles btnMiCuenta.Click
        menuCuenta.IsOpen = False

        Dim miCuenta As New MiCuentaWindow With {.Owner = Me}
        miCuenta.ShowDialog()
        RevisarPreguntaDeSeguridad()
    End Sub

    Private Sub btnCerrarSesion_Click(sender As Object, e As RoutedEventArgs) Handles btnCerrarSesion.Click
        menuCuenta.IsOpen = False

        Dim respuesta = DialogoBiblioteca.Show("¿Seguro que deseas cerrar la sesión?", "Cerrar sesión",
                                               MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        BitacoraService.Registrar(BitacoraService.CIERRE_SESION, "usuario")
        reloj.Stop()
        vigilanteMora.Stop()
        Sesion.Cerrar()

        Dim login As New LoginWindow()
        login.Show()
        Me.Close()
    End Sub

    ' ======================= NAVEGACIÓN =======================

    Private Sub rbPanel_Checked(sender As Object, e As RoutedEventArgs) Handles rbPanel.Checked
        If paginaPanel Is Nothing Then paginaPanel = New PanelPage()
        MostrarPagina(paginaPanel, "Panel de control", "Cómo va la biblioteca hoy")
        paginaPanel.Cargar()
    End Sub

    Private Sub rbPrestar_Checked(sender As Object, e As RoutedEventArgs) Handles rbPrestar.Checked
        If paginaPrestar Is Nothing Then paginaPrestar = New PrestarPage()
        MostrarPagina(paginaPrestar, "Prestar libros", "Elige el socio, agrega los ejemplares y confirma")
        paginaPrestar.Cargar()
    End Sub

    Private Sub rbPrestamos_Checked(sender As Object, e As RoutedEventArgs) Handles rbPrestamos.Checked
        If paginaPrestamos Is Nothing Then paginaPrestamos = New PrestamosPage()
        MostrarPagina(paginaPrestamos, "Préstamos", "Consultar, devolver, renovar y cancelar")
        paginaPrestamos.Cargar()
    End Sub

    Private Sub rbMultas_Checked(sender As Object, e As RoutedEventArgs) Handles rbMultas.Checked
        If paginaMultas Is Nothing Then paginaMultas = New MultasPage()
        MostrarPagina(paginaMultas, "Multas", "Cobros por retraso, daño y extravío")
        paginaMultas.Cargar()
    End Sub

    Private Sub rbCatalogo_Checked(sender As Object, e As RoutedEventArgs) Handles rbCatalogo.Checked
        If paginaCatalogo Is Nothing Then paginaCatalogo = New CatalogoPage()
        MostrarPagina(paginaCatalogo, "Catálogo", "Buscar títulos y ver qué ejemplares hay disponibles")
        paginaCatalogo.Cargar()
    End Sub

    Private Sub rbLibros_Checked(sender As Object, e As RoutedEventArgs) Handles rbLibros.Checked
        If paginaLibros Is Nothing Then paginaLibros = New LibrosPage()
        MostrarPagina(paginaLibros, "Libros y ejemplares", "Catalogar títulos y administrar sus copias físicas")
        paginaLibros.Cargar()
    End Sub

    Private Sub rbCatalogos_Checked(sender As Object, e As RoutedEventArgs) Handles rbCatalogos.Checked
        If paginaCatalogos Is Nothing Then paginaCatalogos = New CatalogosPage()
        MostrarPagina(paginaCatalogos, "Autores y categorías",
                      "Autores, editoriales, categorías Dewey y tipos de socio")
        paginaCatalogos.Cargar()
    End Sub

    Private Sub rbSocios_Checked(sender As Object, e As RoutedEventArgs) Handles rbSocios.Checked
        If paginaSocios Is Nothing Then paginaSocios = New SociosPage()
        MostrarPagina(paginaSocios, "Socios", "Quiénes pueden llevarse libros y en qué condiciones")
        paginaSocios.Cargar()
    End Sub

    Private Sub rbReservas_Checked(sender As Object, e As RoutedEventArgs) Handles rbReservas.Checked
        If paginaReservas Is Nothing Then paginaReservas = New ReservasPage()
        MostrarPagina(paginaReservas, "Reservas", "La fila de espera de los títulos sin copias libres")
        paginaReservas.Cargar()
    End Sub

    Private Sub rbUsuarios_Checked(sender As Object, e As RoutedEventArgs) Handles rbUsuarios.Checked
        If paginaUsuarios Is Nothing Then paginaUsuarios = New UsuariosPage()
        MostrarPagina(paginaUsuarios, "Usuarios", "Cuentas del personal de la biblioteca")
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

    ' ---------- Navegación entre páginas ----------
    ' Permite que una página mande a otra, y que el buscador global entre a la
    ' pantalla correcta ya con el filtro puesto.

    Public Sub IrAPrestamos(Optional situacion As String = Nothing,
                            Optional filtro As String = Nothing)
        rbPrestamos.IsChecked = True
        If paginaPrestamos IsNot Nothing Then paginaPrestamos.Cargar(situacion, filtro)
        RevisarMora()
    End Sub

    Public Sub IrAMultas()
        rbMultas.IsChecked = True
        If paginaMultas IsNot Nothing Then paginaMultas.Cargar()
    End Sub

    Public Sub IrAPrestar()
        rbPrestar.IsChecked = True
        If paginaPrestar IsNot Nothing Then paginaPrestar.Cargar()
    End Sub

    Public Sub IrASocios(Optional filtro As String = Nothing)
        rbSocios.IsChecked = True
        If paginaSocios IsNot Nothing Then paginaSocios.Cargar(filtro)
    End Sub

    Public Sub IrACatalogo(Optional filtro As String = Nothing)
        rbCatalogo.IsChecked = True
        If paginaCatalogo IsNot Nothing Then paginaCatalogo.Cargar(filtro)
    End Sub
End Class
