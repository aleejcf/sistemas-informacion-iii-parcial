Imports Xunit

''' <summary>Pruebas del cálculo del precio de una reserva y de la generación del
''' localizador. Es la parte del sistema donde un error se traduce en dinero mal
''' cobrado, así que se prueba sin depender de la base de datos.</summary>
Public Class ReservaTests

    Private Shared Function Asiento(etiqueta As String, clase As String,
                                    precio As Decimal, impuesto As Decimal) As AsientoElegido
        Return New AsientoElegido With {
            .Asiento = New AsientoMapa With {
                .Etiqueta = etiqueta,
                .Clase = clase,
                .Precio = precio,
                .Impuesto = impuesto,
                .Total = precio + impuesto
            },
            .IdPasajero = "P0000001",
            .NombrePasajero = "Juan Lopez"
        }
    End Function

    ' ---------- Totales ----------

    <Fact>
    Public Sub Totales_SumaUnSoloAsiento()
        Dim asientos = New List(Of AsientoElegido) From {Asiento("12C", "Económica", 1850D, 277.5D)}

        Dim subtotal, impuesto, total As Decimal
        ReservaService.CalcularTotales(asientos, subtotal, impuesto, total)

        Assert.Equal(1850D, subtotal)
        Assert.Equal(277.5D, impuesto)
        Assert.Equal(2127.5D, total)
    End Sub

    <Fact>
    Public Sub Totales_SumaVariosAsientosDeDistintaClase()
        Dim asientos = New List(Of AsientoElegido) From {
            Asiento("1A", "Primera Clase", 7770D, 1165.5D),
            Asiento("3B", "Ejecutiva", 4810D, 721.5D),
            Asiento("12C", "Económica", 1850D, 277.5D)
        }

        Dim subtotal, impuesto, total As Decimal
        ReservaService.CalcularTotales(asientos, subtotal, impuesto, total)

        Assert.Equal(14430D, subtotal)
        Assert.Equal(2164.5D, impuesto)
        Assert.Equal(16594.5D, total)
    End Sub

    <Fact>
    Public Sub Totales_ElTotalSiempreEsSubtotalMasImpuesto()
        Dim asientos = New List(Of AsientoElegido) From {
            Asiento("4A", "Ejecutiva", 4810D, 721.5D),
            Asiento("4B", "Ejecutiva", 4810D, 721.5D)
        }

        Dim subtotal, impuesto, total As Decimal
        ReservaService.CalcularTotales(asientos, subtotal, impuesto, total)

        Assert.Equal(subtotal + impuesto, total)
    End Sub

    <Fact>
    Public Sub Totales_SinAsientosDaCero()
        Dim subtotal, impuesto, total As Decimal
        ReservaService.CalcularTotales(New List(Of AsientoElegido)(), subtotal, impuesto, total)

        Assert.Equal(0D, subtotal)
        Assert.Equal(0D, impuesto)
        Assert.Equal(0D, total)
    End Sub

    <Fact>
    Public Sub Totales_ConNothingNoRevienta()
        Dim subtotal, impuesto, total As Decimal
        ReservaService.CalcularTotales(Nothing, subtotal, impuesto, total)

        Assert.Equal(0D, total)
    End Sub

    ' ---------- Asignación de pasajero ----------

    <Fact>
    Public Sub Asiento_SinPasajeroNoEstaAsignado()
        Dim elegido = New AsientoElegido With {
            .Asiento = New AsientoMapa With {.Etiqueta = "7D", .Clase = "Económica"}
        }
        Assert.False(elegido.Asignado)
    End Sub

    <Fact>
    Public Sub Asiento_ConPasajeroEstaAsignado()
        Assert.True(Asiento("7D", "Económica", 100D, 15D).Asignado)
    End Sub

    ' ---------- Localizador (PNR) ----------

    <Fact>
    Public Sub Pnr_TieneSeisCaracteres()
        Assert.Equal(6, GeneradorPnr.Generar().Length)
    End Sub

    <Fact>
    Public Sub Pnr_SoloUsaCaracteresDelAlfabeto()
        For i = 1 To 200
            Dim pnr = GeneradorPnr.Generar()
            For Each caracter In pnr
                Assert.True(GeneradorPnr.ALFABETO.IndexOf(caracter) >= 0,
                            $"El localizador {pnr} trae un carácter fuera del alfabeto: {caracter}")
            Next
        Next
    End Sub

    <Fact>
    Public Sub Pnr_NuncaUsaCaracteresQueSeConfunden()
        ' I, O, 0 y 1 se confunden al dictar o transcribir un localizador
        For Each prohibido In "IO01"
            Assert.True(GeneradorPnr.ALFABETO.IndexOf(prohibido) < 0,
                        $"El alfabeto no debería incluir el carácter {prohibido}")
        Next
    End Sub

    <Fact>
    Public Sub Pnr_GeneradosSonRazonablementeDistintos()
        ' Con 32^6 combinaciones, 500 localizadores no deberían chocar
        Dim generados As New HashSet(Of String)()
        For i = 1 To 500
            generados.Add(GeneradorPnr.Generar())
        Next
        Assert.True(generados.Count > 495, $"Se repitieron demasiados: {500 - generados.Count}")
    End Sub

    <Theory>
    <InlineData("ABC234", True)>
    <InlineData("abc234", True)>
    <InlineData("ABC23", False)>
    <InlineData("ABC2345", False)>
    <InlineData("ABCI34", False)>
    <InlineData("", False)>
    Public Sub Pnr_ValidaElFormato(pnr As String, esperado As Boolean)
        Assert.Equal(esperado, GeneradorPnr.EsValido(pnr))
    End Sub

    ' ---------- Numeración de comprobantes ----------

    <Fact>
    Public Sub Comprobante_LaFacturaLlevaPrefijoF()
        Assert.Equal("F-000123-1", Comprobante.Numero(Comprobante.FACTURA, 123, 1))
    End Sub

    <Fact>
    Public Sub Comprobante_ElReciboLlevaPrefijoR()
        Assert.Equal("R-000123-2", Comprobante.Numero(Comprobante.RECIBO, 123, 2))
    End Sub

    <Fact>
    Public Sub Comprobante_DosPagosDeLaMismaReservaNoChocan()
        Dim primero = Comprobante.Numero(Comprobante.FACTURA, 77, 1)
        Dim segundo = Comprobante.Numero(Comprobante.FACTURA, 77, 2)
        Assert.NotEqual(primero, segundo)
    End Sub

    <Fact>
    Public Sub Comprobante_CabeEnLaColumnaDeLaBaseDeDatos()
        ' num_comprobante es VARCHAR(15)
        Assert.True(Comprobante.Numero(Comprobante.FACTURA, 999999, 9).Length <= 15)
    End Sub

    ' ---------- Contraseñas temporales ----------

    <Fact>
    Public Sub ClaveTemporal_SiempreCumpleLasReglas()
        For i = 1 To 100
            Assert.Null(Validador.ValidarContrasena(GeneradorClave.GenerarTemporal()))
        Next
    End Sub
End Class
