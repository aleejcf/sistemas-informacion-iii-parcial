Imports System.Data

''' <summary>Panel de control: el resumen de cómo va la operación del día.
''' Es lo primero que ve el usuario al entrar, así que solo lee y muestra;
''' desde aquí no se modifica nada.</summary>
Public Class PanelPage

    ''' <summary>Alto en píxeles que le corresponde a la barra más alta del gráfico.</summary>
    Private Const ALTO_BARRA As Double = 150

    Public Sub Cargar()
        Try
            MostrarIndicadores()
            DibujarGrafico()
            MostrarTablas()

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Cargar el panel de control", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnActualizar_Click(sender As Object, e As RoutedEventArgs) Handles btnActualizar.Click
        Cargar()
    End Sub

    ' ---------- Indicadores ----------

    Private Sub MostrarIndicadores()
        Dim d = PanelService.Estadisticas()

        lblVuelosHoy.Text = LeerEntero(d, "vuelos_hoy").ToString()
        lblProximasSalidas.Text = LeerEntero(d, "proximas_salidas") & " por salir"

        lblPasajerosHoy.Text = LeerEntero(d, "pasajeros_hoy").ToString()
        lblTotalPasajeros.Text = LeerEntero(d, "total_pasajeros") & " pasajeros registrados"

        lblIngresosHoy.Text = Formato.Dinero(LeerMonto(d, "ingresos_hoy"))
        lblIngresosMes.Text = Formato.Dinero(LeerMonto(d, "ingresos_mes")) & " en el mes"

        Dim ocupacion = Math.Max(0, Math.Min(100, LeerMonto(d, "ocupacion_promedio_hoy")))
        lblOcupacion.Text = ocupacion.ToString("N0") & " %"
        barOcupacion.Value = CDbl(ocupacion)

        lblReservasHoy.Text = LeerEntero(d, "reservas_hoy").ToString()
        lblPendientes.Text = LeerEntero(d, "reservas_pendientes").ToString()

        ' Las alertas solo se pintan de rojo cuando de verdad hay algo que atender
        Dim alertas = LeerEntero(d, "alertas")
        lblAlertas.Text = alertas.ToString()
        lblAlertas.Foreground = CType(FindResource(If(alertas > 0, "BrushPeligro", "BrushTextoSuave")), Brush)
    End Sub

    ' ---------- Gráfico de ingresos ----------

    Private Sub DibujarGrafico()
        pnlGrafico.Children.Clear()

        Dim dt = PanelService.Ingresos(7)
        lblSinGrafico.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
        If dt.Rows.Count = 0 Then Return

        Dim mayor As Decimal = 0
        For Each fila As DataRow In dt.Rows
            Dim ingresos = LeerMonto(fila, "ingresos")
            If ingresos > mayor Then mayor = ingresos
        Next

        Dim azulHoy = CType(FindResource("BrushPrimario"), Brush)
        Dim azulSuave As Brush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#93C5FD"), Color))
        Dim grisTexto = CType(FindResource("BrushTextoSuave"), Brush)
        Dim textoFuerte = CType(FindResource("BrushTexto"), Brush)

        For i = 0 To dt.Rows.Count - 1
            Dim fila = dt.Rows(i)
            Dim ingresos = LeerMonto(fila, "ingresos")
            Dim esHoy = (i = dt.Rows.Count - 1)

            ' Un día sin ventas deja igual un tope visible: una barra de alto cero se leería como un hueco
            Dim alto As Double = 3
            If mayor > 0 Then alto = Math.Max(3, CDbl(ingresos / mayor) * ALTO_BARRA)

            Dim barra As New Border With {
                .Width = 40,
                .Height = alto,
                .CornerRadius = New CornerRadius(7, 7, 0, 0),
                .Background = If(esHoy, azulHoy, azulSuave),
                .HorizontalAlignment = HorizontalAlignment.Center,
                .ToolTip = Formato.Dinero(ingresos) & "  ·  " & LeerEntero(fila, "pagos") & " pagos"
            }

            Dim lblMonto As New TextBlock With {
                .Text = MontoCorto(ingresos),
                .FontSize = 10.5,
                .FontWeight = FontWeights.SemiBold,
                .Foreground = If(esHoy, textoFuerte, grisTexto),
                .HorizontalAlignment = HorizontalAlignment.Center,
                .Margin = New Thickness(0, 0, 0, 5)
            }

            Dim apilado As New StackPanel With {.VerticalAlignment = VerticalAlignment.Bottom}
            apilado.Children.Add(lblMonto)
            apilado.Children.Add(barra)

            Dim zona As New Grid With {.Height = ALTO_BARRA + 24}
            zona.Children.Add(apilado)

            Dim lblDia As New TextBlock With {
                .Text = If(IsDBNull(fila("etiqueta")), "", fila("etiqueta").ToString()),
                .FontSize = 11,
                .FontWeight = If(esHoy, FontWeights.SemiBold, FontWeights.Normal),
                .Foreground = If(esHoy, textoFuerte, grisTexto),
                .HorizontalAlignment = HorizontalAlignment.Center,
                .Margin = New Thickness(0, 8, 0, 0)
            }

            Dim columna As New StackPanel With {.Width = 62, .Margin = New Thickness(4, 0, 4, 0)}
            columna.Children.Add(zona)
            columna.Children.Add(lblDia)

            pnlGrafico.Children.Add(columna)
        Next
    End Sub

    ' ---------- Tablas ----------

    Private Sub MostrarTablas()
        Dim salidas = PanelService.ProximasSalidas(8)
        dgSalidas.ItemsSource = salidas.DefaultView
        lblSinSalidas.Visibility = If(salidas.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

        Dim rutas = PanelService.RutasTop(5)
        icRutas.ItemsSource = rutas.DefaultView
        lblSinRutas.Visibility = If(rutas.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

        Dim reservas = PanelService.UltimasReservas(8)
        dgReservas.ItemsSource = reservas.DefaultView
        lblSinReservas.Visibility = If(reservas.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
    End Sub

    ' ---------- Ayudas ----------

    Private Shared Function LeerEntero(fila As DataRow, columna As String) As Integer
        If IsDBNull(fila(columna)) Then Return 0
        Return CInt(fila(columna))
    End Function

    Private Shared Function LeerMonto(fila As DataRow, columna As String) As Decimal
        If IsDBNull(fila(columna)) Then Return 0D
        Return CDec(fila(columna))
    End Function

    ''' <summary>En una barra de 40 px no cabe "L 128,450.00", así que sobre el
    ''' gráfico va el monto abreviado y el exacto queda en la ayuda emergente.</summary>
    Private Shared Function MontoCorto(valor As Decimal) As String
        If valor >= 1000000D Then Return "L " & (valor / 1000000D).ToString("0.#") & "M"
        If valor >= 1000D Then Return "L " & (valor / 1000D).ToString("0.#") & "k"
        Return "L " & valor.ToString("0")
    End Function
End Class
