Imports System.Text.RegularExpressions

''' <summary>Reglas de validación puras: no tocan la base de datos ni la interfaz,
''' por eso se pueden probar con pruebas unitarias.</summary>
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

    ''' <summary>Código de pasajero: la letra P seguida de 7 dígitos (P0000001).</summary>
    Public Shared Function EsCodigoPasajeroValido(codigo As String) As Boolean
        If String.IsNullOrWhiteSpace(codigo) Then Return False
        Return Regex.IsMatch(codigo.Trim().ToUpper(), "^P\d{7}$")
    End Function

    ''' <summary>Código IATA de aeropuerto: exactamente 3 letras (TGU, SAP, MIA).</summary>
    Public Shared Function EsIataValido(iata As String) As Boolean
        If String.IsNullOrWhiteSpace(iata) Then Return False
        Return Regex.IsMatch(iata.Trim().ToUpper(), "^[A-Z]{3}$")
    End Function

    ''' <summary>Localizador de reserva: 6 caracteres alfanuméricos en mayúscula.</summary>
    Public Shared Function EsPnrValido(pnr As String) As Boolean
        If String.IsNullOrWhiteSpace(pnr) Then Return False
        Return Regex.IsMatch(pnr.Trim().ToUpper(), "^[A-Z0-9]{6}$")
    End Function

    ''' <summary>Un pasajero no puede haber nacido en el futuro ni tener más de 120 años.</summary>
    Public Shared Function ValidarFechaNacimiento(fecha As Date?) As String
        If Not fecha.HasValue Then Return "Selecciona la fecha de nacimiento."
        If fecha.Value.Date >= Date.Today Then Return "La fecha de nacimiento no puede ser hoy ni una fecha futura."
        If fecha.Value.Date < Date.Today.AddYears(-120) Then Return "Revisa la fecha de nacimiento: no parece válida."
        Return Nothing
    End Function

    ''' <summary>Un vuelo tiene que llegar después de salir, y su duración debe ser
    ''' razonable (entre 15 minutos y 20 horas).</summary>
    Public Shared Function ValidarHorarioVuelo(salida As DateTime, llegada As DateTime) As String
        If llegada <= salida Then Return "La llegada debe ser posterior a la salida."

        Dim minutos = (llegada - salida).TotalMinutes
        If minutos < 15 Then Return "Un vuelo no puede durar menos de 15 minutos."
        If minutos > 20 * 60 Then Return "Un vuelo no puede durar más de 20 horas."
        Return Nothing
    End Function
End Class
