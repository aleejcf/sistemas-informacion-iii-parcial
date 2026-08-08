Imports System.IO
Imports System.Text.Json

''' <summary>Ajustes que no deben estar quemados en el código, leídos de
''' appsettings.json.
'''
''' El nombre del servidor vivía como constante dentro de Db.vb, y eso ataba el
''' ejecutable a una sola máquina: en cualquier otra computadora —la del
''' profesor, la del laboratorio— el sistema no encontraba la base de datos y no
''' había forma de arreglarlo sin recompilar. Ahora el dato viaja en un archivo
''' de texto junto al .exe y se cambia con el Bloc de notas.
'''
''' Si el archivo no está, o está mal escrito, o le falta una clave, se usan los
''' mismos valores que antes estaban en el código. Un ajuste ausente no puede
''' impedir que el programa arranque.
'''
''' La clase se puede instanciar apuntando a cualquier ruta, y no solo usarse por
''' sus propiedades compartidas. Eso es lo que permite que las pruebas la
''' verifiquen con archivos de mentira sin tocar el de verdad.</summary>
Public Class Configuracion

    Public Const ARCHIVO As String = "appsettings.json"

    ' Los valores que Db.vb traía quemados. Son el plan B, no la fuente oficial.
    Public Const SERVIDOR_POR_DEFECTO As String = "ALECALDE\SQLEXPRESS"
    Public Const BASE_DATOS_POR_DEFECTO As String = "db_biblioteca"
    Public Const ESPERA_POR_DEFECTO As Integer = 8

    Private ReadOnly Raiz As JsonElement?
    Private ReadOnly Ruta As String

    Public Sub New(ruta As String)
        Me.Ruta = ruta
        Me.Raiz = Cargar(ruta)
    End Sub

    ''' El archivo se lee una sola vez, al arrancar. Releerlo en cada consulta
    ''' significaría tocar el disco por cada conexión a la base de datos.
    Private Shared ReadOnly Predeterminada As New Configuracion(
        Path.Combine(AppContext.BaseDirectory, ARCHIVO))

    ' ---------- Lo que usa el resto del sistema ----------

    Public Shared ReadOnly Property Servidor As String
        Get
            Return Predeterminada.LeerServidor()
        End Get
    End Property

    Public Shared ReadOnly Property BaseDatos As String
        Get
            Return Predeterminada.LeerBaseDatos()
        End Get
    End Property

    Public Shared ReadOnly Property SegundosDeEspera As Integer
        Get
            Return Predeterminada.LeerSegundosDeEspera()
        End Get
    End Property

    ''' <summary>De dónde salieron los ajustes. La usa la bitácora para que, si
    ''' no hay conexión, se vea si el archivo se leyó o si el sistema está
    ''' corriendo con los valores de respaldo.</summary>
    Public Shared ReadOnly Property Origen As String
        Get
            Return Predeterminada.LeerOrigen()
        End Get
    End Property

    ' ---------- Lo mismo, instanciable, para poder probarlo ----------

    Public Function LeerServidor() As String
        Return Texto("Db", "Servidor", SERVIDOR_POR_DEFECTO)
    End Function

    Public Function LeerBaseDatos() As String
        Return Texto("Db", "BaseDatos", BASE_DATOS_POR_DEFECTO)
    End Function

    ''' <summary>Segundos que se espera al servidor antes de darlo por caído.</summary>
    Public Function LeerSegundosDeEspera() As Integer
        Return Numero("Db", "SegundosDeEspera", ESPERA_POR_DEFECTO)
    End Function

    Public Function LeerOrigen() As String
        Return If(Raiz.HasValue, Ruta, $"valores por defecto (no se pudo leer {ARCHIVO})")
    End Function

    ' ---------- Lectura ----------

    Private Shared Function Cargar(ruta As String) As JsonElement?
        Try
            If String.IsNullOrWhiteSpace(ruta) OrElse Not File.Exists(ruta) Then Return Nothing

            ' Clone() despega el elemento del JsonDocument: sin eso, el Using de
            ' abajo lo desecha y cualquier lectura posterior falla.
            Using documento = JsonDocument.Parse(File.ReadAllText(ruta))
                Return documento.RootElement.Clone()
            End Using
        Catch ex As Exception
            ' Un archivo con una coma de más no puede tumbar el arranque.
            Registro.Advertencia($"No se pudo leer {ARCHIVO}; se usan los valores por defecto. {ex.Message}")
            Return Nothing
        End Try
    End Function

    Private Function Buscar(seccion As String, clave As String) As JsonElement?
        If Not Raiz.HasValue OrElse Raiz.Value.ValueKind <> JsonValueKind.Object Then Return Nothing

        Dim bloque As JsonElement
        If Not Raiz.Value.TryGetProperty(seccion, bloque) Then Return Nothing
        If bloque.ValueKind <> JsonValueKind.Object Then Return Nothing

        Dim valor As JsonElement
        If Not bloque.TryGetProperty(clave, valor) Then Return Nothing

        Return valor
    End Function

    Private Function Texto(seccion As String, clave As String, porDefecto As String) As String
        Dim valor = Buscar(seccion, clave)
        If Not valor.HasValue OrElse valor.Value.ValueKind <> JsonValueKind.String Then Return porDefecto

        ' Una clave escrita pero dejada en blanco es un descuido, no una orden de
        ' conectarse a un servidor sin nombre.
        Dim leido = valor.Value.GetString()
        Return If(String.IsNullOrWhiteSpace(leido), porDefecto, leido)
    End Function

    Private Function Numero(seccion As String, clave As String, porDefecto As Integer) As Integer
        Dim valor = Buscar(seccion, clave)
        If Not valor.HasValue OrElse valor.Value.ValueKind <> JsonValueKind.Number Then Return porDefecto

        Dim leido As Integer
        If Not valor.Value.TryGetInt32(leido) Then Return porDefecto

        ' Un tiempo de espera de cero o negativo no tiene sentido y dejaría las
        ' conexiones colgadas o fallando al instante.
        Return If(leido > 0, leido, porDefecto)
    End Function
End Class
