Imports System.IO
Imports Xunit

''' <summary>Cada prueba usa su propio prefijo (Configurar cambia estado
''' compartido) para no pisar el archivo de otra prueba que corra al mismo
''' tiempo: el archivo de log se comparte por fecha, no por prueba.</summary>
Public Class RegistroTests

    Private Shared Function PrefijoDePrueba() As String
        Return "prueba_" & Guid.NewGuid().ToString("N").Substring(0, 8)
    End Function

    <Fact>
    Public Sub RutaArchivo_UsaElPrefijoConfigurado()
        Dim prefijo = PrefijoDePrueba()
        Registro.Configurar(prefijo)

        Assert.StartsWith(prefijo & "_", Path.GetFileName(Registro.RutaArchivo))
    End Sub

    <Fact>
    Public Sub Info_EscribeElMensajeYQuienLoGenero()
        Dim prefijo = PrefijoDePrueba()
        Registro.Configurar(prefijo, Function() "usuario-de-prueba")

        Dim marca = "marca-" & Guid.NewGuid().ToString("N").Substring(0, 8)
        Registro.Info(marca)

        Dim contenido = File.ReadAllText(Registro.RutaArchivo)
        Assert.Contains(marca, contenido)
        Assert.Contains("usuario-de-prueba", contenido)
        Assert.Contains("[INFO]", contenido)
    End Sub

    <Fact>
    Public Sub Advertencia_QuedaMarcadaComoTal()
        Dim prefijo = PrefijoDePrueba()
        Registro.Configurar(prefijo)

        Dim marca = "marca-" & Guid.NewGuid().ToString("N").Substring(0, 8)
        Registro.Advertencia(marca)

        Assert.Contains("[ADVERTENCIA]", File.ReadAllText(Registro.RutaArchivo))
    End Sub

    <Fact>
    Public Sub Error_GuardaElTipoYElMensajeDeLaExcepcion()
        Dim prefijo = PrefijoDePrueba()
        Registro.Configurar(prefijo)

        Dim marca = "marca-" & Guid.NewGuid().ToString("N").Substring(0, 8)
        Registro.Error_("Contexto de prueba", New InvalidOperationException(marca))

        Dim contenido = File.ReadAllText(Registro.RutaArchivo)
        Assert.Contains("[ERROR]", contenido)
        Assert.Contains("Contexto de prueba", contenido)
        Assert.Contains(marca, contenido)
    End Sub

    <Fact>
    Public Sub SinConfigurarQuienEscribe_UsaUnGuion()
        ' Simula el arranque, antes de que haya sesión iniciada.
        Dim prefijo = PrefijoDePrueba()
        Registro.Configurar(prefijo, Function() "-")

        Dim marca = "marca-" & Guid.NewGuid().ToString("N").Substring(0, 8)
        Registro.Info(marca)

        Dim contenido = File.ReadAllText(Registro.RutaArchivo)
        Assert.Contains("[-]", contenido)
    End Sub
End Class
