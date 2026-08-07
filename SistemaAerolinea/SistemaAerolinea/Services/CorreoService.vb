Imports System.IO
Imports System.Net
Imports System.Net.Mail
Imports System.Net.Mime
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

    ''' <summary>Identificador con el que el HTML referencia al logotipo incrustado.</summary>
    Private Const CONTENT_ID_LOGO As String = "logoAlas"

    ''' <summary>El icono de la aplicación en PNG de 96x96, sacado del mismo
    ''' `alas.ico` que lleva el ejecutable. Va incrustado y no como archivo suelto
    ''' para que el correo no dependa de que exista una carpeta al lado.</summary>
    Private Const LOGO_BASE64 As String = "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"

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
                mensaje.SubjectEncoding = Encoding.UTF8

                ' El logotipo va como recurso vinculado (CID) y no como
                ' "data:image/png;base64,..." dentro del HTML: Gmail bloquea ese
                ' formato y la imagen sale como un icono roto.
                Dim vista = AlternateView.CreateAlternateViewFromString(
                    CuerpoHtml(nombreDestinatario, codigo), Encoding.UTF8, "text/html")
                vista.LinkedResources.Add(LogoVinculado())
                mensaje.AlternateViews.Add(vista)

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

    ''' <summary>El logotipo que viaja dentro del correo. Es el mismo icono de la
    ''' aplicación, que ya trae su fondo azul noche redondeado: así se ve igual de
    ''' bien tanto si el cliente pinta el correo en claro como en oscuro, sin
    ''' depender de que respete un color de fondo nuestro.</summary>
    Private Shared Function LogoVinculado() As LinkedResource
        Dim bytes = Convert.FromBase64String(LOGO_BASE64)
        Dim recurso As New LinkedResource(New IO.MemoryStream(bytes), MediaTypeNames.Image.Jpeg) With {
            .ContentId = CONTENT_ID_LOGO,
            .ContentType = New ContentType("image/png"),
            .TransferEncoding = TransferEncoding.Base64
        }
        Return recurso
    End Function

    ' ======================= AVISOS DE SEGURIDAD =======================

    ''' <summary>Avisa al dueño de una cuenta de que algo cambió en ella: su
    ''' contraseña o su correo.
    '''
    ''' No es cortesía, es seguridad. Quien se apodera de una cuenta lo primero que
    ''' hace es cambiar la contraseña y el correo para dejar fuera al dueño. Este
    ''' aviso es lo que le da la oportunidad de enterarse a tiempo, y por eso el de
    ''' cambio de correo se manda a la dirección VIEJA: la nueva ya sería la del
    ''' atacante.
    '''
    ''' Devuelve Nothing si salió, o el motivo. Quien llama NO debe tratar el fallo
    ''' como un error de la operación: el cambio ya se hizo, y no avisar es un mal
    ''' menor frente a deshacerlo.</summary>
    Public Shared Function EnviarAviso(destinatario As String, nombreDestinatario As String,
                                       titulo As String, detalle As String) As String
        Dim config = Leer()
        If Not config.EstaCompleta Then
            Return "El envío de correo no está configurado en este equipo."
        End If

        Try
            Using mensaje As New MailMessage()
                mensaje.From = New MailAddress(config.Remitente, config.Nombre)
                mensaje.To.Add(New MailAddress(destinatario))
                mensaje.Subject = titulo
                mensaje.SubjectEncoding = Encoding.UTF8

                Dim vista = AlternateView.CreateAlternateViewFromString(
                    CuerpoAviso(nombreDestinatario, titulo, detalle), Encoding.UTF8, "text/html")
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
    ''' azul noche, el interior variable y el pie.
    '''
    ''' Habla el idioma del sistema —es una tarjeta de embarque— y todo el estilo
    ''' va EN LÍNEA, no en una hoja aparte: Gmail en el teléfono se salta buena
    ''' parte de lo que venga en un bloque `style`, y el correo se lee sobre todo
    ''' en el móvil.</summary>
    Private Shared Function Envolver(interior As String, pie As String) As String
        Return $"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#EFF3F8;'>
