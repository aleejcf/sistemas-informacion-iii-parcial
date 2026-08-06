Imports Microsoft.Data.SqlClient
Imports System.Security.Cryptography

''' <summary>Registro e inicio de sesión. Las contraseñas se guardan con hash BCrypt,
''' nunca en texto plano.</summary>
Public Class AuthService

    Public Shared Function HayUsuarios() As Boolean
        Return CInt(Db.Escalar("SELECT COUNT(*) FROM usuario")) > 0
    End Function

    Public Shared Function ExisteUsuario(usuario As String) As Boolean
        Return CInt(Db.Escalar("SELECT COUNT(*) FROM usuario WHERE usuario = @u",
                               New SqlParameter("@u", usuario.Trim()))) > 0
    End Function

    Public Shared Function ExisteEmail(email As String) As Boolean
        Return CInt(Db.Escalar("SELECT COUNT(*) FROM usuario WHERE email = @e",
                               New SqlParameter("@e", email.Trim().ToLower()))) > 0
    End Function

    ''' <summary>Registra un usuario. Devuelve Nothing si todo salió bien;
    ''' si no, el mensaje de error para mostrar al usuario.
    ''' El primer usuario registrado queda como Administrador.</summary>
    Public Shared Function Registrar(nombreCompleto As String, email As String,
                                     usuario As String, clave As String,
                                     Optional pregunta As String = Nothing,
                                     Optional respuesta As String = Nothing) As String
        If String.IsNullOrWhiteSpace(nombreCompleto) Then Return "Escribe tu nombre completo."
        If Not Validador.EsEmailValido(email) Then Return "El correo electrónico no es válido."
        If Not Validador.EsUsuarioValido(usuario) Then Return "El usuario debe tener de 4 a 30 caracteres (letras, números o _)."

        Dim errorClave = Validador.ValidarContrasena(clave)
        If errorClave IsNot Nothing Then Return errorClave

        If String.IsNullOrWhiteSpace(pregunta) OrElse String.IsNullOrWhiteSpace(respuesta) Then
            Return "Selecciona una pregunta de seguridad y escribe la respuesta (sirve para recuperar tu cuenta)."
        End If

        If ExisteUsuario(usuario) Then Return "Ese nombre de usuario ya está registrado."
        If ExisteEmail(email) Then Return "Ese correo ya está registrado."

        Dim rol As String = If(HayUsuarios(), "Operador", "Administrador")
        Dim hash As String = BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=11)
        ' La respuesta también se guarda con hash (normalizada a minúsculas)
        Dim hashRespuesta As String = BCrypt.Net.BCrypt.HashPassword(respuesta.Trim().ToLower(), workFactor:=11)

        Db.Ejecutar("INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol,
                                          pregunta_seguridad, respuesta_seguridad)
                     VALUES (@n, @e, @u, @h, @r, @p, @rs)",
                    New SqlParameter("@n", nombreCompleto.Trim()),
                    New SqlParameter("@e", email.Trim().ToLower()),
                    New SqlParameter("@u", usuario.Trim()),
                    New SqlParameter("@h", hash),
                    New SqlParameter("@r", rol),
                    New SqlParameter("@p", pregunta.Trim()),
                    New SqlParameter("@rs", hashRespuesta))
        Registro.Info($"Usuario registrado: {usuario.Trim()} con rol {rol}")
        Return Nothing
    End Function

    ' ---------- Recuperación de contraseña ----------

    ''' <summary>Resultado de buscar la pregunta de seguridad de un usuario.</summary>
    Public Enum EstadoPregunta
        Encontrada
        UsuarioNoExiste
        SinConfigurar
    End Enum

    Public Class ConsultaPregunta
        Public Property Estado As EstadoPregunta
        Public Property Pregunta As String
        ''' <summary>Correo de la cuenta, para poder ofrecer la recuperación por código
        ''' cuando no hay pregunta de seguridad configurada (o como respaldo del otro camino).</summary>
        Public Property Email As String
    End Class

    ''' <summary>Busca la pregunta de seguridad e informa por qué no se pudo obtener,
    ''' para poder guiar al usuario con un mensaje útil.</summary>
    Public Shared Function ObtenerPregunta(usuario As String) As ConsultaPregunta
        Dim dt = Db.Consultar("SELECT email, pregunta_seguridad, respuesta_seguridad FROM usuario
                               WHERE usuario = @u AND esta_activo = 1",
                              New SqlParameter("@u", usuario.Trim()))

        If dt.Rows.Count = 0 Then
            Return New ConsultaPregunta With {.Estado = EstadoPregunta.UsuarioNoExiste}
        End If

        Dim fila = dt.Rows(0)
        Dim email = fila("email").ToString()
        Dim pregunta = If(IsDBNull(fila("pregunta_seguridad")), "", fila("pregunta_seguridad").ToString())
        Dim respuesta = If(IsDBNull(fila("respuesta_seguridad")), "", fila("respuesta_seguridad").ToString())

        ' Ambos campos deben estar completos: sin la respuesta guardada no hay nada que verificar
        If String.IsNullOrWhiteSpace(pregunta) OrElse String.IsNullOrWhiteSpace(respuesta) Then
            Return New ConsultaPregunta With {.Estado = EstadoPregunta.SinConfigurar, .Email = email}
        End If

        Return New ConsultaPregunta With {.Estado = EstadoPregunta.Encontrada, .Pregunta = pregunta, .Email = email}
    End Function

    Public Shared Function VerificarRespuesta(usuario As String, respuesta As String) As Boolean
        If String.IsNullOrWhiteSpace(respuesta) Then Return False

        Dim dt = Db.Consultar("SELECT respuesta_seguridad FROM usuario
                               WHERE usuario = @u AND esta_activo = 1",
                              New SqlParameter("@u", usuario.Trim()))
        If dt.Rows.Count = 0 OrElse IsDBNull(dt.Rows(0)("respuesta_seguridad")) Then Return False

        Dim guardada = dt.Rows(0)("respuesta_seguridad").ToString()
        If String.IsNullOrWhiteSpace(guardada) Then Return False

        Try
            Return BCrypt.Net.BCrypt.Verify(respuesta.Trim().ToLower(), guardada)
        Catch ex As Exception
            ' Una respuesta guardada con formato inválido no debe tumbar la aplicación
            Registro.Advertencia($"Respuesta de seguridad con formato inválido para el usuario {usuario.Trim()}")
            Return False
        End Try
    End Function

    ' ---------- Recuperación por código de correo (alternativa a la pregunta de seguridad) ----------

    ''' <summary>Genera un código de 6 dígitos y lo guarda con vencimiento de 30 minutos.
    ''' Devuelve Nothing y el código + nombre del dueño de la cuenta si salió bien
    ''' (el llamador es responsable de enviarlo por correo); si no, el mensaje de error.</summary>
    Public Shared Function GenerarCodigoRecuperacionPorEmail(email As String, ByRef codigoGenerado As String,
                                                             ByRef nombreCompleto As String) As String
        codigoGenerado = ""
        nombreCompleto = ""

        Dim dt = Db.Consultar("SELECT nombre_completo FROM usuario WHERE email = @e AND esta_activo = 1",
                              New SqlParameter("@e", email.Trim().ToLower()))
        If dt.Rows.Count = 0 Then Return "No existe una cuenta activa con ese correo."

        Dim bytesAleatorios(3) As Byte
        RandomNumberGenerator.Fill(bytesAleatorios)
        Dim codigo = (BitConverter.ToUInt32(bytesAleatorios, 0) Mod 1000000UI).ToString("D6")

        Db.Ejecutar("UPDATE usuario SET codigo_recuperacion = @c,
                                        fecha_expiracion_codigo = DATEADD(MINUTE, 30, GETDATE())
                     WHERE email = @e",
                    New SqlParameter("@c", codigo),
                    New SqlParameter("@e", email.Trim().ToLower()))

        codigoGenerado = codigo
        nombreCompleto = dt.Rows(0)("nombre_completo").ToString()
        Registro.Info($"Código de recuperación generado para el correo: {email.Trim().ToLower()}")
        Return Nothing
    End Function

    Public Shared Function VerificarCodigoRecuperacion(email As String, codigo As String) As Boolean
        If String.IsNullOrWhiteSpace(codigo) Then Return False

        Dim dt = Db.Consultar("SELECT codigo_recuperacion, fecha_expiracion_codigo FROM usuario
                               WHERE email = @e AND esta_activo = 1",
                              New SqlParameter("@e", email.Trim().ToLower()))
        If dt.Rows.Count = 0 Then Return False

        Dim fila = dt.Rows(0)
        If IsDBNull(fila("codigo_recuperacion")) OrElse IsDBNull(fila("fecha_expiracion_codigo")) Then Return False
        If fila("codigo_recuperacion").ToString() <> codigo.Trim() Then Return False
        If DateTime.Now > CDate(fila("fecha_expiracion_codigo")) Then Return False

        Return True
    End Function

    ''' <summary>Cambia la contraseña de la cuenta dueña del correo (ya verificada con su código)
    ''' y limpia el código para que no se pueda reutilizar. Devuelve Nothing si salió bien,
    ''' o el mensaje de error.</summary>
    Public Shared Function CambiarContrasenaPorEmail(email As String, nuevaClave As String) As String
        Dim errorClave = Validador.ValidarContrasena(nuevaClave)
        If errorClave IsNot Nothing Then Return errorClave

        Dim hash As String = BCrypt.Net.BCrypt.HashPassword(nuevaClave, workFactor:=11)
        Dim filas = Db.Ejecutar("UPDATE usuario SET contrasena_hash = @h, debe_cambiar_contrasena = 0,
                                        codigo_recuperacion = NULL, fecha_expiracion_codigo = NULL
                                 WHERE email = @e AND esta_activo = 1",
                                New SqlParameter("@h", hash),
                                New SqlParameter("@e", email.Trim().ToLower()))
        If filas > 0 Then Registro.Info($"Contraseña recuperada por código de correo: {email.Trim().ToLower()}")
        Return If(filas > 0, Nothing, "No se encontró el usuario.")
    End Function

    ' ---------- Configuración de la pregunta desde la cuenta ----------

    Public Shared Function TienePreguntaConfigurada(usuario As String) As Boolean
        Return ObtenerPregunta(usuario).Estado = EstadoPregunta.Encontrada
    End Function

    ''' <summary>Guarda o cambia la pregunta de seguridad de un usuario que ya inició sesión.
    ''' Devuelve Nothing si salió bien, o el mensaje de error.</summary>
    Public Shared Function ConfigurarPregunta(usuario As String, pregunta As String,
                                              respuesta As String) As String
        If String.IsNullOrWhiteSpace(pregunta) Then Return "Selecciona o escribe una pregunta de seguridad."
        If String.IsNullOrWhiteSpace(respuesta) Then Return "Escribe la respuesta de tu pregunta de seguridad."

        Dim hashRespuesta As String = BCrypt.Net.BCrypt.HashPassword(respuesta.Trim().ToLower(), workFactor:=11)
        Dim filas = Db.Ejecutar("UPDATE usuario SET pregunta_seguridad = @p, respuesta_seguridad = @r
                                 WHERE usuario = @u AND esta_activo = 1",
                                New SqlParameter("@p", pregunta.Trim()),
                                New SqlParameter("@r", hashRespuesta),
                                New SqlParameter("@u", usuario.Trim()))

        If filas = 0 Then Return "No se encontró el usuario."
        Registro.Info($"Pregunta de seguridad configurada para el usuario: {usuario.Trim()}")
        Return Nothing
    End Function

    ''' <summary>Cambia la contraseña verificando primero la actual.</summary>
    Public Shared Function CambiarContrasenaConActual(usuario As String, claveActual As String,
                                                      claveNueva As String) As String
        If Autenticar(usuario, claveActual) Is Nothing Then
            Return "La contraseña actual no es correcta."
        End If
        Return CambiarContrasena(usuario, claveNueva)
    End Function

    ''' <summary>Cambia la contraseña. Devuelve Nothing si salió bien, o el mensaje de error.</summary>
    Public Shared Function CambiarContrasena(usuario As String, nuevaClave As String) As String
        Dim errorClave = Validador.ValidarContrasena(nuevaClave)
        If errorClave IsNot Nothing Then Return errorClave

        Dim hash As String = BCrypt.Net.BCrypt.HashPassword(nuevaClave, workFactor:=11)
        ' Cualquier cambio de contraseña (recuperación, autoservicio o clave temporal) cierra la obligación de cambiarla
        Dim filas = Db.Ejecutar("UPDATE usuario SET contrasena_hash = @h, debe_cambiar_contrasena = 0
                                 WHERE usuario = @u AND esta_activo = 1",
                                New SqlParameter("@h", hash),
                                New SqlParameter("@u", usuario.Trim()))
        If filas > 0 Then Registro.Info($"Contraseña recuperada para el usuario: {usuario.Trim()}")
        Return If(filas > 0, Nothing, "No se encontró el usuario.")
    End Function

    ''' <summary>Devuelve el usuario autenticado, o Nothing si las credenciales fallan.</summary>
    Public Shared Function Autenticar(usuario As String, clave As String) As Usuario
        If String.IsNullOrWhiteSpace(usuario) OrElse String.IsNullOrWhiteSpace(clave) Then
            Return Nothing
        End If

        Dim dt = Db.Consultar("SELECT usuario_id, nombre_completo, email, usuario, contrasena_hash, rol,
                                      debe_cambiar_contrasena
                               FROM usuario WHERE usuario = @u AND esta_activo = 1",
                              New SqlParameter("@u", usuario.Trim()))
        If dt.Rows.Count = 0 Then
            Registro.Advertencia($"Intento de inicio de sesión con usuario inexistente: {usuario.Trim()}")
            AuditoriaService.Registrar(usuario, exito:=False, detalle:="Usuario inexistente")
            Return Nothing
        End If

        Dim fila = dt.Rows(0)
        If Not BCrypt.Net.BCrypt.Verify(clave, fila("contrasena_hash").ToString()) Then
            Registro.Advertencia($"Contraseña incorrecta para el usuario: {usuario.Trim()}")
            AuditoriaService.Registrar(usuario, exito:=False, detalle:="Contraseña incorrecta")
            Return Nothing
        End If

        Registro.Info($"Inicio de sesión exitoso: {usuario.Trim()}")
        AuditoriaService.Registrar(usuario, exito:=True)
        Return New Usuario With {
            .UsuarioID = CInt(fila("usuario_id")),
            .NombreCompleto = fila("nombre_completo").ToString(),
            .Email = fila("email").ToString(),
            .NombreUsuario = fila("usuario").ToString(),
            .Rol = fila("rol").ToString(),
            .DebeCambiarContrasena = CBool(fila("debe_cambiar_contrasena"))
        }
    End Function

    ' ---------- Alta de usuarios por un Administrador ----------

    ''' <summary>Crea una cuenta con una contraseña temporal que el nuevo usuario deberá
    ''' cambiar en su primer inicio de sesión. Devuelve Nothing y la clave temporal generada
    ''' si todo salió bien; si no, el mensaje de error y una clave vacía.</summary>
    Public Shared Function CrearPorAdministrador(nombreCompleto As String, email As String,
                                                 usuario As String, rol As String,
                                                 ByRef claveTemporal As String) As String
        claveTemporal = ""

        If String.IsNullOrWhiteSpace(nombreCompleto) Then Return "Escribe el nombre completo."
        If Not Validador.EsEmailValido(email) Then Return "El correo electrónico no es válido."
        If Not Validador.EsUsuarioValido(usuario) Then Return "El usuario debe tener de 4 a 30 caracteres (letras, números o _)."
        If rol <> "Administrador" AndAlso rol <> "Operador" Then Return "Selecciona un rol válido."
        If ExisteUsuario(usuario) Then Return "Ese nombre de usuario ya está registrado."
        If ExisteEmail(email) Then Return "Ese correo ya está registrado."

        Dim clave = GeneradorClave.GenerarTemporal()
        Dim hash = BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=11)

        Db.Ejecutar("INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol,
                                          debe_cambiar_contrasena)
                     VALUES (@n, @e, @u, @h, @r, 1)",
                    New SqlParameter("@n", nombreCompleto.Trim()),
                    New SqlParameter("@e", email.Trim().ToLower()),
                    New SqlParameter("@u", usuario.Trim()),
                    New SqlParameter("@h", hash),
                    New SqlParameter("@r", rol))

        Registro.Info($"Usuario creado por administrador: {usuario.Trim()} con rol {rol}")
        claveTemporal = clave
        Return Nothing
    End Function
End Class
