''' <summary>Traduce las excepciones técnicas a mensajes que el usuario entiende.
''' La lógica vive en Comun.MensajeError; aquí solo se fija el mensaje propio de
''' ALAS para cuando dos personas chocan reservando el mismo asiento.</summary>
Public Class MensajeError

    Private Const MENSAJE_DUPLICADO As String =
        "Ya existe un registro con esos datos. " &
        "Si estabas reservando, es probable que otro agente haya tomado ese asiento."

    Public Shared Function Traducir(contexto As String, ex As Exception) As String
        Return Comun.MensajeError.Traducir(contexto, ex, MENSAJE_DUPLICADO)
    End Function

    Public Shared Function Describir(ex As Exception) As String
        Return Comun.MensajeError.Describir(ex, MENSAJE_DUPLICADO)
    End Function
End Class
