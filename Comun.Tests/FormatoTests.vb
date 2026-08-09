Imports System.Globalization
Imports Xunit

''' <summary>Pruebas de los formatos que comparten los tres sistemas. Los propios
''' de cada dominio —duración de vuelo, plazo de préstamo, correlativos— se
''' prueban en el Formato de cada proyecto, no aquí.</summary>
Public Class FormatoTests

    <Fact>
    Public Sub Dinero_UsaSiempreElMismoFormatoSinImportarLaCulturaDelEquipo()
        ' El punto decimal no puede depender de la configuración regional del equipo:
        ' una factura tiene que verse igual en cualquier computadora del mostrador.
        Dim anterior = Threading.Thread.CurrentThread.CurrentCulture
        Try
            Threading.Thread.CurrentThread.CurrentCulture = New CultureInfo("de-DE")
            Assert.Equal("L 1,234.50", Formato.Dinero(1234.5D))
        Finally
            Threading.Thread.CurrentThread.CurrentCulture = anterior
        End Try
    End Sub

    <Fact>
    Public Sub Dinero_ConValorNuloDaCero()
        Assert.Equal("L 0.00", Formato.Dinero(CObj(Nothing)))
    End Sub

    <Fact>
    Public Sub Dinero_ConDbNullDaCero()
        Assert.Equal("L 0.00", Formato.Dinero(DBNull.Value))
    End Sub

    <Fact>
    Public Sub FechaLarga_EnEspanolDeHonduras()
        Assert.Equal("domingo, 02 de agosto de 2026", Formato.FechaLarga(New DateTime(2026, 8, 2)))
    End Sub

    <Fact>
    Public Sub FechaHora_UsaFormatoDeHonduras()
        Assert.Equal("10/08/2026 18:30", Formato.FechaHora(New DateTime(2026, 8, 10, 18, 30, 0)))
    End Sub

    <Fact>
    Public Sub Hora_UsaFormatoDe24Horas()
        Assert.Equal("18:30", Formato.Hora(New DateTime(2026, 8, 10, 18, 30, 0)))
    End Sub

    <Fact>
    Public Sub Saludo_NuncaEstaVacio()
        Assert.False(String.IsNullOrWhiteSpace(Formato.Saludo()))
    End Sub
End Class
