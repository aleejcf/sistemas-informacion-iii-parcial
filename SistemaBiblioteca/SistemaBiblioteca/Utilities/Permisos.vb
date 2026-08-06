''' <summary>Punto único donde se decide qué puede hacer cada rol. Las vistas
''' preguntan por la acción (PuedeEliminar, PuedeCondonarMultas) y nunca comparan
''' el rol directamente: si mañana se agrega un rol nuevo o cambian los permisos,
''' solo hay que tocar esta clase.</summary>
Public Class Permisos

    ''' <summary>Solo un Administrador borra registros; un Bibliotecario los crea y edita.</summary>
    Public Shared ReadOnly Property PuedeEliminar As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    ''' <summary>Los catálogos de autoridad (autores, editoriales, categorías y
    ''' tipos de socio) son configuración del acervo: un Bibliotecario los consulta
    ''' pero no los modifica.</summary>
    Public Shared ReadOnly Property PuedeEditarCatalogos As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    ''' <summary>Condonar una multa es perdonar dinero: es decisión de dirección.</summary>
    Public Shared ReadOnly Property PuedeCondonarMultas As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    ''' <summary>Prestar por encima del límite o a un socio con mora exige
    ''' autorización; un Bibliotecario tiene que llamar al Administrador.</summary>
    Public Shared ReadOnly Property PuedeForzarPrestamo As Boolean
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
End Class
