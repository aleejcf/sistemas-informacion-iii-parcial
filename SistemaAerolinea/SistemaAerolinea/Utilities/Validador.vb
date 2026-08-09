Imports System.Text.RegularExpressions

''' <summary>Reglas de validación propias de ALAS. Las que comparten los tres
''' sistemas —correo, usuario, contraseña— viven en Comun.Validador; aquí solo
''' se reenvían para que el resto del código no tenga que escribir "Comun."
''' delante en cada llamada.</summary>
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
