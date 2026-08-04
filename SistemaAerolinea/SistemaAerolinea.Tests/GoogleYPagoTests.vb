Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports Xunit

''' <summary>Pruebas de las dos piezas nuevas: el cobro desde el portal y las
''' partes verificables del inicio de sesión con Google.
'''
''' El flujo completo de Google no se puede automatizar (abre el navegador y
''' depende de una cuenta real), pero sí sus piezas criptográficas, que son donde
''' un error se traduce en un agujero de seguridad y no en un botón que no anda.</summary>
Public Class GoogleYPagoTests
    Implements IDisposable

    Private Shared ReadOnly HayBaseDeDatos As Boolean = Db.HayConexion()

    Public Sub Dispose() Implements IDisposable.Dispose
        Sesion.Cerrar()
    End Sub

    ' ================================================================
    '  PKCE
    ' ================================================================

    <Fact>
    Public Sub ElVerificadorCumpleLaEspecificacion()
        ' RFC 7636: entre 43 y 128 caracteres, y solo los "unreserved" de una URL
        For i = 1 To 50
            Dim verificador = GoogleAuthService.GenerarVerificador()

            Assert.InRange(verificador.Length, 43, 128)
            Assert.All(verificador, Sub(c)
                                        Assert.True(Char.IsLetterOrDigit(c) OrElse c = "-"c OrElse c = "_"c,
                                                    $"Carácter no permitido en el verificador: {c}")
                                    End Sub)
        Next
    End Sub

    <Fact>
    Public Sub CadaVerificadorEsDistinto()
        ' Si se repitieran, PKCE dejaría de proteger nada
        Dim vistos As New HashSet(Of String)()
        For i = 1 To 200
            vistos.Add(GoogleAuthService.GenerarVerificador())
        Next
        Assert.Equal(200, vistos.Count)
    End Sub

    <Fact>
    Public Sub LaHuellaEsElSha256EnBase64Url()
        Dim verificador = GoogleAuthService.GenerarVerificador()
        Dim huella = GoogleAuthService.HuellaDe(verificador)

        Using sha = SHA256.Create()
            Dim esperado = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(verificador))).
                           TrimEnd("="c).Replace("+", "-").Replace("/", "_")
            Assert.Equal(esperado, huella)
        End Using

        ' Base64url: sin relleno y sin caracteres que rompan una URL
        Assert.DoesNotContain("=", huella)
        Assert.DoesNotContain("+", huella)
        Assert.DoesNotContain("/", huella)
    End Sub

    <Fact>
    Public Sub LaHuellaNoDejaAdivinarElVerificador()
        ' Es lo que impide que otro programa del equipo use un código robado
        Dim verificador = GoogleAuthService.GenerarVerificador()
        Dim huella = GoogleAuthService.HuellaDe(verificador)

        Assert.NotEqual(verificador, huella)
        Assert.NotEqual(huella, GoogleAuthService.HuellaDe(GoogleAuthService.GenerarVerificador()))
    End Sub

    <Fact>
    Public Sub LaHuellaEsEstable()
        Dim verificador = GoogleAuthService.GenerarVerificador()
        Assert.Equal(GoogleAuthService.HuellaDe(verificador), GoogleAuthService.HuellaDe(verificador))
    End Sub

    ' ================================================================
    '  LECTURA DEL id_token
    ' ================================================================

    Private Shared Function TokenDePrueba(carga As String) As String
        Dim base64 = Function(texto As String) GoogleAuthService.Base64Url(Encoding.UTF8.GetBytes(texto))
        Return $"{base64("{""alg"":""RS256""}")}.{base64(carga)}.firma-que-no-se-verifica"
    End Function

    <Fact>
    Public Sub SeLeenLosDatosDeLaCuentaDeGoogle()
        Dim token = TokenDePrueba(
            "{""sub"":""109876543210"",""email"":""ale@gmail.com""," &
            """name"":""Alejandro Calderón"",""email_verified"":true}")

        Dim identidad = GoogleAuthService.LeerIdentidad(token)

        Assert.NotNull(identidad)
        Assert.Equal("109876543210", identidad.GoogleId)
        Assert.Equal("ale@gmail.com", identidad.Email)
        Assert.Equal("Alejandro Calderón", identidad.Nombre)
        Assert.True(identidad.CorreoVerificado)
    End Sub

    <Fact>
    Public Sub UnCorreoSinVerificarSeDetecta()
        Dim token = TokenDePrueba("{""sub"":""1"",""email"":""x@y.com"",""email_verified"":false}")
        Assert.False(GoogleAuthService.LeerIdentidad(token).CorreoVerificado)
    End Sub

    <Fact>
    Public Sub UnTokenRotoNoRevienta()
        Assert.Null(GoogleAuthService.LeerIdentidad("esto-no-es-un-token"))
        Assert.Null(GoogleAuthService.LeerIdentidad(""))
    End Sub

    <Fact>
    Public Sub ElBase64UrlSeDeshaceBien()
        For largo = 1 To 40
            Dim bytes(largo - 1) As Byte
            RandomNumberGenerator.Fill(bytes)

            Dim ida = GoogleAuthService.Base64Url(bytes)
            Assert.Equal(bytes, GoogleAuthService.DesdeBase64Url(ida))
        Next
    End Sub

    ''' <summary>Crea una carpeta vacía lo bastante honda para que la búsqueda del
    ''' archivo, que sube unas cuantas carpetas, no pueda salirse de ella.
    '''
    ''' Hace falta porque el google-oauth.json real SÍ llega a la carpeta de salida
    ''' de las pruebas —el .vbproj lo copia y la referencia de proyecto lo arrastra—
    ''' en cuanto el equipo tiene Google configurado. Mirar ahí haría que la prueba
    ''' pasara solo en la integración continua, que es donde menos falta hace.</summary>
    Private Shared Function RaizTemporal() As String
        Return Path.Combine(Path.GetTempPath(), "alas-pruebas-" & Guid.NewGuid().ToString("N"))
    End Function

    ''' <summary>Crea, dentro de esa raíz recién hecha, una carpeta lo bastante honda
    ''' para que la búsqueda del archivo —que sube unas cuantas carpetas— no pueda
    ''' salirse de ella y encontrar algo que el equipo tenga por ahí.</summary>
    Private Shared Function CarpetaAislada(raiz As String) As String
        Dim honda = raiz
        For nivel = 1 To ConfiguracionGoogle.SALTOS_HACIA_ARRIBA
            honda = Path.Combine(honda, nivel.ToString())
        Next

        Directory.CreateDirectory(honda)
        Return honda
    End Function

    ''' <summary>Hace falta aislar la búsqueda porque el google-oauth.json real SÍ
    ''' llega a la carpeta de salida de las pruebas —el .vbproj lo copia y la
    ''' referencia de proyecto lo arrastra— en cuanto el equipo tiene Google
    ''' configurado. Mirar ahí haría que la prueba pasara solo en la integración
    ''' continua, que es donde menos falta hace.</summary>
    <Fact>
    Public Sub SinArchivoDeCredencialesElBotonNoSeOfrece()
        Dim raiz = RaizTemporal()
        Try
            ConfiguracionGoogle.BuscarEn(CarpetaAislada(raiz))

            ' Sin archivo, la función debe responder que no está disponible
            ' —y no lanzar una excepción, que dejaría rota la pantalla de acceso
            Assert.False(GoogleAuthService.EstaDisponible())
        Finally
            ConfiguracionGoogle.BuscarEn(Nothing)
            Directory.Delete(raiz, recursive:=True)
        End Try
    End Sub

    <Fact>
    Public Sub ConElArchivoDeCredencialesElBotonSeOfrece()
        ' La contraparte: si la prueba de arriba pasara por buscar mal en vez de
        ' por no haber archivo, esta lo delataría
        Dim raiz = RaizTemporal()
        Try
            Dim carpeta = CarpetaAislada(raiz)
            File.WriteAllText(Path.Combine(carpeta, ConfiguracionGoogle.NOMBRE_ARCHIVO),
                              "{""installed"":{""client_id"":""123.apps.googleusercontent.com""," &
                              """client_secret"":""secreto-de-prueba""}}")

            ConfiguracionGoogle.BuscarEn(carpeta)

            Assert.True(GoogleAuthService.EstaDisponible())
            Assert.Equal("123.apps.googleusercontent.com", ConfiguracionGoogle.Cargar().ClientId)
        Finally
            ConfiguracionGoogle.BuscarEn(Nothing)
            Directory.Delete(raiz, recursive:=True)
        End Try
    End Sub

    ' ================================================================
    '  REFERENCIA DEL COBRO
    ' ================================================================

    <Fact>
    Public Sub LaReferenciaTieneElFormatoDeUnaPasarela()
        For i = 1 To 50
            Dim referencia = ReferenciaPago.Generar()

            Assert.StartsWith("ALAS-", referencia)
            Assert.Equal(15, referencia.Length)
            Assert.True(ReferenciaPago.EsValida(referencia))
        Next
    End Sub

    <Fact>
    Public Sub LaReferenciaNoUsaCaracteresQueSeConfunden()
        ' Se dicta por teléfono al reclamar un cobro
        For i = 1 To 200
            Dim sufijo = ReferenciaPago.Generar().Substring(5)
            For Each prohibido In "IO01"
                Assert.DoesNotContain(prohibido.ToString(), sufijo)
            Next
        Next
    End Sub

    <Theory>
    <InlineData("ALAS-7K3M9QP2X4", True)>
    <InlineData("alas-7k3m9qp2x4", True)>
    <InlineData("ALAS-7K3M9QP2", False)>
    <InlineData("OTRO-7K3M9QP2X4", False)>
    <InlineData("ALAS7K3M9QP2X4", False)>
    <InlineData("", False)>
    Public Sub LaReferenciaSeValida(referencia As String, esperado As Boolean)
        Assert.Equal(esperado, ReferenciaPago.EsValida(referencia))
    End Sub

    ' ================================================================
    '  COBRO DESDE EL PORTAL
    ' ================================================================

    <Fact>
    Public Sub UnPasajeroNoPuedePagarLaReservaDeOtro()
        If Not HayBaseDeDatos Then Return

        Sesion.Cerrar()
        Dim reservas = ReservaService.Listar()
        If reservas.Rows.Count = 0 Then Return

        Dim ajena = reservas.Rows(0)
        Dim otro = If(ajena("idpasajero").ToString().Trim() = "P0000020", "P0000001", "P0000020")

        Sesion.UsuarioActual = New Usuario With {
            .UsuarioID = 998, .Rol = "Pasajero", .IdPasajero = otro,
            .NombreUsuario = "intruso", .NombreCompleto = "Intruso"
        }

        Dim referencia As String = ""
        Dim problema = PagoService.PagarDesdeElPortal(CInt(ajena("idreserva")), 2, referencia)

        Assert.NotNull(problema)
        Assert.Contains("no está a tu nombre", problema)
        Assert.Equal("", referencia)
    End Sub

    <Fact>
    Public Sub NoSePuedePagarDosVecesLaMismaReserva()
        If Not HayBaseDeDatos Then Return

        Sesion.Cerrar()
        Dim pagadas = ReservaService.Listar("", "Confirmada")
        If pagadas.Rows.Count = 0 Then Return

        Dim reserva = pagadas.Rows(0)
        If CDec(reserva("saldo")) > 0 Then Return   ' esta todavía debe

        Sesion.UsuarioActual = New Usuario With {
            .UsuarioID = 997, .Rol = "Pasajero",
            .IdPasajero = reserva("idpasajero").ToString().Trim(),
            .NombreUsuario = "duenio", .NombreCompleto = "Dueño"
        }

        Dim referencia As String = ""
        Dim problema = PagoService.PagarDesdeElPortal(CInt(reserva("idreserva")), 2, referencia)

        Assert.NotNull(problema)
        Assert.Contains("ya está pagada", problema)
    End Sub

    <Fact>
    Public Sub ElPortalNoOfreceEfectivo()
        If Not HayBaseDeDatos Then Return

        Dim metodos = PagoService.MetodosEnLinea()
        Assert.NotEmpty(metodos.Rows)

        For Each fila As Data.DataRow In metodos.Rows
            Assert.NotEqual("Efectivo", fila("etiqueta").ToString())
        Next
    End Sub
End Class
