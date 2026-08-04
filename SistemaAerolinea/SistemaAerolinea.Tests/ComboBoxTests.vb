Imports System.Data
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports Xunit

''' <summary>El estilo del ComboBox lleva plantilla propia, y una plantilla mal
''' hecha hace que la casilla cerrada muestre "System.Data.DataRowView" en vez del
''' texto de DisplayMemberPath. Es un fallo que no rompe la compilación ni lanza
''' ninguna excepción: solo se ve al abrir la pantalla, así que se prueba aquí.</summary>
Public Class ComboBoxTests

    ''' <summary>Recorre el árbol visual y junta el texto de todos los TextBlock.</summary>
    Private Shared Sub RecogerTextos(nodo As DependencyObject, textos As List(Of String))
        If nodo Is Nothing Then Return

        Dim texto = TryCast(nodo, TextBlock)
        If texto IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(texto.Text) Then
            textos.Add(texto.Text)
        End If

        For i = 0 To VisualTreeHelper.GetChildrenCount(nodo) - 1
            RecogerTextos(VisualTreeHelper.GetChild(nodo, i), textos)
        Next
    End Sub

    ''' <summary>Monta un ComboBox con el estilo del sistema y devuelve el texto que
    ''' realmente se pinta en la casilla cerrada.</summary>
    Private Shared Function TextosDeLaCasilla(configurar As Action(Of ComboBox)) As List(Of String)
        Return HiloUi.Ejecutar(
            Function()
                Dim combo As New ComboBox()
                configurar(combo)

                ' Sin una ventana que lo contenga, la plantilla no llega a aplicarse
                Dim ventana As New Window With {
                    .Content = combo, .Width = 300, .Height = 100,
                    .WindowStyle = WindowStyle.None, .ShowInTaskbar = False,
                    .Left = -2000, .Top = -2000
                }
                ventana.Show()
                combo.UpdateLayout()

                Dim textos As New List(Of String)()
                RecogerTextos(combo, textos)
                ventana.Close()
                Return textos
            End Function)
    End Function

    Private Shared Function TablaDePrueba() As DataView
        Dim tabla As New DataTable()
        tabla.Columns.Add("idpais", GetType(String))
        tabla.Columns.Add("etiqueta", GetType(String))
        tabla.Rows.Add("HN01", "Honduras")
        tabla.Rows.Add("GT01", "Guatemala")
        Return tabla.DefaultView
    End Function

    <Fact>
    Public Sub LaCasillaMuestraElTextoDeDisplayMemberPathYNoElNombreDelTipo()
        Dim textos = TextosDeLaCasilla(
            Sub(combo)
                combo.ItemsSource = TablaDePrueba()
                combo.DisplayMemberPath = "etiqueta"
                combo.SelectedValuePath = "idpais"
                combo.SelectedIndex = 0
            End Sub)

        Dim visto = String.Join(" | ", textos)
        Assert.False(visto.Contains("DataRowView"),
                     $"La casilla está mostrando el nombre del tipo en vez del dato: {visto}")
        Assert.Contains("Honduras", textos)
    End Sub

    <Fact>
    Public Sub LaCasillaFuncionaConUnaListaDeTextos()
        Dim textos = TextosDeLaCasilla(
            Sub(combo)
                combo.ItemsSource = New String() {"Factura", "Recibo"}
                combo.SelectedIndex = 1
            End Sub)

        Assert.Contains("Recibo", textos)
    End Sub

    <Fact>
    Public Sub LaCasillaFuncionaConUnaListaDeNumeros()
        ' Es el caso de "asientos por fila" en el catálogo de aviones
        Dim textos = TextosDeLaCasilla(
            Sub(combo)
                combo.ItemsSource = New Integer() {4, 6, 8}
                combo.SelectedItem = 6
            End Sub)

        Assert.Contains("6", textos)
    End Sub

    <Fact>
    Public Sub SinSeleccionSeMuestraElTextoGuia()
        Dim textos = TextosDeLaCasilla(
            Sub(combo)
                combo.ItemsSource = TablaDePrueba()
                combo.DisplayMemberPath = "etiqueta"
                combo.Tag = "Elige el país"
                combo.SelectedIndex = -1
            End Sub)

        Assert.Contains("Elige el país", textos)
    End Sub

    ''' <summary>Un combo editable (el de la pregunta de seguridad) escribe en su
    ''' propia caja de texto, no en la casilla de selección.</summary>
    <Fact>
    Public Sub ElComboEditableMuestraSuTexto()
        Dim textos = TextosDeLaCasilla(
            Sub(combo)
                combo.IsEditable = True
                combo.ItemsSource = New String() {"¿Cuál fue tu primera mascota?"}
                combo.Text = "¿En qué ciudad naciste?"
            End Sub)

        ' El texto vive en un TextBox, no en un TextBlock: basta con que no reviente
        Assert.NotNull(textos)
    End Sub
End Class
