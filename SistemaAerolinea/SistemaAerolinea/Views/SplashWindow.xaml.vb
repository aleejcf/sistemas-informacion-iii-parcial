Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Threading

''' <summary>Pantalla de bienvenida. Mientras corre la animación se prueba la
''' conexión real a SQL Server, así el usuario se entera del problema aquí y no
''' al intentar iniciar sesión.
'''
''' No hay barra de progreso: el progreso es el vuelo. El avión recorre la ruta
''' dejando su estela detrás y el aeropuerto de destino se enciende al llegar.
''' Y no hay espera artificial — en cuanto la base responde, el vuelo se completa
''' y la ventana cede el paso.</summary>
Public Class SplashWindow

    Private WithEvents temporizador As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(16)}

    Private progreso As Double = 0
    Private bdLista As Boolean = False
    Private bdConError As Boolean = False
    Private terminando As Boolean = False

    ''' <summary>La curva se muestrea una sola vez y de ahí salen todas las
    ''' posiciones. Recalcularla en cada fotograma sería tirar trabajo: la ruta
    ''' no cambia, lo único que avanza es hasta dónde se ha volado.</summary>
    Private Const MUESTRAS As Integer = 240

    Private puntos(MUESTRAS) As Point
    Private angulos(MUESTRAS) As Double
    Private rutaLista As Boolean = False

    Private Sub SplashWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        MuestrearRuta()
        ColocarAvion(0)

        Task.Run(Sub()
                     bdConError = Not Db.HayConexion()
                     bdLista = True
                 End Sub)
        temporizador.Start()
    End Sub

    ''' <summary>Recorre la curva de la ruta guardando, para cada tramo, dónde está
    ''' y hacia dónde apunta. El ángulo sale de la tangente de la propia curva, que
    ''' es lo que hace que el avión vaya siempre encarado hacia donde vuela en vez
    ''' de resbalar de lado.</summary>
    Private Sub MuestrearRuta()
        Try
            Dim curva = PathGeometry.CreateFromGeometry(ruta.Data)
            If curva Is Nothing Then Return

            For i = 0 To MUESTRAS
                Dim donde As Point, tangente As Point
                curva.GetPointAtFractionLength(i / MUESTRAS, donde, tangente)

                puntos(i) = donde
                ' El avión del logotipo apunta hacia arriba, así que se le suman
                ' los 90° que lo llevan de "mirando al norte" a "mirando al este"
                angulos(i) = Math.Atan2(tangente.Y, tangente.X) * 180 / Math.PI + 90
            Next

            rutaLista = True

        Catch ex As Exception
            ' Un splash no puede impedir entrar al sistema: si la curva fallara,
            ' se sigue sin animación de vuelo
            Registro.Advertencia($"No se pudo trazar la ruta del splash: {ex.Message}")
        End Try
    End Sub

    Private Sub temporizador_Tick(sender As Object, e As EventArgs) Handles temporizador.Tick
        ' Avanza hasta 70 por su cuenta; de ahí en adelante solo si la base ya
        ' respondió. Si responde rápido, el vuelo dura poco más de un segundo.
        If progreso < 70 Then
            progreso += 1.15
        ElseIf bdLista Then
            progreso += 2.6
        End If
        If progreso > 100 Then progreso = 100

        ' Suavizado: despega despacio, cruza rápido y llega frenando, que es como
        ' vuela un avión y no como se mueve una barra de progreso
        Dim t = progreso / 100.0
        ColocarAvion(t * t * (3 - 2 * t))

        ActualizarEstado()

        If progreso >= 100 Then Aterrizar()
    End Sub

    ''' <summary>Pone el avión en el punto que le toca y dibuja la estela hasta ahí.</summary>
    Private Sub ColocarAvion(fraccion As Double)
        If Not rutaLista Then Return

        Dim indice = CInt(Math.Round(Math.Max(0, Math.Min(1, fraccion)) * MUESTRAS))
        Dim donde = puntos(indice)

        Canvas.SetLeft(avion, donde.X - avion.Width / 2)
        Canvas.SetTop(avion, donde.Y - avion.Height / 2)
        giroAvion.Angle = angulos(indice)

        DibujarEstela(indice)
    End Sub

    ''' <summary>La estela es la parte de la ruta que ya se voló: los mismos puntos
    ''' de la curva, hasta donde va el avión.</summary>
    Private Sub DibujarEstela(hasta As Integer)
        If hasta < 2 Then
            estela.Data = Nothing
            Return
        End If

        Dim recorrido As New PointCollection()
        For i = 1 To hasta
            recorrido.Add(puntos(i))
        Next

        Dim figura As New PathFigure With {.StartPoint = puntos(0)}
        figura.Segments.Add(New PolyLineSegment(recorrido, isStroked:=True))

        Dim trazo As New PathGeometry()
        trazo.Figures.Add(figura)
        estela.Data = trazo
    End Sub

    Private Sub ActualizarEstado()
        If progreso < 25 Then
            lblEstado.Text = "Encendiendo motores…"
        ElseIf progreso < 50 Then
            lblEstado.Text = "Conectando con la torre de control…"
        ElseIf progreso < 70 Then
            lblEstado.Text = "Cargando itinerarios…"
        ElseIf progreso < 100 Then
            lblEstado.Text = "En ruta…"
        Else
            ' El avión acaba de aterrizar, así que aquí ya no toca "listos para despegar"
            lblEstado.Text = If(bdConError, "⚠  Sin conexión a la base de datos", "Bienvenido a bordo")
        End If
    End Sub

    ''' <summary>Enciende el aeropuerto de destino y deja ver el aterrizaje un
    ''' instante antes de dar paso al inicio de sesión.</summary>
    Private Sub Aterrizar()
        If terminando Then Return
        terminando = True
        temporizador.Stop()

        EncenderDestino()

        ' Sin conexión hay que avisar, y el aviso no debe pelearse con el fundido
        If bdConError Then
            Terminar()
            Return
        End If

        Dim despedida As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(620)}
        AddHandler despedida.Tick,
            Sub()
                despedida.Stop()
                Dim fundido As New DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(280))
                AddHandler fundido.Completed, Sub() Terminar()
                BeginAnimation(UIElement.OpacityProperty, fundido)
            End Sub
        despedida.Start()
    End Sub

    Private Sub EncenderDestino()
        puntoDestino.BeginAnimation(UIElement.OpacityProperty,
            New DoubleAnimation(1, TimeSpan.FromMilliseconds(320)))
        lblDestino.BeginAnimation(UIElement.OpacityProperty,
            New DoubleAnimation(1, TimeSpan.FromMilliseconds(320)))

        ' El halo se abre y se apaga solo, como el destello de una baliza
        haloDestino.BeginAnimation(UIElement.OpacityProperty,
            New DoubleAnimation(0.5, TimeSpan.FromMilliseconds(260)) With {
                .AutoReverse = True
            })
    End Sub

    Private Sub Terminar()
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
    End Sub
End Class
