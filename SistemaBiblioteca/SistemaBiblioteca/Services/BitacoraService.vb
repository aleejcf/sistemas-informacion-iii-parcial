Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Auditoría en base de datos: quién hizo qué, sobre qué y cuándo.
''' A diferencia del log en archivo (clase Registro), esta bitácora se consulta
''' desde la propia aplicación en la página "Bitácora".</summary>
Public Class BitacoraService

    ' Acciones que el sistema registra. Son constantes para que no se escriban
    ' con variaciones ("login", "Login", "inicio sesion") y el filtro sirva.
    Public Const INICIO_SESION As String = "Inicio de sesión"
    Public Const CIERRE_SESION As String = "Cierre de sesión"
    Public Const REGISTRO_CUENTA As String = "Registro de cuenta"
    Public Const CAMBIO_CLAVE As String = "Cambio de contraseña"
    Public Const RECUPERACION As String = "Recuperación de contraseña"
    Public Const CREAR As String = "Alta"
    Public Const EDITAR As String = "Modificación"
    Public Const ELIMINAR As String = "Baja"
    Public Const PRESTAMO As String = "Préstamo registrado"
    Public Const DEVOLUCION As String = "Devolución registrada"
    Public Const RENOVACION As String = "Préstamo renovado"
    Public Const MULTA_GENERADA As String = "Multa generada"
    Public Const MULTA_PAGADA As String = "Multa pagada"
    Public Const MULTA_CONDONADA As String = "Multa condonada"
    Public Const RESERVA As String = "Reserva"

    ''' <summary>Escribe una entrada. Nunca lanza excepción: que falle la auditoría
    ''' no puede tumbar la operación que el usuario estaba haciendo.</summary>
    Public Shared Sub Registrar(accion As String,
                                Optional entidad As String = Nothing,
                                Optional detalle As String = Nothing,
                                Optional exito As Boolean = True,
                                Optional usuario As String = Nothing)
        Try
            Db.Ejecutar("INSERT INTO bitacora (usuario, accion, entidad, detalle, exito, equipo)
                         VALUES (@u, @a, @en, @d, @ex, @eq)",
                        New SqlParameter("@u", If(usuario, Sesion.NombreUsuario)),
                        New SqlParameter("@a", accion),
                        New SqlParameter("@en", Db.Opcional(entidad)),
                        New SqlParameter("@d", Db.Opcional(TruncarDetalle(detalle))),
                        New SqlParameter("@ex", exito),
                        New SqlParameter("@eq", Environment.MachineName))
        Catch ex As Exception
            Registro.Advertencia($"No se pudo escribir en la bitácora: {ex.Message}")
        End Try
    End Sub

    ''' <summary>La columna `detalle` es VARCHAR(200): si el texto viene más largo
    ''' se recorta en vez de fallar la inserción.</summary>
    Private Shared Function TruncarDetalle(detalle As String) As String
        If String.IsNullOrWhiteSpace(detalle) Then Return Nothing
        Dim limpio = detalle.Trim()
        Return If(limpio.Length <= 200, limpio, limpio.Substring(0, 197) & "...")
    End Function

    Public Shared Function Listar(Optional usuario As String = Nothing,
                                  Optional accion As String = Nothing,
                                  Optional desde As Date? = Nothing) As DataTable
        Return Db.Consultar("EXEC dbo.sp_bitacora @usuario = @u, @accion = @a, @desde = @d",
                            New SqlParameter("@u", Db.Opcional(usuario)),
                            New SqlParameter("@a", Db.Opcional(accion)),
                            New SqlParameter("@d", If(desde.HasValue, CObj(desde.Value), DBNull.Value)))
    End Function

    ''' <summary>Acciones distintas presentes en la bitácora, para llenar el filtro.</summary>
    Public Shared Function Acciones() As DataTable
        Return Db.Consultar("SELECT DISTINCT accion FROM bitacora ORDER BY accion")
    End Function

    Public Shared Function Usuarios() As DataTable
        Return Db.Consultar("SELECT DISTINCT usuario FROM bitacora ORDER BY usuario")
    End Function
End Class
