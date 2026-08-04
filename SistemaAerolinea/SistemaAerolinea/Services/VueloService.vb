Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Programación y operación de vuelos: alta, cambio de horario, cambio
''' de estado (retraso, embarque, cancelación) y el tablero de llegadas y salidas.</summary>
Public Class VueloService

    ''' <summary>Los estados por los que pasa un vuelo. El orden es el de la
    ''' operación real: se programa, se aborda, vuela, aterriza.</summary>
    Public Shared ReadOnly Estados As String() =
        {"Programado", "Abordando", "En vuelo", "Aterrizado", "Retrasado", "Cancelado"}

    Public Shared Function Listar(Optional filtro As String = "",
                                  Optional estado As String = Nothing,
                                  Optional desde As Date? = Nothing) As DataTable
        Return Db.Consultar(
            "SELECT * FROM dbo.v_vuelo_detalle
              WHERE (@f = '' OR codigo_vuelo LIKE @like OR ciudad_origen LIKE @like
                     OR ciudad_destino LIKE @like OR nombre_aero LIKE @like
                     OR iata_origen LIKE @like OR iata_destino LIKE @like)
                AND (@e IS NULL OR estado = @e)
                AND (@d IS NULL OR CAST(fecha_salida AS DATE) >= @d)
              ORDER BY fecha_salida DESC",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"),
            New SqlParameter("@e", Db.Opcional(estado)),
            New SqlParameter("@d", If(desde.HasValue, CObj(desde.Value.Date), DBNull.Value)))
    End Function

    Public Shared Function Obtener(idVuelo As Integer) As DataRow
        Return Db.ConsultarFila("SELECT * FROM dbo.v_vuelo_detalle WHERE idvuelo = @v",
                                New SqlParameter("@v", idVuelo))
    End Function

    ''' <summary>Tablero de llegadas y salidas de un día.</summary>
    Public Shared Function Itinerario(Optional fecha As Date? = Nothing) As DataTable
        Return Db.Consultar("EXEC dbo.sp_itinerario @fecha = @f",
                            New SqlParameter("@f", If(fecha.HasValue, CObj(fecha.Value.Date), DBNull.Value)))
    End Function

    Public Shared Function ExisteCodigo(codigo As String, Optional exceptoId As Integer = 0) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM vuelo WHERE codigo_vuelo = @c AND idvuelo <> @v",
                         New SqlParameter("@c", codigo.Trim().ToUpper()),
                         New SqlParameter("@v", exceptoId)) > 0
    End Function

    ''' <summary>Sugiere el código del próximo vuelo de una aerolínea: su prefijo
    ''' de dos letras, un número correlativo y el día de salida (AV101-0308).</summary>
    Public Shared Function SugerirCodigo(idAerolinea As Integer, fechaSalida As Date) As String
        Dim prefijo = Db.Escalar("SELECT codigo FROM aerolinea WHERE idaerolinea = @a",
                                 New SqlParameter("@a", idAerolinea))
        If prefijo Is Nothing Then Return ""

        Dim numero = Db.Contar("SELECT COUNT(*) + 101 FROM vuelo WHERE idaerolinea = @a",
                               New SqlParameter("@a", idAerolinea))
        Return $"{prefijo}{numero}-{fechaSalida:ddMM}"
    End Function

    ''' <summary>Crea un vuelo. Devuelve Nothing si salió bien, o el mensaje de error.</summary>
    Public Shared Function Crear(codigo As String, idAerolinea As Integer, idAvion As String,
                                 origen As String, destino As String,
                                 salida As DateTime, llegada As DateTime,
                                 precioBase As Decimal, estado As String, puerta As String) As String

        Dim validacion = Validar(codigo, origen, destino, salida, llegada, precioBase)
        If validacion IsNot Nothing Then Return validacion
        If ExisteCodigo(codigo) Then Return "Ya existe un vuelo con ese código."

        Db.Ejecutar("INSERT INTO vuelo (codigo_vuelo, idaerolinea, idavion, idaeropuerto_origen,
                                        idaeropuerto_destino, fecha_salida, fecha_llegada,
                                        precio_base, estado, puerta)
                     VALUES (@c, @al, @av, @o, @d, @s, @l, @p, @e, @g)",
                    New SqlParameter("@c", codigo.Trim().ToUpper()),
                    New SqlParameter("@al", idAerolinea),
                    New SqlParameter("@av", idAvion),
                    New SqlParameter("@o", origen),
                    New SqlParameter("@d", destino),
                    New SqlParameter("@s", salida),
                    New SqlParameter("@l", llegada),
                    New SqlParameter("@p", precioBase),
                    New SqlParameter("@e", estado),
                    New SqlParameter("@g", Db.Opcional(puerta)))

        BitacoraService.Registrar(BitacoraService.CREAR, "vuelo",
                                  $"{codigo.Trim().ToUpper()} · {Formato.FechaHora(salida)}")
        Return Nothing
    End Function

    ''' <summary>Modifica un vuelo. No permite cambiar el avión si ya hay boletos
    ''' vendidos: los asientos comprados pertenecen a esa aeronave concreta.</summary>
    Public Shared Function Actualizar(idVuelo As Integer, codigo As String, idAerolinea As Integer,
                                      idAvion As String, origen As String, destino As String,
                                      salida As DateTime, llegada As DateTime,
                                      precioBase As Decimal, estado As String, puerta As String) As String

        Dim validacion = Validar(codigo, origen, destino, salida, llegada, precioBase)
        If validacion IsNot Nothing Then Return validacion
        If ExisteCodigo(codigo, idVuelo) Then Return "Ya existe otro vuelo con ese código."

        Dim avionActual = Db.Escalar("SELECT idavion FROM vuelo WHERE idvuelo = @v",
                                     New SqlParameter("@v", idVuelo))
        If avionActual IsNot Nothing AndAlso avionActual.ToString() <> idAvion AndAlso BoletosVendidos(idVuelo) > 0 Then
            Return "No se puede cambiar el avión: este vuelo ya tiene boletos vendidos con asientos asignados."
        End If

        Db.Ejecutar("UPDATE vuelo SET codigo_vuelo = @c, idaerolinea = @al, idavion = @av,
                                      idaeropuerto_origen = @o, idaeropuerto_destino = @d,
                                      fecha_salida = @s, fecha_llegada = @l,
                                      precio_base = @p, estado = @e, puerta = @g
                     WHERE idvuelo = @v",
                    New SqlParameter("@v", idVuelo),
                    New SqlParameter("@c", codigo.Trim().ToUpper()),
                    New SqlParameter("@al", idAerolinea),
                    New SqlParameter("@av", idAvion),
                    New SqlParameter("@o", origen),
                    New SqlParameter("@d", destino),
                    New SqlParameter("@s", salida),
                    New SqlParameter("@l", llegada),
                    New SqlParameter("@p", precioBase),
                    New SqlParameter("@e", estado),
                    New SqlParameter("@g", Db.Opcional(puerta)))

        BitacoraService.Registrar(BitacoraService.EDITAR, "vuelo", codigo.Trim().ToUpper())
        Return Nothing
    End Function

    Private Shared Function Validar(codigo As String, origen As String, destino As String,
                                    salida As DateTime, llegada As DateTime,
                                    precioBase As Decimal) As String
        If String.IsNullOrWhiteSpace(codigo) Then Return "Escribe el código del vuelo."
        If String.IsNullOrWhiteSpace(origen) OrElse String.IsNullOrWhiteSpace(destino) Then
            Return "Selecciona el aeropuerto de origen y el de destino."
        End If
        If origen = destino Then Return "El origen y el destino no pueden ser el mismo aeropuerto."
        If precioBase <= 0 Then Return "El precio base debe ser mayor que cero."

        Return Validador.ValidarHorarioVuelo(salida, llegada)
    End Function

    ''' <summary>Cambia solo el estado. Es la acción más frecuente de la operación
    ''' diaria (marcar un retraso, abrir el embarque), por eso tiene su propio método.</summary>
    Public Shared Function CambiarEstado(idVuelo As Integer, estado As String) As String
        If Not Estados.Contains(estado) Then Return "Estado de vuelo no válido."

        Dim codigo = Db.Escalar("SELECT codigo_vuelo FROM vuelo WHERE idvuelo = @v",
                                New SqlParameter("@v", idVuelo))
        If codigo Is Nothing Then Return "No se encontró el vuelo."

        Db.Ejecutar("UPDATE vuelo SET estado = @e WHERE idvuelo = @v",
                    New SqlParameter("@e", estado),
                    New SqlParameter("@v", idVuelo))

        ' Cancelar un vuelo cancela también los boletos: los pasajeros ya no viajan
        ' y sus asientos quedan liberados por el índice único filtrado.
        If estado = "Cancelado" Then
            Db.Ejecutar("UPDATE boleto SET estado = 'Cancelado'
                         WHERE idvuelo = @v AND estado NOT IN ('Cancelado', 'Abordado')",
                        New SqlParameter("@v", idVuelo))
        End If

        BitacoraService.Registrar(BitacoraService.EDITAR, "vuelo", $"{codigo} → {estado}")
        Return Nothing
    End Function

    Public Shared Function BoletosVendidos(idVuelo As Integer) As Integer
        Return Db.Contar("SELECT COUNT(*) FROM boleto WHERE idvuelo = @v AND estado <> 'Cancelado'",
                         New SqlParameter("@v", idVuelo))
    End Function

    ''' <summary>Elimina un vuelo. Solo se permite si nunca se le vendió nada:
    ''' un vuelo con historial se cancela, no se borra.</summary>
    Public Shared Function Eliminar(idVuelo As Integer) As String
        Dim conBoletos = Db.Contar("SELECT COUNT(*) FROM boleto WHERE idvuelo = @v",
                                   New SqlParameter("@v", idVuelo))
        If conBoletos > 0 Then
            Return "Este vuelo tiene boletos asociados. Cámbialo a 'Cancelado' en vez de eliminarlo."
        End If

        Dim codigo = Db.Escalar("SELECT codigo_vuelo FROM vuelo WHERE idvuelo = @v",
                                New SqlParameter("@v", idVuelo))
        Db.Ejecutar("DELETE FROM vuelo WHERE idvuelo = @v", New SqlParameter("@v", idVuelo))

        BitacoraService.Registrar(BitacoraService.ELIMINAR, "vuelo", If(codigo, "").ToString())
        Return Nothing
    End Function
End Class
