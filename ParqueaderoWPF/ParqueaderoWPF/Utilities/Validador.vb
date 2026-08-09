''' <summary>PARKO no tiene reglas de validación propias: las que usa —correo,
''' usuario, contraseña— son justo las que comparten los tres sistemas y viven
''' en Comun.Validador. Esta clase solo reenvía, para que el resto del código
''' no tenga que escribir "Comun." delante en cada llamada.</summary>
Public Class Validador

    Public Shared Function EsEmailValido(email As String) As Boolean
        Return Comun.Validador.EsEmailValido(email)
    End Function

    Public Shared Function ProblemaDelEmail(email As String) As String
        Return Comun.Validador.ProblemaDelEmail(email)
    End Function

    ''' <summary>4 a 30 caracteres: letras, números o guion bajo.</summary>
    Public Shared Function EsUsuarioValido(usuario As String) As Boolean
        Return Comun.Validador.EsUsuarioValido(usuario)
    End Function

    ''' <summary>Devuelve Nothing si la contraseña es válida; si no, el mensaje de error.</summary>
    Public Shared Function ValidarContrasena(clave As String) As String
        Return Comun.Validador.ValidarContrasena(clave)
    End Function
End Class
