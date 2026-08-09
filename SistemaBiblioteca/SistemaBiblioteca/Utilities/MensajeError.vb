''' <summary>Traduce las excepciones técnicas a mensajes que el usuario entiende.
''' La lógica vive en Comun.MensajeError; aquí solo se fija el mensaje propio de
''' Alejandría para cuando dos bibliotecarios chocan prestando el mismo ejemplar
''' (el índice único filtrado UQ_ejemplar_en_prestamo).</summary>
Public Class MensajeError

    Private Const MENSAJE_DUPLICADO As String =
        "Ya existe un registro con esos datos. " &
        "Si estabas prestando, es probable que otro bibliotecario haya " &
        "entregado ese mismo ejemplar hace un momento."

    Public Shared Function Traducir(contexto As String, ex As Exception) As String
        Return Comun.MensajeError.Traducir(contexto, ex, MENSAJE_DUPLICADO)
    End Function

    Public Shared Function Describir(ex As Exception) As String
        Return Comun.MensajeError.Describir(ex, MENSAJE_DUPLICADO)
    End Function
End Class
