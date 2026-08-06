Imports Xunit

''' <summary>Pruebas de las validaciones de cuentas y de los códigos del acervo.</summary>
Public Class ValidadorTests

    ' ---------- Correo electrónico ----------

    <Theory>
    <InlineData("alejandro@gmail.com")>
    <InlineData("a.calderon@alejandria.hn")>
    <InlineData("usuario123@dominio.com.hn")>
    Public Sub EsEmailValido_AceptaCorreosCorrectos(email As String)
        Assert.True(Validador.EsEmailValido(email))
    End Sub

    <Theory>
    <InlineData("sin-arroba.com")>
    <InlineData("sin@punto")>
    <InlineData("@sinusuario.com")>
    <InlineData("con espacio@correo.com")>
    <InlineData("")>
    <InlineData(Nothing)>
    Public Sub EsEmailValido_RechazaCorreosIncorrectos(email As String)
        Assert.False(Validador.EsEmailValido(email))
    End Sub

    ' ---------- Nombre de usuario ----------

    <Theory>
    <InlineData("alec")>
    <InlineData("alejandro_calderon")>
    <InlineData("user123")>
    Public Sub EsUsuarioValido_AceptaUsuariosCorrectos(usuario As String)
        Assert.True(Validador.EsUsuarioValido(usuario))
    End Sub

    <Theory>
    <InlineData("abc")>                      ' menos de 4 caracteres
    <InlineData("con espacio")>              ' espacios no permitidos
    <InlineData("con-guion")>                ' guion no permitido
    <InlineData("correo@dominio")>           ' símbolos no permitidos
    <InlineData("")>
    Public Sub EsUsuarioValido_RechazaUsuariosIncorrectos(usuario As String)
        Assert.False(Validador.EsUsuarioValido(usuario))
    End Sub

    ' ---------- Contraseña ----------

    <Theory>
    <InlineData("clave123")>
    <InlineData("Alejandria2026")>
    <InlineData("a1b2c3")>
    Public Sub ValidarContrasena_AceptaContrasenasSeguras(clave As String)
        Assert.Null(Validador.ValidarContrasena(clave))
    End Sub

    <Fact>
    Public Sub ValidarContrasena_RechazaContrasenaCorta()
        Assert.NotNull(Validador.ValidarContrasena("ab1"))
    End Sub

    <Fact>
    Public Sub ValidarContrasena_RechazaSoloLetras()
        Assert.NotNull(Validador.ValidarContrasena("solamenteletras"))
    End Sub

    <Fact>
    Public Sub ValidarContrasena_RechazaSoloNumeros()
        Assert.NotNull(Validador.ValidarContrasena("12345678"))
    End Sub

    <Fact>
    Public Sub ValidarContrasena_RechazaVacia()
        Assert.NotNull(Validador.ValidarContrasena(""))
    End Sub

    ' ---------- Códigos del acervo ----------

    <Theory>
    <InlineData("L00001")>
    <InlineData("L00020")>
    <InlineData("l99999")>                   ' se acepta en minúscula y se normaliza
    Public Sub EsIdLibroValido_AceptaCodigosDelCatalogo(codigo As String)
        Assert.True(Validador.EsIdLibroValido(codigo))
    End Sub

    <Theory>
    <InlineData("L0001")>                    ' faltan dígitos
    <InlineData("L000011")>                  ' sobran dígitos
    <InlineData("U00001")>                   ' es un socio, no un libro
    <InlineData("00001")>                    ' sin la letra
    <InlineData("")>
    Public Sub EsIdLibroValido_RechazaCodigosMalFormados(codigo As String)
        Assert.False(Validador.EsIdLibroValido(codigo))
    End Sub

    <Theory>
    <InlineData("U00001")>
    <InlineData("U00020")>
    Public Sub EsIdSocioValido_AceptaCodigosDeSocio(codigo As String)
        Assert.True(Validador.EsIdSocioValido(codigo))
    End Sub

    <Theory>
    <InlineData("L00001")>                   ' es un libro, no un socio
    <InlineData("U001")>
    <InlineData("")>
    Public Sub EsIdSocioValido_RechazaCodigosMalFormados(codigo As String)
        Assert.False(Validador.EsIdSocioValido(codigo))
    End Sub

    <Theory>
    <InlineData("L00001-01")>
    <InlineData("L00020-99")>
    Public Sub EsCodigoBarrasValido_AceptaCodigosDeEjemplar(codigo As String)
        Assert.True(Validador.EsCodigoBarrasValido(codigo))
    End Sub

    <Theory>
    <InlineData("L00001")>                   ' es el título, no la copia
    <InlineData("L00001-1")>                 ' el número de copia lleva dos dígitos
    <InlineData("L00001-001")>
    Public Sub EsCodigoBarrasValido_RechazaCodigosMalFormados(codigo As String)
        Assert.False(Validador.EsCodigoBarrasValido(codigo))
    End Sub

    ' ---------- Identidad y teléfono ----------

    <Theory>
    <InlineData("0501199000101")>
    <InlineData("0501-1990-00101")>          ' con guiones también vale
    Public Sub EsIdentidadValida_AceptaTreceDigitos(identidad As String)
        Assert.True(Validador.EsIdentidadValida(identidad))
    End Sub

    <Theory>
    <InlineData("050119900010")>             ' 12 dígitos
    <InlineData("05011990001011")>           ' 14 dígitos
    <InlineData("050119900010A")>            ' con letra
    <InlineData("")>
    Public Sub EsIdentidadValida_RechazaLoDemas(identidad As String)
        Assert.False(Validador.EsIdentidadValida(identidad))
    End Sub

    <Theory>
    <InlineData("98765432")>
    <InlineData("9876-5432")>
    <InlineData("")>                         ' el teléfono es opcional
    <InlineData(Nothing)>
    Public Sub EsTelefonoValido_AceptaOchoDigitosYVacio(telefono As String)
        Assert.True(Validador.EsTelefonoValido(telefono))
    End Sub

    <Theory>
    <InlineData("1234567")>                  ' 7 dígitos
    <InlineData("987654321")>                ' 9 dígitos
    <InlineData("9876543A")>
    Public Sub EsTelefonoValido_RechazaLongitudesIncorrectas(telefono As String)
        Assert.False(Validador.EsTelefonoValido(telefono))
    End Sub

    ' ---------- Año de publicación ----------

    <Fact>
    Public Sub ValidarAnioPublicacion_AceptaAnioRazonable()
        Assert.Null(Validador.ValidarAnioPublicacion(2018))
    End Sub

    <Fact>
    Public Sub ValidarAnioPublicacion_AceptaNothingPorqueEsOpcional()
        Assert.Null(Validador.ValidarAnioPublicacion(Nothing))
    End Sub

    <Fact>
    Public Sub ValidarAnioPublicacion_RechazaAnterioresALaImprenta()
        Assert.NotNull(Validador.ValidarAnioPublicacion(1200))
    End Sub

    <Fact>
    Public Sub ValidarAnioPublicacion_RechazaAnioFuturo()
        Assert.NotNull(Validador.ValidarAnioPublicacion(Date.Today.Year + 5))
    End Sub

    ' ---------- Plazo del préstamo ----------

    <Fact>
    Public Sub ValidarPlazo_AceptaUnaSemana()
        Assert.Null(Validador.ValidarPlazo(Date.Today, Date.Today.AddDays(7)))
    End Sub

    <Fact>
    Public Sub ValidarPlazo_RechazaVencimientoElMismoDia()
        Assert.NotNull(Validador.ValidarPlazo(Date.Today, Date.Today))
    End Sub

    <Fact>
    Public Sub ValidarPlazo_RechazaVencimientoAnterior()
        Assert.NotNull(Validador.ValidarPlazo(Date.Today, Date.Today.AddDays(-1)))
    End Sub

    <Fact>
    Public Sub ValidarPlazo_RechazaPlazoMayorAUnAnio()
        Assert.NotNull(Validador.ValidarPlazo(Date.Today, Date.Today.AddDays(400)))
    End Sub
End Class
