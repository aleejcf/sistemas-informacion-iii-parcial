Imports System.Text

''' <summary>Arma el texto del comprobante que se le entrega al socio. Se copia
''' al portapapeles desde el diálogo, que es lo más cerca de imprimir que llega
''' un sistema de escritorio sin depender de una impresora configurada.</summary>
Public Class Comprobante

    Private Const ANCHO As Integer = 46

    ''' <summary>Boleta de préstamo: qué se llevó, quién y hasta cuándo.</summary>
    Public Shared Function Prestamo(resultado As ResultadoPrestamo,
                                    ejemplares As IEnumerable(Of EjemplarElegido)) As String
        Dim texto As New StringBuilder()
        Encabezado(texto, "COMPROBANTE DE PRÉSTAMO")

        texto.AppendLine($"Folio:       {resultado.Codigo}")
        texto.AppendLine($"Socio:       {resultado.Socio}")
        texto.AppendLine($"Prestado:    {Formato.FechaHora(DateTime.Now)}")
        texto.AppendLine($"Devolver el: {Formato.Fecha(resultado.FechaVencimiento)}")
        texto.AppendLine($"Atendió:     {Sesion.NombreUsuario}")
        Separador(texto)

        For Each ejemplar In ejemplares
            texto.AppendLine($"{ejemplar.CodigoBarras}  {Recortar(ejemplar.Titulo, 26)}")
        Next

        Separador(texto)
        texto.AppendLine($"Total de ejemplares: {resultado.Ejemplares}")
        texto.AppendLine()
        texto.AppendLine("Conserve este comprobante. La devolución")
        texto.AppendLine("después de la fecha indicada genera multa.")
        Pie(texto)
        Return texto.ToString()
    End Function

    ''' <summary>Recibo de multa pagada.</summary>
    Public Shared Function Multa(codigoPrestamo As String, socio As String, motivo As String,
                                 diasRetraso As Integer, monto As Decimal) As String
        Dim texto As New StringBuilder()
        Encabezado(texto, "RECIBO DE MULTA")

        texto.AppendLine($"Préstamo:  {codigoPrestamo}")
        texto.AppendLine($"Socio:     {socio}")
        texto.AppendLine($"Motivo:    {motivo}")
        If diasRetraso > 0 Then texto.AppendLine($"Retraso:   {diasRetraso} días")
        texto.AppendLine($"Cobrado:   {Formato.FechaHora(DateTime.Now)}")
        texto.AppendLine($"Recibió:   {Sesion.NombreUsuario}")
        Separador(texto)
        texto.AppendLine($"TOTAL PAGADO:  {Formato.Dinero(monto)}")
        Pie(texto)
        Return texto.ToString()
    End Function

    Private Shared Sub Encabezado(texto As StringBuilder, titulo As String)
        texto.AppendLine(New String("="c, ANCHO))
        texto.AppendLine(Centrar("BIBLIOTECA ALEJANDRÍA"))
        texto.AppendLine(Centrar(titulo))
        texto.AppendLine(New String("="c, ANCHO))
    End Sub

    Private Shared Sub Separador(texto As StringBuilder)
        texto.AppendLine(New String("-"c, ANCHO))
    End Sub

    Private Shared Sub Pie(texto As StringBuilder)
        texto.AppendLine(New String("="c, ANCHO))
        texto.AppendLine(Centrar("El saber al alcance de todos"))
    End Sub

    Private Shared Function Centrar(texto As String) As String
        If texto.Length >= ANCHO Then Return texto
        Return New String(" "c, (ANCHO - texto.Length) \ 2) & texto
    End Function

    Private Shared Function Recortar(texto As String, largo As Integer) As String
        If String.IsNullOrEmpty(texto) Then Return ""
        Return If(texto.Length <= largo, texto, texto.Substring(0, largo - 1) & "…")
    End Function
End Class
