Imports Xunit

''' <summary>Pruebas de la regla de solvencia del socio: por qué se le niega un
''' préstamo y con qué palabras se le explica. La consulta la calcula la vista
''' `v_socio_detalle`; lo que se prueba aquí es cómo el sistema la traduce.</summary>
Public Class SolvenciaTests

    Private Shared Function SocioAlDia() As SocioResumen
        Return New SocioResumen With {
            .IdSocio = "U00001",
            .NombreCompleto = "Juan López",
            .TipoSocio = "Estudiante",
            .MaxPrestamos = 3,
            .DiasPrestamo = 7,
            .MultaDiaria = 5D,
            .EjemplaresAfuera = 1,
            .CupoDisponible = 2,
            .PrestamosVencidos = 0,
            .MontoAdeudado = 0D,
            .PuedePrestar = True,
            .EstaActivo = True
        }
    End Function

    <Fact>
    Public Sub MotivoBloqueo_EsNothingCuandoElSocioPuedePrestar()
        Assert.Null(SocioAlDia().MotivoBloqueo)
    End Sub

    <Fact>
    Public Sub MotivoBloqueo_ExplicaQueLaCuentaEstaInactiva()
        Dim socio = SocioAlDia()
        socio.PuedePrestar = False
        socio.EstaActivo = False

        Assert.Contains("inactivo", socio.MotivoBloqueo)
    End Sub

    <Fact>
    Public Sub MotivoBloqueo_ExplicaLaMoraEnSingular()
        Dim socio = SocioAlDia()
        socio.PuedePrestar = False
        socio.PrestamosVencidos = 1

        Assert.Contains("un préstamo vencido", socio.MotivoBloqueo)
    End Sub

    <Fact>
    Public Sub MotivoBloqueo_ExplicaLaMoraEnPlural()
        Dim socio = SocioAlDia()
        socio.PuedePrestar = False
        socio.PrestamosVencidos = 3

        Assert.Contains("3 préstamos vencidos", socio.MotivoBloqueo)
    End Sub

    ''' <summary>La deuda se dice con el monto: "tiene multas pendientes" no le
    ''' sirve al bibliotecario, "tiene multas pendientes por L 45.00" sí.</summary>
    <Fact>
    Public Sub MotivoBloqueo_DiceCuantoDebe()
        Dim socio = SocioAlDia()
        socio.PuedePrestar = False
        socio.MontoAdeudado = 45D

        Assert.Contains("L 45.00", socio.MotivoBloqueo)
    End Sub

    <Fact>
    Public Sub MotivoBloqueo_ExplicaElLimiteDeEjemplares()
        Dim socio = SocioAlDia()
        socio.PuedePrestar = False
        socio.EjemplaresAfuera = 3
        socio.CupoDisponible = 0

        Dim motivo = socio.MotivoBloqueo
        Assert.Contains("3", motivo)
        Assert.Contains("Estudiante", motivo)
    End Sub

    ''' <summary>La mora se informa antes que la deuda: es lo que el socio tiene
    ''' que resolver primero, porque devolver el libro también detiene el cobro.</summary>
    <Fact>
    Public Sub MotivoBloqueo_PrefiereLaMoraSobreLaDeudaCuandoHayAmbas()
        Dim socio = SocioAlDia()
        socio.PuedePrestar = False
        socio.PrestamosVencidos = 2
        socio.MontoAdeudado = 100D

        Assert.Contains("vencidos", socio.MotivoBloqueo)
    End Sub
End Class
