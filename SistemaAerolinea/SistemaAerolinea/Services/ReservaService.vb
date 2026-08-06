Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Todo el ciclo de vida de una reserva: buscar el vuelo, ver qué
''' asientos quedan, emitir los boletos, cobrar, hacer el check-in y cancelar.
'''
''' La emisión es la operación crítica del sistema: toca tres tablas y no puede
''' quedar a medias, por eso corre dentro de una transacción.</summary>
Public Class ReservaService

    ' ================================================================
    '  BÚSQUEDA DE VUELOS
    ' ================================================================

    ''' <summary>Vuelos con asientos disponibles. Los tres filtros son opcionales.</summary>
    Public Shared Function BuscarVuelos(Optional origen As String = Nothing,
                                        Optional destino As String = Nothing,
                                        Optional fecha As Date? = Nothing) As DataTable
        Return Db.Consultar("EXEC dbo.sp_buscar_vuelos @origen = @o, @destino = @d, @fecha = @f",
                            New SqlParameter("@o", Db.Opcional(origen)),
                            New SqlParameter("@d", Db.Opcional(destino)),
                            New SqlParameter("@f", If(fecha.HasValue, CObj(fecha.Value.Date), DBNull.Value)))
    End Function

    ''' <summary>Los datos del vuelo que el asistente arrastra de un paso al siguiente.</summary>
    Public Shared Function ObtenerVuelo(idVuelo As Integer) As VueloElegido
        Dim fila = Db.ConsultarFila("SELECT * FROM dbo.v_vuelo_detalle WHERE idvuelo = @v",
                                    New SqlParameter("@v", idVuelo))
        If fila Is Nothing Then Return Nothing

        Return New VueloElegido With {
            .IdVuelo = CInt(fila("idvuelo")),
            .CodigoVuelo = fila("codigo_vuelo").ToString(),
            .Aerolinea = fila("nombre_aero").ToString(),
            .TipoAvion = fila("tipo_avion").ToString(),
            .IataOrigen = fila("iata_origen").ToString(),
            .CiudadOrigen = fila("ciudad_origen").ToString(),
            .IataDestino = fila("iata_destino").ToString(),
            .CiudadDestino = fila("ciudad_destino").ToString(),
            .FechaSalida = CDate(fila("fecha_salida")),
            .FechaLlegada = CDate(fila("fecha_llegada")),
            .DuracionMinutos = CInt(fila("duracion_minutos")),
            .AsientosPorFila = CInt(fila("asientos_por_fila")),
            .AsientosDisponibles = CInt(fila("asientos_disponibles"))
        }
    End Function

    ' ================================================================
    '  MAPA DE ASIENTOS
    ' ================================================================

    ''' <summary>Todos los asientos del avión que hace ese vuelo, marcando cuáles
    ''' ya están vendidos y con el precio que tiene cada clase en ESTE vuelo.</summary>
    Public Shared Function MapaAsientos(idVuelo As Integer) As List(Of AsientoMapa)
        Dim dt = Db.Consultar("EXEC dbo.sp_mapa_asientos @idvuelo = @v",
                              New SqlParameter("@v", idVuelo))

        Dim mapa As New List(Of AsientoMapa)()
        For Each fila As DataRow In dt.Rows
            mapa.Add(New AsientoMapa With {
                .IdAsiento = CInt(fila("idasiento")),
                .Fila = CInt(fila("fila")),
                .Letra = fila("letra").ToString(),
                .Etiqueta = fila("etiqueta").ToString(),
                .Clase = fila("clase").ToString(),
                .IdTarifa = CInt(fila("idtarifa")),
                .Precio = CDec(fila("precio")),
                .Impuesto = CDec(fila("impuesto")),
                .Total = CDec(fila("total")),
                .EquipajeKg = CInt(fila("equipaje_incluido_kg")),
                .Ocupado = CBool(fila("ocupado"))
            })
        Next
        Return mapa
    End Function

    ' ================================================================
    '  EMISIÓN DE LA RESERVA
    ' ================================================================

    ''' <summary>Emite una reserva completa: la reserva, un boleto por asiento y,
    ''' si se indica un método de pago, el pago que la confirma.
    '''
    ''' Todo ocurre dentro de una transacción serializable: si un asiento se vendió
    ''' un instante antes desde otra terminal, se deshace todo y no queda ninguna
    ''' reserva a medias. Devuelve el resultado, o lanza una excepción que la vista
    ''' traduce con MensajeError.</summary>
    ''' <param name="idMetodoPago">Nothing deja la reserva como "Pendiente de pago".</param>
    Public Shared Function Emitir(idVuelo As Integer,
                                  asientos As List(Of AsientoElegido),
                                  idPasajeroTitular As String,
                                  Optional idMetodoPago As Integer? = Nothing,
                                  Optional tipoComprobante As String = Comprobante.FACTURA,
                                  Optional observacion As String = Nothing) As ResultadoReserva

        If asientos Is Nothing OrElse asientos.Count = 0 Then
            Throw New InvalidOperationException("No se seleccionó ningún asiento.")
        End If
        If asientos.Any(Function(a) Not a.Asignado) Then
            Throw New InvalidOperationException("Falta asignarle un pasajero a algún asiento.")
        End If
        If String.IsNullOrWhiteSpace(idPasajeroTitular) Then
            Throw New InvalidOperationException("La reserva necesita un titular.")
        End If

        Dim resultado As New ResultadoReserva()
        Dim usuario = Sesion.NombreUsuario

        Db.EnTransaccion(
            Sub(cn, tx)
                ' --- El vuelo debe seguir siendo vendible ---
                Dim estadoVuelo = Db.EscalarEn(cn, tx,
                    "SELECT estado FROM vuelo WHERE idvuelo = @v",
                    New SqlParameter("@v", idVuelo))

                If estadoVuelo Is Nothing Then
                    Throw New InvalidOperationException("El vuelo ya no existe.")
                End If
                If estadoVuelo.ToString() = "Cancelado" Then
                    Throw New InvalidOperationException("Ese vuelo fue cancelado: ya no se pueden emitir boletos.")
                End If

                ' --- Los asientos deben seguir libres ---
                For Each elegido In asientos
                    Dim vendido = CInt(Db.EscalarEn(cn, tx,
                        "SELECT COUNT(*) FROM boleto
                          WHERE idvuelo = @v AND idasiento = @a AND estado <> 'Cancelado'",
                        New SqlParameter("@v", idVuelo),
                        New SqlParameter("@a", elegido.Asiento.IdAsiento)))

                    If vendido > 0 Then
                        Throw New InvalidOperationException(
                            $"El asiento {elegido.Etiqueta} acaba de ser ocupado por otra terminal. " &
                            "Elige otro asiento e inténtalo de nuevo.")
                    End If
                Next

                ' --- Cabecera de la reserva, con un localizador libre ---
                Dim pnr = ReservarLocalizador(cn, tx)
                Dim idReserva = CInt(Db.EscalarEn(cn, tx,
                    "INSERT INTO reserva (codigo_reserva, idpasajero, estado, subtotal, impuesto,
                                          costo, observacion, usuario_registra)
                     OUTPUT INSERTED.idreserva
                     VALUES (@pnr, @p, 'Pendiente de pago', 0, 0, 0, @obs, @u)",
                    New SqlParameter("@pnr", pnr),
                    New SqlParameter("@p", idPasajeroTitular.Trim()),
                    New SqlParameter("@obs", Db.Opcional(observacion)),
                    New SqlParameter("@u", usuario)))

                ' --- Un boleto por asiento ---
                Dim subtotal As Decimal = 0
                Dim impuesto As Decimal = 0

                For Each elegido In asientos
                    Db.EjecutarEn(cn, tx,
                        "INSERT INTO boleto (idreserva, idvuelo, idasiento, idpasajero, idtarifa,
                                             precio, impuesto, total, estado)
                         VALUES (@r, @v, @a, @p, @t, @precio, @imp, @total, 'Emitido')",
                        New SqlParameter("@r", idReserva),
                        New SqlParameter("@v", idVuelo),
                        New SqlParameter("@a", elegido.Asiento.IdAsiento),
                        New SqlParameter("@p", elegido.IdPasajero),
                        New SqlParameter("@t", elegido.Asiento.IdTarifa),
                        New SqlParameter("@precio", elegido.Precio),
                        New SqlParameter("@imp", elegido.Impuesto),
                        New SqlParameter("@total", elegido.Total))

                    subtotal += elegido.Precio
                    impuesto += elegido.Impuesto
                Next

                Dim total = subtotal + impuesto

                Db.EjecutarEn(cn, tx,
                    "UPDATE reserva SET subtotal = @s, impuesto = @i, costo = @c WHERE idreserva = @r",
                    New SqlParameter("@s", subtotal),
                    New SqlParameter("@i", impuesto),
                    New SqlParameter("@c", total),
                    New SqlParameter("@r", idReserva))

                ' --- Cobro, si se pagó en el momento ---
                If idMetodoPago.HasValue Then
                    Db.EjecutarEn(cn, tx,
                        "INSERT INTO pago (idreserva, idpasajero, idmetodopago, monto, impuesto,
                                           tipo_comprobante, num_comprobante, usuario_registra)
                         VALUES (@r, @p, @m, @monto, @imp, @tipo, @num, @u)",
                        New SqlParameter("@r", idReserva),
                        New SqlParameter("@p", idPasajeroTitular.Trim()),
                        New SqlParameter("@m", idMetodoPago.Value),
                        New SqlParameter("@monto", total),
                        New SqlParameter("@imp", impuesto),
                        New SqlParameter("@tipo", tipoComprobante),
                        New SqlParameter("@num", Comprobante.Numero(tipoComprobante, idReserva, 1)),
                        New SqlParameter("@u", usuario))

                    Db.EjecutarEn(cn, tx,
                        "UPDATE reserva SET estado = 'Confirmada' WHERE idreserva = @r",
                        New SqlParameter("@r", idReserva))
                End If

                resultado.IdReserva = idReserva
                resultado.CodigoReserva = pnr
                resultado.Subtotal = subtotal
                resultado.Impuesto = impuesto
                resultado.Total = total
                resultado.Boletos = asientos.Count
            End Sub)

        Registro.Info($"Reserva {resultado.CodigoReserva} emitida con {resultado.Boletos} boleto(s)")
        BitacoraService.Registrar(BitacoraService.RESERVAR, "reserva",
            $"{resultado.CodigoReserva} · {resultado.Boletos} boleto(s) · {Formato.Dinero(resultado.Total)}")
        Return resultado
    End Function

    ''' <summary>Busca un localizador que no esté en uso. Con 32^6 combinaciones
    ''' la colisión es rarísima, pero si ocurriera la columna UNIQUE la rechazaría
    ''' y la reserva completa se perdería: mejor comprobarlo antes.</summary>
    Private Shared Function ReservarLocalizador(cn As SqlConnection, tx As SqlTransaction) As String
        For intento = 1 To 12
            Dim pnr = GeneradorPnr.Generar()
            Dim usado = CInt(Db.EscalarEn(cn, tx,
                "SELECT COUNT(*) FROM reserva WHERE codigo_reserva = @c",
                New SqlParameter("@c", pnr)))
            If usado = 0 Then Return pnr
        Next
        Throw New InvalidOperationException("No se pudo generar un localizador de reserva libre.")
    End Function

    ' ================================================================
    '  CONSULTA DE RESERVAS
    ' ================================================================

    ''' <summary>Lista las reservas, opcionalmente filtrando por localizador,
    ''' nombre del titular o documento.
    '''
    ''' El filtro por pasajero NO es opcional cuando quien consulta es un pasajero:
    ''' lo aplica automáticamente a partir de la sesión. Así, aunque una pantalla
    ''' del portal se olvidara de pasarlo, nadie puede ver reservas ajenas.</summary>
    Public Shared Function Listar(Optional filtro As String = "",
                                  Optional estado As String = Nothing) As DataTable

        ' Un pasajero solo ve lo suyo, y lo decide el servicio, no la vista.
        ' Se pregunta por el ROL y no por la ficha: si se preguntara por la ficha,
        ' un pasajero sin ella se colaría por la rama de abajo y las vería todas.
        If Sesion.EsSesionDePasajero Then
            Return DelPasajero(Sesion.IdPasajero, filtro, estado)
        End If

        Dim sql = "SELECT * FROM dbo.v_reserva_resumen
                   WHERE (@f = '' OR codigo_reserva LIKE @like OR titular LIKE @like
                          OR num_documento LIKE @like)
                     AND (@e IS NULL OR estado = @e)
                   ORDER BY fecha DESC"

        Return Db.Consultar(sql,
                            New SqlParameter("@f", If(filtro, "").Trim()),
                            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"),
                            New SqlParameter("@e", Db.Opcional(estado)))
    End Function

    ''' <summary>Las reservas de un pasajero: donde es titular y también aquellas
    ''' en que viaja como acompañante de alguien más.</summary>
    Public Shared Function DelPasajero(idPasajero As String,
                                       Optional filtro As String = "",
                                       Optional estado As String = Nothing) As DataTable
        ' Sin ficha no hay reservas que devolver, y hay que atajarlo antes de llegar
        ' a SQL: un parámetro en Nothing no llega a viajar y el procedimiento se
        ' queja de que le falta, con lo que la pantalla reventaría en vez de salir vacía.
        If String.IsNullOrWhiteSpace(idPasajero) Then Return New DataTable()

        Dim datos = Db.Consultar("EXEC dbo.sp_reservas_del_pasajero @idpasajero = @p",
                                 New SqlParameter("@p", idPasajero))

        Dim texto = If(filtro, "").Trim()
        If texto.Length = 0 AndAlso String.IsNullOrWhiteSpace(estado) Then Return datos

        ' El filtrado fino se hace en memoria: son pocas filas y evita repetir el SQL.
        ' Se recorren a mano en vez de armar un RowFilter porque su sintaxis trata
        ' '[', ']', '*' y '%' como comodines: un pasajero que buscara "[" en sus
        ' reservas no encontraría nada, reventaría la pantalla con SyntaxErrorException.
        Dim filtrados = datos.Clone()

        For Each fila As DataRow In datos.Rows
            If Not String.IsNullOrWhiteSpace(estado) AndAlso
               Not String.Equals(fila("estado").ToString(), estado, StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            If texto.Length > 0 AndAlso
               Not Contiene(fila("codigo_reserva"), texto) AndAlso
               Not Contiene(fila("titular"), texto) Then
                Continue For
            End If

            filtrados.ImportRow(fila)
        Next

        Return filtrados
    End Function

    ''' <summary>¿El valor de la celda contiene ese texto, sin distinguir mayúsculas?</summary>
    Private Shared Function Contiene(valor As Object, texto As String) As Boolean
        If valor Is Nothing OrElse IsDBNull(valor) Then Return False
        Return valor.ToString().IndexOf(texto, StringComparison.CurrentCultureIgnoreCase) >= 0
    End Function

    ''' <summary>Próximos vuelos del pasajero, para el encabezado de "Mis vuelos".</summary>
    Public Shared Function ProximosVuelosDe(idPasajero As String) As DataTable
        If String.IsNullOrWhiteSpace(idPasajero) Then Return New DataTable()

        Return Db.Consultar("EXEC dbo.sp_proximos_vuelos_del_pasajero @idpasajero = @p",
                            New SqlParameter("@p", idPasajero))
    End Function

    ''' <summary>¿Esta reserva le pertenece al pasajero de la sesión? Se comprueba
    ''' antes de dejar ver el detalle, hacer check-in o descargar un pase: el
    ''' identificador de una reserva es un número, y probar números ajenos es la
    ''' forma más simple de espiar los datos de otro.</summary>
    Public Shared Function EsDelPasajeroActual(idReserva As Integer) As Boolean
        If Not Sesion.EsSesionDePasajero Then Return True   ' el personal ve todas

        ' Un pasajero sin ficha no tiene reservas que le pertenezcan: el parámetro
        ' va vacío, no le cuadra ninguna fila y no se le abre nada
        Dim idPasajero = If(Sesion.IdPasajero, "")

        Return Db.Contar("SELECT COUNT(*) FROM reserva r
                          WHERE r.idreserva = @r
                            AND (r.idpasajero = @p
                                 OR EXISTS (SELECT 1 FROM boleto b
                                            WHERE b.idreserva = r.idreserva AND b.idpasajero = @p))",
                         New SqlParameter("@r", idReserva),
                         New SqlParameter("@p", idPasajero)) > 0
    End Function

    Public Shared Function ObtenerPorId(idReserva As Integer) As DataRow
        If Not EsDelPasajeroActual(idReserva) Then Return Nothing

        Return Db.ConsultarFila("SELECT * FROM dbo.v_reserva_resumen WHERE idreserva = @r",
                                New SqlParameter("@r", idReserva))
    End Function

    Public Shared Function ObtenerPorPnr(pnr As String) As DataRow
        Dim fila = Db.ConsultarFila("SELECT * FROM dbo.v_reserva_resumen WHERE codigo_reserva = @c",
                                    New SqlParameter("@c", If(pnr, "").Trim().ToUpper()))
        If fila Is Nothing Then Return Nothing

        ' Un localizador ajeno tampoco se abre desde el portal
        If Not EsDelPasajeroActual(CInt(fila("idreserva"))) Then Return Nothing
        Return fila
    End Function

    ''' <summary>Los boletos de una reserva: un renglón por pasajero y asiento.</summary>
    Public Shared Function Boletos(idReserva As Integer) As DataTable
        Dim datos = Db.Consultar("SELECT * FROM dbo.v_boleto_detalle WHERE idreserva = @r ORDER BY idboleto",
                                 New SqlParameter("@r", idReserva))

        If Not EsDelPasajeroActual(idReserva) Then Return datos.Clone()
        Return datos
    End Function

    ''' <summary>Un boleto suelto. Igual que su reserva, no se devuelve si es de
    ''' otro pasajero: el identificador de un boleto también es un número
    ''' correlativo, y sin esta comprobación pedir el de al lado bastaría para ver
    ''' el asiento y el vuelo de un desconocido.</summary>
    Public Shared Function Boleto(idBoleto As Integer) As DataRow
        Dim fila = Db.ConsultarFila("SELECT * FROM dbo.v_boleto_detalle WHERE idboleto = @b",
                                    New SqlParameter("@b", idBoleto))
        If fila Is Nothing Then Return Nothing

        If Not EsDelPasajeroActual(CInt(fila("idreserva"))) Then Return Nothing
        Return fila
    End Function

    ''' <summary>Lista de pasajeros de un vuelo, para el embarque.</summary>
    Public Shared Function Manifiesto(idVuelo As Integer) As DataTable
        Return Db.Consultar("EXEC dbo.sp_manifiesto_vuelo @idvuelo = @v",
                            New SqlParameter("@v", idVuelo))
    End Function

    ' ================================================================
    '  CHECK-IN Y CANCELACIÓN
    ' ================================================================

    ''' <summary>Registra el check-in de un boleto. Devuelve Nothing si salió bien,
    ''' o el mensaje que explica por qué no se pudo.</summary>
    Public Shared Function HacerCheckIn(idBoleto As Integer) As String
        Dim fila = Db.ConsultarFila(
            "SELECT b.idreserva, b.estado AS estado_boleto, r.estado AS estado_reserva,
                    v.estado AS estado_vuelo, v.fecha_salida
             FROM boleto b
             JOIN reserva r ON r.idreserva = b.idreserva
             JOIN vuelo   v ON v.idvuelo   = b.idvuelo
             WHERE b.idboleto = @b",
            New SqlParameter("@b", idBoleto))

        If fila Is Nothing Then Return "No se encontró el boleto."
        If Not EsDelPasajeroActual(CInt(fila("idreserva"))) Then
            Return "Ese boleto no pertenece a tus reservas."
        End If

        Select Case fila("estado_boleto").ToString()
            Case "Cancelado" : Return "Ese boleto está cancelado."
            Case "Check-in" : Return "Ese pasajero ya hizo su check-in."
            Case "Abordado" : Return "Ese pasajero ya abordó."
        End Select

        If fila("estado_reserva").ToString() <> "Confirmada" Then
            Return "La reserva todavía no está pagada: no se puede hacer el check-in."
        End If
        If fila("estado_vuelo").ToString() = "Cancelado" Then
            Return "El vuelo fue cancelado."
        End If
        If CDate(fila("fecha_salida")) < DateTime.Now Then
            Return "El vuelo ya salió."
        End If

        Db.Ejecutar("UPDATE boleto SET estado = 'Check-in', fecha_checkin = GETDATE()
                     WHERE idboleto = @b",
                    New SqlParameter("@b", idBoleto))

        BitacoraService.Registrar(BitacoraService.CHECKIN, "boleto", $"Boleto {idBoleto}")
        Return Nothing
    End Function

    ''' <summary>Cancela una reserva completa. Los boletos quedan como 'Cancelado',
    ''' con lo que sus asientos vuelven a estar disponibles gracias al índice único
    ''' filtrado, pero el histórico no se borra.
    '''
    ''' El permiso se comprueba aquí y no solo en la pantalla: cancelar una reserva
    ''' pagada implica devolver dinero, y no puede depender de que una vista se
    ''' acuerde de esconder el botón.</summary>
    Public Shared Function Cancelar(idReserva As Integer, motivo As String) As String
        If Not Permisos.PuedeCancelarReservas Then
            Return "Solo un Administrador puede cancelar una reserva."
        End If

        Dim fila = Db.ConsultarFila("SELECT codigo_reserva, estado, costo FROM reserva WHERE idreserva = @r",
                                    New SqlParameter("@r", idReserva))
        If fila Is Nothing Then Return "No se encontró la reserva."
        If fila("estado").ToString() = "Cancelada" Then Return "Esa reserva ya estaba cancelada."

        Dim yaAbordo = Db.Contar("SELECT COUNT(*) FROM boleto WHERE idreserva = @r AND estado = 'Abordado'",
                                 New SqlParameter("@r", idReserva))
        If yaAbordo > 0 Then Return "No se puede cancelar: hay pasajeros de esta reserva que ya abordaron."

        Dim codigo = fila("codigo_reserva").ToString()

        Db.EnTransaccion(
            Sub(cn, tx)
                Db.EjecutarEn(cn, tx,
                    "UPDATE boleto SET estado = 'Cancelado' WHERE idreserva = @r AND estado <> 'Cancelado'",
                    New SqlParameter("@r", idReserva))

                Db.EjecutarEn(cn, tx,
                    "UPDATE reserva
                        SET estado = 'Cancelada',
                            observacion = LEFT(ISNULL(observacion + ' | ', '') + @m, 200)
                      WHERE idreserva = @r",
                    New SqlParameter("@r", idReserva),
                    New SqlParameter("@m", $"Cancelada por {Sesion.NombreUsuario}: " &
                                           If(String.IsNullOrWhiteSpace(motivo), "sin motivo indicado", motivo.Trim())))
            End Sub)

        Registro.Info($"Reserva {codigo} cancelada")
        BitacoraService.Registrar(BitacoraService.CANCELAR, "reserva", $"{codigo} · {motivo}")
        Return Nothing
    End Function

    ' ================================================================
    '  CÁLCULO DE TOTALES (función pura, se prueba sin base de datos)
    ' ================================================================

    ''' <summary>Suma el subtotal, el impuesto y el total de una selección de
    ''' asientos. Está aparte de la emisión para poder mostrarle el desglose al
    ''' usuario antes de confirmar y para poder probarlo sin tocar SQL Server.</summary>
    Public Shared Sub CalcularTotales(asientos As IEnumerable(Of AsientoElegido),
                                      ByRef subtotal As Decimal,
                                      ByRef impuesto As Decimal,
                                      ByRef total As Decimal)
        subtotal = 0
        impuesto = 0
        total = 0
        If asientos Is Nothing Then Return

        For Each a In asientos
            subtotal += a.Precio
            impuesto += a.Impuesto
        Next
        total = subtotal + impuesto
    End Sub
End Class
