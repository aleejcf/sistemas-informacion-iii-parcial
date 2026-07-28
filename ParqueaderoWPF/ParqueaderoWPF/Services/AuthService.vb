Imports Microsoft.Data.SqlClient

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
    End Class

    ''' <summary>Busca la pregunta de seguridad e informa por qué no se pudo obtener,
    ''' para poder guiar al usuario con un mensaje útil.</summary>
    Public Shared Function ObtenerPregunta(usuario As String) As ConsultaPregunta
        Dim dt = Db.Consultar("SELECT pregunta_seguridad, respuesta_seguridad FROM usuario
                               WHERE usuario = @u AND esta_activo = 1",
                              New SqlParameter("@u", usuario.Trim()))

        If dt.Rows.Count = 0 Then
            Return New ConsultaPregunta With {.Estado = EstadoPregunta.UsuarioNoExiste}
        End If

        Dim fila = dt.Rows(0)
        Dim pregunta = If(IsDBNull(fila("pregunta_seguridad")), "", fila("pregunta_seguridad").ToString())
        Dim respuesta = If(IsDBNull(fila("respuesta_seguridad")), "", fila("respuesta_seguridad").ToString())

        ' Ambos campos deben estar completos: sin la respuesta guardada no hay nada que verificar
        If String.IsNullOrWhiteSpace(pregunta) OrElse String.IsNullOrWhiteSpace(respuesta) Then
            Return New ConsultaPregunta With {.Estado = EstadoPregunta.SinConfigurar}
        End If

        Return New ConsultaPregunta With {.Estado = EstadoPregunta.Encontrada, .Pregunta = pregunta}
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
        Dim filas = Db.Ejecutar("UPDATE usuario SET contrasena_hash = @h WHERE usuario = @u AND esta_activo = 1",
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

        Dim dt = Db.Consultar("SELECT usuario_id, nombre_completo, email, usuario, contrasena_hash, rol
                               FROM usuario WHERE usuario = @u AND esta_activo = 1",
                              New SqlParameter("@u", usuario.Trim()))
        If dt.Rows.Count = 0 Then
            Registro.Advertencia($"Intento de inicio de sesión con usuario inexistente: {usuario.Trim()}")
            Return Nothing
        End If

        Dim fila = dt.Rows(0)
        If Not BCrypt.Net.BCrypt.Verify(clave, fila("contrasena_hash").ToString()) Then
            Registro.Advertencia($"Contraseña incorrecta para el usuario: {usuario.Trim()}")
            Return Nothing
        End If

        Registro.Info($"Inicio de sesión exitoso: {usuario.Trim()}")
        Return New Usuario With {
            .UsuarioID = CInt(fila("usuario_id")),
            .NombreCompleto = fila("nombre_completo").ToString(),
            .Email = fila("email").ToString(),
            .NombreUsuario = fila("usuario").ToString(),
            .Rol = fila("rol").ToString()
        }
    End Function
End Class
