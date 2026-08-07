Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Los socios de la biblioteca: quienes se llevan los libros. Es la
''' tabla que en el II Parcial se llamaba `Usuarios`; se renombró porque ahora
''' `usuario` son las cuentas del personal que opera el sistema.</summary>
Public Class SocioService

    Public Shared Function Listar(Optional filtro As String = "",
                                  Optional idTipo As Integer? = Nothing,
                                  Optional soloActivos As Boolean = False,
                                  Optional soloConDeuda As Boolean = False) As DataTable
        Return Db.Consultar(
            "SELECT * FROM dbo.v_socio_detalle
              WHERE (@f = '' OR nombre_completo LIKE @like OR idsocio LIKE @like
                     OR email LIKE @like OR identidad LIKE @like OR telefono LIKE @like)
                AND (@t IS NULL OR idtipo = @t)
                AND (@a = 0 OR esta_activo = 1)
                AND (@d = 0 OR monto_adeudado > 0 OR prestamos_vencidos > 0)
              ORDER BY apellido, nombre",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"),
            New SqlParameter("@t", If(idTipo.HasValue, CObj(idTipo.Value), DBNull.Value)),
            New SqlParameter("@a", If(soloActivos, 1, 0)),
            New SqlParameter("@d", If(soloConDeuda, 1, 0)))
    End Function

    ''' <summary>Los socios que pueden aparecer en el combo del mostrador.
    ''' Se incluyen también los que no pueden prestar hoy: el bibliotecario tiene
    ''' que poder elegirlos para VER por qué están bloqueados.</summary>
    Public Shared Function ParaCombo() As DataTable
        Return Db.Consultar("SELECT idsocio, etiqueta FROM dbo.v_socio_detalle
                             WHERE esta_activo = 1 ORDER BY apellido, nombre")
    End Function

    Public Shared Function Obtener(idSocio As String) As DataRow
        Return Db.ConsultarFila("SELECT * FROM dbo.v_socio_detalle WHERE idsocio = @s",
                                New SqlParameter("@s", idSocio))
    End Function

    ''' <summary>La ficha del socio convertida en el objeto que usa el mostrador,
    ''' con su solvencia ya resuelta. Devuelve Nothing si el socio no existe.</summary>
    Public Shared Function Resumen(idSocio As String) As SocioResumen
        Dim fila = Obtener(idSocio)
        If fila Is Nothing Then Return Nothing

        Return New SocioResumen With {
            .IdSocio = Db.Texto(fila, "idsocio"),
            .NombreCompleto = Db.Texto(fila, "nombre_completo"),
            .Email = Db.Texto(fila, "email"),
            .Telefono = Db.Texto(fila, "telefono"),
            .TipoSocio = Db.Texto(fila, "tipo_socio"),
            .MaxPrestamos = Db.Numero(fila, "max_prestamos"),
            .DiasPrestamo = Db.Numero(fila, "dias_prestamo"),
            .MultaDiaria = Db.Monto(fila, "multa_diaria"),
            .EjemplaresAfuera = Db.Numero(fila, "ejemplares_afuera"),
            .PrestamosVencidos = Db.Numero(fila, "prestamos_vencidos"),
            .MultasPendientes = Db.Numero(fila, "multas_pendientes"),
            .MontoAdeudado = Db.Monto(fila, "monto_adeudado"),
            .CupoDisponible = Db.Numero(fila, "cupo_disponible"),
            .PuedePrestar = Not IsDBNull(fila("puede_prestar")) AndAlso CBool(fila("puede_prestar")),
            .EstaActivo = Not IsDBNull(fila("esta_activo")) AndAlso CBool(fila("esta_activo"))
        }
    End Function

    ''' <summary>Estado de cuenta completo: ficha, ejemplares afuera y multas
    ''' pendientes, en una sola ida a la base de datos.</summary>
    Public Shared Function EstadoDeCuenta(idSocio As String) As DataSet
        Return Db.ConsultarVarias("EXEC dbo.sp_estado_cuenta_socio @idsocio = @s",
                                  New SqlParameter("@s", idSocio))
    End Function

    ''' <summary>Historial de préstamos del socio, del más reciente al más viejo.</summary>
    Public Shared Function Historial(idSocio As String) As DataTable
        Return Db.Consultar("SELECT * FROM dbo.v_prestamo_detalle WHERE idsocio = @s
                             ORDER BY fecha_prestamo DESC",
                            New SqlParameter("@s", idSocio))
    End Function

    ' ---------- Alta y edición ----------

    ''' <summary>Siguiente código de socio libre (U00021, U00022…), conservando el
    ''' formato del II Parcial.</summary>
    Public Shared Function SugerirCodigo() As String
        Dim siguiente = Db.Contar("SELECT ISNULL(MAX(CAST(SUBSTRING(idsocio, 2, 5) AS INT)), 0) + 1
                                   FROM socio")
        Return "U" & siguiente.ToString("00000")
    End Function

    Public Shared Function ExisteCodigo(idSocio As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM socio WHERE idsocio = @s",
                         New SqlParameter("@s", idSocio.Trim().ToUpper())) > 0
    End Function

    Public Shared Function Crear(idSocio As String, nombre As String, apellido As String,
                                 identidad As String, telefono As String, email As String,
                                 direccion As String, idTipo As Integer) As String

        Dim validacion = Validar(nombre, apellido, identidad, telefono, email)
        If validacion IsNot Nothing Then Return validacion

        If Not Validador.EsIdSocioValido(idSocio) Then
            Return "El código del socio debe ser la letra U seguida de 5 dígitos (U00021)."
        End If
        If ExisteCodigo(idSocio) Then Return "Ya existe un socio con ese código."

        Dim repetidos = ValidarRepetidos(email, identidad, "")
        If repetidos IsNot Nothing Then Return repetidos

        Db.Ejecutar("INSERT INTO socio (idsocio, nombre, apellido, identidad, telefono,
                                        email, direccion, idtipo)
                     VALUES (@s, @n, @a, @i, @t, @e, @d, @ti)",
                    New SqlParameter("@s", idSocio.Trim().ToUpper()),
                    New SqlParameter("@n", nombre.Trim()),
                    New SqlParameter("@a", apellido.Trim()),
                    New SqlParameter("@i", Db.Opcional(LimpiarIdentidad(identidad))),
                    New SqlParameter("@t", Db.Opcional(telefono)),
                    New SqlParameter("@e", email.Trim().ToLower()),
                    New SqlParameter("@d", Db.Opcional(direccion)),
                    New SqlParameter("@ti", idTipo))

        BitacoraService.Registrar(BitacoraService.CREAR, "socio",
                                  $"{idSocio.Trim().ToUpper()} · {nombre.Trim()} {apellido.Trim()}")
        Return Nothing
    End Function

    Public Shared Function Actualizar(idSocio As String, nombre As String, apellido As String,
                                      identidad As String, telefono As String, email As String,
                                      direccion As String, idTipo As Integer,
                                      estaActivo As Boolean) As String

        Dim validacion = Validar(nombre, apellido, identidad, telefono, email)
        If validacion IsNot Nothing Then Return validacion

        Dim repetidos = ValidarRepetidos(email, identidad, idSocio)
        If repetidos IsNot Nothing Then Return repetidos

        ' Inactivar a alguien que todavía tiene libros afuera dejaría esos
        ' ejemplares en el limbo: nadie los reclamaría.
        If Not estaActivo Then
            Dim afuera = Db.Contar("SELECT COUNT(*)
                                    FROM detalle_prestamo d
                                    JOIN prestamo p ON p.idprestamo = d.idprestamo
                                    WHERE p.idsocio = @s AND d.fecha_devolucion IS NULL",
                                   New SqlParameter("@s", idSocio))
            If afuera > 0 Then
                Return $"No se puede inactivar: el socio tiene {afuera} ejemplares sin devolver."
            End If
        End If

        Db.Ejecutar("UPDATE socio SET nombre = @n, apellido = @a, identidad = @i, telefono = @t,
                                      email = @e, direccion = @d, idtipo = @ti, esta_activo = @ac
                     WHERE idsocio = @s",
                    New SqlParameter("@s", idSocio),
                    New SqlParameter("@n", nombre.Trim()),
                    New SqlParameter("@a", apellido.Trim()),
                    New SqlParameter("@i", Db.Opcional(LimpiarIdentidad(identidad))),
                    New SqlParameter("@t", Db.Opcional(telefono)),
                    New SqlParameter("@e", email.Trim().ToLower()),
                    New SqlParameter("@d", Db.Opcional(direccion)),
                    New SqlParameter("@ti", idTipo),
                    New SqlParameter("@ac", estaActivo))

        BitacoraService.Registrar(BitacoraService.EDITAR, "socio",
                                  $"{idSocio} · {nombre.Trim()} {apellido.Trim()}")
        Return Nothing
    End Function

    Private Shared Function Validar(nombre As String, apellido As String, identidad As String,
                                    telefono As String, email As String) As String
        If String.IsNullOrWhiteSpace(nombre) Then Return "Escribe el nombre del socio."
        If String.IsNullOrWhiteSpace(apellido) Then Return "Escribe el apellido del socio."
        Dim problemaEmail = Validador.ProblemaDelEmail(email)
        If problemaEmail IsNot Nothing Then Return problemaEmail
        If Not String.IsNullOrWhiteSpace(identidad) AndAlso Not Validador.EsIdentidadValida(identidad) Then
            Return "El número de identidad debe tener 13 dígitos."
        End If
        If Not Validador.EsTelefonoValido(telefono) Then Return "El teléfono debe tener 8 dígitos."
        Return Nothing
    End Function

    Private Shared Function ValidarRepetidos(email As String, identidad As String,
                                             exceptoSocio As String) As String
        Dim conEmail = Db.Contar("SELECT COUNT(*) FROM socio WHERE email = @e AND idsocio <> @s",
                                 New SqlParameter("@e", email.Trim().ToLower()),
                                 New SqlParameter("@s", If(exceptoSocio, "")))
        If conEmail > 0 Then Return "Ya hay otro socio registrado con ese correo."

        Dim limpia = LimpiarIdentidad(identidad)
        If Not String.IsNullOrWhiteSpace(limpia) Then
            Dim conIdentidad = Db.Contar("SELECT COUNT(*) FROM socio WHERE identidad = @i AND idsocio <> @s",
                                         New SqlParameter("@i", limpia),
                                         New SqlParameter("@s", If(exceptoSocio, "")))
            If conIdentidad > 0 Then Return "Ya hay otro socio registrado con ese número de identidad."
        End If
        Return Nothing
    End Function

    ''' <summary>La identidad se guarda solo con dígitos para que "0501-1990-00101"
    ''' y "0501199000101" no entren como dos personas distintas.</summary>
    Private Shared Function LimpiarIdentidad(identidad As String) As String
        If String.IsNullOrWhiteSpace(identidad) Then Return Nothing
        Return identidad.Trim().Replace("-", "").Replace(" ", "")
    End Function

    ''' <summary>Elimina un socio. Solo si nunca pidió nada prestado: un socio con
    ''' historial se inactiva para no perder el registro de sus préstamos.</summary>
    Public Shared Function Eliminar(idSocio As String) As String
        Dim conPrestamos = Db.Contar("SELECT COUNT(*) FROM prestamo WHERE idsocio = @s",
                                     New SqlParameter("@s", idSocio))
        If conPrestamos > 0 Then
            Return $"Este socio tiene {conPrestamos} préstamos en su historial. " &
                   "Desmárcalo como activo en vez de eliminarlo, así se conserva el registro."
        End If

        Dim reservas = Db.Contar("SELECT COUNT(*) FROM reserva WHERE idsocio = @s",
                                 New SqlParameter("@s", idSocio))
        If reservas > 0 Then Return "Este socio tiene reservas registradas y no se puede eliminar."

        Dim nombre = Db.Escalar("SELECT nombre + ' ' + apellido FROM socio WHERE idsocio = @s",
                                New SqlParameter("@s", idSocio))
        Db.Ejecutar("DELETE FROM socio WHERE idsocio = @s", New SqlParameter("@s", idSocio))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "socio", $"{idSocio} · {If(nombre, "")}")
        Return Nothing
    End Function
End Class
