Imports System.Data

''' <summary>Los catálogos de autoridad —autores, editoriales, categorías— y la
''' política de préstamo. Las cuatro pestañas siguen el mismo patrón: lista a la
''' izquierda, formulario a la derecha, y guardar decide entre alta y edición
''' según si hay algo seleccionado.</summary>
Public Class CatalogosPage

    Private preparado As Boolean = False
    Private idAutor As Integer = 0
    Private idEditorial As Integer = 0
    Private idCategoria As Integer = 0
    Private idTipo As Integer = 0

    ' ======================= CICLO DE VIDA =======================

    Public Sub Cargar()
        Preparar()
        CargarAutores()
        CargarEditoriales()
        CargarCategorias()
        CargarTipos()
    End Sub

    Private Sub CatalogosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True

        ' Un bibliotecario consulta estos catálogos pero no los cambia: el acervo
        ' y la política de préstamo son decisiones de la dirección.
        If Not Permisos.PuedeEditarCatalogos Then
            pnlAvisoPermiso.Visibility = Visibility.Visible

            For Each boton In {btnAutorGuardar, btnAutorEliminar, btnAutorNuevo,
                               btnEditorialGuardar, btnEditorialEliminar, btnEditorialNuevo,
                               btnCategoriaGuardar, btnCategoriaEliminar, btnCategoriaNuevo,
                               btnTipoGuardar, btnTipoEliminar, btnTipoNuevo}
                boton.IsEnabled = False
            Next
        End If
    End Sub

    ' ======================= AUTORES =======================

    Private Sub CargarAutores()
        Try
            dgAutores.ItemsSource = CatalogoService.ListarAutores(txtBuscarAutor.Text).DefaultView
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar los autores", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscarAutor_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarAutor.TextChanged
        CargarAutores()
    End Sub

    Private Sub dgAutores_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgAutores.SelectionChanged
        Dim vista = TryCast(dgAutores.SelectedItem, DataRowView)
        If vista Is Nothing Then Return

        idAutor = Db.Numero(vista.Row, "idautor")
        txtAutorNombre.Text = Db.Texto(vista.Row, "nombre")
        txtAutorPais.Text = Db.Texto(vista.Row, "nacionalidad")
        lblFormAutor.Text = "Editar autor"
    End Sub

    Private Sub btnAutorNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnAutorNuevo.Click
        dgAutores.SelectedItem = Nothing
        idAutor = 0
        txtAutorNombre.Clear()
        txtAutorPais.Clear()
        lblFormAutor.Text = "Nuevo autor"
    End Sub

    Private Sub btnAutorGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnAutorGuardar.Click
        Try
            Dim problema = CatalogoService.GuardarAutor(idAutor, txtAutorNombre.Text, txtAutorPais.Text)
            If Aviso(problema, "guardar el autor") Then Return

            CargarAutores()
            btnAutorNuevo_Click(Nothing, Nothing)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Guardar el autor", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnAutorEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnAutorEliminar.Click
        If idAutor = 0 Then
            Avisar("Selecciona primero un autor de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar al autor «{txtAutorNombre.Text.Trim()}»?") Then Return

        Try
            Dim problema = CatalogoService.EliminarAutor(idAutor)
            If Aviso(problema, "eliminar el autor") Then Return

            CargarAutores()
            btnAutorNuevo_Click(Nothing, Nothing)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Eliminar el autor", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ======================= EDITORIALES =======================

    Private Sub CargarEditoriales()
        Try
            dgEditoriales.ItemsSource = CatalogoService.ListarEditoriales(txtBuscarEditorial.Text).DefaultView
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar las editoriales", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscarEditorial_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarEditorial.TextChanged
        CargarEditoriales()
    End Sub

    Private Sub dgEditoriales_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgEditoriales.SelectionChanged
        Dim vista = TryCast(dgEditoriales.SelectedItem, DataRowView)
        If vista Is Nothing Then Return

        idEditorial = Db.Numero(vista.Row, "ideditorial")
        txtEditorialNombre.Text = Db.Texto(vista.Row, "nombre")
        txtEditorialPais.Text = Db.Texto(vista.Row, "pais")
        lblFormEditorial.Text = "Editar editorial"
    End Sub

    Private Sub btnEditorialNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnEditorialNuevo.Click
        dgEditoriales.SelectedItem = Nothing
        idEditorial = 0
        txtEditorialNombre.Clear()
        txtEditorialPais.Clear()
        lblFormEditorial.Text = "Nueva editorial"
    End Sub

    Private Sub btnEditorialGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnEditorialGuardar.Click
        Try
            Dim problema = CatalogoService.GuardarEditorial(idEditorial, txtEditorialNombre.Text,
                                                            txtEditorialPais.Text)
            If Aviso(problema, "guardar la editorial") Then Return

            CargarEditoriales()
            btnEditorialNuevo_Click(Nothing, Nothing)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Guardar la editorial", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnEditorialEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEditorialEliminar.Click
        If idEditorial = 0 Then
            Avisar("Selecciona primero una editorial de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar la editorial «{txtEditorialNombre.Text.Trim()}»?") Then Return

        Try
            Dim problema = CatalogoService.EliminarEditorial(idEditorial)
            If Aviso(problema, "eliminar la editorial") Then Return

            CargarEditoriales()
            btnEditorialNuevo_Click(Nothing, Nothing)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Eliminar la editorial", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ======================= CATEGORÍAS =======================

    Private Sub CargarCategorias()
        Try
            dgCategorias.ItemsSource = CatalogoService.ListarCategorias(txtBuscarCategoria.Text).DefaultView
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar las categorías", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscarCategoria_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtBuscarCategoria.TextChanged
        CargarCategorias()
    End Sub

    Private Sub dgCategorias_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgCategorias.SelectionChanged
        Dim vista = TryCast(dgCategorias.SelectedItem, DataRowView)
        If vista Is Nothing Then Return

        idCategoria = Db.Numero(vista.Row, "idcategoria")
        txtCategoriaNombre.Text = Db.Texto(vista.Row, "nombre")
        txtCategoriaDewey.Text = Db.Texto(vista.Row, "codigo_dewey")
        txtCategoriaDescripcion.Text = Db.Texto(vista.Row, "descripcion")
        lblFormCategoria.Text = "Editar categoría"
    End Sub

    Private Sub btnCategoriaNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnCategoriaNuevo.Click
        dgCategorias.SelectedItem = Nothing
        idCategoria = 0
        txtCategoriaNombre.Clear()
        txtCategoriaDewey.Clear()
        txtCategoriaDescripcion.Clear()
        lblFormCategoria.Text = "Nueva categoría"
    End Sub

    Private Sub btnCategoriaGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnCategoriaGuardar.Click
        Try
            Dim problema = CatalogoService.GuardarCategoria(idCategoria, txtCategoriaNombre.Text,
                                                            txtCategoriaDewey.Text,
                                                            txtCategoriaDescripcion.Text)
            If Aviso(problema, "guardar la categoría") Then Return

            CargarCategorias()
            btnCategoriaNuevo_Click(Nothing, Nothing)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Guardar la categoría", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnCategoriaEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnCategoriaEliminar.Click
        If idCategoria = 0 Then
            Avisar("Selecciona primero una categoría de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar la categoría «{txtCategoriaNombre.Text.Trim()}»?") Then Return

        Try
            Dim problema = CatalogoService.EliminarCategoria(idCategoria)
            If Aviso(problema, "eliminar la categoría") Then Return

            CargarCategorias()
            btnCategoriaNuevo_Click(Nothing, Nothing)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Eliminar la categoría", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ======================= TIPOS DE SOCIO =======================

    Private Sub CargarTipos()
        Try
            dgTipos.ItemsSource = CatalogoService.ListarTiposSocio().DefaultView
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar los tipos de socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub dgTipos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgTipos.SelectionChanged
        Dim vista = TryCast(dgTipos.SelectedItem, DataRowView)
        If vista Is Nothing Then Return

        idTipo = Db.Numero(vista.Row, "idtipo")
        txtTipoNombre.Text = Db.Texto(vista.Row, "nombre")
        txtTipoMax.Text = Db.Numero(vista.Row, "max_prestamos").ToString()
        txtTipoDias.Text = Db.Numero(vista.Row, "dias_prestamo").ToString()
        txtTipoMulta.Text = Db.Monto(vista.Row, "multa_diaria").ToString("0.00")
        lblFormTipo.Text = "Editar tipo de socio"
        MostrarEjemploMulta()
    End Sub

    ''' <summary>Traduce la tarifa a un caso concreto. "L 5.00 por día" no dice
    ''' mucho; "una semana tarde son L 35.00" sí.</summary>
    Private Sub MostrarEjemploMulta()
        Dim multa As Decimal
        If Not Decimal.TryParse(txtTipoMulta.Text.Trim(), multa) OrElse multa <= 0 Then
            lblEjemploMulta.Text = ""
            Return
        End If

        lblEjemploMulta.Text = $"Un ejemplar devuelto una semana tarde costaría " &
                               $"{Formato.Dinero(multa * 7)}."
    End Sub

    Private Sub txtTipoMulta_TextChanged(sender As Object, e As TextChangedEventArgs) _
        Handles txtTipoMulta.TextChanged
        MostrarEjemploMulta()
    End Sub

    Private Sub btnTipoNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnTipoNuevo.Click
        dgTipos.SelectedItem = Nothing
        idTipo = 0
        txtTipoNombre.Clear()
        txtTipoMax.Clear()
        txtTipoDias.Clear()
        txtTipoMulta.Clear()
        lblEjemploMulta.Text = ""
        lblFormTipo.Text = "Nuevo tipo de socio"
    End Sub

    Private Sub btnTipoGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnTipoGuardar.Click
        Dim maximo As Integer, dias As Integer
        Dim multa As Decimal

        If Not Integer.TryParse(txtTipoMax.Text.Trim(), maximo) Then
            Avisar("El máximo de ejemplares debe ser un número.")
            Return
        End If
        If Not Integer.TryParse(txtTipoDias.Text.Trim(), dias) Then
            Avisar("Los días de plazo deben ser un número.")
            Return
        End If
        If Not Decimal.TryParse(txtTipoMulta.Text.Trim(), multa) Then
            Avisar("La multa diaria debe ser un número.")
            Return
        End If

        Try
            Dim problema = CatalogoService.GuardarTipoSocio(idTipo, txtTipoNombre.Text, maximo, dias, multa)
            If Aviso(problema, "guardar el tipo de socio") Then Return

            CargarTipos()
            btnTipoNuevo_Click(Nothing, Nothing)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Guardar el tipo de socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnTipoEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnTipoEliminar.Click
        If idTipo = 0 Then
            Avisar("Selecciona primero un tipo de socio de la lista.")
            Return
        End If
        If Not Confirmar($"¿Eliminar el tipo de socio «{txtTipoNombre.Text.Trim()}»?") Then Return

        Try
            Dim problema = CatalogoService.EliminarTipoSocio(idTipo)
            If Aviso(problema, "eliminar el tipo de socio") Then Return

            CargarTipos()
            btnTipoNuevo_Click(Nothing, Nothing)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Eliminar el tipo de socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ======================= AUXILIARES =======================

    ''' <summary>Muestra el problema si lo hay y devuelve True cuando hay que
    ''' detenerse. Evita repetir el mismo If en las doce acciones de la página.</summary>
    Private Shared Function Aviso(problema As String, accion As String) As Boolean
        If problema Is Nothing Then Return False
        DialogoBiblioteca.Show(problema, $"No se pudo {accion}",
                               MessageBoxButton.OK, MessageBoxImage.Warning)
        Return True
    End Function

    Private Shared Function Confirmar(pregunta As String) As Boolean
        Return DialogoBiblioteca.Show(pregunta & " Esta acción no se puede deshacer.", "Confirmar",
                                      MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes
    End Function

    Private Shared Sub Avisar(mensaje As String)
        DialogoBiblioteca.Show(mensaje, "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning)
    End Sub
End Class
