Imports System.Data
Imports System.IO
Imports PdfSharp.Drawing
Imports PdfSharp.Fonts
Imports PdfSharp.Pdf
Imports ZXing
Imports ZXing.Common

''' <summary>Genera el pase de abordar como archivo PDF, con el código de barras
''' PDF417 del estándar IATA que leen los lectores de los aeropuertos.
'''
''' Se entrega un archivo en vez de mandar a la impresora porque el pasajero
''' decide qué hacer con él: guardarlo en el teléfono, reenviarlo o imprimirlo.</summary>
Public Class PaseAbordarPdf

    ' Medidas en puntos (72 por pulgada), que es la unidad del PDF
    Private Const MARGEN As Double = 40
    Private Const ANCHO_PASE As Double = 515
    Private Const ALTO_PASE As Double = 232
    Private Const ANCHO_TALON As Double = 150

    Private Shared ReadOnly Navy As XColor = XColor.FromArgb(8, 24, 47)
    Private Shared ReadOnly Azul As XColor = XColor.FromArgb(12, 124, 213)
    Private Shared ReadOnly Ambar As XColor = XColor.FromArgb(242, 176, 30)
    Private Shared ReadOnly Gris As XColor = XColor.FromArgb(100, 116, 139)
    Private Shared ReadOnly GrisClaro As XColor = XColor.FromArgb(221, 229, 238)
    Private Shared ReadOnly FondoTalon As XColor = XColor.FromArgb(245, 248, 252)

    Private Shared fuentesListas As Boolean = False

    ''' <summary>PDFsharp no trae tipografías propias ni las busca solo: hay que
    ''' darle un resolutor antes de dibujar el primer texto.</summary>
    Private Shared Sub PrepararFuentes()
        If fuentesListas Then Return
        If GlobalFontSettings.FontResolver Is Nothing Then
            GlobalFontSettings.FontResolver = New ResolutorFuentesPdf()
        End If
        fuentesListas = True
    End Sub

    ''' <summary>Crea el PDF con un pase por cada tramo con check-in hecho.
    ''' Devuelve la cantidad de pases escritos.</summary>
    Public Shared Function Generar(boletos As DataTable, rutaDestino As String) As Integer
        PrepararFuentes()

        Dim documento As New PdfDocument()
        documento.Info.Title = "Pase de abordar — ALAS Honduras"
        documento.Info.Author = "ALAS Honduras"
        documento.Info.Subject = "Boarding pass"

        Dim pagina As PdfPage = Nothing
        Dim lienzo As XGraphics = Nothing
        Dim posicionY As Double = 0
        Dim escritos = 0

        For i = 0 To boletos.Rows.Count - 1
            Dim boleto = boletos.Rows(i)

            ' Caben tres pases por hoja; al llenarse se abre una nueva
            If pagina Is Nothing OrElse posicionY + ALTO_PASE > pagina.Height.Point - MARGEN Then
                pagina = documento.AddPage()
                pagina.Size = PdfSharp.PageSize.A4
                lienzo = XGraphics.FromPdfPage(pagina)
                posicionY = MARGEN
            End If

            ' Si el mismo pasajero tiene otro tramo después, este vuelo hace conexión
            Dim conexion = SiguienteTramo(boletos, i)

            DibujarPase(lienzo, boleto, conexion, MARGEN, posicionY)
            posicionY += ALTO_PASE + 22
            escritos += 1
        Next

        If escritos = 0 Then Return 0

        Directory.CreateDirectory(Path.GetDirectoryName(rutaDestino))
        documento.Save(rutaDestino)
        Registro.Info($"Pase de abordar generado: {rutaDestino} ({escritos} tramo(s))")
        Return escritos
    End Function

    ''' <summary>Busca si el mismo pasajero tiene un vuelo posterior dentro de la
    ''' misma reserva: eso es una escala y se anuncia en el pase.</summary>
    Private Shared Function SiguienteTramo(boletos As DataTable, indiceActual As Integer) As DataRow
        Dim actual = boletos.Rows(indiceActual)
        Dim salidaActual = CDate(actual("fecha_salida"))

        Dim candidato As DataRow = Nothing
        For Each otro As DataRow In boletos.Rows
            If otro Is actual Then Continue For
            If otro("idpasajero").ToString() <> actual("idpasajero").ToString() Then Continue For
            If CDate(otro("fecha_salida")) <= salidaActual Then Continue For
            If candidato Is Nothing OrElse CDate(otro("fecha_salida")) < CDate(candidato("fecha_salida")) Then
                candidato = otro
            End If
        Next
        Return candidato
    End Function

    ' ================================================================
    '  DIBUJO DE UN PASE
    ' ================================================================

    Private Shared Sub DibujarPase(g As XGraphics, boleto As DataRow, conexion As DataRow,
                                   x As Double, y As Double)

        Dim negrita = New XFont("Segoe UI", 9, XFontStyleEx.Bold)
        Dim normal = New XFont("Segoe UI", 9, XFontStyleEx.Regular)
        Dim etiqueta = New XFont("Segoe UI", 6.5, XFontStyleEx.Bold)
        Dim titulo = New XFont("Segoe UI", 20, XFontStyleEx.Bold)
        Dim iata = New XFont("Consolas", 26, XFontStyleEx.Bold)
        Dim dato = New XFont("Consolas", 11, XFontStyleEx.Bold)

        ' --- Marco ---
        g.DrawRectangle(New XPen(GrisClaro, 1), XBrushes.White, x, y, ANCHO_PASE, ALTO_PASE)

        ' --- Franja de marca ---
        g.DrawRectangle(New XSolidBrush(Navy), x, y, ANCHO_PASE, 30)
        g.DrawString("ALAS HONDURAS", New XFont("Segoe UI", 12, XFontStyleEx.Bold),
                     XBrushes.White, New XRect(x + 14, y + 7, 260, 18), XStringFormats.TopLeft)
        g.DrawString("PASE DE ABORDAR · BOARDING PASS", New XFont("Segoe UI", 8, XFontStyleEx.Bold),
                     New XSolidBrush(Ambar),
                     New XRect(x + ANCHO_PASE - 244, y + 9, 230, 14), XStringFormats.TopRight)

        Dim anchoCuerpo = ANCHO_PASE - ANCHO_TALON
        Dim cuerpoY = y + 30

        ' --- Talón que se queda en la puerta de embarque ---
        g.DrawRectangle(New XSolidBrush(FondoTalon), x + anchoCuerpo, cuerpoY, ANCHO_TALON, ALTO_PASE - 30)
        Dim lapizCorte As New XPen(GrisClaro, 1) With {.DashStyle = XDashStyle.Dash}
        g.DrawLine(lapizCorte, x + anchoCuerpo, cuerpoY, x + anchoCuerpo, y + ALTO_PASE)

        ' --- Ruta ---
        Dim ciudadOrigen = boleto("ciudad_origen").ToString()
        Dim ciudadDestino = boleto("ciudad_destino").ToString()

        g.DrawString(boleto("iata_origen").ToString(), iata, New XSolidBrush(Navy),
                     New XRect(x + 14, cuerpoY + 12, 90, 30), XStringFormats.TopLeft)
        g.DrawString(ciudadOrigen, normal, New XSolidBrush(Gris),
                     New XRect(x + 14, cuerpoY + 44, 130, 12), XStringFormats.TopLeft)

        g.DrawString("→", New XFont("Segoe UI", 16, XFontStyleEx.Regular), New XSolidBrush(Azul),
                     New XRect(x + 150, cuerpoY + 18, 30, 20), XStringFormats.TopCenter)

        g.DrawString(boleto("iata_destino").ToString(), iata, New XSolidBrush(Navy),
                     New XRect(x + 196, cuerpoY + 12, 90, 30), XStringFormats.TopLeft)
        g.DrawString(ciudadDestino, normal, New XSolidBrush(Gris),
                     New XRect(x + 196, cuerpoY + 44, 150, 12), XStringFormats.TopLeft)

        ' --- Pasajero ---
        CampoConEtiqueta(g, etiqueta, negrita, "PASAJERO / PASSENGER",
                         boleto("pasajero").ToString(), x + 14, cuerpoY + 66, 330)

        ' --- Datos del vuelo, en rejilla ---
        Dim salida = CDate(boleto("fecha_salida"))
        Dim columna = x + 14
        Dim filaY = cuerpoY + 104
        Dim ancho = (anchoCuerpo - 28) / 4

        CampoConEtiqueta(g, etiqueta, dato, "VUELO", boleto("codigo_vuelo").ToString(), columna, filaY, ancho)
        CampoConEtiqueta(g, etiqueta, dato, "FECHA", salida.ToString("dd/MM/yy"), columna + ancho, filaY, ancho)
        CampoConEtiqueta(g, etiqueta, dato, "SALE", salida.ToString("HH:mm"), columna + ancho * 2, filaY, ancho)
        CampoConEtiqueta(g, etiqueta, dato, "ABORDA",
                         salida.AddMinutes(-40).ToString("HH:mm"), columna + ancho * 3, filaY, ancho)

        filaY += 38
        CampoConEtiqueta(g, etiqueta, dato, "PUERTA", TextoOGuion(boleto, "puerta"), columna, filaY, ancho)
        CampoConEtiqueta(g, etiqueta, dato, "ASIENTO", boleto("asiento").ToString(), columna + ancho, filaY, ancho)
        CampoConEtiqueta(g, etiqueta, dato, "CLASE", boleto("clase").ToString(), columna + ancho * 2, filaY, ancho)
        CampoConEtiqueta(g, etiqueta, dato, "EQUIPAJE",
                         $"{boleto("equipaje_incluido_kg")} kg", columna + ancho * 3, filaY, ancho)

        ' --- Aviso de conexión ---
        If conexion IsNot Nothing Then
            Dim aviso = $"Conexión en {conexion("iata_origen")} · continúa en el vuelo " &
                        $"{conexion("codigo_vuelo")} hacia {conexion("ciudad_destino")} " &
                        $"a las {CDate(conexion("fecha_salida")):HH:mm}"
            g.DrawRectangle(New XSolidBrush(XColor.FromArgb(254, 243, 199)),
                            x + 14, y + ALTO_PASE - 26, anchoCuerpo - 28, 18)
            g.DrawString(aviso, New XFont("Segoe UI", 7.5, XFontStyleEx.Bold),
                         New XSolidBrush(XColor.FromArgb(146, 64, 14)),
                         New XRect(x + 20, y + ALTO_PASE - 23, anchoCuerpo - 40, 12), XStringFormats.TopLeft)
        End If

        ' --- Talón ---
        Dim talonX = x + anchoCuerpo + 14
        g.DrawString(boleto("nombre_aero").ToString(), New XFont("Segoe UI", 7, XFontStyleEx.Bold),
                     New XSolidBrush(Gris), New XRect(talonX, cuerpoY + 12, ANCHO_TALON - 28, 10),
                     XStringFormats.TopLeft)

        CampoConEtiqueta(g, etiqueta, New XFont("Consolas", 24, XFontStyleEx.Bold), "ASIENTO",
                         boleto("asiento").ToString(), talonX, cuerpoY + 32, ANCHO_TALON - 28,
                         New XSolidBrush(Azul))

        CampoConEtiqueta(g, etiqueta, New XFont("Consolas", 14, XFontStyleEx.Bold), "LOCALIZADOR",
                         boleto("codigo_reserva").ToString(), talonX, cuerpoY + 84, ANCHO_TALON - 28)

        CampoConEtiqueta(g, etiqueta, New XFont("Segoe UI", 7.5, XFontStyleEx.Regular), "PASAJERO",
                         boleto("pasajero").ToString(), talonX, cuerpoY + 126, ANCHO_TALON - 28)

        g.DrawString($"{boleto("codigo_vuelo")} · {salida:dd/MM HH:mm}",
                     New XFont("Segoe UI", 7, XFontStyleEx.Regular), New XSolidBrush(Gris),
                     New XRect(talonX, cuerpoY + 158, ANCHO_TALON - 28, 10), XStringFormats.TopLeft)

        ' --- Código de barras IATA ---
        DibujarCodigoDeBarras(g, boleto, x + 14, y + ALTO_PASE - 84, anchoCuerpo - 28, 46,
                              conexion IsNot Nothing)
    End Sub

    Private Shared Sub CampoConEtiqueta(g As XGraphics, fuenteEtiqueta As XFont, fuenteValor As XFont,
                                        textoEtiqueta As String, valor As String,
                                        x As Double, y As Double, ancho As Double,
                                        Optional pincelValor As XBrush = Nothing)
        g.DrawString(textoEtiqueta, fuenteEtiqueta, New XSolidBrush(Gris),
                     New XRect(x, y, ancho, 9), XStringFormats.TopLeft)
        g.DrawString(valor, fuenteValor, If(pincelValor, New XSolidBrush(Navy)),
                     New XRect(x, y + 11, ancho, fuenteValor.Height), XStringFormats.TopLeft)
    End Sub

    Private Shared Function TextoOGuion(fila As DataRow, columna As String) As String
        If IsDBNull(fila(columna)) Then Return "—"
        Dim texto = fila(columna).ToString()
        Return If(String.IsNullOrWhiteSpace(texto), "—", texto)
    End Function

    ' ================================================================
    '  CÓDIGO DE BARRAS PDF417 (IATA BCBP)
    ' ================================================================

    Private Shared Sub DibujarCodigoDeBarras(g As XGraphics, boleto As DataRow,
                                             x As Double, y As Double,
                                             ancho As Double, alto As Double,
                                             hayConexion As Boolean)
        Dim cadena As String
        Try
            Dim salida = CDate(boleto("fecha_salida"))
            cadena = CodigoBcbp.Generar(
                boleto("pasajero").ToString(),
                boleto("codigo_reserva").ToString(),
                boleto("iata_origen").ToString(),
                boleto("iata_destino").ToString(),
                CodigoAerolinea(boleto("codigo_vuelo").ToString()),
                CodigoBcbp.NumeroDesdeCodigo(boleto("codigo_vuelo").ToString()),
                salida,
                boleto("clase").ToString(),
                boleto("asiento").ToString(),
                CInt(boleto("idboleto")) Mod 10000)
        Catch ex As Exception
            Registro.Advertencia($"No se pudo armar el código BCBP: {ex.Message}")
            Return
        End Try

        Try
            Dim opciones As New EncodingOptions With {
                .Width = 600, .Height = 160, .Margin = 0, .PureBarcode = True
            }
            Dim matriz = New MultiFormatWriter().encode(cadena, BarcodeFormat.PDF_417,
                                                        opciones.Width, opciones.Height, opciones.Hints)
            DibujarMatriz(g, matriz, x, y, ancho, alto)
        Catch ex As Exception
            Registro.Advertencia($"No se pudo dibujar el código de barras: {ex.Message}")
        End Try

        ' La cadena también va escrita: si el lector falla, el agente la teclea
        g.DrawString(cadena.Replace(" ", "·"), New XFont("Consolas", 5, XFontStyleEx.Regular),
                     New XSolidBrush(Gris), New XRect(x, y + alto + 2, ancho, 8), XStringFormats.TopLeft)
    End Sub

    ''' <summary>Pinta la matriz de módulos uniendo los negros consecutivos de cada
    ''' fila en un solo rectángulo: son miles de módulos y dibujarlos uno por uno
    ''' engordaría el archivo sin necesidad.</summary>
    Private Shared Sub DibujarMatriz(g As XGraphics, matriz As BitMatrix,
                                     x As Double, y As Double, ancho As Double, alto As Double)
        Dim anchoModulo = ancho / matriz.Width
        Dim altoModulo = alto / matriz.Height
        Dim negro As XBrush = XBrushes.Black

        For fila = 0 To matriz.Height - 1
            Dim columna = 0
            While columna < matriz.Width
                If Not matriz(columna, fila) Then
                    columna += 1
                    Continue While
                End If

                Dim inicio = columna
                While columna < matriz.Width AndAlso matriz(columna, fila)
                    columna += 1
                End While

                g.DrawRectangle(negro,
                                x + inicio * anchoModulo,
                                y + fila * altoModulo,
                                (columna - inicio) * anchoModulo + 0.2,
                                altoModulo + 0.2)
            End While
        Next
    End Sub

    ''' <summary>"AA402-0308" → "AA"</summary>
    Private Shared Function CodigoAerolinea(codigoVuelo As String) As String
        If String.IsNullOrWhiteSpace(codigoVuelo) Then Return ""
        Return New String(codigoVuelo.TakeWhile(Function(c) Not Char.IsDigit(c)).ToArray())
    End Function
End Class
