Imports System.Windows.Threading

Public Class SplashWindow

    Private WithEvents temporizador As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(30)}
    Private progreso As Double = 0
    Private bdLista As Boolean = False
    Private bdConError As Boolean = False

    Private Sub SplashWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Mientras corre la animación, se prueba la conexión real a la base de datos
        Task.Run(Sub()
                     Try
                         Db.Escalar("SELECT 1")
                     Catch
                         bdConError = True
                     End Try
                     bdLista = True
                 End Sub)
        temporizador.Start()
    End Sub

    Private Sub temporizador_Tick(sender As Object, e As EventArgs) Handles temporizador.Tick
        ' Avanza rápido hasta 90 %; el tramo final espera a que responda la base de datos
        If progreso < 90 Then
            progreso += 1.4
        ElseIf bdLista Then
            progreso += 2.5
        End If
        If progreso > 100 Then progreso = 100
        barra.Value = progreso

        If progreso < 30 Then
            lblEstado.Text = "Iniciando el sistema…"
        ElseIf progreso < 60 Then
            lblEstado.Text = "Conectando a la base de datos…"
        ElseIf progreso < 90 Then
            lblEstado.Text = "Cargando módulos…"
        Else
            lblEstado.Text = If(bdConError, "⚠ Sin conexión a la base de datos", "¡Todo listo!")
        End If

        If progreso >= 100 Then
            temporizador.Stop()
            Dim login As New LoginWindow()
            login.Show()
            Me.Close()
        End If
    End Sub
End Class
