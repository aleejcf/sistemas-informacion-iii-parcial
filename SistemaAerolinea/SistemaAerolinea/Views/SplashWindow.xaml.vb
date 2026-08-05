Imports System.Diagnostics
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Threading

''' <summary>Pantalla de bienvenida. Mientras corre la animación se prueba la
''' conexión real a SQL Server, así el usuario se entera del problema aquí y no
''' al intentar iniciar sesión.
'''
''' No hay barra de progreso: el progreso es el vuelo. El avión recorre la ruta
''' dejando su estela detrás y el aeropuerto de destino se enciende al llegar.
'''
''' El vuelo dura unos cinco segundos aunque la base conteste al instante. No es
''' relleno: es el rato que tarda el sistema en terminar de arrancar, y verlo
''' volar se lleva mejor que ver una ventana congelada.</summary>
Public Class SplashWindow

    Private WithEvents temporizador As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(16)}

    ''' <summary>Cuánto dura cada tramo del vuelo, en segundos.
    '''
    ''' El avance se calcula con el reloj y NO sumando un poco en cada fotograma:
    ''' DispatcherTimer no garantiza el intervalo que se le pide —pedirle 16 ms y
    ''' que dispare cada 28 es lo normal— así que contar fotogramas hacía que el
    ''' splash durase casi el doble de lo previsto, y en otra máquina duraría otra
    ''' cosa. Atado al reloj, dura lo que dice aquí en cualquier equipo.</summary>
    Private Const SEGUNDOS_CRUCERO As Double = 3.0
    Private Const SEGUNDOS_APROXIMACION As Double = 0.8

    ''' <summary>Hasta dónde se vuela sin saber nada de la base de datos. El resto
    ''' del trayecto solo se recorre cuando ya ha contestado.</summary>
    Private Const ESPERA_A_LA_BASE As Double = 70

    Private ReadOnly reloj As New Stopwatch()
    Private segundoDeAproximacion As Double = -1

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

        ' La marca entra escalonada, línea por línea, en vez de aparecer entera
        TransicionVentana.EntradaEnCascada(panelMarca, pasoMs:=85)
        LatirDestino()

        Task.Run(Sub()
                     bdConError = Not Db.HayConexion()
                     bdLista = True
                 End Sub)

        reloj.Start()
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
        Dim segundos = reloj.Elapsed.TotalSeconds

        If progreso < ESPERA_A_LA_BASE Then
            ' Tramo de crucero: se vuela a ciegas mientras la base contesta
            progreso = Math.Min(ESPERA_A_LA_BASE, ESPERA_A_LA_BASE * segundos / SEGUNDOS_CRUCERO)

        ElseIf bdLista Then
            ' Aproximación: empieza a contar desde que la base contestó, no desde
            ' que arrancó el splash, o una base lenta se saltaría el tramo entero
            If segundoDeAproximacion < 0 Then segundoDeAproximacion = segundos

            Dim avance = (segundos - segundoDeAproximacion) / SEGUNDOS_APROXIMACION
            progreso = ESPERA_A_LA_BASE + (100 - ESPERA_A_LA_BASE) * Math.Min(1, avance)
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
        ElseIf progreso < ESPERA_A_LA_BASE Then
            lblEstado.Text = "Cargando itinerarios…"
        ElseIf progreso < 100 Then
            lblEstado.Text = "En ruta…"
        Else
            ' El avión acaba de aterrizar, así que aquí ya no toca "listos para despegar"
            lblEstado.Text = If(bdConError, "⚠  Sin conexión a la base de datos", "Bienvenido a bordo")
        End If
    End Sub

    ''' <summary>El aeropuerto de destino late flojito mientras espera al avión.
    ''' Se anima desde el código, no desde el guion del XAML, para que el destello
    ''' de la llegada pueda sustituir a este latido sin pelearse con él.</summary>
    Private Sub LatirDestino()
        puntoDestino.BeginAnimation(UIElement.OpacityProperty,
            New DoubleAnimation(0.22, 0.55, TimeSpan.FromMilliseconds(1150)) With {
                .AutoReverse = True,
                .RepeatBehavior = RepeatBehavior.Forever
            })
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

        Dim despedida As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(900)}
        AddHandler despedida.Tick,
            Sub()
                despedida.Stop()
                Dim fundido As New DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(320))
                AddHandler fundido.Completed, Sub() Terminar()
                BeginAnimation(UIElement.OpacityProperty, fundido)
            End Sub
        despedida.Start()
    End Sub

    Private Sub EncenderDestino()
        ' Sustituye al latido de la espera: la última animación que se lanza sobre
        ' una propiedad es la que manda
        puntoDestino.BeginAnimation(UIElement.OpacityProperty,
            New DoubleAnimation(1, TimeSpan.FromMilliseconds(320)))

        ' El halo se abre y se apaga solo, como el destello de una baliza
        haloDestino.BeginAnimation(UIElement.OpacityProperty,
            New DoubleAnimation(0.5, TimeSpan.FromMilliseconds(280)) With {
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
