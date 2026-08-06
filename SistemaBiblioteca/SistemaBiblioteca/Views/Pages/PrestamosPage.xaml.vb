Imports System.Collections.ObjectModel
Imports System.Data

''' <summary>Préstamos: consultarlos, devolverlos, renovarlos y cancelarlos.
''' A la izquierda la lista con sus filtros; a la derecha el préstamo elegido con
''' sus ejemplares, cada uno con su casilla y el estado en que vuelve.</summary>
Public Class PrestamosPage

    ''' <summary>Los estados en que puede volver un ejemplar. Expuesta como
    ''' propiedad porque cada fila de la lista enlaza su combo contra ella.</summary>
    Public ReadOnly Property CondicionesDevolucion As String() =
        {"Nuevo", "Bueno", "Regular", "Deteriorado", "Extraviado"}

    Private ReadOnly renglones As New ObservableCollection(Of LineaDevolucion)

    Private preparado As Boolean = False
    Private ocupado As Boolean = False
    Private idPrestamoActual As Integer = 0
    Private situacionActual As String = ""
    Private moraDiaria As Decimal = 0

    ' ======================= CICLO DE VIDA =======================

    ''' <summary>Se puede entrar con la pantalla ya filtrada: el panel manda aquí
    ''' con `situacion = "Vencido"` y el buscador global con el folio escrito.
    ''' Los filtros se aplican antes de consultar para que la lista abra
    ''' directamente en lo que se venía a ver.</summary>
    Public Sub Cargar(Optional situacion As String = Nothing,
                      Optional filtro As String = Nothing)
        Preparar()

        If situacion IsNot Nothing OrElse filtro IsNot Nothing Then
            ocupado = True
            If situacion IsNot Nothing Then cboSituacion.SelectedItem = situacion
            If filtro IsNot Nothing Then
                txtBuscar.Text = filtro
                ' Al buscar un folio concreto el semáforo estorba: podría estar
                ' devuelto y quedar fuera del filtro anterior.
                cboSituacion.SelectedIndex = -1
            End If
            ocupado = False
        End If

        LimpiarDetalle()
        CargarLista()
    End Sub

    Private Sub PrestamosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True

        Me.DataContext = Me
        ocupado = True
        cboSituacion.ItemsSource = PrestamoService.Situaciones
        ocupado = False

        listaEjemplares.ItemsSource = renglones
        btnCancelar.IsEnabled = Permisos.PuedeEliminar
        lblSinPermiso.Visibility = If(Permisos.PuedeEliminar, Visibility.Collapsed, Visibility.Visible)
    End Sub

    ' ======================= LISTA =======================

    Private Sub CargarLista()
        Try
            Dim dt = PrestamoService.Listar(txtBuscar.Text,
                                            TryCast(cboSituacion.SelectedItem, String),
                                            dtpDesde.SelectedDate)
            dgPrestamos.ItemsSource = dt.DefaultView
            pnlVacio.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            ActualizarIndicadores(dt)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar los préstamos", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ActualizarIndicadores(dt As DataTable)
        Dim activos = 0, vencidos = 0, afuera = 0

        For Each fila As DataRow In dt.Rows
            Dim situacion = Db.Texto(fila, "situacion")
            If situacion <> "Devuelto" AndAlso situacion <> "Cancelado" Then
                activos += 1
                afuera += Db.Numero(fila, "pendientes")
            End If
            If situacion = "Vencido" Then vencidos += 1
        Next

        lblIndTotal.Text = dt.Rows.Count.ToString()
        lblIndActivos.Text = activos.ToString()
        lblIndVencidos.Text = vencidos.ToString()
        lblIndEjemplares.Text = afuera.ToString()
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub cboSituacion_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboSituacion.SelectionChanged
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
        txtBuscar.Clear()
        cboSituacion.SelectedIndex = -1
        dtpDesde.SelectedDate = Nothing
        ocupado = False
        CargarLista()
    End Sub

    ' ======================= DETALLE =======================

    Private Sub dgPrestamos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgPrestamos.SelectionChanged
        Dim fila = TryCast(dgPrestamos.SelectedItem, DataRowView)
        If fila Is Nothing Then
            LimpiarDetalle()
            Return
        End If

        MostrarDetalle(CInt(fila("idprestamo")))
    End Sub

    Private Sub MostrarDetalle(idPrestamo As Integer)
        Try
            Dim cabecera = PrestamoService.Obtener(idPrestamo)
            If cabecera Is Nothing Then
                LimpiarDetalle()
                Return
            End If

            idPrestamoActual = idPrestamo
            situacionActual = Db.Texto(cabecera, "situacion")
            moraDiaria = Db.Monto(cabecera, "multa_diaria")

            pnlSinSeleccion.Visibility = Visibility.Collapsed
            pnlDetalle.Visibility = Visibility.Visible

            lblFolio.Text = Db.Texto(cabecera, "codigo")
            lblSocio.Text = Db.Texto(cabecera, "socio")
            lblSocioDatos.Text = $"{Db.Texto(cabecera, "idsocio")} · {Db.Texto(cabecera, "tipo_socio")}"

            lblSituacion.Text = situacionActual
            lblSituacion.Foreground = EstadoAColorConverter.Trazo(situacionActual)
            pnlSituacion.Background = EstadoAColorFondoConverter.Relleno(situacionActual)

            lblFechaPrestamo.Text = Formato.Fecha(CDate(cabecera("fecha_prestamo")))
            lblFechaVence.Text = Formato.Fecha(CDate(cabecera("fecha_vencimiento")))

            MostrarPlazo(cabecera)
            MostrarMora(cabecera)
            MostrarObservacion(cabecera)
            CargarRenglones(idPrestamo)
            AjustarBotones()

            TransicionVentana.FundirEntrada(pnlDetalle)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Abrir el préstamo", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub MostrarPlazo(cabecera As DataRow)
        If situacionActual = "Devuelto" OrElse situacionActual = "Cancelado" Then
            If IsDBNull(cabecera("fecha_devolucion")) Then
                lblPlazo.Text = "—"
            Else
                lblPlazo.Text = Formato.Fecha(CDate(cabecera("fecha_devolucion")))
            End If
            lblPlazo.Foreground = CType(TryFindResource("BrushTexto"), Media.Brush)
        Else
            Dim restantes = Db.Numero(cabecera, "dias_restantes")
            lblPlazo.Text = Formato.Plazo(restantes)
            lblPlazo.Foreground = EstadoAColorConverter.Trazo(situacionActual)
        End If
    End Sub

    ''' <summary>Muestra cuánto se le cobraría al socio si devolviera hoy. Es el
    ''' número que hay que poder decirle por teléfono sin hacer cuentas a mano.</summary>
    Private Sub MostrarMora(cabecera As DataRow)
        Dim diasRetraso = Db.Numero(cabecera, "dias_retraso")
        Dim pendientes = Db.Numero(cabecera, "pendientes")

        If diasRetraso <= 0 OrElse situacionActual = "Devuelto" OrElse situacionActual = "Cancelado" Then
            pnlMora.Visibility = Visibility.Collapsed
            Return
        End If

        Dim mora = Validador.CalcularMulta(diasRetraso, moraDiaria, pendientes)
        lblMoraTitulo.Text = $"{diasRetraso} días de retraso · {Formato.Dinero(mora)}"
        lblMoraDetalle.Text = $"{pendientes} ejemplares sin devolver × {Formato.Dinero(moraDiaria)} " &
                              $"por día × {diasRetraso} días. La multa se genera al registrar la devolución."
        pnlMora.Visibility = Visibility.Visible
    End Sub

    Private Sub MostrarObservacion(cabecera As DataRow)
        Dim observacion = Db.Texto(cabecera, "observacion")
        Dim renovaciones = Db.Numero(cabecera, "renovaciones")

        Dim notas As New List(Of String)
        If Not String.IsNullOrWhiteSpace(observacion) Then notas.Add(observacion)
        If renovaciones > 0 Then
            notas.Add(If(renovaciones = 1, "Renovado una vez.", $"Renovado {renovaciones} veces."))
        End If

        If notas.Count = 0 Then
            pnlObservacion.Visibility = Visibility.Collapsed
        Else
            lblObservacion.Text = String.Join("  ·  ", notas)
            pnlObservacion.Visibility = Visibility.Visible
        End If
    End Sub

    ''' <summary>Llena la lista de ejemplares. Si el préstamo sigue abierto se
    ''' listan solo los que faltan por volver, con su casilla marcada; si ya está
    ''' cerrado se listan todos, sin poder tocarlos.</summary>
    Private Sub CargarRenglones(idPrestamo As Integer)
        renglones.Clear()

        Dim abierto = situacionActual <> "Devuelto" AndAlso situacionActual <> "Cancelado"
        Dim dt = If(abierto,
                    PrestamoService.RenglonesPendientes(idPrestamo),
                    PrestamoService.Renglones(idPrestamo))

        For Each fila As DataRow In dt.Rows
            renglones.Add(New LineaDevolucion With {
                .IdDetalle = Db.Numero(fila, "iddetalle"),
                .IdEjemplar = Db.Numero(fila, "idejemplar"),
                .CodigoBarras = Db.Texto(fila, "codigo_barras"),
                .Titulo = Db.Texto(fila, "titulo"),
                .Condicion = "Bueno",
                .Marcada = abierto
            })
        Next

        lblTituloEjemplares.Text = If(abierto,
                                      $"EJEMPLARES POR DEVOLVER ({renglones.Count})",
                                      $"EJEMPLARES DEL PRÉSTAMO ({renglones.Count})")
        listaEjemplares.IsEnabled = abierto
        btnMarcarTodos.Visibility = If(abierto, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Sub AjustarBotones()
        Dim abierto = situacionActual <> "Devuelto" AndAlso situacionActual <> "Cancelado"

        btnDevolver.IsEnabled = abierto
        btnRenovar.IsEnabled = abierto AndAlso situacionActual <> "Vencido"
        btnCancelar.IsEnabled = abierto AndAlso Permisos.PuedeEliminar
    End Sub

    Private Sub LimpiarDetalle()
        idPrestamoActual = 0
        situacionActual = ""
        renglones.Clear()
        pnlDetalle.Visibility = Visibility.Collapsed
        pnlSinSeleccion.Visibility = Visibility.Visible
    End Sub

    Private Sub btnMarcarTodos_Click(sender As Object, e As RoutedEventArgs) Handles btnMarcarTodos.Click
        ' Se invierte según lo que haya: si todo estaba marcado, desmarca todo
        Dim marcar = Not renglones.All(Function(r) r.Marcada)
        Dim copia = renglones.ToList()

        renglones.Clear()
        For Each linea In copia
            linea.Marcada = marcar
            renglones.Add(linea)
        Next
    End Sub

    ' ======================= DEVOLUCIÓN =======================

    Private Sub btnDevolver_Click(sender As Object, e As RoutedEventArgs) Handles btnDevolver.Click
        If idPrestamoActual = 0 Then Return

        Dim marcadas = renglones.Where(Function(r) r.Marcada).ToList()
        If marcadas.Count = 0 Then
            Avisar("Marca los ejemplares que el socio está entregando.")
            Return
        End If

        ' Devolver menos de lo que hay afuera es normal, pero conviene confirmarlo
        If marcadas.Count < renglones.Count Then
            Dim faltan = renglones.Count - marcadas.Count
            Dim respuesta = DialogoBiblioteca.Show(
                $"Vas a registrar {marcadas.Count} de {renglones.Count} ejemplares. " &
                $"Quedarán {faltan} sin devolver y el préstamo seguirá abierto. ¿Continuar?",
                "Devolución parcial", MessageBoxButton.YesNo, MessageBoxImage.Question)
            If respuesta <> MessageBoxResult.Yes Then Return
        End If

        ' Un extravío tiene consecuencia económica: se confirma aparte.
        ' Se cuenta con Where(...).Count porque en VB `lista.Count(lambda)` se lee
        ' como si se indizara la propiedad Count, no como la consulta LINQ.
        Dim extraviados = marcadas.Where(Function(r) r.Condicion = "Extraviado").Count()
        If extraviados > 0 Then
            Dim costo = PrestamoService.COSTO_REPOSICION * extraviados
            Dim respuesta = DialogoBiblioteca.Show(
                $"Marcaste {extraviados} ejemplares como extraviados. " &
                $"Se le cobrarán {Formato.Dinero(costo)} de reposición al socio. ¿Confirmas?",
                "Ejemplares extraviados", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            If respuesta <> MessageBoxResult.Yes Then Return
        End If

        Try
            btnDevolver.IsEnabled = False
            btnDevolver.Content = "Registrando…"

            Dim resultado As ResultadoDevolucion = Nothing
            Dim problema = PrestamoService.RegistrarDevolucion(idPrestamoActual, marcadas, resultado)

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo registrar la devolución",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            MostrarResultadoDevolucion(resultado)

            CargarLista()
            MostrarDetalle(idPrestamoActual)

            Dim principal = TryCast(Window.GetWindow(Me), MainWindow)
            If principal IsNot Nothing Then principal.RevisarMora()

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Registrar la devolución", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        Finally
            btnDevolver.IsEnabled = True
            btnDevolver.Content = "✅  Registrar devolución"
        End Try
    End Sub

    Private Sub MostrarResultadoDevolucion(resultado As ResultadoDevolucion)
        Dim mensaje = $"Se registraron {resultado.Ejemplares} " &
                      If(resultado.Ejemplares = 1, "ejemplar.", "ejemplares.")

        mensaje &= If(resultado.PrestamoCerrado,
                      " El préstamo quedó cerrado.",
                      " Todavía quedan ejemplares fuera de la biblioteca.")

        If Not resultado.HuboMulta Then
            DialogoBiblioteca.Show(mensaje, "Devolución registrada con éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If

        ' Con multa el mensaje cambia de tono: hay algo que cobrar
        mensaje &= vbCrLf & vbCrLf &
                   $"Se generó una multa de {Formato.Dinero(resultado.MontoMulta)}"
        If resultado.DiasRetraso > 0 Then mensaje &= $" por {resultado.DiasRetraso} días de retraso"
        mensaje &= ". Queda pendiente de cobro en la página de Multas."

        DialogoBiblioteca.MostrarConDato(mensaje, "Devolución registrada con multa",
                                         Formato.Dinero(resultado.MontoMulta),
                                         MessageBoxImage.Warning)
    End Sub

    ' ======================= RENOVAR =======================

    Private Sub btnRenovar_Click(sender As Object, e As RoutedEventArgs) Handles btnRenovar.Click
        If idPrestamoActual = 0 Then Return

        Try
            Dim nuevoVencimiento As Date
            Dim problema = PrestamoService.Renovar(idPrestamoActual, nuevoVencimiento)

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo renovar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            MostrarDetalle(idPrestamoActual)
            DialogoBiblioteca.Show($"El préstamo se renovó. La nueva fecha de devolución es el " &
                                   $"{Formato.Fecha(nuevoVencimiento)}.",
                                   "Renovado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Renovar el préstamo", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ======================= CANCELAR =======================

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        If idPrestamoActual = 0 Then Return

        Dim respuesta = DialogoBiblioteca.Show(
            $"Cancelar el préstamo {lblFolio.Text} lo anula por completo y devuelve sus ejemplares " &
            "a la estantería. Se usa solo cuando el préstamo se registró por error." & vbCrLf & vbCrLf &
            "¿Deseas continuar?",
            "Cancelar préstamo", MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = PrestamoService.Cancelar(idPrestamoActual, "Registrado por error")

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo cancelar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            MostrarDetalle(idPrestamoActual)

            Dim principal = TryCast(Window.GetWindow(Me), MainWindow)
            If principal IsNot Nothing Then principal.RevisarMora()

            DialogoBiblioteca.Show("El préstamo se canceló y sus ejemplares volvieron a estar disponibles.",
                                   "Cancelado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cancelar el préstamo", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Shared Sub Avisar(mensaje As String)
        DialogoBiblioteca.Show(mensaje, "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning)
    End Sub
End Class
