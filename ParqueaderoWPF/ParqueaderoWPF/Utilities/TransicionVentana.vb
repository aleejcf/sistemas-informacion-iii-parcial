Imports System.Windows.Media.Animation

''' <summary>Fundido de entrada reutilizable: funciona tanto para una ventana completa
''' como para el contenido que se muestra dentro de ella (ej. al cambiar de página).</summary>
Public Class TransicionVentana

    Public Shared Sub FundirEntrada(elemento As UIElement, Optional duracionMs As Integer = 220)
        elemento.Opacity = 0
        Dim animacion As New DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(duracionMs)) With {
            .EasingFunction = New QuadraticEase With {.EasingMode = EasingMode.EaseOut}
        }
        elemento.BeginAnimation(UIElement.OpacityProperty, animacion)
    End Sub
End Class
