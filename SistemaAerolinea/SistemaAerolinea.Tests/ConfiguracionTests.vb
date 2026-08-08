Imports System.IO
Imports Xunit

''' <summary>Pruebas de la lectura de appsettings.json.
'''
''' Todas escriben un archivo de verdad en una carpeta temporal y lo leen: si se
''' probara con un JSON de mentira en memoria no se estaría comprobando lo único
''' que importa, que es que el archivo del disco realmente mande sobre los
''' valores quemados en el código.</summary>
Public Class ConfiguracionTests
    Implements IDisposable

    Private ReadOnly Carpeta As String

    Public Sub New()
        Carpeta = Path.Combine(Path.GetTempPath(), "cfg_" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(Carpeta)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            Directory.Delete(Carpeta, recursive:=True)
        Catch
            ' Una carpeta temporal que no se pudo borrar no invalida la prueba
        End Try
    End Sub

    Private Function ConContenido(json As String) As Configuracion
        Dim ruta = Path.Combine(Carpeta, Configuracion.ARCHIVO)
        File.WriteAllText(ruta, json)
        Return New Configuracion(ruta)
    End Function

    <Fact>
    Public Sub El_archivo_manda_sobre_los_valores_del_codigo()
        Dim cfg = ConContenido("{""Db"":{""Servidor"":""PROFESOR\\SQLEXPRESS"",""BaseDatos"":""otra_bd"",""SegundosDeEspera"":30}}")

        Assert.Equal("PROFESOR\SQLEXPRESS", cfg.LeerServidor())
        Assert.Equal("otra_bd", cfg.LeerBaseDatos())
        Assert.Equal(30, cfg.LeerSegundosDeEspera())
    End Sub

    <Fact>
    Public Sub Sin_archivo_se_usan_los_valores_por_defecto()
        Dim cfg As New Configuracion(Path.Combine(Carpeta, "este-archivo-no-existe.json"))

        Assert.Equal(Configuracion.SERVIDOR_POR_DEFECTO, cfg.LeerServidor())
        Assert.Equal(Configuracion.BASE_DATOS_POR_DEFECTO, cfg.LeerBaseDatos())
        Assert.Equal(Configuracion.ESPERA_POR_DEFECTO, cfg.LeerSegundosDeEspera())
    End Sub

    <Fact>
    Public Sub Un_json_roto_no_tumba_el_arranque()
        ' Una coma de más es el error más fácil de cometer editando a mano.
        Dim cfg = ConContenido("{""Db"":{""Servidor"":""X"",}}")

        Assert.Equal(Configuracion.SERVIDOR_POR_DEFECTO, cfg.LeerServidor())
    End Sub

    <Fact>
    Public Sub Una_clave_en_blanco_no_deja_el_servidor_sin_nombre()
        Dim cfg = ConContenido("{""Db"":{""Servidor"":""   "",""BaseDatos"":""""}}")

        Assert.Equal(Configuracion.SERVIDOR_POR_DEFECTO, cfg.LeerServidor())
        Assert.Equal(Configuracion.BASE_DATOS_POR_DEFECTO, cfg.LeerBaseDatos())
    End Sub

    <Fact>
    Public Sub Una_clave_que_falta_no_arrastra_a_las_demas()
        ' Falta BaseDatos: Servidor debe respetarse igual.
        Dim cfg = ConContenido("{""Db"":{""Servidor"":""LAB\\SQLEXPRESS""}}")

        Assert.Equal("LAB\SQLEXPRESS", cfg.LeerServidor())
        Assert.Equal(Configuracion.BASE_DATOS_POR_DEFECTO, cfg.LeerBaseDatos())
    End Sub

    <Theory>
    <InlineData("0")>
    <InlineData("-5")>
    <InlineData("""ocho""")>
    Public Sub Una_espera_absurda_se_ignora(valor As String)
        ' Cero o negativo dejaría las conexiones colgadas o fallando al instante;
        ' un texto donde va un número es sencillamente un error de tecleo.
        Dim cfg = ConContenido("{""Db"":{""SegundosDeEspera"":" & valor & "}}")

        Assert.Equal(Configuracion.ESPERA_POR_DEFECTO, cfg.LeerSegundosDeEspera())
    End Sub

    <Fact>
    Public Sub El_origen_dice_si_el_archivo_se_leyo()
        Dim leido = ConContenido("{""Db"":{""Servidor"":""X""}}")
        Assert.Contains(Configuracion.ARCHIVO, leido.LeerOrigen())

        Dim sinArchivo As New Configuracion(Path.Combine(Carpeta, "no-existe.json"))
        Assert.Contains("valores por defecto", sinArchivo.LeerOrigen())
    End Sub
End Class
