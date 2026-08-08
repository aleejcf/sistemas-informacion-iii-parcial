Imports System.Data
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
End Class
