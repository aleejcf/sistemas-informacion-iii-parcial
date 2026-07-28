Imports Xunit

''' <summary>Pruebas de las validaciones de registro de usuarios.</summary>
Public Class ValidadorTests

    ' ---------- Correo electrónico ----------

    <Theory>
    <InlineData("alejandro@gmail.com")>
    <InlineData("a.calderon@parko.hn")>
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
    <InlineData("Parko2026")>
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
End Class
