Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        ' A diferencia de ALAS y Alejandría, la Sesion de PARKO no tiene un
        ' NombreUsuario propio: se lee directo del usuario logueado.
        Registro.Configurar("parko", Function() If(Sesion.UsuarioActual?.NombreUsuario, "-"))

        MusicaService.Iniciar()
    End Sub

End Class
