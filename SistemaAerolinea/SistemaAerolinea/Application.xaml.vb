Imports System.Windows.Markup

Class Application

    ''' <summary>WPF formatea las fechas de los enlaces (StringFormat) en inglés por
    ''' omisión, sin importar la configuración de Windows: un "mié 12 ago" saldría
    ''' como "Wed 12 Aug". Esto le dice a toda la interfaz que hable español de
    ''' Honduras, la misma cultura que usa la clase Formato.</summary>
    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Registro.Configurar("alas", Function() Sesion.NombreUsuario)

        FrameworkElement.LanguageProperty.OverrideMetadata(
            GetType(FrameworkElement),
            New FrameworkPropertyMetadata(XmlLanguage.GetLanguage(Formato.Cultura.IetfLanguageTag)))
    End Sub

    ''' <summary>Última red de seguridad: si una excepción se escapa de un manejador,
    ''' se guarda en la bitácora y se le muestra al usuario un mensaje entendible
    ''' en vez de que la aplicación se cierre de golpe.</summary>
    Private Sub Application_DispatcherUnhandledException(sender As Object,
        e As Threading.DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException

        Dim mensaje = MensajeError.Traducir("Error no controlado", e.Exception)
        DialogoAlas.Show(mensaje, "Error inesperado", MessageBoxButton.OK, MessageBoxImage.Error)
        e.Handled = True
    End Sub

End Class
