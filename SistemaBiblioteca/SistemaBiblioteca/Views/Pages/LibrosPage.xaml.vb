Imports System.Data
Imports Microsoft.Win32

''' <summary>Catalogación: la ficha bibliográfica de cada obra y el inventario de
''' sus copias físicas. Arriba los títulos, abajo las copias del título elegido y
''' a la derecha el formulario de la ficha.</summary>
Public Class LibrosPage

    Private preparado As Boolean = False
    Private ocupado As Boolean = False
    ''' <summary>Código del libro que se está editando. Vacío = se está creando uno.</summary>
    Private editandoId As String = ""

    ' ======================= CICLO DE VIDA =======================

    Public Sub Cargar()
        Preparar()
        CargarCombos()
        LimpiarFormulario()
        CargarLista()
    End Sub

    Private Sub LibrosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True

        ocupado = True
        cboIdioma.ItemsSource = CatalogoService.IdiomasParaCombo()
        ocupado = False

        btnEliminar.IsEnabled = Permisos.PuedeEliminar
        lblSinPermiso.Visibility = If(Permisos.PuedeEliminar, Visibility.Collapsed, Visibility.Visible)
    End Sub

    Private Sub CargarCombos()
        Try
            ocupado = True

            Dim autor = cboAutor.SelectedValue
            Dim editorial = cboEditorial.SelectedValue
            Dim categoria = cboCategoria.SelectedValue
            Dim filtro = cboFiltroCategoria.SelectedValue

            cboAutor.ItemsSource = CatalogoService.AutoresParaCombo().DefaultView
            cboEditorial.ItemsSource = CatalogoService.EditorialesParaCombo().DefaultView
            cboCategoria.ItemsSource = CatalogoService.CategoriasParaCombo().DefaultView
            cboFiltroCategoria.ItemsSource = CatalogoService.CategoriasParaCombo().DefaultView

            cboAutor.SelectedValue = autor
            cboEditorial.SelectedValue = editorial
            cboCategoria.SelectedValue = categoria
            cboFiltroCategoria.SelectedValue = filtro

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cargar los catálogos", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        Finally
            ocupado = False
        End Try
    End Sub

    ' ======================= LISTA =======================

    Private Sub CargarLista()
        Try
            Dim idCategoria As Integer? = Nothing
            If cboFiltroCategoria.SelectedValue IsNot Nothing Then
                idCategoria = CInt(cboFiltroCategoria.SelectedValue)
            End If

            Dim dt = LibroService.Listar(txtBuscar.Text, idCategoria)
            dgLibros.ItemsSource = dt.DefaultView
            pnlVacio.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

            Dim copias = 0, libres = 0
            For Each fila As DataRow In dt.Rows
                copias += Db.Numero(fila, "total_ejemplares")
                libres += Db.Numero(fila, "disponibles")
            Next
            lblResumen.Text = $"{dt.Rows.Count} títulos · {copias} copias · {libres} libres"

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar el catálogo", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub cboFiltroCategoria_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboFiltroCategoria.SelectionChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        ocupado = True
        txtBuscar.Clear()
        cboFiltroCategoria.SelectedIndex = -1
        ocupado = False
        CargarLista()
    End Sub

    ' ======================= FORMULARIO =======================

    Private Sub dgLibros_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgLibros.SelectionChanged
        Dim vista = TryCast(dgLibros.SelectedItem, DataRowView)
        If vista Is Nothing Then Return

        Dim fila = vista.Row
        editandoId = Db.Texto(fila, "idlibro")

        txtCodigo.Text = editandoId
        txtCodigo.IsEnabled = False        ' el código es la llave: no se cambia
        txtTitulo.Text = Db.Texto(fila, "titulo")
        txtIsbn.Text = Db.Texto(fila, "isbn")
        cboAutor.SelectedValue = Db.Numero(fila, "idautor")

        If IsDBNull(fila("ideditorial")) Then
            cboEditorial.SelectedIndex = -1
        Else
            cboEditorial.SelectedValue = Db.Numero(fila, "ideditorial")
        End If

        cboCategoria.SelectedValue = Db.Numero(fila, "idcategoria")

        Dim anio = Db.Numero(fila, "anio_publicacion")
        txtAnio.Text = If(anio > 0, anio.ToString(), "")
        txtEdicion.Text = Db.Texto(fila, "edicion")
        cboIdioma.Text = Db.Texto(fila, "idioma")
        txtSinopsis.Text = Db.Texto(fila, "sinopsis")

        lblTituloFormulario.Text = "Editar título"
        lblSubtituloFormulario.Text = $"{editandoId} · {Db.Texto(fila, "titulo")}"

        MostrarPortada(editandoId)
        CargarEjemplares(Db.Texto(fila, "titulo"))
    End Sub

    ' ======================= PORTADA =======================

    Private Sub MostrarPortada(codigo As String)
        Dim bmp = Portada.Cargar(codigo)
        imgPortada.Source = bmp
        lblSinPortada.Visibility = If(bmp Is Nothing, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Sub btnPortada_Click(sender As Object, e As RoutedEventArgs) Handles btnPortada.Click
        If String.IsNullOrWhiteSpace(txtCodigo.Text) Then
            Avisar("Primero escribe o sugiere el código del libro.")
            Return
        End If

        Dim dialogo As New OpenFileDialog With {
            .Filter = "Imágenes|*.jpg;*.jpeg;*.png",
            .Title = "Seleccionar portada del libro"
        }
        If dialogo.ShowDialog() <> True Then Return

        Try
            Portada.Guardar(txtCodigo.Text, dialogo.FileName)
            MostrarPortada(txtCodigo.Text)
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("No se pudo copiar la portada", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Async Sub btnPortadaIsbn_Click(sender As Object, e As RoutedEventArgs) Handles btnPortadaIsbn.Click
        If String.IsNullOrWhiteSpace(txtCodigo.Text) Then
            Avisar("Primero escribe o sugiere el código del libro.")
            Return
        End If
        If String.IsNullOrWhiteSpace(txtIsbn.Text) Then
            Avisar("Este título no tiene ISBN escrito, así que no hay nada que buscar.")
            Return
        End If

        btnPortadaIsbn.IsEnabled = False
        Try
            Dim encontrada = Await Portada.DescargarPorIsbnAsync(txtCodigo.Text, txtIsbn.Text)
            If encontrada Then
                MostrarPortada(txtCodigo.Text)
            Else
                DialogoBiblioteca.Show("No se encontró portada para ese ISBN en Open Library. " &
                                       "Pasa seguido con ediciones locales o poco conocidas.",
                                       "Sin resultados", MessageBoxButton.OK, MessageBoxImage.Information)
            End If
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Buscar la portada por ISBN", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        Finally
            btnPortadaIsbn.IsEnabled = True
        End Try
    End Sub

    Private Sub btnSugerir_Click(sender As Object, e As RoutedEventArgs) Handles btnSugerir.Click
        Try
            txtCodigo.Text = LibroService.SugerirCodigo()
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Sugerir el código del libro", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevo.Click
        dgLibros.SelectedItem = Nothing
        LimpiarFormulario()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        If String.IsNullOrWhiteSpace(txtTitulo.Text) Then
            Avisar("Escribe el título del libro.")
            Return
        End If
        If cboAutor.SelectedValue Is Nothing Then
            Avisar("Elige el autor. Si no está en la lista, agrégalo en 'Autores y categorías'.")
            Return
        End If
        If cboCategoria.SelectedValue Is Nothing Then
            Avisar("Elige la categoría del libro.")
            Return
        End If

        Dim anio As Integer? = Nothing
        If Not String.IsNullOrWhiteSpace(txtAnio.Text) Then
            Dim valor As Integer
            If Not Integer.TryParse(txtAnio.Text.Trim(), valor) Then
                Avisar("El año de publicación debe ser un número.")
                Return
            End If
            anio = valor
        End If

        Dim idEditorial As Integer? = Nothing
        If cboEditorial.SelectedValue IsNot Nothing Then idEditorial = CInt(cboEditorial.SelectedValue)

        Try
            Dim problema As String
            Dim codigo = txtCodigo.Text.Trim().ToUpper()

            If editandoId = "" Then
                problema = LibroService.Crear(codigo, txtIsbn.Text, txtTitulo.Text,
                                              CInt(cboAutor.SelectedValue), idEditorial,
                                              CInt(cboCategoria.SelectedValue), anio,
                                              txtEdicion.Text, cboIdioma.Text, txtSinopsis.Text)
            Else
                problema = LibroService.Actualizar(editandoId, txtIsbn.Text, txtTitulo.Text,
                                                   CInt(cboAutor.SelectedValue), idEditorial,
                                                   CInt(cboCategoria.SelectedValue), anio,
                                                   txtEdicion.Text, cboIdioma.Text, txtSinopsis.Text)
            End If

            ' El formulario se conserva tal cual para que el usuario corrija el dato señalado
            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo guardar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim titulo = txtTitulo.Text.Trim()
            Dim eraNuevo = editandoId = ""

            CargarLista()
            dgLibros.SelectedItem = Nothing
            LimpiarFormulario()

            If eraNuevo Then
                DialogoBiblioteca.Show($"«{titulo}» quedó catalogado con el código {codigo}. " &
                                       "Ahora agrégale sus copias físicas desde el panel de abajo.",
                                       "Título catalogado con éxito", MessageBoxButton.OK,
                                       MessageBoxImage.Information)
            Else
                DialogoBiblioteca.Show($"La ficha de «{titulo}» quedó actualizada.",
                                       "Guardado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            End If

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Guardar el título", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminar.Click
        If editandoId = "" Then
            Avisar("Selecciona primero un título de la lista.")
            Return
        End If

        Dim titulo = txtTitulo.Text.Trim()
        If DialogoBiblioteca.Show($"¿Eliminar «{titulo}» del catálogo? Esta acción no se puede deshacer.",
                                  "Confirmar", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = LibroService.Eliminar(editandoId)
            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo eliminar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            dgLibros.SelectedItem = Nothing
            LimpiarFormulario()
            DialogoBiblioteca.Show($"«{titulo}» se quitó del catálogo.", "Eliminado con éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Eliminar el título", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editandoId = ""
        ocupado = True
        imgPortada.Source = Nothing
        lblSinPortada.Visibility = Visibility.Visible
        txtCodigo.Clear()
        txtCodigo.IsEnabled = True
        txtTitulo.Clear()
        txtIsbn.Clear()
        txtAnio.Clear()
        txtEdicion.Clear()
        txtSinopsis.Clear()
        cboAutor.SelectedIndex = -1
        cboEditorial.SelectedIndex = -1
        cboCategoria.SelectedIndex = -1
        cboIdioma.Text = "Español"
        ocupado = False

        lblTituloFormulario.Text = "Catalogar un título"
        lblSubtituloFormulario.Text = "Ficha bibliográfica de la obra"

        dgEjemplares.ItemsSource = Nothing
        lblTituloCopias.Text = "Copias físicas"
        lblSubtituloCopias.Text = "Selecciona un título de la lista"
    End Sub

    ' ======================= EJEMPLARES =======================

    Private Sub CargarEjemplares(titulo As String)
        Try
            Dim dt = LibroService.ListarEjemplares(editandoId)
            dgEjemplares.ItemsSource = dt.DefaultView

            lblTituloCopias.Text = $"Copias físicas de «{titulo}»"
            lblSubtituloCopias.Text = If(dt.Rows.Count = 0,
                                         "Este título todavía no tiene copias registradas",
                                         $"{dt.Rows.Count} copias en el inventario")

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar los ejemplares", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnAgregarEjemplares_Click(sender As Object, e As RoutedEventArgs) _
        Handles btnAgregarEjemplares.Click
        If editandoId = "" Then
            Avisar("Selecciona primero el título al que le vas a agregar copias.")
            Return
        End If

        Dim cantidad As Integer
        If Not Integer.TryParse(txtCantidad.Text.Trim(), cantidad) OrElse cantidad < 1 Then
            Avisar("Escribe cuántas copias vas a agregar (un número mayor que cero).")
            Return
        End If

        Try
            Dim problema = LibroService.AgregarEjemplares(editandoId, cantidad,
                                                          txtUbicacion.Text, "Nuevo")
            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudieron agregar las copias",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarEjemplares(txtTitulo.Text.Trim())
            CargarLista()
            txtCantidad.Text = "1"

            DialogoBiblioteca.Show($"Se agregaron {cantidad} copias con su código de barras.",
                                   "Copias agregadas con éxito", MessageBoxButton.OK,
                                   MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Agregar ejemplares", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub EnviarAReparacion_Click(sender As Object, e As RoutedEventArgs)
        CambiarEstado(sender, "Reparación", "Deteriorado")
    End Sub

    Private Sub VolverADisponible_Click(sender As Object, e As RoutedEventArgs)
        CambiarEstado(sender, "Disponible", "Bueno")
    End Sub

    Private Sub DarDeBaja_Click(sender As Object, e As RoutedEventArgs)
        Dim boton = TryCast(sender, Button)
        Dim fila = TryCast(boton?.Tag, DataRowView)
        If fila Is Nothing Then Return

        If DialogoBiblioteca.Show(
            $"¿Dar de baja el ejemplar {fila("codigo_barras")}? Sale de circulación y solo se " &
            "podrá vender, no prestar.", "Confirmar baja",
            MessageBoxButton.YesNo, MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        CambiarEstado(sender, "Baja", fila("condicion").ToString())
    End Sub

    ''' <summary>Abre la ventana de venta para un ejemplar dado de baja. El precio
    ''' y el comprador se piden ahí; aquí solo se decide si corresponde abrirla.</summary>
    Private Sub Vender_Click(sender As Object, e As RoutedEventArgs)
        Dim boton = TryCast(sender, Button)
        Dim fila = TryCast(boton?.Tag, DataRowView)
        If fila Is Nothing Then Return

        If fila("estado").ToString() <> "Baja" Then
            Avisar("Solo se pueden vender ejemplares dados de baja. Dale de baja primero con el botón 📤 Baja.")
            Return
        End If

        Dim ventana As New VenderEjemplarWindow With {
            .Owner = Window.GetWindow(Me),
            .IdEjemplar = CInt(fila("idejemplar")),
            .Titulo = fila("titulo").ToString(),
            .CodigoBarras = fila("codigo_barras").ToString()
        }
        If ventana.ShowDialog() <> True Then Return

        CargarEjemplares(txtTitulo.Text.Trim())
        CargarLista()
        DialogoBiblioteca.Show($"Ejemplar {fila("codigo_barras")} vendido.", "Venta registrada con éxito",
                               MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Private Sub CambiarEstado(sender As Object, estado As String, condicion As String)
        Dim boton = TryCast(sender, Button)
        Dim fila = TryCast(boton?.Tag, DataRowView)
        If fila Is Nothing Then Return

        Try
            Dim problema = LibroService.CambiarEstadoEjemplar(CInt(fila("idejemplar")), estado, condicion)
            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo cambiar el estado",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarEjemplares(txtTitulo.Text.Trim())
            CargarLista()

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cambiar el estado del ejemplar", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Shared Sub Avisar(mensaje As String)
        DialogoBiblioteca.Show(mensaje, "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning)
    End Sub
End Class
