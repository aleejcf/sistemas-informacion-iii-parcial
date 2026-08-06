Imports Xunit

''' <summary>Pruebas del cálculo de mora. Es la regla que convierte en dinero un
''' préstamo devuelto tarde, así que conviene tenerla cubierta: un error aquí se
''' le cobra de más o de menos a una persona real.</summary>
Public Class MultaTests

    ' ---------- Días de retraso ----------

    <Fact>
    Public Sub DiasDeRetraso_CuentaLosDiasPosterioresAlVencimiento()
        Dim vence = New Date(2026, 8, 1)
        Assert.Equal(5, Validador.DiasDeRetraso(vence, New Date(2026, 8, 6)))
    End Sub

    <Fact>
    Public Sub DiasDeRetraso_DevolverElMismoDiaNoEsRetraso()
        Dim vence = New Date(2026, 8, 1)
        Assert.Equal(0, Validador.DiasDeRetraso(vence, vence))
    End Sub

    ''' <summary>Devolver antes de tiempo no genera crédito a favor: son cero días,
    ''' nunca negativos.</summary>
    <Fact>
    Public Sub DiasDeRetraso_DevolverAntesNoDaNumerosNegativos()
        Dim vence = New Date(2026, 8, 10)
        Assert.Equal(0, Validador.DiasDeRetraso(vence, New Date(2026, 8, 3)))
    End Sub

    <Fact>
    Public Sub DiasDeRetraso_IgnoraLaHoraDelDia()
        Dim vence = New Date(2026, 8, 1)
        Dim devuelto = New DateTime(2026, 8, 3, 23, 45, 0)
        Assert.Equal(2, Validador.DiasDeRetraso(vence, devuelto))
    End Sub

    ' ---------- Monto de la multa ----------

    ''' <summary>Un estudiante (L 5.00 diarios) que devuelve un libro con tres días
    ''' de retraso paga L 15.00.</summary>
    <Fact>
    Public Sub CalcularMulta_MultiplicaDiasPorTarifa()
        Assert.Equal(15D, Validador.CalcularMulta(3, 5D, 1))
    End Sub

    ''' <summary>Se cobra por ejemplar: cada libro que no volvió es un libro que
    ''' otro socio no pudo llevarse.</summary>
    <Fact>
    Public Sub CalcularMulta_CobraPorCadaEjemplarEntregadoTarde()
        Assert.Equal(45D, Validador.CalcularMulta(3, 5D, 3))
    End Sub

    <Fact>
    Public Sub CalcularMulta_SinRetrasoNoHayMulta()
        Assert.Equal(0D, Validador.CalcularMulta(0, 5D, 3))
    End Sub

    <Fact>
    Public Sub CalcularMulta_RetrasoNegativoNoGeneraCobro()
        Assert.Equal(0D, Validador.CalcularMulta(-4, 5D, 2))
    End Sub

    <Fact>
    Public Sub CalcularMulta_SinEjemplaresNoHayMulta()
        Assert.Equal(0D, Validador.CalcularMulta(10, 5D, 0))
    End Sub

    ''' <summary>Un tipo de socio con multa cero (una exoneración institucional,
    ''' por ejemplo) no debe generar cobros.</summary>
    <Fact>
    Public Sub CalcularMulta_TarifaCeroNoGeneraCobro()
        Assert.Equal(0D, Validador.CalcularMulta(10, 0D, 2))
    End Sub

    <Fact>
    Public Sub CalcularMulta_RedondeaADosDecimales()
        ' 3 días × L 3.335 × 1 ejemplar = 10.005 → 10.01
        Assert.Equal(10.01D, Validador.CalcularMulta(3, 3.335D, 1))
    End Sub

    ''' <summary>Caso completo de la semilla: el préstamo PR-000012 de un
    ''' estudiante, un ejemplar devuelto nueve días tarde a L 5.00 diarios.</summary>
    <Fact>
    Public Sub CalcularMulta_ReproduceElCasoDeLaBaseDeDatos()
        Dim vence = New Date(2026, 7, 1)
        Dim devuelto = New Date(2026, 7, 10)

        Dim dias = Validador.DiasDeRetraso(vence, devuelto)
        Assert.Equal(9, dias)
        Assert.Equal(45D, Validador.CalcularMulta(dias, 5D, 1))
    End Sub
End Class
