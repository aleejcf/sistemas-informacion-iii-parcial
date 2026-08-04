Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Los datos del panel de control. Cada consulta es un procedimiento
''' almacenado: la agregación se hace en el servidor, que es donde están los datos,
''' y la aplicación solo recibe el resultado ya calculado.</summary>
Public Class PanelService

    ''' <summary>Los diez indicadores del encabezado, en una sola fila.</summary>
    Public Shared Function Estadisticas() As DataRow
        Return Db.Consultar("EXEC dbo.sp_panel_estadisticas").Rows(0)
    End Function

    ''' <summary>Ingresos por día para el gráfico de barras. Incluye los días sin
    ''' ventas en cero para que la serie no tenga huecos.</summary>
    Public Shared Function Ingresos(Optional dias As Integer = 7) As DataTable
        Return Db.Consultar("EXEC dbo.sp_panel_ingresos @dias = @d",
                            New SqlParameter("@d", dias))
    End Function

    Public Shared Function RutasTop(Optional cuantas As Integer = 5) As DataTable
        Return Db.Consultar("EXEC dbo.sp_panel_rutas_top @top = @t",
                            New SqlParameter("@t", cuantas))
    End Function

    ''' <summary>Próximas salidas, para el bloque de operación del panel.</summary>
    Public Shared Function ProximasSalidas(Optional cuantas As Integer = 8) As DataTable
        Return Db.Consultar("SELECT TOP (@n) codigo_vuelo, ruta, ciudad_destino, fecha_salida,
                                    estado, puerta, asientos_vendidos, capacidad_pasajeros, ocupacion
                             FROM dbo.v_vuelo_detalle
                             WHERE fecha_salida >= GETDATE() AND estado <> 'Cancelado'
                             ORDER BY fecha_salida",
                            New SqlParameter("@n", cuantas))
    End Function

    ''' <summary>Últimas reservas emitidas.</summary>
    Public Shared Function UltimasReservas(Optional cuantas As Integer = 8) As DataTable
        Return Db.Consultar("SELECT TOP (@n) codigo_reserva, titular, fecha, boletos, costo, estado
                             FROM dbo.v_reserva_resumen
                             ORDER BY fecha DESC",
                            New SqlParameter("@n", cuantas))
    End Function
End Class
