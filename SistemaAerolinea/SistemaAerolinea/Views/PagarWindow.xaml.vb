Imports System.Data
Imports System.Windows.Media.Animation

''' <summary>Pago en línea del pasajero, como en cualquier aerolínea de hoy:
''' elige el método, paga el saldo completo y la reserva queda confirmada al
''' instante, sin pasar por el mostrador.
'''
''' No se pide el número de la tarjeta en ningún momento. En un sistema real ese
''' dato lo captura la pasarela (Stripe, PayPal, un banco), que solo devuelve la
''' autorización: así el comercio nunca toca el número y no queda obligado a
''' cumplir la norma PCI-DSS. Aquí se simula esa respuesta manteniendo el mismo
''' flujo y los mismos datos que se guardarían de verdad.</summary>
Public Class PagarWindow

    Private ReadOnly idReserva As Integer

    ''' <summary>Queda en True si el cobro se aprobó, para que la pantalla que
    ''' abrió esta ventana sepa que tiene que recargarse.</summary>
    Public Property Pagado As Boolean = False

    Public Sub New(reserva As Integer)
        InitializeComponent()
        idReserva = reserva
    End Sub

    Private Sub PagarWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)

        Try
            Dim reserva = ReservaService.ObtenerPorId(idReserva)
            If reserva Is Nothing Then
                DialogoAlas.Show("No se encontró la reserva, o no está a tu nombre.",
                                 "Reserva no disponible", MessageBoxButton.OK, MessageBoxImage.Warning)
                Me.Close()
                Return
            End If

            Dim saldo = CDec(reserva("costo")) - CDec(reserva("pagado"))
            If saldo <= 0 Then
                DialogoAlas.Show("Esta reserva ya está pagada.", "Nada que cobrar",
                                 MessageBoxButton.OK, MessageBoxImage.Information)
                Me.Close()
                Return
            End If

            lblPnr.Text = reserva("codigo_reserva").ToString()
            lblBoletos.Text = $"{reserva("boletos")} boleto(s)"
            lblItinerario.Text = DescribirItinerario()

            lblSubtotal.Text = Formato.Dinero(reserva("subtotal"))
            lblImpuesto.Text = Formato.Dinero(reserva("impuesto"))
            lblYaPagado.Text = Formato.Dinero(reserva("pagado"))
            lblTotal.Text = Formato.Dinero(saldo)

            cboMetodo.ItemsSource = PagoService.MetodosEnLinea().DefaultView
            cboMetodo.SelectedIndex = 0

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Preparar el pago", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
            Me.Close()
        End Try
    End Sub

    Private Function DescribirItinerario() As String
        Dim boletos = ReservaService.Boletos(idReserva)
        Dim vivos = boletos.Select("estado <> 'Cancelado'", "fecha_salida")
        If vivos.Length = 0 Then Return ""

        Dim primero = vivos(0)
        Dim texto = $"{primero("iata_origen")} → {primero("iata_destino")} · " &
                    $"{CDate(primero("fecha_salida")):ddd dd MMM, HH:mm}"

        If vivos.Length > 1 Then texto &= $" (+{vivos.Length - 1} tramo(s) más)"
        Return texto
    End Function

    ' ---------- Cobro ----------

    Private Async Sub btnPagar_Click(sender As Object, e As RoutedEventArgs) Handles btnPagar.Click
        OcultarError()

        If cboMetodo.SelectedValue Is Nothing Then
            MostrarError("Elige cómo quieres pagar.")
            Return
        End If

        Dim idMetodo = CInt(cboMetodo.SelectedValue)

        pnlCheckout.Visibility = Visibility.Collapsed
        pnlProcesando.Visibility = Visibility.Visible
        LatirLogo()

        Try
            ' La espera imita el ida y vuelta con la pasarela. Sin ella el pago se
            ' sentiría irreal, y además da tiempo a leer que algo está pasando.
            barraProceso.Value = 25
            Await Task.Delay(700)

            lblProcesando.Text = "Autorizando el cobro…"
            barraProceso.Value = 60

            Dim referencia As String = ""
            Dim problema = Await Task.Run(
                Function()
                    Dim propia As String = ""
                    Dim resultado = PagoService.PagarDesdeElPortal(idReserva, idMetodo, propia)
                    Return Tuple.Create(resultado, propia)
                End Function)

            If problema.Item1 IsNot Nothing Then
                pnlProcesando.Visibility = Visibility.Collapsed
                pnlCheckout.Visibility = Visibility.Visible
                MostrarError(problema.Item1)
                Return
            End If

            referencia = problema.Item2

            lblProcesando.Text = "Confirmando tu reserva…"
            barraProceso.Value = 100
            Await Task.Delay(500)

            MostrarComprobante(referencia)

        Catch ex As Exception
            pnlProcesando.Visibility = Visibility.Collapsed
            pnlCheckout.Visibility = Visibility.Visible
            MostrarError(MensajeError.Traducir("Procesar el pago", ex))
        End Try
    End Sub

    Private Sub MostrarComprobante(referencia As String)
        Pagado = True

        lblReferencia.Text = referencia
        lblListoDetalle.Text = $"Se cobraron {lblTotal.Text} de la reserva {lblPnr.Text}."

        Try
            Dim pagos = PagoService.DeLaReserva(idReserva)
            If pagos.Rows.Count > 0 Then
                Dim ultimo = pagos.Rows(pagos.Rows.Count - 1)
                lblComprobante.Text = $"Factura {ultimo("num_comprobante")} · " &
                                      $"{ultimo("metodo_pago")} · {CDate(ultimo("fecha")):dd/MM/yyyy HH:mm}"
            End If
        Catch ex As Exception
            Registro.Advertencia($"No se pudo leer el comprobante recién creado: {ex.Message}")
        End Try

        pnlProcesando.Visibility = Visibility.Collapsed
        pnlListo.Visibility = Visibility.Visible
        TransicionVentana.FundirEntrada(pnlListo)
    End Sub

    ''' <summary>El logotipo late mientras se procesa: es la señal de que el
    ''' sistema sigue trabajando y no se quedó congelado.</summary>
    Private Sub LatirLogo()
        Dim latido As New DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(700)) With {
            .AutoReverse = True,
            .RepeatBehavior = RepeatBehavior.Forever
        }
        logoProcesando.BeginAnimation(OpacityProperty, latido)
    End Sub

    Private Sub btnCopiar_Click(sender As Object, e As RoutedEventArgs) Handles btnCopiar.Click
        Try
            Clipboard.SetText(lblReferencia.Text)
            btnCopiar.Content = "¡Copiado!"
        Catch
            btnCopiar.Content = "No se pudo copiar"
        End Try
    End Sub

    Private Sub btnListo_Click(sender As Object, e As RoutedEventArgs) Handles btnListo.Click
        Me.Close()
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        Me.Close()
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
