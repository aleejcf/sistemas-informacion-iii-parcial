Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Registro estructurado de intentos de inicio de sesión, consultable desde la BD
''' (a diferencia de Registro.vb, que solo escribe a un archivo de texto en disco).</summary>
Public Class AuditoriaService

    Public Shared Sub Registrar(usuario As String, exito As Boolean, Optional detalle As String = Nothing)
        Try
            Db.Ejecutar("INSERT INTO auditoria_login (usuario, exito, detalle, equipo)
                         VALUES (@u, @e, @d, @eq)",
                        New SqlParameter("@u", If(usuario, "").Trim()),
                        New SqlParameter("@e", exito),
                        New SqlParameter("@d", If(detalle, "").Trim()),
                        New SqlParameter("@eq", Environment.MachineName))
        Catch ex As Exception
            ' Un fallo al auditar no debe impedir que el usuario inicie sesión
            Registro.Error_("Registrar auditoría de login", ex)
        End Try
    End Sub

    Public Shared Function Listar(Optional filtro As String = "") As DataTable
        If String.IsNullOrWhiteSpace(filtro) Then
            Return Db.Consultar("SELECT TOP 200 usuario,
                                        CASE WHEN exito = 1 THEN 'Correcto' ELSE 'Fallido' END AS resultado,
                                        detalle, equipo, fecha
                                 FROM auditoria_login ORDER BY fecha DESC")
        End If
        Return Db.Consultar("SELECT TOP 200 usuario,
                                    CASE WHEN exito = 1 THEN 'Correcto' ELSE 'Fallido' END AS resultado,
                                    detalle, equipo, fecha
                             FROM auditoria_login
                             WHERE usuario LIKE @f
                             ORDER BY fecha DESC",
                            New SqlParameter("@f", "%" & filtro.Trim() & "%"))
    End Function
End Class
