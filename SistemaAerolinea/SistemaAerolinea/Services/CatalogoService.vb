Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Los catálogos del negocio: países, aeropuertos, aerolíneas, aviones,
''' tarifas y métodos de pago. Se agrupan en un solo servicio porque comparten la
''' misma forma (listar, guardar, eliminar) y siempre se consultan juntos para
''' llenar las listas desplegables del resto del sistema.</summary>
Public Class CatalogoService

    ' ================================================================
    '  LISTAS PARA COMBOS  (columna `etiqueta` = lo que ve el usuario)
    ' ================================================================

    Public Shared Function PaisesParaCombo() As DataTable
        Return Db.Consultar("SELECT idpais, nombre_pais AS etiqueta FROM pais ORDER BY nombre_pais")
    End Function

    Public Shared Function AeropuertosParaCombo() As DataTable
        Return Db.Consultar("SELECT idaeropuerto,
                                    iata + ' · ' + ciudad + ' (' + nombre + ')' AS etiqueta
                             FROM aeropuerto ORDER BY ciudad")
    End Function

    Public Shared Function AerolineasParaCombo() As DataTable
        Return Db.Consultar("SELECT idaerolinea, codigo + ' · ' + nombre_aero AS etiqueta
                             FROM aerolinea ORDER BY nombre_aero")
    End Function

    Public Shared Function AvionesParaCombo() As DataTable
        Return Db.Consultar("SELECT a.idavion,
                                    a.idavion + ' · ' + a.tipo + ' (' +
                                    CAST(a.capacidad_pasajeros AS VARCHAR(4)) + ' asientos) — ' +
                                    al.nombre_aero AS etiqueta,
                                    a.idaerolinea
                             FROM avion a
                             JOIN aerolinea al ON al.idaerolinea = a.idaerolinea
                             ORDER BY a.idavion")
    End Function

    Public Shared Function MetodosPagoParaCombo() As DataTable
        Return Db.Consultar("SELECT idmetodopago, nombre AS etiqueta FROM metodo_pago ORDER BY idmetodopago")
    End Function

    ' ================================================================
    '  PAÍSES
    ' ================================================================

    Public Shared Function ListarPaises(Optional filtro As String = "") As DataTable
        Return Db.Consultar("SELECT p.idpais, p.nombre_pais,
                                    (SELECT COUNT(*) FROM aeropuerto a WHERE a.idpais = p.idpais) AS aeropuertos,
                                    (SELECT COUNT(*) FROM pasajero x WHERE x.idpais = p.idpais) AS pasajeros
                             FROM pais p
                             WHERE @f = '' OR p.nombre_pais LIKE @like OR p.idpais LIKE @like
                             ORDER BY p.nombre_pais",
                            New SqlParameter("@f", filtro.Trim()),
                            New SqlParameter("@like", "%" & filtro.Trim() & "%"))
    End Function

    Public Shared Function ExistePais(idPais As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM pais WHERE idpais = @i",
                         New SqlParameter("@i", idPais.Trim().ToUpper())) > 0
    End Function

    Public Shared Sub GuardarPais(idPais As String, nombre As String, editando As Boolean)
        If editando Then
            Db.Ejecutar("UPDATE pais SET nombre_pais = @n WHERE idpais = @i",
                        New SqlParameter("@i", idPais.Trim().ToUpper()),
                        New SqlParameter("@n", nombre.Trim()))
        Else
            Db.Ejecutar("INSERT INTO pais (idpais, nombre_pais) VALUES (@i, @n)",
                        New SqlParameter("@i", idPais.Trim().ToUpper()),
                        New SqlParameter("@n", nombre.Trim()))
        End If
        BitacoraService.Registrar(If(editando, BitacoraService.EDITAR, BitacoraService.CREAR),
                                  "pais", $"{idPais.Trim().ToUpper()} · {nombre.Trim()}")
    End Sub

    Public Shared Sub EliminarPais(idPais As String)
        Db.Ejecutar("DELETE FROM pais WHERE idpais = @i",
                    New SqlParameter("@i", idPais.Trim().ToUpper()))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "pais", idPais.Trim().ToUpper())
    End Sub

    ' ================================================================
    '  AEROPUERTOS
    ' ================================================================

    Public Shared Function ListarAeropuertos(Optional filtro As String = "") As DataTable
        Return Db.Consultar("SELECT a.idaeropuerto, a.iata, a.nombre, a.ciudad, a.idpais,
                                    p.nombre_pais,
                                    (SELECT COUNT(*) FROM vuelo v
                                      WHERE v.idaeropuerto_origen = a.idaeropuerto
                                         OR v.idaeropuerto_destino = a.idaeropuerto) AS vuelos
                             FROM aeropuerto a
                             JOIN pais p ON p.idpais = a.idpais
                             WHERE @f = '' OR a.nombre LIKE @like OR a.ciudad LIKE @like
                                OR a.iata LIKE @like OR a.idaeropuerto LIKE @like
                             ORDER BY a.idaeropuerto",
                            New SqlParameter("@f", filtro.Trim()),
                            New SqlParameter("@like", "%" & filtro.Trim() & "%"))
    End Function

    Public Shared Function ExisteAeropuerto(id As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM aeropuerto WHERE idaeropuerto = @i",
                         New SqlParameter("@i", id.Trim().ToUpper())) > 0
    End Function

    Public Shared Sub GuardarAeropuerto(id As String, nombre As String, ciudad As String,
                                        iata As String, idPais As String, editando As Boolean)
        If editando Then
            Db.Ejecutar("UPDATE aeropuerto SET nombre = @n, ciudad = @c, iata = @t, idpais = @p
                         WHERE idaeropuerto = @i",
                        New SqlParameter("@i", id.Trim().ToUpper()),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@c", ciudad.Trim()),
                        New SqlParameter("@t", iata.Trim().ToUpper()),
                        New SqlParameter("@p", idPais))
        Else
            Db.Ejecutar("INSERT INTO aeropuerto (idaeropuerto, nombre, ciudad, iata, idpais)
                         VALUES (@i, @n, @c, @t, @p)",
                        New SqlParameter("@i", id.Trim().ToUpper()),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@c", ciudad.Trim()),
                        New SqlParameter("@t", iata.Trim().ToUpper()),
                        New SqlParameter("@p", idPais))
        End If
        BitacoraService.Registrar(If(editando, BitacoraService.EDITAR, BitacoraService.CREAR),
                                  "aeropuerto", $"{iata.Trim().ToUpper()} · {nombre.Trim()}")
    End Sub

    Public Shared Sub EliminarAeropuerto(id As String)
        Db.Ejecutar("DELETE FROM aeropuerto WHERE idaeropuerto = @i",
                    New SqlParameter("@i", id.Trim().ToUpper()))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "aeropuerto", id.Trim().ToUpper())
    End Sub

    ' ================================================================
    '  AEROLÍNEAS
    ' ================================================================

    Public Shared Function ListarAerolineas(Optional filtro As String = "") As DataTable
        Return Db.Consultar("SELECT al.idaerolinea, al.codigo, al.nombre_aero, al.rtn,
                                    (SELECT COUNT(*) FROM avion av WHERE av.idaerolinea = al.idaerolinea) AS aviones,
                                    (SELECT COUNT(*) FROM vuelo v WHERE v.idaerolinea = al.idaerolinea) AS vuelos
                             FROM aerolinea al
                             WHERE @f = '' OR al.nombre_aero LIKE @like OR al.rtn LIKE @like
                                OR al.codigo LIKE @like
                             ORDER BY al.nombre_aero",
                            New SqlParameter("@f", filtro.Trim()),
                            New SqlParameter("@like", "%" & filtro.Trim() & "%"))
    End Function

    Public Shared Function ExisteAerolinea(id As Integer) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM aerolinea WHERE idaerolinea = @i",
                         New SqlParameter("@i", id)) > 0
    End Function

    Public Shared Function SiguienteIdAerolinea() As Integer
        Return Db.Contar("SELECT ISNULL(MAX(idaerolinea), 0) + 1 FROM aerolinea")
    End Function

    Public Shared Sub GuardarAerolinea(id As Integer, codigo As String, nombre As String,
                                       rtn As String, editando As Boolean)
        If editando Then
            Db.Ejecutar("UPDATE aerolinea SET codigo = @c, nombre_aero = @n, rtn = @r
                         WHERE idaerolinea = @i",
                        New SqlParameter("@i", id),
                        New SqlParameter("@c", codigo.Trim().ToUpper()),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@r", rtn.Trim()))
        Else
            Db.Ejecutar("INSERT INTO aerolinea (idaerolinea, codigo, nombre_aero, rtn)
                         VALUES (@i, @c, @n, @r)",
                        New SqlParameter("@i", id),
                        New SqlParameter("@c", codigo.Trim().ToUpper()),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@r", rtn.Trim()))
        End If
        BitacoraService.Registrar(If(editando, BitacoraService.EDITAR, BitacoraService.CREAR),
                                  "aerolinea", $"{codigo.Trim().ToUpper()} · {nombre.Trim()}")
    End Sub

    Public Shared Sub EliminarAerolinea(id As Integer)
        Db.Ejecutar("DELETE FROM aerolinea WHERE idaerolinea = @i", New SqlParameter("@i", id))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "aerolinea", id.ToString())
    End Sub

    ' ================================================================
    '  AVIONES
    ' ================================================================

    Public Shared Function ListarAviones(Optional filtro As String = "") As DataTable
        Return Db.Consultar("SELECT av.idavion, av.fabricante, av.tipo, av.capacidad_pasajeros,
                                    av.asientos_por_fila, av.idaerolinea, al.nombre_aero,
                                    (SELECT COUNT(*) FROM asiento s WHERE s.idavion = av.idavion) AS asientos,
                                    (SELECT COUNT(*) FROM vuelo v WHERE v.idavion = av.idavion) AS vuelos
                             FROM avion av
                             JOIN aerolinea al ON al.idaerolinea = av.idaerolinea
                             WHERE @f = '' OR av.idavion LIKE @like OR av.tipo LIKE @like
                                OR av.fabricante LIKE @like OR al.nombre_aero LIKE @like
                             ORDER BY av.idavion",
                            New SqlParameter("@f", filtro.Trim()),
                            New SqlParameter("@like", "%" & filtro.Trim() & "%"))
    End Function

    Public Shared Function ExisteAvion(id As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM avion WHERE idavion = @i",
                         New SqlParameter("@i", id.Trim().ToUpper())) > 0
    End Function

    ''' <summary>Da de alta un avión y le genera su mapa de asientos completo.
    ''' Un avión sin asientos no se le podría vender a nadie, así que las dos cosas
    ''' van juntas dentro de la misma transacción.</summary>
    Public Shared Sub CrearAvion(id As String, idAerolinea As Integer, fabricante As String,
                                 tipo As String, capacidad As Integer, asientosPorFila As Integer)
        Dim codigo = id.Trim().ToUpper()

        Db.EnTransaccion(
            Sub(cn, tx)
                Db.EjecutarEn(cn, tx,
                    "INSERT INTO avion (idavion, idaerolinea, fabricante, tipo,
                                        capacidad_pasajeros, asientos_por_fila)
                     VALUES (@i, @al, @f, @t, @c, @apf)",
                    New SqlParameter("@i", codigo),
                    New SqlParameter("@al", idAerolinea),
                    New SqlParameter("@f", Db.Opcional(fabricante)),
                    New SqlParameter("@t", tipo.Trim()),
                    New SqlParameter("@c", capacidad),
                    New SqlParameter("@apf", asientosPorFila))

                GenerarAsientos(cn, tx, codigo, capacidad, asientosPorFila)
            End Sub)

        BitacoraService.Registrar(BitacoraService.CREAR, "avion",
                                  $"{codigo} · {tipo.Trim()} · {capacidad} asientos")
    End Sub

    ''' <summary>Genera el mapa de asientos: filas numeradas y columnas A, B, C…
    ''' La primera fila (y la segunda en aviones grandes) es Primera Clase, las
    ''' siguientes Ejecutiva y el resto Económica.</summary>
    Private Shared Sub GenerarAsientos(cn As SqlConnection, tx As SqlTransaction,
                                       idAvion As String, capacidad As Integer, porFila As Integer)
        Const LETRAS As String = "ABCDEFGH"
        Dim filasPrimera = If(capacidad >= 150, 2, 0)
        Dim filasEjecutiva = If(capacidad >= 150, 5, 2)

        For n = 1 To capacidad
            Dim fila = ((n - 1) \ porFila) + 1
            Dim letra = LETRAS((n - 1) Mod porFila)

            Dim clase As String
            If fila <= filasPrimera Then
                clase = "Primera Clase"
            ElseIf fila <= filasEjecutiva Then
                clase = "Ejecutiva"
            Else
                clase = "Económica"
            End If

            Db.EjecutarEn(cn, tx,
                "INSERT INTO asiento (idavion, fila, letra, clase) VALUES (@a, @f, @l, @c)",
                New SqlParameter("@a", idAvion),
                New SqlParameter("@f", fila),
                New SqlParameter("@l", letra.ToString()),
                New SqlParameter("@c", clase))
        Next
    End Sub

    ''' <summary>Editar un avión no toca su mapa de asientos: cambiar la capacidad
    ''' de una aeronave con boletos vendidos dejaría pasajeros sin asiento. Por eso
    ''' la capacidad solo se puede fijar al crearlo.</summary>
    Public Shared Sub ActualizarAvion(id As String, idAerolinea As Integer,
                                      fabricante As String, tipo As String)
        Db.Ejecutar("UPDATE avion SET idaerolinea = @al, fabricante = @f, tipo = @t
                     WHERE idavion = @i",
                    New SqlParameter("@i", id.Trim().ToUpper()),
                    New SqlParameter("@al", idAerolinea),
                    New SqlParameter("@f", Db.Opcional(fabricante)),
                    New SqlParameter("@t", tipo.Trim()))
        BitacoraService.Registrar(BitacoraService.EDITAR, "avion", id.Trim().ToUpper())
    End Sub

    Public Shared Sub EliminarAvion(id As String)
        Dim codigo = id.Trim().ToUpper()
        Db.EnTransaccion(
            Sub(cn, tx)
                Db.EjecutarEn(cn, tx, "DELETE FROM asiento WHERE idavion = @i",
                              New SqlParameter("@i", codigo))
                Db.EjecutarEn(cn, tx, "DELETE FROM avion WHERE idavion = @i",
                              New SqlParameter("@i", codigo))
            End Sub)
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "avion", codigo)
    End Sub

    ' ================================================================
    '  TARIFAS
    ' ================================================================

    Public Shared Function ListarTarifas() As DataTable
        Return Db.Consultar("SELECT t.idtarifa, t.clase, t.multiplicador, t.impuesto,
                                    t.equipaje_incluido_kg,
                                    CAST(t.impuesto * 100 AS DECIMAL(5,2)) AS impuesto_pct,
                                    (SELECT COUNT(*) FROM asiento a WHERE a.clase = t.clase) AS asientos,
                                    (SELECT COUNT(*) FROM boleto b WHERE b.idtarifa = t.idtarifa) AS boletos
                             FROM tarifa t ORDER BY t.multiplicador")
    End Function

    ''' <summary>Solo se pueden ajustar el multiplicador, el impuesto y el equipaje:
    ''' el nombre de la clase está enlazado con la clase de cada asiento.</summary>
    Public Shared Sub ActualizarTarifa(idTarifa As Integer, multiplicador As Decimal,
                                       impuesto As Decimal, equipajeKg As Integer)
        Db.Ejecutar("UPDATE tarifa SET multiplicador = @m, impuesto = @i, equipaje_incluido_kg = @e
                     WHERE idtarifa = @t",
                    New SqlParameter("@t", idTarifa),
                    New SqlParameter("@m", multiplicador),
                    New SqlParameter("@i", impuesto),
                    New SqlParameter("@e", equipajeKg))
        BitacoraService.Registrar(BitacoraService.EDITAR, "tarifa",
                                  $"Tarifa {idTarifa}: x{multiplicador}, impuesto {impuesto:P0}")
    End Sub

    ' ================================================================
    '  MÉTODOS DE PAGO
    ' ================================================================

    Public Shared Function ListarMetodosPago() As DataTable
        Return Db.Consultar("SELECT m.idmetodopago, m.nombre,
                                    (SELECT COUNT(*) FROM pago p WHERE p.idmetodopago = m.idmetodopago) AS pagos,
                                    (SELECT ISNULL(SUM(p.monto), 0) FROM pago p
                                      WHERE p.idmetodopago = m.idmetodopago) AS recaudado
                             FROM metodo_pago m ORDER BY m.idmetodopago")
    End Function

    Public Shared Sub GuardarMetodoPago(id As Integer, nombre As String, editando As Boolean)
        If editando Then
            Db.Ejecutar("UPDATE metodo_pago SET nombre = @n WHERE idmetodopago = @i",
                        New SqlParameter("@i", id),
                        New SqlParameter("@n", nombre.Trim()))
        Else
            Db.Ejecutar("INSERT INTO metodo_pago (idmetodopago, nombre)
                         VALUES ((SELECT ISNULL(MAX(idmetodopago), 0) + 1 FROM metodo_pago), @n)",
                        New SqlParameter("@n", nombre.Trim()))
        End If
        BitacoraService.Registrar(If(editando, BitacoraService.EDITAR, BitacoraService.CREAR),
                                  "metodo_pago", nombre.Trim())
    End Sub

    Public Shared Sub EliminarMetodoPago(id As Integer)
        Db.Ejecutar("DELETE FROM metodo_pago WHERE idmetodopago = @i", New SqlParameter("@i", id))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "metodo_pago", id.ToString())
    End Sub
End Class
