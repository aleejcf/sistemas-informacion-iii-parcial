Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>Pruebas de las versiones asíncronas de Db, contra la base real. Si
''' SQL Server no está disponible se dan por buenas, igual que el resto de las
''' pruebas que dependen de la base de datos (ver ConsultasTests).</summary>
Public Class DbAsyncTests

    Private Shared ReadOnly HayBaseDeDatos As Boolean = Db.HayConexion()

    <Fact>
    Public Async Function HayConexionAsync_CoincideConLaVersionSincrona() As Task
        If Not HayBaseDeDatos Then Return

        Assert.True(Await Db.HayConexionAsync())
    End Function

    <Fact>
    Public Async Function EscalarAsync_DevuelveElMismoValorQueEscalar() As Task
        If Not HayBaseDeDatos Then Return

        Assert.Equal(Db.Escalar("SELECT 1"), Await Db.EscalarAsync("SELECT 1"))
    End Function

    <Fact>
    Public Async Function ConsultarAsync_DevuelveLasMismasFilasQueConsultar() As Task
        If Not HayBaseDeDatos Then Return

        Dim sincrona = Db.Consultar("SELECT TOP 5 iata FROM aeropuerto ORDER BY iata")
        Dim asincrona = Await Db.ConsultarAsync("SELECT TOP 5 iata FROM aeropuerto ORDER BY iata")

        Assert.Equal(sincrona.Rows.Count, asincrona.Rows.Count)
        For i = 0 To sincrona.Rows.Count - 1
            Assert.Equal(sincrona.Rows(i)(0), asincrona.Rows(i)(0))
        Next
    End Function

    <Fact>
    Public Async Function ConsultarFilaAsync_DevuelveNothingSiNoHayResultados() As Task
        If Not HayBaseDeDatos Then Return

        Dim fila = Await Db.ConsultarFilaAsync("SELECT * FROM aeropuerto WHERE iata = @c",
                                               New SqlParameter("@c", "ZZZ"))
        Assert.Null(fila)
    End Function

    <Fact>
    Public Async Function EjecutarEnAsync_YEnTransaccionAsync_HacenRollbackSiElTrabajoFalla() As Task
        If Not HayBaseDeDatos Then Return

        Dim idPaisExistente = CStr(Db.Escalar("SELECT TOP 1 idpais FROM pais"))

        Await Assert.ThrowsAsync(Of InvalidOperationException)(
            Function() Db.EnTransaccionAsync(
                Async Function(cn, tx)
                    Await Db.EjecutarEnAsync(cn, tx,
                        "INSERT INTO aeropuerto (idaeropuerto, nombre, ciudad, iata, idpais) " &
                        "VALUES ('A9999', 'Prueba async', 'Prueba', 'ZZZ', @idpais)",
                        New SqlParameter("@idpais", idPaisExistente))
                    Throw New InvalidOperationException("fuerza el rollback")
                End Function))

        ' Si el rollback no funcionó, esta fila quedaría en la base de datos.
        Dim quedó = Await Db.ContarAsync("SELECT COUNT(*) FROM aeropuerto WHERE iata = @c",
                                         New SqlParameter("@c", "ZZZ"))
        Assert.Equal(0, quedó)
    End Function
End Class
