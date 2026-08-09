Imports System.IO
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

''' <summary>Portada de cada título: un jpg por código de libro, guardado aparte
''' de la base de datos -igual que PARKO guarda las fotos de sus parqueaderos-
''' para no tener que meter binarios en SQL Server.</summary>
Public Class Portada

    Public Shared ReadOnly Property Carpeta As String
        Get
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "portadas")
        End Get
    End Property

    Private Shared Function Ruta(codigo As String) As String
        Return Path.Combine(Carpeta, codigo.Trim().ToUpper() & ".jpg")
    End Function

    ''' <summary>Nothing si el título todavía no tiene portada.</summary>
    Public Shared Function Cargar(codigo As String) As BitmapImage
        Dim archivo = Ruta(codigo)
        If Not File.Exists(archivo) Then Return Nothing

        ' Se carga en memoria para no bloquear el archivo y poder reemplazarlo
        Dim bmp As New BitmapImage()
        bmp.BeginInit()
        bmp.CacheOption = BitmapCacheOption.OnLoad
        bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache
        bmp.UriSource = New Uri(archivo)
        bmp.EndInit()
        Return bmp
    End Function

    Public Shared Sub Guardar(codigo As String, archivoOrigen As String)
        Directory.CreateDirectory(Carpeta)
        File.Copy(archivoOrigen, Ruta(codigo), overwrite:=True)
    End Sub

    ''' <summary>Busca la portada en Open Library por ISBN y la guarda si existe.
    ''' Devuelve False cuando ese ISBN no tiene portada ahí -no es un error: pasa
    ''' seguido con ediciones locales o poco conocidas- para que quien llama decida
    ''' cómo avisarlo.</summary>
    Public Shared Async Function DescargarPorIsbnAsync(codigo As String, isbn As String) As Task(Of Boolean)
        Dim limpio = Regex.Replace(isbn.Trim(), "[\s-]", "")
        If limpio = "" Then Return False

        Using cliente As New HttpClient()
            Dim url = $"https://covers.openlibrary.org/b/isbn/{limpio}-L.jpg?default=false"
            Dim respuesta = Await cliente.GetAsync(url)
            If Not respuesta.IsSuccessStatusCode Then Return False

            Dim bytes = Await respuesta.Content.ReadAsByteArrayAsync()
            Directory.CreateDirectory(Carpeta)
            Await File.WriteAllBytesAsync(Ruta(codigo), bytes)
            Return True
        End Using
    End Function
End Class
