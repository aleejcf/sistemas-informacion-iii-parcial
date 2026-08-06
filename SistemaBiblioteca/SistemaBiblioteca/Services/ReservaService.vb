Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>La fila de espera: cuando todas las copias de un título están
''' prestadas, el socio se apunta en vez de volver a preguntar cada día. La
''' reserva se atiende sola cuando ese socio se lleva el libro.</summary>
Public Class ReservaService

    ''' <summary>Días que la reserva se mantiene en pie. Pasado ese plazo caduca:
    ''' de otro modo una reserva olvidada bloquearía las renovaciones para siempre.</summary>
    Public Const DIAS_VIGENCIA As Integer = 7

    Public Shared Function Listar(Optional soloActivas As Boolean = True) As DataTable
        Return Db.Consultar(
            "SELECT r.idreserva, r.idlibro, l.titulo, a.nombre AS autor,
                    r.idsocio, s.nombre + ' ' + s.apellido AS socio, s.email,
                    r.fecha_reserva, r.fecha_expira, r.estado,
                    DATEDIFF(DAY, CAST(GETDATE() AS DATE), r.fecha_expira) AS dias_restantes,
                    acervo.disponibles
             FROM reserva r
             JOIN libro l ON l.idlibro = r.idlibro
             JOIN autor a ON a.idautor = l.idautor
             JOIN socio s ON s.idsocio = r.idsocio
             CROSS APPLY (SELECT COUNT(*) AS disponibles FROM ejemplar e
                           WHERE e.idlibro = r.idlibro AND e.estado = 'Disponible') AS acervo
             WHERE (@a = 0 OR r.estado = 'Activa')
             ORDER BY r.fecha_reserva",
            New SqlParameter("@a", If(soloActivas, 1, 0)))
    End Function

    Public Shared Function ReservasDelSocio(idSocio As String) As DataTable
        Return Db.Consultar(
            "SELECT r.idreserva, r.idlibro, l.titulo, r.fecha_reserva, r.fecha_expira, r.estado
             FROM reserva r
             JOIN libro l ON l.idlibro = r.idlibro
             WHERE r.idsocio = @s AND r.estado = 'Activa'
             ORDER BY r.fecha_reserva",
            New SqlParameter("@s", idSocio))
    End Function

    ''' <summary>Aparta un título para un socio. Solo tiene sentido si no hay
    ''' copias libres: si las hay, que se lo lleve ahora mismo.</summary>
    Public Shared Function Crear(idLibro As String, idSocio As String) As String
        Dim libro = LibroService.Obtener(idLibro)
        If libro Is Nothing Then Return "No se encontró el título."

        Dim socio = SocioService.Resumen(idSocio)
        If socio Is Nothing Then Return "No se encontró el socio."
        If Not socio.EstaActivo Then Return "El socio está inactivo y no puede reservar."

        If Db.Numero(libro, "disponibles") > 0 Then
            Return "Este título tiene ejemplares disponibles ahora mismo: no hace falta reservarlo."
        End If

        Dim yaReservado = Db.Contar(
            "SELECT COUNT(*) FROM reserva WHERE idlibro = @l AND idsocio = @s AND estado = 'Activa'",
            New SqlParameter("@l", idLibro),
            New SqlParameter("@s", idSocio))
        If yaReservado > 0 Then Return "Este socio ya tiene una reserva activa de ese título."

        ' Un socio que ya lo tiene prestado no necesita reservarlo
        Dim yaLoTiene = Db.Contar(
            "SELECT COUNT(*)
             FROM detalle_prestamo d
             JOIN prestamo p ON p.idprestamo = d.idprestamo
             JOIN ejemplar e ON e.idejemplar = d.idejemplar
             WHERE p.idsocio = @s AND e.idlibro = @l AND d.fecha_devolucion IS NULL",
            New SqlParameter("@s", idSocio),
            New SqlParameter("@l", idLibro))
        If yaLoTiene > 0 Then Return "Este socio ya tiene prestado un ejemplar de ese título."

        Db.Ejecutar("INSERT INTO reserva (idlibro, idsocio, fecha_expira, estado)
                     VALUES (@l, @s, @f, 'Activa')",
                    New SqlParameter("@l", idLibro),
                    New SqlParameter("@s", idSocio),
                    New SqlParameter("@f", Date.Today.AddDays(DIAS_VIGENCIA)))

        BitacoraService.Registrar(BitacoraService.RESERVA, "reserva",
                                  $"{idLibro} · {Db.Texto(libro, "titulo")} · {socio.NombreCompleto}")
        Return Nothing
    End Function

    Public Shared Function Cancelar(idReserva As Integer) As String
        Dim filas = Db.Ejecutar("UPDATE reserva SET estado = 'Cancelada'
                                 WHERE idreserva = @r AND estado = 'Activa'",
                                New SqlParameter("@r", idReserva))
        If filas = 0 Then Return "Esa reserva ya no está activa."

        BitacoraService.Registrar(BitacoraService.RESERVA, "reserva", $"Reserva {idReserva} cancelada")
        Return Nothing
    End Function

    ''' <summary>Marca como vencidas las reservas que pasaron su fecha. Se llama
    ''' al abrir el panel: es barato y mantiene la fila de espera limpia sin
    ''' necesidad de un trabajo programado en el servidor.</summary>
    Public Shared Function CaducarVencidas() As Integer
        Try
            Return Db.Ejecutar("UPDATE reserva SET estado = 'Vencida'
                                WHERE estado = 'Activa' AND fecha_expira < CAST(GETDATE() AS DATE)")
        Catch ex As Exception
            Registro.Advertencia($"No se pudieron caducar las reservas vencidas: {ex.Message}")
            Return 0
        End Try
    End Function
End Class
