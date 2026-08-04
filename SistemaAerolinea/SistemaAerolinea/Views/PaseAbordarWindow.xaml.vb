Imports System.Data
Imports System.Diagnostics
Imports System.IO

''' <summary>Muestra y descarga los pases de abordar de una reserva.
'''
''' Un pase solo existe después del check-in: es el momento en que la aerolínea
''' confirma que esa persona viaja y le asigna su lugar en la puerta de embarque.
''' Por eso aquí solo aparecen los boletos con check-in hecho o ya abordados.</summary>
Public Class PaseAbordarWindow

    Private ReadOnly idReserva As Integer
    Private ReadOnly idBoleto As Integer
    Private pases As DataTable

    Public Sub New(reserva As Integer)
        InitializeComponent()
        idReserva = reserva
        idBoleto = 0
    End Sub

    Public Sub New(reserva As Integer, boleto As Integer)
        InitializeComponent()
        idReserva = reserva
        idBoleto = boleto
    End Sub

    Private Sub PaseAbordarWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)

        Try
            Dim boletos = ReservaService.Boletos(idReserva)

            Dim filtro = "estado IN ('Check-in', 'Abordado')"
            If idBoleto > 0 Then filtro &= $" AND idboleto = {idBoleto}"
            Dim conPase = boletos.Select(filtro, "fecha_salida")

            If conPase.Length = 0 Then
                MostrarPorQueNoHayPase(boletos)
                Return
            End If

            pases = boletos.Clone()
            For Each fila In conPase
                pases.ImportRow(fila)
            Next

            lstPases.ItemsSource = pases.DefaultView
            lblEncabezado.Text = $"Localizador {pases.Rows(0)("codigo_reserva")} · " &
                                 $"{pases.Rows.Count} pase(s) de abordar"

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Cargar los pases de abordar", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ''' <summary>No basta con decir "no hay pases": hay que explicar qué falta,
    ''' porque el motivo cambia lo que el agente tiene que hacer.</summary>
    Private Sub MostrarPorQueNoHayPase(boletos As DataTable)
        visorPases.Visibility = Visibility.Collapsed
        btnDescargar.IsEnabled = False
        lblSinPases.Visibility = Visibility.Visible

        Dim vivos = boletos.Select("estado <> 'Cancelado'")

        If boletos.Rows.Count = 0 Then
            lblEncabezado.Text = "Reserva sin boletos"
            lblSinPases.Text = "Esta reserva no tiene boletos."
        ElseIf vivos.Length = 0 Then
            lblEncabezado.Text = "Reserva cancelada"
            lblSinPases.Text = "Todos los boletos de esta reserva están cancelados, " &
                               "así que no hay pases de abordar que entregar."
        Else
            lblEncabezado.Text = "Falta el check-in"
            lblSinPases.Text = "El pase de abordar se genera después del check-in." & vbCrLf & vbCrLf &
                               "Vuelve a la pantalla de Reservas, elige el boleto del pasajero y " &
                               "pulsa «Check-in». La reserva también tiene que estar pagada."
        End If
    End Sub

    ''' <summary>Genera el PDF y deja que el usuario elija dónde guardarlo. Se le
    ''' entrega el archivo en vez de mandarlo a la impresora: así decide él si lo
    ''' imprime, lo guarda en el teléfono o lo reenvía.</summary>
    Private Sub btnDescargar_Click(sender As Object, e As RoutedEventArgs) Handles btnDescargar.Click
        If pases Is Nothing OrElse pases.Rows.Count = 0 Then Return

        Dim pnr = pases.Rows(0)("codigo_reserva").ToString().Trim()

        Dim dialogo As New Microsoft.Win32.SaveFileDialog With {
            .Title = "Guardar el pase de abordar",
            .FileName = $"PaseAbordar_{pnr}.pdf",
            .DefaultExt = ".pdf",
            .Filter = "Documento PDF (*.pdf)|*.pdf",
            .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            .OverwritePrompt = True
        }
        If dialogo.ShowDialog() <> True Then Return

        btnDescargar.IsEnabled = False
        btnDescargar.Content = "Generando…"

        Try
            Dim cuantos = PaseAbordarPdf.Generar(pases, dialogo.FileName)

            If cuantos = 0 Then
                DialogoAlas.Show("No se generó ningún pase.", "Sin pases",
                                 MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            BitacoraService.Registrar(BitacoraService.CHECKIN, "boleto",
                                      $"Pase de abordar descargado · reserva {pnr}")

            Dim abrir = DialogoAlas.Show(
                $"Se guardó el PDF con {cuantos} pase(s) de abordar." & vbCrLf & vbCrLf &
                $"{dialogo.FileName}" & vbCrLf & vbCrLf & "¿Quieres abrirlo ahora?",
                "Descarga con éxito", MessageBoxButton.YesNo, MessageBoxImage.Question)

            If abrir = MessageBoxResult.Yes Then AbrirArchivo(dialogo.FileName)

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Generar el pase de abordar en PDF", ex), "Error",
                             MessageBoxButton.OK, MessageBoxImage.Error)
        Finally
            btnDescargar.IsEnabled = True
            btnDescargar.Content = "⤓  Descargar PDF"
        End Try
    End Sub

    Private Shared Sub AbrirArchivo(ruta As String)
        Try
            If Not File.Exists(ruta) Then Return
            ' UseShellExecute deja que Windows elija el lector de PDF del usuario
            Process.Start(New ProcessStartInfo(ruta) With {.UseShellExecute = True})
        Catch ex As Exception
            Registro.Advertencia($"No se pudo abrir el PDF: {ex.Message}")
        End Try
    End Sub

    Private Sub btnCerrar_Click(sender As Object, e As RoutedEventArgs) Handles btnCerrar.Click
        Me.Close()
    End Sub
End Class
