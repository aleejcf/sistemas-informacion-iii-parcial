''' <summary>Traduce las excepciones técnicas a mensajes que el usuario entiende.
''' La lógica vive en Comun.MensajeError; PARKO no tiene un mensaje propio para
''' el caso de duplicado, así que usa el genérico.
'''
''' Al unificarse con ALAS y Alejandría, PARKO gana dos casos que antes no
''' tenía: el interbloqueo de SQL Server (código 1205) y un mensaje específico
''' para InvalidOperationException en vez del genérico de "error inesperado".
''' Ninguna prueba dependía de la redacción anterior de esos dos casos.</summary>
Public Class MensajeError

    Private Const MENSAJE_DUPLICADO As String =
        "Ya existe un registro con esos datos. Revisa el código o los campos únicos."

    Public Shared Function Traducir(contexto As String, ex As Exception) As String
        Return Comun.MensajeError.Traducir(contexto, ex, MENSAJE_DUPLICADO)
    End Function

    Public Shared Function Describir(ex As Exception) As String
        Return Comun.MensajeError.Describir(ex, MENSAJE_DUPLICADO)
    End Function
End Class
