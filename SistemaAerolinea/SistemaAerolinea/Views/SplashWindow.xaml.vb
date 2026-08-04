Imports System.Windows.Threading

''' <summary>Pantalla de bienvenida. Mientras corre la animación se prueba la
''' conexión real a SQL Server, así el usuario se entera del problema aquí y no
''' al intentar iniciar sesión.</summary>
Public Class SplashWindow

    Private WithEvents temporizador As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(30)}
    Private progreso As Double = 0
    Private bdLista As Boolean = False
    Private bdConError As Boolean = False

    Private Sub SplashWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Task.Run(Sub()
                     bdConError = Not Db.HayConexion()
                     bdLista = True
                 End Sub)
        temporizador.Start()
    End Sub

    Private Sub temporizador_Tick(sender As Object, e As EventArgs) Handles temporizador.Tick
        ' Avanza rápido hasta 90 %; el último tramo espera a que responda la base de datos
        If progreso < 90 Then
            progreso += 1.5
        ElseIf bdLista Then
            progreso += 2.5
        End If
        If progreso > 100 Then progreso = 100
        barra.Value = progreso

        If progreso < 30 Then
            lblEstado.Text = "Encendiendo motores…"
        ElseIf progreso < 60 Then
            lblEstado.Text = "Conectando con la torre de control…"
        ElseIf progreso < 90 Then
            lblEstado.Text = "Cargando itinerarios…"
        Else
            lblEstado.Text = If(bdConError, "⚠ Sin conexión a la base de datos", "Listos para despegar")
        End If

        If progreso >= 100 Then
            temporizador.Stop()

            If bdConError Then
                DialogoAlas.Show(
                    $"No se pudo conectar con la base de datos {Db.BASE_DATOS} en {Db.SERVIDOR}." & vbCrLf & vbCrLf &
                    "Verifica que SQL Server esté encendido y que los scripts de la carpeta Scripts " &
                    "ya se hayan ejecutado. Puedes seguir abriendo el sistema, pero no podrás iniciar sesión.",
                    "Sin conexión", MessageBoxButton.OK, MessageBoxImage.Warning)
            End If

            Dim login As New LoginWindow()
            login.Show()
            Me.Close()
        End If
    End Sub
End Class
