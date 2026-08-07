Imports System.IO
Imports System.Net
Imports System.Net.Mail

''' <summary>Envío de correo por SMTP. Lo usa la recuperación de contraseña para
''' mandarle al usuario su código de verificación.
'''
''' La cuenta y su contraseña de aplicación NO viven en el código: se leen de un
''' archivo de configuración en la carpeta del usuario. Así la credencial no viaja
''' con el proyecto ni queda escrita en el repositorio, y cada quien configura la
''' suya.
'''
''' Si el archivo no está o está incompleto, la vía del correo NO SE OFRECE y la
''' pantalla lo dice. En ningún caso se enseña el código: un código que no llega a
''' su dueño no verifica que sea él quien lo pide, y enseñarlo convertiría la
''' recuperación en una puerta abierta —bastaría con saber el correo de alguien
''' para leer su código y cambiarle la contraseña.</summary>
Public Class CorreoService

    ''' <summary>Identificador con el que el HTML referencia al logotipo incrustado.</summary>
    Private Const CONTENT_ID_LOGO As String = "logoAlejandria"

    ''' <summary>El icono de la aplicación en PNG de 96x96, sacado del mismo
    ''' `alejandria.ico` que lleva el ejecutable. Va incrustado y no como archivo
    ''' suelto para que el correo no dependa de que exista una carpeta al lado.</summary>
    Private Const LOGO_BASE64 As String = "iVBORw0KGgoAAAANSUhEUgAAAGAAAABgCAYAAADimHc4AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAk6SURBVHhe7Z1tkBTFGccnFUVRyHHbcy/eTffM7M1O9+zxdp7REtALSAQVNX5ABAorqTLmpbQ0LxoLRQwBqkARBeIBai6IyPEWDSaKJhWNSdRAokJOkQ+axA+pIlWpVMqKCSHxST29M3fc3K4My9ze29NVv2Jvb3em5//vfrpnjn7aMMosTMqxTDoyo/iNphJLmOLrmBSdTPJXTSUOm4oPO5gUB03Jn2ZKdDAlVpjKvr26WUytHe/WxfXpt5IJnItYIO5kSuxjkn9oBgLMwIaaiPwwJ7xOvOYCApjib7NArK2RYm7VBFEd1yyVkmm2Ayb5ZqbE0ZpmR1dGV0CJEY82BTVRaIb4FZP2AqPNOCOuYVmlJl8zhgV8WUZaf4laQLwCRA81eQf/PWYqsa9a2RfH9TylUpsTWab4cyYJf2roUOxgbzg6LmhcFNc1UanO82lMWl061MRPQCRDh2n+byb5fUZbW/KQhCM7U+JPYXciToPuQVvx1YZhfCqudZ9S5QuXKXGQWn6KFGZL/zUlvy2ud68iJohqJq0XSfz00T1BiX+YqmFOXPfuwpS1XMetIgcgTh89MEvrvYzXaMW1N+oC3swU/yu1/v4F9c341pK4/oapRAcNuv1POCD/sUrWOd3i1yr7YlOJv9OdbWXQUcZvvL+n9ftiCd5Kxz9I9A9hpNnfoBqYUZPPj2FS/JLudCsLU+KfTNrTjXHNziQm+TH9VLPIB4n+ASNORtp3G0zyr1LsrzzhbPMZnPuvIAMqjw75Uuw3MpJvovg/AGgD+Ps4/99LBgwAgQ1M8n8ZTPG36O534MBB+EhaBjAl4DOegLFNpanyBFT7Apjs+/2BBOtUletb3ziZlOtt4F/70zAABa0NBAQX2jB5qgOTpvQF3/cvsOG88QLOzXIYZXMY7XIY0yRgXK5wcf1pDDYQPAeKjQ3l3KyAs5xCPWrzAtwWGyaG9exT9ymO/p01Md06pmYACphtsWHNYg9+usGHH60twkM+PLkqB+3f9WD1XR4svd2Dm2/MwuyrHMi12voYCLY0FCit1nbiMfF14wQbps5wYME8F75zSxOsvMODDUs96FiZgz1hPeN1//E6H7bf78PM2Q6M9fqeo1xSMwBDC4q4Y00OjuyR8Nb24hzqlNC1Q8HhXQre3qngwFYJLz8mtWk/WOnDbV9ugmmXOdpMDAnnuFy32Pj5koCi4/cbxguYOMWGG+a68OBiT4v5800+vLalUJ93dyvo2qn063h9I7p2SvjtExKuvdbVPTZ+rnJJ1QCv1Yatq3L6QrCySUADfr+t50LxNb7f+UAOvnFzE1w6E5+ho5DJegR+NhIew+EN17uw/l5Pm4zHfvOpwnneeErC756UsH9r3zoVA7/7yuMSrr5mmBkQBwVBU1Ao7CEvPSZh3T0eXHedq8+D4hbrESi8ngB4Alqm2XDXLU2wZ21OH+cPO3rE3l/knEkYMQbEwQt/Z6eC17dI2HCvB9Nn6f/uoc8XnTsTDqr5i2y48+tN8OJGX9cBSdrCT8aINSAiClMYt5d90wP1WRy0bXygBXWBgIXzXdjXLuHg9kLvSUv4iBFvQASa8OoPJcy8wu0OR/XNAh5f7sGRPSp14SPIgBA04Cfr/e5pK577LJvDim97emYV/3xakAEhaMCz6309TY0MwBup5WRAZQ1oOp8M6AUZUB5kQELIgBAyoARkQHmQAQkhA0LIgBKQAeVBBiSEDAghA0pABpQHGZAQMiCEDCgBGVAeZEBCyIAQMqAEZEB5kAEJIQNCyIASkAHlQQYkhAwIIQNKQAaUBxmQEDIghAwoARlQHmRAQsiAEDKgBGRAeZABCSEDQsiAEpAB5UEGJIQMCCEDSoAG4LqtzpOslEfe2FZYtxu/yCT0hwHRYvFoEXcx9Er5LYN4pTyKgeu2MBXAc98P8y0UY60PP9vkw+tPSC0YLsbGpaVJV66fjgHRInAUW4u6Q8Lh3Uo3CFxJjykMopwWcfau86HzgUGcKyLKloKLpVumFTKOFAMzj1wy04Er57gw/3oXbr0pCw/dnYNda3z4TUdPS0QOFOklp2oAio7mRj0Pf36h3YfNy3J6Nf0XF2bhmi+4MGOWC62X9GR1KQb+btBmS0GS5AtCME3M2WGaGGxNjeML48cFlzqwaEEWHr7Hg+cf8bVY2ENODFdJDcDv4liEn3/pUR+2rcrBt77SBNMvd3QOCXtSIffE2U5hmSvmlsDQEq9rnCT5Kk6FVA0oB2xNKGRkHGZIQVAkbJ2bluX0wmwMF2jEyQxA4QtxW8HuB3Nwx9eyMGWGo1fTn5iQCV+nLWY5DLgBcdAQFAYFGu0IqMsL+PwVrl6I/esOTC0jde+IG4A5f95/RukQtm21D4vmu6AutOGcLC+03EGYpQsZdAbEwVQEGK5QvM9d7kD7fZ4eFOMr5dEANGbhPBfcybYOKSh8/HiDjUFvQAQagYLiQH/ZLBeykzFRR+F3aEQU2zGO45QYx6P4MQYjaMC7Q8GAiCg1De7ZFb2Hr6N0ZENF+Ag04J2hZEA3wyTXtZHxrVeGpAHDAcn/h4lbd1Hm3AGgkDn3b0ZG2hvIgMqDmjMpugxT8sWUPb3yhAa8jKmLrx4uA9pQItzGZKOBe1oxyT+gMFRZagL7YyathYbRapxpSnsLbWFVOaKtrDK5+kDvopTJW1+iqWjl0DtWSf50a6txpjbgvHxWMCUOkQkVoDD9/I8ZOPO69xHDUq3ETXrHz/gXiFTBRp7xrWeNfH5ULwNw63JT8tdoLOhHClPP46ay2nqJH5VMTsxkin9IO6r2D7px+6L9Ezd2zkhxqxnYx2lami7hHvPP13teTVzz3iWfH2VGjyfoBi0VMO4zKQ7ibuVxuYsWy7JGM8k39+yH3vegRDLCln/IVOL8uM6fXNrazjCVWGoG4iMaE8oAG6/eQVu8YLoNflzexAVvl03F/4zdiHpDArTwNrb64xklNtZ79SeJ+QlKnS9cJsXaaMtzPZrT+NALHa71nsz8GLb6at+abcw1Ph3X8rSKGdgtprQeYUq8aUrxMRrR3TNGUu8IQsFD0QuDLP8go6y9LG8vwOdrce1SLZkJjZapxBym7O+ZSvwC/7DPJD/ap6LDFCbFR0zy90wlDrCAP6qfpeUaJsd1SlL+DylZwfVGMXbBAAAAAElFTkSuQmCC"

    ''' <summary>Datos de la cuenta que envía. Los llena el archivo de configuración.</summary>
    Public Class Configuracion
        Public Property Servidor As String = "smtp.gmail.com"
        Public Property Puerto As Integer = 587
        Public Property Remitente As String = ""
        Public Property Clave As String = ""
        Public Property Nombre As String = "Biblioteca Alejandría"

        ''' <summary>Sin remitente o sin clave no hay nada que intentar.</summary>
        Public ReadOnly Property EstaCompleta As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(Remitente) AndAlso
                       Not String.IsNullOrWhiteSpace(Clave) AndAlso
                       Not String.IsNullOrWhiteSpace(Servidor) AndAlso
                       Puerto > 0
            End Get
        End Property
    End Class

    ''' <summary>El archivo vive en la carpeta del usuario, no junto al ejecutable:
    ''' así sobrevive a recompilaciones y no se copia por accidente al entregar.</summary>
    Public Shared ReadOnly Property RutaConfiguracion As String
        Get
            Dim carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BibliotecaAlejandria")
            Directory.CreateDirectory(carpeta)
            Return Path.Combine(carpeta, "correo.config")
        End Get
    End Property

    ' ======================= CONFIGURACIÓN =======================

    ''' <summary>Lee el archivo. Nunca lanza: si algo falla devuelve una
    ''' configuración vacía y el sistema usa el modo de respaldo.</summary>
    Public Shared Function Leer() As Configuracion
        Dim config As New Configuracion()

        Try
            If Not File.Exists(RutaConfiguracion) Then Return config

            For Each linea In File.ReadAllLines(RutaConfiguracion)
                Dim limpia = linea.Trim()
                If limpia = "" OrElse limpia.StartsWith("#") Then Continue For

                Dim igual = limpia.IndexOf("="c)
                If igual <= 0 Then Continue For

                Dim clave = limpia.Substring(0, igual).Trim().ToLower()
                Dim valor = limpia.Substring(igual + 1).Trim()

                Select Case clave
                    Case "remitente" : config.Remitente = valor
                    Case "clave"
                        ' Google muestra la contraseña de aplicación en grupos de
                        ' cuatro ("abcd efgh ijkl mnop"), pero esos espacios son solo
                        ' para leerla: si se copian tal cual, la autenticación falla.
                        config.Clave = valor.Replace(" ", "").Replace(vbTab, "")
                    Case "servidor" : config.Servidor = valor
                    Case "nombre" : config.Nombre = valor
                    Case "puerto"
                        Dim puerto As Integer
                        If Integer.TryParse(valor, puerto) Then config.Puerto = puerto
                End Select
            Next

        Catch ex As Exception
            ' Un archivo mal escrito no puede impedir que se abra el sistema
            Registro.Advertencia($"No se pudo leer la configuración de correo: {ex.Message}")
            Return New Configuracion()
        End Try

        Return config
    End Function

    Public Shared Function HayConfiguracion() As Boolean
        Return Leer().EstaCompleta
    End Function

    ''' <summary>Crea el archivo de ejemplo la primera vez, con las instrucciones
    ''' adentro. Así el usuario solo tiene que abrirlo y rellenar dos líneas.</summary>
    Public Shared Sub CrearPlantillaSiFalta()
        Try
            If File.Exists(RutaConfiguracion) Then Return

            File.WriteAllText(RutaConfiguracion,
"# ============================================================================
#  Biblioteca Alejandría — configuración del correo saliente
# ============================================================================
#  Sirve para enviar el código de verificación cuando alguien recupera su
#  contraseña. Mientras esté vacío, el sistema muestra el código en pantalla
#  en vez de enviarlo.
#
#  Para Gmail hace falta una CONTRASEÑA DE APLICACIÓN (no la del correo):
#    1. Activa la verificación en dos pasos en la cuenta de Google.
#    2. Entra a  https://myaccount.google.com/apppasswords
#    3. Genera una contraseña para la aplicación y pégala en `clave`.
#
#  Este archivo no forma parte del proyecto y no se entrega con él.
# ============================================================================

remitente =
clave     =

servidor  = smtp.gmail.com
puerto    = 587
nombre    = Biblioteca Alejandría
", Text.Encoding.UTF8)

            Registro.Info($"Se creó la plantilla de configuración de correo en {RutaConfiguracion}")

        Catch ex As Exception
            Registro.Advertencia($"No se pudo crear la plantilla de correo: {ex.Message}")
        End Try
    End Sub

    ' ======================= ENVÍO =======================

    ''' <summary>Envía el código de verificación. Devuelve Nothing si se envió, o
    ''' el mensaje de error para mostrar en pantalla.
    '''
    ''' Es una operación de red y puede tardar varios segundos: hay que llamarla
    ''' desde un Task.Run para no congelar la ventana.</summary>
    Public Shared Function EnviarCodigo(destinatario As String, nombreDestinatario As String,
                                        codigo As String) As String
        Dim config = Leer()
        If Not config.EstaCompleta Then
            Return "El envío de correo no está configurado."
        End If

        Try
            Using mensaje As New MailMessage()
                mensaje.From = New MailAddress(config.Remitente, config.Nombre)
                mensaje.To.Add(New MailAddress(destinatario))
                mensaje.Subject = $"Tu código de verificación: {codigo}"
                mensaje.SubjectEncoding = Text.Encoding.UTF8

                ' El logotipo va como recurso vinculado (CID) y no como
                ' "data:image/png;base64,..." dentro del HTML: Gmail bloquea ese
                ' formato y la imagen sale como un icono roto.
                Dim vista = AlternateView.CreateAlternateViewFromString(
                    CuerpoHtml(nombreDestinatario, codigo), Text.Encoding.UTF8, "text/html")
                vista.LinkedResources.Add(LogoVinculado())
                mensaje.AlternateViews.Add(vista)

                Using cliente As New SmtpClient(config.Servidor, config.Puerto)
                    cliente.EnableSsl = True
                    cliente.DeliveryMethod = SmtpDeliveryMethod.Network
                    ' UseDefaultCredentials se apaga ANTES de asignar las propias:
                    ' al revés, el .NET las descarta silenciosamente.
                    cliente.UseDefaultCredentials = False
                    cliente.Credentials = New NetworkCredential(config.Remitente, config.Clave)
                    cliente.Timeout = 20000

                    cliente.Send(mensaje)
                End Using
            End Using

            ' La bitácora registra a quién se le envió, nunca la credencial
            Registro.Info($"Código de recuperación enviado por correo a {destinatario}")
            Return Nothing

        Catch ex As SmtpException
            Registro.Error_("Enviar el código de recuperación", ex)
            Return TraducirFalla(ex)

        Catch ex As Exception
            Registro.Error_("Enviar el código de recuperación", ex)
            Return "No se pudo enviar el correo. El detalle quedó en la bitácora."
        End Try
    End Function

    ''' <summary>Traduce las fallas de SMTP a algo que se pueda leer y actuar.</summary>
    Private Shared Function TraducirFalla(ex As SmtpException) As String
        Select Case ex.StatusCode
            Case SmtpStatusCode.MailboxBusy, SmtpStatusCode.MailboxUnavailable
                Return "El servidor de correo rechazó la dirección de destino."
            Case SmtpStatusCode.MustIssueStartTlsFirst
                Return "El servidor exige una conexión segura. Revisa el puerto en la configuración."
        End Select

        ' Gmail responde 535 cuando la contraseña de aplicación está mal o venció
        If ex.Message.Contains("5.7.8") OrElse ex.Message.Contains("535") OrElse
           ex.Message.IndexOf("Authentication", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return "El correo rechazó las credenciales. Revisa la contraseña de aplicación " &
                   "en el archivo de configuración."
        End If

        Return "No se pudo conectar con el servidor de correo. Revisa tu conexión a internet."
    End Function

    ''' <summary>El logotipo que viaja dentro del correo. Es el mismo icono de la
    ''' aplicación, que ya trae su fondo verde tinta redondeado: así se ve igual de
    ''' bien tanto si el cliente pinta el correo en claro como en oscuro, sin
    ''' depender de que respete un color de fondo nuestro.</summary>
    Private Shared Function LogoVinculado() As LinkedResource
        Dim bytes = Convert.FromBase64String(LOGO_BASE64)
        Dim recurso As New LinkedResource(New IO.MemoryStream(bytes)) With {
            .ContentId = CONTENT_ID_LOGO,
            .ContentType = New Mime.ContentType("image/png"),
            .TransferEncoding = Mime.TransferEncoding.Base64
        }
        Return recurso
    End Function

    ''' <summary>El correo que le llega al usuario.
    '''
    ''' Habla el idioma del sistema: es una ficha de biblioteca. Tipografía con
    ''' remates, el papel color hueso de siempre, filetes dorados como los de un
    ''' sello, y el código presentado como la fecha estampada en la ficha de un
    ''' libro prestado.
    '''
    ''' Todo el estilo va EN LÍNEA y no en una hoja aparte: Gmail en el teléfono se
    ''' salta buena parte de lo que venga en un bloque `style`, y el correo tiene
    ''' que verse igual en el móvil, que es donde se va a leer.</summary>
    ''' <summary>Avisa al dueño de una cuenta de que algo cambió en ella: su
    ''' contraseña o su correo.
    '''
    ''' No es cortesía, es seguridad. Quien se apodera de una cuenta lo primero que
    ''' hace es cambiar la contraseña y el correo para dejar fuera al dueño. Este
    ''' aviso es lo que le da la oportunidad de enterarse a tiempo, y por eso el de
    ''' cambio de correo se manda a la dirección VIEJA: la nueva ya sería la del
    ''' atacante.</summary>
    Public Shared Function EnviarAviso(destinatario As String, nombreDestinatario As String,
                                       titulo As String, detalle As String) As String
        Dim config = Leer()
        If Not config.EstaCompleta Then
            Return "El envío de correo no está configurado."
        End If

        Try
            Using mensaje As New MailMessage()
                mensaje.From = New MailAddress(config.Remitente, config.Nombre)
                mensaje.To.Add(New MailAddress(destinatario))
                mensaje.Subject = titulo
                mensaje.SubjectEncoding = Text.Encoding.UTF8

                Dim vista = AlternateView.CreateAlternateViewFromString(
                    CuerpoAviso(nombreDestinatario, titulo, detalle), Text.Encoding.UTF8, "text/html")
                vista.LinkedResources.Add(LogoVinculado())
                mensaje.AlternateViews.Add(vista)

                Using cliente As New SmtpClient(config.Servidor, config.Puerto)
                    cliente.EnableSsl = True
                    cliente.DeliveryMethod = SmtpDeliveryMethod.Network
                    cliente.UseDefaultCredentials = False
                    cliente.Credentials = New NetworkCredential(config.Remitente, config.Clave)
                    cliente.Timeout = 20000
                    cliente.Send(mensaje)
                End Using
            End Using

            Registro.Info($"Aviso de seguridad enviado a {destinatario}: {titulo}")
            Return Nothing

        Catch ex As Exception
            Registro.Error_("Enviar el aviso de seguridad", ex)
            Return "No se pudo enviar el aviso."
        End Try
    End Function

    ' ======================= PLANTILLA =======================

    ''' <summary>La envoltura común a todos los correos: el logotipo, la cabecera
    ''' verde tinta con su filete dorado, el interior variable y el pie.
    '''
    ''' Habla el idioma del sistema —es una ficha de biblioteca— y todo el estilo
    ''' va EN LÍNEA, no en una hoja aparte: Gmail en el teléfono se salta buena
    ''' parte de lo que venga en un bloque `style`.</summary>
    Private Shared Function Envolver(interior As String, pie As String) As String
        Return $"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#F5F2EA;'>
<div style='font-family:Georgia,Cambria,Times New Roman,serif;background:#F5F2EA;padding:30px 16px;'>
  <div style='max-width:520px;margin:0 auto;'>

    <div style='text-align:center;padding-bottom:22px;'>
      <img src='cid:{CONTENT_ID_LOGO}' alt='Biblioteca Alejandría' width='56' height='56'
           style='width:56px;height:56px;display:inline-block;border:0;'/>
    </div>

    <div style='background:#ffffff;border:1px solid #E3DED0;border-radius:4px;overflow:hidden;'>

      <div style='background:#14281E;padding:22px 30px;'>
        <div style='font-size:22px;font-weight:bold;color:#ffffff;letter-spacing:0.5px;'>
          ALEJAN<span style='color:#C9A227;'>DRÍA</span>
        </div>
        <div style='font-family:Arial,Helvetica,sans-serif;font-size:10px;color:#9DBBAA;
                    margin-top:6px;letter-spacing:2px;text-transform:uppercase;'>
          Sistema de gestión bibliotecaria
        </div>
      </div>

      <div style='height:3px;background:#C9A227;'></div>

      {interior}

      <div style='background:#F5F2EA;padding:18px 30px;border-top:1px solid #E3DED0;'>
        <p style='font-family:Arial,Helvetica,sans-serif;font-size:12.5px;color:#6E7D74;
                  margin:0;line-height:1.6;'>{pie}</p>
      </div>
    </div>

    <div style='padding:20px 8px 0;text-align:center;'>
      <p style='font-size:13px;color:#8A9A90;margin:0 0 6px;font-style:italic;'>
        El saber al alcance de todos.
      </p>
      <p style='font-family:Arial,Helvetica,sans-serif;font-size:11px;color:#A8B5AC;margin:0;'>
        Correo automático · No respondas a esta dirección
      </p>
    </div>
  </div>
</div>
</body></html>"
    End Function

    ''' <summary>El correo del código de recuperación.</summary>
    Private Shared Function CuerpoHtml(nombre As String, codigo As String) As String
        Dim saludo = If(String.IsNullOrWhiteSpace(nombre), "Hola", $"Hola, {nombre}")
        Dim fecha = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy")

        Dim interior = $"
      <div style='padding:30px;'>
        <p style='font-size:16px;color:#14231C;margin:0 0 8px;'>{saludo}:</p>
        <p style='font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#6E7D74;
                  margin:0 0 26px;line-height:1.65;'>
          Alguien pidió recuperar la contraseña de tu cuenta. Este es tu código de
          verificación:
        </p>

        <div style='border:2px solid #C9A227;border-radius:3px;padding:22px 18px;text-align:center;
                    background:#FDFBF6;'>
          <div style='font-family:Arial,Helvetica,sans-serif;font-size:9.5px;color:#A08A3C;
                      letter-spacing:2.5px;margin-bottom:12px;'>CÓDIGO DE VERIFICACIÓN</div>
          <div style='font-family:Consolas,Courier New,monospace;font-size:34px;font-weight:bold;
                      letter-spacing:10px;color:#1B7A52;'>{codigo}</div>
        </div>

        <table role='presentation' cellpadding='0' cellspacing='0' border='0'
               style='width:100%;margin-top:24px;font-family:Arial,Helvetica,sans-serif;'>
          <tr>
            <td style='padding:9px 0;font-size:13px;color:#6E7D74;border-bottom:1px solid #EFEADD;
                       width:130px;'>Solicitado</td>
            <td style='padding:9px 0;font-size:13px;color:#14231C;border-bottom:1px solid #EFEADD;'>{fecha}</td>
          </tr>
          <tr>
            <td style='padding:9px 0;font-size:13px;color:#6E7D74;border-bottom:1px solid #EFEADD;'>Válido por</td>
            <td style='padding:9px 0;font-size:13px;color:#14231C;border-bottom:1px solid #EFEADD;'>30 minutos, un solo uso</td>
          </tr>
        </table>
      </div>"

        Return Envolver(interior,
            "Si no fuiste vos, no hace falta que hagas nada: sin este código nadie " &
            "puede cambiar tu contraseña.")
    End Function

    ''' <summary>El correo que avisa de un cambio en la cuenta.</summary>
    Private Shared Function CuerpoAviso(nombre As String, titulo As String, detalle As String) As String
        Dim saludo = If(String.IsNullOrWhiteSpace(nombre), "Hola", $"Hola, {nombre}")
        Dim cuando = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy, HH:mm")

        Dim interior = $"
      <div style='padding:30px;'>
        <p style='font-size:16px;color:#14231C;margin:0 0 8px;'>{saludo}:</p>
        <p style='font-size:18px;color:#14231C;font-weight:bold;margin:0 0 12px;'>{titulo}</p>
        <p style='font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#6E7D74;
                  margin:0 0 24px;line-height:1.65;'>{detalle}</p>

        <table role='presentation' cellpadding='0' cellspacing='0' border='0'
               style='width:100%;font-family:Arial,Helvetica,sans-serif;'>
          <tr>
            <td style='padding:9px 0;font-size:13px;color:#6E7D74;border-bottom:1px solid #EFEADD;
                       width:130px;'>Cuándo</td>
            <td style='padding:9px 0;font-size:13px;color:#14231C;border-bottom:1px solid #EFEADD;'>{cuando}</td>
          </tr>
        </table>
      </div>"

        Return Envolver(interior,
            "Si fuiste vos, ignorá este mensaje. Si NO fuiste vos, tu cuenta puede " &
            "estar en riesgo: recuperala cuanto antes o avisá a la biblioteca.")
    End Function
End Class
