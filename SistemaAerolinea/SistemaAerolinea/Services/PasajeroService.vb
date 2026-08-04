Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Los pasajeros: quienes viajan. No son cuentas del sistema (eso es
''' la tabla `usuario`), sino los datos de la persona que va en el avión.</summary>
Public Class PasajeroService

    Public Shared ReadOnly TiposDocumento As String() =
        {"DNI", "Pasaporte", "Carné de residencia", "Documento de menor"}

    Public Shared Function Listar(Optional filtro As String = "") As DataTable
        Return Db.Consultar(
            "SELECT p.idpasajero, p.nombre_p, p.apaterno, p.amaterno,
                    p.nombre_p + ' ' + p.apaterno + ISNULL(' ' + p.amaterno, '') AS nombre_completo,
                    p.tipo_documento, p.num_documento, p.fecha_nacimiento,
                    DATEDIFF(YEAR, p.fecha_nacimiento, GETDATE()) AS edad,
                    p.idpais, pa.nombre_pais, p.telefono, p.email, p.fecha_registro,
                    (SELECT COUNT(*) FROM boleto b WHERE b.idpasajero = p.idpasajero
                       AND b.estado <> 'Cancelado') AS vuelos
             FROM pasajero p
             JOIN pais pa ON pa.idpais = p.idpais
             WHERE @f = '' OR p.nombre_p LIKE @like OR p.apaterno LIKE @like
                OR p.amaterno LIKE @like OR p.num_documento LIKE @like
                OR p.email LIKE @like OR p.idpasajero LIKE @like
             ORDER BY p.apaterno, p.nombre_p",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"))
    End Function

    ''' <summary>Lista corta para las listas desplegables del asistente de reserva.
    '''
    ''' A un pasajero solo se le ofrece a sí mismo: si le devolviéramos el directorio
    ''' completo, cualquiera podría ir leyendo los nombres y documentos de los demás
    ''' desde el portal. Para viajar acompañado registra a su acompañante con el
    ''' botón de pasajero nuevo.</summary>
    Public Shared Function ParaCombo(Optional filtro As String = "") As DataTable
        If Sesion.IdPasajero IsNot Nothing Then
            Return Db.Consultar(
                "SELECT idpasajero, nombre_p + ' ' + apaterno + ' · ' + num_documento AS etiqueta
                 FROM pasajero WHERE idpasajero = @p",
                New SqlParameter("@p", Sesion.IdPasajero))
        End If

        Return Db.Consultar(
            "SELECT TOP 200 idpasajero,
                    nombre_p + ' ' + apaterno + ' · ' + num_documento AS etiqueta
             FROM pasajero
             WHERE @f = '' OR nombre_p LIKE @like OR apaterno LIKE @like
                OR num_documento LIKE @like OR idpasajero LIKE @like
             ORDER BY apaterno, nombre_p",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"))
    End Function

    Public Shared Function Obtener(idPasajero As String) As DataRow
        Return Db.ConsultarFila("SELECT * FROM pasajero WHERE idpasajero = @p",
                                New SqlParameter("@p", idPasajero.Trim().ToUpper()))
    End Function

    Public Shared Function Existe(idPasajero As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM pasajero WHERE idpasajero = @p",
                         New SqlParameter("@p", idPasajero.Trim().ToUpper())) > 0
    End Function

    ''' <summary>Propone el siguiente código libre siguiendo el formato P0000001.</summary>
    Public Shared Function SiguienteCodigo() As String
        Dim maximo = Db.Escalar(
            "SELECT MAX(CAST(SUBSTRING(idpasajero, 2, 7) AS INT))
             FROM pasajero
             WHERE idpasajero LIKE 'P[0-9][0-9][0-9][0-9][0-9][0-9][0-9]'")

        Dim siguiente = If(maximo Is Nothing OrElse IsDBNull(maximo), 1, CInt(maximo) + 1)
        Return "P" & siguiente.ToString("D7")
    End Function

    ''' <summary>Valida y guarda. Devuelve Nothing si salió bien, o el mensaje de error.</summary>
    Public Shared Function Guardar(idPasajero As String, nombre As String, apaterno As String,
                                   amaterno As String, tipoDocumento As String, numDocumento As String,
                                   fechaNacimiento As Date?, idPais As String, telefono As String,
                                   email As String, editando As Boolean) As String

        If String.IsNullOrWhiteSpace(idPasajero) Then Return "Escribe el código del pasajero."
        If Not Validador.EsCodigoPasajeroValido(idPasajero) Then
            Return "El código debe ser la letra P seguida de 7 dígitos (por ejemplo P0000001)."
        End If
        If String.IsNullOrWhiteSpace(nombre) Then Return "Escribe el nombre del pasajero."
        If String.IsNullOrWhiteSpace(apaterno) Then Return "Escribe el apellido paterno."
        If String.IsNullOrWhiteSpace(tipoDocumento) Then Return "Selecciona el tipo de documento."
        If String.IsNullOrWhiteSpace(numDocumento) Then Return "Escribe el número de documento."
        If String.IsNullOrWhiteSpace(idPais) Then Return "Selecciona el país del pasajero."
        If Not Validador.EsEmailValido(email) Then Return "El correo electrónico no es válido."

        Dim errorFecha = Validador.ValidarFechaNacimiento(fechaNacimiento)
        If errorFecha IsNot Nothing Then Return errorFecha

        Dim codigo = idPasajero.Trim().ToUpper()

        If Not editando AndAlso Existe(codigo) Then Return "Ya existe un pasajero con ese código."

        If DocumentoRepetido(numDocumento, If(editando, codigo, Nothing)) Then
            Return "Ya hay otro pasajero registrado con ese número de documento."
        End If
        If EmailRepetido(email, If(editando, codigo, Nothing)) Then
            Return "Ya hay otro pasajero registrado con ese correo."
        End If

        If editando Then
            Db.Ejecutar("UPDATE pasajero SET nombre_p = @n, apaterno = @ap, amaterno = @am,
                                             tipo_documento = @td, num_documento = @nd,
                                             fecha_nacimiento = @fn, idpais = @pa,
                                             telefono = @tel, email = @em
                         WHERE idpasajero = @p",
                        New SqlParameter("@p", codigo),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@ap", apaterno.Trim()),
                        New SqlParameter("@am", Db.Opcional(amaterno)),
                        New SqlParameter("@td", tipoDocumento),
                        New SqlParameter("@nd", numDocumento.Trim()),
                        New SqlParameter("@fn", fechaNacimiento.Value.Date),
                        New SqlParameter("@pa", idPais),
                        New SqlParameter("@tel", Db.Opcional(telefono)),
                        New SqlParameter("@em", email.Trim().ToLower()))
        Else
            Db.Ejecutar("INSERT INTO pasajero (idpasajero, nombre_p, apaterno, amaterno,
                                               tipo_documento, num_documento, fecha_nacimiento,
                                               idpais, telefono, email)
                         VALUES (@p, @n, @ap, @am, @td, @nd, @fn, @pa, @tel, @em)",
                        New SqlParameter("@p", codigo),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@ap", apaterno.Trim()),
                        New SqlParameter("@am", Db.Opcional(amaterno)),
                        New SqlParameter("@td", tipoDocumento),
                        New SqlParameter("@nd", numDocumento.Trim()),
                        New SqlParameter("@fn", fechaNacimiento.Value.Date),
                        New SqlParameter("@pa", idPais),
                        New SqlParameter("@tel", Db.Opcional(telefono)),
                        New SqlParameter("@em", email.Trim().ToLower()))
        End If

        BitacoraService.Registrar(If(editando, BitacoraService.EDITAR, BitacoraService.CREAR),
                                  "pasajero", $"{codigo} · {nombre.Trim()} {apaterno.Trim()}")
        Return Nothing
    End Function

    Private Shared Function DocumentoRepetido(numDocumento As String, exceptoCodigo As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM pasajero
                          WHERE num_documento = @d AND (@p IS NULL OR idpasajero <> @p)",
                         New SqlParameter("@d", numDocumento.Trim()),
                         New SqlParameter("@p", Db.Opcional(exceptoCodigo))) > 0
    End Function

    Private Shared Function EmailRepetido(email As String, exceptoCodigo As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM pasajero
                          WHERE email = @e AND (@p IS NULL OR idpasajero <> @p)",
                         New SqlParameter("@e", email.Trim().ToLower()),
                         New SqlParameter("@p", Db.Opcional(exceptoCodigo))) > 0
    End Function

    ''' <summary>Elimina un pasajero. No se permite si tiene historial de vuelos:
    ''' borrarlo dejaría boletos y reservas apuntando a nadie.</summary>
    Public Shared Function Eliminar(idPasajero As String) As String
        Dim codigo = idPasajero.Trim().ToUpper()

        Dim conBoletos = Db.Contar("SELECT COUNT(*) FROM boleto WHERE idpasajero = @p",
                                   New SqlParameter("@p", codigo))
        Dim conReservas = Db.Contar("SELECT COUNT(*) FROM reserva WHERE idpasajero = @p",
                                    New SqlParameter("@p", codigo))

        If conBoletos > 0 OrElse conReservas > 0 Then
            Return "No se puede eliminar: este pasajero tiene reservas o boletos en el sistema."
        End If

        Db.Ejecutar("DELETE FROM pasajero WHERE idpasajero = @p", New SqlParameter("@p", codigo))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "pasajero", codigo)
        Return Nothing
    End Function

    ''' <summary>Historial de vuelos de un pasajero.</summary>
    Public Shared Function Historial(idPasajero As String) As DataTable
        Return Db.Consultar("SELECT codigo_reserva, codigo_vuelo, fecha_salida,
                                    iata_origen, iata_destino, asiento, clase, total, estado
                             FROM dbo.v_boleto_detalle
                             WHERE idpasajero = @p
                             ORDER BY fecha_salida DESC",
                            New SqlParameter("@p", idPasajero.Trim().ToUpper()))
    End Function
End Class
