Imports System.IO
Imports System.Windows.Media

''' <summary>Música de fondo de la app. Vive durante toda la sesión (se inicia una sola
''' vez desde Application.xaml.vb y persiste entre ventanas).
'''
''' El MP3 no viene incluido en el proyecto — colócalo en:
'''   Resources\musica_fondo.mp3   (Build Action = None, Copy to Output Directory = PreserveNewest,
'''                                  ya configurado en el .vbproj para *.mp3 dentro de Resources\)
''' Si el archivo no existe, Iniciar() simplemente no hace nada (no revienta la app).
'''
''' Uso:
'''   MusicaService.Iniciar()     ' una vez, en Application_Startup
'''   MusicaService.Detener()     ' opcional, ej. al cerrar sesión
'''   MusicaService.AlternarMute() ' para un botón de silenciar en la UI
''' </summary>
Public Class MusicaService

    Private Shared _reproductor As MediaPlayer
    Private Shared _iniciado As Boolean = False
    Private Shared _volumenPrevio As Double = 0.5
    Private Shared _muteado As Boolean = False

    ''' <summary>Volumen de 0.0 (silencio) a 1.0 (máximo). Por defecto 0.5.</summary>
    Public Shared Property Volumen As Double
        Get
            Return If(_reproductor IsNot Nothing, _reproductor.Volume, 0.5)
        End Get
        Set(value As Double)
            If _reproductor IsNot Nothing Then
                _reproductor.Volume = Math.Max(0.0, Math.Min(1.0, value))
            End If
        End Set
    End Property

    Public Shared ReadOnly Property EstaMuteado As Boolean
        Get
            Return _muteado
        End Get
    End Property

    ''' <summary>Alterna entre silenciar y restaurar el volumen anterior. Devuelve True si quedó muteada.</summary>
    Public Shared Function AlternarMute() As Boolean
        If _reproductor Is Nothing Then Return _muteado

        If _muteado Then
            _reproductor.Volume = _volumenPrevio
            _muteado = False
        Else
            _volumenPrevio = If(_reproductor.Volume > 0, _reproductor.Volume, 0.5)
            _reproductor.Volume = 0.0
            _muteado = True
        End If

        Return _muteado
    End Function

    ''' <summary>Inicia la música de fondo. Idempotente: si ya está iniciada, no hace nada.</summary>
    Public Shared Sub Iniciar()
        If _iniciado Then Return

        Try
            Dim rutaMp3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "musica_fondo.mp3")
            If Not File.Exists(rutaMp3) Then
                Registro.Info("Música de fondo no encontrada (Resources\musica_fondo.mp3); se omite.")
                Return
            End If

            _reproductor = New MediaPlayer()
            _reproductor.Volume = 0.5
            AddHandler _reproductor.MediaEnded, AddressOf AlTerminar

            _reproductor.Open(New Uri(rutaMp3, UriKind.Absolute))
            _reproductor.Play()
            _iniciado = True
        Catch ex As Exception
            Registro.Advertencia($"Error iniciando música de fondo: {ex.Message}")
        End Try
    End Sub

    Public Shared Sub Detener()
        Try
            If _reproductor IsNot Nothing Then
                _reproductor.Stop()
                _reproductor.Close()
                _iniciado = False
            End If
        Catch ex As Exception
            Registro.Advertencia($"Error deteniendo música: {ex.Message}")
        End Try
    End Sub

    Public Shared Sub Pausar()
        _reproductor?.Pause()
    End Sub

    Public Shared Sub Reanudar()
        _reproductor?.Play()
    End Sub

    ' Loop manual: MediaPlayer no tiene una opción nativa de repetición
    Private Shared Sub AlTerminar(sender As Object, e As EventArgs)
        Try
            _reproductor.Position = TimeSpan.Zero
            _reproductor.Play()
        Catch ex As Exception
            Registro.Advertencia($"Error en el loop de la música: {ex.Message}")
        End Try
    End Sub
End Class
