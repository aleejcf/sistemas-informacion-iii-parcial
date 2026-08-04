Imports System.Windows.Controls
Imports System.Windows.Data

''' <summary>Hace que la casilla cerrada de un ComboBox respete DisplayMemberPath.
'''
''' WPF solo resuelve DisplayMemberPath al generar los elementos de la lista
''' desplegable. Para lo que se muestra cuando el combo está cerrado, expone
''' SelectionBoxItemTemplate — que viene en Nothing si la vista únicamente puso
''' DisplayMemberPath. El ContentPresenter se queda entonces sin plantilla y acaba
''' llamando a ToString() sobre el elemento: por eso un combo alimentado con un
''' DataTable mostraba literalmente "System.Data.DataRowView".
'''
''' Este selector se engancha en la plantilla del ComboBox y arma al vuelo la
''' plantilla que falta. Solo actúa cuando no hay otra: si la vista define su
''' propio ItemTemplate, ese sigue mandando.</summary>
Public Class PlantillaCasillaComboBox
    Inherits DataTemplateSelector

    ' Las plantillas se reutilizan por ruta: solo hay un puñado en todo el sistema
    Private Shared ReadOnly Cache As New Dictionary(Of String, DataTemplate)()

    Public Overrides Function SelectTemplate(item As Object, container As DependencyObject) As DataTemplate
        Dim presentador = TryCast(container, FrameworkElement)
        If presentador Is Nothing Then Return Nothing

        ' El ContentPresenter vive dentro de la plantilla del ComboBox,
        ' así que su TemplatedParent es el ComboBox mismo.
        Dim combo = TryCast(presentador.TemplatedParent, ComboBox)
        If combo Is Nothing Then Return Nothing

        Dim ruta = combo.DisplayMemberPath
        If String.IsNullOrWhiteSpace(ruta) Then Return Nothing

        ' Un texto suelto ya se muestra bien solo; armar una plantilla lo rompería
        If TypeOf item Is String Then Return Nothing

        Return PlantillaPara(ruta)
    End Function

    Private Shared Function PlantillaPara(ruta As String) As DataTemplate
        SyncLock Cache
            Dim plantilla As DataTemplate = Nothing
            If Cache.TryGetValue(ruta, plantilla) Then Return plantilla

            Dim texto As New FrameworkElementFactory(GetType(TextBlock))
            texto.SetBinding(TextBlock.TextProperty, New Binding(ruta))
            texto.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis)

            plantilla = New DataTemplate With {.VisualTree = texto}
            plantilla.Seal()

            Cache(ruta) = plantilla
            Return plantilla
        End SyncLock
    End Function
End Class
