Imports System.Windows.Media

''' <summary>Logotipo vectorial de ALEJANDRÍA: un libro abierto dentro de un halo.
''' Se usa en el splash, el inicio de sesión, el menú lateral y los diálogos.</summary>
Public Class LogoBiblioteca

    Public Shared ReadOnly TamanoProperty As DependencyProperty =
        DependencyProperty.Register("Tamano", GetType(Double), GetType(LogoBiblioteca),
                                    New PropertyMetadata(44.0, AddressOf AlCambiarTamano))

    ''' <summary>Lado del logotipo en píxeles.</summary>
    Public Property Tamano As Double
        Get
            Return CDbl(GetValue(TamanoProperty))
        End Get
        Set(value As Double)
            SetValue(TamanoProperty, value)
        End Set
    End Property

    Private Shared Sub AlCambiarTamano(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim logo = TryCast(d, LogoBiblioteca)
        If logo Is Nothing Then Return
        logo.caja.Width = CDbl(e.NewValue)
        logo.caja.Height = CDbl(e.NewValue)
    End Sub

    ''' <summary>Pinta el libro de otro color (blanco sobre el menú lateral, por ejemplo).</summary>
    Public Property ColorLibro As Brush
        Get
            Return libro.Fill
        End Get
        Set(value As Brush)
            libro.Fill = value
        End Set
    End Property

    Public Property ColorHalo As Brush
        Get
            Return halo.Fill
        End Get
        Set(value As Brush)
            halo.Fill = value
        End Set
    End Property
End Class
