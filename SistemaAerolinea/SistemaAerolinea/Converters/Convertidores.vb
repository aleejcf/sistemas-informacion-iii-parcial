Imports System.Globalization
Imports System.Windows.Data
Imports System.Windows.Media

''' <summary>Convierte el estado de un vuelo, una reserva o un boleto en el color
''' de su texto. Tener el mapa de colores en un solo lugar evita que cada vista
''' invente el suyo y que un estado se vea distinto según la pantalla.</summary>
Public Class EstadoAColorConverter
    Implements IValueConverter

    Public Shared Function Trazo(estado As String) As Brush
        Select Case If(estado, "").Trim()
            Case "Programado", "Emitido", "Pendiente de pago"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#0C7CD5"))
            Case "Abordando", "Check-in"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#7C3AED"))
            Case "En vuelo"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#0891B2"))
            Case "Aterrizado", "Confirmada", "Abordado", "Activo"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#15803D"))
            Case "Retrasado"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#B45309"))
            Case "Cancelado", "Cancelada", "Inactivo"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#B91C1C"))
            Case Else
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#64748B"))
        End Select
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
            Case "Programado", "Emitido", "Pendiente de pago"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#E3F0FC"))
            Case "Abordando", "Check-in"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#EDE9FE"))
            Case "En vuelo"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#CFFAFE"))
            Case "Aterrizado", "Confirmada", "Abordado", "Activo"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#DCFCE7"))
            Case "Retrasado"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#FEF3C7"))
            Case "Cancelado", "Cancelada", "Inactivo"
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#FEE2E2"))
            Case Else
                Return New SolidColorBrush(ColorConverter.ConvertFromString("#EEF2F7"))
        End Select
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

''' <summary>135 minutos se lee mejor como "2h 15m".</summary>
Public Class MinutosADuracionConverter
    Implements IValueConverter

    Public Shared Function Formatear(minutos As Integer) As String
        If minutos <= 0 Then Return "—"
        Dim horas = minutos \ 60
        Dim resto = minutos Mod 60
        If horas = 0 Then Return $"{resto}m"
        If resto = 0 Then Return $"{horas}h"
        Return $"{horas}h {resto}m"
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

''' <summary>Muestra un elemento solo cuando el texto asociado está vacío:
''' es lo que hace aparecer el texto guía dentro de las cajas de búsqueda.</summary>
Public Class VacioAVisibleConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object,
                            culture As CultureInfo) As Object Implements IValueConverter.Convert
        Dim texto = If(value, "").ToString()
        Return If(String.IsNullOrEmpty(texto), Visibility.Visible, Visibility.Collapsed)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object,
                                culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class
