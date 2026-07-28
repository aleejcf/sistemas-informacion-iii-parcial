Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'TODO: esta línea de código carga datos en la tabla 'ParqueaderoDataSet.cliente' Puede moverla o quitarla según sea necesario.
        Me.ClienteTableAdapter.Fill(Me.ParqueaderoDataSet.cliente)

    End Sub

    ' ---------- NAVEGACIÓN ----------
    Private Sub btnPrimero_Click(sender As Object, e As EventArgs) Handles btnPrimero.Click
        ClienteBindingSource.MoveFirst()
    End Sub

    Private Sub btnAnterior_Click(sender As Object, e As EventArgs) Handles btnAnterior.Click
        ClienteBindingSource.MovePrevious()
    End Sub

    Private Sub btnSiguiente_Click(sender As Object, e As EventArgs) Handles btnSiguiente.Click
        ClienteBindingSource.MoveNext()
    End Sub

    Private Sub btnUltimo_Click(sender As Object, e As EventArgs) Handles btnUltimo.Click
        ClienteBindingSource.MoveLast()
    End Sub

    ' ---------- NUEVO ----------
    Private Sub btnNuevo_Click(sender As Object, e As EventArgs) Handles btnNuevo.Click
        ClienteBindingSource.AddNew()
    End Sub

    ' ---------- GUARDAR ----------
    Private Sub btnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
        Try
            Me.Validate()
            ClienteBindingSource.EndEdit()
            TableAdapterManager.UpdateAll(Me.ParqueaderoDataSet)
            MessageBox.Show("Registro guardado correctamente.", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Error al guardar: " & ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ---------- ELIMINAR ----------
    Private Sub btnEliminar_Click(sender As Object, e As EventArgs) Handles btnEliminar.Click
        If ClienteBindingSource.Count = 0 Then Exit Sub

        Dim respuesta = MessageBox.Show("¿Seguro que deseas eliminar este registro?",
                                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If respuesta = DialogResult.Yes Then
            Try
                ClienteBindingSource.RemoveCurrent()
                TableAdapterManager.UpdateAll(Me.ParqueaderoDataSet)
                MessageBox.Show("Registro eliminado.", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show("Error al eliminar: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub
    Private Sub ClienteBindingSource_CurrentChanged(sender As Object, e As EventArgs) _
        Handles ClienteBindingSource.CurrentChanged
        lblRegistro.Text = $"Registro {ClienteBindingSource.Position + 1} de {ClienteBindingSource.Count}"
    End Sub

    Private Sub btnIrParqueadero_Click(sender As Object, e As EventArgs) Handles btnIrParqueadero.Click
        FormMenu.Show()   ' abre el formulario de parqueadero
        Me.Hide()      ' oculta el actual
    End Sub
End Class
