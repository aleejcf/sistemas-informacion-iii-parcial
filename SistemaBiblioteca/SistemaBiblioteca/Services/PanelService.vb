Imports System.Data

''' <summary>Los indicadores del panel de control. Todos salen de un solo
''' procedimiento almacenado para no hacer doce viajes a la base de datos cada
''' vez que alguien abre la pantalla de inicio.</summary>
Public Class PanelService

    Public Shared Function Indicadores() As DataRow
        Return Db.ConsultarFila("EXEC dbo.sp_panel")
    End Function

    Public Shared Function MasPrestados(Optional top As Integer = 6) As DataTable
        Return LibroService.MasPrestados(top)
    End Function

    Public Shared Function Vencidos() As DataTable
        Return PrestamoService.Vencidos()
    End Function

    Public Shared Function MovimientoDiario(Optional dias As Integer = 14) As DataTable
        Return PrestamoService.MovimientoDiario(dias)
    End Function

    ''' <summary>Los préstamos que vencen en los próximos días: es la lista a la
    ''' que hay que llamar por teléfono antes de que se conviertan en mora.</summary>
    Public Shared Function PorVencer(Optional dias As Integer = 3) As DataTable
        Return Db.Consultar(
            "SELECT TOP 12 codigo, socio, telefono, titulos, fecha_vencimiento,
                    dias_restantes, total_ejemplares
             FROM dbo.v_prestamo_detalle
             WHERE estado = 'Activo' AND dias_restantes BETWEEN 0 AND @d
             ORDER BY dias_restantes, socio",
            New Microsoft.Data.SqlClient.SqlParameter("@d", dias))
    End Function

    ''' <summary>Reparto del acervo por categoría, para la gráfica del panel.</summary>
    Public Shared Function AcervoPorCategoria() As DataTable
        Return Db.Consultar(
            "SELECT TOP 8 c.nombre AS categoria,
                    COUNT(e.idejemplar) AS ejemplares,
                    SUM(CASE WHEN e.estado = 'Prestado' THEN 1 ELSE 0 END) AS prestados
             FROM categoria c
             JOIN libro    l ON l.idcategoria = c.idcategoria
             JOIN ejemplar e ON e.idlibro     = l.idlibro
             GROUP BY c.nombre
             ORDER BY COUNT(e.idejemplar) DESC")
    End Function
End Class
