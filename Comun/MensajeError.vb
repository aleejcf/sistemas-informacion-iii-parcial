Imports Microsoft.Data.SqlClient

''' <summary>Traduce las excepciones técnicas a mensajes que el usuario entiende.
''' El detalle real queda en la bitácora, no en pantalla (recomendación OWASP:
''' un mensaje de error no debe revelar la estructura interna del sistema).
'''
''' El único caso que varía de un sistema a otro es el de "ya existe un
''' registro con esos datos" (código 2627/2601): en ALAS significa que otro
''' agente tomó el asiento, en Alejandría que otro bibliotecario entregó el
''' ejemplar. Por eso ese mensaje se recibe como parámetro en vez de estar
''' escrito aquí.</summary>
Public Class MensajeError

    Private Const MENSAJE_DUPLICADO_POR_DEFECTO As String =
        "Ya existe un registro con esos datos. Revisa el código o los campos únicos."

    ''' <summary>Registra el error en la bitácora y devuelve el mensaje a mostrar.</summary>
    Public Shared Function Traducir(contexto As String, ex As Exception,
                                    Optional mensajeDuplicado As String = MENSAJE_DUPLICADO_POR_DEFECTO) As String
        Registro.Error_(contexto, ex)
        Return Describir(ex, mensajeDuplicado)
    End Function

    ''' <summary>La traducción propiamente dicha, sin efectos secundarios:
    ''' se puede probar con pruebas unitarias.</summary>
    Public Shared Function Describir(ex As Exception,
                                     Optional mensajeDuplicado As String = MENSAJE_DUPLICADO_POR_DEFECTO) As String
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
                    Return mensajeDuplicado
                Case 547
                    Return "No se puede completar la operación porque el registro está " &
                           "relacionado con otros datos del sistema."
                Case 8152, 2628
                    Return "Uno de los datos es más largo de lo permitido. Acórtalo e inténtalo de nuevo."
                Case 515
                    Return "Falta completar un campo obligatorio."
                Case 1205
                    Return "Otra operación estaba usando los mismos datos al mismo tiempo. " &
                           "Vuelve a intentarlo."
            End Select
            Return "Ocurrió un problema con la base de datos. El detalle quedó guardado en la bitácora."
        End If

        If TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is IO.IOException Then
            Return "No se pudo acceder al archivo. Verifica que no esté abierto en otro programa."
        End If

        If TypeOf ex Is InvalidOperationException Then
            Return "La operación no se pudo completar en este momento. Revisa los datos e inténtalo de nuevo."
        End If

        Return "Ocurrió un error inesperado. El detalle quedó guardado en la bitácora del sistema."
    End Function
End Class
