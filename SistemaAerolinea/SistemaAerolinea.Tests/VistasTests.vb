Imports System.Windows
Imports System.Windows.Controls
Imports Xunit

''' <summary>Pruebas de humo de la interfaz. Ni un StaticResource mal escrito ni un
''' ComboBox mal inicializado rompen la compilación: revientan al abrir la pantalla,
''' delante del usuario. Estas pruebas cargan el sistema de diseño, construyen TODAS
''' las vistas y además muestran cada página para que su manejador de Loaded se
''' ejecute de verdad.</summary>
Public Class VistasTests

    <Fact>
    Public Sub TodasLasVistasSeConstruyenSinError()
        Dim fallos As New List(Of String)()

        HiloUi.Ejecutar(
            Sub()
                Dim vistas As New Dictionary(Of String, Func(Of Object)) From {
                    {"PanelPage", Function() New PanelPage()},
                    {"ItinerarioPage", Function() New ItinerarioPage()},
                    {"ReservarPage", Function() New ReservarPage()},
                    {"ReservasPage", Function() New ReservasPage()},
                    {"PagosPage", Function() New PagosPage()},
                    {"VuelosPage", Function() New VuelosPage()},
                    {"PasajerosPage", Function() New PasajerosPage()},
                    {"CatalogosPage", Function() New CatalogosPage()},
                    {"UsuariosPage", Function() New UsuariosPage()},
                    {"BitacoraPage", Function() New BitacoraPage()},
                    {"MisVuelosPage", Function() New MisVuelosPage()},
                    {"MiPerfilPage", Function() New MiPerfilPage()},
                    {"LogoAlas", Function() New LogoAlas()},
                    {"CajaClaveVisible", Function() New CajaClaveVisible()},
                    {"SplashWindow", Function() New SplashWindow()},
                    {"LoginWindow", Function() New LoginWindow()},
                    {"MainWindow", Function() New MainWindow()},
                    {"RegisterWindow", Function() New RegisterWindow()},
                    {"RecuperarClaveWindow", Function() New RecuperarClaveWindow()},
                    {"CambiarContrasenaObligatoriaWindow", Function() New CambiarContrasenaObligatoriaWindow()},
                    {"MiCuentaWindow", Function() New MiCuentaWindow()},
                    {"NuevoPasajeroWindow", Function() New NuevoPasajeroWindow()},
                    {"DialogoAlas", Function() New DialogoAlas()},
                    {"PaseAbordarWindow", Function() New PaseAbordarWindow(1)},
                    {"PagarWindow", Function() New PagarWindow(1)},
                    {"CodigosRespaldoWindow", Function() New CodigosRespaldoWindow(
                        {"ABCD-EFGH-JKLM", "NPQR-STUV-WXYZ"}, "prueba")}
                }

                For Each vista In vistas
                    Try
                        ' .Invoke() explícito, y NO `vista.Value()`: en VB los
                        ' paréntesis son opcionales al leer una propiedad, así que
                        ' `vista.Value()` devolvía el propio Func —que nunca es
                        ' Nothing— en vez de ejecutarlo. La prueba pasaba siempre
                        ' sin llegar a construir una sola vista.
                        Assert.NotNull(vista.Value.Invoke())
                    Catch ex As Exception
                        fallos.Add($"{vista.Key}: {ex.GetType().Name} — {ex.Message}")
                    End Try
                Next
            End Sub)

        Assert.True(fallos.Count = 0,
                    "Vistas que no se pudieron construir:" & Environment.NewLine &
                    String.Join(Environment.NewLine, fallos))
    End Sub

    ''' <summary>Construir una página no ejecuta su manejador de Loaded, y ahí es
    ''' donde se llenan las listas desplegables. Un ComboBox que ya trae elementos
    ''' escritos en el XAML y además recibe ItemsSource desde código revienta con
    ''' "La colección de elementos debe estar vacía antes de usar ItemsSource",
    ''' pero solo al abrir la pantalla. Esta prueba mete cada página en una ventana
    ''' para que Loaded se dispare de verdad.
    '''
    ''' Se excluye ReservarPage: su Loaded consulta la base de datos y, si no
    ''' responde, abre un diálogo modal que dejaría la prueba colgada.</summary>
    <Fact>
    Public Sub LasPaginasSobrevivenASuManejadorDeLoaded()
        Dim fallos As New List(Of String)()

        HiloUi.Ejecutar(
            Sub()
                Dim paginas As New Dictionary(Of String, Func(Of UserControl)) From {
                    {"PanelPage", Function() New PanelPage()},
                    {"ItinerarioPage", Function() New ItinerarioPage()},
                    {"ReservasPage", Function() New ReservasPage()},
                    {"PagosPage", Function() New PagosPage()},
                    {"VuelosPage", Function() New VuelosPage()},
                    {"PasajerosPage", Function() New PasajerosPage()},
                    {"CatalogosPage", Function() New CatalogosPage()},
                    {"UsuariosPage", Function() New UsuariosPage()},
                    {"BitacoraPage", Function() New BitacoraPage()},
                    {"MisVuelosPage", Function() New MisVuelosPage()},
                    {"MiPerfilPage", Function() New MiPerfilPage()}
                }

                For Each pagina In paginas
                    Dim ventana As Window = Nothing
                    Try
                        ventana = New Window With {
                            .Content = pagina.Value(), .Width = 1280, .Height = 800,
                            .WindowStyle = WindowStyle.None, .ShowInTaskbar = False,
                            .Left = -4000, .Top = -4000
                        }
                        ventana.Show()
                        ventana.UpdateLayout()
                    Catch ex As Exception
                        fallos.Add($"{pagina.Key}: {ex.GetType().Name} — {ex.Message}")
                    Finally
                        If ventana IsNot Nothing Then ventana.Close()
                    End Try
                Next
            End Sub)

        Assert.True(fallos.Count = 0,
                    "Páginas que fallaron al mostrarse:" & Environment.NewLine &
                    String.Join(Environment.NewLine, fallos))
    End Sub
End Class
