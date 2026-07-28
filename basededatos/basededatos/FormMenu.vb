
Public Class FormMenu
    Private Sub btnClientes_Click(sender As Object, e As EventArgs) Handles btnClientes.Click
        Form1.Show()
        Me.Hide()
    End Sub

    Private Sub btnParqueaderos_Click(sender As Object, e As EventArgs) Handles btnParqueaderos.Click
        Form2.Show()
        Me.Hide()
    End Sub

    Private Sub btnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
        Application.Exit()
    End Sub
End Class
