Class Application

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Registro.Configurar("alejandria", Function() Sesion.NombreUsuario)
    End Sub

    ''' <summary>Última red de seguridad: si una excepción se escapa de un manejador,
    ''' se guarda en la bitácora y se le muestra al usuario un mensaje entendible
    ''' en vez de que la aplicación se cierre de golpe.</summary>
    Private Sub Application_DispatcherUnhandledException(sender As Object,
        e As Threading.DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException

        Dim mensaje = MensajeError.Traducir("Error no controlado", e.Exception)
        DialogoBiblioteca.Show(mensaje, "Error inesperado", MessageBoxButton.OK, MessageBoxImage.Error)
        e.Handled = True
    End Sub

End Class
