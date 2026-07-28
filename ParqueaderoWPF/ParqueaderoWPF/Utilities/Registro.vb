Imports System.IO

''' <summary>Bitácora del sistema. Los detalles técnicos de los errores se guardan
''' en un archivo de texto; al usuario solo se le muestra un mensaje entendible.</summary>
Public Class Registro

    Private Shared ReadOnly Candado As New Object()

    Public Shared ReadOnly Property RutaArchivo As String
        Get
            Dim carpeta = Path.Combine(AppContext.BaseDirectory, "logs")
            Directory.CreateDirectory(carpeta)
            Return Path.Combine(carpeta, $"parko_{DateTime.Now:yyyy-MM-dd}.log")
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
                Dim usuario = If(Sesion.UsuarioActual?.NombreUsuario, "-")
                File.AppendAllText(RutaArchivo,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{nivel}] [{usuario}] {mensaje}{Environment.NewLine}")
            End SyncLock
        Catch
            ' Si no se puede escribir la bitácora, la aplicación no debe fallar por eso
        End Try
    End Sub
End Class
