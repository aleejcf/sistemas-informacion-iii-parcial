Imports System.IO
Imports Xunit

''' <summary>Pruebas de la traducción de excepciones a mensajes entendibles. El
''' mensaje del caso "ya existe un registro" es distinto por sistema y se prueba
''' con un texto de ejemplo, no con el de ninguno en particular.</summary>
Public Class MensajeErrorTests

    <Fact>
    Public Sub Describir_NoRevelaDetallesTecnicos()
        Dim mensaje = MensajeError.Describir(New Exception("Object reference not set to an instance"))

        Assert.DoesNotContain("Object reference", mensaje)
        Assert.DoesNotContain("Exception", mensaje)
        Assert.Contains("bitácora", mensaje)
    End Sub

    <Fact>
    Public Sub Describir_ExplicaLosProblemasDeArchivo()
        Dim mensaje = MensajeError.Describir(New IOException("archivo en uso"))
        Assert.Contains("archivo", mensaje.ToLower())
    End Sub

    <Fact>
    Public Sub Describir_ExplicaLasOperacionesInvalidas()
        Dim mensaje = MensajeError.Describir(New InvalidOperationException("estado inválido"))
        Assert.Contains("Revisa los datos", mensaje)
    End Sub

    <Fact>
    Public Sub Describir_SiempreDevuelveAlgoQueMostrar()
        For Each ex As Exception In {New Exception(), New InvalidOperationException(),
                                     New UnauthorizedAccessException(), New ArgumentException()}
            Assert.False(String.IsNullOrWhiteSpace(MensajeError.Describir(ex)))
        Next
    End Sub

    <Fact>
    Public Sub Traducir_GuardaElDetalleEnLaBitacora()
        Registro.Configurar("prueba_mensajeerror_" & Guid.NewGuid().ToString("N").Substring(0, 8))
        Dim marca = "detalle-unico-" & Guid.NewGuid().ToString("N").Substring(0, 8)

        MensajeError.Traducir("Prueba de bitácora", New Exception(marca))

        Assert.True(File.Exists(Registro.RutaArchivo), "Debe crearse el archivo de bitácora")
        Assert.Contains(marca, File.ReadAllText(Registro.RutaArchivo))
    End Sub
End Class
