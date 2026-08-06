Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Catálogos de autoridad: autores, editoriales, categorías y tipos de
''' socio. Son las listas que alimentan los combos de todo el sistema y la
''' configuración del negocio (cuántos libros presta cada tipo de socio y cuánto
''' se le multa). Editarlos es privilegio del Administrador.</summary>
Public Class CatalogoService

    ' ======================= LISTAS PARA COMBOS =======================
    ' Todas devuelven una columna `etiqueta` para mostrar y su id para guardar,
    ' de modo que las vistas siempre usan DisplayMemberPath="etiqueta".

    Public Shared Function AutoresParaCombo() As DataTable
        Return Db.Consultar("SELECT idautor, nombre AS etiqueta FROM autor ORDER BY nombre")
    End Function

    Public Shared Function EditorialesParaCombo() As DataTable
        Return Db.Consultar("SELECT ideditorial, nombre AS etiqueta FROM editorial ORDER BY nombre")
    End Function

    Public Shared Function CategoriasParaCombo() As DataTable
        Return Db.Consultar("SELECT idcategoria,
                                    nombre + ' (' + ISNULL(codigo_dewey, '—') + ')' AS etiqueta
                             FROM categoria ORDER BY nombre")
    End Function

    Public Shared Function TiposSocioParaCombo() As DataTable
        Return Db.Consultar("SELECT idtipo,
                                    nombre + ' · ' + CAST(max_prestamos AS VARCHAR(3)) + ' libros / ' +
                                    CAST(dias_prestamo AS VARCHAR(3)) + ' días' AS etiqueta
                             FROM tipo_socio ORDER BY nombre")
    End Function

    ''' <summary>Los idiomas que ya existen en el acervo, para no obligar a
    ''' escribirlos a mano y evitar "Ingles" y "Inglés" como dos idiomas.</summary>
    Public Shared Function IdiomasParaCombo() As List(Of String)
        Dim lista As New List(Of String) From {"Español", "Inglés", "Francés", "Portugués", "Alemán"}
        Try
            For Each fila As DataRow In Db.Consultar("SELECT DISTINCT idioma FROM libro
                                                      WHERE idioma IS NOT NULL").Rows
                Dim idioma = Db.Texto(fila, "idioma")
                If idioma <> "" AndAlso Not lista.Contains(idioma) Then lista.Add(idioma)
            Next
        Catch ex As Exception
            Registro.Advertencia($"No se pudieron leer los idiomas del acervo: {ex.Message}")
        End Try
        lista.Sort()
        Return lista
    End Function

    ' ======================= AUTORES =======================

    Public Shared Function ListarAutores(Optional filtro As String = "") As DataTable
        Return Db.Consultar(
            "SELECT a.idautor, a.nombre, a.nacionalidad,
                    (SELECT COUNT(*) FROM libro l WHERE l.idautor = a.idautor) AS titulos
             FROM autor a
             WHERE (@f = '' OR a.nombre LIKE @like OR a.nacionalidad LIKE @like)
             ORDER BY a.nombre",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"))
    End Function

    Public Shared Function GuardarAutor(idAutor As Integer, nombre As String,
                                        nacionalidad As String) As String
        If String.IsNullOrWhiteSpace(nombre) Then Return "Escribe el nombre del autor."

        Dim repetido = Db.Contar("SELECT COUNT(*) FROM autor WHERE nombre = @n AND idautor <> @id",
                                 New SqlParameter("@n", nombre.Trim()),
                                 New SqlParameter("@id", idAutor))
        If repetido > 0 Then Return "Ya existe un autor con ese nombre."

        If idAutor = 0 Then
            Dim nuevo = SiguienteId("autor", "idautor")
            Db.Ejecutar("INSERT INTO autor (idautor, nombre, nacionalidad) VALUES (@id, @n, @na)",
                        New SqlParameter("@id", nuevo),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@na", Db.Opcional(nacionalidad)))
            BitacoraService.Registrar(BitacoraService.CREAR, "autor", nombre.Trim())
        Else
            Db.Ejecutar("UPDATE autor SET nombre = @n, nacionalidad = @na WHERE idautor = @id",
                        New SqlParameter("@id", idAutor),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@na", Db.Opcional(nacionalidad)))
            BitacoraService.Registrar(BitacoraService.EDITAR, "autor", nombre.Trim())
        End If
        Return Nothing
    End Function

    Public Shared Function EliminarAutor(idAutor As Integer) As String
        Dim conLibros = Db.Contar("SELECT COUNT(*) FROM libro WHERE idautor = @id",
                                  New SqlParameter("@id", idAutor))
        If conLibros > 0 Then
            Return $"Este autor tiene {conLibros} títulos en el acervo. " &
                   "Reasígnalos antes de eliminarlo."
        End If

        Dim nombre = Db.Escalar("SELECT nombre FROM autor WHERE idautor = @id",
                                New SqlParameter("@id", idAutor))
        Db.Ejecutar("DELETE FROM autor WHERE idautor = @id", New SqlParameter("@id", idAutor))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "autor", If(nombre, "").ToString())
        Return Nothing
    End Function

    ' ======================= EDITORIALES =======================

    Public Shared Function ListarEditoriales(Optional filtro As String = "") As DataTable
        Return Db.Consultar(
            "SELECT e.ideditorial, e.nombre, e.pais,
                    (SELECT COUNT(*) FROM libro l WHERE l.ideditorial = e.ideditorial) AS titulos
             FROM editorial e
             WHERE (@f = '' OR e.nombre LIKE @like OR e.pais LIKE @like)
             ORDER BY e.nombre",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"))
    End Function

    Public Shared Function GuardarEditorial(idEditorial As Integer, nombre As String,
                                            pais As String) As String
        If String.IsNullOrWhiteSpace(nombre) Then Return "Escribe el nombre de la editorial."

        Dim repetido = Db.Contar("SELECT COUNT(*) FROM editorial WHERE nombre = @n AND ideditorial <> @id",
                                 New SqlParameter("@n", nombre.Trim()),
                                 New SqlParameter("@id", idEditorial))
        If repetido > 0 Then Return "Ya existe una editorial con ese nombre."

        If idEditorial = 0 Then
            Dim nuevo = SiguienteId("editorial", "ideditorial")
            Db.Ejecutar("INSERT INTO editorial (ideditorial, nombre, pais) VALUES (@id, @n, @p)",
                        New SqlParameter("@id", nuevo),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@p", Db.Opcional(pais)))
            BitacoraService.Registrar(BitacoraService.CREAR, "editorial", nombre.Trim())
        Else
            Db.Ejecutar("UPDATE editorial SET nombre = @n, pais = @p WHERE ideditorial = @id",
                        New SqlParameter("@id", idEditorial),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@p", Db.Opcional(pais)))
            BitacoraService.Registrar(BitacoraService.EDITAR, "editorial", nombre.Trim())
        End If
        Return Nothing
    End Function

    Public Shared Function EliminarEditorial(idEditorial As Integer) As String
        Dim conLibros = Db.Contar("SELECT COUNT(*) FROM libro WHERE ideditorial = @id",
                                  New SqlParameter("@id", idEditorial))
        If conLibros > 0 Then
            Return $"Esta editorial tiene {conLibros} títulos en el acervo. " &
                   "Reasígnalos antes de eliminarla."
        End If

        Dim nombre = Db.Escalar("SELECT nombre FROM editorial WHERE ideditorial = @id",
                                New SqlParameter("@id", idEditorial))
        Db.Ejecutar("DELETE FROM editorial WHERE ideditorial = @id", New SqlParameter("@id", idEditorial))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "editorial", If(nombre, "").ToString())
        Return Nothing
    End Function

    ' ======================= CATEGORÍAS =======================

    Public Shared Function ListarCategorias(Optional filtro As String = "") As DataTable
        Return Db.Consultar(
            "SELECT c.idcategoria, c.nombre, c.codigo_dewey, c.descripcion,
                    (SELECT COUNT(*) FROM libro l WHERE l.idcategoria = c.idcategoria) AS titulos
             FROM categoria c
             WHERE (@f = '' OR c.nombre LIKE @like OR c.codigo_dewey LIKE @like)
             ORDER BY c.nombre",
            New SqlParameter("@f", If(filtro, "").Trim()),
            New SqlParameter("@like", "%" & If(filtro, "").Trim() & "%"))
    End Function

    Public Shared Function GuardarCategoria(idCategoria As Integer, nombre As String,
                                            dewey As String, descripcion As String) As String
        If String.IsNullOrWhiteSpace(nombre) Then Return "Escribe el nombre de la categoría."
        If Not String.IsNullOrWhiteSpace(dewey) AndAlso dewey.Trim().Length <> 3 Then
            Return "El código Dewey son exactamente 3 caracteres (por ejemplo 005)."
        End If

        Dim repetido = Db.Contar("SELECT COUNT(*) FROM categoria WHERE nombre = @n AND idcategoria <> @id",
                                 New SqlParameter("@n", nombre.Trim()),
                                 New SqlParameter("@id", idCategoria))
        If repetido > 0 Then Return "Ya existe una categoría con ese nombre."

        If idCategoria = 0 Then
            Dim nuevo = SiguienteId("categoria", "idcategoria")
            Db.Ejecutar("INSERT INTO categoria (idcategoria, nombre, codigo_dewey, descripcion)
                         VALUES (@id, @n, @d, @de)",
                        New SqlParameter("@id", nuevo),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@d", Db.Opcional(dewey)),
                        New SqlParameter("@de", Db.Opcional(descripcion)))
            BitacoraService.Registrar(BitacoraService.CREAR, "categoria", nombre.Trim())
        Else
            Db.Ejecutar("UPDATE categoria SET nombre = @n, codigo_dewey = @d, descripcion = @de
                         WHERE idcategoria = @id",
                        New SqlParameter("@id", idCategoria),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@d", Db.Opcional(dewey)),
                        New SqlParameter("@de", Db.Opcional(descripcion)))
            BitacoraService.Registrar(BitacoraService.EDITAR, "categoria", nombre.Trim())
        End If
        Return Nothing
    End Function

    Public Shared Function EliminarCategoria(idCategoria As Integer) As String
        Dim conLibros = Db.Contar("SELECT COUNT(*) FROM libro WHERE idcategoria = @id",
                                  New SqlParameter("@id", idCategoria))
        If conLibros > 0 Then
            Return $"Esta categoría tiene {conLibros} títulos en el acervo. " &
                   "Reasígnalos antes de eliminarla."
        End If

        Dim nombre = Db.Escalar("SELECT nombre FROM categoria WHERE idcategoria = @id",
                                New SqlParameter("@id", idCategoria))
        Db.Ejecutar("DELETE FROM categoria WHERE idcategoria = @id", New SqlParameter("@id", idCategoria))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "categoria", If(nombre, "").ToString())
        Return Nothing
    End Function

    ' ======================= TIPOS DE SOCIO =======================
    ' Esta es la tabla de política: cambiar aquí un número cambia cómo presta
    ' toda la biblioteca. Por eso solo la toca un Administrador.

    Public Shared Function ListarTiposSocio() As DataTable
        Return Db.Consultar(
            "SELECT t.idtipo, t.nombre, t.max_prestamos, t.dias_prestamo, t.multa_diaria,
                    (SELECT COUNT(*) FROM socio s WHERE s.idtipo = t.idtipo) AS socios
             FROM tipo_socio t ORDER BY t.nombre")
    End Function

    Public Shared Function GuardarTipoSocio(idTipo As Integer, nombre As String,
                                            maxPrestamos As Integer, diasPrestamo As Integer,
                                            multaDiaria As Decimal) As String
        If String.IsNullOrWhiteSpace(nombre) Then Return "Escribe el nombre del tipo de socio."
        If maxPrestamos < 1 OrElse maxPrestamos > 20 Then Return "El máximo de préstamos debe estar entre 1 y 20."
        If diasPrestamo < 1 OrElse diasPrestamo > 90 Then Return "El plazo debe estar entre 1 y 90 días."
        If multaDiaria < 0 Then Return "La multa diaria no puede ser negativa."

        Dim repetido = Db.Contar("SELECT COUNT(*) FROM tipo_socio WHERE nombre = @n AND idtipo <> @id",
                                 New SqlParameter("@n", nombre.Trim()),
                                 New SqlParameter("@id", idTipo))
        If repetido > 0 Then Return "Ya existe un tipo de socio con ese nombre."

        If idTipo = 0 Then
            Dim nuevo = SiguienteId("tipo_socio", "idtipo")
            Db.Ejecutar("INSERT INTO tipo_socio (idtipo, nombre, max_prestamos, dias_prestamo, multa_diaria)
                         VALUES (@id, @n, @m, @d, @mu)",
                        New SqlParameter("@id", nuevo),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@m", maxPrestamos),
                        New SqlParameter("@d", diasPrestamo),
                        New SqlParameter("@mu", multaDiaria))
            BitacoraService.Registrar(BitacoraService.CREAR, "tipo_socio", nombre.Trim())
        Else
            Db.Ejecutar("UPDATE tipo_socio SET nombre = @n, max_prestamos = @m,
                                dias_prestamo = @d, multa_diaria = @mu
                         WHERE idtipo = @id",
                        New SqlParameter("@id", idTipo),
                        New SqlParameter("@n", nombre.Trim()),
                        New SqlParameter("@m", maxPrestamos),
                        New SqlParameter("@d", diasPrestamo),
                        New SqlParameter("@mu", multaDiaria))
            BitacoraService.Registrar(BitacoraService.EDITAR, "tipo_socio",
                                      $"{nombre.Trim()} · {maxPrestamos} libros / {diasPrestamo} días")
        End If
        Return Nothing
    End Function

    Public Shared Function EliminarTipoSocio(idTipo As Integer) As String
        Dim conSocios = Db.Contar("SELECT COUNT(*) FROM socio WHERE idtipo = @id",
                                  New SqlParameter("@id", idTipo))
        If conSocios > 0 Then
            Return $"Hay {conSocios} socios de este tipo. Cámbialos de tipo antes de eliminarlo."
        End If

        Dim nombre = Db.Escalar("SELECT nombre FROM tipo_socio WHERE idtipo = @id",
                                New SqlParameter("@id", idTipo))
        Db.Ejecutar("DELETE FROM tipo_socio WHERE idtipo = @id", New SqlParameter("@id", idTipo))
        BitacoraService.Registrar(BitacoraService.ELIMINAR, "tipo_socio", If(nombre, "").ToString())
        Return Nothing
    End Function

    ' ======================= AUXILIAR =======================

    ''' <summary>Estas tablas de catálogo usan llaves numéricas asignadas a mano
    ''' (no IDENTITY) porque el script del II Parcial ya las traía así. El
    ''' siguiente id es simplemente el mayor más uno.</summary>
    Private Shared Function SiguienteId(tabla As String, columna As String) As Integer
        ' `tabla` y `columna` son constantes escritas en este archivo, nunca
        ' entrada del usuario: aquí no hay superficie de inyección SQL.
        Return Db.Contar($"SELECT ISNULL(MAX({columna}), 0) + 1 FROM {tabla}")
    End Function
End Class
