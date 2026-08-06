Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Circulación: prestar, renovar y devolver. Es el servicio central del
''' sistema y el único que necesita transacciones, porque un préstamo toca tres
''' tablas a la vez y ninguna puede quedarse a medias.</summary>
Public Class PrestamoService

    ''' <summary>Un préstamo se puede extender una sola vez. Más allá de eso el
    ''' socio tiene que traer el libro y volver a llevárselo, que es la forma de
    ''' que el ejemplar aparezca de vuelta en la estantería aunque sea un momento.</summary>
    Public Const MAX_RENOVACIONES As Integer = 1

    ''' <summary>Lo que se le cobra al socio cuando un ejemplar no vuelve o vuelve
    ''' inservible. Son cifras de política de la biblioteca, no cálculos: por eso
    ''' están aquí como constantes con nombre y no escondidas en una consulta.</summary>
    Public Const COSTO_REPOSICION As Decimal = 400D
    Public Const COSTO_DANO As Decimal = 150D

    ' ======================= CONSULTAS =======================

    ''' <summary>Lista los préstamos. `situacion` filtra por el semáforo calculado
    ''' (Activo, Por vencer, Vencido, Devuelto, Cancelado), no por el estado crudo.</summary>
    Public Shared Function Listar(Optional filtro As String = "",
                                  Optional situacion As String = Nothing,
                                  Optional desde As Date? = Nothing) As DataTable
        Return Db.Consultar(
            "SELECT * FROM dbo.v_prestamo_detalle
              WHERE (@f = '' OR codigo LIKE @like OR socio LIKE @like
                     OR idsocio LIKE @like OR titulos LIKE @like)
                AND (@s IS NULL OR situacion = @s)
                AND (@d IS NULL OR CAST(fecha_prestamo AS DATE) >= @d)
              ORDER BY CASE situacion
                         WHEN 'Vencido'    THEN 1
                         WHEN 'Por vencer' THEN 2
                         WHEN 'Activo'     THEN 3
                         ELSE 4 END,
                       fecha_vencimiento, fecha_prestamo DESC",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"),
            New SqlParameter("@s", Db.Opcional(situacion)),
            New SqlParameter("@d", If(desde.HasValue, CObj(desde.Value.Date), DBNull.Value)))
    End Function

    Public Shared ReadOnly Situaciones As String() =
        {"Activo", "Por vencer", "Vencido", "Devuelto", "Cancelado"}

    Public Shared Function Obtener(idPrestamo As Integer) As DataRow
        Return Db.ConsultarFila("SELECT * FROM dbo.v_prestamo_detalle WHERE idprestamo = @p",
                                New SqlParameter("@p", idPrestamo))
    End Function

    ''' <summary>Los renglones de un préstamo: qué ejemplares lleva y cuáles ya
    ''' volvieron.</summary>
    Public Shared Function Renglones(idPrestamo As Integer) As DataTable
        Return Db.Consultar(
            "SELECT d.iddetalle, d.idejemplar, e.codigo_barras, l.idlibro, l.titulo,
                    a.nombre AS autor, e.ubicacion, d.fecha_devolucion, d.condicion_devolucion,
                    CASE WHEN d.fecha_devolucion IS NULL THEN 'Prestado' ELSE 'Devuelto' END AS situacion
             FROM detalle_prestamo d
             JOIN ejemplar e ON e.idejemplar = d.idejemplar
             JOIN libro    l ON l.idlibro    = e.idlibro
             JOIN autor    a ON a.idautor    = l.idautor
             WHERE d.idprestamo = @p
             ORDER BY l.titulo",
            New SqlParameter("@p", idPrestamo))
    End Function

    ''' <summary>Solo los ejemplares que todavía no vuelven: es lo que se marca
    ''' en la pantalla de devolución.</summary>
    Public Shared Function RenglonesPendientes(idPrestamo As Integer) As DataTable
        Return Db.Consultar(
            "SELECT d.iddetalle, d.idejemplar, e.codigo_barras, l.titulo
             FROM detalle_prestamo d
             JOIN ejemplar e ON e.idejemplar = d.idejemplar
             JOIN libro    l ON l.idlibro    = e.idlibro
             WHERE d.idprestamo = @p AND d.fecha_devolucion IS NULL
             ORDER BY l.titulo",
            New SqlParameter("@p", idPrestamo))
    End Function

    Public Shared Function Vencidos() As DataTable
        Return Db.Consultar("EXEC dbo.sp_prestamos_vencidos")
    End Function

    Public Shared Function MovimientoDiario(Optional dias As Integer = 14) As DataTable
        Return Db.Consultar("EXEC dbo.sp_movimiento_diario @dias = @d",
                            New SqlParameter("@d", dias))
    End Function

    ' ======================= REGISTRAR UN PRÉSTAMO =======================

    ''' <summary>Registra el préstamo de uno o varios ejemplares a un socio.
    ''' Devuelve Nothing si salió bien (y llena `resultado`), o el mensaje de error.
    '''
    ''' Todo ocurre dentro de una transacción Serializable porque entre el momento
    ''' en que la pantalla mostró "disponible" y el momento en que se guarda, otro
    ''' bibliotecario pudo haber entregado esa misma copia. Las comprobaciones se
    ''' repiten aquí adentro aunque la interfaz ya las haya hecho: la interfaz
    ''' informa, la transacción decide.</summary>
    Public Shared Function Registrar(idSocio As String,
                                     ejemplares As IList(Of EjemplarElegido),
                                     fechaVencimiento As Date,
                                     observacion As String,
                                     forzar As Boolean,
                                     ByRef resultado As ResultadoPrestamo) As String
        resultado = Nothing

        If String.IsNullOrWhiteSpace(idSocio) Then Return "Selecciona el socio que se lleva los libros."
        If ejemplares Is Nothing OrElse ejemplares.Count = 0 Then
            Return "Agrega al menos un ejemplar al préstamo."
        End If

        Dim socio = SocioService.Resumen(idSocio)
        If socio Is Nothing Then Return "No se encontró el socio."
        If Not socio.EstaActivo Then Return "El socio está inactivo y no puede llevarse libros."

        ' Forzar es un permiso de Administrador: sirve para casos excepcionales
        ' (un docente con una multa mínima que necesita el libro hoy), y queda
        ' registrado en la bitácora.
        If Not socio.PuedePrestar Then
            If Not forzar Then Return socio.MotivoBloqueo
            If Not Permisos.PuedeForzarPrestamo Then
                Return socio.MotivoBloqueo & " Solo un administrador puede autorizar el préstamo."
            End If
        End If

        Dim plazoInvalido = Validador.ValidarPlazo(Date.Today, fechaVencimiento)
        If plazoInvalido IsNot Nothing Then Return plazoInvalido

        Dim codigosRepetidos = ejemplares.GroupBy(Function(e) e.IdEjemplar).Any(Function(g) g.Count() > 1)
        If codigosRepetidos Then Return "Hay un ejemplar repetido en la lista."

        Dim problema As String = Nothing
        Dim salida As ResultadoPrestamo = Nothing

        ' Las comprobaciones de adentro avisan lanzando una excepción, que es lo
        ' que hace a la transacción deshacerse. Aquí se atrapa esa misma excepción
        ' —y solo esa, reconocible porque dejó escrito el motivo en `problema`—
        ' para devolverla como mensaje en palabras. Sin este Catch, la excepción
        ' saldría del servicio y la pantalla mostraría un "error inesperado"
        ' genérico en lugar de "ese ejemplar ya no está disponible".
        Try
            Db.EnTransaccion(
                Sub(cn, tx)
                    ' 1) El cupo, otra vez y con los datos de este instante
                    Dim afuera = Db.ContarEn(cn, tx,
                        "SELECT COUNT(*) FROM detalle_prestamo d
                          JOIN prestamo p ON p.idprestamo = d.idprestamo
                         WHERE p.idsocio = @s AND d.fecha_devolucion IS NULL",
                        New SqlParameter("@s", idSocio))

                    If Not forzar AndAlso afuera + ejemplares.Count > socio.MaxPrestamos Then
                        problema = $"Como {socio.TipoSocio} puede tener {socio.MaxPrestamos} ejemplares a la vez. " &
                                   $"Ya tiene {afuera} y está intentando llevarse {ejemplares.Count} más."
                        Throw New InvalidOperationException(problema)
                    End If

                    ' 2) Que cada ejemplar siga disponible. UPDLOCK reserva la fila
                    '    hasta el commit: nadie más la puede tomar mientras tanto.
                    For Each ejemplar In ejemplares
                        Dim estado = Db.EscalarEn(cn, tx,
                            "SELECT estado FROM ejemplar WITH (UPDLOCK, ROWLOCK) WHERE idejemplar = @e",
                            New SqlParameter("@e", ejemplar.IdEjemplar))

                        If estado Is Nothing Then
                            problema = $"El ejemplar {ejemplar.CodigoBarras} ya no existe en el inventario."
                            Throw New InvalidOperationException(problema)
                        End If
                        If estado.ToString() <> "Disponible" Then
                            problema = $"El ejemplar {ejemplar.CodigoBarras} ya no está disponible " &
                                       $"(ahora figura como {estado.ToString().ToLower()})."
                            Throw New InvalidOperationException(problema)
                        End If
                    Next

                    ' 3) El folio del préstamo. Dentro de la transacción Serializable
                    '    dos mostradores no pueden sacar el mismo número.
                    Dim siguiente = Db.ContarEn(cn, tx,
                        "SELECT ISNULL(MAX(CAST(SUBSTRING(codigo, 4, 6) AS INT)), 0) + 1 FROM prestamo")
                    Dim codigo = Formato.Correlativo("PR-", siguiente)

                    ' 4) La cabecera
                    Dim idPrestamo = CInt(Db.EscalarEn(cn, tx,
                        "INSERT INTO prestamo (codigo, idsocio, fecha_vencimiento, estado, usuario_registra, observacion)
                         VALUES (@c, @s, @v, 'Activo', @u, @o);
                         SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        New SqlParameter("@c", codigo),
                        New SqlParameter("@s", idSocio),
                        New SqlParameter("@v", fechaVencimiento.Date),
                        New SqlParameter("@u", Sesion.NombreUsuario),
                        New SqlParameter("@o", Db.Opcional(observacion))))

                    ' 5) Los renglones y el estado de cada copia
                    For Each ejemplar In ejemplares
                        Db.EjecutarEn(cn, tx,
                            "INSERT INTO detalle_prestamo (idprestamo, idejemplar) VALUES (@p, @e)",
                            New SqlParameter("@p", idPrestamo),
                            New SqlParameter("@e", ejemplar.IdEjemplar))

                        Db.EjecutarEn(cn, tx,
                            "UPDATE ejemplar SET estado = 'Prestado' WHERE idejemplar = @e",
                            New SqlParameter("@e", ejemplar.IdEjemplar))
                    Next

                    ' 6) Si el socio tenía reservado alguno de estos títulos, la
                    '    reserva queda atendida: para eso se hizo la fila de espera.
                    For Each idLibro In ejemplares.Select(Function(e) e.IdLibro).Distinct()
                        Db.EjecutarEn(cn, tx,
                            "UPDATE reserva SET estado = 'Atendida'
                             WHERE idlibro = @l AND idsocio = @s AND estado = 'Activa'",
                            New SqlParameter("@l", idLibro),
                            New SqlParameter("@s", idSocio))
                    Next

                    salida = New ResultadoPrestamo With {
                        .IdPrestamo = idPrestamo,
                        .Codigo = codigo,
                        .FechaVencimiento = fechaVencimiento.Date,
                        .Ejemplares = ejemplares.Count,
                        .Socio = socio.NombreCompleto
                    }
                End Sub)

        Catch ex As InvalidOperationException When problema IsNot Nothing
            ' Rechazo previsto: la transacción ya se deshizo y `problema` explica
            ' por qué. Cualquier otra excepción sí sigue subiendo.
        End Try

        If problema IsNot Nothing Then Return problema

        resultado = salida
        BitacoraService.Registrar(BitacoraService.PRESTAMO, "prestamo",
            $"{salida.Codigo} · {socio.NombreCompleto} · {salida.Ejemplares} ejemplares · " &
            $"vence {Formato.Fecha(salida.FechaVencimiento)}" & If(forzar, " · AUTORIZADO manualmente", ""))
        Registro.Info($"Préstamo {salida.Codigo} registrado a {idSocio}")
        Return Nothing
    End Function

    ''' <summary>La fecha de devolución que le toca a un socio según su tipo.
    ''' Es la que la pantalla propone; el bibliotecario puede cambiarla.</summary>
    Public Shared Function VencimientoSugerido(idSocio As String) As Date
        Dim dias = Db.Contar("SELECT t.dias_prestamo FROM socio s
                              JOIN tipo_socio t ON t.idtipo = s.idtipo
                              WHERE s.idsocio = @s",
                             New SqlParameter("@s", idSocio))
        Return Date.Today.AddDays(If(dias > 0, dias, 7))
    End Function

    ' ======================= DEVOLVER =======================

    ''' <summary>Registra la devolución de los ejemplares marcados. Devuelve
    ''' Nothing y llena `resultado`, o el mensaje de error.
    '''
    ''' Aquí es donde la fecha de vencimiento deja de ser decorativa: si el libro
    ''' llegó tarde se genera la multa, y si llegó roto o no llegó, también.</summary>
    Public Shared Function RegistrarDevolucion(idPrestamo As Integer,
                                               lineas As IList(Of LineaDevolucion),
                                               ByRef resultado As ResultadoDevolucion) As String
        resultado = Nothing

        If lineas Is Nothing OrElse lineas.Count = 0 Then
            Return "Marca al menos un ejemplar para registrar la devolución."
        End If

        Dim cabecera = Obtener(idPrestamo)
        If cabecera Is Nothing Then Return "No se encontró el préstamo."
        If Db.Texto(cabecera, "estado") <> "Activo" Then
            Return "Este préstamo ya está cerrado; no hay nada que devolver."
        End If

        Dim idSocio = Db.Texto(cabecera, "idsocio")
        Dim codigo = Db.Texto(cabecera, "codigo")
        Dim multaDiaria = Db.Monto(cabecera, "multa_diaria")
        Dim fechaVencimiento = CDate(cabecera("fecha_vencimiento"))
        Dim diasRetraso = Validador.DiasDeRetraso(fechaVencimiento, Date.Today)

        Dim problema As String = Nothing
        Dim salida As New ResultadoDevolucion With {.DiasRetraso = diasRetraso}

        ' Mismo patrón que en Registrar: la excepción deshace la transacción y
        ' aquí se convierte en un mensaje entendible.
        Try
            Db.EnTransaccion(
                Sub(cn, tx)
                    Dim devueltos = 0
                    Dim extraviados = 0
                    Dim danados = 0

                    For Each linea In lineas
                        ' Solo se cierra el renglón si sigue abierto: si otro usuario
                        ' ya registró esta devolución, este UPDATE afecta 0 filas y no
                        ' se cuenta dos veces.
                        Dim filas = Db.EjecutarEn(cn, tx,
                            "UPDATE detalle_prestamo
                                SET fecha_devolucion = GETDATE(), condicion_devolucion = @c
                              WHERE iddetalle = @d AND idprestamo = @p AND fecha_devolucion IS NULL",
                            New SqlParameter("@d", linea.IdDetalle),
                            New SqlParameter("@p", idPrestamo),
                            New SqlParameter("@c", linea.Condicion))

                        If filas = 0 Then Continue For
                        devueltos += 1

                        ' El estado en que vuelve la copia decide si regresa a la
                        ' estantería, al taller o al registro de pérdidas.
                        Dim estadoEjemplar As String
                        Select Case linea.Condicion
                            Case "Extraviado"
                                estadoEjemplar = "Extraviado"
                                extraviados += 1
                            Case "Deteriorado"
                                estadoEjemplar = "Reparación"
                                danados += 1
                            Case Else
                                estadoEjemplar = "Disponible"
                        End Select

                        Db.EjecutarEn(cn, tx,
                            "UPDATE ejemplar SET estado = @es,
                                    condicion = CASE WHEN @co = 'Extraviado' THEN condicion ELSE @co END
                             WHERE idejemplar = @e",
                            New SqlParameter("@e", linea.IdEjemplar),
                            New SqlParameter("@es", estadoEjemplar),
                            New SqlParameter("@co", linea.Condicion))
                    Next

                    If devueltos = 0 Then
                        problema = "Esos ejemplares ya figuran como devueltos."
                        Throw New InvalidOperationException(problema)
                    End If
                    salida.Ejemplares = devueltos

                    ' ¿Quedó algo afuera? Si no, el préstamo se cierra.
                    Dim pendientes = Db.ContarEn(cn, tx,
                        "SELECT COUNT(*) FROM detalle_prestamo
                         WHERE idprestamo = @p AND fecha_devolucion IS NULL",
                        New SqlParameter("@p", idPrestamo))

                    If pendientes = 0 Then
                        Db.EjecutarEn(cn, tx,
                            "UPDATE prestamo SET estado = 'Devuelto', fecha_devolucion = GETDATE()
                             WHERE idprestamo = @p",
                            New SqlParameter("@p", idPrestamo))
                        salida.PrestamoCerrado = True
                    End If

                    ' ---- Multas ----
                    ' Por retraso: se cobra por cada ejemplar que llegó tarde en ESTA
                    ' entrega. Si el socio trae dos hoy y uno la otra semana, cada
                    ' entrega genera su propia multa con sus propios días.
                    Dim montoTotal As Decimal = 0

                    If diasRetraso > 0 Then
                        Dim monto = Validador.CalcularMulta(diasRetraso, multaDiaria, devueltos)
                        If monto > 0 Then
                            InsertarMulta(cn, tx, idPrestamo, idSocio, "Retraso", diasRetraso, monto,
                                          $"{devueltos} ejemplares con {diasRetraso} días de retraso " &
                                          $"a {Formato.Dinero(multaDiaria)} por día.")
                            montoTotal += monto
                        End If
                    End If

                    If extraviados > 0 Then
                        Dim monto = COSTO_REPOSICION * extraviados
                        InsertarMulta(cn, tx, idPrestamo, idSocio, "Extravío", 0, monto,
                                      $"{extraviados} ejemplares no devueltos, a {Formato.Dinero(COSTO_REPOSICION)} de reposición.")
                        montoTotal += monto
                    End If

                    If danados > 0 Then
                        Dim monto = COSTO_DANO * danados
                        InsertarMulta(cn, tx, idPrestamo, idSocio, "Daño", 0, monto,
                                      $"{danados} ejemplares devueltos deteriorados, a {Formato.Dinero(COSTO_DANO)} cada uno.")
                        montoTotal += monto
                    End If

                    salida.MontoMulta = montoTotal
                End Sub)

        Catch ex As InvalidOperationException When problema IsNot Nothing
            ' Rechazo previsto: no había nada que devolver
        End Try

        If problema IsNot Nothing Then Return problema

        resultado = salida
        BitacoraService.Registrar(BitacoraService.DEVOLUCION, "prestamo",
            $"{codigo} · {salida.Ejemplares} ejemplares" &
            If(salida.HuboMulta, $" · multa {Formato.Dinero(salida.MontoMulta)}", "") &
            If(salida.PrestamoCerrado, " · préstamo cerrado", " · quedan ejemplares afuera"))

        If salida.HuboMulta Then
            BitacoraService.Registrar(BitacoraService.MULTA_GENERADA, "multa",
                                      $"{codigo} · {Formato.Dinero(salida.MontoMulta)}")
        End If
        Return Nothing
    End Function

    Private Shared Sub InsertarMulta(cn As SqlConnection, tx As SqlTransaction,
                                     idPrestamo As Integer, idSocio As String, motivo As String,
                                     diasRetraso As Integer, monto As Decimal, detalle As String)
        Db.EjecutarEn(cn, tx,
            "INSERT INTO multa (idprestamo, idsocio, motivo, dias_retraso, monto,
                                estado, usuario_registra, observacion)
             VALUES (@p, @s, @m, @d, @mo, 'Pendiente', @u, @o)",
            New SqlParameter("@p", idPrestamo),
            New SqlParameter("@s", idSocio),
            New SqlParameter("@m", motivo),
            New SqlParameter("@d", diasRetraso),
            New SqlParameter("@mo", monto),
            New SqlParameter("@u", Sesion.NombreUsuario),
            New SqlParameter("@o", detalle))
    End Sub

    ' ======================= RENOVAR =======================

    ''' <summary>Extiende el plazo de un préstamo activo. Se niega si ya venció
    ''' (renovar una mora sería premiarla), si ya se renovó, o si otro socio está
    ''' esperando alguno de esos títulos.</summary>
    Public Shared Function Renovar(idPrestamo As Integer, ByRef nuevoVencimiento As Date) As String
        nuevoVencimiento = Date.Today

        Dim cabecera = Obtener(idPrestamo)
        If cabecera Is Nothing Then Return "No se encontró el préstamo."
        If Db.Texto(cabecera, "estado") <> "Activo" Then Return "Solo se renuevan los préstamos activos."

        If Db.Numero(cabecera, "dias_retraso") > 0 Then
            Return "Este préstamo ya está vencido. Regístrale la devolución antes de volver a prestarlo."
        End If

        Dim renovaciones = Db.Numero(cabecera, "renovaciones")
        If renovaciones >= MAX_RENOVACIONES Then
            Return $"Este préstamo ya se renovó {renovaciones} vez. " &
                   "El socio debe traer los libros antes de volver a llevárselos."
        End If

        ' Si alguien reservó uno de los títulos, la prórroga se le negaría a
        ' quien está esperando en la fila.
        Dim reservados = Db.Contar(
            "SELECT COUNT(*)
             FROM detalle_prestamo d
             JOIN ejemplar e ON e.idejemplar = d.idejemplar
             JOIN reserva  r ON r.idlibro = e.idlibro AND r.estado = 'Activa'
             WHERE d.idprestamo = @p AND d.fecha_devolucion IS NULL",
            New SqlParameter("@p", idPrestamo))

        If reservados > 0 Then
            Return "No se puede renovar: otro socio tiene reservado uno de estos títulos."
        End If

        Dim dias = Db.Numero(cabecera, "dias_prestamo")
        If dias <= 0 Then dias = 7
        Dim vencimientoActual = CDate(cabecera("fecha_vencimiento"))
        Dim vencimiento = vencimientoActual.AddDays(dias)

        Db.Ejecutar("UPDATE prestamo SET fecha_vencimiento = @v, renovaciones = renovaciones + 1
                     WHERE idprestamo = @p",
                    New SqlParameter("@p", idPrestamo),
                    New SqlParameter("@v", vencimiento))

        nuevoVencimiento = vencimiento
        BitacoraService.Registrar(BitacoraService.RENOVACION, "prestamo",
            $"{Db.Texto(cabecera, "codigo")} · nuevo vencimiento {Formato.Fecha(vencimiento)}")
        Return Nothing
    End Function

    ' ======================= CANCELAR =======================

    ''' <summary>Anula un préstamo registrado por error y devuelve sus ejemplares
    ''' a la estantería. No sirve para préstamos con devoluciones parciales: eso
    ''' ya es historia real y se cierra devolviendo, no cancelando.</summary>
    Public Shared Function Cancelar(idPrestamo As Integer, motivo As String) As String
        Dim cabecera = Obtener(idPrestamo)
        If cabecera Is Nothing Then Return "No se encontró el préstamo."
        If Db.Texto(cabecera, "estado") <> "Activo" Then Return "Este préstamo ya no está activo."
        If Db.Numero(cabecera, "devueltos") > 0 Then
            Return "Este préstamo ya tiene ejemplares devueltos: ciérralo con una devolución, no cancelándolo."
        End If
        If String.IsNullOrWhiteSpace(motivo) Then Return "Escribe el motivo de la cancelación."

        Dim codigo = Db.Texto(cabecera, "codigo")

        Db.EnTransaccion(
            Sub(cn, tx)
                ' Las copias vuelven a estar disponibles
                Db.EjecutarEn(cn, tx,
                    "UPDATE e SET e.estado = 'Disponible'
                       FROM ejemplar e
                       JOIN detalle_prestamo d ON d.idejemplar = e.idejemplar
                      WHERE d.idprestamo = @p AND d.fecha_devolucion IS NULL",
                    New SqlParameter("@p", idPrestamo))

                ' Los renglones se borran: el préstamo no llegó a existir de verdad
                Db.EjecutarEn(cn, tx, "DELETE FROM detalle_prestamo WHERE idprestamo = @p",
                              New SqlParameter("@p", idPrestamo))

                Db.EjecutarEn(cn, tx,
                    "UPDATE prestamo SET estado = 'Cancelado', fecha_devolucion = GETDATE(),
                            observacion = @o
                     WHERE idprestamo = @p",
                    New SqlParameter("@p", idPrestamo),
                    New SqlParameter("@o", $"Cancelado por {Sesion.NombreUsuario}: {motivo.Trim()}"))
            End Sub)

        BitacoraService.Registrar(BitacoraService.EDITAR, "prestamo",
                                  $"{codigo} cancelado · {motivo.Trim()}")
        Return Nothing
    End Function
End Class
