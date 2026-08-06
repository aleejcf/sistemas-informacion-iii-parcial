Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Shapes
Imports System.Windows.Threading

''' <summary>Pantalla de bienvenida. Mientras corre la animación se prueba la
''' conexión real a SQL Server, así el usuario se entera del problema aquí y no
''' al intentar iniciar sesión.
'''
''' No hay barra de progreso: el progreso son los lomos de la estantería, que se
''' encienden de izquierda a derecha. Y no hay espera artificial — en cuanto la
''' base responde, la carga se completa y la ventana cede el paso.</summary>
Public Class SplashWindow

    Private WithEvents temporizador As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(28)}

    Private progreso As Double = 0
    Private bdLista As Boolean = False
    Private bdConError As Boolean = False

    ''' <summary>Todos los lomos, de la repisa de arriba y la de abajo, en el orden
    ''' en que se van a encender.</summary>
    Private lomos As New List(Of Rectangle)

    Private Sub SplashWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        lomos.AddRange(repisaAlta.Children.OfType(Of Rectangle)())
        lomos.AddRange(repisaBaja.Children.OfType(Of Rectangle)())

        Task.Run(Sub()
                     ' Deja lista la plantilla del correo saliente la primera vez
                     ' que se abre el sistema, para que solo haya que rellenarla
                     CorreoService.CrearPlantillaSiFalta()
                     bdConError = Not Db.HayConexion()
                     bdLista = True
                 End Sub)
        temporizador.Start()
    End Sub

    Private Sub temporizador_Tick(sender As Object, e As EventArgs) Handles temporizador.Tick
        ' Avanza hasta 70 por su cuenta; de ahí en adelante solo si la base ya
        ' respondió. Si responde rápido, el splash dura poco más de un segundo.
        If progreso < 70 Then
            progreso += 3.2
        ElseIf bdLista Then
            progreso += 7.5
        End If
        If progreso > 100 Then progreso = 100

        EncenderLomos()
        ActualizarEstado()

        If progreso >= 100 Then Terminar()
    End Sub

    ''' <summary>Enciende tantos lomos como corresponda al avance. Cada uno se
    ''' aclara con su propia animación para que la estantería se llene con ritmo
    ''' en vez de saltar de golpe.</summary>
    Private Sub EncenderLomos()
        Dim encendidos = CInt(Math.Floor(progreso / 100.0 * lomos.Count))

        For i = 0 To lomos.Count - 1
            Dim lomo = lomos(i)
            Dim deberiaEstarEncendido = i < encendidos

            ' `Tag` recuerda cuáles ya se animaron: sin esa marca se relanzaría la
            ' animación en cada tic y el lomo parpadearía.
            If deberiaEstarEncendido AndAlso lomo.Tag Is Nothing Then
                lomo.Tag = "encendido"
                lomo.Fill = If(i Mod 3 = 0,
                               CType(TryFindResource("BrushAcento"), Brush),
                               Brushes.White)
                lomo.BeginAnimation(UIElement.OpacityProperty,
                    New DoubleAnimation(0.13, If(i Mod 3 = 0, 0.95, 0.75),
                                        TimeSpan.FromMilliseconds(260)))
            End If
        Next
    End Sub

    Private Sub ActualizarEstado()
        If progreso < 30 Then
            lblEstado.Text = "Abriendo la sala de lectura…"
        ElseIf progreso < 60 Then
            lblEstado.Text = "Ordenando el catálogo…"
        ElseIf progreso < 95 Then
            lblEstado.Text = "Revisando los préstamos del día…"
        Else
            lblEstado.Text = If(bdConError, "⚠  Sin conexión a la base de datos", "Biblioteca abierta")
        End If
    End Sub

    Private Sub Terminar()
        temporizador.Stop()

        If bdConError Then
            DialogoBiblioteca.Show(
                $"No se pudo conectar con la base de datos {Db.BASE_DATOS} en {Db.SERVIDOR}." & vbCrLf & vbCrLf &
                "Verifica que SQL Server esté encendido y que los scripts de la carpeta Scripts " &
                "ya se hayan ejecutado. Puedes seguir abriendo el sistema, pero no podrás iniciar sesión.",
                "Sin conexión", MessageBoxButton.OK, MessageBoxImage.Warning)
        End If

        Dim login As New LoginWindow()
        login.Show()
        Me.Close()
    End Sub
End Class
