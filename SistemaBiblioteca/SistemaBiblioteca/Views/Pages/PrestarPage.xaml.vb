Imports System.Collections.ObjectModel
Imports System.Data

''' <summary>El mostrador de préstamo. Un asistente de tres pasos —socio, libros,
''' confirmar— con el carrito siempre a la vista, para que el bibliotecario nunca
''' pierda de vista qué lleva acumulado ni a quién se lo va a entregar.
'''
''' La página informa y guía; quien decide si el préstamo procede es
''' PrestamoService dentro de su transacción.</summary>
Public Class PrestarPage

    ''' <summary>El carrito. ObservableCollection y no List: la lista de la derecha
    ''' se redibuja sola cada vez que se agrega o quita un ejemplar.</summary>
    Private ReadOnly carrito As New ObservableCollection(Of EjemplarElegido)

    Private socioElegido As SocioResumen
    Private libroSeleccionado As String = ""
    Private preparado As Boolean = False
    Private ocupado As Boolean = False
    Private pasoActual As Integer = 1

    ' ======================= CICLO DE VIDA =======================

    Public Sub Cargar()
        Preparar()
        CargarCategorias()
        ReiniciarAsistente()
    End Sub

    Private Sub PrestarPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True

        listaCarrito.ItemsSource = carrito
        listaResumen.ItemsSource = carrito
        AddHandler carrito.CollectionChanged, Sub() ActualizarCarrito()
        lblResumenUsuario.Text = Sesion.NombreUsuario
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

    ''' <summary>Deja el mostrador como recién abierto: sin socio, sin carrito y
    ''' en el paso uno. Se llama al entrar a la página y después de cada préstamo.</summary>
    Private Sub ReiniciarAsistente()
        socioElegido = Nothing
        libroSeleccionado = ""
        carrito.Clear()

        ocupado = True
        txtBuscarSocio.Clear()
        txtBuscarLibro.Clear()
        txtObservacion.Clear()
        cboCategoria.SelectedIndex = -1
        chkForzar.IsChecked = False
        ocupado = False

        listaEjemplares.ItemsSource = Nothing
        lblSinEjemplares.Visibility = Visibility.Visible
        lblTituloEjemplares.Text = "COPIAS DISPONIBLES"

        MostrarSocio()
        CargarSocios()
        IrAPaso(1)
    End Sub

    ' ======================= PASOS =======================

    Private Sub IrAPaso(paso As Integer)
        pasoActual = paso

        panelPaso1.Visibility = If(paso = 1, Visibility.Visible, Visibility.Collapsed)
        panelPaso2.Visibility = If(paso = 2, Visibility.Visible, Visibility.Collapsed)
        panelPaso3.Visibility = If(paso = 3, Visibility.Visible, Visibility.Collapsed)

        PintarPaso(paso1, txtPaso1, paso >= 1)
        PintarPaso(paso2, txtPaso2, paso >= 2)
        PintarPaso(paso3, txtPaso3, paso >= 3)

        ' El botón de confirmar solo existe en el último paso: no se puede
        ' registrar un préstamo sin haber pasado por la revisión.
        btnSiguiente.Visibility = If(paso = 3, Visibility.Collapsed, Visibility.Visible)
        btnConfirmar.Visibility = If(paso = 3, Visibility.Visible, Visibility.Collapsed)

        btnSiguiente.Content = If(paso = 1, "Continuar con los libros", "Revisar y confirmar")

        If paso = 3 Then PrepararResumen()
        TransicionVentana.FundirEntrada(If(paso = 1, CType(panelPaso1, UIElement),
                                           If(paso = 2, CType(panelPaso2, UIElement), panelPaso3)))
    End Sub

    Private Sub PintarPaso(marco As Border, texto As TextBlock, activo As Boolean)
        marco.Style = CType(TryFindResource(If(activo, "PasoAsistenteActivo", "PasoAsistente")), Style)
        texto.Foreground = CType(TryFindResource(If(activo, "BrushBlanco", "BrushTextoSuave")), Media.Brush)
    End Sub

    Private Sub btnSiguiente_Click(sender As Object, e As RoutedEventArgs) Handles btnSiguiente.Click
        If pasoActual = 1 Then
            If socioElegido Is Nothing Then
                Avisar("Elige primero el socio que se lleva los libros.")
                Return
            End If
            CargarLibros()
            IrAPaso(2)

        ElseIf pasoActual = 2 Then
            If carrito.Count = 0 Then
                Avisar("Agrega al menos un ejemplar antes de continuar.")
                Return
            End If
            IrAPaso(3)
        End If
    End Sub

    Private Sub btnVolverPaso1_Click(sender As Object, e As RoutedEventArgs) Handles btnVolverPaso1.Click
        IrAPaso(1)
    End Sub

    Private Sub btnVolverPaso2_Click(sender As Object, e As RoutedEventArgs) Handles btnVolverPaso2.Click
        CargarLibros()
        IrAPaso(2)
    End Sub

    ' ======================= PASO 1: SOCIO =======================

    Private Sub CargarSocios()
        Try
            Dim dt = SocioService.Listar(txtBuscarSocio.Text, soloActivos:=True)
            dgSocios.ItemsSource = dt.DefaultView
            pnlSinSocios.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar los socios", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscarSocio_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarSocio.TextChanged
        If ocupado Then Return
        CargarSocios()
    End Sub

    Private Sub dgSocios_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgSocios.SelectionChanged
        Dim fila = TryCast(dgSocios.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        Try
            socioElegido = SocioService.Resumen(fila("idsocio").ToString())
            MostrarSocio()

            ' Cambiar de socio a mitad de un préstamo vacía el carrito: el cupo y
            ' la solvencia son de la persona, no de los libros.
            If carrito.Count > 0 Then
                carrito.Clear()
                Avisar("Se vació la lista de libros porque cambiaste de socio.")
            End If

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Leer la ficha del socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ''' <summary>Pinta la ficha del socio en la columna derecha, incluida la razón
    ''' del bloqueo si la hay. Decirle al bibliotecario POR QUÉ no puede prestar es
    ''' lo que le permite resolverlo en el momento.</summary>
    Private Sub MostrarSocio()
        If socioElegido Is Nothing Then
            lblSinSocio.Visibility = Visibility.Visible
            pnlSocio.Visibility = Visibility.Collapsed
            Return
        End If

        lblSinSocio.Visibility = Visibility.Collapsed
        pnlSocio.Visibility = Visibility.Visible

        lblSocioNombre.Text = socioElegido.NombreCompleto
        lblSocioDatos.Text = $"{socioElegido.IdSocio} · {socioElegido.TipoSocio} · " &
                             $"{socioElegido.DiasPrestamo} días de plazo"
        lblSocioAfuera.Text = socioElegido.EjemplaresAfuera.ToString()
        lblSocioCupo.Text = socioElegido.CupoDisponible.ToString()
        lblSocioDeuda.Text = Formato.Dinero(socioElegido.MontoAdeudado)

        lblSocioDeuda.Foreground = CType(TryFindResource(
            If(socioElegido.MontoAdeudado > 0, "BrushPeligro", "BrushTexto")), Media.Brush)

        If socioElegido.PuedePrestar Then
            pnlBloqueo.Visibility = Visibility.Collapsed
        Else
            lblBloqueo.Text = socioElegido.MotivoBloqueo
            pnlBloqueo.Visibility = Visibility.Visible
        End If
    End Sub

    ' ======================= PASO 2: LIBROS =======================

    Private Sub CargarLibros()
        Try
            Dim idCategoria As Integer? = Nothing
            If cboCategoria.SelectedValue IsNot Nothing Then idCategoria = CInt(cboCategoria.SelectedValue)

            ' Solo títulos con copias libres: en el mostrador no sirve de nada ver
            ' los que no se pueden entregar.
            Dim dt = LibroService.Listar(txtBuscarLibro.Text, idCategoria, soloDisponibles:=True)
            dgLibros.ItemsSource = dt.DefaultView

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar el catálogo", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscarLibro_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarLibro.TextChanged
        If ocupado Then Return
        CargarLibros()
    End Sub

    Private Sub cboCategoria_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboCategoria.SelectionChanged
        If ocupado Then Return
        CargarLibros()
    End Sub

    Private Sub btnLimpiarLibro_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiarLibro.Click
        ocupado = True
        txtBuscarLibro.Clear()
        cboCategoria.SelectedIndex = -1
        ocupado = False
        CargarLibros()
    End Sub

    Private Sub dgLibros_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgLibros.SelectionChanged
        Dim fila = TryCast(dgLibros.SelectedItem, DataRowView)
        If fila Is Nothing Then
            listaEjemplares.ItemsSource = Nothing
            lblSinEjemplares.Visibility = Visibility.Visible
            Return
        End If

        libroSeleccionado = fila("idlibro").ToString()
        MostrarEjemplares(fila("titulo").ToString())
    End Sub

    ''' <summary>Muestra las copias físicas libres del título elegido. Aquí es donde
    ''' el sistema deja de hablar de "stock" y empieza a hablar de "esta copia,
    ''' la del estante 005-02".</summary>
    Private Sub MostrarEjemplares(titulo As String)
        Try
            Dim dt = LibroService.EjemplaresDisponibles(libroSeleccionado)

            ' Las que ya están en el carrito no se vuelven a ofrecer
            Dim vista As New DataView(dt)
            Dim yaEnCarrito = carrito.Select(Function(c) c.IdEjemplar).ToList()
            If yaEnCarrito.Count > 0 Then
                vista.RowFilter = "idejemplar NOT IN (" & String.Join(",", yaEnCarrito) & ")"
            End If

            listaEjemplares.ItemsSource = vista
            lblTituloEjemplares.Text = $"COPIAS DISPONIBLES — {titulo.ToUpper()}"

            If vista.Count = 0 Then
                lblSinEjemplares.Text = "Todas las copias libres de este título ya están en la lista."
                lblSinEjemplares.Visibility = Visibility.Visible
            Else
                lblSinEjemplares.Visibility = Visibility.Collapsed
            End If

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar los ejemplares del título", ex),
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ''' <summary>Agrega una copia al carrito. Las comprobaciones de cupo se hacen
    ''' aquí para avisar temprano; la definitiva la hace el servicio.</summary>
    Private Sub AgregarEjemplar_Click(sender As Object, e As RoutedEventArgs)
        Dim boton = TryCast(sender, Button)
        Dim fila = TryCast(boton?.Tag, DataRowView)
        If fila Is Nothing OrElse socioElegido Is Nothing Then Return

        Dim idEjemplar = CInt(fila("idejemplar"))
        If carrito.Any(Function(c) c.IdEjemplar = idEjemplar) Then Return

        If Not Permisos.PuedeForzarPrestamo AndAlso carrito.Count >= socioElegido.CupoDisponible Then
            Avisar($"A {socioElegido.NombreCompleto} solo le quedan {socioElegido.CupoDisponible} " &
                   $"ejemplares de cupo como {socioElegido.TipoSocio}.")
            Return
        End If

        Dim libro = TryCast(dgLibros.SelectedItem, DataRowView)

        carrito.Add(New EjemplarElegido With {
            .IdEjemplar = idEjemplar,
            .CodigoBarras = fila("codigo_barras").ToString(),
            .IdLibro = libroSeleccionado,
            .Titulo = If(libro IsNot Nothing, libro("titulo").ToString(), ""),
            .Autor = If(libro IsNot Nothing, libro("autor").ToString(), ""),
            .Ubicacion = If(IsDBNull(fila("ubicacion")), "Sin ubicación", fila("ubicacion").ToString()),
            .Condicion = If(IsDBNull(fila("condicion")), "Bueno", fila("condicion").ToString())
        })

        ' La copia recién agregada desaparece de las opciones y la lista se refresca
        If libro IsNot Nothing Then MostrarEjemplares(libro("titulo").ToString())
        CargarLibros()
    End Sub

    Private Sub QuitarEjemplar_Click(sender As Object, e As RoutedEventArgs)
        Dim boton = TryCast(sender, Button)
        Dim ejemplar = TryCast(boton?.Tag, EjemplarElegido)
        If ejemplar Is Nothing Then Return

        carrito.Remove(ejemplar)

        ' Si se quitó una copia del título que está a la vista, vuelve a ofrecerse
        If pasoActual = 2 AndAlso ejemplar.IdLibro = libroSeleccionado Then
            MostrarEjemplares(ejemplar.Titulo)
            CargarLibros()
        End If

        ' Quitar el último ejemplar deja el paso 3 sin sentido
        If carrito.Count = 0 AndAlso pasoActual = 3 Then IrAPaso(2)
    End Sub

    Private Sub btnVaciar_Click(sender As Object, e As RoutedEventArgs) Handles btnVaciar.Click
        If carrito.Count = 0 Then Return
        carrito.Clear()

        If pasoActual = 3 Then
            IrAPaso(2)
        ElseIf pasoActual = 2 AndAlso libroSeleccionado <> "" Then
            Dim libro = TryCast(dgLibros.SelectedItem, DataRowView)
            If libro IsNot Nothing Then MostrarEjemplares(libro("titulo").ToString())
            CargarLibros()
        End If
    End Sub

    Private Sub ActualizarCarrito()
        pnlCarritoVacio.Visibility = If(carrito.Count = 0, Visibility.Visible, Visibility.Collapsed)

        If carrito.Count = 0 Then
            lblContadorCarrito.Text = "Sin ejemplares agregados"
        ElseIf socioElegido IsNot Nothing Then
            lblContadorCarrito.Text = $"{carrito.Count} de {socioElegido.CupoDisponible} de cupo disponible"
        Else
            lblContadorCarrito.Text = $"{carrito.Count} ejemplares"
        End If
    End Sub

    ' ======================= PASO 3: CONFIRMAR =======================

    Private Sub PrepararResumen()
        If socioElegido Is Nothing Then Return

        lblResumenSocio.Text = socioElegido.NombreCompleto
        lblResumenTipo.Text = $"{socioElegido.IdSocio} · {socioElegido.TipoSocio}"
        lblResumenFecha.Text = Formato.FechaHora(DateTime.Now)

        ' El plazo lo propone el tipo de socio; el bibliotecario puede cambiarlo
        If Not dtpVencimiento.SelectedDate.HasValue Then
            dtpVencimiento.SelectedDate = Date.Today.AddDays(socioElegido.DiasPrestamo)
        End If
        lblPlazoSugerido.Text = $"Un {socioElegido.TipoSocio.ToLower()} tiene " &
                                $"{socioElegido.DiasPrestamo} días de plazo."
        ActualizarVencimiento()

        ' El aviso de autorización solo aparece si de verdad hay un bloqueo
        If socioElegido.PuedePrestar Then
            pnlForzar.Visibility = Visibility.Collapsed
        Else
            lblMotivoBloqueo.Text = socioElegido.MotivoBloqueo
            pnlForzar.Visibility = Visibility.Visible
            chkForzar.IsEnabled = Permisos.PuedeForzarPrestamo
            lblSoloAdmin.Visibility = If(Permisos.PuedeForzarPrestamo,
                                         Visibility.Collapsed, Visibility.Visible)
        End If
    End Sub

    Private Sub dtpVencimiento_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dtpVencimiento.SelectedDateChanged
        ActualizarVencimiento()
    End Sub

    Private Sub ActualizarVencimiento()
        If dtpVencimiento.SelectedDate.HasValue Then
            lblResumenVence.Text = Formato.Fecha(dtpVencimiento.SelectedDate.Value)
        Else
            lblResumenVence.Text = "—"
        End If
    End Sub

    Private Sub btnConfirmar_Click(sender As Object, e As RoutedEventArgs) Handles btnConfirmar.Click
        If socioElegido Is Nothing Then
            Avisar("Elige el socio antes de registrar el préstamo.")
            Return
        End If
        If carrito.Count = 0 Then
            Avisar("Agrega al menos un ejemplar.")
            Return
        End If
        If Not dtpVencimiento.SelectedDate.HasValue Then
            Avisar("Elige la fecha de devolución.")
            Return
        End If

        Try
            btnConfirmar.IsEnabled = False
            btnConfirmar.Content = "Registrando…"

            Dim resultado As ResultadoPrestamo = Nothing
            Dim problema = PrestamoService.Registrar(socioElegido.IdSocio, carrito.ToList(),
                                                     dtpVencimiento.SelectedDate.Value,
                                                     txtObservacion.Text,
                                                     chkForzar.IsChecked = True,
                                                     resultado)

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo registrar el préstamo",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                ' El catálogo pudo cambiar mientras se armaba el préstamo
                CargarLibros()
                Return
            End If

            ' El comprobante se arma con el carrito todavía lleno, antes de vaciarlo.
            ' La variable no se llama `comprobante` porque VB no distingue mayúsculas
            ' y le haría sombra a la clase Comprobante.
            Dim boleta = Comprobante.Prestamo(resultado, carrito)

            DialogoBiblioteca.MostrarComprobante(
                $"{resultado.Socio} se lleva {resultado.Ejemplares} " &
                If(resultado.Ejemplares = 1, "ejemplar", "ejemplares") &
                $". Debe devolverlos el {Formato.Fecha(resultado.FechaVencimiento)}.",
                "Préstamo registrado con éxito", resultado.Codigo, boleta)

            ReiniciarAsistente()

            Dim principal = TryCast(Window.GetWindow(Me), MainWindow)
            If principal IsNot Nothing Then principal.RevisarMora()

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Registrar el préstamo", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        Finally
            btnConfirmar.IsEnabled = True
            btnConfirmar.Content = "📖  Registrar préstamo"
        End Try
    End Sub

    Private Shared Sub Avisar(mensaje As String)
        DialogoBiblioteca.Show(mensaje, "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning)
    End Sub
End Class
