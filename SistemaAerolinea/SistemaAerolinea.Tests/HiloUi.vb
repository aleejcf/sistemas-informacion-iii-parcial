Imports System.Threading
Imports System.Windows.Threading

''' <summary>Hilo de interfaz compartido por las pruebas visuales.
'''
''' WPF impone dos reglas que obligan a esto: solo puede existir un objeto
''' Application por proceso, y todo lo que se cree a partir de él pertenece al
''' hilo que lo creó. Con un único hilo STA vivo durante toda la corrida, las dos
''' se cumplen sin que las clases de prueba tengan que coordinarse entre ellas.</summary>
Public NotInheritable Class HiloUi

    Private Shared ReadOnly Candado As New Object()
    Private Shared despachador As Dispatcher

    Private Sub New()
    End Sub

    Public Shared Function Ejecutar(Of T)(trabajo As Func(Of T)) As T
        Iniciar()
        Return despachador.Invoke(trabajo)
    End Function

    Public Shared Sub Ejecutar(trabajo As Action)
        Iniciar()
        despachador.Invoke(trabajo)
    End Sub

    Private Shared Sub Iniciar()
        SyncLock Candado
            If despachador IsNot Nothing Then Return

            Dim listo As New ManualResetEventSlim(False)

            Dim hilo As New Thread(
                Sub()
                    despachador = Dispatcher.CurrentDispatcher

                    ' Carga Application.xaml: la paleta, los estilos y los conversores
                    If Application.Current Is Nothing Then
                        Dim app As New SistemaAerolinea.Application()
                        app.InitializeComponent()
                    End If

                    listo.Set()
                    ' El despachador queda atendiendo peticiones hasta que termina la corrida
                    Dispatcher.Run()
                End Sub)

            hilo.IsBackground = True
            hilo.SetApartmentState(ApartmentState.STA)
            hilo.Start()

            If Not listo.Wait(TimeSpan.FromMinutes(1)) Then
                Throw New TimeoutException("El hilo de interfaz de las pruebas no arrancó.")
            End If
        End SyncLock
    End Sub
End Class
