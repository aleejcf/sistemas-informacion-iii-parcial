Imports System.IO
Imports System.Net
Imports System.Net.Mail
Imports System.Text

''' <summary>Envío de correo por SMTP. Lo usa la recuperación de contraseña para
''' mandarle al dueño de la cuenta su código de verificación.
'''
''' La cuenta y su contraseña de aplicación NO viven en el código: se leen de un
''' archivo en la carpeta del usuario, así la credencial no viaja con el proyecto
''' ni queda escrita en el repositorio.
'''
''' REGLA QUE NO SE ROMPE: si no hay servidor configurado, esta vía NO SE OFRECE.
''' Antes había un "modo de respaldo" que enseñaba el código en pantalla cuando el
''' envío no estaba configurado, y eso no era recuperar una cuenta: era regalarla.
''' Cualquiera escribía el correo de otro, leía el código en su propia pantalla y
''' le cambiaba la contraseña. Un código que no llega a su dueño no verifica nada,
''' así que sin correo esta puerta se queda cerrada y se usan los códigos de
''' respaldo o la pregunta de seguridad.</summary>
Public Class CorreoService

    ''' <summary>Datos de la cuenta que envía. Los llena el archivo de configuración.</summary>
    Public Class Configuracion
        Public Property Servidor As String = "smtp.gmail.com"
        Public Property Puerto As Integer = 587
        Public Property Remitente As String = ""
        Public Property Clave As String = ""
        Public Property Nombre As String = "ALAS Honduras"

        Public ReadOnly Property EstaCompleta As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(Remitente) AndAlso
                       Not String.IsNullOrWhiteSpace(Clave) AndAlso
                       Not String.IsNullOrWhiteSpace(Servidor) AndAlso
                       Puerto > 0
            End Get
        End Property
    End Class

    ''' <summary>El archivo vive en la carpeta del usuario y no junto al ejecutable:
    ''' así sobrevive a recompilaciones y no se copia por accidente al entregar.</summary>
    Public Shared ReadOnly Property RutaConfiguracion As String
        Get
            Dim carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AlasHonduras")
            Directory.CreateDirectory(carpeta)
            Return Path.Combine(carpeta, "correo.config")
        End Get
    End Property

    ' ======================= CONFIGURACIÓN =======================

    ''' <summary>Lee el archivo. Nunca lanza: si algo falla devuelve una
    ''' configuración vacía y la vía del correo simplemente no se ofrece.</summary>
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
                        ' Google enseña la contraseña de aplicación en grupos de
                        ' cuatro ("abcd efgh ijkl mnop"), pero esos espacios son solo
                        ' para leerla: copiados tal cual, la autenticación falla.
                        config.Clave = valor.Replace(" ", "").Replace(vbTab, "")
                    Case "servidor" : config.Servidor = valor
                    Case "nombre" : config.Nombre = valor
                    Case "puerto"
                        Dim puerto As Integer
                        If Integer.TryParse(valor, puerto) Then config.Puerto = puerto
                End Select
            Next

        Catch ex As Exception
            Registro.Advertencia($"No se pudo leer la configuración de correo: {ex.Message}")
            Return New Configuracion()
        End Try

        Return config
    End Function

    Public Shared Function EstaDisponible() As Boolean
        Return Leer().EstaCompleta
    End Function

    ''' <summary>Crea el archivo de ejemplo la primera vez, con las instrucciones
    ''' dentro, para que solo haya que rellenar dos líneas.</summary>
    Public Shared Sub CrearPlantillaSiFalta()
        Try
            If File.Exists(RutaConfiguracion) Then Return

            File.WriteAllText(RutaConfiguracion,
"# ============================================================================
#  ALAS Honduras — configuración del correo saliente
# ============================================================================
#  Sirve para enviar el código de verificación cuando alguien recupera su
#  contraseña. Mientras esté vacío, esa vía NO se ofrece en la pantalla de
#  recuperación y se usan los códigos de respaldo o la pregunta de seguridad.
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
nombre    = ALAS Honduras
", Encoding.UTF8)

            Registro.Info($"Se creó la plantilla de configuración de correo en {RutaConfiguracion}")

        Catch ex As Exception
            Registro.Advertencia($"No se pudo crear la plantilla de correo: {ex.Message}")
        End Try
    End Sub

    ' ======================= ENVÍO =======================

    ''' <summary>Envía el código de verificación. Devuelve Nothing si salió, o el
    ''' mensaje de error para enseñar en pantalla.
    '''
    ''' Es una operación de red y puede tardar varios segundos: hay que llamarla
    ''' desde un Task.Run para no congelar la ventana.</summary>
    Public Shared Function EnviarCodigo(destinatario As String, nombreDestinatario As String,
                                        codigo As String) As String
        Dim config = Leer()
        If Not config.EstaCompleta Then
            Return "El envío de correo no está configurado en este equipo."
        End If

        Try
            Using mensaje As New MailMessage()
                mensaje.From = New MailAddress(config.Remitente, config.Nombre)
                mensaje.To.Add(New MailAddress(destinatario))
                mensaje.Subject = $"Tu código de verificación: {codigo}"
                mensaje.Body = CuerpoHtml(nombreDestinatario, codigo)
                mensaje.IsBodyHtml = True
                mensaje.BodyEncoding = Encoding.UTF8
                mensaje.SubjectEncoding = Encoding.UTF8

                Using cliente As New SmtpClient(config.Servidor, config.Puerto)
                    cliente.EnableSsl = True
                    cliente.DeliveryMethod = SmtpDeliveryMethod.Network
                    ' UseDefaultCredentials se apaga ANTES de asignar las propias:
                    ' al revés, .NET las descarta silenciosamente.
                    cliente.UseDefaultCredentials = False
                    cliente.Credentials = New NetworkCredential(config.Remitente, config.Clave)
                    cliente.Timeout = 20000

                    cliente.Send(mensaje)
                End Using
            End Using

            ' La bitácora registra a quién se le envió, nunca la credencial ni el código
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

    ''' <summary>Traduce las fallas de SMTP a algo sobre lo que se pueda actuar.</summary>
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

    ''' <summary>El correo que le llega al usuario, con la identidad de ALAS: el
    ''' azul noche del encabezado, el ámbar del código y la tipografía del tablero.</summary>
    Private Shared Function CuerpoHtml(nombre As String, codigo As String) As String
        Dim saludo = If(String.IsNullOrWhiteSpace(nombre), "Hola", $"Hola, {nombre}")

        Return $"
<div style=""font-family:Segoe UI,Arial,sans-serif;background:#EFF3F8;padding:28px;"">
  <div style=""max-width:520px;margin:0 auto;background:#ffffff;border-radius:14px;overflow:hidden;"">

    <div style=""background:#08182F;padding:22px 28px;"">
      <div style=""font-size:22px;font-weight:bold;color:#ffffff;letter-spacing:4px;"">A L A S</div>
      <div style=""font-size:11px;color:#8AA8C8;margin-top:4px;letter-spacing:2px;"">
        H O N D U R A S
      </div>
    </div>

    <div style=""padding:28px;"">
      <p style=""font-size:15px;color:#0B1B2B;margin:0 0 6px;"">{saludo}:</p>
      <p style=""font-size:14px;color:#64748B;margin:0 0 22px;line-height:1.5;"">
        Alguien pidió recuperar la contraseña de tu cuenta. Este es tu código de verificación:
      </p>

      <div style=""background:#EFF3F8;border:1px solid #DDE5EE;border-radius:12px;
                  padding:18px;text-align:center;"">
        <div style=""font-family:Consolas,monospace;font-size:34px;font-weight:bold;
                    letter-spacing:9px;color:#0C7CD5;"">{codigo}</div>
        <div style=""font-size:11px;color:#64748B;margin-top:8px;"">Vence en 30 minutos</div>
      </div>

      <p style=""font-size:12.5px;color:#64748B;margin:22px 0 0;line-height:1.5;"">
        Si no fuiste tú, no hace falta que hagas nada: sin este código nadie puede
        cambiar tu contraseña.
      </p>
    </div>

    <div style=""background:#EFF3F8;padding:14px 28px;font-size:11px;color:#64748B;
                border-top:1px solid #DDE5EE;"">
      Correo automático · No respondas a esta dirección
    </div>
  </div>
</div>"
    End Function
End Class
