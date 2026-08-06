Imports System.Globalization
Imports System.Windows.Data
Imports System.Windows.Media

''' <summary>Convierte el estado de un ejemplar, un préstamo o una multa en el
''' color de su texto. Tener el mapa de colores en un solo lugar evita que cada
''' vista invente el suyo y que un mismo estado se vea distinto según la pantalla.</summary>
Public Class EstadoAColorConverter
    Implements IValueConverter

    Public Shared Function Trazo(estado As String) As Brush
        Select Case If(estado, "").Trim()
            Case "Disponible", "Devuelto", "Pagada", "Activa", "Atendida", "Nuevo", "Bueno"
                Return Pincel("#15803D")
            Case "Activo", "Al día"
                Return Pincel("#1B7A52")
            Case "Por vencer", "Pendiente", "Prestado", "Reparación", "Regular"
                Return Pincel("#B45309")
            Case "Vencido", "Vencida", "Extraviado", "Deteriorado",
                 "Retraso", "Daño", "Extravío", "Sin ejemplares", "Con bloqueo"
                Return Pincel("#B91C1C")
            Case "Cancelado", "Cancelada", "Baja", "Condonada", "Inactivo", "No circula"
                Return Pincel("#6E7D74")
            Case Else
                Return Pincel("#6E7D74")
        End Select
    End Function

    Private Shared Function Pincel(hex As String) As Brush
        Dim brocha As New SolidColorBrush(ColorConverter.ConvertFromString(hex))
        ' Congelar el pincel evita recrearlo en cada refresco de una tabla larga
        brocha.Freeze()
        Return brocha
    End Function

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return Trazo(If(value, "").ToString())
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

''' <summary>El fondo suave que acompaña al color del estado en las insignias.</summary>
Public Class EstadoAColorFondoConverter
    Implements IValueConverter

    Public Shared Function Relleno(estado As String) As Brush
        Select Case If(estado, "").Trim()
            Case "Disponible", "Devuelto", "Pagada", "Activa", "Atendida", "Nuevo", "Bueno"
                Return Pincel("#DCFCE7")
            Case "Activo", "Al día"
                Return Pincel("#E2F1E9")
            Case "Por vencer", "Pendiente", "Prestado", "Reparación", "Regular"
                Return Pincel("#FEF3C7")
            Case "Vencido", "Vencida", "Extraviado", "Deteriorado",
                 "Retraso", "Daño", "Extravío", "Sin ejemplares", "Con bloqueo"
                Return Pincel("#FEE2E2")
            Case Else
                Return Pincel("#EDEFEA")
        End Select
    End Function

    Private Shared Function Pincel(hex As String) As Brush
        Dim brocha As New SolidColorBrush(ColorConverter.ConvertFromString(hex))
        brocha.Freeze()
        Return brocha
    End Function

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return Relleno(If(value, "").ToString())
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

''' <summary>Muestra un elemento solo cuando el texto asociado está vacío: es lo
''' que hace aparecer el texto guía dentro de las cajas de búsqueda. Con
''' ConverterParameter="Invertir" hace lo contrario, que es como se muestra
''' "prestado a Fulano" solo en los ejemplares que sí están prestados.</summary>
Public Class VacioAVisibleConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        ' IsDBNull importa aquí: una columna NULL de SQL no es cadena vacía
        Dim texto = If(value Is Nothing OrElse IsDBNull(value), "", value.ToString())
        Dim vacio = String.IsNullOrWhiteSpace(texto)

        If parameter IsNot Nothing AndAlso
           String.Equals(parameter.ToString(), "Invertir", StringComparison.OrdinalIgnoreCase) Then
            vacio = Not vacio
        End If

        Return If(vacio, Visibility.Visible, Visibility.Collapsed)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

''' <summary>Traduce un porcentaje (0-100) al ancho en píxeles de una barra.
''' El ancho total de la barra se pasa como ConverterParameter.</summary>
Public Class PorcentajeAAnchoConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        If value Is Nothing OrElse IsDBNull(value) Then Return 0.0

        Dim porcentaje = Math.Max(0, Math.Min(100, CDbl(value)))
        Dim total As Double = 100
        If parameter IsNot Nothing Then Double.TryParse(parameter.ToString(),
            NumberStyles.Any, CultureInfo.InvariantCulture, total)

        Return porcentaje / 100.0 * total
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

''' <summary>Booleano a visibilidad. Con ConverterParameter="Invertir" hace lo
''' contrario, que es como se oculta el botón de prestar a un socio moroso.</summary>
Public Class BooleanoAVisibleConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        Dim verdadero = value IsNot Nothing AndAlso Not IsDBNull(value) AndAlso CBool(value)
        If parameter IsNot Nothing AndAlso
           String.Equals(parameter.ToString(), "Invertir", StringComparison.OrdinalIgnoreCase) Then
            verdadero = Not verdadero
        End If
        Return If(verdadero, Visibility.Visible, Visibility.Collapsed)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

''' <summary>Los días que faltan para vencer, dichos como los diría una persona:
''' "vence hoy", "en 3 días", "5 días de retraso".</summary>
Public Class DiasRestantesATextoConverter
    Implements IValueConverter

    Public Shared Function Formatear(dias As Integer) As String
        If dias < -1 Then Return $"{Math.Abs(dias)} días de retraso"
        If dias = -1 Then Return "1 día de retraso"
        If dias = 0 Then Return "Vence hoy"
        If dias = 1 Then Return "Vence mañana"
        Return $"En {dias} días"
    End Function

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        If value Is Nothing OrElse IsDBNull(value) Then Return "—"
        Return Formatear(CInt(value))
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

''' <summary>Muestra un elemento solo cuando el número asociado es mayor que cero:
''' así la insignia de "3 reservas" no aparece en los títulos que no tienen ninguna.</summary>
Public Class ConteoAVisibleConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        If value Is Nothing OrElse IsDBNull(value) Then Return Visibility.Collapsed
        Dim numero As Double
        If Not Double.TryParse(value.ToString(), NumberStyles.Any,
                               CultureInfo.InvariantCulture, numero) Then Return Visibility.Collapsed
        Return If(numero > 0, Visibility.Visible, Visibility.Collapsed)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class
