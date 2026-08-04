Imports System.Security.Cryptography

''' <summary>Genera contraseñas temporales para las cuentas que crea un Administrador.</summary>
Public Class GeneradorClave

    ' Sin caracteres ambiguos (0/O, 1/l/I): la clave se dicta o se transcribe a mano
    Private Const CARACTERES As String = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789"

    ''' <summary>Contraseña aleatoria de 10 caracteres que ya cumple las reglas
    ''' de Validador.ValidarContrasena (letras y números).</summary>
    Public Shared Function GenerarTemporal() As String
        Dim clave As String
        Do
            clave = GenerarCadena(10)
        Loop While Validador.ValidarContrasena(clave) IsNot Nothing
        Return clave
    End Function

    Private Shared Function GenerarCadena(longitud As Integer) As String
        Dim resultado(longitud - 1) As Char
        Dim indices(longitud - 1) As Byte
        RandomNumberGenerator.Fill(indices)
        For i = 0 To longitud - 1
            resultado(i) = CARACTERES(indices(i) Mod CARACTERES.Length)
        Next
        Return New String(resultado)
    End Function
End Class
