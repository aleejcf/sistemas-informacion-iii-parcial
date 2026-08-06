Imports System.Data

''' <summary>El catálogo público de la biblioteca: buscar un título y ver, copia
''' por copia, cuáles están en la estantería y cuáles andan afuera y con quién.
''' Es la pantalla que responde la pregunta que más se hace en un mostrador:
''' "¿tienen este libro y está disponible?".</summary>
Public Class CatalogoPage

    Private preparado As Boolean = False
    Private ocupado As Boolean = False
    Private libroSeleccionado As String = ""
    Private tituloSeleccionado As String = ""
    Private disponiblesSeleccionado As Integer = 0

    ' ======================= CICLO DE VIDA =======================

    ''' <summary>El buscador global de la barra superior entra aquí con el texto
    ''' ya escrito, así que la búsqueda se aplica antes de consultar.</summary>
    Public Sub Cargar(Optional filtro As String = Nothing)
        Preparar()
        CargarCategorias()

        If filtro IsNot Nothing Then
            ocupado = True
            txtBuscar.Text = filtro
            cboCategoria.SelectedIndex = -1
            chkDisponibles.IsChecked = False
            ocupado = False
        End If

        CargarLista()
    End Sub

    Private Sub CatalogoPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True
    End Sub

    Private Sub CargarCategorias()
        Try
            ocupado = True
            Dim elegida = cboCategoria.SelectedValue
            cboCategoria.ItemsSource = CatalogoService.CategoriasParaCombo().DefaultView
            cboCategoria.SelectedValue = elegida
        Catch ex As Exception
            Registro.Advertencia($"No se pudieron cargar las categorías: {ex.Message}")
        Finally
            ocupado = False
        End Try
    End Sub

    ' ======================= BÚSQUEDA =======================

    Private Sub CargarLista()
        Try
            Dim idCategoria As Integer? = Nothing
            If cboCategoria.SelectedValue IsNot Nothing Then idCategoria = CInt(cboCategoria.SelectedValue)

            Dim dt = LibroService.Listar(txtBuscar.Text, idCategoria, chkDisponibles.IsChecked = True)
            dgLibros.ItemsSource = dt.DefaultView
            pnlVacio.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

            lblResultados.Text = If(dt.Rows.Count = 1, "1 título", $"{dt.Rows.Count} títulos")

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar el catálogo", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub cboCategoria_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboCategoria.SelectionChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub chkDisponibles_Changed(sender As Object, e As RoutedEventArgs) _
        Handles chkDisponibles.Checked, chkDisponibles.Unchecked
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        ocupado = True
        txtBuscar.Clear()
        cboCategoria.SelectedIndex = -1
        chkDisponibles.IsChecked = False
        ocupado = False
        CargarLista()
    End Sub

    ' ======================= FICHA =======================

    Private Sub dgLibros_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgLibros.SelectionChanged
        Dim fila = TryCast(dgLibros.SelectedItem, DataRowView)
        If fila Is Nothing Then
            pnlFicha.Visibility = Visibility.Collapsed
            pnlSinSeleccion.Visibility = Visibility.Visible
            libroSeleccionado = ""
            Return
        End If

        MostrarFicha(fila.Row)
    End Sub

    Private Sub MostrarFicha(fila As DataRow)
        libroSeleccionado = Db.Texto(fila, "idlibro")
        tituloSeleccionado = Db.Texto(fila, "titulo")
        disponiblesSeleccionado = Db.Numero(fila, "disponibles")

        pnlSinSeleccion.Visibility = Visibility.Collapsed
        pnlFicha.Visibility = Visibility.Visible

        lblTitulo.Text = tituloSeleccionado
        lblAutor.Text = Db.Texto(fila, "autor")
        lblCodigo.Text = libroSeleccionado

        Dim isbn = Db.Texto(fila, "isbn")
        lblIsbn.Text = If(isbn = "", "— sin ISBN —", isbn)

        lblEditorial.Text = Db.Texto(fila, "editorial")
        lblCategoria.Text = Db.Texto(fila, "categoria")
        lblIdioma.Text = Db.Texto(fila, "idioma")

        Dim edicion = Db.Texto(fila, "edicion")
        Dim anio = Db.Numero(fila, "anio_publicacion")
        lblEdicion.Text = String.Join(" · ", {edicion, If(anio > 0, anio.ToString(), "")}.
                                             Where(Function(t) t <> ""))
        If lblEdicion.Text = "" Then lblEdicion.Text = "—"

        Dim disponibilidad = Db.Texto(fila, "disponibilidad")
        lblDisponibilidad.Text = disponibilidad.ToUpper()
        lblDisponibilidad.Foreground = EstadoAColorConverter.Trazo(disponibilidad)
        pnlDisponibilidad.Background = EstadoAColorFondoConverter.Relleno(disponibilidad)

        Dim dewey = Db.Texto(fila, "codigo_dewey")
        lblDewey.Text = If(dewey = "", "SIN CLASIFICAR", "DEWEY " & dewey)

        Dim reservas = Db.Numero(fila, "reservas_activas")
        If reservas > 0 Then
            lblReservas.Text = If(reservas = 1, "1 RESERVA EN ESPERA", $"{reservas} RESERVAS EN ESPERA")
            pnlReservas.Visibility = Visibility.Visible
        Else
            pnlReservas.Visibility = Visibility.Collapsed
        End If

        Dim sinopsis = Db.Texto(fila, "sinopsis")
        lblSinopsis.Text = sinopsis
        lblSinopsis.Visibility = If(sinopsis = "", Visibility.Collapsed, Visibility.Visible)

        CargarEjemplares(Db.Numero(fila, "total_ejemplares"))
        AjustarBotones()
        TransicionVentana.FundirEntrada(pnlFicha)
    End Sub

    Private Sub CargarEjemplares(total As Integer)
        Try
            listaEjemplares.ItemsSource = LibroService.ListarEjemplares(libroSeleccionado).DefaultView
            lblTituloCopias.Text = If(total = 1, "COPIA FÍSICA (1)", $"COPIAS FÍSICAS ({total})")

        Catch ex As Exception
            Registro.Advertencia($"No se pudieron cargar los ejemplares: {ex.Message}")
        End Try
    End Sub

    Private Sub AjustarBotones()
        ' Prestar solo tiene sentido si hay copias libres; apartar, solo si NO las hay
        btnPrestar.IsEnabled = disponiblesSeleccionado > 0
        btnReservar.IsEnabled = disponiblesSeleccionado = 0

        btnReservar.ToolTip = If(disponiblesSeleccionado > 0,
                                 "No hace falta apartar: hay copias disponibles ahora mismo.",
                                 "Apartar el título para el próximo socio de la fila.")
    End Sub

    ' ======================= ACCIONES =======================

    Private Sub btnPrestar_Click(sender As Object, e As RoutedEventArgs) Handles btnPrestar.Click
        Dim principal = TryCast(Window.GetWindow(Me), MainWindow)
        If principal IsNot Nothing Then principal.IrAPrestar()
    End Sub

    Private Sub btnReservar_Click(sender As Object, e As RoutedEventArgs) Handles btnReservar.Click
        If libroSeleccionado = "" Then Return

        Dim selector As New SeleccionarSocioWindow With {
            .Owner = Window.GetWindow(Me),
            .Subtitulo = $"¿Para quién se aparta «{tituloSeleccionado}»?"
        }
        If selector.ShowDialog() <> True Then Return

        Try
            Dim problema = ReservaService.Crear(libroSeleccionado, selector.SocioElegido)

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo apartar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            DialogoBiblioteca.Show(
                $"«{tituloSeleccionado}» quedó apartado para {selector.NombreElegido}. " &
                $"La reserva vence en {ReservaService.DIAS_VIGENCIA} días.",
                "Apartado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Apartar el título", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub
End Class
