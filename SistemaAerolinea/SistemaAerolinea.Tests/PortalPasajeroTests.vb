Imports System.Data
Imports Xunit

''' <summary>Pruebas del portal del pasajero. Lo que se comprueba aquí no es que
''' la pantalla se vea bien, sino que un pasajero NO pueda alcanzar los datos de
''' otro: el aislamiento vive en los servicios, no en la interfaz, precisamente
''' para que ninguna pantalla pueda saltárselo por descuido.</summary>
Public Class PortalPasajeroTests
    Implements IDisposable

    Private Shared ReadOnly HayBaseDeDatos As Boolean = Db.HayConexion()

    ''' <summary>Sesion.UsuarioActual es estático: cada prueba lo deja como estaba.</summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Sesion.Cerrar()
    End Sub

    Private Shared Function CuentaDe(rol As String, Optional idPasajero As String = Nothing) As Usuario
        Return New Usuario With {
            .UsuarioID = 999,
            .NombreCompleto = "Cuenta de prueba",
            .NombreUsuario = "prueba",
            .Email = "prueba@alas.hn",
            .Rol = rol,
            .IdPasajero = idPasajero
        }
    End Function

    ' ================================================================
    '  PERMISOS  (sin base de datos)
    ' ================================================================

    <Fact>
    Public Sub UnAdministradorPuedeTodo()
        Sesion.UsuarioActual = CuentaDe("Administrador")

        Assert.True(Permisos.EsPersonal)
        Assert.False(Permisos.EsPasajero)
        Assert.True(Permisos.PuedeEliminar)
        Assert.True(Permisos.PuedeGestionarUsuarios)
        Assert.True(Permisos.PuedeVerBitacora)
        Assert.True(Permisos.PuedeEditarCatalogos)
        Assert.True(Permisos.PuedeCancelarReservas)
        Assert.True(Permisos.PuedeReservarParaCualquiera)
    End Sub

    <Fact>
    Public Sub UnAgenteVendePeroNoAdministra()
        Sesion.UsuarioActual = CuentaDe("Agente")

        Assert.True(Permisos.EsPersonal)
        Assert.True(Permisos.PuedeReservarParaCualquiera)
        Assert.False(Permisos.PuedeEliminar)
        Assert.False(Permisos.PuedeGestionarUsuarios)
        Assert.False(Permisos.PuedeVerBitacora)
        Assert.False(Permisos.PuedeEditarCatalogos)
        Assert.False(Permisos.PuedeCancelarReservas)
    End Sub

    <Fact>
    Public Sub UnPasajeroNoAlcanzaNadaDeLaOperacion()
        Sesion.UsuarioActual = CuentaDe("Pasajero", "P0000001")

        Assert.True(Permisos.EsPasajero)
        Assert.False(Permisos.EsPersonal)
        Assert.False(Permisos.PuedeEliminar)
        Assert.False(Permisos.PuedeGestionarUsuarios)
        Assert.False(Permisos.PuedeVerBitacora)
        Assert.False(Permisos.PuedeEditarCatalogos)
        Assert.False(Permisos.PuedeCancelarReservas)
        Assert.False(Permisos.PuedeReservarParaCualquiera)

        ' Pero sí puede hacer lo suyo
        Assert.True(Permisos.PuedeHacerCheckIn)
        Assert.Equal("P0000001", Sesion.IdPasajero)
    End Sub

    <Fact>
    Public Sub SinSesionNadieTienePermisos()
        Sesion.Cerrar()

        Assert.False(Permisos.EsPersonal)
        Assert.False(Permisos.EsPasajero)
        Assert.False(Permisos.PuedeEliminar)
        Assert.False(Permisos.PuedeHacerCheckIn)
        Assert.Null(Sesion.IdPasajero)
    End Sub

    ''' <summary>El permiso lo comprueba el propio servicio, antes de tocar la base:
    ''' esconder el botón en la pantalla no basta, porque la pantalla se puede
    ''' equivocar y el servicio es el que mueve el dinero.</summary>
    <Fact>
    Public Sub CancelarUnaReservaExigeSerAdministrador()
        Sesion.UsuarioActual = CuentaDe("Agente")
        Assert.Contains("Administrador", ReservaService.Cancelar(1, "prueba"))

        Sesion.UsuarioActual = CuentaDe("Pasajero", "P0000001")
        Assert.Contains("Administrador", ReservaService.Cancelar(1, "prueba"))

        Sesion.Cerrar()
        Assert.Contains("Administrador", ReservaService.Cancelar(1, "prueba"))
    End Sub

    <Fact>
    Public Sub ElPersonalNoTieneFichaDeViajero()
        Sesion.UsuarioActual = CuentaDe("Agente")
        ' Aunque el objeto trajera un idpasajero por error, Sesion no lo expone
        Sesion.UsuarioActual.IdPasajero = "P0000009"
        Assert.Null(Sesion.IdPasajero)
    End Sub

    ' ================================================================
    '  AISLAMIENTO CONTRA LA BASE DE DATOS
    ' ================================================================

    ''' <summary>Busca un pasajero de la semilla que sí tenga reservas.</summary>
    Private Shared Function PasajeroConReservas() As String
        Sesion.Cerrar()
        Dim reservas = ReservaService.Listar()
        If reservas.Rows.Count = 0 Then Return Nothing
        Return reservas.Rows(0)("idpasajero").ToString()
    End Function

    <Fact>
    Public Sub UnPasajeroSoloVeSusPropiasReservas()
        If Not HayBaseDeDatos Then Return

        Dim idPasajero = PasajeroConReservas()
        If idPasajero Is Nothing Then Return

        ' Cuántas hay en total, visto por el personal
        Sesion.UsuarioActual = CuentaDe("Administrador")
        Dim todas = ReservaService.Listar().Rows.Count

        ' Y cuántas ve el pasajero
        Sesion.UsuarioActual = CuentaDe("Pasajero", idPasajero)
        Dim suyas = ReservaService.Listar()

        Assert.True(suyas.DefaultView.Count > 0, "El pasajero de prueba debería tener reservas.")
        Assert.True(suyas.DefaultView.Count < todas,
                    "El filtro no se aplicó: el pasajero está viendo todas las reservas del sistema.")

        ' Y todas las que ve son realmente suyas
        For Each fila As DataRowView In suyas.DefaultView
            Assert.True(ReservaService.EsDelPasajeroActual(CInt(fila("idreserva"))),
                        $"Se coló la reserva {fila("codigo_reserva")}, que no le pertenece.")
        Next
    End Sub

    <Fact>
    Public Sub UnPasajeroNoPuedeAbrirLaReservaDeOtro()
        If Not HayBaseDeDatos Then Return

        Sesion.Cerrar()
        Dim reservas = ReservaService.Listar()
        If reservas.Rows.Count < 2 Then Return

        Dim primera = reservas.Rows(0)
        ' Una reserva de OTRA persona
        Dim ajena As DataRow = Nothing
        For Each fila As DataRow In reservas.Rows
            If fila("idpasajero").ToString() <> primera("idpasajero").ToString() Then
                ajena = fila
                Exit For
            End If
        Next
        If ajena Is Nothing Then Return

        Sesion.UsuarioActual = CuentaDe("Pasajero", primera("idpasajero").ToString())
        Dim idAjena = CInt(ajena("idreserva"))

        Assert.False(ReservaService.EsDelPasajeroActual(idAjena))
        Assert.Null(ReservaService.ObtenerPorId(idAjena))
        Assert.Empty(ReservaService.Boletos(idAjena).Rows)
        Assert.Null(ReservaService.ObtenerPorPnr(ajena("codigo_reserva").ToString()))
    End Sub

    ''' <summary>Un idboleto es un número correlativo: pedir el de al lado no puede
    ''' devolver el asiento y el vuelo de un desconocido.</summary>
    <Fact>
    Public Sub UnPasajeroNoPuedeAbrirElBoletoDeOtro()
        If Not HayBaseDeDatos Then Return

        Sesion.Cerrar()
        Dim reservas = ReservaService.Listar()
        If reservas.Rows.Count = 0 Then Return

        Dim ajena = reservas.Rows(0)
        Dim boletos = ReservaService.Boletos(CInt(ajena("idreserva")))
        If boletos.Rows.Count = 0 Then Return

        Dim idBoleto = CInt(boletos.Rows(0)("idboleto"))

        ' Una persona distinta a la dueña de esa reserva
        Dim otro = If(ajena("idpasajero").ToString().Trim() = "P0000020", "P0000001", "P0000020")

        Sesion.UsuarioActual = CuentaDe("Pasajero", otro)
        Assert.Null(ReservaService.Boleto(idBoleto))

        ' Y el personal sí lo ve, para que la prueba no pase por un boleto inexistente
        Sesion.UsuarioActual = CuentaDe("Administrador")
        Assert.NotNull(ReservaService.Boleto(idBoleto))
    End Sub

    ''' <summary>El buscador del portal filtra en memoria. Los corchetes y los
    ''' comodines son sintaxis para RowFilter, así que buscarlos hacía reventar la
    ''' pantalla en vez de no encontrar nada.</summary>
    <Fact>
    Public Sub BuscarCaracteresRarosNoRevientaElPortal()
        If Not HayBaseDeDatos Then Return

        Dim idPasajero = PasajeroConReservas()
        If idPasajero Is Nothing Then Return

        Sesion.UsuarioActual = CuentaDe("Pasajero", idPasajero)

        ' Ninguno de estos puede aparecer en un localizador ni en un nombre, así que
        ' además de no reventar, la búsqueda debe salir vacía
        For Each raro In {"[", "]", "*", "%", "[0-9]", "[a-z]*"}
            Dim encontradas = ReservaService.Listar(raro)
            Assert.NotNull(encontradas)
            Assert.Empty(encontradas.Rows)
        Next

        ' Y una búsqueda normal sí encuentra
        Dim mias = ReservaService.Listar()
        Assert.NotEmpty(mias.Rows)
        Assert.NotEmpty(ReservaService.Listar(mias.Rows(0)("codigo_reserva").ToString()).Rows)
    End Sub

    <Fact>
    Public Sub UnPasajeroNoPuedeHacerCheckInDeUnBoletoAjeno()
        If Not HayBaseDeDatos Then Return

        Sesion.Cerrar()
        Dim reservas = ReservaService.Listar()
        If reservas.Rows.Count < 2 Then Return

        Dim ajena = reservas.Rows(0)
        Dim boletos = ReservaService.Boletos(CInt(ajena("idreserva")))
        If boletos.Rows.Count = 0 Then Return

        ' Una persona distinta a la dueña de esa reserva
        Dim otro = "P0000020"
        If ajena("idpasajero").ToString() = otro Then otro = "P0000001"

        Sesion.UsuarioActual = CuentaDe("Pasajero", otro)
        Dim problema = ReservaService.HacerCheckIn(CInt(boletos.Rows(0)("idboleto")))

        Assert.NotNull(problema)
        Assert.Contains("no pertenece", problema)
    End Sub

    <Fact>
    Public Sub UnPasajeroSoloSeVeASiMismoEnLaListaDePersonas()
        If Not HayBaseDeDatos Then Return

        Sesion.UsuarioActual = CuentaDe("Administrador")
        Dim todosLosPasajeros = PasajeroService.ParaCombo().Rows.Count
        Assert.True(todosLosPasajeros > 1, "La semilla debería tener varios pasajeros.")

        ' Devolverle el directorio completo dejaría leer nombres y documentos ajenos
        Sesion.UsuarioActual = CuentaDe("Pasajero", "P0000001")
        Dim visibles = PasajeroService.ParaCombo()

        Assert.Single(visibles.Rows)
        Assert.Equal("P0000001", visibles.Rows(0)("idpasajero").ToString().Trim())
    End Sub

    <Fact>
    Public Sub ElPersonalSigueViendoTodo()
        If Not HayBaseDeDatos Then Return

        Sesion.UsuarioActual = CuentaDe("Agente")
        Assert.True(PasajeroService.ParaCombo().Rows.Count > 1)

        Sesion.Cerrar()
        Dim todas = ReservaService.Listar()
        Sesion.UsuarioActual = CuentaDe("Administrador")
        Assert.Equal(todas.Rows.Count, ReservaService.Listar().Rows.Count)
    End Sub
End Class
