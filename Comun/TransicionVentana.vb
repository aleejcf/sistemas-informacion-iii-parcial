Imports System.Windows.Media.Animation

''' <summary>Transiciones reutilizables. Funcionan igual para una ventana completa
''' que para el contenido que se muestra dentro de ella al cambiar de página.</summary>
Public Class TransicionVentana

    Public Shared Sub FundirEntrada(elemento As UIElement, Optional duracionMs As Integer = 220)
        elemento.Opacity = 0
        Dim animacion As New DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(duracionMs)) With {
            .EasingFunction = New QuadraticEase With {.EasingMode = EasingMode.EaseOut}
        }
        elemento.BeginAnimation(UIElement.OpacityProperty, animacion)
    End Sub

    ''' <summary>Entrada con un leve deslizamiento hacia arriba. Se usa en cascada
    ''' sobre los campos de un formulario dándole a cada uno más retraso.</summary>
    Public Shared Sub DeslizarEntrada(elemento As UIElement, Optional retrasoMs As Integer = 0,
                                      Optional duracionMs As Integer = 450)
        elemento.Opacity = 0
        Dim desplazamiento As New TranslateTransform(0, 18)
        elemento.RenderTransform = desplazamiento

        Dim aparecer As New DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(duracionMs)) With {
            .BeginTime = TimeSpan.FromMilliseconds(retrasoMs)
        }
        Dim subir As New DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(duracionMs)) With {
            .BeginTime = TimeSpan.FromMilliseconds(retrasoMs),
            .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
        }
        elemento.BeginAnimation(UIElement.OpacityProperty, aparecer)
        desplazamiento.BeginAnimation(TranslateTransform.YProperty, subir)
    End Sub

    ''' <summary>Aplica la entrada en cascada a todos los hijos de un panel.</summary>
    Public Shared Sub EntradaEnCascada(panel As Panel, Optional pasoMs As Integer = 70)
        Dim retraso = 0
        For Each hijo As UIElement In panel.Children
            DeslizarEntrada(hijo, retraso)
            retraso += pasoMs
        Next
    End Sub

    ''' <summary>Sacudida horizontal: retroalimentación inmediata de "esto está mal".</summary>
    Public Shared Sub Sacudir(elemento As FrameworkElement)
        Dim desplazamiento As New TranslateTransform()
        elemento.RenderTransform = desplazamiento

        Dim sacudida As New DoubleAnimationUsingKeyFrames With {
            .Duration = TimeSpan.FromMilliseconds(420)
        }
        Dim valores = {0.0, -13.0, 11.0, -8.0, 6.0, -3.0, 0.0}
        For i = 0 To valores.Length - 1
            sacudida.KeyFrames.Add(New EasingDoubleKeyFrame(valores(i),
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 70))))
        Next
        desplazamiento.BeginAnimation(TranslateTransform.XProperty, sacudida)
    End Sub
End Class
