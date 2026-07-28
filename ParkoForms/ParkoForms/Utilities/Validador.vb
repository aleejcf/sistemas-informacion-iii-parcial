Imports System.Text.RegularExpressions

Public Class Validador

    Public Shared Function EsEmailValido(email As String) As Boolean
        If String.IsNullOrWhiteSpace(email) Then Return False
        Return Regex.IsMatch(email.Trim(), "^[^@\s]+@[^@\s]+\.[^@\s]+$")
    End Function

    ''' <summary>4 a 30 caracteres: letras, números o guion bajo.</summary>
    Public Shared Function EsUsuarioValido(usuario As String) As Boolean
        If String.IsNullOrWhiteSpace(usuario) Then Return False
        Return Regex.IsMatch(usuario.Trim(), "^[a-zA-Z0-9_]{4,30}$")
    End Function

    ''' <summary>Devuelve Nothing si la contraseña es válida; si no, el mensaje de error.</summary>
    Public Shared Function ValidarContrasena(clave As String) As String
        If String.IsNullOrEmpty(clave) OrElse clave.Length < 6 Then
            Return "La contraseña debe tener al menos 6 caracteres."
        End If
        If Not Regex.IsMatch(clave, "[A-Za-z]") OrElse Not Regex.IsMatch(clave, "[0-9]") Then
            Return "La contraseña debe combinar letras y números."
        End If
        Return Nothing
    End Function
End Class
