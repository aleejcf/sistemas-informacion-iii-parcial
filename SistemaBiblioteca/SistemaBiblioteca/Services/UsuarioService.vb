Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Administración de las cuentas del personal. Solo un Administrador
''' llega aquí; las reglas de contraseña y el hash viven en AuthService.</summary>
Public Class UsuarioService

    Public Shared Function Listar(Optional filtro As String = "") As DataTable
        Return Db.Consultar(
            "SELECT usuario_id, nombre_completo, email, usuario, rol, esta_activo,
                    debe_cambiar_contrasena, ultimo_acceso, fecha_creacion,
                    CASE WHEN pregunta_seguridad IS NULL OR respuesta_seguridad IS NULL
                         THEN 0 ELSE 1 END AS tiene_pregunta,
                    CASE WHEN esta_activo = 1 THEN 'Activo' ELSE 'Inactivo' END AS situacion
             FROM usuario
             WHERE (@f = '' OR nombre_completo LIKE @like OR usuario LIKE @like OR email LIKE @like)
             ORDER BY nombre_completo",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"))
    End Function

    Public Shared Function Obtener(usuarioId As Integer) As DataRow
        Return Db.ConsultarFila("SELECT usuario_id, nombre_completo, email, usuario, rol, esta_activo
                                 FROM usuario WHERE usuario_id = @id",
                                New SqlParameter("@id", usuarioId))
    End Function

    ''' <summary>Cambia el rol de una cuenta. Se niega a dejar el sistema sin
    ''' ningún administrador activo: sería imposible volver a entrar a administrar.</summary>
    Public Shared Function CambiarRol(usuarioId As Integer, rol As String) As String
        If Not AuthService.Roles.Contains(rol) Then Return "Selecciona un rol válido."

        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        If Db.Texto(fila, "rol") = "Administrador" AndAlso rol <> "Administrador" Then
            If EsUltimoAdministrador(usuarioId) Then
                Return "Es la única cuenta de administrador activa. " &
                       "Nombra otro administrador antes de cambiarle el rol a esta."
            End If
        End If

        Db.Ejecutar("UPDATE usuario SET rol = @r WHERE usuario_id = @id",
                    New SqlParameter("@id", usuarioId),
                    New SqlParameter("@r", rol))

        BitacoraService.Registrar(BitacoraService.EDITAR, "usuario",
                                  $"{Db.Texto(fila, "usuario")} → rol {rol}")
        Return Nothing
    End Function

    ''' <summary>Activa o desactiva una cuenta. Desactivar es la forma de dar de
    ''' baja sin borrar: la bitácora conserva lo que esa persona hizo.</summary>
    Public Shared Function CambiarEstado(usuarioId As Integer, activo As Boolean) As String
        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        If Not activo Then
            If Sesion.UsuarioActual IsNot Nothing AndAlso Sesion.UsuarioActual.UsuarioID = usuarioId Then
                Return "No puedes desactivar tu propia cuenta mientras la estás usando."
            End If
            If Db.Texto(fila, "rol") = "Administrador" AndAlso EsUltimoAdministrador(usuarioId) Then
                Return "Es la única cuenta de administrador activa y no se puede desactivar."
            End If
        End If

        Db.Ejecutar("UPDATE usuario SET esta_activo = @a WHERE usuario_id = @id",
                    New SqlParameter("@id", usuarioId),
                    New SqlParameter("@a", activo))

        BitacoraService.Registrar(BitacoraService.EDITAR, "usuario",
            $"{Db.Texto(fila, "usuario")} → {If(activo, "activada", "desactivada")}")
        Return Nothing
    End Function

    ''' <summary>Le genera una contraseña temporal a alguien que perdió la suya y
    ''' no configuró pregunta de seguridad. Tendrá que cambiarla al entrar.</summary>
    Public Shared Function RestablecerContrasena(usuarioId As Integer,
                                                 ByRef claveTemporal As String) As String
        claveTemporal = ""

        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        Dim clave = GeneradorClave.GenerarTemporal()
        Dim hash = BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=11)

        Db.Ejecutar("UPDATE usuario SET contrasena_hash = @h, debe_cambiar_contrasena = 1,
                            codigo_recuperacion = NULL, fecha_expiracion_codigo = NULL
                     WHERE usuario_id = @id",
                    New SqlParameter("@id", usuarioId),
                    New SqlParameter("@h", hash))

        claveTemporal = clave
        Registro.Info($"Contraseña restablecida por administrador para: {Db.Texto(fila, "usuario")}")
        BitacoraService.Registrar(BitacoraService.CAMBIO_CLAVE, "usuario",
                                  $"Contraseña temporal generada para {Db.Texto(fila, "usuario")}")
        Return Nothing
    End Function

    ''' <summary>Elimina una cuenta definitivamente. Solo para cuentas creadas por
    ''' error: si ya operó el sistema, lo correcto es desactivarla.</summary>
    Public Shared Function Eliminar(usuarioId As Integer) As String
        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        If Sesion.UsuarioActual IsNot Nothing AndAlso Sesion.UsuarioActual.UsuarioID = usuarioId Then
            Return "No puedes eliminar tu propia cuenta."
        End If
        If Db.Texto(fila, "rol") = "Administrador" AndAlso EsUltimoAdministrador(usuarioId) Then
            Return "Es la única cuenta de administrador activa y no se puede eliminar."
        End If

        Dim nombreUsuario = Db.Texto(fila, "usuario")
        Dim conActividad = Db.Contar("SELECT COUNT(*) FROM bitacora WHERE usuario = @u",
                                     New SqlParameter("@u", nombreUsuario))
        If conActividad > 0 Then
            Return $"Esta cuenta tiene {conActividad} registros en la bitácora. " &
                   "Desactívala en vez de eliminarla, así se conserva la auditoría."
        End If

        Db.Ejecutar("DELETE FROM usuario WHERE usuario_id = @id", New SqlParameter("@id", usuarioId))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "usuario", nombreUsuario)
        Return Nothing
    End Function

    Private Shared Function EsUltimoAdministrador(usuarioId As Integer) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM usuario
                          WHERE rol = 'Administrador' AND esta_activo = 1 AND usuario_id <> @id",
                         New SqlParameter("@id", usuarioId)) = 0
    End Function
End Class
