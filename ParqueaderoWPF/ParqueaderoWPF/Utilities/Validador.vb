Imports System.Text.RegularExpressions

Public Class Validador

    Public Shared Function EsEmailValido(email As String) As Boolean
        Return ProblemaDelEmail(email) Is Nothing
    End Function

    ''' <summary>Dominios que la gente teclea mal, con su forma correcta.
    '''
    ''' Un correo con el dominio mal escrito PASA cualquier validación de formato
    ''' —`gmail.con` tiene arroba y punto— pero no existe, así que el código de
    ''' recuperación no llega nunca y su dueño se queda fuera sin entender por qué.
    ''' Pasó de verdad con una cuenta, y esa persona no habría podido recuperarla.</summary>
    Private Shared ReadOnly DominiosMalEscritos As New Dictionary(Of String, String)(
        StringComparer.OrdinalIgnoreCase) From {
        {"gmail.con", "gmail.com"}, {"gmail.cm", "gmail.com"}, {"gmail.om", "gmail.com"},
        {"gmail.co", "gmail.com"}, {"gmail.cmo", "gmail.com"}, {"gmail.ocm", "gmail.com"},
        {"gmial.com", "gmail.com"}, {"gamil.com", "gmail.com"}, {"gmai.com", "gmail.com"},
        {"gmail.vom", "gmail.com"}, {"gmaill.com", "gmail.com"},
        {"hotmail.con", "hotmail.com"}, {"hotmial.com", "hotmail.com"},
        {"hotmail.cm", "hotmail.com"}, {"hotmail.co", "hotmail.com"},
        {"outlook.con", "outlook.com"}, {"outlok.com", "outlook.com"},
        {"outlook.cm", "outlook.com"},
        {"yahoo.con", "yahoo.com"}, {"yahooo.com", "yahoo.com"}, {"yaho.com", "yahoo.com"},
        {"icloud.con", "icloud.com"}, {"iclod.com", "icloud.com"}
    }

    ''' <summary>Devuelve Nothing si el correo sirve, o el motivo por el que no.
    '''
    ''' Comprueba el formato y además los dominios que se teclean mal a menudo. NO
    ''' comprueba que el buzón exista —eso solo se sabe mandándole algo— pero sí
    ''' ataja los errores que hacen que un correo con buena pinta no llegue a
    ''' ninguna parte.</summary>
    Public Shared Function ProblemaDelEmail(email As String) As String
        If String.IsNullOrWhiteSpace(email) Then Return "Escribe un correo electrónico."

        Dim limpio = email.Trim()

        If limpio.Contains(" ") Then Return "El correo no puede llevar espacios."
        If Not Regex.IsMatch(limpio, "^[^@\s]+@[^@\s]+\.[^@\s]+$") Then
            Return "El correo electrónico no es válido. Revisa que tenga la forma nombre@dominio.com"
        End If

        Dim dominio = limpio.Substring(limpio.IndexOf("@"c) + 1)

        Dim correcto As String = Nothing
        If DominiosMalEscritos.TryGetValue(dominio, correcto) Then
            Dim usuario = limpio.Substring(0, limpio.IndexOf("@"c))
            Return $"El dominio «{dominio}» no existe, así que a ese correo no llegaría nada. " &
                   $"¿Quisiste decir {usuario}@{correcto}?"
        End If

        ' Un dominio que termina en punto, o cuya última parte es de una sola letra,
        ' no puede corresponder a ningún dominio real
        Dim ultima = dominio.Substring(dominio.LastIndexOf("."c) + 1)
        If ultima.Length < 2 Then
            Return $"«{dominio}» no parece un dominio real: revisa lo que va después del último punto."
        End If

        Return Nothing
    End Function

    ''' <summary>4 a 30 caracteres: letras, números o guion bajo.</summary>

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
