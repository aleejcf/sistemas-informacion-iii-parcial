Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Net.Sockets
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports System.Threading

''' <summary>Inicio de sesión con Google para aplicaciones de escritorio, con el
''' flujo OAuth 2.0 "Authorization Code + PKCE" (RFC 8252 / RFC 7636).
'''
''' Cómo funciona, en orden:
'''   1. Se inventa un secreto de un solo uso (el "verificador") y se manda a
'''      Google solo su huella SHA-256. Eso es PKCE, y es lo que impide que otro
'''      programa del mismo equipo robe el código de autorización: sin el
'''      verificador original, el código no sirve para nada.
'''   2. Se abre el NAVEGADOR del usuario, no una ventana dentro del programa.
'''      Es la regla de RFC 8252: así la contraseña de Google se escribe en el
'''      sitio de Google, con su candado y su gestor de contraseñas, y esta
'''      aplicación nunca la ve ni podría verla.
'''   3. Google redirige a un servidor diminuto que se levanta en este equipo
'''      (127.0.0.1, puerto libre) y que solo vive los segundos que dura el
'''      intercambio.
'''   4. Con el código y el verificador se pide el token, y del `id_token` se leen
'''      el identificador, el nombre y el correo.
'''
''' Las credenciales salen de google-oauth.json — nunca están escritas aquí.</summary>
Public Class GoogleAuthService

    ''' <summary>Lo que Google confirma sobre la persona que inició sesión.</summary>
    Public Class IdentidadGoogle
        ''' <summary>El `sub` de Google: su identificador interno, que no cambia
        ''' aunque la persona cambie de correo. Es la llave con la que se liga.</summary>
        Public Property GoogleId As String
        Public Property Email As String
        Public Property Nombre As String
        Public Property CorreoVerificado As Boolean
    End Class

    Public Class ResultadoGoogle
        Public Property Identidad As IdentidadGoogle
        Public Property Mensaje As String

        Public ReadOnly Property Exitoso As Boolean
            Get
                Return Identidad IsNot Nothing
            End Get
        End Property
    End Class

    Private Const TIEMPO_LIMITE_SEGUNDOS As Integer = 180

    Public Shared Function EstaDisponible() As Boolean
        Dim config = ConfiguracionGoogle.Cargar()
        Return config IsNot Nothing AndAlso config.EstaConfigurada
    End Function

    ''' <summary>Corre el flujo completo y devuelve la identidad, o el motivo por el
    ''' que no se pudo. Nunca lanza excepción: el inicio de sesión con Google es un
    ''' camino alterno y un fallo suyo no debe tumbar la pantalla de acceso.</summary>
    Public Shared Async Function IniciarSesionAsync(Optional cancelar As CancellationToken = Nothing) As Task(Of ResultadoGoogle)
        Dim config = ConfiguracionGoogle.Cargar()
        If config Is Nothing OrElse Not config.EstaConfigurada Then
            Return New ResultadoGoogle With {
                .Mensaje = "Falta configurar el acceso con Google. Coloca el archivo " &
                           $"{ConfiguracionGoogle.NOMBRE_ARCHIVO} junto al programa."
            }
        End If

        Dim escucha As TcpListener = Nothing

        Try
            ' --- PKCE: el secreto se queda aquí; a Google solo le llega su huella ---
            Dim verificador = GenerarVerificador()
            Dim desafio = HuellaDe(verificador)
            Dim estado = GenerarVerificador()

            ' --- Servidor de un solo uso en un puerto libre del propio equipo ---
            escucha = New TcpListener(IPAddress.Loopback, 0)
            escucha.Start()
            Dim puerto = CType(escucha.LocalEndpoint, IPEndPoint).Port
            Dim redireccion = $"http://127.0.0.1:{puerto}/"

            ' --- Se abre el navegador del usuario ---
            Dim url = ConstruirUrlDeAutorizacion(config, redireccion, desafio, estado)
            AbrirNavegador(url)

            ' --- Se espera la vuelta de Google ---
            Dim respuesta = Await EsperarRespuestaAsync(escucha, cancelar)
            If respuesta Is Nothing Then
                Return New ResultadoGoogle With {
                    .Mensaje = "No se recibió respuesta de Google. Puede que se haya cerrado el navegador."
                }
            End If

            If respuesta.ContainsKey("error") Then
                Dim detalle = respuesta("error")
                Registro.Advertencia($"Google rechazó el acceso: {detalle}")
                Return New ResultadoGoogle With {
                    .Mensaje = If(detalle = "access_denied",
                                  "Cancelaste el acceso con Google.",
                                  $"Google no autorizó el acceso ({detalle}).")
                }
            End If

            ' El `state` amarra esta respuesta con la petición que salió de aquí
            If Not respuesta.ContainsKey("state") OrElse respuesta("state") <> estado Then
                Registro.Advertencia("El parámetro state no coincide: se descarta la respuesta de Google.")
                Return New ResultadoGoogle With {
                    .Mensaje = "La respuesta de Google no se pudo verificar. Inténtalo de nuevo."
                }
            End If

            If Not respuesta.ContainsKey("code") Then
                Return New ResultadoGoogle With {.Mensaje = "Google no devolvió el código de autorización."}
            End If

            ' --- Canje del código por el token ---
            Dim idToken = Await CanjearCodigoAsync(config, respuesta("code"), verificador, redireccion)
            If idToken Is Nothing Then
                Return New ResultadoGoogle With {
                    .Mensaje = "No se pudo completar el intercambio con Google. Revisa tu conexión."
                }
            End If

            Dim identidad = LeerIdentidad(idToken)
            If identidad Is Nothing OrElse String.IsNullOrWhiteSpace(identidad.GoogleId) Then
                Return New ResultadoGoogle With {.Mensaje = "Google no devolvió los datos de la cuenta."}
            End If

            If Not identidad.CorreoVerificado Then
                Return New ResultadoGoogle With {
                    .Mensaje = "Esa cuenta de Google no tiene el correo verificado."
                }
            End If

            Registro.Info($"Inicio de sesión con Google: {identidad.Email}")
            Return New ResultadoGoogle With {.Identidad = identidad}

        Catch ex As Exception
            Registro.Error_("Inicio de sesión con Google", ex)
            Return New ResultadoGoogle With {
                .Mensaje = "No se pudo completar el inicio de sesión con Google. " &
                           "El detalle quedó en la bitácora."
            }
        Finally
            Try
                If escucha IsNot Nothing Then escucha.Stop()
            Catch
                ' El servidor era temporal: si ya se cerró, mejor
            End Try
        End Try
    End Function

    ' ================================================================
    '  PKCE
    ' ================================================================

    ''' <summary>Cadena aleatoria segura en base64url, sin relleno (RFC 7636).</summary>
    Public Shared Function GenerarVerificador() As String
        Dim bytes(31) As Byte
        RandomNumberGenerator.Fill(bytes)
        Return Base64Url(bytes)
    End Function

    ''' <summary>La huella que viaja a Google: BASE64URL(SHA-256(verificador)).</summary>
    Public Shared Function HuellaDe(verificador As String) As String
        Using sha = SHA256.Create()
            Return Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verificador)))
        End Using
    End Function

    ''' <summary>Base64 de la web: sin relleno y sin caracteres que rompan una URL.</summary>
    Public Shared Function Base64Url(bytes As Byte()) As String
        Return Convert.ToBase64String(bytes).
            TrimEnd("="c).Replace("+", "-").Replace("/", "_")
    End Function

    ' ================================================================
    '  FLUJO
    ' ================================================================

    Private Shared Function ConstruirUrlDeAutorizacion(config As ConfiguracionGoogle,
                                                       redireccion As String,
                                                       desafio As String,
                                                       estado As String) As String
        Dim parametros As New Dictionary(Of String, String) From {
            {"client_id", config.ClientId},
            {"redirect_uri", redireccion},
            {"response_type", "code"},
            {"scope", "openid email profile"},
            {"code_challenge", desafio},
            {"code_challenge_method", "S256"},
            {"state", estado},
            {"access_type", "offline"},
            {"prompt", "select_account"}
        }

        Dim consulta = String.Join("&", parametros.Select(
            Function(p) $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"))

        Return $"{config.AuthUri}?{consulta}"
    End Function

    ''' <summary>Abre el navegador predeterminado. La contraseña de Google se
    ''' escribe allí, nunca dentro de esta aplicación.</summary>
    Private Shared Sub AbrirNavegador(url As String)
        Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
    End Sub

    ''' <summary>Atiende UNA conexión: lee la línea de petición, saca los parámetros
    ''' y responde con una página que le dice al usuario que ya puede volver.
    '''
    ''' Se usa TcpListener y no HttpListener a propósito: HttpListener exige en
    ''' Windows una reserva de URL o permisos de administrador, y este programa
    ''' tiene que funcionar con una cuenta normal.</summary>
    Private Shared Async Function EsperarRespuestaAsync(escucha As TcpListener,
                                                        cancelar As CancellationToken) As Task(Of Dictionary(Of String, String))
        Dim aceptar = escucha.AcceptTcpClientAsync()
        Dim limite = Task.Delay(TimeSpan.FromSeconds(TIEMPO_LIMITE_SEGUNDOS), cancelar)

        If Await Task.WhenAny(aceptar, limite) Is limite Then Return Nothing

        Using cliente = Await aceptar
            Using flujo = cliente.GetStream()
                Dim lector As New StreamReader(flujo, Encoding.ASCII)
                Dim peticion = Await lector.ReadLineAsync()
                If String.IsNullOrWhiteSpace(peticion) Then Return Nothing

                ' "GET /?code=...&state=... HTTP/1.1"
                Dim partes = peticion.Split(" "c)
                If partes.Length < 2 Then Return Nothing

                Await ResponderAlNavegadorAsync(flujo)
                Return LeerParametros(partes(1))
            End Using
        End Using
    End Function

    Private Shared Async Function ResponderAlNavegadorAsync(flujo As NetworkStream) As Task
        Dim pagina =
            "<!doctype html><html lang=""es""><head><meta charset=""utf-8"">" &
            "<title>ALAS Honduras</title></head>" &
            "<body style=""font-family:Segoe UI,system-ui,sans-serif;background:#08182F;color:#fff;" &
            "display:flex;align-items:center;justify-content:center;height:100vh;margin:0"">" &
            "<div style=""text-align:center"">" &
            "<div style=""font-size:52px;color:#F2B01E"">&#9992;</div>" &
            "<h1 style=""margin:18px 0 8px;font-size:26px"">Listo</h1>" &
            "<p style=""color:#BFD4E9;margin:0"">Ya puedes volver a ALAS Honduras.</p>" &
            "<p style=""color:#5A7A9C;font-size:13px;margin-top:22px"">Puedes cerrar esta pestaña.</p>" &
            "</div></body></html>"

        Dim cuerpo = Encoding.UTF8.GetBytes(pagina)
        Dim encabezado = Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK" & vbCrLf &
            "Content-Type: text/html; charset=utf-8" & vbCrLf &
            $"Content-Length: {cuerpo.Length}" & vbCrLf &
            "Connection: close" & vbCrLf & vbCrLf)

        Await flujo.WriteAsync(encabezado, 0, encabezado.Length)
        Await flujo.WriteAsync(cuerpo, 0, cuerpo.Length)
        Await flujo.FlushAsync()
    End Function

    Private Shared Function LeerParametros(rutaConsulta As String) As Dictionary(Of String, String)
        Dim valores As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        Dim inicio = rutaConsulta.IndexOf("?"c)
        If inicio < 0 Then Return valores

        For Each pareja In rutaConsulta.Substring(inicio + 1).Split("&"c)
            Dim igual = pareja.IndexOf("="c)
            If igual <= 0 Then Continue For

            Dim clave = Uri.UnescapeDataString(pareja.Substring(0, igual))
            Dim valor = Uri.UnescapeDataString(pareja.Substring(igual + 1).Replace("+", " "))
            valores(clave) = valor
        Next

        Return valores
    End Function

    ''' <summary>Cambia el código por el token. Aquí sí viaja el verificador de
    ''' PKCE: es la prueba de que quien pide el token es quien inició el flujo.</summary>
    Private Shared Async Function CanjearCodigoAsync(config As ConfiguracionGoogle, codigo As String,
                                                     verificador As String,
                                                     redireccion As String) As Task(Of String)
        Dim campos As New Dictionary(Of String, String) From {
            {"code", codigo},
            {"client_id", config.ClientId},
            {"redirect_uri", redireccion},
            {"grant_type", "authorization_code"},
            {"code_verifier", verificador}
        }

        ' En un cliente de escritorio el "secreto" no es realmente secreto (viaja
        ' dentro del programa instalado); quien protege el flujo es PKCE. Aun así
        ' Google lo pide, así que se manda cuando el archivo lo trae.
        If Not String.IsNullOrWhiteSpace(config.ClientSecret) Then
            campos("client_secret") = config.ClientSecret
        End If

        Using cliente As New HttpClient()
            cliente.Timeout = TimeSpan.FromSeconds(30)

            Dim respuesta = Await cliente.PostAsync(config.TokenUri, New FormUrlEncodedContent(campos))
            Dim cuerpo = Await respuesta.Content.ReadAsStringAsync()

            If Not respuesta.IsSuccessStatusCode Then
                Registro.Advertencia($"Google devolvió {CInt(respuesta.StatusCode)} al canjear el código: {cuerpo}")
                Return Nothing
            End If

            Using documento = JsonDocument.Parse(cuerpo)
                Dim token As JsonElement
                If Not documento.RootElement.TryGetProperty("id_token", token) Then Return Nothing
                Return token.GetString()
            End Using
        End Using
    End Function

    ''' <summary>Lee los datos del `id_token`. No hace falta verificar su firma:
    ''' el token no viene del navegador sino directo del servidor de Google sobre
    ''' TLS, que es la excepción que contempla la propia especificación.</summary>
    Public Shared Function LeerIdentidad(idToken As String) As IdentidadGoogle
        Try
            Dim partes = idToken.Split("."c)
            If partes.Length < 2 Then Return Nothing

            Using documento = JsonDocument.Parse(DesdeBase64Url(partes(1)))
                Dim raiz = documento.RootElement

                Dim identidad As New IdentidadGoogle With {
                    .GoogleId = Cadena(raiz, "sub"),
                    .Email = Cadena(raiz, "email"),
                    .Nombre = Cadena(raiz, "name")
                }

                Dim verificado As JsonElement
                If raiz.TryGetProperty("email_verified", verificado) Then
                    identidad.CorreoVerificado = verificado.ValueKind = JsonValueKind.True OrElse
                                                 (verificado.ValueKind = JsonValueKind.String AndAlso
                                                  verificado.GetString() = "true")
                End If

                Return identidad
            End Using

        Catch ex As Exception
            Registro.Error_("Leer el id_token de Google", ex)
            Return Nothing
        End Try
    End Function

    Private Shared Function Cadena(nodo As JsonElement, propiedad As String) As String
        Dim valor As JsonElement
        If Not nodo.TryGetProperty(propiedad, valor) Then Return Nothing
        Return valor.GetString()
    End Function

    ''' <summary>Deshace el base64url: le devuelve los caracteres normales y el relleno.</summary>
    Public Shared Function DesdeBase64Url(texto As String) As Byte()
        Dim normal = texto.Replace("-", "+").Replace("_", "/")
        Select Case normal.Length Mod 4
            Case 2 : normal &= "=="
            Case 3 : normal &= "="
        End Select
        Return Convert.FromBase64String(normal)
    End Function
End Class
