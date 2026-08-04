''' <summary>Punto único donde se decide qué puede hacer cada rol. Las vistas
''' preguntan por la acción (PuedeEliminar, PuedeGestionarUsuarios) y nunca
''' comparan el rol directamente: si mañana se agrega un rol nuevo o cambian
''' los permisos, solo hay que tocar esta clase.
'''
''' Hay tres roles: Administrador y Agente son el personal de la aerolínea;
''' Pasajero es quien viaja y solo alcanza lo suyo.</summary>
Public Class Permisos

    ''' <summary>Todo lo que sea operar la aerolínea (vender, programar vuelos,
    ''' cobrar, ver catálogos) es cosa del personal.</summary>
    Public Shared ReadOnly Property EsPersonal As Boolean
        Get
            Return Sesion.UsuarioActual IsNot Nothing AndAlso Sesion.UsuarioActual.EsPersonal
        End Get
    End Property

    Public Shared ReadOnly Property EsPasajero As Boolean
        Get
            Return Sesion.UsuarioActual IsNot Nothing AndAlso Sesion.UsuarioActual.EsPasajero
        End Get
    End Property

    ''' <summary>Solo un Administrador borra registros; un Agente los crea y edita.</summary>
    Public Shared ReadOnly Property PuedeEliminar As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    ''' <summary>Los catálogos (aviones, aeropuertos, tarifas) son configuración
    ''' del negocio: un Agente los consulta pero no los modifica.</summary>
    Public Shared ReadOnly Property PuedeEditarCatalogos As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    Public Shared ReadOnly Property PuedeGestionarUsuarios As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    Public Shared ReadOnly Property PuedeVerBitacora As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    ''' <summary>Cancelar una reserva ya pagada implica devolver dinero, así que
    ''' queda en manos de un Administrador. El pasajero pide la cancelación en el
    ''' mostrador; no la ejecuta él.</summary>
    Public Shared ReadOnly Property PuedeCancelarReservas As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    ''' <summary>El personal ve y reserva para cualquiera; un pasajero solo para sí
    ''' mismo y para los acompañantes que él mismo registre. Es lo que impide que
    ''' desde el portal se pueda ir leyendo el directorio de pasajeros.</summary>
    Public Shared ReadOnly Property PuedeReservarParaCualquiera As Boolean
        Get
            Return EsPersonal
        End Get
    End Property

    ''' <summary>Un pasajero puede hacer su propio check-in desde el portal.</summary>
    Public Shared ReadOnly Property PuedeHacerCheckIn As Boolean
        Get
            Return Sesion.UsuarioActual IsNot Nothing
        End Get
    End Property
End Class
