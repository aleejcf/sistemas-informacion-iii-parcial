Imports Microsoft.Data.SqlClient

''' <summary>Traduce las excepciones técnicas a mensajes que el usuario entiende.
''' El detalle real queda en la bitácora, no en pantalla (recomendación OWASP).</summary>
Public Class MensajeError

    ''' <summary>Registra el error en la bitácora y devuelve un mensaje amigable.</summary>
    Public Shared Function Traducir(contexto As String, ex As Exception) As String
        Registro.Error_(contexto, ex)

        Dim exSql = TryCast(ex, SqlException)
        If exSql IsNot Nothing Then
            Select Case exSql.Number
                Case 2, 53, 258, -2, 10060, 10061
                    Return "No se pudo conectar con la base de datos. " &
                           "Verifica que SQL Server esté encendido e inténtalo de nuevo."
                Case 18456
                    Return "La base de datos rechazó las credenciales de conexión. " &
                           "Avisa al administrador del sistema."
                Case 2627, 2601
                    Return "Ya existe un registro con esos datos. Revisa el código o los campos únicos."
                Case 547
                    Return "No se puede completar la operación porque el registro está " &
                           "relacionado con otros datos del sistema."
                Case 8152, 2628
                    Return "Uno de los datos es más largo de lo permitido. Acórtalo e inténtalo de nuevo."
                Case 515
                    Return "Falta completar un campo obligatorio."
            End Select
            Return "Ocurrió un problema con la base de datos. El detalle quedó guardado en la bitácora."
        End If

        If TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is IO.IOException Then
            Return "No se pudo acceder al archivo. Verifica que no esté abierto en otro programa."
        End If

        Return "Ocurrió un error inesperado. El detalle quedó guardado en la bitácora del sistema."
    End Function
End Class
