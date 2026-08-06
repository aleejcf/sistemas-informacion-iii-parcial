''' <summary>Punto único donde se decide qué puede hacer cada rol.
''' Las vistas preguntan por la acción (PuedeEliminar, PuedeGestionarUsuarios),
''' nunca comparan el rol directamente: así solo hay que tocar esta clase
''' si mañana se agrega un rol nuevo o cambian los permisos.</summary>
Public Class Permisos

    Public Shared ReadOnly Property PuedeEliminar As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    Public Shared ReadOnly Property PuedeGestionarUsuarios As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property

    Public Shared ReadOnly Property PuedeVerAuditoria As Boolean
        Get
            Return Sesion.EsAdministrador
        End Get
    End Property
End Class
