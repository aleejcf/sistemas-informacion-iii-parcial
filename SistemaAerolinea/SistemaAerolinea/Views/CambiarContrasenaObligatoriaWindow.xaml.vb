''' <summary>Se muestra cuando una cuenta entra con la contraseña temporal que le
''' generó un Administrador. Si se cierra sin cambiarla, la sesión se descarta:
''' de otro modo la clave temporal serviría indefinidamente.</summary>
Public Class CambiarContrasenaObligatoriaWindow

    Private cambioRealizado As Boolean = False

    Private Sub Ventana_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        txtNueva.Focus()
    End Sub

    Private Async Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        OcultarError()

        If Sesion.UsuarioActual Is Nothing Then
            MostrarError("La sesión ya no está activa. Vuelve a iniciar sesión.")
            Return
        End If

        If txtNueva.Password <> txtNueva2.Password Then
            MostrarError("Las dos contraseñas no coinciden.")
            TransicionVentana.Sacudir(panelFormulario)
            Return
        End If

        btnGuardar.IsEnabled = False
        btnGuardar.Content = "Guardando…"

        Try
            Dim usuario = Sesion.UsuarioActual.NombreUsuario
            Dim nueva = txtNueva.Password
            Dim problema = Await Task.Run(Function() AuthService.CambiarContrasena(usuario, nueva))

            If problema IsNot Nothing Then
                MostrarError(problema)
                TransicionVentana.Sacudir(panelFormulario)
                Return
            End If

            Sesion.UsuarioActual.DebeCambiarContrasena = False
            cambioRealizado = True
            DialogoAlas.Show("Tu contraseña se actualizó. ¡Bienvenido a bordo!",
                             "Contraseña actualizada con éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            Me.Close()

        Catch ex As Exception
            MostrarError(MensajeError.Traducir("Cambio obligatorio de contraseña", ex))
        Finally
            btnGuardar.IsEnabled = True
            btnGuardar.Content = "Guardar y entrar"
        End Try
    End Sub

    Private Sub lnkSalir_Click(sender As Object, e As RoutedEventArgs) Handles lnkSalir.Click
        Me.Close()
    End Sub

    ''' <summary>Si la ventana se cierra sin haber cambiado la contraseña, se cierra
    ''' también la sesión: quien entró con clave temporal no puede seguir adelante.</summary>
    Private Sub Ventana_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Me.Closing
        If Not cambioRealizado Then Sesion.Cerrar()
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visibility = Visibility.Visible
    End Sub

    Private Sub OcultarError()
        lblError.Visibility = Visibility.Collapsed
    End Sub
End Class
