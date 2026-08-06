Imports System.Text.RegularExpressions

''' <summary>Reglas de validación puras: no tocan la base de datos ni la interfaz,
''' por eso se pueden probar con pruebas unitarias.</summary>
Public Class Validador

    Public Shared Function EsEmailValido(email As String) As Boolean
        If String.IsNullOrWhiteSpace(email) Then Return False
        Return Regex.IsMatch(email.Trim(), "^[^@\s]+@[^@\s]+\.[^@\s]+$")
    End Function

    ''' <summary>4 a 30 caracteres: letras, números o guion bajo.</summary>
    Public Shared Function EsUsuarioValido(usuario As String) As Boolean
        If String.IsNullOrWhiteSpace(usuario) Then Return False
        Return Regex.IsMatch(usuario.Trim(), "^[a-zA-Z0-9_]{4,30}$")
    End Function

    ''' <summary>Devuelve Nothing si la contraseña es válida; si no, el mensaje de error.</summary>
    Public Shared Function ValidarContrasena(clave As String) As String
        If String.IsNullOrEmpty(clave) OrElse clave.Length < 6 Then
            Return "La contraseña debe tener al menos 6 caracteres."
        End If
        If Not Regex.IsMatch(clave, "[A-Za-z]") OrElse Not Regex.IsMatch(clave, "[0-9]") Then
            Return "La contraseña debe combinar letras y números."
        End If
        Return Nothing
    End Function

    ' ---------- Códigos del acervo ----------

    ''' <summary>Código de libro: la letra L seguida de 5 dígitos (L00001).
    ''' Es el formato que traía el catálogo del II Parcial y se conserva.</summary>
    Public Shared Function EsIdLibroValido(codigo As String) As Boolean
        If String.IsNullOrWhiteSpace(codigo) Then Return False
        Return Regex.IsMatch(codigo.Trim().ToUpper(), "^L\d{5}$")
    End Function

    ''' <summary>Código de socio: la letra U seguida de 5 dígitos (U00001).</summary>
    Public Shared Function EsIdSocioValido(codigo As String) As Boolean
        If String.IsNullOrWhiteSpace(codigo) Then Return False
        Return Regex.IsMatch(codigo.Trim().ToUpper(), "^U\d{5}$")
    End Function

    ''' <summary>Código de barras de un ejemplar: el libro y el número de copia
    ''' (L00001-03).</summary>
    Public Shared Function EsCodigoBarrasValido(codigo As String) As Boolean
        If String.IsNullOrWhiteSpace(codigo) Then Return False
        Return Regex.IsMatch(codigo.Trim().ToUpper(), "^L\d{5}-\d{2}$")
    End Function

    ''' <summary>Número de identidad hondureño: 13 dígitos.</summary>
    Public Shared Function EsIdentidadValida(identidad As String) As Boolean
        If String.IsNullOrWhiteSpace(identidad) Then Return False
        Return Regex.IsMatch(identidad.Trim().Replace("-", ""), "^\d{13}$")
    End Function

    ''' <summary>Teléfono de 8 dígitos, con o sin guiones.</summary>
    Public Shared Function EsTelefonoValido(telefono As String) As Boolean
        If String.IsNullOrWhiteSpace(telefono) Then Return True   ' es opcional
        Return Regex.IsMatch(telefono.Trim().Replace("-", "").Replace(" ", ""), "^\d{8}$")
    End Function

    ' ---------- ISBN ----------

    ''' <summary>Valida un ISBN de 10 o 13 dígitos comprobando su dígito de control.
    ''' El ISBN no es un número cualquiera: su última cifra se calcula a partir de
    ''' las anteriores, así que un dígito mal tecleado se detecta aquí y no meses
    ''' después con el catálogo ya sucio. Un ISBN vacío se acepta: es opcional.</summary>
    Public Shared Function EsIsbnValido(isbn As String) As Boolean
        If String.IsNullOrWhiteSpace(isbn) Then Return True

        Dim limpio = Regex.Replace(isbn.Trim().ToUpper(), "[\s-]", "")

        If limpio.Length = 13 Then Return VerificarIsbn13(limpio)
        If limpio.Length = 10 Then Return VerificarIsbn10(limpio)
        Return False
    End Function

    ''' <summary>ISBN-13: las cifras se multiplican alternando por 1 y por 3, y la
    ''' suma total tiene que ser múltiplo de 10.</summary>
    Private Shared Function VerificarIsbn13(isbn As String) As Boolean
        If Not Regex.IsMatch(isbn, "^\d{13}$") Then Return False

        Dim suma As Integer = 0
        For i = 0 To 12
            Dim digito = CInt(Char.GetNumericValue(isbn(i)))
            suma += If(i Mod 2 = 0, digito, digito * 3)
        Next
        Return suma Mod 10 = 0
    End Function

    ''' <summary>ISBN-10: cada cifra se multiplica por su posición descendente
    ''' (10, 9, 8…) y la suma tiene que ser múltiplo de 11. La última puede ser
    ''' una X, que en este esquema vale 10.</summary>
    Private Shared Function VerificarIsbn10(isbn As String) As Boolean
        If Not Regex.IsMatch(isbn, "^\d{9}[\dX]$") Then Return False

        Dim suma As Integer = 0
        For i = 0 To 9
            Dim valor As Integer
            If isbn(i) = "X"c Then
                If i <> 9 Then Return False
                valor = 10
            Else
                valor = CInt(Char.GetNumericValue(isbn(i)))
            End If
            suma += valor * (10 - i)
        Next
        Return suma Mod 11 = 0
    End Function

    ' ---------- Reglas del acervo y de la circulación ----------

    ''' <summary>Un libro no puede haberse publicado antes de la imprenta ni el
    ''' año que viene. Devuelve Nothing si el año es válido.</summary>
    Public Shared Function ValidarAnioPublicacion(anio As Integer?) As String
        If Not anio.HasValue Then Return Nothing            ' es opcional
        If anio.Value < 1450 Then Return "El año de publicación no puede ser anterior a 1450."
        If anio.Value > Date.Today.Year + 1 Then Return "El año de publicación no puede ser futuro."
        Return Nothing
    End Function

    ''' <summary>El plazo de un préstamo tiene que ser posterior al día en que se
    ''' hace y no puede pasar de un año.</summary>
    Public Shared Function ValidarPlazo(fechaPrestamo As Date, fechaVencimiento As Date) As String
        If fechaVencimiento <= fechaPrestamo.Date Then
            Return "La fecha de devolución debe ser posterior al día del préstamo."
        End If
        If (fechaVencimiento - fechaPrestamo.Date).TotalDays > 365 Then
            Return "Un préstamo no puede exceder un año."
        End If
        Return Nothing
    End Function

    ''' <summary>Calcula los días de mora entre la fecha de vencimiento y el día en
    ''' que el libro volvió. Nunca devuelve negativos: devolver antes de tiempo no
    ''' genera crédito a favor.</summary>
    Public Shared Function DiasDeRetraso(fechaVencimiento As Date, fechaDevolucion As Date) As Integer
        Dim dias = CInt((fechaDevolucion.Date - fechaVencimiento.Date).TotalDays)
        Return If(dias > 0, dias, 0)
    End Function

    ''' <summary>La multa por retraso: días de mora × tarifa diaria del tipo de
    ''' socio × ejemplares que se entregaron tarde. Se cobra por ejemplar porque
    ''' cada libro que no volvió es un libro que otro socio no pudo llevarse.</summary>
    Public Shared Function CalcularMulta(diasRetraso As Integer, multaDiaria As Decimal,
                                         ejemplares As Integer) As Decimal
        If diasRetraso <= 0 OrElse ejemplares <= 0 OrElse multaDiaria <= 0 Then Return 0D

        ' AwayFromZero y no el redondeo por defecto de .NET: Math.Round usa
        ' redondeo bancario (10.005 → 10.00), que no es como se redondea el
        ' dinero en un mostrador. Media unidad sube.
        Return Math.Round(diasRetraso * multaDiaria * ejemplares, 2, MidpointRounding.AwayFromZero)
    End Function
End Class
