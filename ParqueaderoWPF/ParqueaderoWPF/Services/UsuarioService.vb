Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Consulta y administración de cuentas ya existentes.
''' Crear cuentas nuevas y todo lo relacionado con contraseñas vive en AuthService.</summary>
Public Class UsuarioService

    Public Shared Function Listar(Optional filtro As String = "") As DataTable
        If String.IsNullOrWhiteSpace(filtro) Then
            Return Db.Consultar("SELECT usuario_id, nombre_completo, email, usuario, rol, esta_activo,
                                        debe_cambiar_contrasena
                                 FROM usuario ORDER BY nombre_completo")
        End If
        Return Db.Consultar("SELECT usuario_id, nombre_completo, email, usuario, rol, esta_activo,
                                    debe_cambiar_contrasena
                             FROM usuario
                             WHERE nombre_completo LIKE @f OR usuario LIKE @f OR email LIKE @f
                             ORDER BY nombre_completo",
                            New SqlParameter("@f", "%" & filtro.Trim() & "%"))
    End Function

    ''' <summary>Activa o desactiva una cuenta. Se usa en vez de eliminarla para no perder
    ''' la referencia a sus movimientos registrados (usuario_registra).</summary>
    Public Shared Sub CambiarActivo(usuarioId As Integer, activo As Boolean)
        Db.Ejecutar("UPDATE usuario SET esta_activo = @a WHERE usuario_id = @id",
                    New SqlParameter("@a", activo),
                    New SqlParameter("@id", usuarioId))
    End Sub

    Public Shared Sub CambiarRol(usuarioId As Integer, nuevoRol As String)
        Db.Ejecutar("UPDATE usuario SET rol = @r WHERE usuario_id = @id",
                    New SqlParameter("@r", nuevoRol),
                    New SqlParameter("@id", usuarioId))
    End Sub
End Class
