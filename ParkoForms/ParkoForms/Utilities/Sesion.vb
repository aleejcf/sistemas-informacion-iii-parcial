''' <summary>Guarda el usuario que inició sesión, accesible desde toda la app.</summary>
Public Class Sesion
    Public Shared Property UsuarioActual As Usuario

    Public Shared ReadOnly Property EsAdministrador As Boolean
        Get
            Return UsuarioActual IsNot Nothing AndAlso UsuarioActual.EsAdministrador
        End Get
    End Property

    Public Shared Sub Cerrar()
        UsuarioActual = Nothing
    End Sub
End Class
