Imports System.IO
Imports System.Windows.Media

''' <summary>Efectos de sonido de la interfaz (click, éxito, error), a volumen bajo.
''' Usa MediaPlayer en vez de System.Media.SoundPlayer porque este último no permite
''' ajustar el volumen. Los .wav viven en Resources\ y se copian junto al .exe
''' (ver ParqueaderoWPF.vbproj), así que se reproducen directo desde ahí.</summary>
Public Class Sonido

    ''' <summary>Volumen de los efectos, de 0.0 a 1.0. Por defecto bajo (15%).</summary>
    Public Shared Property Volumen As Double = 0.15

    Public Shared Sub Click()
        Reproducir("click.wav")
    End Sub

    Public Shared Sub Exito()
        Reproducir("exito.wav")
    End Sub

    Public Shared Sub [Error]()
        Reproducir("error.wav")
    End Sub

    ''' <summary>Traduce el ícono del diálogo al efecto más apropiado.</summary>
    Public Shared Sub Reproducir(icono As MessageBoxImage)
        Select Case icono
            Case MessageBoxImage.Error, MessageBoxImage.Warning
                [Error]()
            Case MessageBoxImage.Question
                Click()
            Case Else
                Exito()
        End Select
    End Sub

    Private Shared Sub Reproducir(archivo As String)
        Try
            Dim ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", archivo)
            If Not File.Exists(ruta) Then Return

            Dim reproductor As New MediaPlayer()
            reproductor.Volume = Math.Max(0.0, Math.Min(1.0, Volumen))
            reproductor.Open(New Uri(ruta, UriKind.Absolute))
            reproductor.Play()

            ' Se libera solo al terminar, para no acumular instancias de MediaPlayer
            AddHandler reproductor.MediaEnded, Sub() reproductor.Close()
        Catch ex As Exception
            ' El sonido es un extra: un problema de audio nunca debe romper la app
            Registro.Advertencia($"Error reproduciendo sonido '{archivo}': {ex.Message}")
        End Try
    End Sub
End Class
