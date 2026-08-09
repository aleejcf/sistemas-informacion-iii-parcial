''' <summary>Formatos propios de ALAS. Los que comparten los tres sistemas —dinero,
''' fechas, saludo— viven en Comun.Formato; aquí solo se reenvían, igual que
''' Validador.vb.</summary>
Public Class Formato

    Public Shared ReadOnly Property Cultura As Globalization.CultureInfo
        Get
            Return Comun.Formato.Cultura
        End Get
    End Property

    ''' <summary>L 1,234.56</summary>
    Public Shared Function Dinero(valor As Decimal) As String
        Return Comun.Formato.Dinero(valor)
    End Function

    Public Shared Function Dinero(valor As Object) As String
        Return Comun.Formato.Dinero(valor)
    End Function

    ''' <summary>domingo, 02 de agosto de 2026</summary>
    Public Shared Function FechaLarga(valor As DateTime) As String
        Return Comun.Formato.FechaLarga(valor)
    End Function

    Public Shared Function FechaHora(valor As DateTime) As String
        Return Comun.Formato.FechaHora(valor)
    End Function

    Public Shared Function Hora(valor As DateTime) As String
        Return Comun.Formato.Hora(valor)
    End Function

    ''' <summary>135 → "2h 15m"</summary>
    Public Shared Function Duracion(minutos As Integer) As String
        Return MinutosADuracionConverter.Formatear(minutos)
    End Function

    ''' <summary>Tapa un correo dejando solo lo justo para reconocerlo:
    ''' `alejandro@gmail.com` queda como `a•••••••o@gmail.com`.
    '''
    ''' Se usa al recuperar una cuenta. Hay que enseñar a dónde va a llegar el
    ''' código para que su dueño sepa dónde buscarlo, pero enseñarlo entero
    ''' convertiría la pantalla en un buscador de correos ajenos: bastaría con
    ''' probar nombres de usuario para ir cosechando direcciones.</summary>
    Public Shared Function CorreoOculto(correo As String) As String
        If String.IsNullOrWhiteSpace(correo) Then Return ""

        Dim arroba = correo.IndexOf("@"c)
        If arroba <= 0 Then Return "•••"

        Dim usuario = correo.Substring(0, arroba)
        Dim dominio = correo.Substring(arroba)

        ' Con uno o dos caracteres no hay nada que tapar sin borrarlo entero
        If usuario.Length <= 2 Then Return New String("•"c, 3) & dominio

        Return usuario(0) & New String("•"c, Math.Min(usuario.Length - 2, 8)) &
               usuario(usuario.Length - 1) & dominio
    End Function

    ''' <summary>Saludo según la hora del día, para la pantalla de inicio de sesión.</summary>
    Public Shared Function Saludo() As String
        Return Comun.Formato.Saludo()
    End Function
End Class
