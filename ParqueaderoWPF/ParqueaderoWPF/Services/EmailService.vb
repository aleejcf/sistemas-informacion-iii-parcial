Imports System.IO
Imports System.Net
Imports System.Net.Mail
Imports System.Text

''' <summary>Envío de correos del sistema (código de recuperación de contraseña)
''' por SMTP.
'''
''' La cuenta y su contraseña de aplicación NO viven en el código. Antes estaban
''' escritas aquí como constantes, y una credencial en el código fuente es una
''' credencial regalada: viaja en el repositorio, queda para siempre en el
''' historial de git aunque después se borre del archivo, y se va con el proyecto
''' a cualquiera que lo reciba. Ahora se leen de un archivo en la carpeta del
''' usuario, que ni se entrega ni se versiona.
'''
''' Si el archivo no está o está incompleto, el envío no se intenta y se dice por
''' qué. Nunca se enseña el código en pantalla como sustituto: un código que no
''' llega a su dueño no verifica que sea él quien lo pide.</summary>
Public Class EmailService

    Public Class Configuracion
        Public Property Servidor As String = "smtp.gmail.com"
        Public Property Puerto As Integer = 587
        Public Property Remitente As String = ""
        Public Property Clave As String = ""
        Public Property Nombre As String = "PARKO Honduras"

        Public ReadOnly Property EstaCompleta As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(Remitente) AndAlso
                       Not String.IsNullOrWhiteSpace(Clave) AndAlso
                       Not String.IsNullOrWhiteSpace(Servidor) AndAlso
                       Puerto > 0
            End Get
        End Property
    End Class

    Public Shared ReadOnly Property RutaConfiguracion As String
        Get
            Dim carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ParkoHonduras")
            Directory.CreateDirectory(carpeta)
            Return Path.Combine(carpeta, "correo.config")
        End Get
    End Property

    ' ======================= CONFIGURACIÓN =======================

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
#  PARKO Honduras — configuración del correo saliente
# ============================================================================
#  Sirve para enviar el código de verificación cuando alguien recupera su
#  contraseña. Mientras esté vacío, esa vía no se ofrece.
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
nombre    = PARKO Honduras
", Encoding.UTF8)

            Registro.Info($"Se creó la plantilla de configuración de correo en {RutaConfiguracion}")

        Catch ex As Exception
            Registro.Advertencia($"No se pudo crear la plantilla de correo: {ex.Message}")
        End Try
    End Sub

    ' ======================= ENVÍO =======================

    ''' <summary>Envía el código de recuperación. Lanza si no se pudo enviar: la
    ''' pantalla lo atrapa y avisa, porque dar por bueno un envío que falló dejaría
    ''' a la persona esperando un correo que no va a llegar nunca.</summary>
    Public Shared Async Function EnviarCodigoRecuperacion(destino As String, nombreCompleto As String,
                                                          codigo As String) As Task
        Dim config = Leer()
        If Not config.EstaCompleta Then
            Throw New InvalidOperationException(
                "El envío de correo no está configurado en este equipo. " &
                $"Rellena el remitente y la clave en {RutaConfiguracion}")
        End If

        Dim cuerpo = $"
            <div style='font-family:Segoe UI,Arial,sans-serif;max-width:480px;margin:auto'>
                <div style='background:#0A2540;padding:22px;border-radius:12px 12px 0 0;text-align:center'>
                    <span style='color:white;font-size:22px;font-weight:bold'>PAR<span style='color:#00E676'>KO</span></span>
                    <div style='color:#8FB0CE;font-size:11px;letter-spacing:1px'>HONDURAS</div>
                </div>
                <div style='background:#F8FAFC;padding:26px;border-radius:0 0 12px 12px'>
                    <p>Hola {WebUtility.HtmlEncode(nombreCompleto)},</p>
                    <p>Usa este código para recuperar el acceso a tu cuenta. Vence en 30 minutos.</p>
                    <div style='background:white;border:1.5px solid #0A2540;border-radius:10px;
                                text-align:center;padding:16px;margin:18px 0'>
                        <span style='font-size:28px;font-weight:bold;letter-spacing:6px;color:#0A2540'>{codigo}</span>
                    </div>
                    <p style='font-size:12px;color:#64748B'>Si no solicitaste este código, ignora este correo.</p>
                </div>
            </div>"

        Using mensaje As New MailMessage()
            mensaje.From = New MailAddress(config.Remitente, config.Nombre)
            mensaje.To.Add(New MailAddress(destino))
            mensaje.Subject = "Código de recuperación — PARKO Honduras"
            mensaje.Body = cuerpo
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

                Await cliente.SendMailAsync(mensaje)
            End Using
        End Using

        ' La bitácora registra a quién se le envió, nunca la credencial ni el código
        Registro.Info($"Código de recuperación enviado por correo a {destino}")
    End Function
End Class
