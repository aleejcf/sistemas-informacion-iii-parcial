Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Gestión de las cuentas del sistema. Solo un Administrador llega aquí
''' (lo decide la clase Permisos), pero el servicio protege además las reglas que
''' no deben romperse ni por error: no quedarse sin administradores y no dejar que
''' alguien se desactive o se degrade a sí mismo.</summary>
Public Class UsuarioService

    ''' <summary>Los tres roles que existen. Se usa para MOSTRAR y para cambiar el
    ''' rol de una cuenta que ya existe.
    '''
    ''' Antes esta lista no incluía Pasajero, y por eso al seleccionar a un pasajero
    ''' el combo de la pantalla salía en blanco —su rol no estaba entre las opciones—
    ''' y CambiarRol rechazaba devolverle el suyo a quien se hubiera promovido.</summary>
    Public Shared ReadOnly Roles As String() = {"Administrador", "Agente", "Pasajero"}

    Public Shared Function Listar(Optional filtro As String = "") As DataTable
        Return Db.Consultar(
            "SELECT usuario_id, nombre_completo, usuario, email, rol,
                    esta_activo, debe_cambiar_contrasena, ultimo_acceso, fecha_creacion,
                    idpasajero,
                    CASE WHEN esta_activo = 1 THEN 'Activo' ELSE 'Inactivo' END AS estado,
                    CASE WHEN pregunta_seguridad IS NULL OR respuesta_seguridad IS NULL
                         THEN 0 ELSE 1 END AS tiene_pregunta,
                    dbo.fn_codigos_disponibles(usuario_id) AS codigos_respaldo
             FROM usuario
             WHERE @f = '' OR nombre_completo LIKE @like OR usuario LIKE @like OR email LIKE @like
             ORDER BY rol, nombre_completo",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"))
    End Function

    Public Shared Function Obtener(usuarioId As Integer) As DataRow
        Return Db.ConsultarFila("SELECT * FROM usuario WHERE usuario_id = @i",
                                New SqlParameter("@i", usuarioId))
    End Function

    Private Shared Function AdministradoresActivos(Optional exceptoId As Integer = 0) As Integer
        Return Db.Contar("SELECT COUNT(*) FROM usuario
                          WHERE rol = 'Administrador' AND esta_activo = 1 AND usuario_id <> @i",
                         New SqlParameter("@i", exceptoId))
    End Function

    ''' <summary>Activa o desactiva una cuenta. Devuelve Nothing si salió bien,
    ''' o el mensaje que explica por qué no se pudo.</summary>
    Public Shared Function CambiarEstado(usuarioId As Integer, activo As Boolean) As String
        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        Dim nombre = fila("usuario").ToString()

        If Not activo Then
            If Sesion.UsuarioActual IsNot Nothing AndAlso Sesion.UsuarioActual.UsuarioID = usuarioId Then
                Return "No puedes desactivar tu propia cuenta."
            End If
            If fila("rol").ToString() = "Administrador" AndAlso AdministradoresActivos(usuarioId) = 0 Then
                Return "No se puede desactivar: es el único Administrador activo del sistema."
            End If
        End If

        Db.Ejecutar("UPDATE usuario SET esta_activo = @a WHERE usuario_id = @i",
                    New SqlParameter("@a", activo),
                    New SqlParameter("@i", usuarioId))

        BitacoraService.Registrar(BitacoraService.EDITAR, "usuario",
                                  $"{nombre} → {If(activo, "activada", "desactivada")}")
        Return Nothing
    End Function

    Public Shared Function CambiarRol(usuarioId As Integer, rol As String) As String
        If Not Roles.Contains(rol) Then Return "Selecciona un rol válido."

        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        If Sesion.UsuarioActual IsNot Nothing AndAlso Sesion.UsuarioActual.UsuarioID = usuarioId AndAlso
           rol <> "Administrador" Then
            Return "No puedes quitarte a ti mismo el rol de Administrador."
        End If

        If fila("rol").ToString() = "Administrador" AndAlso rol <> "Administrador" AndAlso
           AdministradoresActivos(usuarioId) = 0 Then
            Return "No se puede cambiar: es el único Administrador activo del sistema."
        End If

        ' Un Pasajero sin ficha de viajero no puede existir: no podría comprar un
        ' boleto ni hacer check-in, y además todo el aislamiento del portal se apoya
        ' en esa ficha. Sin ella la cuenta quedaría en tierra de nadie.
        If rol = "Pasajero" AndAlso IsDBNull(fila("idpasajero")) Then
            Return "Esa cuenta no tiene ficha de viajero, así que no puede ser de Pasajero. " &
                   "Regístrala desde la pantalla de acceso para que se le cree su ficha."
        End If

        Db.Ejecutar("UPDATE usuario SET rol = @r WHERE usuario_id = @i",
                    New SqlParameter("@r", rol),
                    New SqlParameter("@i", usuarioId))

        BitacoraService.Registrar(BitacoraService.EDITAR, "usuario",
                                  $"{fila("usuario")} → rol {rol}")
        Return Nothing
    End Function

    ''' <summary>Corrige el nombre y el correo de una cuenta desde la pantalla de
    ''' Usuarios.
    '''
    ''' Es la salida de emergencia para quien perdió el buzón y ya no puede
    ''' confirmar nada por sí mismo: sin esto, esa persona se queda fuera para
    ''' siempre. Por eso NO se le pide su contraseña —no la tiene a mano, o no
    ''' podría pedir ayuda— y por eso queda en la bitácora con el antes y el
    ''' después: cambiar el correo de una cuenta ajena es poder apoderarse de ella,
    ''' y un poder así tiene que dejar rastro.</summary>
    Public Shared Function CorregirDatos(usuarioId As Integer, nombreCompleto As String,
                                         email As String) As String
        If String.IsNullOrWhiteSpace(nombreCompleto) Then Return "Escribe el nombre completo."
        If Not Validador.EsEmailValido(email) Then Return "El correo electrónico no es válido."

        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        Dim limpio = email.Trim().ToLower()

        If Db.Contar("SELECT COUNT(*) FROM usuario WHERE email = @e AND usuario_id <> @i",
                     New SqlParameter("@e", limpio),
                     New SqlParameter("@i", usuarioId)) > 0 Then
            Return "Ese correo ya está registrado en otra cuenta."
        End If

        Dim antes = If(IsDBNull(fila("email")), "", fila("email").ToString())
        Dim cuenta = fila("usuario").ToString()

        Db.Ejecutar("UPDATE usuario SET nombre_completo = @n, email = @e WHERE usuario_id = @i",
                    New SqlParameter("@n", nombreCompleto.Trim()),
                    New SqlParameter("@e", limpio),
                    New SqlParameter("@i", usuarioId))

        Registro.Info($"Un Administrador corrigió los datos de {cuenta}")

        ' El antes y el después, para que la bitácora sirva de algo si alguien
        ' pregunta por qué esa cuenta cambió de dueño
        If Not String.Equals(antes, limpio, StringComparison.OrdinalIgnoreCase) Then
            BitacoraService.Registrar(BitacoraService.EDITAR, "usuario",
                                      $"{cuenta} · correo {antes} → {limpio}")
        Else
            BitacoraService.Registrar(BitacoraService.EDITAR, "usuario", $"{cuenta} · nombre corregido")
        End If

        Return Nothing
    End Function

    ''' <summary>Genera una contraseña temporal y obliga a cambiarla al entrar.
    ''' Devuelve Nothing y la clave por referencia si salió bien.</summary>
    Public Shared Function RestablecerContrasena(usuarioId As Integer,
                                                 ByRef claveTemporal As String) As String
        claveTemporal = ""

        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        Dim clave = GeneradorClave.GenerarTemporal()
        Dim hash = BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=11)

        Db.Ejecutar("UPDATE usuario SET contrasena_hash = @h, debe_cambiar_contrasena = 1,
                                        codigo_recuperacion = NULL, fecha_expiracion_codigo = NULL
                     WHERE usuario_id = @i",
                    New SqlParameter("@h", hash),
                    New SqlParameter("@i", usuarioId))

        claveTemporal = clave
        BitacoraService.Registrar(BitacoraService.CAMBIO_CLAVE, "usuario",
                                  $"Contraseña restablecida a {fila("usuario")}")
        Return Nothing
    End Function

    ''' <summary>Elimina una cuenta. Las cuentas con historial en la bitácora se
    ''' desactivan en vez de borrarse, para no perder la trazabilidad.</summary>
    Public Shared Function Eliminar(usuarioId As Integer) As String
        Dim fila = Obtener(usuarioId)
        If fila Is Nothing Then Return "No se encontró la cuenta."

        If Sesion.UsuarioActual IsNot Nothing AndAlso Sesion.UsuarioActual.UsuarioID = usuarioId Then
            Return "No puedes eliminar tu propia cuenta."
        End If
        If fila("rol").ToString() = "Administrador" AndAlso AdministradoresActivos(usuarioId) = 0 Then
            Return "No se puede eliminar: es el único Administrador activo del sistema."
        End If

        Dim nombre = fila("usuario").ToString()

        Dim conHistorial = Db.Contar("SELECT COUNT(*) FROM bitacora WHERE usuario = @u",
                                     New SqlParameter("@u", nombre))
        If conHistorial > 0 Then
            Return "Esta cuenta tiene actividad registrada en la bitácora. " &
                   "Desactívala en lugar de eliminarla para conservar la trazabilidad."
        End If

        Db.Ejecutar("DELETE FROM usuario WHERE usuario_id = @i", New SqlParameter("@i", usuarioId))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "usuario", nombre)
        Return Nothing
    End Function
End Class
