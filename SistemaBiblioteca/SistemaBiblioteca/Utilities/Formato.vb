''' <summary>Formatos propios de Alejandría. Los que comparten los tres sistemas
''' —dinero, fechas, saludo— viven en Comun.Formato; aquí solo se reenvían,
''' igual que Validador.vb.</summary>
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

    Public Shared Function Fecha(valor As DateTime) As String
        Return valor.ToString("dd/MM/yyyy")
    End Function

    Public Shared Function FechaHora(valor As DateTime) As String
        Return Comun.Formato.FechaHora(valor)
    End Function

    Public Shared Function Hora(valor As DateTime) As String
        Return Comun.Formato.Hora(valor)
    End Function

    ''' <summary>-3 → "3 días de retraso"; 0 → "Vence hoy"; 5 → "En 5 días"</summary>
    Public Shared Function Plazo(diasRestantes As Integer) As String
        Return DiasRestantesATextoConverter.Formatear(diasRestantes)
    End Function

    ''' <summary>Correlativo con prefijo y ceros a la izquierda: PR-000042.</summary>
    Public Shared Function Correlativo(prefijo As String, numero As Integer,
                                       Optional ancho As Integer = 6) As String
        Return prefijo & numero.ToString(New String("0"c, ancho))
    End Function

    ''' <summary>Saludo según la hora del día, para la pantalla de inicio de sesión.</summary>
    Public Shared Function Saludo() As String
        Return Comun.Formato.Saludo()
    End Function
End Class
