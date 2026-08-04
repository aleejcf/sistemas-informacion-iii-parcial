Imports System.Security.Cryptography

''' <summary>Genera el localizador de reserva (PNR): los 6 caracteres que toda
''' aerolínea le entrega al pasajero para consultar su vuelo.</summary>
Public Class GeneradorPnr

    ''' <summary>Alfabeto sin I, O ni 0/1: un PNR se dicta por teléfono y se
    ''' escribe a mano, así que no puede tener caracteres que se confundan.</summary>
    Public Const ALFABETO As String = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"

    Public Const LONGITUD As Integer = 6

    ''' <summary>Devuelve un localizador nuevo. Con 32^6 combinaciones (más de mil
    ''' millones) la repetición es improbable, pero igual la base de datos tiene
    ''' la columna como UNIQUE y ReservaService reintenta si choca.</summary>
    Public Shared Function Generar() As String
        Dim resultado(LONGITUD - 1) As Char
        Dim indices(LONGITUD - 1) As Byte
        RandomNumberGenerator.Fill(indices)
        For i = 0 To LONGITUD - 1
            resultado(i) = ALFABETO(indices(i) Mod ALFABETO.Length)
        Next
        Return New String(resultado)
    End Function

    ''' <summary>Comprueba que un texto tenga forma de localizador.</summary>
    Public Shared Function EsValido(pnr As String) As Boolean
        If String.IsNullOrWhiteSpace(pnr) Then Return False

        Dim limpio = pnr.Trim().ToUpper()
        If limpio.Length <> LONGITUD Then Return False

        For Each caracter In limpio
            If ALFABETO.IndexOf(caracter) < 0 Then Return False
        Next
        Return True
    End Function
End Class
