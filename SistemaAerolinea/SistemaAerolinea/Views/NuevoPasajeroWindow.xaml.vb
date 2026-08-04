''' <summary>Alta rápida de un pasajero desde el asistente de reserva. Evita que
''' el agente tenga que abandonar la venta para ir a la pantalla de Pasajeros.
''' Al cerrarse, CodigoCreado trae el código del pasajero nuevo (o queda vacío
''' si se canceló), que es lo que el asistente usa para seleccionarlo.</summary>
Public Class NuevoPasajeroWindow

    Public Property CodigoCreado As String = ""

    Private Sub NuevoPasajeroWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)

        Try
            cboTipoDocumento.ItemsSource = PasajeroService.TiposDocumento
            cboTipoDocumento.SelectedIndex = 0
            cboPais.ItemsSource = CatalogoService.PaisesParaCombo().DefaultView
            txtCodigo.Text = PasajeroService.SiguienteCodigo()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Preparar el formulario de pasajero", ex))
        End Try

        txtNombre.Focus()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        OcultarError()

        If cboPais.SelectedValue Is Nothing Then
            MostrarError("Selecciona el país del pasajero.")
            Return
        End If

        btnGuardar.IsEnabled = False

        Try
            Dim problema = PasajeroService.Guardar(
                txtCodigo.Text, txtNombre.Text, txtApaterno.Text, txtAmaterno.Text,
                If(cboTipoDocumento.SelectedItem Is Nothing, "", cboTipoDocumento.SelectedItem.ToString()),
                txtDocumento.Text, dpNacimiento.SelectedDate, cboPais.SelectedValue.ToString(),
                txtTelefono.Text, txtCorreo.Text, editando:=False)

            If problema IsNot Nothing Then
                MostrarError(problema)
                Return
            End If

            CodigoCreado = txtCodigo.Text.Trim().ToUpper()
            Me.Close()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Guardar el pasajero", ex))
        Finally
            btnGuardar.IsEnabled = True
        End Try
    End Sub

    Private Sub btnCancelar_Click(sender As Object, e As RoutedEventArgs) Handles btnCancelar.Click
        CodigoCreado = ""
        Me.Close()
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
        TransicionVentana.Sacudir(panelFormulario)
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
