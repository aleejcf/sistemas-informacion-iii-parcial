''' <summary>La generación de claves temporales es idéntica en los tres sistemas y
''' vive en Comun.GeneradorClave; esto solo reenvía, igual que Validador.vb.</summary>
Public Class GeneradorClave

    ''' <summary>Contraseña aleatoria de 10 caracteres que ya cumple las reglas
    ''' de Validador.ValidarContrasena (letras y números).</summary>
    Public Shared Function GenerarTemporal() As String
        Return Comun.GeneradorClave.GenerarTemporal()
    End Function
End Class
