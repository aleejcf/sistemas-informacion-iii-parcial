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
                mensaje.Body = CuerpoHtml(nombreDestinatario, codigo)
                mensaje.IsBodyHtml = True
                mensaje.BodyEncoding = Text.Encoding.UTF8
                mensaje.SubjectEncoding = Text.Encoding.UTF8

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

    ''' <summary>El correo que le llega al usuario, con la marca de la biblioteca.</summary>
    Private Shared Function CuerpoHtml(nombre As String, codigo As String) As String
        Dim saludo = If(String.IsNullOrWhiteSpace(nombre), "Hola", $"Hola, {nombre}")

        Return $"
<div style=""font-family:Segoe UI,Arial,sans-serif;background:#F5F2EA;padding:28px;"">
  <div style=""max-width:520px;margin:0 auto;background:#ffffff;border-radius:14px;overflow:hidden;"">

    <div style=""background:#14281E;padding:22px 28px;"">
      <div style=""font-family:Georgia,serif;font-size:21px;font-weight:bold;color:#ffffff;"">
        ALEJAN<span style=""color:#C9A227;"">DRÍA</span>
      </div>
      <div style=""font-size:11px;color:#9DBBAA;margin-top:3px;"">
        Sistema de gestión bibliotecaria
      </div>
    </div>

    <div style=""padding:28px;"">
      <p style=""font-size:15px;color:#14231C;margin:0 0 6px;"">{saludo}:</p>
      <p style=""font-size:14px;color:#6E7D74;margin:0 0 22px;line-height:1.5;"">
        Alguien pidió recuperar la contraseña de tu cuenta. Este es tu código de verificación:
      </p>

      <div style=""background:#F5F2EA;border:1px solid #E3DED0;border-radius:12px;
                  padding:18px;text-align:center;"">
        <div style=""font-family:Consolas,monospace;font-size:34px;font-weight:bold;
                    letter-spacing:9px;color:#1B7A52;"">{codigo}</div>
        <div style=""font-size:11px;color:#6E7D74;margin-top:8px;"">Vence en 30 minutos</div>
      </div>

      <p style=""font-size:12.5px;color:#6E7D74;margin:22px 0 0;line-height:1.5;"">
        Si no fuiste vos, no hace falta que hagas nada: sin este código nadie puede
        cambiar tu contraseña.
      </p>
    </div>

    <div style=""background:#F5F2EA;padding:14px 28px;font-size:11px;color:#6E7D74;
                border-top:1px solid #E3DED0;"">
      Correo automático · No respondas a esta dirección
    </div>
  </div>
</div>"
    End Function
End Class
