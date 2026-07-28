Public Class Usuario
    Public Property UsuarioID As Integer
    Public Property NombreCompleto As String
    Public Property Email As String
    Public Property NombreUsuario As String
    Public Property Rol As String

    Public ReadOnly Property EsAdministrador As Boolean
        Get
            Return Rol = "Administrador"
        End Get
    End Property
End Class
