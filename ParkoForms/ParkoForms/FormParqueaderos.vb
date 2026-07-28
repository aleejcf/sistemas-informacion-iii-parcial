Imports System.Data
Imports System.IO

Public Class FormParqueaderos
    Inherits Form

    Private dgv As DataGridView
    Private txtCodigo, txtDireccion, txtTelefono, txtNit, txtAdministrador, txtOperador, txtHorario As TextBox
    Private pbFoto As PictureBox
    Private btnNuevo, btnGuardar, btnEliminar As Button
    Private editando As Boolean = False
    Private cargandoLista As Boolean = False

    Private ReadOnly Property CarpetaFotos As String
        Get
            Return Path.Combine(AppContext.BaseDirectory, "fotos")
        End Get
    End Property

    Public Sub New()
        Me.Text = "Parqueaderos — PARKO"
        Me.ClientSize = New Size(1080, 700)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Fondo
        Me.Font = New Font("Segoe UI", 9.0F)

        ' ===== Lista =====
        Dim panelLista As New Panel With {.Location = New Point(12, 12), .Size = New Size(690, 676), .BackColor = Color.White}
        dgv = New DataGridView With {.Location = New Point(16, 16), .Size = New Size(658, 644)}
        EstilizarGrid(dgv)
        AddHandler dgv.SelectionChanged, AddressOf dgv_SelectionChanged
        panelLista.Controls.Add(dgv)

        ' ===== Formulario =====
        Dim panelForm As New Panel With {.Location = New Point(714, 12), .Size = New Size(354, 676), .BackColor = Color.White}
        Dim lblTitulo As New Label With {
            .Text = "Datos del parqueadero", .AutoSize = True, .Location = New Point(16, 12),
            .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }

        pbFoto = New PictureBox With {
            .Location = New Point(16, 44), .Size = New Size(320, 130),
            .BorderStyle = BorderStyle.FixedSingle, .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.FromArgb(248, 250, 252)
        }
        Dim btnFoto As New Button With {.Text = "📷 Seleccionar foto…", .Size = New Size(320, 30),
            .Location = New Point(16, 180)}
        EstilizarBoton(btnFoto, Gris)
        btnFoto.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        AddHandler btnFoto.Click, AddressOf btnFoto_Click

        panelForm.Controls.Add(EtiquetaCampo("CÓDIGO (AUTOMÁTICO)", 16, 222))
        txtCodigo = CajaTexto(16, 240, 120)
        txtCodigo.ReadOnly = True
        txtCodigo.BackColor = Fondo

        panelForm.Controls.Add(EtiquetaCampo("DIRECCIÓN", 16, 278))
        txtDireccion = CajaTexto(16, 296, 320)
        panelForm.Controls.Add(EtiquetaCampo("TELÉFONO", 16, 334))
        txtTelefono = CajaTexto(16, 352, 320)
        panelForm.Controls.Add(EtiquetaCampo("NIT", 16, 390))
        txtNit = CajaTexto(16, 408, 320)
        panelForm.Controls.Add(EtiquetaCampo("ADMINISTRADOR", 16, 446))
        txtAdministrador = CajaTexto(16, 464, 320)
        panelForm.Controls.Add(EtiquetaCampo("OPERADOR", 16, 502))
        txtOperador = CajaTexto(16, 520, 320)
        panelForm.Controls.Add(EtiquetaCampo("HORARIO", 16, 558))
        txtHorario = CajaTexto(16, 576, 320)

        btnNuevo = New Button With {.Text = "➕ Nuevo", .Size = New Size(100, 38), .Location = New Point(16, 620)}
        EstilizarBoton(btnNuevo, Gris)
        AddHandler btnNuevo.Click, Sub() LimpiarFormulario()
        btnGuardar = New Button With {.Text = "💾 Guardar", .Size = New Size(102, 38), .Location = New Point(124, 620)}
        EstilizarBoton(btnGuardar, VerdeBoton)
        AddHandler btnGuardar.Click, AddressOf btnGuardar_Click
        btnEliminar = New Button With {.Text = "🗑 Eliminar", .Size = New Size(102, 38), .Location = New Point(234, 620)}
        EstilizarBoton(btnEliminar, Peligro)
        AddHandler btnEliminar.Click, AddressOf btnEliminar_Click

        panelForm.Controls.AddRange({lblTitulo, pbFoto, btnFoto, txtCodigo, txtDireccion, txtTelefono,
                                     txtNit, txtAdministrador, txtOperador, txtHorario,
                                     btnNuevo, btnGuardar, btnEliminar})
        Me.Controls.AddRange({panelLista, panelForm})
    End Sub

    Private Sub FormParqueaderos_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not Sesion.EsAdministrador Then
            btnEliminar.Enabled = False
            Dim ayuda As New ToolTip()
            ayuda.SetToolTip(btnEliminar, "Solo un Administrador puede eliminar registros.")
        End If
        CargarLista()
        LimpiarFormulario()
    End Sub

    Private Sub CargarLista()
        Try
            cargandoLista = True
            dgv.DataSource = ParqueaderoService.Listar()
            If dgv.Columns.Count > 0 Then
                dgv.Columns("codigo_parqueadero").HeaderText = "Código"
                dgv.Columns("codigo_parqueadero").FillWeight = 50
                dgv.Columns("direccion").HeaderText = "Dirección"
                dgv.Columns("direccion").FillWeight = 130
                dgv.Columns("telefono").HeaderText = "Teléfono"
                dgv.Columns("nit").HeaderText = "NIT"
                dgv.Columns("administrador").HeaderText = "Administrador"
                dgv.Columns("operador").HeaderText = "Operador"
                dgv.Columns("horario").HeaderText = "Horario"
            End If
            dgv.ClearSelection()
        Catch ex As Exception
            DialogoParko.Show("Error al cargar parqueaderos: " & ex.Message, "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            cargandoLista = False
        End Try
    End Sub

    Private Sub dgv_SelectionChanged(sender As Object, e As EventArgs)
        If cargandoLista OrElse dgv.SelectedRows.Count = 0 Then Return
        Dim fila = TryCast(dgv.SelectedRows(0).DataBoundItem, DataRowView)
        If fila Is Nothing Then Return

        editando = True
        txtCodigo.Text = fila("codigo_parqueadero").ToString()
        txtDireccion.Text = fila("direccion").ToString()
        txtTelefono.Text = fila("telefono").ToString()
        txtNit.Text = fila("nit").ToString()
        txtAdministrador.Text = fila("administrador").ToString()
        txtOperador.Text = fila("operador").ToString()
        txtHorario.Text = fila("horario").ToString()
        MostrarFoto(txtCodigo.Text)
    End Sub

    ' ---------- Foto ----------
    Private Sub MostrarFoto(codigo As String)
        Dim ruta = Path.Combine(CarpetaFotos, codigo & ".jpg")
        If File.Exists(ruta) Then
            ' Se carga en memoria para no bloquear el archivo
            pbFoto.Image = Image.FromStream(New MemoryStream(File.ReadAllBytes(ruta)))
        Else
            pbFoto.Image = Nothing
        End If
    End Sub

    Private Sub btnFoto_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtCodigo.Text) Then Return

        Using dialogo As New OpenFileDialog With {
            .Filter = "Imágenes|*.jpg;*.jpeg;*.png",
            .Title = "Seleccionar foto del parqueadero"}
            If dialogo.ShowDialog() <> DialogResult.OK Then Return

            Try
                Directory.CreateDirectory(CarpetaFotos)
                File.Copy(dialogo.FileName, Path.Combine(CarpetaFotos, txtCodigo.Text.Trim() & ".jpg"), True)
                MostrarFoto(txtCodigo.Text.Trim())
            Catch ex As Exception
                DialogoParko.Show("No se pudo copiar la foto: " & ex.Message, "Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    ' ---------- Botones ----------
    Private Sub btnGuardar_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtCodigo.Text) OrElse String.IsNullOrWhiteSpace(txtDireccion.Text) Then
            DialogoParko.Show("Completa al menos la dirección.", "Campos incompletos",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            If editando Then
                ParqueaderoService.Actualizar(txtCodigo.Text, txtDireccion.Text, txtTelefono.Text,
                                              txtNit.Text, txtAdministrador.Text, txtOperador.Text, txtHorario.Text)
                DialogoParko.Show("Parqueadero actualizado correctamente.", "Éxito")
            Else
                ParqueaderoService.Insertar(txtCodigo.Text, txtDireccion.Text, txtTelefono.Text,
                                            txtNit.Text, txtAdministrador.Text, txtOperador.Text, txtHorario.Text)
                DialogoParko.Show("Parqueadero registrado correctamente.", "Éxito")
            End If
            CargarLista()
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show("Error al guardar: " & ex.Message, "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As EventArgs)
        If Not editando Then
            DialogoParko.Show("Selecciona primero un parqueadero de la lista.", "Sin selección",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If DialogoParko.Show($"¿Eliminar el parqueadero '{txtDireccion.Text}'?", "Confirmar",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        Try
            ParqueaderoService.Eliminar(txtCodigo.Text)
            DialogoParko.Show("Parqueadero eliminado.", "Éxito")
            CargarLista()
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show("No se pudo eliminar. Es posible que tenga clientes o vehículos asociados.",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editando = False
        Try
            txtCodigo.Text = ParqueaderoService.SiguienteCodigo()
        Catch
            txtCodigo.Text = ""
        End Try
        txtDireccion.Clear()
        txtTelefono.Clear()
        txtNit.Clear()
        txtAdministrador.Clear()
        txtOperador.Clear()
        txtHorario.Clear()
        pbFoto.Image = Nothing
        dgv.ClearSelection()
        txtDireccion.Focus()
    End Sub
End Class
