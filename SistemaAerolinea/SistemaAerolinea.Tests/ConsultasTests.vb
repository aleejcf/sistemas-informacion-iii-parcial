Imports Xunit

''' <summary>Prueba de humo del acceso a datos: ejecuta todas las consultas de solo
''' lectura del sistema contra la base real. Un nombre de columna mal escrito no
''' rompe la compilación —revienta al abrir la pantalla—, así que aquí se caza.
'''
''' Ninguna de estas llamadas modifica datos. Si SQL Server no está disponible, las
''' pruebas se dan por buenas en vez de fallar: en un servidor de integración
''' continua no hay base de datos que consultar.</summary>
Public Class ConsultasTests

    Private Shared ReadOnly HayBaseDeDatos As Boolean = Db.HayConexion()

    Private Shared Sub Ejecutar(nombre As String, consulta As Action, fallos As List(Of String))
        Try
            consulta()
        Catch ex As Exception
            fallos.Add($"{nombre}: {ex.GetType().Name} — {ex.Message}")
        End Try
    End Sub

    <Fact>
    Public Sub TodasLasConsultasDeLecturaSeEjecutan()
        If Not HayBaseDeDatos Then Return

        Dim fallos As New List(Of String)()

        ' --- Catálogos ---
        Ejecutar("CatalogoService.PaisesParaCombo", Sub() CatalogoService.PaisesParaCombo(), fallos)
        Ejecutar("CatalogoService.AeropuertosParaCombo", Sub() CatalogoService.AeropuertosParaCombo(), fallos)
        Ejecutar("CatalogoService.AerolineasParaCombo", Sub() CatalogoService.AerolineasParaCombo(), fallos)
        Ejecutar("CatalogoService.AvionesParaCombo", Sub() CatalogoService.AvionesParaCombo(), fallos)
        Ejecutar("CatalogoService.MetodosPagoParaCombo", Sub() CatalogoService.MetodosPagoParaCombo(), fallos)
        Ejecutar("CatalogoService.ListarPaises", Sub() CatalogoService.ListarPaises("hon"), fallos)
        Ejecutar("CatalogoService.ListarAeropuertos", Sub() CatalogoService.ListarAeropuertos("tgu"), fallos)
        Ejecutar("CatalogoService.ListarAerolineas", Sub() CatalogoService.ListarAerolineas(""), fallos)
        Ejecutar("CatalogoService.ListarAviones", Sub() CatalogoService.ListarAviones(""), fallos)
        Ejecutar("CatalogoService.ListarTarifas", Sub() CatalogoService.ListarTarifas(), fallos)
        Ejecutar("CatalogoService.ListarMetodosPago", Sub() CatalogoService.ListarMetodosPago(), fallos)
        Ejecutar("CatalogoService.ExistePais", Sub() CatalogoService.ExistePais("HN01"), fallos)
        Ejecutar("CatalogoService.SiguienteIdAerolinea", Sub() CatalogoService.SiguienteIdAerolinea(), fallos)

        ' --- Vuelos ---
        Ejecutar("VueloService.Listar", Sub() VueloService.Listar("TGU"), fallos)
        Ejecutar("VueloService.Listar con filtros", Sub() VueloService.Listar("", "Programado", Date.Today), fallos)
        Ejecutar("VueloService.Itinerario", Sub() VueloService.Itinerario(Date.Today), fallos)
        Ejecutar("VueloService.ExisteCodigo", Sub() VueloService.ExisteCodigo("XX999"), fallos)

        ' --- Pasajeros ---
        Ejecutar("PasajeroService.Listar", Sub() PasajeroService.Listar("lopez"), fallos)
        Ejecutar("PasajeroService.ParaCombo", Sub() PasajeroService.ParaCombo(""), fallos)
        Ejecutar("PasajeroService.SiguienteCodigo", Sub() PasajeroService.SiguienteCodigo(), fallos)
        Ejecutar("PasajeroService.Historial", Sub() PasajeroService.Historial("P0000001"), fallos)

        ' --- Reservas ---
        Ejecutar("ReservaService.BuscarVuelos", Sub() ReservaService.BuscarVuelos(Nothing, Nothing, Nothing), fallos)
        Ejecutar("ReservaService.Listar", Sub() ReservaService.Listar(""), fallos)
        Ejecutar("ReservaService.Listar por estado", Sub() ReservaService.Listar("", "Confirmada"), fallos)

        ' --- Pagos ---
        Ejecutar("PagoService.Listar", Sub() PagoService.Listar(""), fallos)
        Ejecutar("PagoService.Listar por fechas",
                 Sub() PagoService.Listar("", Date.Today.AddDays(-30), Date.Today), fallos)
        Ejecutar("PagoService.ReservasConSaldo", Sub() PagoService.ReservasConSaldo(), fallos)
        Ejecutar("PagoService.ResumenDelDia", Sub() PagoService.ResumenDelDia(), fallos)

        ' --- Panel de control ---
        Ejecutar("PanelService.Estadisticas", Sub() PanelService.Estadisticas(), fallos)
        Ejecutar("PanelService.Ingresos", Sub() PanelService.Ingresos(7), fallos)
        Ejecutar("PanelService.RutasTop", Sub() PanelService.RutasTop(5), fallos)
        Ejecutar("PanelService.ProximasSalidas", Sub() PanelService.ProximasSalidas(8), fallos)
        Ejecutar("PanelService.UltimasReservas", Sub() PanelService.UltimasReservas(8), fallos)

        ' --- Cuentas y auditoría ---
        Ejecutar("UsuarioService.Listar", Sub() UsuarioService.Listar(""), fallos)
        Ejecutar("AuthService.HayUsuarios", Sub() AuthService.HayUsuarios(), fallos)
        Ejecutar("AuthService.ObtenerPregunta", Sub() AuthService.ObtenerPregunta("inexistente"), fallos)
        Ejecutar("BitacoraService.Listar", Sub() BitacoraService.Listar(), fallos)
        Ejecutar("BitacoraService.Acciones", Sub() BitacoraService.Acciones(), fallos)
        Ejecutar("BitacoraService.Usuarios", Sub() BitacoraService.Usuarios(), fallos)

        Assert.True(fallos.Count = 0,
                    "Consultas que fallaron:" & Environment.NewLine &
                    String.Join(Environment.NewLine, fallos))
    End Sub

    ''' <summary>El mapa de asientos es la consulta que más columnas mueve y la que
    ''' sostiene la venta: se comprueba contra datos reales.</summary>
    <Fact>
    Public Sub ElMapaDeAsientosEsCoherenteConLaDisponibilidadDelVuelo()
        If Not HayBaseDeDatos Then Return

        Dim vuelos = ReservaService.BuscarVuelos()
        If vuelos.Rows.Count = 0 Then Return

        Dim idVuelo = CInt(vuelos.Rows(0)("idvuelo"))

        Dim vuelo = ReservaService.ObtenerVuelo(idVuelo)
        Assert.NotNull(vuelo)
        Assert.False(String.IsNullOrWhiteSpace(vuelo.CodigoVuelo))
        Assert.True(vuelo.FechaLlegada > vuelo.FechaSalida, "La llegada debe ser posterior a la salida.")
        Assert.True(vuelo.AsientosPorFila >= 4, "Toda aeronave tiene al menos 4 asientos por fila.")

        Dim mapa = ReservaService.MapaAsientos(idVuelo)
        Assert.NotEmpty(mapa)
        Assert.All(mapa, Sub(a)
                             Assert.False(String.IsNullOrWhiteSpace(a.Etiqueta))
                             Assert.True(a.Precio > 0, $"El asiento {a.Etiqueta} salió sin precio.")
                             Assert.Equal(a.Precio + a.Impuesto, a.Total)
                         End Sub)

        ' Los asientos libres del mapa tienen que cuadrar con lo que dice la vista del vuelo
        Dim libres = mapa.Where(Function(a) Not a.Ocupado).Count()
        Assert.Equal(vuelo.AsientosDisponibles, libres)

        Assert.NotNull(ReservaService.Manifiesto(idVuelo))
    End Sub

    <Fact>
    Public Sub ElTotalDeUnaReservaEsLaSumaDeSusBoletos()
        If Not HayBaseDeDatos Then Return

        Dim reservas = ReservaService.Listar("")
        If reservas.Rows.Count = 0 Then Return

        Dim idReserva = CInt(reservas.Rows(0)("idreserva"))

        Dim resumen = ReservaService.ObtenerPorId(idReserva)
        Assert.NotNull(resumen)
        Assert.Equal(6, resumen("codigo_reserva").ToString().Trim().Length)

        Dim boletos = ReservaService.Boletos(idReserva)
        Assert.True(boletos.Rows.Count > 0, "Una reserva sin boletos no debería existir.")

        Dim suma As Decimal = 0
        For Each fila As Data.DataRow In boletos.Select("estado <> 'Cancelado'")
            suma += CDec(fila("total"))
        Next
        Assert.Equal(CDec(resumen("costo")), suma)
    End Sub
End Class
