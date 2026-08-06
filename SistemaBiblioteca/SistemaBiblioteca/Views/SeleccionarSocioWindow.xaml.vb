Imports System.Data

''' <summary>Selector de socio reutilizable. Lo usan el catálogo (para apartar un
''' título) y la página de reservas: en vez de que cada pantalla arme su propia
''' búsqueda de socios, todas abren esta.</summary>
Public Class SeleccionarSocioWindow

    ''' <summary>El socio elegido, o Nothing si se cerró sin elegir.</summary>
    Public Property SocioElegido As String

    Public Property NombreElegido As String

    ''' <summary>Texto que explica para qué se está eligiendo el socio. Se asigna
    ''' como propiedad y no por constructor: así la ventana conserva el
    ''' constructor sin parámetros que WPF necesita para cargar su XAML.</summary>
    Public Property Subtitulo As String = "Busca a quién corresponde"

    Private Sub Ventana_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        lblSubtitulo.Text = Subtitulo
        CargarSocios()
        txtBuscar.Focus()
    End Sub

    Private Sub CargarSocios()
        Try
            dgSocios.ItemsSource = SocioService.Listar(txtBuscar.Text, soloActivos:=True).DefaultView
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar los socios", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        CargarSocios()
    End Sub

    Private Sub dgSocios_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) _
        Handles dgSocios.MouseDoubleClick
        Elegir()
    End Sub

    Private Sub btnElegir_Click(sender As Object, e As RoutedEventArgs) Handles btnElegir.Click
        Elegir()
    End Sub

    Private Sub Elegir()
        Dim fila = TryCast(dgSocios.SelectedItem, DataRowView)
        If fila Is Nothing Then
            DialogoBiblioteca.Show("Selecciona un socio de la lista.", "Falta elegir",
                                   MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        SocioElegido = fila("idsocio").ToString()
        NombreElegido = fila("nombre_completo").ToString()
        Me.DialogResult = True
        Me.Close()
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        Me.Close()
    End Sub
End Class
