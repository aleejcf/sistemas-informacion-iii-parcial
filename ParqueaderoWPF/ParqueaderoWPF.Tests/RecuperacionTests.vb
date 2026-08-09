Imports Xunit

''' <summary>Pruebas de la recuperación de contraseña contra la base de datos.
''' Si SQL Server no está disponible se dan por buenas, igual que el resto de
''' las pruebas que dependen de la base de datos.
''' Los usuarios de prueba se crean y se borran dentro de las mismas pruebas.</summary>
Public Class RecuperacionTests
    Implements IDisposable

    Private Shared ReadOnly HayBaseDeDatos As Boolean = Db.HayConexion()

    Private Const UsuarioCorrupto As String = "test_corrupto"
    Private Const UsuarioSinPregunta As String = "test_sinpregunta"
    Private Const HashFalso As String = "$2a$11$abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQR"

    Public Sub New()
        If Not HayBaseDeDatos Then Return

        Limpiar()

        ' Usuario con la respuesta guardada en texto plano (dato corrupto)
        Db.Ejecutar($"INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol,
                                           pregunta_seguridad, respuesta_seguridad)
                      VALUES ('Prueba Corrupta', 'corrupto@test.hn', '{UsuarioCorrupto}',
                              '{HashFalso}', 'Operador', '¿Cuál es tu comida favorita?',
                              'baleadas en texto plano')")

        ' Usuario sin pregunta de seguridad (como las cuentas creadas antes de la función)
        Db.Ejecutar($"INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol)
                      VALUES ('Prueba Sin Pregunta', 'sinpregunta@test.hn', '{UsuarioSinPregunta}',
                              '{HashFalso}', 'Operador')")
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If HayBaseDeDatos Then Limpiar()
    End Sub

    Private Shared Sub Limpiar()
        Db.Ejecutar($"DELETE FROM usuario WHERE usuario IN ('{UsuarioCorrupto}', '{UsuarioSinPregunta}')")
    End Sub

    ' ---------- Estado de la pregunta ----------

    <Fact>
    Public Sub ObtenerPregunta_UsuarioInexistente_InformaQueNoExiste()
        If Not HayBaseDeDatos Then Return

        Dim consulta = AuthService.ObtenerPregunta("no_existe_este_usuario_xyz")
        Assert.Equal(AuthService.EstadoPregunta.UsuarioNoExiste, consulta.Estado)
    End Sub

    <Fact>
    Public Sub ObtenerPregunta_SinConfigurar_LoDistingueDeUsuarioInexistente()
        If Not HayBaseDeDatos Then Return

        ' Este era el caso que dejaba al usuario sin saber qué hacer
        Dim consulta = AuthService.ObtenerPregunta(UsuarioSinPregunta)
        Assert.Equal(AuthService.EstadoPregunta.SinConfigurar, consulta.Estado)
    End Sub

    ' ---------- Verificación de la respuesta ----------

    <Fact>
    Public Sub VerificarRespuesta_RespuestaGuardadaCorrupta_NoTumbaLaAplicacion()
        If Not HayBaseDeDatos Then Return

        ' Antes lanzaba SaltParseException y cerraba el programa
        Dim resultado = AuthService.VerificarRespuesta(UsuarioCorrupto, "baleadas en texto plano")
        Assert.False(resultado)
    End Sub

    <Fact>
    Public Sub VerificarRespuesta_UsuarioSinRespuesta_DevuelveFalso()
        If Not HayBaseDeDatos Then Return

        Assert.False(AuthService.VerificarRespuesta(UsuarioSinPregunta, "lo que sea"))
    End Sub

    <Fact>
    Public Sub VerificarRespuesta_RespuestaVacia_DevuelveFalso()
        If Not HayBaseDeDatos Then Return

        Assert.False(AuthService.VerificarRespuesta(UsuarioCorrupto, ""))
    End Sub

    ' ---------- Configurar la pregunta desde la cuenta ----------

    <Fact>
    Public Sub ConfigurarPregunta_ArreglaUnaCuentaSinPregunta()
        If Not HayBaseDeDatos Then Return

        ' Antes: la cuenta no podía recuperar su contraseña
        Assert.Equal(AuthService.EstadoPregunta.SinConfigurar,
                     AuthService.ObtenerPregunta(UsuarioSinPregunta).Estado)

        Dim errorMsg = AuthService.ConfigurarPregunta(UsuarioSinPregunta,
                                                      "¿En qué ciudad naciste?", "Tegucigalpa")
        Assert.Null(errorMsg)

        ' Después: ya tiene pregunta y la respuesta se verifica correctamente
        Dim consulta = AuthService.ObtenerPregunta(UsuarioSinPregunta)
        Assert.Equal(AuthService.EstadoPregunta.Encontrada, consulta.Estado)
        Assert.Equal("¿En qué ciudad naciste?", consulta.Pregunta)
        Assert.True(AuthService.VerificarRespuesta(UsuarioSinPregunta, "Tegucigalpa"))
    End Sub

    <Fact>
    Public Sub ConfigurarPregunta_LaRespuestaNoDistingueMayusculasNiEspacios()
        If Not HayBaseDeDatos Then Return

        AuthService.ConfigurarPregunta(UsuarioSinPregunta, "¿En qué ciudad naciste?", "Tegucigalpa")

        Assert.True(AuthService.VerificarRespuesta(UsuarioSinPregunta, "  TEGUCIGALPA  "))
        Assert.True(AuthService.VerificarRespuesta(UsuarioSinPregunta, "tegucigalpa"))
        Assert.False(AuthService.VerificarRespuesta(UsuarioSinPregunta, "San Pedro Sula"))
    End Sub

    <Fact>
    Public Sub ConfigurarPregunta_NoGuardaLaRespuestaEnTextoPlano()
        If Not HayBaseDeDatos Then Return

        AuthService.ConfigurarPregunta(UsuarioSinPregunta, "¿Cuál es tu equipo favorito?", "Olimpia")

        Dim guardada = Db.Escalar($"SELECT respuesta_seguridad FROM usuario
                                    WHERE usuario = '{UsuarioSinPregunta}'").ToString()
        Assert.DoesNotContain("Olimpia", guardada, StringComparison.OrdinalIgnoreCase)
        Assert.StartsWith("$2", guardada)
    End Sub

    <Fact>
    Public Sub ConfigurarPregunta_RechazaCamposVacios()
        If Not HayBaseDeDatos Then Return

        Assert.NotNull(AuthService.ConfigurarPregunta(UsuarioSinPregunta, "", "respuesta"))
        Assert.NotNull(AuthService.ConfigurarPregunta(UsuarioSinPregunta, "¿Pregunta?", ""))
    End Sub
End Class
