Imports System.Threading
Imports System.Windows
Imports Xunit

''' <summary>Prueba de humo de la interfaz: construye todas las ventanas, páginas
''' y controles del sistema.
'''
''' Un `StaticResource` mal escrito no da error de compilación — revienta en tiempo
''' de ejecución, y solo cuando alguien abre esa pantalla. Esta prueba abre todas
''' de una vez, así el error aparece aquí y no en medio de una demostración.
'''
''' No toca la base de datos: construir una vista no dispara su evento Loaded,
''' que es donde cada página consulta sus datos.</summary>
Public Class VistasTests

    ''' <summary>WPF solo funciona en un hilo STA y admite una sola Application por
    ''' proceso. Se crea a mano aquí porque las pruebas corren sin ella.</summary>
    Private Shared Sub EnHiloDeInterfaz(trabajo As Action)
        Dim error_ As Exception = Nothing

        Dim hilo As New Thread(
            Sub()
                Try
                    If Application.Current Is Nothing Then
                        Dim app As New SistemaBiblioteca.Application()
                        app.InitializeComponent()      ' carga el diccionario de estilos
                    End If
                    trabajo()
                Catch ex As Exception
                    error_ = ex
                End Try
            End Sub)

        hilo.SetApartmentState(ApartmentState.STA)
        hilo.Start()
        hilo.Join()

        If error_ IsNot Nothing Then Throw New Exception(error_.Message, error_)
    End Sub

    ''' <summary>Todo junto en una sola prueba porque solo puede existir una
    ''' Application por proceso: repartirlo en varias haría fallar a la segunda.</summary>
    <Fact>
    Public Sub TodasLasVistasSeConstruyenSinErroresDeRecursos()
        EnHiloDeInterfaz(
            Sub()
                ' ---- Controles compartidos ----
                Assert.NotNull(New LogoBiblioteca())
                Assert.NotNull(New CajaClaveVisible())

                ' ---- Ventanas ----
                Assert.NotNull(New SplashWindow())
                Assert.NotNull(New LoginWindow())
                Assert.NotNull(New RegisterWindow())
                Assert.NotNull(New RecuperarClaveWindow())
                Assert.NotNull(New CambiarContrasenaObligatoriaWindow())
                Assert.NotNull(New MiCuentaWindow())
                Assert.NotNull(New MainWindow())
                Assert.NotNull(New SeleccionarSocioWindow())
                Assert.NotNull(New DialogoBiblioteca())

                ' ---- Páginas ----
                Assert.NotNull(New PanelPage())
                Assert.NotNull(New PrestarPage())
                Assert.NotNull(New PrestamosPage())
                Assert.NotNull(New MultasPage())
                Assert.NotNull(New CatalogoPage())
                Assert.NotNull(New LibrosPage())
                Assert.NotNull(New CatalogosPage())
                Assert.NotNull(New SociosPage())
                Assert.NotNull(New ReservasPage())
                Assert.NotNull(New UsuariosPage())
                Assert.NotNull(New BitacoraPage())
            End Sub)
    End Sub

    ''' <summary>El sistema de diseño tiene que traer todos los recursos que las
    ''' vistas piden por nombre.</summary>
    <Fact>
    Public Sub ElSistemaDeDisenoDefineTodosSusRecursos()
        EnHiloDeInterfaz(
            Sub()
                Dim recursos = {
                    "BrushFondo", "BrushLateral", "BrushLateralHover", "BrushPrimario",
                    "BrushPrimarioSuave", "BrushAcento", "BrushAcentoSuave", "BrushExito",
                    "BrushExitoSuave", "BrushPeligro", "BrushPeligroSuave", "BrushAdvertencia",
                    "BrushAdvSuave", "BrushGris", "BrushTexto", "BrushTextoSuave", "BrushBorde",
                    "BrushSeleccion", "BrushBlanco", "BrushCuero", "BrushDorado",
                    "IconoApp", "GeoLibro", "GeoLomo",
                    "BotonPrimario", "BotonExito", "BotonPeligro", "BotonGris", "BotonAcento",
                    "BotonContorno", "BotonPlano", "BotonFila",
                    "BotonRiel", "RotuloRiel", "BotonIcono", "BotonIconoClaro",
                    "ChipUsuario", "OpcionCuenta", "CajaBusquedaGlobal", "PatronEstanteria",
                    "PasoAsistente", "PasoAsistenteActivo",
                    "CajaTexto", "CajaTextoLarga", "CajaClave",
                    "BarraCarga", "BarraAcervo",
                    "TituloPagina", "TituloTarjeta", "Subtitulo", "Etiqueta", "Cifra",
                    "Codigo", "TituloLibro",
                    "EnlaceSutil", "EnlaceEnfasis",
                    "Tarjeta", "TarjetaClicable", "TarjetaFlotante",
                    "Insignia", "TextoInsignia", "LineaPunteada",
                    "EstadoAColor", "EstadoAColorFondo", "VacioAVisible", "PorcentajeAAncho",
                    "BooleanoAVisible", "DiasRestantesATexto", "ConteoAVisible"
                }

                For Each nombre In recursos
                    Assert.True(Application.Current.Resources.Contains(nombre),
                                $"Falta el recurso '{nombre}' en Application.xaml")
                Next
            End Sub)
    End Sub
End Class
