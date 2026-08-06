''' <summary>Guarda quién inició sesión, accesible desde toda la aplicación.</summary>
Public Class Sesion

    Public Shared Property UsuarioActual As Usuario

    Public Shared ReadOnly Property EsAdministrador As Boolean
        Get
            Return UsuarioActual IsNot Nothing AndAlso UsuarioActual.EsAdministrador
        End Get
    End Property

    ''' <summary>Nombre de usuario para las columnas `usuario_registra` y la bitácora.</summary>
    Public Shared ReadOnly Property NombreUsuario As String
        Get
            Return If(UsuarioActual?.NombreUsuario, "-")
        End Get
    End Property

    Public Shared Sub Cerrar()
        UsuarioActual = Nothing
    End Sub
End Class
