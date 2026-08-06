Imports System.Data
Imports System.Security.Cryptography
Imports Microsoft.Data.SqlClient

''' <summary>Registro, inicio de sesión y recuperación de contraseña.
''' Las contraseñas y las respuestas de seguridad se guardan con hash BCrypt,
''' nunca en texto plano.</summary>
Public Class AuthService

    ''' <summary>Factor de trabajo de BCrypt: cada punto duplica el costo de
    ''' calcular el hash. 11 es el equilibrio recomendado entre seguridad y
    ''' que el inicio de sesión siga sintiéndose instantáneo.</summary>
    Private Const FACTOR_BCRYPT As Integer = 11

    Public Shared ReadOnly Roles As String() = {"Administrador", "Bibliotecario"}

    Public Shared ReadOnly PreguntasSugeridas As String() = {
        "¿Cuál fue tu primera mascota?",
        "¿En qué ciudad naciste?",
        "¿Cuál es el nombre de tu mejor amigo de la infancia?",
        "¿Cuál fue el primer libro que leíste completo?",
        "¿Cuál es tu comida favorita?",
        "¿Cómo se llamaba tu escuela primaria?"
    }

    ' ---------- Consultas de existencia ----------

    Public Shared Function HayUsuarios() As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM usuario") > 0
    End Function

    Public Shared Function ExisteUsuario(usuario As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM usuario WHERE usuario = @u",
                         New SqlParameter("@u", usuario.Trim())) > 0
    End Function

    Public Shared Function ExisteEmail(email As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM usuario WHERE email = @e",
                         New SqlParameter("@e", email.Trim().ToLower())) > 0
    End Function

    ' ---------- Registro ----------

    ''' <summary>Registra una cuenta. Devuelve Nothing si todo salió bien; si no,
    ''' el mensaje de error para mostrar al usuario.
    ''' El primer usuario que se registra queda como Administrador porque alguien
    ''' tiene que poder crear a los demás; los siguientes entran como Bibliotecarios.</summary>
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

        Dim rol As String = If(HayUsuarios(), "Bibliotecario", "Administrador")
        Dim hash As String = BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=FACTOR_BCRYPT)
        ' La respuesta también va con hash, normalizada a minúsculas y sin espacios
        Dim hashRespuesta As String = BCrypt.Net.BCrypt.HashPassword(respuesta.Trim().ToLower(),
                                                                     workFactor:=FACTOR_BCRYPT)

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

        Registro.Info($"Cuenta registrada: {usuario.Trim()} con rol {rol}")
        BitacoraService.Registrar(BitacoraService.REGISTRO_CUENTA, "usuario",
                                  $"Cuenta {usuario.Trim()} creada con rol {rol}",
                                  usuario:=usuario.Trim())
        Return Nothing
    End Function

    ' ---------- Inicio de sesión ----------

    ''' <summary>Devuelve el usuario autenticado, o Nothing si las credenciales fallan.
    ''' El mensaje que ve quien intenta entrar es el mismo tanto si el usuario no
    ''' existe como si la contraseña está mal: revelar cuál de los dos falló le
    ''' diría a un atacante qué usuarios sí existen.</summary>
    Public Shared Function Autenticar(usuario As String, clave As String) As Usuario
        If String.IsNullOrWhiteSpace(usuario) OrElse String.IsNullOrWhiteSpace(clave) Then
            Return Nothing
        End If

        Dim nombre = usuario.Trim()
        Dim fila = Db.ConsultarFila("SELECT usuario_id, nombre_completo, email, usuario,
                                            contrasena_hash, rol, debe_cambiar_contrasena
                                     FROM usuario WHERE usuario = @u AND esta_activo = 1",
                                    New SqlParameter("@u", nombre))

        If fila Is Nothing Then
            Registro.Advertencia($"Intento de inicio de sesión con usuario inexistente: {nombre}")
            BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario",
                                      "Usuario inexistente o inactivo", exito:=False, usuario:=nombre)
            Return Nothing
        End If

        If Not VerificarHash(clave, fila("contrasena_hash").ToString()) Then
            Registro.Advertencia($"Contraseña incorrecta para el usuario: {nombre}")
            BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario",
                                      "Contraseña incorrecta", exito:=False, usuario:=nombre)
            Return Nothing
        End If

        Db.Ejecutar("UPDATE usuario SET ultimo_acceso = GETDATE() WHERE usuario_id = @id",
                    New SqlParameter("@id", CInt(fila("usuario_id"))))

        Registro.Info($"Inicio de sesión exitoso: {nombre}")
        BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario", Nothing,
                                  exito:=True, usuario:=nombre)

        Return New Usuario With {
            .UsuarioID = CInt(fila("usuario_id")),
            .NombreCompleto = fila("nombre_completo").ToString(),
            .Email = fila("email").ToString(),
            .NombreUsuario = fila("usuario").ToString(),
            .Rol = fila("rol").ToString(),
            .DebeCambiarContrasena = CBool(fila("debe_cambiar_contrasena"))
        }
    End Function

    ''' <summary>Un hash con formato inválido en la base de datos no debe tumbar
    ''' la aplicación: se trata como credencial incorrecta.</summary>
    Private Shared Function VerificarHash(texto As String, hashGuardado As String) As Boolean
        If String.IsNullOrWhiteSpace(hashGuardado) Then Return False
        Try
            Return BCrypt.Net.BCrypt.Verify(texto, hashGuardado)
        Catch ex As Exception
            Registro.Advertencia($"Hash con formato inválido en la base de datos: {ex.Message}")
            Return False
        End Try
    End Function

    ' ---------- Recuperación por pregunta de seguridad ----------

    Public Enum EstadoPregunta
        Encontrada
        UsuarioNoExiste
        SinConfigurar
    End Enum

    Public Class ConsultaPregunta
        Public Property Estado As EstadoPregunta
        Public Property Pregunta As String
        ''' <summary>Correo de la cuenta: permite ofrecer la recuperación por código
        ''' cuando no hay pregunta configurada, o como camino alterno.</summary>
        Public Property Email As String
    End Class

    ''' <summary>Busca la pregunta de seguridad e informa por qué no se pudo obtener,
    ''' para poder guiar al usuario con un mensaje útil en lugar de un "no se pudo".</summary>
    Public Shared Function ObtenerPregunta(usuario As String) As ConsultaPregunta
        Dim fila = Db.ConsultarFila("SELECT email, pregunta_seguridad, respuesta_seguridad
                                     FROM usuario WHERE usuario = @u AND esta_activo = 1",
                                    New SqlParameter("@u", usuario.Trim()))

        If fila Is Nothing Then
            Return New ConsultaPregunta With {.Estado = EstadoPregunta.UsuarioNoExiste}
        End If

        Dim email = fila("email").ToString()
        Dim pregunta = Db.Texto(fila, "pregunta_seguridad")
        Dim respuesta = Db.Texto(fila, "respuesta_seguridad")

        ' Los dos campos deben estar completos: sin respuesta guardada no hay nada que verificar
        If String.IsNullOrWhiteSpace(pregunta) OrElse String.IsNullOrWhiteSpace(respuesta) Then
            Return New ConsultaPregunta With {.Estado = EstadoPregunta.SinConfigurar, .Email = email}
        End If

        Return New ConsultaPregunta With {
            .Estado = EstadoPregunta.Encontrada, .Pregunta = pregunta, .Email = email
        }
    End Function

    Public Shared Function VerificarRespuesta(usuario As String, respuesta As String) As Boolean
        If String.IsNullOrWhiteSpace(respuesta) Then Return False

        Dim fila = Db.ConsultarFila("SELECT respuesta_seguridad FROM usuario
                                     WHERE usuario = @u AND esta_activo = 1",
                                    New SqlParameter("@u", usuario.Trim()))
        If fila Is Nothing OrElse IsDBNull(fila("respuesta_seguridad")) Then Return False

        Return VerificarHash(respuesta.Trim().ToLower(), fila("respuesta_seguridad").ToString())
    End Function

    ' ---------- Recuperación por código enviado al correo ----------

    ''' <summary>Genera un código de 6 dígitos con vencimiento de 30 minutos.
    ''' Devuelve Nothing y, por referencia, el código y el nombre del dueño de la
    ''' cuenta si salió bien; si no, el mensaje de error.</summary>
    Public Shared Function GenerarCodigoRecuperacion(email As String, ByRef codigoGenerado As String,
                                                     ByRef nombreCompleto As String) As String
        codigoGenerado = ""
        nombreCompleto = ""

        Dim fila = Db.ConsultarFila("SELECT nombre_completo FROM usuario
                                     WHERE email = @e AND esta_activo = 1",
                                    New SqlParameter("@e", email.Trim().ToLower()))
        If fila Is Nothing Then Return "No existe una cuenta activa con ese correo."

        ' RandomNumberGenerator y no Random: un código de recuperación predecible
        ' es un código que se puede adivinar.
        Dim bytesAleatorios(3) As Byte
        RandomNumberGenerator.Fill(bytesAleatorios)
        Dim codigo = (BitConverter.ToUInt32(bytesAleatorios, 0) Mod 1000000UI).ToString("D6")

        Db.Ejecutar("UPDATE usuario SET codigo_recuperacion = @c,
                                        fecha_expiracion_codigo = DATEADD(MINUTE, 30, GETDATE())
                     WHERE email = @e",
                    New SqlParameter("@c", codigo),
                    New SqlParameter("@e", email.Trim().ToLower()))

        codigoGenerado = codigo
        nombreCompleto = fila("nombre_completo").ToString()
        Registro.Info($"Código de recuperación generado para: {email.Trim().ToLower()}")
        Return Nothing
    End Function

    Public Shared Function VerificarCodigoRecuperacion(email As String, codigo As String) As Boolean
        If String.IsNullOrWhiteSpace(codigo) Then Return False

        Dim fila = Db.ConsultarFila("SELECT codigo_recuperacion, fecha_expiracion_codigo
                                     FROM usuario WHERE email = @e AND esta_activo = 1",
                                    New SqlParameter("@e", email.Trim().ToLower()))
        If fila Is Nothing Then Return False
        If IsDBNull(fila("codigo_recuperacion")) OrElse IsDBNull(fila("fecha_expiracion_codigo")) Then Return False
        If fila("codigo_recuperacion").ToString() <> codigo.Trim() Then Return False
        If DateTime.Now > CDate(fila("fecha_expiracion_codigo")) Then Return False

        Return True
    End Function

    ''' <summary>Cambia la contraseña de la cuenta dueña del correo (ya verificada
    ''' con su código) y limpia el código para que no se pueda reutilizar.</summary>
    Public Shared Function CambiarContrasenaPorEmail(email As String, nuevaClave As String) As String
        Dim errorClave = Validador.ValidarContrasena(nuevaClave)
        If errorClave IsNot Nothing Then Return errorClave

        Dim hash As String = BCrypt.Net.BCrypt.HashPassword(nuevaClave, workFactor:=FACTOR_BCRYPT)
        Dim filas = Db.Ejecutar("UPDATE usuario SET contrasena_hash = @h, debe_cambiar_contrasena = 0,
                                        codigo_recuperacion = NULL, fecha_expiracion_codigo = NULL
                                 WHERE email = @e AND esta_activo = 1",
                                New SqlParameter("@h", hash),
                                New SqlParameter("@e", email.Trim().ToLower()))

        If filas = 0 Then Return "No se encontró la cuenta."
        Registro.Info($"Contraseña recuperada por código: {email.Trim().ToLower()}")
        BitacoraService.Registrar(BitacoraService.RECUPERACION, "usuario",
                                  $"Recuperación por código para {email.Trim().ToLower()}")
        Return Nothing
    End Function

    ' ---------- Cambios desde la propia cuenta ----------

    Public Shared Function TienePreguntaConfigurada(usuario As String) As Boolean
        Return ObtenerPregunta(usuario).Estado = EstadoPregunta.Encontrada
    End Function

    ''' <summary>Guarda o cambia la pregunta de seguridad de un usuario ya autenticado.</summary>
    Public Shared Function ConfigurarPregunta(usuario As String, pregunta As String,
                                              respuesta As String) As String
        If String.IsNullOrWhiteSpace(pregunta) Then Return "Selecciona o escribe una pregunta de seguridad."
        If String.IsNullOrWhiteSpace(respuesta) Then Return "Escribe la respuesta de tu pregunta de seguridad."

        Dim hashRespuesta As String = BCrypt.Net.BCrypt.HashPassword(respuesta.Trim().ToLower(),
                                                                     workFactor:=FACTOR_BCRYPT)
        Dim filas = Db.Ejecutar("UPDATE usuario SET pregunta_seguridad = @p, respuesta_seguridad = @r
                                 WHERE usuario = @u AND esta_activo = 1",
                                New SqlParameter("@p", pregunta.Trim()),
                                New SqlParameter("@r", hashRespuesta),
                                New SqlParameter("@u", usuario.Trim()))

        If filas = 0 Then Return "No se encontró la cuenta."
        Registro.Info($"Pregunta de seguridad configurada para: {usuario.Trim()}")
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

    ''' <summary>Cambia la contraseña. Cualquier cambio (recuperación, autoservicio
    ''' o clave temporal) cierra la obligación de cambiarla.</summary>
    Public Shared Function CambiarContrasena(usuario As String, nuevaClave As String) As String
        Dim errorClave = Validador.ValidarContrasena(nuevaClave)
        If errorClave IsNot Nothing Then Return errorClave

        Dim hash As String = BCrypt.Net.BCrypt.HashPassword(nuevaClave, workFactor:=FACTOR_BCRYPT)
        Dim filas = Db.Ejecutar("UPDATE usuario SET contrasena_hash = @h, debe_cambiar_contrasena = 0
                                 WHERE usuario = @u AND esta_activo = 1",
                                New SqlParameter("@h", hash),
                                New SqlParameter("@u", usuario.Trim()))

        If filas = 0 Then Return "No se encontró la cuenta."
        Registro.Info($"Contraseña cambiada para: {usuario.Trim()}")
        BitacoraService.Registrar(BitacoraService.CAMBIO_CLAVE, "usuario", Nothing,
                                  usuario:=usuario.Trim())
        Return Nothing
    End Function

    ' ---------- Alta de cuentas por un Administrador ----------

    ''' <summary>Crea una cuenta con contraseña temporal que su dueño deberá cambiar
    ''' al primer inicio de sesión. Devuelve Nothing y la clave generada si salió
    ''' bien; si no, el mensaje de error y una clave vacía.</summary>
    Public Shared Function CrearPorAdministrador(nombreCompleto As String, email As String,
                                                 usuario As String, rol As String,
                                                 ByRef claveTemporal As String) As String
        claveTemporal = ""

        If String.IsNullOrWhiteSpace(nombreCompleto) Then Return "Escribe el nombre completo."
        If Not Validador.EsEmailValido(email) Then Return "El correo electrónico no es válido."
        If Not Validador.EsUsuarioValido(usuario) Then Return "El usuario debe tener de 4 a 30 caracteres (letras, números o _)."
        If Not Roles.Contains(rol) Then Return "Selecciona un rol válido."
        If ExisteUsuario(usuario) Then Return "Ese nombre de usuario ya está registrado."
        If ExisteEmail(email) Then Return "Ese correo ya está registrado."

        Dim clave = GeneradorClave.GenerarTemporal()
        Dim hash = BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=FACTOR_BCRYPT)

        Db.Ejecutar("INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol,
                                          debe_cambiar_contrasena)
                     VALUES (@n, @e, @u, @h, @r, 1)",
                    New SqlParameter("@n", nombreCompleto.Trim()),
                    New SqlParameter("@e", email.Trim().ToLower()),
                    New SqlParameter("@u", usuario.Trim()),
                    New SqlParameter("@h", hash),
                    New SqlParameter("@r", rol))

        Registro.Info($"Cuenta creada por administrador: {usuario.Trim()} con rol {rol}")
        BitacoraService.Registrar(BitacoraService.CREAR, "usuario",
                                  $"Cuenta {usuario.Trim()} creada con rol {rol}")
        claveTemporal = clave
        Return Nothing
    End Function
End Class
