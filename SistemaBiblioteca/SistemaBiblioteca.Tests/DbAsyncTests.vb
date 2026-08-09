Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>Pruebas de las versiones asíncronas de Db, contra la base real. Si
''' SQL Server no está disponible se dan por buenas, igual que el resto de las
''' pruebas que dependen de la base de datos.</summary>
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

        Dim sincrona = Db.Consultar("SELECT TOP 5 idcategoria FROM categoria ORDER BY idcategoria")
        Dim asincrona = Await Db.ConsultarAsync("SELECT TOP 5 idcategoria FROM categoria ORDER BY idcategoria")

        Assert.Equal(sincrona.Rows.Count, asincrona.Rows.Count)
        For i = 0 To sincrona.Rows.Count - 1
            Assert.Equal(sincrona.Rows(i)(0), asincrona.Rows(i)(0))
        Next
    End Function

    <Fact>
    Public Async Function ConsultarFilaAsync_DevuelveNothingSiNoHayResultados() As Task
        If Not HayBaseDeDatos Then Return

        Dim fila = Await Db.ConsultarFilaAsync("SELECT * FROM categoria WHERE idcategoria = @c",
                                               New SqlParameter("@c", 999999))
        Assert.Null(fila)
    End Function

    ''' <summary>sp_estado_cuenta_socio devuelve varios resultados en una sola
    ''' ida (ficha, ejemplares afuera, multas). Es el único caso real de
    ''' ConsultarVariasAsync, que no se puede probar con una consulta suelta.</summary>
    <Fact>
    Public Async Function ConsultarVariasAsync_TraeLaMismaCantidadDeTablasQueConsultarVarias() As Task
        If Not HayBaseDeDatos Then Return

        Dim idSocio = CStr(Db.Escalar("SELECT TOP 1 idsocio FROM socio"))
        If idSocio Is Nothing Then Return   ' base de prueba sin socios todavía

        Dim sincrono = Db.ConsultarVarias("EXEC sp_estado_cuenta_socio @idsocio", New SqlParameter("@idsocio", idSocio))
        Dim asincrono = Await Db.ConsultarVariasAsync("EXEC sp_estado_cuenta_socio @idsocio", New SqlParameter("@idsocio", idSocio))

        Assert.Equal(sincrono.Tables.Count, asincrono.Tables.Count)
        For i = 0 To sincrono.Tables.Count - 1
            Assert.Equal(sincrono.Tables(i).Rows.Count, asincrono.Tables(i).Rows.Count)
        Next
    End Function

    <Fact>
    Public Async Function EjecutarEnAsync_YEnTransaccionAsync_HacenRollbackSiElTrabajoFalla() As Task
        If Not HayBaseDeDatos Then Return

        Await Assert.ThrowsAsync(Of InvalidOperationException)(
            Function() Db.EnTransaccionAsync(
                Async Function(cn, tx)
                    Await Db.EjecutarEnAsync(cn, tx,
                        "INSERT INTO categoria (idcategoria, nombre) VALUES (999999, 'Prueba async')")
                    Throw New InvalidOperationException("fuerza el rollback")
                End Function))

        ' Si el rollback no funcionó, esta fila quedaría en la base de datos.
        Dim quedó = Await Db.ContarAsync("SELECT COUNT(*) FROM categoria WHERE idcategoria = @c",
                                         New SqlParameter("@c", 999999))
        Assert.Equal(0, quedó)
    End Function
End Class
