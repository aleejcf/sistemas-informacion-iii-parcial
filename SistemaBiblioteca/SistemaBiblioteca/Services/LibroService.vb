Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>El acervo: los títulos catalogados y sus ejemplares físicos.
''' La distinción es el corazón del sistema — un título es la obra, un ejemplar
''' es la copia que alguien se lleva bajo el brazo.</summary>
Public Class LibroService

    Public Shared ReadOnly EstadosEjemplar As String() =
        {"Disponible", "Prestado", "Reparación", "Extraviado", "Baja"}

    Public Shared ReadOnly Condiciones As String() =
        {"Nuevo", "Bueno", "Regular", "Deteriorado"}

    ' ======================= CATÁLOGO =======================

    ''' <summary>Busca en el catálogo. El filtro entra por título, autor, ISBN o
    ''' código, que son las cuatro formas en que alguien pide un libro.</summary>
    Public Shared Function Listar(Optional filtro As String = "",
                                  Optional idCategoria As Integer? = Nothing,
                                  Optional soloDisponibles As Boolean = False) As DataTable
        Return Db.Consultar(
            "SELECT * FROM dbo.v_libro_detalle
              WHERE (@f = '' OR titulo LIKE @like OR autor LIKE @like
                     OR isbn LIKE @like OR idlibro LIKE @like OR editorial LIKE @like)
                AND (@c IS NULL OR idcategoria = @c)
                AND (@d = 0 OR disponibles > 0)
              ORDER BY titulo",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"),
            New SqlParameter("@c", If(idCategoria.HasValue, CObj(idCategoria.Value), DBNull.Value)),
            New SqlParameter("@d", If(soloDisponibles, 1, 0)))
    End Function

    Public Shared Function Obtener(idLibro As String) As DataRow
        Return Db.ConsultarFila("SELECT * FROM dbo.v_libro_detalle WHERE idlibro = @l",
                                New SqlParameter("@l", idLibro))
    End Function

    ''' <summary>Sugiere el siguiente código de libro libre (L00021, L00022…).
    ''' Se conserva el formato del II Parcial para que el catálogo viejo y el
    ''' nuevo se lean igual.</summary>
    Public Shared Function SugerirCodigo() As String
        ' SUBSTRING salta la L y convierte el resto a número: así L00009 + 1 da
        ' L00010 y no L000010, que es lo que pasaría concatenando texto.
        Dim siguiente = Db.Contar("SELECT ISNULL(MAX(CAST(SUBSTRING(idlibro, 2, 5) AS INT)), 0) + 1
                                   FROM libro")
        Return "L" & siguiente.ToString("00000")
    End Function

    Public Shared Function ExisteCodigo(idLibro As String) As Boolean
        Return Db.Contar("SELECT COUNT(*) FROM libro WHERE idlibro = @l",
                         New SqlParameter("@l", idLibro.Trim().ToUpper())) > 0
    End Function

    ''' <summary>Registra un título nuevo. Devuelve Nothing si salió bien, o el
    ''' mensaje de error. No crea ejemplares: eso se hace aparte, porque catalogar
    ''' una obra y recibir sus copias son dos momentos distintos.</summary>
    Public Shared Function Crear(idLibro As String, isbn As String, titulo As String,
                                 idAutor As Integer, idEditorial As Integer?, idCategoria As Integer,
                                 anio As Integer?, edicion As String, idioma As String,
                                 sinopsis As String) As String

        Dim validacion = Validar(isbn, titulo, anio)
        If validacion IsNot Nothing Then Return validacion
        If Not Validador.EsIdLibroValido(idLibro) Then
            Return "El código del libro debe ser la letra L seguida de 5 dígitos (L00021)."
        End If
        If ExisteCodigo(idLibro) Then Return "Ya existe un libro con ese código."

        Dim isbnRepetido = ValidarIsbnRepetido(isbn, "")
        If isbnRepetido IsNot Nothing Then Return isbnRepetido

        Db.Ejecutar("INSERT INTO libro (idlibro, isbn, titulo, idautor, ideditorial, idcategoria,
                                        anio_publicacion, edicion, idioma, sinopsis)
                     VALUES (@l, @i, @t, @a, @e, @c, @an, @ed, @id, @s)",
                    New SqlParameter("@l", idLibro.Trim().ToUpper()),
                    New SqlParameter("@i", Db.Opcional(NormalizarIsbn(isbn))),
                    New SqlParameter("@t", titulo.Trim()),
                    New SqlParameter("@a", idAutor),
                    New SqlParameter("@e", If(idEditorial.HasValue, CObj(idEditorial.Value), DBNull.Value)),
                    New SqlParameter("@c", idCategoria),
                    New SqlParameter("@an", If(anio.HasValue, CObj(anio.Value), DBNull.Value)),
                    New SqlParameter("@ed", Db.Opcional(edicion)),
                    New SqlParameter("@id", If(String.IsNullOrWhiteSpace(idioma), "Español", idioma.Trim())),
                    New SqlParameter("@s", Db.Opcional(sinopsis)))

        BitacoraService.Registrar(BitacoraService.CREAR, "libro", $"{idLibro.Trim().ToUpper()} · {titulo.Trim()}")
        Return Nothing
    End Function

    Public Shared Function Actualizar(idLibro As String, isbn As String, titulo As String,
                                      idAutor As Integer, idEditorial As Integer?, idCategoria As Integer,
                                      anio As Integer?, edicion As String, idioma As String,
                                      sinopsis As String) As String

        Dim validacion = Validar(isbn, titulo, anio)
        If validacion IsNot Nothing Then Return validacion

        Dim isbnRepetido = ValidarIsbnRepetido(isbn, idLibro)
        If isbnRepetido IsNot Nothing Then Return isbnRepetido

        Db.Ejecutar("UPDATE libro SET isbn = @i, titulo = @t, idautor = @a, ideditorial = @e,
                                      idcategoria = @c, anio_publicacion = @an, edicion = @ed,
                                      idioma = @id, sinopsis = @s
                     WHERE idlibro = @l",
                    New SqlParameter("@l", idLibro),
                    New SqlParameter("@i", Db.Opcional(NormalizarIsbn(isbn))),
                    New SqlParameter("@t", titulo.Trim()),
                    New SqlParameter("@a", idAutor),
                    New SqlParameter("@e", If(idEditorial.HasValue, CObj(idEditorial.Value), DBNull.Value)),
                    New SqlParameter("@c", idCategoria),
                    New SqlParameter("@an", If(anio.HasValue, CObj(anio.Value), DBNull.Value)),
                    New SqlParameter("@ed", Db.Opcional(edicion)),
                    New SqlParameter("@id", If(String.IsNullOrWhiteSpace(idioma), "Español", idioma.Trim())),
                    New SqlParameter("@s", Db.Opcional(sinopsis)))

        BitacoraService.Registrar(BitacoraService.EDITAR, "libro", $"{idLibro} · {titulo.Trim()}")
        Return Nothing
    End Function

    Private Shared Function Validar(isbn As String, titulo As String, anio As Integer?) As String
        If String.IsNullOrWhiteSpace(titulo) Then Return "Escribe el título del libro."
        If Not Validador.EsIsbnValido(isbn) Then
            Return "El ISBN no es válido. Revisa que tenga 10 o 13 dígitos y que estén bien tecleados."
        End If
        Return Validador.ValidarAnioPublicacion(anio)
    End Function

    Private Shared Function ValidarIsbnRepetido(isbn As String, exceptoLibro As String) As String
        Dim limpio = NormalizarIsbn(isbn)
        If String.IsNullOrWhiteSpace(limpio) Then Return Nothing

        Dim repetido = Db.Contar("SELECT COUNT(*) FROM libro WHERE isbn = @i AND idlibro <> @l",
                                 New SqlParameter("@i", limpio),
                                 New SqlParameter("@l", If(exceptoLibro, "")))
        If repetido > 0 Then Return "Ya hay otro título registrado con ese ISBN."
        Return Nothing
    End Function

    Private Shared Function NormalizarIsbn(isbn As String) As String
        If String.IsNullOrWhiteSpace(isbn) Then Return Nothing
        Return isbn.Trim().ToUpper()
    End Function

    ''' <summary>Elimina un título. Solo si no tiene ningún ejemplar: un libro con
    ''' copias en la estantería no se borra del catálogo, se dan de baja sus
    ''' ejemplares primero.</summary>
    Public Shared Function Eliminar(idLibro As String) As String
        Dim ejemplares = Db.Contar("SELECT COUNT(*) FROM ejemplar WHERE idlibro = @l",
                                   New SqlParameter("@l", idLibro))
        If ejemplares > 0 Then
            Return $"Este título tiene {ejemplares} ejemplares registrados. " &
                   "Elimínalos o dales de baja antes de quitar el título del catálogo."
        End If

        Dim reservas = Db.Contar("SELECT COUNT(*) FROM reserva WHERE idlibro = @l",
                                 New SqlParameter("@l", idLibro))
        If reservas > 0 Then Return "Este título tiene reservas registradas y no se puede eliminar."

        Dim titulo = Db.Escalar("SELECT titulo FROM libro WHERE idlibro = @l",
                                New SqlParameter("@l", idLibro))
        Db.Ejecutar("DELETE FROM libro WHERE idlibro = @l", New SqlParameter("@l", idLibro))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "libro", $"{idLibro} · {If(titulo, "")}")
        Return Nothing
    End Function

    ' ======================= EJEMPLARES =======================

    Public Shared Function ListarEjemplares(idLibro As String) As DataTable
        Return Db.Consultar("SELECT * FROM dbo.v_ejemplar_detalle WHERE idlibro = @l
                             ORDER BY codigo_barras",
                            New SqlParameter("@l", idLibro))
    End Function

    ''' <summary>Los ejemplares que se pueden prestar ahora mismo de un título.
    ''' La usa el mostrador de préstamo para elegir cuál copia entregar.</summary>
    Public Shared Function EjemplaresDisponibles(idLibro As String) As DataTable
        Return Db.Consultar("SELECT idejemplar, codigo_barras, ubicacion, condicion
                             FROM ejemplar
                             WHERE idlibro = @l AND estado = 'Disponible'
                             ORDER BY codigo_barras",
                            New SqlParameter("@l", idLibro))
    End Function

    Public Shared Function BuscarPorCodigoBarras(codigoBarras As String) As DataRow
        Return Db.ConsultarFila("SELECT * FROM dbo.v_ejemplar_detalle WHERE codigo_barras = @c",
                                New SqlParameter("@c", codigoBarras.Trim().ToUpper()))
    End Function

    ''' <summary>Registra N copias nuevas de un título. Se numeran continuando
    ''' desde la última existente, así que agregar tres copias a un libro que ya
    ''' tiene cinco crea la 06, la 07 y la 08.</summary>
    Public Shared Function AgregarEjemplares(idLibro As String, cantidad As Integer,
                                             ubicacion As String, condicion As String) As String
        If cantidad < 1 OrElse cantidad > 50 Then Return "La cantidad debe estar entre 1 y 50 ejemplares."
        If Not ExisteCodigo(idLibro) Then Return "No se encontró el título en el catálogo."
        If Not Condiciones.Contains(condicion) Then condicion = "Bueno"

        ' El número de copia sale del código de barras existente más alto, no del
        ' conteo: si se dio de baja la copia 03, la siguiente debe ser la 06 y no
        ' repetir la 05 que ya existe.
        Dim ultimo = Db.Contar("SELECT ISNULL(MAX(CAST(RIGHT(codigo_barras, 2) AS INT)), 0)
                                FROM ejemplar WHERE idlibro = @l",
                               New SqlParameter("@l", idLibro))

        If ultimo + cantidad > 99 Then
            Return "Un título no puede pasar de 99 ejemplares con este formato de código de barras."
        End If

        For i = 1 To cantidad
            Db.Ejecutar("INSERT INTO ejemplar (codigo_barras, idlibro, ubicacion, estado, condicion)
                         VALUES (@c, @l, @u, 'Disponible', @co)",
                        New SqlParameter("@c", $"{idLibro}-{(ultimo + i):00}"),
                        New SqlParameter("@l", idLibro),
                        New SqlParameter("@u", Db.Opcional(ubicacion)),
                        New SqlParameter("@co", condicion))
        Next

        BitacoraService.Registrar(BitacoraService.CREAR, "ejemplar",
                                  $"{cantidad} ejemplares agregados a {idLibro}")
        Return Nothing
    End Function

    ''' <summary>Cambia el estado de un ejemplar (mandarlo a reparación, darlo por
    ''' extraviado, devolverlo a la estantería). No deja tocar un ejemplar que está
    ''' prestado: primero tiene que volver.</summary>
    Public Shared Function CambiarEstadoEjemplar(idEjemplar As Integer, estado As String,
                                                 condicion As String) As String
        If Not EstadosEjemplar.Contains(estado) Then Return "Estado de ejemplar no válido."
        If estado = "Prestado" Then
            Return "El estado 'Prestado' lo pone el sistema al registrar un préstamo, no se asigna a mano."
        End If

        Dim fila = Db.ConsultarFila("SELECT codigo_barras, estado FROM ejemplar WHERE idejemplar = @e",
                                    New SqlParameter("@e", idEjemplar))
        If fila Is Nothing Then Return "No se encontró el ejemplar."

        If Db.Texto(fila, "estado") = "Prestado" Then
            Return "Este ejemplar está prestado. Regístrale la devolución antes de cambiarle el estado."
        End If

        Db.Ejecutar("UPDATE ejemplar SET estado = @es, condicion = @co WHERE idejemplar = @e",
                    New SqlParameter("@e", idEjemplar),
                    New SqlParameter("@es", estado),
                    New SqlParameter("@co", If(Condiciones.Contains(condicion), condicion, "Bueno")))

        BitacoraService.Registrar(BitacoraService.EDITAR, "ejemplar",
                                  $"{Db.Texto(fila, "codigo_barras")} → {estado}")
        Return Nothing
    End Function

    Public Shared Function ActualizarUbicacion(idEjemplar As Integer, ubicacion As String) As String
        Db.Ejecutar("UPDATE ejemplar SET ubicacion = @u WHERE idejemplar = @e",
                    New SqlParameter("@e", idEjemplar),
                    New SqlParameter("@u", Db.Opcional(ubicacion)))
        Return Nothing
    End Function

    ''' <summary>Elimina un ejemplar del inventario. Solo si nunca se prestó:
    ''' una copia con historial se da de baja para no perder el registro de quién
    ''' la tuvo.</summary>
    Public Shared Function EliminarEjemplar(idEjemplar As Integer) As String
        Dim conHistorial = Db.Contar("SELECT COUNT(*) FROM detalle_prestamo WHERE idejemplar = @e",
                                     New SqlParameter("@e", idEjemplar))
        If conHistorial > 0 Then
            Return "Este ejemplar tiene historial de préstamos. " &
                   "Cámbialo a 'Baja' en vez de eliminarlo, así se conserva el registro."
        End If

        Dim codigo = Db.Escalar("SELECT codigo_barras FROM ejemplar WHERE idejemplar = @e",
                                New SqlParameter("@e", idEjemplar))
        Db.Ejecutar("DELETE FROM ejemplar WHERE idejemplar = @e", New SqlParameter("@e", idEjemplar))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "ejemplar", If(codigo, "").ToString())
        Return Nothing
    End Function

    ' ======================= REPORTES =======================

    Public Shared Function MasPrestados(Optional top As Integer = 8,
                                        Optional dias As Integer = 180) As DataTable
        Return Db.Consultar("EXEC dbo.sp_libros_mas_prestados @top = @t, @dias = @d",
                            New SqlParameter("@t", top),
                            New SqlParameter("@d", dias))
    End Function
End Class
