Imports System.IO
Imports Xunit

''' <summary>Verifica que los detalles técnicos de los errores nunca lleguen a la pantalla
''' del usuario y que sí queden registrados en la bitácora (recomendación OWASP).</summary>
Public Class MensajeErrorTests

    <Fact>
    Public Sub Traducir_NoFiltraElMensajeTecnicoAlUsuario()
        Dim detalleInterno = "Column 'contrasena_hash' at Server=ALECALDE\SQLEXPRESS"
        Dim ex As New InvalidOperationException(detalleInterno)

        Dim mensaje = MensajeError.Traducir("Prueba", ex)

        Assert.DoesNotContain(detalleInterno, mensaje)
        Assert.DoesNotContain("ALECALDE", mensaje)
        Assert.DoesNotContain("contrasena_hash", mensaje)
    End Sub

    <Fact>
    Public Sub Traducir_DevuelveUnMensajeEntendible()
        Dim mensaje = MensajeError.Traducir("Prueba", New Exception("boom"))

        Assert.False(String.IsNullOrWhiteSpace(mensaje))
        Assert.True(mensaje.Length > 20, "El mensaje debe explicar algo al usuario")
        Assert.DoesNotContain("Exception", mensaje)
    End Sub

    <Fact>
    Public Sub Traducir_ErrorDeArchivo_DaMensajeEspecifico()
        Dim mensaje = MensajeError.Traducir("Copiar foto", New IOException("file locked"))

        Assert.Contains("archivo", mensaje, StringComparison.OrdinalIgnoreCase)
        Assert.DoesNotContain("file locked", mensaje)
    End Sub

    <Fact>
    Public Sub Traducir_GuardaElDetalleEnLaBitacora()
        Dim marca = "detalle-unico-" & Guid.NewGuid().ToString("N").Substring(0, 8)

        MensajeError.Traducir("Prueba de bitácora", New Exception(marca))

        Assert.True(File.Exists(Registro.RutaArchivo), "Debe crearse el archivo de bitácora")
        Dim contenido = File.ReadAllText(Registro.RutaArchivo)
        Assert.Contains(marca, contenido)
    End Sub
End Class
