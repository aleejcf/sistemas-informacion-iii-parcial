Imports Xunit

''' <summary>Pruebas del cálculo de cobro del parqueadero.
''' Regla del negocio: se cobra por hora completa o fracción, con un mínimo de 1 hora.</summary>
Public Class CobroTests

    Private Const TarifaCarro As Decimal = 30D
    Private Const TarifaMoto As Decimal = 15D

    <Fact>
    Public Sub CalcularCobro_UnMinuto_CobraHoraCompleta()
        ' Aunque solo esté 1 minuto, se cobra la hora mínima
        Assert.Equal(30D, MovimientoService.CalcularCobro(1, TarifaCarro))
    End Sub

    <Fact>
    Public Sub CalcularCobro_ExactamenteUnaHora_CobraUnaHora()
        Assert.Equal(30D, MovimientoService.CalcularCobro(60, TarifaCarro))
    End Sub

    <Fact>
    Public Sub CalcularCobro_UnaHoraYUnMinuto_CobraDosHoras()
        ' La fracción se cobra como hora completa
        Assert.Equal(60D, MovimientoService.CalcularCobro(61, TarifaCarro))
    End Sub

    <Fact>
    Public Sub CalcularCobro_TresHorasExactas_CobraTresHoras()
        Assert.Equal(90D, MovimientoService.CalcularCobro(180, TarifaCarro))
    End Sub

    <Theory>
    <InlineData(30, 15)>      ' media hora de moto → 1 hora
    <InlineData(90, 30)>      ' hora y media de moto → 2 horas
    <InlineData(1440, 360)>   ' un día completo → 24 horas
    Public Sub CalcularCobro_Moto_CalculaSegunTarifa(minutos As Integer, esperado As Integer)
        Assert.Equal(CDec(esperado), MovimientoService.CalcularCobro(minutos, TarifaMoto))
    End Sub

    <Fact>
    Public Sub CalcularCobro_CeroMinutos_CobraMinimoUnaHora()
        ' Caso límite: nunca se cobra cero
        Assert.Equal(30D, MovimientoService.CalcularCobro(0, TarifaCarro))
    End Sub

    <Fact>
    Public Sub CalcularCobro_NuncaDevuelveNegativo()
        Assert.True(MovimientoService.CalcularCobro(-5, TarifaCarro) > 0D)
    End Sub
End Class
