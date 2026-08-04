Imports System.IO
Imports PdfSharp.Fonts

''' <summary>Le dice a PDFsharp de dónde sacar las tipografías.
'''
''' PDFsharp no trae fuentes incorporadas ni las busca solo: sin un resolutor
''' lanza "No appropriate font found for family name". Este las lee de la carpeta
''' de fuentes de Windows, que es donde ya están instaladas Segoe UI y Consolas —
''' las mismas dos que usa la interfaz del sistema, para que el PDF se vea igual
''' que la pantalla.</summary>
Public Class ResolutorFuentesPdf
    Implements IFontResolver

    ' Nombre interno de la cara → archivo dentro de la carpeta de fuentes
    Private Shared ReadOnly Archivos As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"SegoeUI", "segoeui.ttf"},
        {"SegoeUI-Bold", "segoeuib.ttf"},
        {"Consolas", "consola.ttf"},
        {"Consolas-Bold", "consolab.ttf"},
        {"Arial", "arial.ttf"},
        {"Arial-Bold", "arialbd.ttf"}
    }

    Private Shared ReadOnly Cache As New Dictionary(Of String, Byte())(StringComparer.OrdinalIgnoreCase)

    Public Function ResolveTypeface(familyName As String, isBold As Boolean,
                                    isItalic As Boolean) As FontResolverInfo _
                                    Implements IFontResolver.ResolveTypeface

        Dim familia = If(familyName, "").Replace(" ", "")

        ' Cualquier fuente que no sea monoespaciada cae en Segoe UI, que es la de la interfaz
        If Not familia.Equals("Consolas", StringComparison.OrdinalIgnoreCase) Then
            familia = "SegoeUI"
        End If

        Dim cara = If(isBold, $"{familia}-Bold", familia)
        If Not Archivos.ContainsKey(cara) Then cara = familia

        Return New FontResolverInfo(cara)
    End Function

    Public Function GetFont(faceName As String) As Byte() Implements IFontResolver.GetFont
        SyncLock Cache
            Dim datos As Byte() = Nothing
            If Cache.TryGetValue(faceName, datos) Then Return datos

            Dim archivo As String = Nothing
            If Not Archivos.TryGetValue(faceName, archivo) Then archivo = "arial.ttf"

            Dim ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), archivo)

            ' Si la tipografía no estuviera instalada, Arial sirve de red de seguridad:
            ' es preferible un PDF con otra letra que un PDF que no se genera.
            If Not File.Exists(ruta) Then
                ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf")
                Registro.Advertencia($"Tipografía no encontrada para el PDF: {archivo}. Se usa Arial.")
            End If

            datos = File.ReadAllBytes(ruta)
            Cache(faceName) = datos
            Return datos
        End SyncLock
    End Function
End Class