<div style='font-family:Segoe UI,Arial,Helvetica,sans-serif;background:#EFF3F8;padding:30px 16px;'>
  <div style='max-width:520px;margin:0 auto;'>

    <div style='text-align:center;padding-bottom:22px;'>
      <img src='cid:{CONTENT_ID_LOGO}' alt='ALAS Honduras' width='56' height='56'
           style='width:56px;height:56px;display:inline-block;border:0;'/>
    </div>

    <div style='background:#ffffff;border:1px solid #DDE5EE;border-radius:14px;overflow:hidden;'>

      <div style='background:#08182F;padding:20px 28px;'>
        <div style='font-size:20px;font-weight:bold;color:#ffffff;letter-spacing:5px;'>A L A S</div>
        <div style='font-size:10px;color:#8AA8C8;margin-top:5px;letter-spacing:3px;'>H O N D U R A S</div>
      </div>

      {interior}

      <div style='border-top:2px dashed #DDE5EE;'></div>

      <div style='background:#F6F9FC;padding:18px 28px;'>
        <p style='font-size:12.5px;color:#64748B;margin:0;line-height:1.6;'>{pie}</p>
      </div>
    </div>

    <div style='padding:20px 8px 0;text-align:center;'>
      <p style='font-size:12px;color:#8AA8C8;margin:0 0 6px;font-style:italic;'>
        Conectamos Honduras con el mundo.
      </p>
      <p style='font-size:11px;color:#A8B8CC;margin:0;'>
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
        Dim fecha = DateTime.Now.ToString("dd MMM yyyy").ToUpperInvariant()

        Dim interior = $"
      <div style='padding:28px 28px 22px;'>
        <p style='font-size:15px;color:#0B1B2B;margin:0 0 6px;'>{saludo}:</p>
        <p style='font-size:14px;color:#64748B;margin:0 0 24px;line-height:1.6;'>
          Alguien pidió recuperar la contraseña de tu cuenta. Este es tu código:
        </p>

        <div style='font-size:10px;font-weight:bold;color:#8AA8C8;letter-spacing:1.5px;margin-bottom:8px;'>
          CÓDIGO DE VERIFICACIÓN
        </div>
        <div style='background:#08182F;border-radius:10px;padding:20px;text-align:center;'>
          <div style='font-family:Consolas,Courier New,monospace;font-size:34px;font-weight:bold;
                      letter-spacing:10px;color:#F2B01E;'>{codigo}</div>
        </div>

        <table role='presentation' cellpadding='0' cellspacing='0' border='0'
               style='width:100%;margin-top:22px;'>
          <tr>
            <td style='width:50%;padding-right:10px;'>
              <div style='font-size:10px;font-weight:bold;color:#8AA8C8;letter-spacing:1.5px;'>EMITIDO</div>
              <div style='font-family:Consolas,Courier New,monospace;font-size:13px;color:#0B1B2B;
                          font-weight:bold;padding-top:4px;'>{fecha}</div>
            </td>
            <td style='width:50%;'>
              <div style='font-size:10px;font-weight:bold;color:#8AA8C8;letter-spacing:1.5px;'>VENCE EN</div>
              <div style='font-family:Consolas,Courier New,monospace;font-size:13px;color:#0B1B2B;
                          font-weight:bold;padding-top:4px;'>30 MINUTOS</div>
            </td>
          </tr>
        </table>
      </div>"

        Return Envolver(interior,
            "Si no fuiste tú, no hace falta que hagas nada: sin este código nadie " &
            "puede cambiar tu contraseña.")
    End Function

    ''' <summary>El correo que avisa de un cambio en la cuenta.</summary>
    Private Shared Function CuerpoAviso(nombre As String, titulo As String, detalle As String) As String
        Dim saludo = If(String.IsNullOrWhiteSpace(nombre), "Hola", $"Hola, {nombre}")
        Dim cuando = DateTime.Now.ToString("dd MMM yyyy · HH:mm").ToUpperInvariant()

        Dim interior = $"
      <div style='padding:28px 28px 22px;'>
        <p style='font-size:15px;color:#0B1B2B;margin:0 0 6px;'>{saludo}:</p>
        <p style='font-size:16px;color:#0B1B2B;font-weight:bold;margin:0 0 10px;'>{titulo}</p>
        <p style='font-size:14px;color:#64748B;margin:0 0 22px;line-height:1.6;'>{detalle}</p>

        <div style='font-size:10px;font-weight:bold;color:#8AA8C8;letter-spacing:1.5px;'>CUÁNDO</div>
        <div style='font-family:Consolas,Courier New,monospace;font-size:13px;color:#0B1B2B;
                    font-weight:bold;padding-top:4px;'>{cuando}</div>
      </div>"

        Return Envolver(interior,
            "Si fuiste tú, ignora este mensaje. Si NO fuiste tú, tu cuenta puede " &
            "estar en riesgo: recupérala cuanto antes o avisa a la aerolínea.")
    End Function
End Class
