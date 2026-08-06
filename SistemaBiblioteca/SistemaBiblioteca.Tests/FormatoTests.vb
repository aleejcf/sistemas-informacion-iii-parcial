Imports Xunit

''' <summary>Pruebas de los formatos y de las traducciones de errores: lo que el
''' usuario termina leyendo en pantalla.</summary>
Public Class FormatoTests

    ' ---------- Dinero ----------

    <Fact>
    Public Sub Dinero_UsaLempirasConDosDecimales()
        Assert.Equal("L 1,234.56", Formato.Dinero(1234.56D))
    End Sub

    <Fact>
    Public Sub Dinero_MuestraCeroCuandoNoHayValor()
        Assert.Equal("L 0.00", Formato.Dinero(CObj(Nothing)))
    End Sub

    <Fact>
    Public Sub Dinero_MuestraCeroCuandoElDatoEsNuloEnLaBase()
        Assert.Equal("L 0.00", Formato.Dinero(DBNull.Value))
    End Sub

    ' ---------- Correlativos ----------

    <Fact>
    Public Sub Correlativo_RellenaConCerosALaIzquierda()
        Assert.Equal("PR-000042", Formato.Correlativo("PR-", 42))
    End Sub

    <Fact>
    Public Sub Correlativo_NoRecortaNumerosGrandes()
        Assert.Equal("PR-123456", Formato.Correlativo("PR-", 123456))
    End Sub

    ' ---------- Plazos en palabras ----------

    <Theory>
    <InlineData(0, "Vence hoy")>
    <InlineData(1, "Vence mañana")>
    <InlineData(5, "En 5 días")>
    <InlineData(-1, "1 día de retraso")>
    <InlineData(-7, "7 días de retraso")>
    Public Sub Plazo_LoDiceComoLoDiriaUnaPersona(dias As Integer, esperado As String)
        Assert.Equal(esperado, Formato.Plazo(dias))
    End Sub

    ' ---------- Traducción de errores ----------

    ''' <summary>Una excepción cualquiera nunca debe llegar cruda a la pantalla:
    ''' el detalle técnico va a la bitácora y el usuario ve una frase entendible.</summary>
    <Fact>
    Public Sub Describir_TraduceCualquierExcepcionAUnMensajeEntendible()
        Dim mensaje = MensajeError.Describir(New Exception("Object reference not set"))

        Assert.DoesNotContain("Object reference", mensaje)
        Assert.Contains("bitácora", mensaje)
    End Sub

    <Fact>
    Public Sub Describir_ExplicaLosProblemasDeArchivo()
        Dim mensaje = MensajeError.Describir(New IO.IOException("locked"))
        Assert.Contains("archivo", mensaje)
    End Sub

    <Fact>
    Public Sub Describir_ExplicaLasOperacionesInvalidas()
        Dim mensaje = MensajeError.Describir(New InvalidOperationException("estado inválido"))
        Assert.Contains("Revisa los datos", mensaje)
    End Sub
End Class
