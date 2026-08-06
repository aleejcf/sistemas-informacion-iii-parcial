Imports Xunit

''' <summary>Pruebas del dígito de control del ISBN. Es la validación menos obvia
''' del sistema: la última cifra de un ISBN se calcula a partir de las anteriores,
''' así que un número mal tecleado se detecta sin consultar nada externo.</summary>
Public Class IsbnTests

    ' ---------- ISBN-13 ----------

    <Theory>
    <InlineData("978-99926-11-01-2")>        ' Programación en Pascal, del acervo
    <InlineData("978-99926-11-20-3")>        ' Inteligencia Artificial, del acervo
    <InlineData("9789992611012")>            ' el mismo, sin guiones
    <InlineData("978-3-16-148410-0")>        ' ejemplo canónico de ISBN-13
    Public Sub EsIsbnValido_AceptaIsbn13ConDigitoCorrecto(isbn As String)
        Assert.True(Validador.EsIsbnValido(isbn))
    End Sub

    <Theory>
    <InlineData("978-99926-11-01-4")>        ' un dígito de control equivocado
    <InlineData("978-99926-11-01-9")>
    <InlineData("978-3-16-148410-1")>
    Public Sub EsIsbnValido_RechazaIsbn13ConDigitoIncorrecto(isbn As String)
        Assert.False(Validador.EsIsbnValido(isbn))
    End Sub

    ''' <summary>Dos cifras intercambiadas es el error de tecleo más común y el
    ''' dígito de control existe justamente para atraparlo.</summary>
    <Fact>
    Public Sub EsIsbnValido_DetectaDigitosTranspuestos()
        Assert.True(Validador.EsIsbnValido("9789992611012"))
        Assert.False(Validador.EsIsbnValido("9789992611102"))
    End Sub

    ' ---------- ISBN-10 ----------

    <Theory>
    <InlineData("0-306-40615-2")>            ' ejemplo canónico de ISBN-10
    <InlineData("0306406152")>
    <InlineData("043942089X")>               ' la X final vale 10
    Public Sub EsIsbnValido_AceptaIsbn10ConDigitoCorrecto(isbn As String)
        Assert.True(Validador.EsIsbnValido(isbn))
    End Sub

    <Theory>
    <InlineData("0-306-40615-3")>
    <InlineData("0306406153")>
    <InlineData("043942089Y")>               ' solo la X es letra válida
    <InlineData("04394X089X")>               ' y solo en la última posición
    Public Sub EsIsbnValido_RechazaIsbn10Invalido(isbn As String)
        Assert.False(Validador.EsIsbnValido(isbn))
    End Sub

    ' ---------- Casos límite ----------

    <Theory>
    <InlineData("")>
    <InlineData("   ")>
    <InlineData(Nothing)>
    Public Sub EsIsbnValido_AceptaVacioPorqueElIsbnEsOpcional(isbn As String)
        Assert.True(Validador.EsIsbnValido(isbn))
    End Sub

    <Theory>
    <InlineData("12345")>                    ' ni 10 ni 13 cifras
    <InlineData("978999261101")>             ' 12 cifras
    <InlineData("97899926110123")>           ' 14 cifras
    <InlineData("no-es-un-isbn")>
    Public Sub EsIsbnValido_RechazaLongitudesQueNoSonIsbn(isbn As String)
        Assert.False(Validador.EsIsbnValido(isbn))
    End Sub
End Class
