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

    ''' <summary>Contraseñas seguidas que se pueden errar antes de que la cuenta
    ''' se bloquee, y cuánto dura el castigo.</summary>
    Public Const INTENTOS_PERMITIDOS As Integer = 3
    Public Const SEGUNDOS_BLOQUEO As Integer = 30

    ''' <summary>El mismo mensaje tanto si el usuario no existe como si la
    ''' contraseña está mal: cualquier diferencia entre los dos le diría a un
    ''' atacante qué nombres de usuario sí están registrados.</summary>
    Private Const CREDENCIAL_INVALIDA As String = "Usuario o contraseña incorrectos."

    Public Shared ReadOnly PreguntasSugeridas As String() = {
        "¿Cuál fue tu primera mascota?",
        "¿En qué ciudad naciste?",
        "¿Cuál es el nombre de tu mejor amigo de la infancia?",
        "¿Cuál fue tu primer trabajo?",
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

    ''' <summary>Datos de viaje que hacen falta para abrir una cuenta de pasajero.
    ''' Van juntos porque una cuenta de pasajero sin ficha de viajero no sirve
    ''' de nada: no podría comprar un boleto ni hacer check-in.</summary>
    Public Class DatosPasajero
        Public Property Nombre As String
        Public Property ApellidoPaterno As String
        Public Property ApellidoMaterno As String
        Public Property TipoDocumento As String
        Public Property NumDocumento As String
        Public Property FechaNacimiento As Date?
        Public Property IdPais As String
        Public Property Telefono As String
    End Class

    ''' <summary>¿Todavía no hay nadie? Entonces quien se registre será el
    ''' Administrador que después crea a los demás.</summary>
    Public Shared Function EsPrimeraCuenta() As Boolean
        Return Not HayUsuarios()
    End Function

    ''' <summary>Crea la primera cuenta del sistema, que queda como Administrador.
    ''' Solo funciona si la tabla está vacía: después de eso las cuentas del
    ''' personal las crea un Administrador desde la pantalla de Usuarios, y el
    ''' registro público pasa a ser el de pasajeros.</summary>
    Public Shared Function RegistrarAdministradorInicial(nombreCompleto As String, email As String,
                                                         usuario As String, clave As String,
                                                         pregunta As String, respuesta As String) As String
        If HayUsuarios() Then
            Return "Ya existe al menos una cuenta en el sistema. " &
                   "Las cuentas del personal las crea un Administrador desde la pantalla de Usuarios."
        End If

        Dim problema = ValidarCuenta(nombreCompleto, email, usuario, clave, pregunta, respuesta)
        If problema IsNot Nothing Then Return problema

        Db.Ejecutar("INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol,
                                          pregunta_seguridad, respuesta_seguridad)
                     VALUES (@n, @e, @u, @h, 'Administrador', @p, @rs)",
                    New SqlParameter("@n", nombreCompleto.Trim()),
                    New SqlParameter("@e", email.Trim().ToLower()),
                    New SqlParameter("@u", usuario.Trim()),
                    New SqlParameter("@h", BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=FACTOR_BCRYPT)),
                    New SqlParameter("@p", pregunta.Trim()),
                    New SqlParameter("@rs", HashRespuesta(respuesta)))

        Registro.Info($"Cuenta inicial registrada: {usuario.Trim()} como Administrador")
        BitacoraService.Registrar(BitacoraService.REGISTRO_CUENTA, "usuario",
                                  $"Cuenta {usuario.Trim()} creada como Administrador inicial",
                                  usuario:=usuario.Trim())
        Return Nothing
    End Function

    ''' <summary>Registro público de un pasajero: crea su cuenta y su ficha de
    ''' viajero de una sola vez, dentro de una transacción. Si algo falla no puede
    ''' quedar una cuenta sin ficha (no podría viajar) ni una ficha suelta con los
    ''' datos de alguien que nunca terminó de registrarse.
    '''
    ''' Si la persona ya estaba en el sistema —porque un agente la registró en el
    ''' mostrador— se reutiliza su ficha en vez de duplicarla, y así conserva su
    ''' historial de vuelos.</summary>
    Public Shared Function RegistrarPasajero(nombreCompleto As String, email As String,
                                             usuario As String, clave As String,
                                             pregunta As String, respuesta As String,
                                             datos As DatosPasajero,
                                             Optional googleId As String = Nothing) As String

        Dim conGoogle = Not String.IsNullOrWhiteSpace(googleId)

        Dim problema = ValidarCuenta(nombreCompleto, email, usuario, clave, pregunta, respuesta, conGoogle)
        If problema IsNot Nothing Then Return problema

        ' Quien entra con Google no elige contraseña: se guarda una aleatoria que
        ' nadie conoce, y si algún día quiere entrar sin Google la restablece por correo.
        If conGoogle Then
            clave = GeneradorClave.GenerarTemporal() & GeneradorClave.GenerarTemporal()
            pregunta = Nothing
            respuesta = Nothing
        End If

        If datos Is Nothing Then Return "Faltan tus datos de viajero."
        If String.IsNullOrWhiteSpace(datos.Nombre) Then Return "Escribe tu nombre."
        If String.IsNullOrWhiteSpace(datos.ApellidoPaterno) Then Return "Escribe tu apellido paterno."
        If String.IsNullOrWhiteSpace(datos.TipoDocumento) Then Return "Selecciona tu tipo de documento."
        If String.IsNullOrWhiteSpace(datos.NumDocumento) Then Return "Escribe tu número de documento."
        If String.IsNullOrWhiteSpace(datos.IdPais) Then Return "Selecciona tu país."

        Dim errorFecha = Validador.ValidarFechaNacimiento(datos.FechaNacimiento)
        If errorFecha IsNot Nothing Then Return errorFecha

        Dim mensaje As String = Nothing
        Dim codigoPasajero As String = Nothing

        Db.EnTransaccion(
            Sub(cn, tx)
                ' ¿Ya lo conocemos? El documento es lo que identifica a una persona
                Dim existente = Db.ConsultarEn(cn, tx,
                    "SELECT idpasajero FROM pasajero WHERE num_documento = @d",
                    New SqlParameter("@d", datos.NumDocumento.Trim()))

                If existente.Rows.Count > 0 Then
                    codigoPasajero = existente.Rows(0)("idpasajero").ToString()

                    Dim yaTieneCuenta = CInt(Db.EscalarEn(cn, tx,
                        "SELECT COUNT(*) FROM usuario WHERE idpasajero = @p",
                        New SqlParameter("@p", codigoPasajero)))

                    If yaTieneCuenta > 0 Then
                        mensaje = "Ya existe una cuenta para ese número de documento. " &
                                  "Si es tuya, entra con ella o recupera la contraseña."
                        Return
                    End If
                Else
                    ' Ficha nueva: el código se calcula dentro de la transacción para
                    ' que dos registros simultáneos no se peleen el mismo número
                    Dim maximo = Db.EscalarEn(cn, tx,
                        "SELECT MAX(CAST(SUBSTRING(idpasajero, 2, 7) AS INT)) FROM pasajero
                          WHERE idpasajero LIKE 'P[0-9][0-9][0-9][0-9][0-9][0-9][0-9]'")

                    Dim siguiente = If(maximo Is Nothing OrElse IsDBNull(maximo), 1, CInt(maximo) + 1)
                    codigoPasajero = "P" & siguiente.ToString("D7")

                    Db.EjecutarEn(cn, tx,
                        "INSERT INTO pasajero (idpasajero, nombre_p, apaterno, amaterno,
                                               tipo_documento, num_documento, fecha_nacimiento,
                                               idpais, telefono, email)
                         VALUES (@p, @n, @ap, @am, @td, @nd, @fn, @pa, @tel, @em)",
                        New SqlParameter("@p", codigoPasajero),
                        New SqlParameter("@n", datos.Nombre.Trim()),
                        New SqlParameter("@ap", datos.ApellidoPaterno.Trim()),
                        New SqlParameter("@am", Db.Opcional(datos.ApellidoMaterno)),
                        New SqlParameter("@td", datos.TipoDocumento),
                        New SqlParameter("@nd", datos.NumDocumento.Trim()),
                        New SqlParameter("@fn", datos.FechaNacimiento.Value.Date),
                        New SqlParameter("@pa", datos.IdPais),
                        New SqlParameter("@tel", Db.Opcional(datos.Telefono)),
                        New SqlParameter("@em", email.Trim().ToLower()))
                End If

                Db.EjecutarEn(cn, tx,
                    "INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol,
                                          pregunta_seguridad, respuesta_seguridad, idpasajero, google_id)
                     VALUES (@n, @e, @u, @h, 'Pasajero', @p, @rs, @idp, @g)",
                    New SqlParameter("@n", nombreCompleto.Trim()),
                    New SqlParameter("@e", email.Trim().ToLower()),
                    New SqlParameter("@u", usuario.Trim()),
                    New SqlParameter("@h", BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=FACTOR_BCRYPT)),
                    New SqlParameter("@p", Db.Opcional(pregunta)),
                    New SqlParameter("@rs", If(String.IsNullOrWhiteSpace(respuesta),
                                               CObj(DBNull.Value), HashRespuesta(respuesta))),
                    New SqlParameter("@idp", codigoPasajero),
                    New SqlParameter("@g", Db.Opcional(googleId)))
            End Sub)

        If mensaje IsNot Nothing Then Return mensaje

        Registro.Info($"Pasajero registrado: {usuario.Trim()} ligado a {codigoPasajero}")
        BitacoraService.Registrar(BitacoraService.REGISTRO_CUENTA, "usuario",
                                  $"Cuenta de pasajero {usuario.Trim()} ligada a {codigoPasajero}",
                                  usuario:=usuario.Trim())
        Return Nothing
    End Function

    ''' <summary>Validaciones comunes a cualquier cuenta nueva. Una cuenta ligada a
    ''' Google no lleva contraseña ni pregunta de seguridad: su recuperación la
    ''' resuelve Google, que para eso es el dueño de la identidad.</summary>
    Private Shared Function ValidarCuenta(nombreCompleto As String, email As String,
                                          usuario As String, clave As String,
                                          pregunta As String, respuesta As String,
                                          Optional conGoogle As Boolean = False) As String
        If String.IsNullOrWhiteSpace(nombreCompleto) Then Return "Escribe tu nombre completo."
        If Not Validador.EsEmailValido(email) Then Return "El correo electrónico no es válido."
        If Not Validador.EsUsuarioValido(usuario) Then Return "El usuario debe tener de 4 a 30 caracteres (letras, números o _)."

        If Not conGoogle Then
            Dim errorClave = Validador.ValidarContrasena(clave)
            If errorClave IsNot Nothing Then Return errorClave

            If String.IsNullOrWhiteSpace(pregunta) OrElse String.IsNullOrWhiteSpace(respuesta) Then
                Return "Selecciona una pregunta de seguridad y escribe la respuesta (sirve para recuperar tu cuenta)."
            End If
        End If

        If ExisteUsuario(usuario) Then Return "Ese nombre de usuario ya está registrado."
        If ExisteEmail(email) Then Return "Ese correo ya está registrado."
        Return Nothing
    End Function

    ' ---------- Identidad de Google ----------

    Public Class ResultadoAccesoGoogle
        Public Property Cuenta As Usuario
        ''' <summary>True cuando la persona de Google todavía no tiene cuenta aquí:
        ''' hay que pedirle sus datos de viajero antes de poder crearla.</summary>
        Public Property NecesitaRegistro As Boolean
        Public Property Mensaje As String
    End Class

    ''' <summary>Entra con una identidad ya verificada por Google.
    '''
    ''' Se busca primero por el identificador de Google y solo después por el
    ''' correo: una persona puede cambiar de correo en Google y seguir siendo la
    ''' misma, mientras que el `sub` nunca cambia. Cuando se encuentra por correo,
    ''' las dos identidades quedan ligadas para la próxima vez.</summary>
    Public Shared Function AutenticarConGoogle(identidad As GoogleAuthService.IdentidadGoogle) As ResultadoAccesoGoogle
        If identidad Is Nothing OrElse String.IsNullOrWhiteSpace(identidad.GoogleId) Then
            Return New ResultadoAccesoGoogle With {.Mensaje = "Google no devolvió una identidad válida."}
        End If

        Dim correo = If(identidad.Email, "").Trim().ToLower()

        Dim fila = Db.ConsultarFila("SELECT usuario_id, nombre_completo, email, usuario, rol,
                                            debe_cambiar_contrasena, idpasajero, esta_activo
                                     FROM usuario WHERE google_id = @g",
                                    New SqlParameter("@g", identidad.GoogleId))

        If fila Is Nothing AndAlso correo.Length > 0 Then
            ' Ya tenía cuenta con ese correo: se liga y desde ahora entra con Google
            fila = Db.ConsultarFila("SELECT usuario_id, nombre_completo, email, usuario, rol,
                                            debe_cambiar_contrasena, idpasajero, esta_activo
                                     FROM usuario WHERE email = @e AND google_id IS NULL",
                                    New SqlParameter("@e", correo))

            If fila IsNot Nothing Then
                Db.Ejecutar("UPDATE usuario SET google_id = @g WHERE usuario_id = @id",
                            New SqlParameter("@g", identidad.GoogleId),
                            New SqlParameter("@id", CInt(fila("usuario_id"))))

                Registro.Info($"Cuenta {fila("usuario")} ligada a la identidad de Google {correo}")
                BitacoraService.Registrar(BitacoraService.EDITAR, "usuario",
                                          "Cuenta ligada a Google", usuario:=fila("usuario").ToString())
            End If
        End If

        If fila Is Nothing Then
            BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario",
                                      $"Google sin cuenta en el sistema: {correo}",
                                      exito:=False, usuario:=correo)
            Return New ResultadoAccesoGoogle With {.NecesitaRegistro = True}
        End If

        If Not CBool(fila("esta_activo")) Then
            BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario",
                                      "Cuenta desactivada (Google)", exito:=False,
                                      usuario:=fila("usuario").ToString())
            Return New ResultadoAccesoGoogle With {
                .Mensaje = "Tu cuenta está desactivada. Comunícate con la aerolínea."
            }
        End If

        Db.Ejecutar("UPDATE usuario SET ultimo_acceso = GETDATE() WHERE usuario_id = @id",
                    New SqlParameter("@id", CInt(fila("usuario_id"))))

        BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario", "Con Google",
                                  exito:=True, usuario:=fila("usuario").ToString())

        Return New ResultadoAccesoGoogle With {
            .Cuenta = New Usuario With {
                .UsuarioID = CInt(fila("usuario_id")),
                .NombreCompleto = fila("nombre_completo").ToString(),
                .Email = fila("email").ToString(),
                .NombreUsuario = fila("usuario").ToString(),
                .Rol = fila("rol").ToString(),
                .DebeCambiarContrasena = CBool(fila("debe_cambiar_contrasena")),
                .IdPasajero = If(IsDBNull(fila("idpasajero")), Nothing, fila("idpasajero").ToString())
            }
        }
    End Function

    ''' <summary>Propone un nombre de usuario libre a partir del correo de Google,
    ''' para que la persona no tenga que inventarse uno.</summary>
    Public Shared Function SugerirUsuarioDesdeEmail(email As String) As String
        If String.IsNullOrWhiteSpace(email) Then Return ""

        Dim base = New String(email.Split("@"c)(0).
                              Where(Function(c) Char.IsLetterOrDigit(c) OrElse c = "_"c).ToArray())
        If base.Length < 4 Then base = (base & "vuela").Substring(0, 5)
        If base.Length > 26 Then base = base.Substring(0, 26)

        Try
            If Not ExisteUsuario(base) Then Return base
            For intento = 1 To 99
                Dim candidato = $"{base}{intento}"
                If Not ExisteUsuario(candidato) Then Return candidato
            Next
        Catch ex As Exception
            Registro.Advertencia($"No se pudo sugerir un usuario: {ex.Message}")
        End Try

        Return base
    End Function

    ''' <summary>La respuesta de seguridad también va con hash, normalizada a
    ''' minúsculas y sin espacios de sobra.</summary>
    Private Shared Function HashRespuesta(respuesta As String) As String
        Return BCrypt.Net.BCrypt.HashPassword(respuesta.Trim().ToLower(), workFactor:=FACTOR_BCRYPT)
    End Function

    ' ---------- Inicio de sesión ----------

    ''' <summary>Resultado de un intento de acceso: la cuenta si entró, y si no,
    ''' el motivo y cuántos segundos falta esperar cuando la cuenta está bloqueada.</summary>
    Public Class ResultadoAcceso
        Public Property Cuenta As Usuario
        ''' <summary>Segundos que faltan para poder volver a intentar; 0 si no hay bloqueo.</summary>
        Public Property SegundosBloqueo As Integer
        Public Property Mensaje As String

        Public ReadOnly Property Exitoso As Boolean
            Get
                Return Cuenta IsNot Nothing
            End Get
        End Property
    End Class

    ''' <summary>Devuelve la cuenta autenticada, o el motivo por el que no se pudo.
    '''
    ''' El bloqueo por intentos fallidos se cuenta y se aplica AQUÍ, sobre la
    ''' cuenta, y no en la ventana: si viviera en la pantalla bastaría con cerrarla
    ''' y volver a abrirla para reiniciar el contador, y una segunda terminal no se
    ''' enteraría siquiera. Guardado en la cuenta, el castigo vale para todas.</summary>
    Public Shared Function Autenticar(usuario As String, clave As String) As ResultadoAcceso
        If String.IsNullOrWhiteSpace(usuario) OrElse String.IsNullOrWhiteSpace(clave) Then
            Return New ResultadoAcceso With {.Mensaje = CREDENCIAL_INVALIDA}
        End If

        Dim nombre = usuario.Trim()
        Dim fila = Db.ConsultarFila("SELECT usuario_id, nombre_completo, email, usuario,
                                            contrasena_hash, rol, debe_cambiar_contrasena, idpasajero,
                                            intentos_fallidos,
                                            DATEDIFF(SECOND, GETDATE(), bloqueado_hasta) AS faltan
                                     FROM usuario WHERE usuario = @u AND esta_activo = 1",
                                    New SqlParameter("@u", nombre))

        If fila Is Nothing Then
            Registro.Advertencia($"Intento de inicio de sesión con usuario inexistente: {nombre}")
            BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario",
                                      "Usuario inexistente o inactivo", exito:=False, usuario:=nombre)
            Return New ResultadoAcceso With {.Mensaje = CREDENCIAL_INVALIDA}
        End If

        ' El bloqueo se comprueba ANTES que la contraseña: si se comprobara después,
        ' acertarla durante el castigo dejaría entrar igual y el bloqueo no serviría.
        Dim faltan = If(IsDBNull(fila("faltan")), 0, CInt(fila("faltan")))
        If faltan > 0 Then
            Registro.Advertencia($"Intento de acceso durante el bloqueo temporal: {nombre}")
            BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario",
                                      "Intento durante el bloqueo temporal", exito:=False, usuario:=nombre)
            Return New ResultadoAcceso With {
                .SegundosBloqueo = faltan,
                .Mensaje = $"Demasiados intentos fallidos. Espera {faltan} segundos."
            }
        End If

        If Not VerificarHash(clave, fila("contrasena_hash").ToString()) Then
            Return ContarFallo(nombre, CInt(fila("intentos_fallidos")))
        End If

        ' Entró: el contador vuelve a cero, si no el próximo error arrastraría los viejos
        Db.Ejecutar("UPDATE usuario SET ultimo_acceso = GETDATE(),
                                        intentos_fallidos = 0, bloqueado_hasta = NULL
                     WHERE usuario_id = @id",
                    New SqlParameter("@id", CInt(fila("usuario_id"))))

        Registro.Info($"Inicio de sesión exitoso: {nombre}")
        BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario", Nothing,
                                  exito:=True, usuario:=nombre)

        Return New ResultadoAcceso With {
            .Cuenta = New Usuario With {
                .UsuarioID = CInt(fila("usuario_id")),
                .NombreCompleto = fila("nombre_completo").ToString(),
                .Email = fila("email").ToString(),
                .NombreUsuario = fila("usuario").ToString(),
                .Rol = fila("rol").ToString(),
                .DebeCambiarContrasena = CBool(fila("debe_cambiar_contrasena")),
                .IdPasajero = If(IsDBNull(fila("idpasajero")), Nothing, fila("idpasajero").ToString())
            }
        }
    End Function

    ''' <summary>Suma un intento fallido y, si se llegó al límite, bloquea la cuenta.
    ''' Al bloquear se reinicia el contador: cuando pase el castigo la persona vuelve
    ''' a tener sus tres intentos, no queda bloqueada para siempre.</summary>
    Private Shared Function ContarFallo(nombre As String, fallosPrevios As Integer) As ResultadoAcceso
        Dim fallos = fallosPrevios + 1

        If fallos >= INTENTOS_PERMITIDOS Then
            Db.Ejecutar("UPDATE usuario SET intentos_fallidos = 0,
                                            bloqueado_hasta = DATEADD(SECOND, @s, GETDATE())
                         WHERE usuario = @u",
                        New SqlParameter("@s", SEGUNDOS_BLOQUEO),
                        New SqlParameter("@u", nombre))

            Registro.Advertencia($"Cuenta bloqueada {SEGUNDOS_BLOQUEO}s tras {INTENTOS_PERMITIDOS} intentos: {nombre}")
            BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario",
                                      $"Cuenta bloqueada {SEGUNDOS_BLOQUEO} segundos por intentos fallidos",
                                      exito:=False, usuario:=nombre)

            Return New ResultadoAcceso With {
                .SegundosBloqueo = SEGUNDOS_BLOQUEO,
                .Mensaje = $"Demasiados intentos fallidos. Espera {SEGUNDOS_BLOQUEO} segundos."
            }
        End If

        Db.Ejecutar("UPDATE usuario SET intentos_fallidos = @n WHERE usuario = @u",
                    New SqlParameter("@n", fallos),
                    New SqlParameter("@u", nombre))

        Registro.Advertencia($"Contraseña incorrecta para el usuario: {nombre}")
        BitacoraService.Registrar(BitacoraService.INICIO_SESION, "usuario",
                                  "Contraseña incorrecta", exito:=False, usuario:=nombre)
        Return New ResultadoAcceso With {.Mensaje = CREDENCIAL_INVALIDA}
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
        Dim pregunta = If(IsDBNull(fila("pregunta_seguridad")), "", fila("pregunta_seguridad").ToString())
        Dim respuesta = If(IsDBNull(fila("respuesta_seguridad")), "", fila("respuesta_seguridad").ToString())

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

    ''' <summary>Cambia la contraseña verificando primero la actual.
    '''
    ''' Comprueba el hash directamente en vez de pasar por Autenticar: a esta
    ''' pantalla solo se llega con la sesión ya abierta, así que equivocarse al
    ''' teclear no debe gastar intentos ni dejar a la persona bloqueada fuera de
    ''' un sistema en el que ya está dentro.</summary>
    Public Shared Function CambiarContrasenaConActual(usuario As String, claveActual As String,
                                                      claveNueva As String) As String
        If String.IsNullOrWhiteSpace(claveActual) Then Return "La contraseña actual no es correcta."

        Dim fila = Db.ConsultarFila("SELECT contrasena_hash FROM usuario
                                     WHERE usuario = @u AND esta_activo = 1",
                                    New SqlParameter("@u", If(usuario, "").Trim()))

        If fila Is Nothing OrElse Not VerificarHash(claveActual, fila("contrasena_hash").ToString()) Then
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
        ' Se valida contra la lista del servicio y no contra dos textos escritos aquí:
        ' así no puede pasar que la pantalla ofrezca un rol que el alta rechace
        If Not UsuarioService.RolesDelPersonal.Contains(rol) Then
            Return "Un Administrador solo puede crear cuentas de Administrador o de Agente."
        End If
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
