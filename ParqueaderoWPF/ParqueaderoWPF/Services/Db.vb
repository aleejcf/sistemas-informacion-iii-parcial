Imports System.Data
Imports System.Threading.Tasks
Imports Microsoft.Data.SqlClient

''' <summary>Conexión central a SQL Server. Todas las consultas usan parámetros
''' para evitar inyección SQL.</summary>
Public Class Db

    ' Estos tres datos salen de appsettings.json; ver Configuracion.vb para el
    ' porqué. Dejaron de ser Const porque una constante se resuelve al compilar,
    ' que es justo lo que ataba el ejecutable a una sola máquina.
    Public Shared ReadOnly Property SERVIDOR As String
        Get
            Return Configuracion.Servidor
        End Get
    End Property

    Public Shared ReadOnly Property BASE_DATOS As String
        Get
            Return Configuracion.BaseDatos
        End Get
    End Property

    Private Shared ReadOnly Property CADENA As String
        Get
            Return $"Data Source={SERVIDOR};Initial Catalog={BASE_DATOS}" &
                   $";Integrated Security=True;TrustServerCertificate=True" &
                   $";Connect Timeout={Configuracion.SegundosDeEspera}"
        End Get
    End Property

    Public Shared Function Conexion() As SqlConnection
        Return New SqlConnection(CADENA)
    End Function

    Public Shared Function Consultar(sql As String, ParamArray parametros As SqlParameter()) As DataTable
        Using cn As SqlConnection = Conexion(), cmd As New SqlCommand(sql, cn)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Dim dt As New DataTable()
            Using da As New SqlDataAdapter(cmd)
                da.Fill(dt)
            End Using
            Return dt
        End Using
    End Function

    Public Shared Function Ejecutar(sql As String, ParamArray parametros As SqlParameter()) As Integer
        Using cn As SqlConnection = Conexion(), cmd As New SqlCommand(sql, cn)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            cn.Open()
            Return cmd.ExecuteNonQuery()
        End Using
    End Function

    Public Shared Function Escalar(sql As String, ParamArray parametros As SqlParameter()) As Object
        Using cn As SqlConnection = Conexion(), cmd As New SqlCommand(sql, cn)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            cn.Open()
            Return cmd.ExecuteScalar()
        End Using
    End Function

    ' ---------- Versiones asíncronas ----------
    '
    ' Cada consulta de esta clase bloquea el hilo que la llama; con SQL Server en
    ' la misma red casi no se nota, pero es el hilo de la interfaz el que se
    ' congela mientras tanto. Estas son la misma operación sin bloquear, para
    ' pantallas nuevas o que se vayan actualizando: las síncronas de arriba
    ' siguen ahí y no las usa nadie menos porque estas existan.

    Public Shared Async Function ConsultarAsync(sql As String, ParamArray parametros As SqlParameter()) As Task(Of DataTable)
        Using cn As SqlConnection = Conexion(), cmd As New SqlCommand(sql, cn)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Await cn.OpenAsync()
            Dim dt As New DataTable()
            Using lector = Await cmd.ExecuteReaderAsync()
                dt.Load(lector)
            End Using
            Return dt
        End Using
    End Function

    Public Shared Async Function EjecutarAsync(sql As String, ParamArray parametros As SqlParameter()) As Task(Of Integer)
        Using cn As SqlConnection = Conexion(), cmd As New SqlCommand(sql, cn)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Await cn.OpenAsync()
            Return Await cmd.ExecuteNonQueryAsync()
        End Using
    End Function

    Public Shared Async Function EscalarAsync(sql As String, ParamArray parametros As SqlParameter()) As Task(Of Object)
        Using cn As SqlConnection = Conexion(), cmd As New SqlCommand(sql, cn)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Await cn.OpenAsync()
            Return Await cmd.ExecuteScalarAsync()
        End Using
    End Function

    ' ---------- Diagnóstico ----------

    ''' <summary>Prueba que el servidor responda. La usa la pantalla de bienvenida
    ''' para avisar antes de que el usuario intente iniciar sesión a ciegas.</summary>
    Public Shared Function HayConexion() As Boolean
        Try
            Escalar("SELECT 1")
            Return True
        Catch ex As Exception
            Registro.Advertencia($"Sin conexión a la base de datos: {ex.Message}")
            Return False
        End Try
    End Function
End Class
