Imports System.IO
Imports System.Text

''' <summary>Entrega los códigos de respaldo recién generados. Se enseñan una sola
''' vez: en la base solo queda su hash BCrypt, así que ni el propio sistema puede
''' volver a mostrarlos.</summary>
Public Class CodigosRespaldoWindow

    Private ReadOnly codigos As String()
    Private ReadOnly nombreCuenta As String

    Public Sub New(codigosGenerados As String(), usuario As String)
        InitializeComponent()
        codigos = If(codigosGenerados, Array.Empty(Of String)())
        nombreCuenta = If(usuario, "")
        lstCodigos.ItemsSource = codigos
    End Sub

    ''' <summary>Abre la ventana si hay códigos que entregar. Devuelve sin hacer
    ''' nada cuando el lote vino vacío —por ejemplo si la base falló al generarlo—
    ''' para que un problema al crear los códigos no impida terminar el registro.</summary>
    Public Shared Sub Entregar(codigosGenerados As String(), usuario As String, propietaria As Window)
        If codigosGenerados Is Nothing OrElse codigosGenerados.Length = 0 Then Return

        Dim ventana As New CodigosRespaldoWindow(codigosGenerados, usuario)
        If propietaria IsNot Nothing AndAlso propietaria.IsLoaded Then ventana.Owner = propietaria
        ventana.ShowDialog()
    End Sub

    Private Sub CodigosRespaldoWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
    End Sub

    ''' <summary>Cerrar exige marcar la casilla. Es un estorbo a propósito: quien
    ''' cierre esta ventana sin guardar los códigos se queda sin ellos.</summary>
    Private Sub chkGuardados_Changed(sender As Object, e As RoutedEventArgs) _
        Handles chkGuardados.Checked, chkGuardados.Unchecked
        btnListo.IsEnabled = chkGuardados.IsChecked = True
    End Sub

    Private Sub btnCopiar_Click(sender As Object, e As RoutedEventArgs) Handles btnCopiar.Click
        Try
            Clipboard.SetText(String.Join(Environment.NewLine, codigos))
            btnCopiar.Content = "Copiados ✓"

        Catch ex As Exception
            ' El portapapeles lo puede tener tomado otro programa; no es para tanto
            Registro.Advertencia($"No se pudieron copiar los códigos: {ex.Message}")
            DialogoAlas.Show("No se pudo usar el portapapeles. Guárdalos en un archivo.",
                             "No se pudo copiar", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        Try
            Dim dialogo As New Microsoft.Win32.SaveFileDialog With {
                .FileName = $"codigos-respaldo-alas-{nombreCuenta}.txt",
                .Filter = "Archivo de texto (*.txt)|*.txt",
                .Title = "Guardar los códigos de respaldo"
            }
            If dialogo.ShowDialog() <> True Then Return

            File.WriteAllText(dialogo.FileName, Contenido(), Encoding.UTF8)

            btnGuardar.Content = "Guardado ✓"
            chkGuardados.IsChecked = True

        Catch ex As Exception
            DialogoAlas.Show(MensajeError.Traducir("Guardar los códigos", ex),
                             "No se pudo guardar", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Function Contenido() As String
        Dim texto As New StringBuilder()
        texto.AppendLine("ALAS Honduras · Códigos de respaldo")
        texto.AppendLine($"Cuenta: {nombreCuenta}")
        texto.AppendLine($"Generados: {Formato.FechaLarga(DateTime.Now)}")
        texto.AppendLine()
        texto.AppendLine("Cada código sirve UNA sola vez. Guárdalos en un lugar seguro:")
        texto.AppendLine("con ellos se puede cambiar la contraseña de esta cuenta.")
        texto.AppendLine()

        For Each codigo In codigos
            texto.AppendLine($"   {codigo}")
        Next

        Return texto.ToString()
    End Function

    Private Sub btnListo_Click(sender As Object, e As RoutedEventArgs) Handles btnListo.Click
        Me.Close()
    End Sub
End Class
