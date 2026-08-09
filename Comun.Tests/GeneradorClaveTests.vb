Imports Xunit

Public Class GeneradorClaveTests

    <Fact>
    Public Sub GenerarTemporal_CumpleLasReglasDeValidarContrasena()
        Dim clave = GeneradorClave.GenerarTemporal()

        Assert.Null(Validador.ValidarContrasena(clave))
    End Sub

    <Fact>
    Public Sub GenerarTemporal_TieneDiezCaracteres()
        Assert.Equal(10, GeneradorClave.GenerarTemporal().Length)
    End Sub

    <Fact>
    Public Sub GenerarTemporal_NoUsaCaracteresAmbiguos()
        ' 0/O, 1/l/I se confunden al transcribir la clave a mano.
        Dim ambiguos = "0O1lI".ToCharArray()

        For i = 1 To 50
            Dim clave = GeneradorClave.GenerarTemporal()
            Assert.Empty(clave.ToCharArray().Intersect(ambiguos))
        Next
    End Sub

    <Fact>
    Public Sub GenerarTemporal_NoRepiteSiempreLaMisma()
        Dim claves = Enumerable.Range(1, 20).Select(Function(i) GeneradorClave.GenerarTemporal()).Distinct()

        Assert.True(claves.Count() > 1, "20 claves generadas no deberían salir todas iguales")
    End Sub
End Class
