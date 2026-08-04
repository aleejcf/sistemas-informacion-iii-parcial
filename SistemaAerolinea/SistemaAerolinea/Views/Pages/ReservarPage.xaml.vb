Imports System.Data
Imports System.Windows.Media

''' <summary>Asistente de reserva en cuatro pasos: buscar el vuelo, elegir los
''' asientos en el mapa de la cabina, decir quién viaja en cada uno y cobrar.
'''
''' Todo el estado del asistente vive en los campos de esta clase y la emisión
''' final se hace en una sola llamada a ReservaService.Emitir, que es transaccional:
''' hasta ese momento no se ha escrito nada en la base de datos.</summary>
Public Class ReservarPage

    ' ---------- Estado del asistente ----------
    Private vueloElegido As VueloElegido
    Private mapa As New List(Of AsientoMapa)()
    Private ReadOnly seleccion As New List(Of AsientoElegido)()
    Private ReadOnly botonesAsiento As New Dictionary(Of Integer, Button)()
    Private ReadOnly filasAsignacion As New List(Of FilaAsignacion)()
    Private tablaPasajeros As DataTable
    Private limpiando As Boolean = False

    ''' <summary>Une un asiento elegido con el ComboBox donde se le asigna pasajero.</summary>
    Private Class FilaAsignacion
        Public Property Elegido As AsientoElegido
        Public Property Combo As ComboBox
    End Class

    ' ---------- Colores del mapa de asientos ----------
    Private Shared ReadOnly FondoEconomica As SolidColorBrush = Brocha("#E3F0FC")
    Private Shared ReadOnly BordeEconomica As SolidColorBrush = Brocha("#93C5FD")
    Private Shared ReadOnly FondoEjecutiva As SolidColorBrush = Brocha("#EDE9FE")
    Private Shared ReadOnly BordeEjecutiva As SolidColorBrush = Brocha("#C4B5FD")
    Private Shared ReadOnly FondoPrimera As SolidColorBrush = Brocha("#FEF3C7")
    Private Shared ReadOnly BordePrimera As SolidColorBrush = Brocha("#FCD34D")
    Private Shared ReadOnly FondoOcupado As SolidColorBrush = Brocha("#E2E8F0")
    Private Shared ReadOnly BordeOcupado As SolidColorBrush = Brocha("#CBD5E1")
    Private Shared ReadOnly FondoElegido As SolidColorBrush = Brocha("#15A05A")
    Private Shared ReadOnly BordeElegido As SolidColorBrush = Brocha("#0F7A44")

    Private Shared Function Brocha(hex As String) As SolidColorBrush
        Dim pincel As New SolidColorBrush(CType(ColorConverter.ConvertFromString(hex), Color))
        pincel.Freeze()
        Return pincel
    End Function

    ' ================================================================
    '  ARRANQUE
    ' ================================================================

    Private Sub ReservarPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Los combos se llenan una sola vez; volver a la página no debe perder el avance
        If cboOrigen.ItemsSource Is Nothing Then CargarCombos()
        If dpFecha.SelectedDate Is Nothing Then dpFecha.SelectedDate = Date.Today
        If vueloElegido Is Nothing Then MostrarPaso(1)
        Listar(Nothing)
    End Sub

    Private Sub CargarCombos()
        Try
            Dim aeropuertos = CatalogoService.AeropuertosParaCombo()
            cboOrigen.ItemsSource = aeropuertos.DefaultView
            cboDestino.ItemsSource = aeropuertos.Copy().DefaultView

            ' Un pasajero paga por internet: el efectivo solo tiene sentido en el mostrador
            cboMetodoPago.ItemsSource = If(Permisos.EsPasajero,
                                           PagoService.MetodosEnLinea().DefaultView,
                                           CatalogoService.MetodosPagoParaCombo().DefaultView)
            cboMetodoPago.SelectedIndex = 0

            cboComprobante.ItemsSource = New String() {Comprobante.FACTURA, Comprobante.RECIBO}
            cboComprobante.SelectedIndex = 0

            RecargarPasajeros()
        Catch ex As Exception
            Avisar("Cargar los catálogos", ex)
        End Try
    End Sub

    Private Sub RecargarPasajeros()
        tablaPasajeros = PasajeroService.ParaCombo()
        cboTitular.ItemsSource = tablaPasajeros.DefaultView

        ' Un pasajero que reserva para sí mismo no debería tener que elegirse
        If Sesion.IdPasajero IsNot Nothing AndAlso cboTitular.SelectedValue Is Nothing Then
            cboTitular.SelectedValue = Sesion.IdPasajero
        End If

        For Each fila In filasAsignacion
            Dim seleccionado = fila.Combo.SelectedValue
            fila.Combo.ItemsSource = tablaPasajeros.DefaultView
            fila.Combo.SelectedValue = seleccionado
        Next
    End Sub

    ' ================================================================
    '  NAVEGACIÓN ENTRE PASOS
    ' ================================================================

    Private Sub MostrarPaso(numero As Integer)
        pnlPaso1.Visibility = If(numero = 1, Visibility.Visible, Visibility.Collapsed)
        pnlPaso2.Visibility = If(numero = 2, Visibility.Visible, Visibility.Collapsed)
        pnlPaso3.Visibility = If(numero = 3, Visibility.Visible, Visibility.Collapsed)
        pnlPaso4.Visibility = If(numero = 4, Visibility.Visible, Visibility.Collapsed)

        PintarPaso(paso1, txtPaso1, numero >= 1)
        PintarPaso(paso2, txtPaso2, numero >= 2)
        PintarPaso(paso3, txtPaso3, numero >= 3)
        PintarPaso(paso4, txtPaso4, numero >= 4)

        Select Case numero
            Case 1 : TransicionVentana.FundirEntrada(pnlPaso1)
            Case 2 : TransicionVentana.FundirEntrada(pnlPaso2)
            Case 3 : TransicionVentana.FundirEntrada(pnlPaso3)
            Case 4 : TransicionVentana.FundirEntrada(pnlPaso4)
        End Select
    End Sub

    Private Sub PintarPaso(caja As Border, texto As TextBlock, alcanzado As Boolean)
        caja.Background = If(alcanzado, TryFindResource("BrushPrimarioSuave"), TryFindResource("BrushBorde"))
        texto.Foreground = If(alcanzado, TryFindResource("BrushPrimario"), TryFindResource("BrushTextoSuave"))
    End Sub

    Private Sub btnReiniciar_Click(sender As Object, e As RoutedEventArgs) Handles btnReiniciar.Click
        If seleccion.Count > 0 Then
            Dim respuesta = DialogoAlas.Show("Se perderá la selección de asientos. ¿Empezar de nuevo?",
                                             "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question)
            If respuesta <> MessageBoxResult.Yes Then Return
        End If
        Reiniciar()
    End Sub

    Private Sub Reiniciar()
        vueloElegido = Nothing
        mapa.Clear()
        seleccion.Clear()
        botonesAsiento.Clear()
        filasAsignacion.Clear()
        pnlCabina.Children.Clear()
        pnlSeleccion.Children.Clear()
        pnlAsignacion.Children.Clear()
        txtObservacion.Clear()
        cboTitular.SelectedIndex = -1
        MostrarPaso(1)

        ' Se vuelve a listar la ruta que estaba elegida: lo habitual tras emitir
        ' una reserva es vender otra igual o parecida
        Listar(Nothing)
    End Sub

    ' ================================================================
    '  PASO 1 · BUSCAR VUELO
    ' ================================================================

    Private Sub btnBuscar_Click(sender As Object, e As RoutedEventArgs) Handles btnBuscar.Click
        Listar(dpFecha.SelectedDate)
    End Sub

    ''' <summary>En cuanto se elige un aeropuerto se listan solos los próximos
    ''' vuelos de esa ruta, sin filtrar por fecha. Así se ve de inmediato si la
    ''' ruta existe y qué días tiene salida, en vez de ir probando fecha por fecha
    ''' con el botón de buscar.</summary>
    Private Sub cboOrigen_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboOrigen.SelectionChanged
        Listar(Nothing)
    End Sub

    Private Sub cboDestino_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboDestino.SelectionChanged
        Listar(Nothing)
    End Sub

    Private Sub btnLimpiarBusqueda_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiarBusqueda.Click
        ' Los dos combos se limpian de golpe para no lanzar dos búsquedas seguidas
        limpiando = True
        cboOrigen.SelectedIndex = -1
        cboDestino.SelectedIndex = -1
        dpFecha.SelectedDate = Date.Today
        limpiando = False
        Listar(Nothing)
    End Sub

    ''' <summary>Llena la lista de vuelos. Con fecha aplica el filtro exacto del
    ''' botón; sin fecha muestra todos los próximos vuelos de la ruta elegida.</summary>
    Private Sub Listar(fecha As Date?)
        If limpiando OrElse Not IsLoaded Then Return

        Dim origen = If(cboOrigen.SelectedValue Is Nothing, Nothing, cboOrigen.SelectedValue.ToString())
        Dim destino = If(cboDestino.SelectedValue Is Nothing, Nothing, cboDestino.SelectedValue.ToString())

        ' Sin ningún aeropuerto elegido no se lista nada: serían todos los vuelos del sistema
        If origen Is Nothing AndAlso destino Is Nothing Then
            lstVuelos.ItemsSource = Nothing
            lblResumenBusqueda.Text = ""
            lblSinVuelos.Text = "Elige el origen o el destino y verás aquí los próximos vuelos de esa ruta."
            lblSinVuelos.Visibility = Visibility.Visible
            Return
        End If

        If origen IsNot Nothing AndAlso origen = destino Then
            lstVuelos.ItemsSource = Nothing
            lblResumenBusqueda.Text = ""
            lblSinVuelos.Text = "El origen y el destino no pueden ser el mismo aeropuerto."
            lblSinVuelos.Visibility = Visibility.Visible
            Return
        End If

        Try
            Dim resultados = ReservaService.BuscarVuelos(origen, destino, fecha)
            lstVuelos.ItemsSource = resultados.DefaultView

            If resultados.Rows.Count = 0 Then
                lblSinVuelos.Text = MensajeSinResultados(origen, destino, fecha)
                lblSinVuelos.Visibility = Visibility.Visible
                lblResumenBusqueda.Text = ""
            Else
                lblSinVuelos.Visibility = Visibility.Collapsed
                lblResumenBusqueda.Text = ResumenDeResultados(resultados, fecha)
            End If

        Catch ex As Exception
            Avisar("Buscar vuelos", ex)
        End Try
    End Sub

    ''' <summary>Cuando no hay vuelos, decir POR QUÉ: no es lo mismo que la ruta no
    ''' exista a que exista pero no tenga salidas ese día concreto.</summary>
    Private Function MensajeSinResultados(origen As String, destino As String, fecha As Date?) As String
        Dim ruta = DescribirRuta(origen, destino)

        If fecha.HasValue Then
            ' Puede que la ruta sí tenga vuelos, solo que no ese día
            Dim sinFecha = ReservaService.BuscarVuelos(origen, destino, Nothing)
            If sinFecha.Rows.Count > 0 Then
                Dim primera = CDate(sinFecha.Rows(0)("fecha_salida"))
                Return $"No hay vuelos {ruta} el {fecha.Value:dd/MM/yyyy}." & vbCrLf &
                       $"El próximo sale el {primera:dd/MM/yyyy}. Quita la fecha para verlos todos."
            End If
        End If

        Return $"No hay próximos vuelos con asientos disponibles {ruta}." & vbCrLf &
               "Prueba con otro aeropuerto, o programa el vuelo desde el módulo Vuelos."
    End Function

    Private Function ResumenDeResultados(resultados As DataTable, fecha As Date?) As String
        Dim cantidad = resultados.Rows.Count

        If fecha.HasValue Then
            Return $"{cantidad} vuelo(s) el {fecha.Value:dd/MM/yyyy}"
        End If

        ' Sin filtro de fecha conviene decir hasta cuándo llega la programación
        Dim primera = CDate(resultados.Rows(0)("fecha_salida"))
        Dim ultima = CDate(resultados.Rows(cantidad - 1)("fecha_salida"))

        If primera.Date = ultima.Date Then
            Return $"{cantidad} próximo(s) vuelo(s) · {primera:dd/MM/yyyy}"
        End If
        Return $"{cantidad} próximo(s) vuelo(s) · del {primera:dd/MM/yyyy} al {ultima:dd/MM/yyyy}"
    End Function

    Private Function DescribirRuta(origen As String, destino As String) As String
        If origen IsNot Nothing AndAlso destino IsNot Nothing Then Return $"de {Iata(origen)} a {Iata(destino)}"
        If origen IsNot Nothing Then Return $"saliendo de {Iata(origen)}"
        Return $"llegando a {Iata(destino)}"
    End Function

    ''' <summary>Saca el código IATA de la etiqueta del combo ("TGU · Tegucigalpa (…)").
    ''' En un mensaje corto "de SAP a CDG" se lee mejor que el nombre completo.</summary>
    Private Function Iata(idAeropuerto As String) As String
        For Each combo In {cboOrigen, cboDestino}
            Dim fila = TryCast(combo.SelectedItem, DataRowView)
            If fila Is Nothing OrElse fila("idaeropuerto").ToString() <> idAeropuerto Then Continue For

            Dim etiqueta = fila("etiqueta").ToString()
            Dim corte = etiqueta.IndexOf(" ·")
            Return If(corte > 0, etiqueta.Substring(0, corte), etiqueta)
        Next
        Return idAeropuerto
    End Function

    Private Sub lstVuelos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles lstVuelos.SelectionChanged

        Dim fila = TryCast(lstVuelos.SelectedItem, DataRowView)
        If fila Is Nothing Then Return

        Try
            vueloElegido = ReservaService.ObtenerVuelo(CInt(fila("idvuelo")))
            If vueloElegido Is Nothing Then
                DialogoAlas.Show("Ese vuelo ya no está disponible.", "Vuelo no encontrado",
                                 MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            seleccion.Clear()
            CargarMapaDeAsientos()
            MostrarPaso(2)

        Catch ex As Exception
            Avisar("Abrir el vuelo", ex)
        End Try
    End Sub

    ' ================================================================
    '  PASO 2 · MAPA DE ASIENTOS
    ' ================================================================

    Private Sub CargarMapaDeAsientos()
        lblVueloElegido.Text = $"{vueloElegido.CodigoVuelo} · {vueloElegido.CiudadOrigen} → " &
                               $"{vueloElegido.CiudadDestino} · {Formato.FechaHora(vueloElegido.FechaSalida)} · " &
                               $"{vueloElegido.TipoAvion}"

        mapa = ReservaService.MapaAsientos(vueloElegido.IdVuelo)
        DibujarCabina()
        ActualizarSeleccion()
    End Sub

    ''' <summary>Dibuja la cabina fila por fila. Se hace desde código y no en XAML
    ''' porque la distribución (4, 6 u 8 asientos por fila y dónde va el pasillo)
    ''' depende de la aeronave que opera el vuelo.</summary>
    Private Sub DibujarCabina()
        pnlCabina.Children.Clear()
        botonesAsiento.Clear()

        Dim porFila = Math.Max(2, vueloElegido.AsientosPorFila)
        Dim mitad = porFila \ 2

        pnlCabina.Children.Add(Morro())

        Dim claseAnterior As String = Nothing

        For Each grupo In mapa.GroupBy(Function(a) a.Fila).OrderBy(Function(g) g.Key)
            Dim asientosDeLaFila = grupo.OrderBy(Function(a) a.Letra).ToList()
            Dim claseDeLaFila = asientosDeLaFila.First().Clase

            ' Un separador cada vez que cambia la cabina de clase
            If claseDeLaFila <> claseAnterior Then
                pnlCabina.Children.Add(SeparadorDeClase(claseDeLaFila))
                claseAnterior = claseDeLaFila
            End If

            Dim filaPanel As New StackPanel With {
                .Orientation = Orientation.Horizontal,
                .HorizontalAlignment = HorizontalAlignment.Center
            }

            filaPanel.Children.Add(NumeroDeFila(grupo.Key))

            Dim indice = 0
            For Each asiento In asientosDeLaFila
                If indice = mitad Then filaPanel.Children.Add(Pasillo())
                Dim boton = CrearBotonAsiento(asiento)
                botonesAsiento(asiento.IdAsiento) = boton
                filaPanel.Children.Add(boton)
                indice += 1
            Next

            filaPanel.Children.Add(NumeroDeFila(grupo.Key))
            pnlCabina.Children.Add(filaPanel)
        Next
    End Sub

    Private Function Morro() As UIElement
        Dim borde As New Border With {
            .Background = TryFindResource("BrushPrimarioSuave"),
            .CornerRadius = New CornerRadius(60, 60, 10, 10),
            .Padding = New Thickness(30, 12, 30, 12),
            .Margin = New Thickness(0, 0, 0, 14),
            .HorizontalAlignment = HorizontalAlignment.Center
        }
        borde.Child = New TextBlock With {
            .Text = "✈  FRENTE DE LA CABINA",
            .FontSize = 10.5,
            .FontWeight = FontWeights.Bold,
            .Foreground = TryFindResource("BrushPrimario"),
            .HorizontalAlignment = HorizontalAlignment.Center
        }
        Return borde
    End Function

    Private Function SeparadorDeClase(clase As String) As UIElement
        Return New TextBlock With {
            .Text = clase.ToUpper(),
            .FontSize = 10,
            .FontWeight = FontWeights.Bold,
            .Foreground = TryFindResource("BrushTextoSuave"),
            .HorizontalAlignment = HorizontalAlignment.Center,
            .Margin = New Thickness(0, 12, 0, 6)
        }
    End Function

    Private Function NumeroDeFila(numero As Integer) As UIElement
        Return New TextBlock With {
            .Text = numero.ToString(),
            .Width = 30,
            .FontSize = 11,
            .Foreground = TryFindResource("BrushTextoSuave"),
            .VerticalAlignment = VerticalAlignment.Center,
            .TextAlignment = TextAlignment.Center
        }
    End Function

    Private Function Pasillo() As UIElement
        Return New Border With {.Width = 26}
    End Function

    Private Function CrearBotonAsiento(asiento As AsientoMapa) As Button
        Dim boton As New Button With {
            .Style = CType(FindResource("BotonAsiento"), Style),
            .Content = asiento.Letra,
            .Tag = asiento,
            .IsEnabled = Not asiento.Ocupado
        }

        boton.ToolTip = If(asiento.Ocupado,
            $"{asiento.Etiqueta} · {asiento.Clase} · Ocupado",
            $"{asiento.Etiqueta} · {asiento.Clase} · {Formato.Dinero(asiento.Total)} (incluye impuesto)")

        AddHandler boton.Click, AddressOf AsientoClic
        PintarAsiento(boton)
        Return boton
    End Function

    Private Sub AsientoClic(sender As Object, e As RoutedEventArgs)
        Dim boton = CType(sender, Button)
        Dim asiento = CType(boton.Tag, AsientoMapa)

        Dim yaElegido = seleccion.FirstOrDefault(Function(s) s.Asiento.IdAsiento = asiento.IdAsiento)
        If yaElegido IsNot Nothing Then
            seleccion.Remove(yaElegido)
        Else
            ' Un límite razonable: más de nueve pasajeros es una reserva de grupo,
            ' que en un sistema real se maneja por otro canal.
            If seleccion.Count >= 9 Then
                DialogoAlas.Show("Una reserva admite hasta 9 asientos. Para grupos más grandes, " &
                                 "emite varias reservas.", "Límite de la reserva",
                                 MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If
            seleccion.Add(New AsientoElegido With {.Asiento = asiento})
        End If

        PintarAsiento(boton)
        ActualizarSeleccion()
    End Sub

    Private Sub PintarAsiento(boton As Button)
        Dim asiento = CType(boton.Tag, AsientoMapa)

        If seleccion.Any(Function(s) s.Asiento.IdAsiento = asiento.IdAsiento) Then
            boton.Background = FondoElegido
            boton.BorderBrush = BordeElegido
            boton.Foreground = Brushes.White
            Return
        End If

        boton.Foreground = TryFindResource("BrushTexto")

        If asiento.Ocupado Then
            boton.Background = FondoOcupado
            boton.BorderBrush = BordeOcupado
        ElseIf asiento.Clase.StartsWith("Primera") Then
            boton.Background = FondoPrimera
            boton.BorderBrush = BordePrimera
        ElseIf asiento.Clase.StartsWith("Ejecutiva") Then
            boton.Background = FondoEjecutiva
            boton.BorderBrush = BordeEjecutiva
        Else
            boton.Background = FondoEconomica
            boton.BorderBrush = BordeEconomica
        End If
    End Sub

    ''' <summary>Refresca la lista lateral de asientos elegidos y el desglose de precios.</summary>
    Private Sub ActualizarSeleccion()
        pnlSeleccion.Children.Clear()

        For Each elegido In seleccion.OrderBy(Function(s) s.Asiento.Fila).ThenBy(Function(s) s.Asiento.Letra)
            pnlSeleccion.Children.Add(TarjetaAsientoElegido(elegido))
        Next

        lblSinAsientos.Visibility = If(seleccion.Count = 0, Visibility.Visible, Visibility.Collapsed)
        btnAPasajeros.IsEnabled = seleccion.Count > 0

        Dim subtotal, impuesto, total As Decimal
        ReservaService.CalcularTotales(seleccion, subtotal, impuesto, total)

        lblSubtotal2.Text = Formato.Dinero(subtotal)
        lblImpuesto2.Text = Formato.Dinero(impuesto)
        lblTotal2.Text = Formato.Dinero(total)
    End Sub

    Private Function TarjetaAsientoElegido(elegido As AsientoElegido) As UIElement
        Dim marco As New Border With {
            .Background = TryFindResource("BrushFondo"),
            .CornerRadius = New CornerRadius(10),
            .Padding = New Thickness(12, 10, 12, 10),
            .Margin = New Thickness(0, 0, 0, 8)
        }

        Dim contenido As New DockPanel()

        Dim quitar As New Button With {
            .Content = "✕",
            .Style = CType(FindResource("BotonPlano"), Style),
            .Padding = New Thickness(6, 2, 6, 2),
            .VerticalAlignment = VerticalAlignment.Center,
            .Tag = elegido,
            .ToolTip = "Quitar este asiento"
        }
        AddHandler quitar.Click, AddressOf QuitarAsientoClic
        DockPanel.SetDock(quitar, Dock.Right)
        contenido.Children.Add(quitar)

        Dim datos As New StackPanel()
        datos.Children.Add(New TextBlock With {
            .Text = elegido.Etiqueta,
            .FontSize = 15,
            .FontWeight = FontWeights.Bold,
            .Foreground = TryFindResource("BrushTexto")
        })
        datos.Children.Add(New TextBlock With {
            .Text = $"{elegido.Clase} · {Formato.Dinero(elegido.Total)}",
            .FontSize = 11.5,
            .Foreground = TryFindResource("BrushTextoSuave"),
            .Margin = New Thickness(0, 2, 0, 0)
        })
        contenido.Children.Add(datos)

        marco.Child = contenido
        Return marco
    End Function

    Private Sub QuitarAsientoClic(sender As Object, e As RoutedEventArgs)
        Dim elegido = CType(CType(sender, Button).Tag, AsientoElegido)
        seleccion.Remove(elegido)

        Dim boton As Button = Nothing
        If botonesAsiento.TryGetValue(elegido.Asiento.IdAsiento, boton) Then PintarAsiento(boton)
        ActualizarSeleccion()
    End Sub

    Private Sub btnVolverAVuelos_Click(sender As Object, e As RoutedEventArgs) Handles btnVolverAVuelos.Click
        MostrarPaso(1)
    End Sub

    Private Sub btnAPasajeros_Click(sender As Object, e As RoutedEventArgs) Handles btnAPasajeros.Click
        ConstruirAsignacion()
        MostrarPaso(3)
    End Sub

    ' ================================================================
    '  PASO 3 · ASIGNAR PASAJEROS
    ' ================================================================

    Private Sub ConstruirAsignacion()
        pnlAsignacion.Children.Clear()
        filasAsignacion.Clear()

        For Each elegido In seleccion.OrderBy(Function(s) s.Asiento.Fila).ThenBy(Function(s) s.Asiento.Letra)
            pnlAsignacion.Children.Add(FilaDeAsignacion(elegido))
        Next

        ActualizarResumenPaso3()
    End Sub

    Private Function FilaDeAsignacion(elegido As AsientoElegido) As UIElement
        Dim marco As New Border With {
            .Background = TryFindResource("BrushFondo"),
            .CornerRadius = New CornerRadius(10),
            .Padding = New Thickness(12, 9, 12, 9),
            .Margin = New Thickness(0, 0, 0, 8)
        }

        Dim rejilla As New Grid()
        rejilla.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(80)})
        rejilla.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(130)})
        rejilla.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(1, GridUnitType.Star)})
        rejilla.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(110)})
        rejilla.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(44)})

        ' Asiento
        Dim etiquetaAsiento As New TextBlock With {
            .Text = elegido.Etiqueta,
            .FontSize = 16,
            .FontWeight = FontWeights.Bold,
            .Foreground = TryFindResource("BrushTexto"),
            .VerticalAlignment = VerticalAlignment.Center
        }
        Grid.SetColumn(etiquetaAsiento, 0)
        rejilla.Children.Add(etiquetaAsiento)

        ' Clase
        Dim insignia As New Border With {
            .Style = CType(FindResource("Insignia"), Style),
            .Background = EstadoAColorFondoConverter.Relleno(elegido.Clase)
        }
        insignia.Child = New TextBlock With {
            .Text = elegido.Clase,
            .Style = CType(FindResource("TextoInsignia"), Style),
            .Foreground = TryFindResource("BrushTextoSuave")
        }
        Grid.SetColumn(insignia, 1)
        rejilla.Children.Add(insignia)

        ' Pasajero
        Dim combo As New ComboBox With {
            .ItemsSource = tablaPasajeros.DefaultView,
            .DisplayMemberPath = "etiqueta",
            .SelectedValuePath = "idpasajero",
            .Tag = elegido,
            .Margin = New Thickness(0, 0, 10, 0),
            .IsEditable = Permisos.PuedeReservarParaCualquiera,
            .IsTextSearchEnabled = True,
            .StaysOpenOnEdit = True
        }
        AddHandler combo.SelectionChanged, AddressOf PasajeroElegido

        ' Si el pasajero reserva un solo asiento, es obvio que es para él
        If Sesion.IdPasajero IsNot Nothing AndAlso seleccion.Count = 1 Then
            combo.SelectedValue = Sesion.IdPasajero
        End If
        Grid.SetColumn(combo, 2)
        rejilla.Children.Add(combo)

        ' Total
        Dim total As New TextBlock With {
            .Text = Formato.Dinero(elegido.Total),
            .FontSize = 13,
            .FontWeight = FontWeights.SemiBold,
            .TextAlignment = TextAlignment.Right,
            .VerticalAlignment = VerticalAlignment.Center,
            .Foreground = TryFindResource("BrushTexto")
        }
        Grid.SetColumn(total, 3)
        rejilla.Children.Add(total)

        ' Alta rápida de un pasajero nuevo
        Dim nuevo As New Button With {
            .Content = "➕",
            .Style = CType(FindResource("BotonPlano"), Style),
            .ToolTip = "Registrar un pasajero nuevo",
            .Tag = combo,
            .VerticalAlignment = VerticalAlignment.Center,
            .Margin = New Thickness(6, 0, 0, 0)
        }
        AddHandler nuevo.Click, AddressOf NuevoPasajeroClic
        Grid.SetColumn(nuevo, 4)
        rejilla.Children.Add(nuevo)

        marco.Child = rejilla
        filasAsignacion.Add(New FilaAsignacion With {.Elegido = elegido, .Combo = combo})
        Return marco
    End Function

    Private Sub PasajeroElegido(sender As Object, e As SelectionChangedEventArgs)
        Dim combo = CType(sender, ComboBox)
        Dim elegido = CType(combo.Tag, AsientoElegido)

        Dim fila = TryCast(combo.SelectedItem, DataRowView)
        If fila Is Nothing Then
            elegido.IdPasajero = Nothing
            elegido.NombrePasajero = Nothing
        Else
            elegido.IdPasajero = fila("idpasajero").ToString()
            elegido.NombrePasajero = fila("etiqueta").ToString()

            ' El primer pasajero asignado se propone como titular de la reserva
            If cboTitular.SelectedValue Is Nothing Then cboTitular.SelectedValue = elegido.IdPasajero
        End If

        ActualizarResumenPaso3()
    End Sub

    Private Sub NuevoPasajeroClic(sender As Object, e As RoutedEventArgs)
        Dim combo = CType(CType(sender, Button).Tag, ComboBox)

        Dim ventana As New NuevoPasajeroWindow With {.Owner = Window.GetWindow(Me)}
        ventana.ShowDialog()

        If String.IsNullOrWhiteSpace(ventana.CodigoCreado) Then Return

        Try
            RecargarPasajeros()
            combo.SelectedValue = ventana.CodigoCreado
        Catch ex As Exception
            Avisar("Actualizar la lista de pasajeros", ex)
        End Try
    End Sub

    Private Sub ActualizarResumenPaso3()
        ' Where().Count() y no Count(lambda): en VB, Count sobre una List resuelve
        ' a la propiedad de la lista y no al método de extensión de LINQ
        Dim asignados = seleccion.Where(Function(s) s.Asignado).Count()
        lblResumenPaso3.Text = $"{seleccion.Count} asiento(s) en el vuelo {vueloElegido.CodigoVuelo}." & vbCrLf &
                               $"{asignados} de {seleccion.Count} con pasajero asignado."
    End Sub

    Private Sub btnVolverAAsientos_Click(sender As Object, e As RoutedEventArgs) Handles btnVolverAAsientos.Click
        MostrarPaso(2)
    End Sub

    Private Sub btnAPago_Click(sender As Object, e As RoutedEventArgs) Handles btnAPago.Click
        Dim sinAsignar = seleccion.Where(Function(s) Not s.Asignado).Select(Function(s) s.Etiqueta).ToList()
        If sinAsignar.Count > 0 Then
            DialogoAlas.Show($"Falta asignarle pasajero a: {String.Join(", ", sinAsignar)}.",
                             "Asientos sin pasajero", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        ' Un mismo pasajero no puede ocupar dos asientos del mismo vuelo
        Dim repetido = seleccion.GroupBy(Function(s) s.IdPasajero).
                                 FirstOrDefault(Function(g) g.Count() > 1)
        If repetido IsNot Nothing Then
            DialogoAlas.Show($"{repetido.First().NombrePasajero} está asignado a más de un asiento " &
                             "del mismo vuelo. Elige un pasajero distinto para cada asiento.",
                             "Pasajero repetido", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        If cboTitular.SelectedValue Is Nothing Then
            cboTitular.SelectedValue = seleccion.First().IdPasajero
        End If

        PrepararResumen()
        MostrarPaso(4)
    End Sub

    ' ================================================================
    '  PASO 4 · CONFIRMAR Y EMITIR
    ' ================================================================

    Private Sub PrepararResumen()
        lblHoraSalida.Text = Formato.Hora(vueloElegido.FechaSalida)
        lblOrigenResumen.Text = $"{vueloElegido.IataOrigen} · {vueloElegido.CiudadOrigen}"
        lblHoraLlegada.Text = Formato.Hora(vueloElegido.FechaLlegada)
        lblDestinoResumen.Text = $"{vueloElegido.IataDestino} · {vueloElegido.CiudadDestino}"
        lblDuracionResumen.Text = Formato.Duracion(vueloElegido.DuracionMinutos)
        lblVueloResumen.Text = $"{vueloElegido.CodigoVuelo} · {vueloElegido.Aerolinea} · " &
                               $"{vueloElegido.FechaSalida:dd/MM/yyyy}"

        dgResumen.ItemsSource = seleccion.OrderBy(Function(s) s.Asiento.Fila).
                                          ThenBy(Function(s) s.Asiento.Letra).ToList()

        Dim subtotal, impuesto, total As Decimal
        ReservaService.CalcularTotales(seleccion, subtotal, impuesto, total)
        lblSubtotal4.Text = Formato.Dinero(subtotal)
        lblImpuesto4.Text = Formato.Dinero(impuesto)
        lblTotal4.Text = Formato.Dinero(total)
    End Sub

    Private Sub chkCobrarAhora_Checked(sender As Object, e As RoutedEventArgs) Handles chkCobrarAhora.Checked
        If pnlCobro IsNot Nothing Then pnlCobro.Visibility = Visibility.Visible
    End Sub

    Private Sub chkCobrarAhora_Unchecked(sender As Object, e As RoutedEventArgs) Handles chkCobrarAhora.Unchecked
        If pnlCobro IsNot Nothing Then pnlCobro.Visibility = Visibility.Collapsed
    End Sub

    Private Sub btnVolverAPasajeros_Click(sender As Object, e As RoutedEventArgs) Handles btnVolverAPasajeros.Click
        MostrarPaso(3)
    End Sub

    Private Async Sub btnEmitir_Click(sender As Object, e As RoutedEventArgs) Handles btnEmitir.Click
        If cboTitular.SelectedValue Is Nothing Then
            DialogoAlas.Show("Elige el titular de la reserva.", "Falta el titular",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim cobrar = chkCobrarAhora.IsChecked = True
        If cobrar AndAlso cboMetodoPago.SelectedValue Is Nothing Then
            DialogoAlas.Show("Elige el método de pago o desmarca 'Cobrar ahora'.", "Falta el método de pago",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        btnEmitir.IsEnabled = False
        btnEmitir.Content = "Emitiendo…"

        Try
            Dim idVuelo = vueloElegido.IdVuelo
            Dim titular = cboTitular.SelectedValue.ToString()
            Dim asientos = seleccion.ToList()
            Dim metodo As Integer? = If(cobrar, CType(CInt(cboMetodoPago.SelectedValue), Integer?), Nothing)
            Dim tipo = If(cboComprobante.SelectedItem Is Nothing, Comprobante.FACTURA,
                          cboComprobante.SelectedItem.ToString())
            Dim observacion = txtObservacion.Text

            Dim resultado = Await Task.Run(Function() ReservaService.Emitir(
                idVuelo, asientos, titular, metodo, tipo, observacion))

            DialogoAlas.MostrarConDato(
                $"Se emitieron {resultado.Boletos} boleto(s) por {Formato.Dinero(resultado.Total)}." & vbCrLf & vbCrLf &
                "Este es el localizador de la reserva; entrégaselo al pasajero.",
                "Reserva emitida con éxito", resultado.CodigoReserva)

            ' El pase de abordar no se entrega aquí: existe a partir del check-in,
            ' que se hace desde la pantalla de Reservas cuando el pasajero se presenta.
            Reiniciar()

        Catch ex As InvalidOperationException
            ' Reglas del negocio: el mensaje ya viene escrito para el usuario
            DialogoAlas.Show(ex.Message, "No se pudo emitir la reserva",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            RefrescarMapaTrasConflicto()

        Catch ex As Exception
            Avisar("Emitir la reserva", ex)
            RefrescarMapaTrasConflicto()

        Finally
            btnEmitir.IsEnabled = True
            btnEmitir.Content = "✓  Emitir reserva"
        End Try
    End Sub

    ''' <summary>Si la emisión falló porque otra terminal se adelantó, lo honesto es
    ''' volver al mapa con la disponibilidad ya actualizada.</summary>
    Private Sub RefrescarMapaTrasConflicto()
        Try
            If vueloElegido Is Nothing Then Return
            seleccion.Clear()
            CargarMapaDeAsientos()
            MostrarPaso(2)
        Catch ex As Exception
            Registro.Error_("Refrescar el mapa de asientos", ex)
        End Try
    End Sub

    ' ================================================================

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub
End Class
