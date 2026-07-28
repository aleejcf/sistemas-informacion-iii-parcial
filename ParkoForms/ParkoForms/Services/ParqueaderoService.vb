Imports System.Data
Imports Microsoft.Data.SqlClient

Public Class ParqueaderoService

    Public Shared Function Listar() As DataTable
        Return Db.Consultar("SELECT codigo_parqueadero, direccion, telefono, nit, administrador, operador, horario
                             FROM parqueadero ORDER BY codigo_parqueadero")
    End Function

    Public Shared Function Existe(codigo As String) As Boolean
        Return CInt(Db.Escalar("SELECT COUNT(*) FROM parqueadero WHERE codigo_parqueadero = @c",
                               New SqlParameter("@c", codigo.Trim()))) > 0
    End Function

    ''' <summary>El sistema asigna el código automáticamente: el mayor existente + 1.</summary>
    Public Shared Function SiguienteCodigo() As String
        Dim maximo = Db.Escalar("SELECT ISNULL(MAX(TRY_CAST(codigo_parqueadero AS INT)), 0) FROM parqueadero")
        Return CStr(CInt(maximo) + 1)
    End Function

    Public Shared Sub Insertar(codigo As String, direccion As String, telefono As String,
                               nit As String, administrador As String, operador As String, horario As String)
        Db.Ejecutar("INSERT INTO parqueadero (codigo_parqueadero, direccion, telefono, nit, administrador, operador, horario)
                     VALUES (@c, @d, @t, @n, @a, @o, @h)",
                    New SqlParameter("@c", codigo.Trim()),
                    New SqlParameter("@d", direccion.Trim()),
                    New SqlParameter("@t", telefono.Trim()),
                    New SqlParameter("@n", nit.Trim()),
                    New SqlParameter("@a", administrador.Trim()),
                    New SqlParameter("@o", operador.Trim()),
                    New SqlParameter("@h", If(horario, "").Trim()))
    End Sub

    Public Shared Sub Actualizar(codigo As String, direccion As String, telefono As String,
                                 nit As String, administrador As String, operador As String, horario As String)
        Db.Ejecutar("UPDATE parqueadero SET direccion = @d, telefono = @t, nit = @n,
                     administrador = @a, operador = @o, horario = @h
                     WHERE codigo_parqueadero = @c",
                    New SqlParameter("@c", codigo.Trim()),
                    New SqlParameter("@d", direccion.Trim()),
                    New SqlParameter("@t", telefono.Trim()),
                    New SqlParameter("@n", nit.Trim()),
                    New SqlParameter("@a", administrador.Trim()),
                    New SqlParameter("@o", operador.Trim()),
                    New SqlParameter("@h", If(horario, "").Trim()))
    End Sub

    Public Shared Sub Eliminar(codigo As String)
        Db.Ejecutar("DELETE FROM parqueadero WHERE codigo_parqueadero = @c",
                    New SqlParameter("@c", codigo.Trim()))
    End Sub

    ''' <summary>Lista para llenar combos: código + dirección.</summary>
    Public Shared Function ParaCombo() As DataTable
        Return Db.Consultar("SELECT codigo_parqueadero, codigo_parqueadero + ' - ' + direccion AS etiqueta
                             FROM parqueadero ORDER BY codigo_parqueadero")
    End Function
End Class
