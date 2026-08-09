Imports Microsoft.Data.SqlClient
Imports Xunit

''' <summary>Pruebas de las versiones asíncronas de Db, contra la base real. Si
''' SQL Server no está disponible se dan por buenas, igual que el resto de las
''' pruebas que dependen de la base de datos.
'''
''' PARKO no tiene EnTransaccion ni HayConexion -su Db.vb solo tiene Consultar,
''' Ejecutar y Escalar-, así que EjecutarAsync se prueba con una limpieza manual
''' en Finally en vez del patrón de rollback que usan ALAS y Alejandría.</summary>
Public Class DbAsyncTests

    Private Shared ReadOnly HayBaseDeDatos As Boolean = Comprobar()

    Private Shared Function Comprobar() As Boolean
        Try
            Db.Escalar("SELECT 1")
            Return True
        Catch
            Return False
        End Try
    End Function

    <Fact>
    Public Async Function EscalarAsync_DevuelveElMismoValorQueEscalar() As Task
        If Not HayBaseDeDatos Then Return

        Assert.Equal(Db.Escalar("SELECT 1"), Await Db.EscalarAsync("SELECT 1"))
    End Function

    <Fact>
    Public Async Function ConsultarAsync_DevuelveLasMismasFilasQueConsultar() As Task
        If Not HayBaseDeDatos Then Return

        Dim sincrona = Db.Consultar("SELECT tipo_vehiculo FROM tarifa ORDER BY tipo_vehiculo")
        Dim asincrona = Await Db.ConsultarAsync("SELECT tipo_vehiculo FROM tarifa ORDER BY tipo_vehiculo")

        Assert.Equal(sincrona.Rows.Count, asincrona.Rows.Count)
        For i = 0 To sincrona.Rows.Count - 1
            Assert.Equal(sincrona.Rows(i)(0), asincrona.Rows(i)(0))
        Next
    End Function

    <Fact>
    Public Async Function EjecutarAsync_InsertaYSeLeeDeVuelta() As Task
        If Not HayBaseDeDatos Then Return

        Const TIPO As String = "prueba_async"
        Try
            Dim filas = Await Db.EjecutarAsync(
                "INSERT INTO tarifa (tipo_vehiculo, valor_hora) VALUES (@t, 1)",
                New SqlParameter("@t", TIPO))
            Assert.Equal(1, filas)

            Dim valor = Await Db.EscalarAsync(
                "SELECT valor_hora FROM tarifa WHERE tipo_vehiculo = @t", New SqlParameter("@t", TIPO))
            Assert.Equal(1D, CDec(valor))
        Finally
            Db.Ejecutar("DELETE FROM tarifa WHERE tipo_vehiculo = @t", New SqlParameter("@t", TIPO))
        End Try
    End Function
End Class
