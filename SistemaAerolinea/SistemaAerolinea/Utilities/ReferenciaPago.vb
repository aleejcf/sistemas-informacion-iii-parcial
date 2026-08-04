Imports System.Security.Cryptography

''' <summary>Genera la referencia de autorización de un cobro: el número que la
''' pasarela devuelve al aprobar el pago y que el pasajero cita cuando reclama.
'''
''' En un sistema en producción esta referencia la emite la pasarela (Stripe,
''' PayPal, un procesador local) y aquí solo se guardaría. Como este proyecto no
''' se conecta a ninguna, se genera con el mismo formato para que el flujo y los
''' datos sean los reales.</summary>
Public Class ReferenciaPago

    ''' <summary>Sin I, O, 0 ni 1: una referencia se dicta por teléfono al reclamar.</summary>
    Private Const ALFABETO As String = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"

    Public Const PREFIJO As String = "ALAS"

    ''' <summary>ALAS-7K3M9QP2X4</summary>
    Public Shared Function Generar() As String
        Dim caracteres(9) As Char
        Dim indices(9) As Byte
        RandomNumberGenerator.Fill(indices)

        For i = 0 To 9
            caracteres(i) = ALFABETO(indices(i) Mod ALFABETO.Length)
        Next

        Return $"{PREFIJO}-{New String(caracteres)}"
    End Function

    Public Shared Function EsValida(referencia As String) As Boolean
        If String.IsNullOrWhiteSpace(referencia) Then Return False

        Dim partes = referencia.Trim().ToUpper().Split("-"c)
        If partes.Length <> 2 Then Return False
        If partes(0) <> PREFIJO Then Return False
        If partes(1).Length <> 10 Then Return False

        Return partes(1).All(Function(c) ALFABETO.IndexOf(c) >= 0)
    End Function
End Class
