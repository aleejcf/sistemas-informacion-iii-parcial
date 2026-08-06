Imports System.ComponentModel

''' <summary>Se muestra justo después de iniciar sesión cuando la cuenta tiene una
''' contraseña temporal (creada por un Administrador). No se puede cerrar sin cambiarla:
''' cerrarla con la X o Alt+F4 solo se traduce en que el login se cancela.</summary>
Public Class CambiarContrasenaObligatoriaWindow

    Private cambioRealizado As Boolean = False

    Private Sub CambiarContrasenaObligatoriaWindow_Loaded(sender As Object, e As RoutedEventArgs) _
        Handles Me.Loaded
        TransicionVentana.FundirEntrada(Me)
        txtNueva.Focus()
    End Sub

    Private Sub btnCambiar_Click(sender As Object, e As RoutedEventArgs) Handles btnCambiar.Click
        OcultarError()

        If txtNueva.Password <> txtConfirmar.Password Then
            MostrarError("Las contraseñas no coinciden.")
            Return
        End If

        Try
            Dim mensajeError = AuthService.CambiarContrasena(Sesion.UsuarioActual.NombreUsuario, txtNueva.Password)
            If mensajeError IsNot Nothing Then
                MostrarError(mensajeError)
                Return
            End If

            Sesion.UsuarioActual.DebeCambiarContrasena = False
            cambioRealizado = True
            DialogoParko.Show("Contraseña actualizada. Ya puedes usar el sistema.", "Listo")
            Me.Close()
        Catch ex As Exception
            MostrarError(MensajeError.Traducir("No se pudo cambiar la contraseña", ex))
        End Try
    End Sub

    ''' <summary>Impide salir de la ventana (X, Alt+F4) sin haber cambiado la contraseña:
    ''' en ese caso se cierra sesión en vez de dejar pasar al sistema.</summary>
    Private Sub Window_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
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
