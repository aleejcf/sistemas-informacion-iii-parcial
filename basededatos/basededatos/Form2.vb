Public Class Form2
    Private Sub ParqueaderoBindingNavigatorSaveItem_Click(sender As Object, e As EventArgs)
        Me.Validate()
        Me.ParqueaderoBindingSource.EndEdit()
        Me.TableAdapterManager.UpdateAll(Me.ParqueaderoDataSet)

    End Sub

    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'TODO: esta línea de código carga datos en la tabla 'ParqueaderoDataSet.parqueadero' Puede moverla o quitarla según sea necesario.
        Me.ParqueaderoTableAdapter.Fill(Me.ParqueaderoDataSet.parqueadero)
        ParqueaderoBindingSource_CurrentChanged(Nothing, Nothing)
    End Sub
    Private Sub ParqueaderoBindingSource_CurrentChanged(sender As Object, e As EventArgs) _
        Handles ParqueaderoBindingSource.CurrentChanged

        Dim fila = TryCast(ParqueaderoBindingSource.Current, DataRowView)
        If fila Is Nothing Then Exit Sub

        Dim codigo = fila("codigo_parqueadero").ToString()
        Dim ruta = IO.Path.Combine(Application.StartupPath, "fotos", codigo & ".jpg")

        If IO.File.Exists(ruta) Then
            PictureBox1.Image = Image.FromFile(ruta)
        Else
            PictureBox1.Image = Nothing
        End If

        lblRegistro.Text = $"Registro {ParqueaderoBindingSource.Position + 1} de {ParqueaderoBindingSource.Count}"
    End Sub

    ' ---------- NAVEGACIÓN ----------
    Private Sub btnPrimero_Click(sender As Object, e As EventArgs) Handles btnPrimero.Click
        ParqueaderoBindingSource.MoveFirst()
    End Sub

    Private Sub btnAnterior_Click(sender As Object, e As EventArgs) Handles btnAnterior.Click
        ParqueaderoBindingSource.MovePrevious()
    End Sub

    Private Sub btnSiguiente_Click(sender As Object, e As EventArgs) Handles btnSiguiente.Click
        ParqueaderoBindingSource.MoveNext()
    End Sub

    Private Sub btnUltimo_Click(sender As Object, e As EventArgs) Handles btnUltimo.Click
        ParqueaderoBindingSource.MoveLast()
    End Sub

    ' ---------- NUEVO ----------
    Private Sub btnNuevo_Click(sender As Object, e As EventArgs) Handles btnNuevo.Click
        ParqueaderoBindingSource.AddNew()
    End Sub

    ' ---------- GUARDAR ----------
    Private Sub btnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
        Try
            Me.Validate()
            ParqueaderoBindingSource.EndEdit()
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
        If ParqueaderoBindingSource.Count = 0 Then Exit Sub

        Dim respuesta = MessageBox.Show("¿Seguro que deseas eliminar este registro?",
                                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If respuesta = DialogResult.Yes Then
            Try
                ParqueaderoBindingSource.RemoveCurrent()
                TableAdapterManager.UpdateAll(Me.ParqueaderoDataSet)
                MessageBox.Show("Registro eliminado.", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show("Error al eliminar: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    Private Sub btnIrClientes_Click(sender As Object, e As EventArgs) Handles btnIrClientes.Click
        FormMenu.Show()
        Me.Hide()
    End Sub
End Class
