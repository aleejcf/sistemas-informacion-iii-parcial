Imports System.Data

''' <summary>Panel de control: el estado de la biblioteca de un vistazo.
''' Todas las consultas se hacen al entrar a la página, no en un temporizador:
''' los datos de una biblioteca cambian cuando alguien atiende el mostrador, no solos.</summary>
Public Class PanelPage

    ''' <summary>Alto máximo de una columna de la gráfica, en píxeles. La barra más
    ''' alta del período llega a este valor y el resto se escala proporcionalmente.</summary>
    Private Const ALTO_GRAFICA As Double = 120

    Public Sub Cargar()
        CargarIndicadores()
        CargarMovimiento()
        CargarMasPrestados()
        CargarVencidos()
        CargarPorVencer()
    End Sub

    Private Sub PanelPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' La carga la ordena MainWindow al mostrar la página; aquí solo se anima
        TransicionVentana.FundirEntrada(Me)
    End Sub

    ' ---------- Indicadores ----------

    Private Sub CargarIndicadores()
        Try
            Dim fila = PanelService.Indicadores()
            If fila Is Nothing Then Return

            Dim ejemplares = Db.Numero(fila, "ejemplares")
            Dim disponibles = Db.Numero(fila, "disponibles")
            Dim prestados = Db.Numero(fila, "prestados")

            lblTitulos.Text = Db.Numero(fila, "titulos").ToString()
            lblEjemplares.Text = ejemplares.ToString()
            lblActivos.Text = Db.Numero(fila, "prestamos_activos").ToString()
            lblVencidos.Text = Db.Numero(fila, "prestamos_vencidos").ToString()
            lblSocios.Text = Db.Numero(fila, "socios").ToString()
            lblMultas.Text = Formato.Dinero(Db.Monto(fila, "multas_por_cobrar"))

            ' La barra muestra qué porción del acervo está fuera de la biblioteca
            barraAcervo.Value = If(ejemplares = 0, 0, prestados * 100.0 / ejemplares)
            lblAcervoDetalle.Text = $"{disponibles} en estantería · {prestados} prestados"

            Dim vencenHoy = Db.Numero(fila, "vencen_hoy")
            lblVencenHoy.Text = If(vencenHoy = 0, "", $"{vencenHoy} vencen hoy")

            Dim reservas = Db.Numero(fila, "reservas_activas")
            lblReservas.Text = If(reservas = 0, "Sin reservas en espera",
                                  If(reservas = 1, "1 reserva en espera", $"{reservas} reservas en espera"))

            Dim cobradoHoy = Db.Monto(fila, "cobrado_hoy")
            lblCobradoHoy.Text = If(cobradoHoy > 0, $"Hoy se cobró {Formato.Dinero(cobradoHoy)}", "")

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cargar los indicadores del panel", ex),
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- Gráfica de movimiento ----------

    ''' <summary>El procedimiento devuelve conteos; para dibujarlos hacen falta
    ''' alturas en píxeles. La conversión se hace aquí, agregando dos columnas
    ''' calculadas a la tabla, en vez de meter aritmética dentro del XAML.</summary>
    Private Sub CargarMovimiento()
        Try
            Dim dt = PanelService.MovimientoDiario(14)

            dt.Columns.Add("alto_prestamos", GetType(Double))
            dt.Columns.Add("alto_devoluciones", GetType(Double))
            dt.Columns.Add("etiqueta_dia", GetType(String))

            ' El máximo del período fija la escala. Con mínimo 1 se evita dividir
            ' entre cero el día que no hubo ningún movimiento.
            Dim maximo As Double = 1
            For Each fila As DataRow In dt.Rows
                maximo = Math.Max(maximo, Db.Numero(fila, "prestamos"))
                maximo = Math.Max(maximo, Db.Numero(fila, "devoluciones"))
            Next

            For Each fila As DataRow In dt.Rows
                ' El mínimo de 3 px deja una marca visible en los días de cero,
                ' que es información: "ese día no pasó nada" y no "falta el dato".
                fila("alto_prestamos") = Math.Max(3, Db.Numero(fila, "prestamos") / maximo * ALTO_GRAFICA)
                fila("alto_devoluciones") = Math.Max(3, Db.Numero(fila, "devoluciones") / maximo * ALTO_GRAFICA)
                fila("etiqueta_dia") = CDate(fila("fecha")).ToString("dd/MM")
            Next

            graficaMovimiento.ItemsSource = dt.DefaultView

        Catch ex As Exception
            Registro.Advertencia($"No se pudo cargar la gráfica de movimiento: {ex.Message}")
        End Try
    End Sub

    ' ---------- Más prestados ----------

    Private Sub CargarMasPrestados()
        Try
            Dim dt = PanelService.MasPrestados(6)

            ' La barra se dibuja en porcentaje relativo al título más pedido
            dt.Columns.Add("porcentaje", GetType(Double))
            Dim maximo As Double = 1
            For Each fila As DataRow In dt.Rows
                maximo = Math.Max(maximo, Db.Numero(fila, "veces_prestado"))
            Next
            For Each fila As DataRow In dt.Rows
                fila("porcentaje") = Db.Numero(fila, "veces_prestado") / maximo * 100.0
            Next

            listaMasPrestados.ItemsSource = dt.DefaultView

        Catch ex As Exception
            Registro.Advertencia($"No se pudieron cargar los títulos más prestados: {ex.Message}")
        End Try
    End Sub

    ' ---------- Mora ----------

    Private Sub CargarVencidos()
        Try
            Dim dt = PanelService.Vencidos()
            listaVencidos.ItemsSource = dt.DefaultView
            pnlSinVencidos.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

        Catch ex As Exception
            Registro.Advertencia($"No se pudieron cargar los préstamos vencidos: {ex.Message}")
        End Try
    End Sub

    Private Sub CargarPorVencer()
        Try
            Dim dt = PanelService.PorVencer(3)
            listaPorVencer.ItemsSource = dt.DefaultView
            lblSinPorVencer.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)

        Catch ex As Exception
            Registro.Advertencia($"No se pudieron cargar los préstamos por vencer: {ex.Message}")
        End Try
    End Sub

    ' ---------- Atajos ----------

    Private Sub btnVerVencidos_Click(sender As Object, e As RoutedEventArgs) Handles btnVerVencidos.Click
        IrAVencidos()
    End Sub

    Private Sub tarjetaVencidos_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) _
        Handles tarjetaVencidos.MouseLeftButtonUp
        IrAVencidos()
    End Sub

    Private Sub tarjetaMultas_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) _
        Handles tarjetaMultas.MouseLeftButtonUp
        Dim principal = TryCast(Window.GetWindow(Me), MainWindow)
        If principal IsNot Nothing Then principal.IrAMultas()
    End Sub

    Private Sub IrAVencidos()
        Dim principal = TryCast(Window.GetWindow(Me), MainWindow)
        If principal IsNot Nothing Then principal.IrAPrestamos("Vencido")
    End Sub
End Class
