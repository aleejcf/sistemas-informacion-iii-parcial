Imports System.Globalization

''' <summary>Formatos que el sistema usa en todas partes: dinero en lempiras,
''' fechas en español de Honduras y duraciones de vuelo.</summary>
Public Class Formato

    Public Shared ReadOnly Cultura As New CultureInfo("es-HN")

    ''' <summary>L 1,234.56</summary>
    Public Shared Function Dinero(valor As Decimal) As String
        Return "L " & valor.ToString("N2", CultureInfo.InvariantCulture)
    End Function

    Public Shared Function Dinero(valor As Object) As String
        If valor Is Nothing OrElse IsDBNull(valor) Then Return "L 0.00"
        Return Dinero(CDec(valor))
    End Function

    ''' <summary>domingo, 02 de agosto de 2026</summary>
    Public Shared Function FechaLarga(valor As DateTime) As String
        Return valor.ToString("dddd, dd 'de' MMMM 'de' yyyy", Cultura)
    End Function

    Public Shared Function FechaHora(valor As DateTime) As String
        Return valor.ToString("dd/MM/yyyy HH:mm")
    End Function

    Public Shared Function Hora(valor As DateTime) As String
        Return valor.ToString("HH:mm")
    End Function

    ''' <summary>135 → "2h 15m"</summary>
    Public Shared Function Duracion(minutos As Integer) As String
        Return MinutosADuracionConverter.Formatear(minutos)
    End Function

    ''' <summary>Saludo según la hora del día, para la pantalla de inicio de sesión.</summary>
    Public Shared Function Saludo() As String
        Dim hora = DateTime.Now.Hour
        If hora >= 5 AndAlso hora < 12 Then Return "¡Buenos días!"
        If hora >= 12 AndAlso hora < 19 Then Return "¡Buenas tardes!"
        Return "¡Buenas noches!"
    End Function
End Class
