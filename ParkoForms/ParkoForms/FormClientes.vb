Imports System.Data

Public Class FormClientes
    Inherits Form

    Private dgv As DataGridView
    Private txtBuscar, txtCodigo, txtNombre, txtCedula, txtCelular As TextBox
    Private cboParqueadero, cboTipo As ComboBox
    Private btnNuevo, btnGuardar, btnEliminar As Button
    Private lblSoloAdmin As Label
    Private editando As Boolean = False
    Private cargandoLista As Boolean = False

    Public Sub New()
        Me.Text = "Clientes — PARKO"
        Me.ClientSize = New Size(1080, 640)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Fondo
        Me.Font = New Font("Segoe UI", 9.0F)

        ' ===== Lista con búsqueda =====
        Dim panelLista As New Panel With {.Location = New Point(12, 12), .Size = New Size(690, 616), .BackColor = Color.White}
        txtBuscar = CajaTexto(16, 14, 590)
        AddHandler txtBuscar.TextChanged, Sub() CargarLista(txtBuscar.Text)
        Dim btnLimpiar As New Button With {.Text = "✕", .Size = New Size(60, 31), .Location = New Point(612, 13)}
        EstilizarBoton(btnLimpiar, Gris)
        AddHandler btnLimpiar.Click, Sub() txtBuscar.Clear()

        dgv = New DataGridView With {.Location = New Point(16, 58), .Size = New Size(658, 542)}
        EstilizarGrid(dgv)
        AddHandler dgv.SelectionChanged, AddressOf dgv_SelectionChanged
        panelLista.Controls.AddRange({txtBuscar, btnLimpiar, dgv})

        ' ===== Formulario =====
        Dim panelForm As New Panel With {.Location = New Point(714, 12), .Size = New Size(354, 616), .BackColor = Color.White}
        Dim lblTitulo As New Label With {
            .Text = "Datos del cliente", .AutoSize = True, .Location = New Point(16, 12),
            .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }

        panelForm.Controls.Add(EtiquetaCampo("CÓDIGO (AUTOMÁTICO)", 16, 50))
        txtCodigo = CajaTexto(16, 68, 120)
        txtCodigo.ReadOnly = True
        txtCodigo.BackColor = Fondo

        panelForm.Controls.Add(EtiquetaCampo("PARQUEADERO", 16, 106))
        cboParqueadero = New ComboBox With {.Location = New Point(16, 124), .Width = 320,
            .Font = New Font("Segoe UI", 11.0F), .DropDownStyle = ComboBoxStyle.DropDownList}

        panelForm.Controls.Add(EtiquetaCampo("NOMBRE", 16, 162))
        txtNombre = CajaTexto(16, 180, 320)
        panelForm.Controls.Add(EtiquetaCampo("CÉDULA", 16, 218))
        txtCedula = CajaTexto(16, 236, 320)
        panelForm.Controls.Add(EtiquetaCampo("CELULAR", 16, 274))
        txtCelular = CajaTexto(16, 292, 320)

        panelForm.Controls.Add(EtiquetaCampo("TIPO DE VEHÍCULO", 16, 330))
        cboTipo = New ComboBox With {.Location = New Point(16, 348), .Width = 320,
            .Font = New Font("Segoe UI", 11.0F), .DropDownStyle = ComboBoxStyle.DropDownList}
        cboTipo.Items.AddRange({"carro", "moto", "camioneta", "bus"})

        btnNuevo = New Button With {.Text = "➕ Nuevo", .Size = New Size(100, 38), .Location = New Point(16, 402)}
        EstilizarBoton(btnNuevo, Gris)
        AddHandler btnNuevo.Click, Sub() LimpiarFormulario()
        btnGuardar = New Button With {.Text = "💾 Guardar", .Size = New Size(102, 38), .Location = New Point(124, 402)}
        EstilizarBoton(btnGuardar, VerdeBoton)
        AddHandler btnGuardar.Click, AddressOf btnGuardar_Click
        btnEliminar = New Button With {.Text = "🗑 Eliminar", .Size = New Size(102, 38), .Location = New Point(234, 402)}
        EstilizarBoton(btnEliminar, Peligro)
        AddHandler btnEliminar.Click, AddressOf btnEliminar_Click

        lblSoloAdmin = New Label With {
            .Text = "Solo un Administrador puede eliminar registros.", .AutoSize = False,
            .Size = New Size(320, 30), .Location = New Point(16, 450),
            .ForeColor = TextoSuave, .Font = New Font("Segoe UI", 8.5F), .Visible = False
        }

        panelForm.Controls.AddRange({lblTitulo, txtCodigo, cboParqueadero, txtNombre, txtCedula,
                                     txtCelular, cboTipo, btnNuevo, btnGuardar, btnEliminar, lblSoloAdmin})
        Me.Controls.AddRange({panelLista, panelForm})
    End Sub

    Private Sub FormClientes_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not Sesion.EsAdministrador Then
            btnEliminar.Enabled = False
            lblSoloAdmin.Visible = True
        End If
        Try
            cboParqueadero.DataSource = ParqueaderoService.ParaCombo()
            cboParqueadero.DisplayMember = "etiqueta"
            cboParqueadero.ValueMember = "codigo_parqueadero"
        Catch ex As Exception
            DialogoParko.Show("Error al cargar parqueaderos: " & ex.Message, "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        CargarLista()
        LimpiarFormulario()
    End Sub

    Private Sub CargarLista(Optional filtro As String = "")
        Try
            cargandoLista = True
            dgv.DataSource = ClienteService.Listar(filtro)
            ConfigurarColumnas()
            dgv.ClearSelection()
        Catch ex As Exception
            DialogoParko.Show("Error al cargar clientes: " & ex.Message, "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            cargandoLista = False
        End Try
    End Sub

    Private Sub ConfigurarColumnas()
        If dgv.Columns.Count = 0 Then Return
        dgv.Columns("codigo_cliente").HeaderText = "Código"
        dgv.Columns("codigo_cliente").FillWeight = 55
        dgv.Columns("nombre").HeaderText = "Nombre"
        dgv.Columns("nombre").FillWeight = 140
        dgv.Columns("cedula").HeaderText = "Cédula"
        dgv.Columns("celular").HeaderText = "Celular"
        dgv.Columns("tipo_vehiculo").HeaderText = "Vehículo"
        dgv.Columns("tipo_vehiculo").FillWeight = 70
        dgv.Columns("codigo_parqueadero").HeaderText = "Parq."
        dgv.Columns("codigo_parqueadero").FillWeight = 45
    End Sub

    Private Sub dgv_SelectionChanged(sender As Object, e As EventArgs)
        If cargandoLista OrElse dgv.SelectedRows.Count = 0 Then Return
        Dim fila = TryCast(dgv.SelectedRows(0).DataBoundItem, DataRowView)
        If fila Is Nothing Then Return

        editando = True
        txtCodigo.Text = fila("codigo_cliente").ToString()
        cboParqueadero.SelectedValue = fila("codigo_parqueadero").ToString()
        txtNombre.Text = fila("nombre").ToString()
        txtCedula.Text = fila("cedula").ToString()
        txtCelular.Text = fila("celular").ToString()
        cboTipo.SelectedItem = fila("tipo_vehiculo").ToString()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtCodigo.Text) OrElse String.IsNullOrWhiteSpace(txtNombre.Text) OrElse
           cboParqueadero.SelectedValue Is Nothing OrElse cboTipo.SelectedItem Is Nothing Then
            DialogoParko.Show("Completa al menos: nombre, parqueadero y tipo de vehículo.",
                              "Campos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            If editando Then
                ClienteService.Actualizar(txtCodigo.Text, cboParqueadero.SelectedValue.ToString(),
                                          txtNombre.Text, txtCelular.Text, txtCedula.Text,
                                          cboTipo.SelectedItem.ToString())
                DialogoParko.Show("Cliente actualizado correctamente.", "Éxito")
            Else
                ClienteService.Insertar(txtCodigo.Text, cboParqueadero.SelectedValue.ToString(),
                                        txtNombre.Text, txtCelular.Text, txtCedula.Text,
                                        cboTipo.SelectedItem.ToString())
                DialogoParko.Show("Cliente registrado correctamente.", "Éxito")
            End If
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show("Error al guardar: " & ex.Message, "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As EventArgs)
        If Not editando Then
            DialogoParko.Show("Selecciona primero un cliente de la lista.", "Sin selección",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If DialogoParko.Show($"¿Eliminar al cliente '{txtNombre.Text}'?", "Confirmar",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        Try
            ClienteService.Eliminar(txtCodigo.Text)
            DialogoParko.Show("Cliente eliminado.", "Éxito")
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show("No se pudo eliminar. Es posible que el cliente tenga vehículos registrados.",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editando = False
        Try
            txtCodigo.Text = ClienteService.SiguienteCodigo()
        Catch
            txtCodigo.Text = ""
        End Try
        txtNombre.Clear()
        txtCedula.Clear()
        txtCelular.Clear()
        cboParqueadero.SelectedIndex = -1
        cboTipo.SelectedIndex = -1
        dgv.ClearSelection()
        txtNombre.Focus()
    End Sub
End Class
