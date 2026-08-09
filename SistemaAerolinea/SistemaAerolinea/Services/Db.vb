Imports System.Data
Imports System.Runtime.ExceptionServices
Imports System.Threading.Tasks
Imports Microsoft.Data.SqlClient

''' <summary>Punto único de acceso a SQL Server. Toda consulta del sistema pasa
''' por aquí y siempre con parámetros: concatenar valores dentro del SQL es lo
''' que abre la puerta a la inyección SQL.</summary>
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

    ' ---------- Operaciones sueltas ----------

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

    ''' <summary>Devuelve la primera fila de la consulta, o Nothing si no hubo resultados.</summary>
    Public Shared Function ConsultarFila(sql As String, ParamArray parametros As SqlParameter()) As DataRow
        Dim dt = Consultar(sql, parametros)
        Return If(dt.Rows.Count > 0, dt.Rows(0), Nothing)
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

    Public Shared Function Contar(sql As String, ParamArray parametros As SqlParameter()) As Integer
        Dim valor = Escalar(sql, parametros)
        Return If(valor Is Nothing OrElse IsDBNull(valor), 0, CInt(valor))
    End Function

    ' ---------- Operaciones dentro de una transacción ----------
    '
    ' Emitir una reserva toca tres tablas (reserva, boleto y pago). Si algo falla
    ' a mitad de camino no puede quedar una reserva sin boletos ni un asiento
    ' marcado como vendido sin cobrar: o se guarda todo, o no se guarda nada.

    ''' <summary>Ejecuta el trabajo dentro de una transacción. Si el trabajo lanza
    ''' una excepción se deshacen todos los cambios y la excepción se vuelve a lanzar.</summary>
    Public Shared Sub EnTransaccion(trabajo As Action(Of SqlConnection, SqlTransaction))
        Using cn As SqlConnection = Conexion()
            cn.Open()
            ' Serializable: mientras se emite una reserva nadie más puede vender
            ' el mismo asiento; es el nivel que exige un inventario de asientos.
            Using tx As SqlTransaction = cn.BeginTransaction(IsolationLevel.Serializable)
                Try
                    trabajo(cn, tx)
                    tx.Commit()
                Catch
                    Try
                        tx.Rollback()
                    Catch
                        ' Si la conexión ya se cayó, el servidor deshace la transacción solo
                    End Try
                    Throw
                End Try
            End Using
        End Using
    End Sub

    Public Shared Function EjecutarEn(cn As SqlConnection, tx As SqlTransaction, sql As String,
                                      ParamArray parametros As SqlParameter()) As Integer
        Using cmd As New SqlCommand(sql, cn, tx)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Return cmd.ExecuteNonQuery()
        End Using
    End Function

    Public Shared Function EscalarEn(cn As SqlConnection, tx As SqlTransaction, sql As String,
                                     ParamArray parametros As SqlParameter()) As Object
        Using cmd As New SqlCommand(sql, cn, tx)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Return cmd.ExecuteScalar()
        End Using
    End Function

    Public Shared Function ConsultarEn(cn As SqlConnection, tx As SqlTransaction, sql As String,
                                       ParamArray parametros As SqlParameter()) As DataTable
        Using cmd As New SqlCommand(sql, cn, tx)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Dim dt As New DataTable()
            Using lector = cmd.ExecuteReader()
                dt.Load(lector)
            End Using
            Return dt
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

    Public Shared Async Function ConsultarFilaAsync(sql As String, ParamArray parametros As SqlParameter()) As Task(Of DataRow)
        Dim dt = Await ConsultarAsync(sql, parametros)
        Return If(dt.Rows.Count > 0, dt.Rows(0), Nothing)
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

    Public Shared Async Function ContarAsync(sql As String, ParamArray parametros As SqlParameter()) As Task(Of Integer)
        Dim valor = Await EscalarAsync(sql, parametros)
        Return If(valor Is Nothing OrElse IsDBNull(valor), 0, CInt(valor))
    End Function

    ''' <summary>Igual que EnTransaccion, pero el trabajo también es asíncrono.
    '''
    ''' VB no deja usar Await dentro de un Catch, así que la excepción se
    ''' captura primero y el Rollback se espera después, ya fuera del Catch.
    ''' ExceptionDispatchInfo relanza conservando la traza original; un simple
    ''' "Throw capturada" la reescribiría a partir de esta línea.</summary>
    Public Shared Async Function EnTransaccionAsync(trabajo As Func(Of SqlConnection, SqlTransaction, Task)) As Task
        Using cn As SqlConnection = Conexion()
            Await cn.OpenAsync()
            Using tx As SqlTransaction = cn.BeginTransaction(IsolationLevel.Serializable)
                Dim capturada As ExceptionDispatchInfo = Nothing
                Try
                    Await trabajo(cn, tx)
                    Await tx.CommitAsync()
                Catch ex As Exception
                    capturada = ExceptionDispatchInfo.Capture(ex)
                End Try

                If capturada IsNot Nothing Then
                    Try
                        Await tx.RollbackAsync()
                    Catch
                        ' Si la conexión ya se cayó, el servidor deshace la transacción solo
                    End Try
                    capturada.Throw()
                End If
            End Using
        End Using
    End Function

    Public Shared Async Function EjecutarEnAsync(cn As SqlConnection, tx As SqlTransaction, sql As String,
                                                 ParamArray parametros As SqlParameter()) As Task(Of Integer)
        Using cmd As New SqlCommand(sql, cn, tx)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Return Await cmd.ExecuteNonQueryAsync()
        End Using
    End Function

    Public Shared Async Function EscalarEnAsync(cn As SqlConnection, tx As SqlTransaction, sql As String,
                                                ParamArray parametros As SqlParameter()) As Task(Of Object)
        Using cmd As New SqlCommand(sql, cn, tx)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Return Await cmd.ExecuteScalarAsync()
        End Using
    End Function

    Public Shared Async Function ConsultarEnAsync(cn As SqlConnection, tx As SqlTransaction, sql As String,
                                                  ParamArray parametros As SqlParameter()) As Task(Of DataTable)
        Using cmd As New SqlCommand(sql, cn, tx)
            If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
            Dim dt As New DataTable()
            Using lector = Await cmd.ExecuteReaderAsync()
                dt.Load(lector)
            End Using
            Return dt
        End Using
    End Function

    Public Shared Async Function HayConexionAsync() As Task(Of Boolean)
        Try
            Await EscalarAsync("SELECT 1")
            Return True
        Catch ex As Exception
            Registro.Advertencia($"Sin conexión a la base de datos: {ex.Message}")
            Return False
        End Try
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

    ''' <summary>Convierte Nothing en DBNull para que los parámetros opcionales
    ''' lleguen bien a SQL Server.</summary>
    Public Shared Function Opcional(valor As Object) As Object
        If valor Is Nothing Then Return DBNull.Value
        Dim texto = TryCast(valor, String)
        If texto IsNot Nothing AndAlso String.IsNullOrWhiteSpace(texto) Then Return DBNull.Value
        Return valor
    End Function
End Class
