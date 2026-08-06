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

    ''' <summary>Ficha de viajero de quien tiene la sesión abierta, o Nothing si es
    ''' personal de la aerolínea. Es el filtro de todo el portal del pasajero.</summary>
    Public Shared ReadOnly Property IdPasajero As String
        Get
            If UsuarioActual Is Nothing OrElse Not UsuarioActual.EsPasajero Then Return Nothing
            Return UsuarioActual.IdPasajero
        End Get
    End Property

    ''' <summary>¿Esta sesión hay que limitarla a un pasajero?
    '''
    ''' Existe aparte de IdPasajero porque los servicios preguntaban
    ''' `IdPasajero IsNot Nothing` para decidirlo, y eso falla abriendo: una cuenta
    ''' con rol Pasajero pero sin ficha devuelve Nothing y se colaba por la rama del
    ''' personal, viendo TODAS las reservas del sistema. Aquí lo que manda es el rol,
    ''' así que sin ficha se sigue limitando —y como el filtro se hace con un
    ''' identificador vacío, no le cuadra ninguna fila y no ve nada. Falla cerrando,
    ''' que es como tiene que fallar un permiso.</summary>
    Public Shared ReadOnly Property EsSesionDePasajero As Boolean
        Get
            Return UsuarioActual IsNot Nothing AndAlso UsuarioActual.EsPasajero
        End Get
    End Property

    Public Shared Sub Cerrar()
        UsuarioActual = Nothing
    End Sub
End Class
