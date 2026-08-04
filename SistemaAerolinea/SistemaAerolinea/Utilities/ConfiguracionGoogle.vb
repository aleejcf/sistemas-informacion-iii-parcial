Imports System.IO
Imports System.Text.Json

''' <summary>Lee las credenciales de OAuth de Google desde un archivo local.
'''
''' El archivo es EL MISMO que descarga la consola de Google Cloud al crear un
''' cliente de tipo "Aplicación de escritorio": basta con renombrarlo a
''' google-oauth.json y dejarlo junto al programa. Así las credenciales nunca se
''' escriben dentro del código ni terminan en el control de versiones — el
''' .gitignore las excluye a propósito.
'''
''' Si el archivo no está, el sistema sigue funcionando con normalidad: el botón
''' de Google simplemente avisa que falta configurarlo.</summary>
Public Class ConfiguracionGoogle

    Public Const NOMBRE_ARCHIVO As String = "google-oauth.json"

    ''' <summary>Cuántas carpetas se suben buscando el archivo.</summary>
    Public Const SALTOS_HACIA_ARRIBA As Integer = 5

    Public Property ClientId As String
    Public Property ClientSecret As String
    Public Property AuthUri As String = "https://accounts.google.com/o/oauth2/v2/auth"
    Public Property TokenUri As String = "https://oauth2.googleapis.com/token"

    Private Shared cacheada As ConfiguracionGoogle
    Private Shared yaBuscada As Boolean = False
    Private Shared carpetaDeBusqueda As String = Nothing

    Public ReadOnly Property EstaConfigurada As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(ClientId)
        End Get
    End Property

    ''' <summary>Devuelve la configuración, o Nothing si no hay archivo válido.</summary>
    Public Shared Function Cargar() As ConfiguracionGoogle
        If yaBuscada Then Return cacheada
        yaBuscada = True

        Dim ruta = Localizar()
        If ruta Is Nothing Then
            Registro.Info($"No se encontró {NOMBRE_ARCHIVO}: el inicio de sesión con Google queda desactivado.")
            Return Nothing
        End If

        Try
            Using documento = JsonDocument.Parse(File.ReadAllText(ruta))
                Dim raiz = documento.RootElement

                ' Google envuelve las credenciales en "installed" (escritorio) o "web"
                Dim nodo As JsonElement
                If Not raiz.TryGetProperty("installed", nodo) AndAlso
                   Not raiz.TryGetProperty("web", nodo) Then
                    nodo = raiz
                End If

                cacheada = New ConfiguracionGoogle With {
                    .ClientId = Texto(nodo, "client_id"),
                    .ClientSecret = Texto(nodo, "client_secret")
                }

                Dim auth = Texto(nodo, "auth_uri")
                If Not String.IsNullOrWhiteSpace(auth) Then cacheada.AuthUri = auth

                Dim token = Texto(nodo, "token_uri")
                If Not String.IsNullOrWhiteSpace(token) Then cacheada.TokenUri = token
            End Using

            If Not cacheada.EstaConfigurada Then
                Registro.Advertencia($"{NOMBRE_ARCHIVO} no trae client_id.")
                cacheada = Nothing
            Else
                Registro.Info($"Credenciales de Google cargadas desde {ruta}")
            End If

        Catch ex As Exception
            Registro.Error_($"Leer {NOMBRE_ARCHIVO}", ex)
            cacheada = Nothing
        End Try

        Return cacheada
    End Function

    ''' <summary>Busca el archivo junto al ejecutable y, si no está, subiendo por
    ''' las carpetas del proyecto: así funciona igual ejecutando desde Visual
    ''' Studio que desde la carpeta de publicación.</summary>
    Private Shared Function Localizar() As String
        Dim carpeta As New DirectoryInfo(If(carpetaDeBusqueda, AppContext.BaseDirectory))

        For salto = 0 To SALTOS_HACIA_ARRIBA
            If carpeta Is Nothing Then Exit For

            Dim intento = Path.Combine(carpeta.FullName, NOMBRE_ARCHIVO)
            If File.Exists(intento) Then Return intento

            carpeta = carpeta.Parent
        Next

        Return Nothing
    End Function

    Private Shared Function Texto(nodo As JsonElement, propiedad As String) As String
        Dim valor As JsonElement
        If Not nodo.TryGetProperty(propiedad, valor) Then Return Nothing
        If valor.ValueKind <> JsonValueKind.String Then Return Nothing
        Return valor.GetString()
    End Function

    ''' <summary>Vuelve a leer el archivo. Sirve para que el usuario lo coloque sin
    ''' tener que cerrar y abrir el programa.</summary>
    Public Shared Sub Olvidar()
        yaBuscada = False
        cacheada = Nothing
    End Sub

    ''' <summary>Cambia la carpeta desde la que se busca el archivo; Nothing vuelve
    ''' a la del ejecutable.
    '''
    ''' Existe para las pruebas: el google-oauth.json real se copia a la carpeta de
    ''' salida de las pruebas cuando el equipo tiene Google configurado, así que
    ''' mirar ahí no permite comprobar el caso "sin credenciales".</summary>
    Public Shared Sub BuscarEn(carpeta As String)
        carpetaDeBusqueda = carpeta
        Olvidar()
    End Sub
End Class
