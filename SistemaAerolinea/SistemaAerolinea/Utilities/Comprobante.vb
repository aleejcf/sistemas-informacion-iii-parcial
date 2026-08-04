''' <summary>Numeración de comprobantes fiscales. El número se arma con el
''' identificador de la reserva y un consecutivo dentro de ella, así queda
''' garantizado que es único (la columna en la base de datos es UNIQUE) y que
''' se puede rastrear a qué reserva pertenece con solo leerlo.</summary>
Public Class Comprobante

    Public Const FACTURA As String = "Factura"
    Public Const RECIBO As String = "Recibo"

    ''' <summary>F-000123-1 → factura de la reserva 123, primer pago.</summary>
    Public Shared Function Numero(tipo As String, idReserva As Integer, secuencia As Integer) As String
        Dim prefijo = If(tipo = RECIBO, "R", "F")
        Return $"{prefijo}-{idReserva:D6}-{secuencia}"
    End Function
End Class
