Imports System.Security.Cryptography
Imports Microsoft.Data.SqlClient

''' <summary>Códigos de respaldo: la red que sostiene una cuenta cuando su dueño
''' no puede entrar por el camino normal.
'''
''' Son diez códigos de un solo uso que se entregan al registrarse y se enseñan
''' UNA vez. Se guardan con hash BCrypt igual que las contraseñas, así que ni
''' leyendo la base se pueden recuperar, y cada uno sirve exactamente una vez.
''' Es el patrón que usan Google, GitHub y Auth0, y el que recomienda la guía de
''' OWASP para recuperar una cuenta sin depender de un correo.
'''
''' Por qué existen aquí: quien entra con Google no elige contraseña ni pregunta
''' de seguridad, así que sin esto una persona que perdiera el acceso a su cuenta
''' de Google perdería la del sistema para siempre.</summary>
Public Class RecuperacionService

    Public Const CODIGOS_POR_LOTE As Integer = 10

    ''' <summary>Doce caracteres en tres grupos de cuatro. OWASP pide ocho como
    ''' mínimo y recomienda doce; en grupos se leen y se dictan sin perderse.</summary>
    Private Const LARGO_GRUPO As Integer = 4
    Private Const GRUPOS As Integer = 3

    ''' <summary>Sin I, O, 0 ni 1: son los que se confunden al copiarlos a mano
    ''' desde un papel, que es justo donde va a acabar guardado un código de estos.</summary>
    Private Const ALFABETO As String = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"

    Private Const FACTOR_BCRYPT As Integer = 11

    ''' <summary>Un código de respaldo se puede intentar adivinar, así que la cuenta
    ''' lleva su propio freno. Va aparte del bloqueo de inicio de sesión: fallar
    ''' recuperando no debe dejar a nadie sin poder entrar con su contraseña.</summary>
    Public Const INTENTOS_PERMITIDOS As Integer = 5
    Public Const MINUTOS_BLOQUEO As Integer = 15

    ' ================================================================
    '  GENERACIÓN
    ' ================================================================

    ''' <summary>Genera un lote nuevo y devuelve los códigos EN CLARO. Es la única
    ''' vez que existen legibles: a partir de aquí solo queda su hash.
    '''
    ''' Generar un lote anula el anterior por completo. Es a propósito y es lo que
    ''' pide OWASP: si alguien sospecha que le vieron los códigos, pedir unos
    ''' nuevos tiene que dejar los viejos sin valor.</summary>
    Public Shared Function Generar(usuarioId As Integer) As String()
        Dim codigos(CODIGOS_POR_LOTE - 1) As String

        For i = 0 To CODIGOS_POR_LOTE - 1
            codigos(i) = GenerarUno()
        Next

        Db.EnTransaccion(
            Sub(cn, tx)
                Db.EjecutarEn(cn, tx,
                    "DELETE FROM codigo_respaldo WHERE usuario_id = @u",
                    New SqlParameter("@u", usuarioId))

                For Each codigo In codigos
                    ' Se guarda el hash de la forma NORMALIZADA —sin guiones y en
                    ' mayúsculas—: así al canjearlo basta normalizar lo que teclee
                    ' la persona y comparar, sin tener que reconstruir los grupos.
                    Db.EjecutarEn(cn, tx,
                        "INSERT INTO codigo_respaldo (usuario_id, codigo_hash) VALUES (@u, @h)",
                        New SqlParameter("@u", usuarioId),
                        New SqlParameter("@h", BCrypt.Net.BCrypt.HashPassword(
                            Normalizar(codigo), workFactor:=FACTOR_BCRYPT)))
                Next
            End Sub)

        Registro.Info($"Códigos de respaldo generados para la cuenta {usuarioId}")
        BitacoraService.Registrar(BitacoraService.EDITAR, "usuario",
                                  $"{CODIGOS_POR_LOTE} códigos de respaldo generados")
        Return codigos
    End Function

    ''' <summary>Lo mismo, buscando la cuenta por su nombre de usuario. Lo usa el
    ''' registro, que acaba de crear la cuenta y no conoce su identificador.</summary>
    Public Shared Function GenerarPara(nombreUsuario As String) As String()
        Dim id = IdDe(nombreUsuario)
        If id = 0 Then Return Array.Empty(Of String)()
        Return Generar(id)
    End Function

    ''' <summary>Un código: tres grupos de cuatro, con el azar del generador
    ''' criptográfico. Random no vale aquí — un código de recuperación que se pueda
    ''' predecir es un código que se puede adivinar.</summary>
    Private Shared Function GenerarUno() As String
        Dim partes(GRUPOS - 1) As String

        For g = 0 To GRUPOS - 1
            Dim grupo(LARGO_GRUPO - 1) As Char
            For c = 0 To LARGO_GRUPO - 1
                grupo(c) = ALFABETO(RandomNumberGenerator.GetInt32(ALFABETO.Length))
            Next
            partes(g) = New String(grupo)
        Next

        Return String.Join("-", partes)
    End Function

    ' ================================================================
    '  CONSULTA
    ' ================================================================

    Public Shared Function Disponibles(usuarioId As Integer) As Integer
        Return Db.Contar("SELECT COUNT(*) FROM codigo_respaldo WHERE usuario_id = @u AND usado = 0",
                         New SqlParameter("@u", usuarioId))
    End Function

    Public Shared Function DisponiblesPara(nombreUsuario As String) As Integer
        Dim id = IdDe(nombreUsuario)
        Return If(id = 0, 0, Disponibles(id))
    End Function

    Private Shared Function IdDe(nombreUsuario As String) As Integer
        Dim fila = Db.ConsultarFila("SELECT usuario_id FROM usuario WHERE usuario = @u",
                                    New SqlParameter("@u", If(nombreUsuario, "").Trim()))
        Return If(fila Is Nothing, 0, CInt(fila("usuario_id")))
    End Function

    ' ================================================================
    '  CANJE
    ' ================================================================

    Public Class Resultado
        Public Property Valido As Boolean
        ''' <summary>Segundos que faltan para poder volver a intentar; 0 si no hay freno.</summary>
        Public Property SegundosBloqueo As Integer
        Public Property Restantes As Integer
        Public Property Mensaje As String
    End Class

    ''' <summary>Gasta un código de respaldo. Si vale, queda marcado como usado y ya
    ''' no sirve nunca más.
    '''
    ''' Hay que recorrer los códigos comparando hash por hash: BCrypt mete una sal
    ''' distinta en cada uno, así que el mismo código produce hashes distintos y no
    ''' se puede buscar por igualdad. Son diez como mucho, y solo los sin usar.</summary>
    Public Shared Function Canjear(nombreUsuario As String, codigo As String) As Resultado
        If String.IsNullOrWhiteSpace(codigo) Then
            Return New Resultado With {.Mensaje = "Escribe uno de tus códigos de respaldo."}
        End If

        Dim id = IdDe(nombreUsuario)
        If id = 0 Then
            ' Mismo mensaje que un código incorrecto: decir "esa cuenta no existe"
            ' le confirmaría a un atacante qué usuarios están registrados
            Return New Resultado With {.Mensaje = "El código no es válido."}
        End If

        Dim faltan = SegundosDeBloqueo(id)
        If faltan > 0 Then
            Return New Resultado With {
                .SegundosBloqueo = faltan,
                .Mensaje = $"Demasiados intentos. Espera {Math.Ceiling(faltan / 60.0)} minuto(s) " &
                           "antes de volver a probar."
            }
        End If

        ' Se normaliza: la persona lo copia de un papel y los guiones o las
        ' minúsculas no deberían decidir si entra o no
        Dim limpio = Normalizar(codigo)

        Dim pendientes = Db.Consultar(
            "SELECT idcodigo, codigo_hash FROM codigo_respaldo
              WHERE usuario_id = @u AND usado = 0",
            New SqlParameter("@u", id))

        For Each fila As Data.DataRow In pendientes.Rows
            If Not Verificar(limpio, fila("codigo_hash").ToString()) Then Continue For

            Db.Ejecutar("UPDATE codigo_respaldo SET usado = 1, fecha_uso = GETDATE()
                         WHERE idcodigo = @c",
                        New SqlParameter("@c", CInt(fila("idcodigo"))))

            LimpiarIntentos(id)

            Dim restantes = Disponibles(id)
            Registro.Info($"Código de respaldo canjeado por {nombreUsuario}; quedan {restantes}")
            BitacoraService.Registrar(BitacoraService.RECUPERACION, "usuario",
                                      $"Código de respaldo canjeado · quedan {restantes}",
                                      usuario:=nombreUsuario)

            Return New Resultado With {.Valido = True, .Restantes = restantes}
        Next

        Return ContarFallo(id, nombreUsuario)
    End Function

    ''' <summary>Quita guiones, espacios y mayúsculas de más.</summary>
    Private Shared Function Normalizar(codigo As String) As String
        Return codigo.Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "")
    End Function

    Private Shared Function Verificar(limpio As String, hashGuardado As String) As Boolean
        Try
            Return BCrypt.Net.BCrypt.Verify(limpio, hashGuardado)

        Catch ex As Exception
            ' Un hash con formato inválido en la base se trata como código incorrecto
            Registro.Advertencia($"Código de respaldo con hash inválido: {ex.Message}")
            Return False
        End Try
    End Function

    ' ================================================================
    '  FRENO A LOS INTENTOS
    ' ================================================================

    Public Shared Function SegundosDeBloqueo(usuarioId As Integer) As Integer
        Dim fila = Db.ConsultarFila(
            "SELECT DATEDIFF(SECOND, GETDATE(), bloqueo_recuperacion_hasta) AS faltan
             FROM usuario WHERE usuario_id = @u",
            New SqlParameter("@u", usuarioId))

        If fila Is Nothing OrElse IsDBNull(fila("faltan")) Then Return 0
        Return Math.Max(0, CInt(fila("faltan")))
    End Function

    Private Shared Function ContarFallo(usuarioId As Integer, nombreUsuario As String) As Resultado
        Dim fila = Db.ConsultarFila("SELECT intentos_recuperacion FROM usuario WHERE usuario_id = @u",
                                    New SqlParameter("@u", usuarioId))
        Dim fallos = If(fila Is Nothing, 1, CInt(fila("intentos_recuperacion")) + 1)

        If fallos >= INTENTOS_PERMITIDOS Then
            Db.Ejecutar("UPDATE usuario SET intentos_recuperacion = 0,
                                            bloqueo_recuperacion_hasta = DATEADD(MINUTE, @m, GETDATE())
                         WHERE usuario_id = @u",
                        New SqlParameter("@m", MINUTOS_BLOQUEO),
                        New SqlParameter("@u", usuarioId))

            Registro.Advertencia($"Recuperación bloqueada {MINUTOS_BLOQUEO} min para {nombreUsuario}")
            BitacoraService.Registrar(BitacoraService.RECUPERACION, "usuario",
                                      $"Recuperación bloqueada {MINUTOS_BLOQUEO} minutos por intentos fallidos",
                                      exito:=False, usuario:=nombreUsuario)

            Return New Resultado With {
                .SegundosBloqueo = MINUTOS_BLOQUEO * 60,
                .Mensaje = $"Demasiados intentos. Espera {MINUTOS_BLOQUEO} minutos antes de volver a probar."
            }
        End If

        Db.Ejecutar("UPDATE usuario SET intentos_recuperacion = @n WHERE usuario_id = @u",
                    New SqlParameter("@n", fallos),
                    New SqlParameter("@u", usuarioId))

        BitacoraService.Registrar(BitacoraService.RECUPERACION, "usuario",
                                  "Código de respaldo incorrecto", exito:=False, usuario:=nombreUsuario)
        Return New Resultado With {.Mensaje = "El código no es válido."}
    End Function

    Private Shared Sub LimpiarIntentos(usuarioId As Integer)
        Db.Ejecutar("UPDATE usuario SET intentos_recuperacion = 0, bloqueo_recuperacion_hasta = NULL
                     WHERE usuario_id = @u",
                    New SqlParameter("@u", usuarioId))
    End Sub
End Class
