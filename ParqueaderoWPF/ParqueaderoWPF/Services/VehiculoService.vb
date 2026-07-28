Imports System.Data
Imports Microsoft.Data.SqlClient

Public Class VehiculoService

    Public Shared Function Listar(Optional filtroPlaca As String = "") As DataTable
        If String.IsNullOrWhiteSpace(filtroPlaca) Then
            Return Db.Consultar("SELECT v.codigo_vehiculo, v.placa, v.marca, v.modelo, v.estado,
                                        v.fecha_ingreso, v.codigo_cliente, v.codigo_parqueadero, c.nombre AS cliente
                                 FROM vehiculo v LEFT JOIN cliente c ON c.codigo_cliente = v.codigo_cliente
                                 ORDER BY v.codigo_vehiculo")
        End If
        Return Db.Consultar("SELECT v.codigo_vehiculo, v.placa, v.marca, v.modelo, v.estado,
                                    v.fecha_ingreso, v.codigo_cliente, v.codigo_parqueadero, c.nombre AS cliente
                             FROM vehiculo v LEFT JOIN cliente c ON c.codigo_cliente = v.codigo_cliente
                             WHERE v.placa LIKE @f OR v.marca LIKE @f OR v.codigo_vehiculo LIKE @f
                             ORDER BY v.codigo_vehiculo",
                            New SqlParameter("@f", "%" & filtroPlaca.Trim() & "%"))
    End Function

    Public Shared Function Existe(codigo As String) As Boolean
        Return CInt(Db.Escalar("SELECT COUNT(*) FROM vehiculo WHERE codigo_vehiculo = @c",
                               New SqlParameter("@c", codigo.Trim()))) > 0
    End Function

    Public Shared Sub Insertar(codigo As String, codParqueadero As String, codCliente As String,
                               fechaIngreso As Date?, marca As String, modelo As String,
                               estado As String, placa As String)
        Db.Ejecutar("INSERT INTO vehiculo (codigo_vehiculo, codigo_parqueadero, codigo_cliente,
                                           fecha_ingreso, marca, modelo, estado, placa)
                     VALUES (@c, @p, @cl, @f, @ma, @mo, @e, @pl)",
                    New SqlParameter("@c", codigo.Trim()),
                    New SqlParameter("@p", codParqueadero),
                    New SqlParameter("@cl", codCliente),
                    New SqlParameter("@f", If(fechaIngreso.HasValue, CObj(fechaIngreso.Value), DBNull.Value)),
                    New SqlParameter("@ma", If(marca, "").Trim()),
                    New SqlParameter("@mo", modelo.Trim()),
                    New SqlParameter("@e", estado.Trim()),
                    New SqlParameter("@pl", placa.Trim().ToUpper()))
    End Sub

    Public Shared Sub Actualizar(codigo As String, codParqueadero As String, codCliente As String,
                                 fechaIngreso As Date?, marca As String, modelo As String,
                                 estado As String, placa As String)
        Db.Ejecutar("UPDATE vehiculo SET codigo_parqueadero = @p, codigo_cliente = @cl, fecha_ingreso = @f,
                     marca = @ma, modelo = @mo, estado = @e, placa = @pl
                     WHERE codigo_vehiculo = @c",
                    New SqlParameter("@c", codigo.Trim()),
                    New SqlParameter("@p", codParqueadero),
                    New SqlParameter("@cl", codCliente),
                    New SqlParameter("@f", If(fechaIngreso.HasValue, CObj(fechaIngreso.Value), DBNull.Value)),
                    New SqlParameter("@ma", If(marca, "").Trim()),
                    New SqlParameter("@mo", modelo.Trim()),
                    New SqlParameter("@e", estado.Trim()),
                    New SqlParameter("@pl", placa.Trim().ToUpper()))
    End Sub

    Public Shared Sub Eliminar(codigo As String)
        Db.Ejecutar("DELETE FROM vehiculo WHERE codigo_vehiculo = @c",
                    New SqlParameter("@c", codigo.Trim()))
    End Sub
End Class
