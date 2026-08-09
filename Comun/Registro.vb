Imports System.IO

''' <summary>Bitácora en archivo, compartida por los tres sistemas. Los detalles
''' técnicos de un error se guardan aquí; al usuario solo se le muestra un
''' mensaje entendible (recomendación OWASP: no exponer información interna en
''' pantalla).
'''
''' El nombre del archivo y quién aparece como autor de cada línea son distintos
''' en cada sistema —el prefijo del log, y de dónde sale el nombre del usuario
''' logueado, que en PARKO se lee distinto que en ALAS y Alejandría—. Por eso
''' cada aplicación llama a Configurar() una sola vez, al arrancar.</summary>
Public Class Registro

    Private Shared ReadOnly Candado As New Object()

    Private Shared prefijo As String = "sistema"
    Private Shared obtenerUsuarioActual As Func(Of String) = Function() "-"

    ''' <summary>Se llama una sola vez, al arrancar la aplicación (en el evento
    ''' Startup de Application.xaml.vb).</summary>
    Public Shared Sub Configurar(prefijoArchivo As String, Optional obtenerUsuario As Func(Of String) = Nothing)
        prefijo = prefijoArchivo
        If obtenerUsuario IsNot Nothing Then obtenerUsuarioActual = obtenerUsuario
    End Sub

    Public Shared ReadOnly Property RutaArchivo As String
        Get
            Dim carpeta = Path.Combine(AppContext.BaseDirectory, "logs")
            Directory.CreateDirectory(carpeta)
            Return Path.Combine(carpeta, $"{prefijo}_{DateTime.Now:yyyy-MM-dd}.log")
        End Get
    End Property

    Public Shared Sub Info(mensaje As String)
        Escribir("INFO", mensaje)
    End Sub

    Public Shared Sub Advertencia(mensaje As String)
        Escribir("ADVERTENCIA", mensaje)
    End Sub

    Public Shared Sub Error_(contexto As String, ex As Exception)
        Escribir("ERROR", $"{contexto} — {ex.GetType().Name}: {ex.Message}")
        If ex.StackTrace IsNot Nothing Then Escribir("ERROR", "   " & ex.StackTrace)
    End Sub

    Private Shared Sub Escribir(nivel As String, mensaje As String)
        Try
            SyncLock Candado
                File.AppendAllText(RutaArchivo,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{nivel}] [{obtenerUsuarioActual()}] {mensaje}{Environment.NewLine}")
            End SyncLock
        Catch
            ' Si no se puede escribir la bitácora, la aplicación no debe fallar por eso
        End Try
    End Sub
End Class
