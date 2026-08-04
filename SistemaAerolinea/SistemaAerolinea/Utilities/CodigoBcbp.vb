Imports System.Globalization
Imports System.Text

''' <summary>Arma la cadena que va dentro del código de barras del pase de abordar,
''' siguiendo el estándar IATA BCBP (Resolución 792), formato "M1".
'''
''' Es el mismo formato que llevan los pases de cualquier aerolínea del mundo: son
''' 60 caracteres de ancho fijo, sin separadores, y cada dato ocupa siempre la
''' misma posición. Gracias a eso un lector de cualquier aeropuerto puede leer un
''' pase emitido en otro país sin ponerse de acuerdo antes.
'''
''' La función es pura —no toca la base de datos ni la interfaz—, así que las
''' pruebas unitarias pueden comprobar posición por posición.</summary>
Public Class CodigoBcbp

    Public Const LONGITUD_OBLIGATORIA As Integer = 60

    ''' <summary>Genera la cadena BCBP de un tramo.</summary>
    ''' <param name="pasajero">Nombre completo; se convierte a APELLIDO/NOMBRE.</param>
    ''' <param name="pnr">Localizador de la reserva (6 caracteres).</param>
    ''' <param name="iataOrigen">Código de 3 letras del aeropuerto de salida.</param>
    ''' <param name="iataDestino">Código de 3 letras del aeropuerto de llegada.</param>
    ''' <param name="codigoAerolinea">Designador de 2 letras (AV, AA, CM…).</param>
    ''' <param name="numeroVuelo">Número del vuelo, sin el prefijo de la aerolínea.</param>
    ''' <param name="fechaSalida">Fecha de salida; en el código va como día juliano.</param>
    ''' <param name="clase">Clase del asiento, que define el código de compartimento.</param>
    ''' <param name="asiento">Asiento en formato 12C.</param>
    ''' <param name="secuencia">Número de secuencia del check-in.</param>
    Public Shared Function Generar(pasajero As String, pnr As String,
                                   iataOrigen As String, iataDestino As String,
                                   codigoAerolinea As String, numeroVuelo As Integer,
                                   fechaSalida As DateTime, clase As String,
                                   asiento As String, secuencia As Integer) As String

        Dim cadena As New StringBuilder()

        cadena.Append("M")                                     ' Código de formato
        cadena.Append("1")                                     ' Número de tramos
        cadena.Append(Campo(NombreBcbp(pasajero), 20))         ' Pasajero
        cadena.Append("E")                                     ' Boleto electrónico
        cadena.Append(Campo(SoloAscii(pnr).ToUpper(), 7))      ' Localizador
        cadena.Append(Campo(SoloAscii(iataOrigen).ToUpper(), 3))
        cadena.Append(Campo(SoloAscii(iataDestino).ToUpper(), 3))
        cadena.Append(Campo(SoloAscii(codigoAerolinea).ToUpper(), 3))
        cadena.Append(Campo(NumeroDeVuelo(numeroVuelo), 5))
        cadena.Append(DiaJuliano(fechaSalida))                  ' Fecha como día del año
        cadena.Append(Compartimento(clase))
        cadena.Append(Campo(AsientoBcbp(asiento), 4))
        cadena.Append(Campo(secuencia.ToString("D4"), 5))
        cadena.Append("1")                                     ' Estado: con pase emitido
        cadena.Append("00")                                    ' Sin datos condicionales

        Return cadena.ToString()
    End Function

    ''' <summary>"Juan Lopez Martinez" → "LOPEZ/JUAN". El estándar pide apellido,
    ''' barra y nombre, todo en mayúsculas y sin acentos.</summary>
    Public Shared Function NombreBcbp(nombreCompleto As String) As String
        If String.IsNullOrWhiteSpace(nombreCompleto) Then Return ""

        Dim partes = SoloAscii(nombreCompleto).ToUpper().
                     Split(" "c).Where(Function(p) p.Length > 0).ToArray()
        If partes.Length = 0 Then Return ""
        If partes.Length = 1 Then Return partes(0)

        ' En los nombres hispanos el primer apellido es la segunda palabra
        Dim nombre = partes(0)
        Dim apellido = partes(1)
        Return $"{apellido}/{nombre}"
    End Function

    ''' <summary>El código de barras es ASCII: los acentos y la eñe se transliteran.</summary>
    Public Shared Function SoloAscii(texto As String) As String
        If String.IsNullOrEmpty(texto) Then Return ""

        Dim descompuesto = texto.Normalize(NormalizationForm.FormD)
        Dim limpio As New StringBuilder()

        For Each caracter In descompuesto
            Dim categoria = CharUnicodeInfo.GetUnicodeCategory(caracter)
            If categoria = UnicodeCategory.NonSpacingMark Then Continue For
            If AscW(caracter) < 32 OrElse AscW(caracter) > 126 Then Continue For
            limpio.Append(caracter)
        Next

        Return limpio.ToString()
    End Function

    ''' <summary>Y económica, C ejecutiva, F primera clase.</summary>
    Public Shared Function Compartimento(clase As String) As String
        Dim limpia = SoloAscii(If(clase, "")).ToUpper()
        If limpia.StartsWith("PRIMERA") Then Return "F"
        If limpia.StartsWith("EJECUTIVA") Then Return "C"
        Return "Y"
    End Function

    ''' <summary>"12C" → "012C": tres dígitos de fila y la letra.</summary>
    Public Shared Function AsientoBcbp(asiento As String) As String
        Dim limpio = SoloAscii(If(asiento, "")).ToUpper().Trim()
        If limpio.Length = 0 Then Return ""

        Dim letra = limpio.Last()
        Dim fila As Integer
        Integer.TryParse(New String(limpio.TakeWhile(AddressOf Char.IsDigit).ToArray()), fila)
        Return fila.ToString("D3") & letra
    End Function

    ''' <summary>Día del año en tres dígitos: el 3 de agosto es el 215.</summary>
    Public Shared Function DiaJuliano(fecha As DateTime) As String
        Return fecha.DayOfYear.ToString("D3")
    End Function

    Private Shared Function NumeroDeVuelo(numero As Integer) As String
        ' Cuatro dígitos y un espacio final reservado al sufijo operativo
        Return Math.Abs(numero Mod 10000).ToString("D4") & " "
    End Function

    ''' <summary>Recorta o rellena con espacios hasta el ancho exacto del campo.
    ''' El BCBP no lleva separadores: si un campo se corre, todos los siguientes
    ''' se leen mal.</summary>
    Private Shared Function Campo(valor As String, ancho As Integer) As String
        Dim texto = If(valor, "")
        If texto.Length > ancho Then Return texto.Substring(0, ancho)
        Return texto.PadRight(ancho)
    End Function

    ''' <summary>Saca el número del código de vuelo del sistema ("AA402-0308" → 402).</summary>
    Public Shared Function NumeroDesdeCodigo(codigoVuelo As String) As Integer
        If String.IsNullOrWhiteSpace(codigoVuelo) Then Return 0

        Dim antesDelGuion = codigoVuelo.Split("-"c)(0)
        Dim digitos = New String(antesDelGuion.Where(AddressOf Char.IsDigit).ToArray())

        Dim numero As Integer
        Integer.TryParse(digitos, numero)
        Return numero
    End Function
End Class
