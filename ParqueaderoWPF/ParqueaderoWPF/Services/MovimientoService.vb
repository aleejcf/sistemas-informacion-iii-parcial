Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Resultado de una salida: lo que se le cobra al cliente.</summary>
Public Class ResultadoSalida
    Public Property Placa As String
    Public Property Entrada As DateTime
    Public Property Salida As DateTime
    Public Property Minutos As Integer
    Public Property HorasCobradas As Integer
    Public Property ValorHora As Decimal
    Public Property Total As Decimal
End Class

''' <summary>Entradas y salidas del parqueadero con cálculo de tarifa.</summary>
Public Class MovimientoService

    Public Shared Function Tarifas() As DataTable
        Return Db.Consultar("SELECT tipo_vehiculo, valor_hora,
                                    tipo_vehiculo + ' — L ' + FORMAT(valor_hora, 'N2') + '/hora' AS etiqueta
                             FROM tarifa ORDER BY tipo_vehiculo")
    End Function

    Public Shared Function TieneEntradaAbierta(placa As String) As Boolean
        Return CInt(Db.Escalar("SELECT COUNT(*) FROM movimiento WHERE placa = @p AND fecha_salida IS NULL",
                               New SqlParameter("@p", placa.Trim().ToUpper()))) > 0
    End Function

    Public Shared Sub RegistrarEntrada(placa As String, tipoVehiculo As String,
                                       codParqueadero As String, usuario As String)
        Db.Ejecutar("EXEC dbo.sp_registrar_entrada @placa = @p, @tipo = @t, @parqueadero = @c, @usuario = @u",
                    New SqlParameter("@p", placa.Trim().ToUpper()),
                    New SqlParameter("@t", tipoVehiculo),
                    New SqlParameter("@c", codParqueadero),
                    New SqlParameter("@u", If(usuario, "")))
    End Sub

    ''' <summary>Cobro por hora completa o fracción, mínimo 1 hora.
    ''' Función pura: se puede probar sin base de datos.</summary>
    Public Shared Function CalcularCobro(minutos As Integer, valorHora As Decimal) As Decimal
        Dim horas As Integer = Math.Max(1, CInt(Math.Ceiling(minutos / 60.0)))
        Return horas * valorHora
    End Function

    ''' <summary>Cierra el movimiento y calcula el cobro: hora completa o fracción (mínimo 1 hora).</summary>
    Public Shared Function RegistrarSalida(movimientoId As Integer) As ResultadoSalida
        Dim dt = Db.Consultar("SELECT m.placa, m.fecha_entrada, t.valor_hora
                               FROM movimiento m JOIN tarifa t ON t.tipo_vehiculo = m.tipo_vehiculo
                               WHERE m.movimiento_id = @id AND m.fecha_salida IS NULL",
                              New SqlParameter("@id", movimientoId))
        If dt.Rows.Count = 0 Then Return Nothing

        Dim fila = dt.Rows(0)
        Dim entrada As DateTime = CDate(fila("fecha_entrada"))
        Dim salida As DateTime = DateTime.Now
        Dim minutos As Integer = Math.Max(1, CInt(Math.Ceiling((salida - entrada).TotalMinutes)))
        Dim valorHora As Decimal = CDec(fila("valor_hora"))
        Dim total As Decimal = CalcularCobro(minutos, valorHora)
        Dim horas As Integer = Math.Max(1, CInt(Math.Ceiling(minutos / 60.0)))

        Db.Ejecutar("UPDATE movimiento SET fecha_salida = @s, minutos = @m, total = @t
                     WHERE movimiento_id = @id",
                    New SqlParameter("@s", salida),
                    New SqlParameter("@m", minutos),
                    New SqlParameter("@t", total),
                    New SqlParameter("@id", movimientoId))

        Return New ResultadoSalida With {
            .Placa = fila("placa").ToString(),
            .Entrada = entrada,
            .Salida = salida,
            .Minutos = minutos,
            .HorasCobradas = horas,
            .ValorHora = valorHora,
            .Total = total
        }
    End Function

    Public Shared Function Abiertos() As DataTable
        Return Db.Consultar("EXEC dbo.sp_movimientos_abiertos")
    End Function

    Public Shared Function Historial() As DataTable
        Return Db.Consultar("EXEC dbo.sp_movimientos_historial")
    End Function

    ' ---------- Estadísticas para el dashboard ----------

    ''' <summary>Una sola llamada al procedimiento almacenado trae las 4 estadísticas.</summary>
    Public Shared Function Estadisticas() As DataRow
        Return Db.Consultar("EXEC dbo.sp_estadisticas_dashboard").Rows(0)
    End Function

    Public Shared Function UltimosMovimientos() As DataTable
        Return Db.Consultar("EXEC dbo.sp_ultimos_movimientos")
    End Function
End Class
