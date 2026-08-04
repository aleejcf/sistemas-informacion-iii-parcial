Imports Xunit

''' <summary>Pruebas de las reglas de validación. Son funciones puras: no tocan
''' la base de datos ni la interfaz, por eso se pueden probar sin montar nada.</summary>
Public Class ValidadorTests

    ' ---------- Correo ----------

    <Theory>
    <InlineData("juan.lopez@gmail.com")>
    <InlineData("a@b.co")>
    <InlineData("maria.h+vuelos@empresa.hn")>
    Public Sub EmailValido_AceptaCorreosBienFormados(email As String)
        Assert.True(Validador.EsEmailValido(email))
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("   ")>
    <InlineData("sinarroba.com")>
    <InlineData("dos@@arrobas.com")>
    <InlineData("sin@dominio")>
    <InlineData("con espacio@correo.com")>
    Public Sub EmailValido_RechazaCorreosMalFormados(email As String)
        Assert.False(Validador.EsEmailValido(email))
    End Sub

    <Fact>
    Public Sub EmailValido_RechazaNothing()
        Assert.False(Validador.EsEmailValido(Nothing))
    End Sub

    ' ---------- Usuario ----------

    <Theory>
    <InlineData("alec")>
    <InlineData("agente_01")>
    <InlineData("ADMIN2026")>
    Public Sub UsuarioValido_AceptaLetrasNumerosYGuionBajo(usuario As String)
        Assert.True(Validador.EsUsuarioValido(usuario))
    End Sub

    <Theory>
    <InlineData("abc")>
    <InlineData("con espacio")>
    <InlineData("con-guion")>
    <InlineData("acento_ñ")>
    Public Sub UsuarioValido_RechazaLoQueNoCumple(usuario As String)
        Assert.False(Validador.EsUsuarioValido(usuario))
    End Sub

    <Fact>
    Public Sub UsuarioValido_RechazaMasDe30Caracteres()
        Assert.False(Validador.EsUsuarioValido(New String("a"c, 31)))
    End Sub

    ' ---------- Contraseña ----------

    <Fact>
    Public Sub Contrasena_AceptaLetrasYNumeros()
        Assert.Null(Validador.ValidarContrasena("alas2026"))
    End Sub

    <Fact>
    Public Sub Contrasena_RechazaCortas()
        Assert.NotNull(Validador.ValidarContrasena("ab12"))
    End Sub

    <Fact>
    Public Sub Contrasena_RechazaSoloLetras()
        Assert.NotNull(Validador.ValidarContrasena("solamenteletras"))
    End Sub

    <Fact>
    Public Sub Contrasena_RechazaSoloNumeros()
        Assert.NotNull(Validador.ValidarContrasena("12345678"))
    End Sub

    ' ---------- Códigos del negocio ----------

    <Theory>
    <InlineData("P0000001", True)>
    <InlineData("p0000020", True)>
    <InlineData("P000001", False)>
    <InlineData("X0000001", False)>
    <InlineData("P00000A1", False)>
    Public Sub CodigoPasajero_ValidaElFormato(codigo As String, esperado As Boolean)
        Assert.Equal(esperado, Validador.EsCodigoPasajeroValido(codigo))
    End Sub

    <Theory>
    <InlineData("TGU", True)>
    <InlineData("sap", True)>
    <InlineData("TG", False)>
    <InlineData("TGUA", False)>
    <InlineData("TG1", False)>
    Public Sub Iata_ValidaTresLetras(iata As String, esperado As Boolean)
        Assert.Equal(esperado, Validador.EsIataValido(iata))
    End Sub

    ' ---------- Fecha de nacimiento ----------

    <Fact>
    Public Sub FechaNacimiento_AceptaUnaFechaRazonable()
        Assert.Null(Validador.ValidarFechaNacimiento(Date.Today.AddYears(-30)))
    End Sub

    <Fact>
    Public Sub FechaNacimiento_RechazaElFuturo()
        Assert.NotNull(Validador.ValidarFechaNacimiento(Date.Today.AddDays(1)))
    End Sub

    <Fact>
    Public Sub FechaNacimiento_RechazaHoyMismo()
        Assert.NotNull(Validador.ValidarFechaNacimiento(Date.Today))
    End Sub

    <Fact>
    Public Sub FechaNacimiento_RechazaMasDe120Anios()
        Assert.NotNull(Validador.ValidarFechaNacimiento(Date.Today.AddYears(-121)))
    End Sub

    <Fact>
    Public Sub FechaNacimiento_RechazaQueNoSeIndique()
        Assert.NotNull(Validador.ValidarFechaNacimiento(Nothing))
    End Sub

    ' ---------- Horario de vuelo ----------

    <Fact>
    Public Sub HorarioVuelo_AceptaUnVueloNormal()
        Dim salida = New DateTime(2026, 8, 10, 6, 15, 0)
        Assert.Null(Validador.ValidarHorarioVuelo(salida, salida.AddMinutes(75)))
    End Sub

    <Fact>
    Public Sub HorarioVuelo_RechazaLlegadaAntesDeLaSalida()
        Dim salida = New DateTime(2026, 8, 10, 6, 15, 0)
        Assert.NotNull(Validador.ValidarHorarioVuelo(salida, salida.AddMinutes(-30)))
    End Sub

    <Fact>
    Public Sub HorarioVuelo_RechazaVuelosDemasiadoCortos()
        Dim salida = New DateTime(2026, 8, 10, 6, 15, 0)
        Assert.NotNull(Validador.ValidarHorarioVuelo(salida, salida.AddMinutes(10)))
    End Sub

    <Fact>
    Public Sub HorarioVuelo_RechazaVuelosDeMasDe20Horas()
        Dim salida = New DateTime(2026, 8, 10, 6, 15, 0)
        Assert.NotNull(Validador.ValidarHorarioVuelo(salida, salida.AddHours(21)))
    End Sub
End Class
