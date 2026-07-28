Imports System.Data
Imports Microsoft.Data.SqlClient

Public Class ClienteService

    Public Shared Function Listar(Optional filtro As String = "") As DataTable
        If String.IsNullOrWhiteSpace(filtro) Then
            Return Db.Consultar("SELECT codigo_cliente, nombre, cedula, celular, tipo_vehiculo, codigo_parqueadero
                                 FROM cliente ORDER BY nombre")
        End If
        Return Db.Consultar("SELECT codigo_cliente, nombre, cedula, celular, tipo_vehiculo, codigo_parqueadero
                             FROM cliente
                             WHERE nombre LIKE @f OR cedula LIKE @f OR codigo_cliente LIKE @f
                             ORDER BY nombre",
                            New SqlParameter("@f", "%" & filtro.Trim() & "%"))
    End Function

    Public Shared Function Existe(codigo As String) As Boolean
        Return CInt(Db.Escalar("SELECT COUNT(*) FROM cliente WHERE codigo_cliente = @c",
                               New SqlParameter("@c", codigo.Trim()))) > 0
    End Function

    ''' <summary>El sistema asigna el código automáticamente: el mayor existente + 1.</summary>
    Public Shared Function SiguienteCodigo() As String
        Dim maximo = Db.Escalar("SELECT ISNULL(MAX(TRY_CAST(codigo_cliente AS INT)), 0) FROM cliente")
        Return CStr(CInt(maximo) + 1)
    End Function

    Public Shared Sub Insertar(codigo As String, codParqueadero As String, nombre As String,
                               celular As String, cedula As String, tipoVehiculo As String)
        Db.Ejecutar("INSERT INTO cliente (codigo_cliente, codigo_parqueadero, nombre, celular, cedula, tipo_vehiculo)
                     VALUES (@c, @p, @n, @cel, @ced, @t)",
                    New SqlParameter("@c", codigo.Trim()),
                    New SqlParameter("@p", codParqueadero),
                    New SqlParameter("@n", nombre.Trim()),
                    New SqlParameter("@cel", celular.Trim()),
                    New SqlParameter("@ced", If(cedula, "").Trim()),
                    New SqlParameter("@t", tipoVehiculo))
    End Sub

    Public Shared Sub Actualizar(codigo As String, codParqueadero As String, nombre As String,
                                 celular As String, cedula As String, tipoVehiculo As String)
        Db.Ejecutar("UPDATE cliente SET codigo_parqueadero = @p, nombre = @n, celular = @cel,
                     cedula = @ced, tipo_vehiculo = @t WHERE codigo_cliente = @c",
                    New SqlParameter("@c", codigo.Trim()),
                    New SqlParameter("@p", codParqueadero),
                    New SqlParameter("@n", nombre.Trim()),
                    New SqlParameter("@cel", celular.Trim()),
                    New SqlParameter("@ced", If(cedula, "").Trim()),
                    New SqlParameter("@t", tipoVehiculo))
    End Sub

    Public Shared Sub Eliminar(codigo As String)
        Db.Ejecutar("DELETE FROM cliente WHERE codigo_cliente = @c",
                    New SqlParameter("@c", codigo.Trim()))
    End Sub

    ''' <summary>Lista para llenar combos: código + nombre.</summary>
    Public Shared Function ParaCombo() As DataTable
        Return Db.Consultar("SELECT codigo_cliente, codigo_cliente + ' - ' + nombre AS etiqueta
                             FROM cliente ORDER BY codigo_cliente")
    End Function
End Class
