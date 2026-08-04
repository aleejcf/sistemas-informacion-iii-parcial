Imports System.Data
Imports System.IO
Imports Xunit

''' <summary>Pruebas del pase de abordar: la cadena IATA que va en el código de
''' barras y la generación del PDF.</summary>
Public Class PaseAbordarTests

    ' ================================================================
    '  CADENA IATA BCBP  (formato M1, Resolución 792)
    ' ================================================================

    Private Shared Function CadenaDeEjemplo() As String
        Return CodigoBcbp.Generar(
            pasajero:="Juan Lopez Martinez",
            pnr:="HQVYB5",
            iataOrigen:="SAP",
            iataDestino:="MIA",
            codigoAerolinea:="AA",
            numeroVuelo:=402,
            fechaSalida:=New DateTime(2026, 8, 3, 8, 45, 0),
            clase:="Económica",
            asiento:="12C",
            secuencia:=17)
    End Function

    <Fact>
    Public Sub LaCadenaMideExactamente60Caracteres()
        ' El BCBP no lleva separadores: si sobra o falta un carácter, todos los
        ' campos siguientes se leen corridos.
        Assert.Equal(CodigoBcbp.LONGITUD_OBLIGATORIA, CadenaDeEjemplo().Length)
    End Sub

    <Fact>
    Public Sub CadaCampoQuedaEnSuPosicion()
        Dim c = CadenaDeEjemplo()

        Assert.Equal("M", c.Substring(0, 1))                  ' Código de formato
        Assert.Equal("1", c.Substring(1, 1))                  ' Un solo tramo
        Assert.Equal("LOPEZ/JUAN", c.Substring(2, 20).Trim()) ' Apellido/Nombre
        Assert.Equal("E", c.Substring(22, 1))                 ' Boleto electrónico
        Assert.Equal("HQVYB5", c.Substring(23, 7).Trim())     ' Localizador
        Assert.Equal("SAP", c.Substring(30, 3))               ' Origen
        Assert.Equal("MIA", c.Substring(33, 3))               ' Destino
        Assert.Equal("AA", c.Substring(36, 3).Trim())         ' Aerolínea
        Assert.Equal("0402", c.Substring(39, 5).Trim())       ' Número de vuelo
        Assert.Equal("215", c.Substring(44, 3))               ' 3 de agosto = día 215
        Assert.Equal("Y", c.Substring(47, 1))                 ' Económica
        Assert.Equal("012C", c.Substring(48, 4))              ' Asiento
        Assert.Equal("0017", c.Substring(52, 5).Trim())       ' Secuencia de check-in
        Assert.Equal("1", c.Substring(57, 1))                 ' Con pase emitido
        Assert.Equal("00", c.Substring(58, 2))                ' Sin datos condicionales
    End Sub

    <Fact>
    Public Sub LaCadenaEsAsciiPuro()
        ' Un acento dentro del código de barras lo vuelve ilegible para los lectores
        Dim c = CodigoBcbp.Generar("José Núñez Peña", "AB2C3D", "TGU", "MAD", "IB", 801,
                                   New DateTime(2026, 12, 31), "Primera Clase", "1A", 3)
        Assert.All(c, Sub(caracter) Assert.InRange(AscW(caracter), 32, 126))
        Assert.Contains("NUNEZ/JOSE", c)
    End Sub

    <Theory>
    <InlineData("Económica", "Y")>
    <InlineData("Ejecutiva", "C")>
    <InlineData("Primera Clase", "F")>
    <InlineData("", "Y")>
    Public Sub ElCompartimentoSigueLaClase(clase As String, esperado As String)
        Assert.Equal(esperado, CodigoBcbp.Compartimento(clase))
    End Sub

    <Theory>
    <InlineData("12C", "012C")>
    <InlineData("1A", "001A")>
    <InlineData("30F", "030F")>
    Public Sub ElAsientoSeRellenaATresDigitos(asiento As String, esperado As String)
        Assert.Equal(esperado, CodigoBcbp.AsientoBcbp(asiento))
    End Sub

    <Theory>
    <InlineData("AA402-0308", 402)>
    <InlineData("IB801-1208", 801)>
    <InlineData("CM1001-0208", 1001)>
    <InlineData("", 0)>
    Public Sub ElNumeroDeVueloSaleDelCodigo(codigo As String, esperado As Integer)
        Assert.Equal(esperado, CodigoBcbp.NumeroDesdeCodigo(codigo))
    End Sub

    <Fact>
    Public Sub ElDiaJulianoSonTresDigitos()
        Assert.Equal("001", CodigoBcbp.DiaJuliano(New DateTime(2026, 1, 1)))
        Assert.Equal("215", CodigoBcbp.DiaJuliano(New DateTime(2026, 8, 3)))
        Assert.Equal("365", CodigoBcbp.DiaJuliano(New DateTime(2026, 12, 31)))
    End Sub

    ' ================================================================
    '  GENERACIÓN DEL PDF
    ' ================================================================

    ''' <summary>Arma una tabla con las mismas columnas que devuelve
    ''' ReservaService.Boletos, para poder probar sin base de datos.</summary>
    Private Shared Function TablaDeBoletos() As DataTable
        Dim t As New DataTable()
        For Each columna In {"codigo_reserva", "codigo_vuelo", "iata_origen", "ciudad_origen",
                             "iata_destino", "ciudad_destino", "nombre_aero", "pasajero",
                             "idpasajero", "asiento", "clase", "puerta", "estado"}
            t.Columns.Add(columna, GetType(String))
        Next
        t.Columns.Add("fecha_salida", GetType(DateTime))
        t.Columns.Add("equipaje_incluido_kg", GetType(Integer))
        t.Columns.Add("idboleto", GetType(Integer))

        t.Rows.Add("HQVYB5", "AA402-0308", "SAP", "San Pedro Sula", "MIA", "Miami",
                   "American Airlines", "Juan Lopez Martinez", "P0000001", "12C",
                   "Económica", "B2", "Check-in", New DateTime(2026, 8, 3, 8, 45, 0), 20, 17)

        ' Segundo tramo del mismo pasajero: el pase debe anunciar la conexión
        t.Rows.Add("HQVYB5", "IB801-0308", "MIA", "Miami", "MAD", "Madrid",
                   "Iberia", "Juan Lopez Martinez", "P0000001", "3A",
                   "Ejecutiva", "C7", "Check-in", New DateTime(2026, 8, 3, 18, 30, 0), 35, 18)
        Return t
    End Function

    <Fact>
    Public Sub SeGeneraUnPdfValidoConUnPasePorTramo()
        Dim ruta = Path.Combine(Path.GetTempPath(), $"alas_pase_prueba_{Guid.NewGuid():N}.pdf")

        Try
            Dim cuantos = PaseAbordarPdf.Generar(TablaDeBoletos(), ruta)

            Assert.Equal(2, cuantos)
            Assert.True(File.Exists(ruta), "El archivo PDF no se creó.")

            Dim bytes = File.ReadAllBytes(ruta)
            ' Todo PDF empieza con la firma %PDF-
            Assert.True(bytes.Length > 4000, $"El PDF salió sospechosamente pequeño: {bytes.Length} bytes")
            Assert.Equal(Convert.ToByte(Asc("%"c)), bytes(0))
            Assert.Equal(Convert.ToByte(Asc("P"c)), bytes(1))
            Assert.Equal(Convert.ToByte(Asc("D"c)), bytes(2))
            Assert.Equal(Convert.ToByte(Asc("F"c)), bytes(3))

        Finally
            If File.Exists(ruta) Then File.Delete(ruta)
        End Try
    End Sub

    <Fact>
    Public Sub SinTramosNoSeEscribeArchivo()
        Dim ruta = Path.Combine(Path.GetTempPath(), $"alas_pase_vacio_{Guid.NewGuid():N}.pdf")

        Dim vacia = TablaDeBoletos().Clone()
        Assert.Equal(0, PaseAbordarPdf.Generar(vacia, ruta))
        Assert.False(File.Exists(ruta), "No debería crearse un PDF sin pases.")
    End Sub
End Class
