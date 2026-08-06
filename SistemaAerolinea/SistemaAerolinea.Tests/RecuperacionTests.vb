Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>Pruebas de los códigos de respaldo. Lo que se comprueba aquí no es que
''' la pantalla se vea bien, sino que un código sirva UNA vez, que no se pueda
''' adivinar a fuerza de intentos y que en la base no quede nada legible: son las
''' tres cosas en las que un error se convierte en un agujero y no en un botón que
''' no anda.</summary>
Public Class RecuperacionTests
    Implements IDisposable

    Private Shared ReadOnly HayBaseDeDatos As Boolean = Db.HayConexion()

    ''' <summary>Cuenta de usar y tirar, para no tocar ninguna real.</summary>
    Private cuentaDePrueba As Integer = 0
    Private nombreDePrueba As String = ""

    Public Sub Dispose() Implements IDisposable.Dispose
        If cuentaDePrueba <> 0 Then
            Try
                ' Los códigos caen con ella: la clave foránea va con ON DELETE CASCADE
                Db.Ejecutar("DELETE FROM usuario WHERE usuario_id = @u",
                            New SqlParameter("@u", cuentaDePrueba))
            Catch
            End Try
        End If
        Sesion.Cerrar()
    End Sub

    ''' <summary>Crea una cuenta suelta y devuelve su nombre de usuario.</summary>
    Private Function CrearCuenta() As String
        nombreDePrueba = "prueba_" & Guid.NewGuid().ToString("N").Substring(0, 12)

        Db.Ejecutar("INSERT INTO usuario (nombre_completo, email, usuario, contrasena_hash, rol)
                     VALUES (@n, @e, @u, @h, 'Agente')",
                    New SqlParameter("@n", "Cuenta de prueba"),
                    New SqlParameter("@e", nombreDePrueba & "@prueba.hn"),
                    New SqlParameter("@u", nombreDePrueba),
                    New SqlParameter("@h", BCrypt.Net.BCrypt.HashPassword("Prueba123", workFactor:=4)))

        cuentaDePrueba = CInt(Db.Escalar("SELECT usuario_id FROM usuario WHERE usuario = @u",
                                         New SqlParameter("@u", nombreDePrueba)))
        Return nombreDePrueba
    End Function

    ' ================================================================
    '  FORMA DE LOS CÓDIGOS  (sin base de datos)
    ' ================================================================

    <Fact>
    Public Sub SonDiezYNoSeRepiten()
        If Not HayBaseDeDatos Then Return

        Dim usuario = CrearCuenta()
        Dim codigos = RecuperacionService.Generar(cuentaDePrueba)

        Assert.Equal(RecuperacionService.CODIGOS_POR_LOTE, codigos.Length)
        Assert.Equal(codigos.Length, codigos.Distinct().Count())
    End Sub

    <Fact>
    Public Sub NoLlevanCaracteresQueSeConfunden()
        If Not HayBaseDeDatos Then Return

        CrearCuenta()

        ' Se dictan y se copian a mano desde un papel: I, O, 0 y 1 no pueden estar
        For Each codigo In RecuperacionService.Generar(cuentaDePrueba)
            Assert.Equal(14, codigo.Length)          ' 12 caracteres + 2 guiones
            Assert.Equal(3, codigo.Split("-"c).Length)

            For Each prohibido In "IO01"
                Assert.DoesNotContain(prohibido.ToString(), codigo.Replace("-", ""))
            Next
        Next
    End Sub

    ' ================================================================
    '  CANJE
    ' ================================================================

    <Fact>
    Public Sub UnCodigoSirveUnaSolaVez()
        If Not HayBaseDeDatos Then Return

        Dim usuario = CrearCuenta()
        Dim codigos = RecuperacionService.Generar(cuentaDePrueba)

        Assert.Equal(10, RecuperacionService.Disponibles(cuentaDePrueba))

        ' Primera vez: entra
        Dim primero = RecuperacionService.Canjear(usuario, codigos(0))
        Assert.True(primero.Valido)
        Assert.Equal(9, primero.Restantes)

        ' Segunda vez con el MISMO código: ya no vale
        Dim repetido = RecuperacionService.Canjear(usuario, codigos(0))
        Assert.False(repetido.Valido)
        Assert.Equal(9, RecuperacionService.Disponibles(cuentaDePrueba))
    End Sub

    <Fact>
    Public Sub DaIgualComoSeEscriba()
        If Not HayBaseDeDatos Then Return

        Dim usuario = CrearCuenta()
        Dim codigos = RecuperacionService.Generar(cuentaDePrueba)

        ' Se copia a mano desde un papel: ni los guiones ni las mayúsculas
        ' deberían decidir si alguien recupera su cuenta o no
        Dim suelto = codigos(0).Replace("-", "").ToLowerInvariant()
        Assert.True(RecuperacionService.Canjear(usuario, "  " & suelto & "  ").Valido)
    End Sub

    <Fact>
    Public Sub GenerarDeNuevoAnulaLosViejos()
        If Not HayBaseDeDatos Then Return

        Dim usuario = CrearCuenta()
        Dim viejos = RecuperacionService.Generar(cuentaDePrueba)
        RecuperacionService.Generar(cuentaDePrueba)

        ' Es lo que pide OWASP: si sospechas que te los vieron, pedir unos nuevos
        ' tiene que dejar los anteriores sin valor
        Assert.False(RecuperacionService.Canjear(usuario, viejos(0)).Valido)
        Assert.Equal(10, RecuperacionService.Disponibles(cuentaDePrueba))
    End Sub

    <Fact>
    Public Sub EnLaBaseNoQuedaNingunCodigoLegible()
        If Not HayBaseDeDatos Then Return

        CrearCuenta()
        Dim codigos = RecuperacionService.Generar(cuentaDePrueba)

        Dim guardados = Db.Consultar("SELECT codigo_hash FROM codigo_respaldo WHERE usuario_id = @u",
                                     New SqlParameter("@u", cuentaDePrueba))

        For Each fila As Data.DataRow In guardados.Rows
            Dim hash = fila("codigo_hash").ToString()

            Assert.StartsWith("$2", hash)                 ' es un hash BCrypt
            For Each codigo In codigos
                Assert.DoesNotContain(codigo.Replace("-", ""), hash)
            Next
        Next
    End Sub

    ' ================================================================
    '  FRENO A LOS INTENTOS
    ' ================================================================

    <Fact>
    Public Sub AdivinarACiegasTerminaBloqueado()
        If Not HayBaseDeDatos Then Return

        Dim usuario = CrearCuenta()
        RecuperacionService.Generar(cuentaDePrueba)

        For i = 1 To RecuperacionService.INTENTOS_PERMITIDOS
            RecuperacionService.Canjear(usuario, "ZZZZ-ZZZZ-ZZZZ")
        Next

        Dim tras = RecuperacionService.Canjear(usuario, "ZZZZ-ZZZZ-ZZZZ")
        Assert.False(tras.Valido)
        Assert.True(tras.SegundosBloqueo > 0, "Tras varios intentos hay que frenar.")
        Assert.True(RecuperacionService.SegundosDeBloqueo(cuentaDePrueba) > 0)
    End Sub

    <Fact>
    Public Sub UnaCuentaQueNoExisteNoSeDelata()
        If Not HayBaseDeDatos Then Return

        ' Responder "esa cuenta no existe" le confirmaría a un atacante qué
        ' nombres de usuario están registrados
        Dim resultado = RecuperacionService.Canjear("no_existe_" & Guid.NewGuid().ToString("N"),
                                                    "ABCD-EFGH-JKLM")
        Assert.False(resultado.Valido)
        Assert.Equal("El código no es válido.", resultado.Mensaje)
    End Sub

    ' ================================================================
    '  EL CORREO NUNCA ENSEÑA EL CÓDIGO
    ' ================================================================

    <Fact>
    Public Sub SinCorreoConfiguradoLaViaNoSeOfrece()
        ' La regla que sustituye al agujero: sin servidor, no hay vía. Nunca hay
        ' un "modo de respaldo" que enseñe el código en pantalla.
        Dim disponible = CorreoService.EstaDisponible()
        Dim config = CorreoService.Leer()

        Assert.Equal(config.EstaCompleta, disponible)
        If Not disponible Then
            Assert.NotNull(CorreoService.EnviarCodigo("quien@sea.hn", "Quien Sea", "123456"))
        End If
    End Sub

    <Fact>
    Public Sub ElCorreoSeEnsenaTapado()
        ' Hay que decir a dónde llega el código, pero no la dirección entera: si no,
        ' probando nombres de usuario se irían cosechando correos ajenos
        ' Queda la primera y la última letra: bastante para reconocer el buzón
        ' propio, insuficiente para deducir uno ajeno
        Assert.Equal("a••••••••n@gmail.com", Formato.CorreoOculto("alejandrocalderon@gmail.com"))
        Assert.Equal("j••e@alas.hn", Formato.CorreoOculto("jose@alas.hn"))
        Assert.Equal("•••@alas.hn", Formato.CorreoOculto("jo@alas.hn"))
        Assert.Equal("", Formato.CorreoOculto(""))
        Assert.Equal("•••", Formato.CorreoOculto("esto-no-es-un-correo"))
    End Sub
End Class
