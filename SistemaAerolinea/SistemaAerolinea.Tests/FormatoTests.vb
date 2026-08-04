Imports System.Globalization
Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>Pruebas de los formatos que ve el usuario y de la traducción de
''' errores técnicos a mensajes entendibles.</summary>
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

    <Theory>
    <InlineData(0, "—")>
    <InlineData(45, "45m")>
    <InlineData(60, "1h")>
    <InlineData(75, "1h 15m")>
    <InlineData(135, "2h 15m")>
    <InlineData(600, "10h")>
    Public Sub Duracion_SeLeeComoEnUnItinerario(minutos As Integer, esperado As String)
        Assert.Equal(esperado, Formato.Duracion(minutos))
    End Sub

    <Fact>
    Public Sub Hora_UsaFormatoDe24Horas()
        Assert.Equal("18:30", Formato.Hora(New DateTime(2026, 8, 10, 18, 30, 0)))
    End Sub

    ' ---------- Traducción de errores ----------

    <Fact>
    Public Sub MensajeError_NoRevelaDetallesTecnicos()
        Dim mensaje = MensajeError.Describir(New Exception("Object reference not set to an instance"))

        Assert.DoesNotContain("Object reference", mensaje)
        Assert.DoesNotContain("Exception", mensaje)
        Assert.Contains("bitácora", mensaje)
    End Sub

    <Fact>
    Public Sub MensajeError_ExplicaLosProblemasDeArchivo()
        Dim mensaje = MensajeError.Describir(New IO.IOException("archivo en uso"))
        Assert.Contains("archivo", mensaje.ToLower())
    End Sub

    <Fact>
    Public Sub MensajeError_SiempreDevuelveAlgoQueMostrar()
        For Each ex As Exception In {New Exception(), New InvalidOperationException(),
                                     New UnauthorizedAccessException(), New ArgumentException()}
            Assert.False(String.IsNullOrWhiteSpace(MensajeError.Describir(ex)))
        Next
    End Sub

    ' ---------- Colores de estado ----------

    <Fact>
    Public Sub Estado_CadaEstadoTieneSuColor()
        ' Un estado sin color asignado saldría gris y se confundiría con los demás
        Dim estados = {"Programado", "Abordando", "En vuelo", "Aterrizado",
                       "Retrasado", "Cancelado", "Confirmada", "Pendiente de pago"}

        For Each estado In estados
            Assert.NotNull(EstadoAColorConverter.Trazo(estado))
            Assert.NotNull(EstadoAColorFondoConverter.Relleno(estado))
        Next
    End Sub

    <Fact>
    Public Sub Estado_UnEstadoDesconocidoNoRevienta()
        Assert.NotNull(EstadoAColorConverter.Trazo("cualquier cosa"))
        Assert.NotNull(EstadoAColorConverter.Trazo(Nothing))
    End Sub
End Class
