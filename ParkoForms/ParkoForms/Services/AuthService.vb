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
                                     usuario As String, clave As String) As String
        If String.IsNullOrWhiteSpace(nombreCompleto) Then Return "Escribe tu nombre completo."
        If Not Validador.EsEmailValido(email) Then Return "El correo electrónico no es válido."
        If Not Validador.EsUsuarioValido(usuario) Then Return "El usuario debe tener de 4 a 30 caracteres (letras, números o _)."

        Dim errorClave = Validador.ValidarContrasena(clave)
        If errorClave IsNot Nothing Then Return errorClave

        If ExisteUsuario(usuario) Then Return "Ese nombre de usuario ya está registrado."
        If ExisteEmail(email) Then Return "Ese correo ya está registrado."

        Dim rol As String = If(HayUsuarios(), "Operador", "Administrador")
        Dim hash As String = BCrypt.Net.BCrypt.HashPassword(clave, workFactor:=11)

        Db.Ejecutar("INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol)
                     VALUES (@n, @e, @u, @h, @r)",
                    New SqlParameter("@n", nombreCompleto.Trim()),
                    New SqlParameter("@e", email.Trim().ToLower()),
                    New SqlParameter("@u", usuario.Trim()),
                    New SqlParameter("@h", hash),
                    New SqlParameter("@r", rol))
        Return Nothing
    End Function

    ''' <summary>Devuelve el usuario autenticado, o Nothing si las credenciales fallan.</summary>
    Public Shared Function Autenticar(usuario As String, clave As String) As Usuario
        If String.IsNullOrWhiteSpace(usuario) OrElse String.IsNullOrWhiteSpace(clave) Then
            Return Nothing
        End If

        Dim dt = Db.Consultar("SELECT usuario_id, nombre_completo, email, usuario, contrasena_hash, rol
                               FROM usuario WHERE usuario = @u AND esta_activo = 1",
                              New SqlParameter("@u", usuario.Trim()))
        If dt.Rows.Count = 0 Then Return Nothing

        Dim fila = dt.Rows(0)
        If Not BCrypt.Net.BCrypt.Verify(clave, fila("contrasena_hash").ToString()) Then
            Return Nothing
        End If

        Return New Usuario With {
            .UsuarioID = CInt(fila("usuario_id")),
            .NombreCompleto = fila("nombre_completo").ToString(),
            .Email = fila("email").ToString(),
            .NombreUsuario = fila("usuario").ToString(),
            .Rol = fila("rol").ToString()
        }
    End Function
End Class
