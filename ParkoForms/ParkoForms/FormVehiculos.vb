Imports System.Data

Public Class FormVehiculos
    Inherits Form

    Private dgv As DataGridView
    Private txtBuscar, txtCodigo, txtPlaca, txtMarca, txtModelo As TextBox
    Private cboParqueadero, cboCliente, cboEstado As ComboBox
    Private dtpFecha As DateTimePicker
    Private btnNuevo, btnGuardar, btnEliminar As Button
    Private editando As Boolean = False
    Private cargandoLista As Boolean = False

    Public Sub New()
        Me.Text = "Vehículos — PARKO"
        Me.ClientSize = New Size(1080, 660)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Fondo
        Me.Font = New Font("Segoe UI", 9.0F)

        ' ===== Lista con búsqueda =====
        Dim panelLista As New Panel With {.Location = New Point(12, 12), .Size = New Size(690, 636), .BackColor = Color.White}
        txtBuscar = CajaTexto(16, 14, 590)
        AddHandler txtBuscar.TextChanged, Sub() CargarLista(txtBuscar.Text)
        Dim btnLimpiar As New Button With {.Text = "✕", .Size = New Size(60, 31), .Location = New Point(612, 13)}
        EstilizarBoton(btnLimpiar, Gris)
        AddHandler btnLimpiar.Click, Sub() txtBuscar.Clear()

        dgv = New DataGridView With {.Location = New Point(16, 58), .Size = New Size(658, 562)}
        EstilizarGrid(dgv)
        AddHandler dgv.SelectionChanged, AddressOf dgv_SelectionChanged
        panelLista.Controls.AddRange({txtBuscar, btnLimpiar, dgv})

        ' ===== Formulario =====
        Dim panelForm As New Panel With {.Location = New Point(714, 12), .Size = New Size(354, 636), .BackColor = Color.White}
        Dim lblTitulo As New Label With {
            .Text = "Datos del vehículo", .AutoSize = True, .Location = New Point(16, 12),
            .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold), .ForeColor = TextoOscuro
        }

        panelForm.Controls.Add(EtiquetaCampo("CÓDIGO", 16, 48))
        txtCodigo = CajaTexto(16, 66, 120)
        txtCodigo.MaxLength = 5

        panelForm.Controls.Add(EtiquetaCampo("PLACA", 16, 104))
        txtPlaca = CajaTexto(16, 122, 320)
        txtPlaca.MaxLength = 10
        txtPlaca.CharacterCasing = CharacterCasing.Upper

        panelForm.Controls.Add(EtiquetaCampo("PARQUEADERO", 16, 160))
        cboParqueadero = New ComboBox With {.Location = New Point(16, 178), .Width = 320,
            .Font = New Font("Segoe UI", 11.0F), .DropDownStyle = ComboBoxStyle.DropDownList}

        panelForm.Controls.Add(EtiquetaCampo("CLIENTE (DUEÑO)", 16, 216))
        cboCliente = New ComboBox With {.Location = New Point(16, 234), .Width = 320,
            .Font = New Font("Segoe UI", 11.0F), .DropDownStyle = ComboBoxStyle.DropDownList}

        panelForm.Controls.Add(EtiquetaCampo("FECHA DE INGRESO", 16, 272))
        dtpFecha = New DateTimePicker With {.Location = New Point(16, 290), .Width = 320,
            .Font = New Font("Segoe UI", 11.0F), .Format = DateTimePickerFormat.Short, .ShowCheckBox = True}

        panelForm.Controls.Add(EtiquetaCampo("MARCA", 16, 330))
        txtMarca = CajaTexto(16, 348, 320)
        txtMarca.MaxLength = 20
        panelForm.Controls.Add(EtiquetaCampo("MODELO", 16, 386))
        txtModelo = CajaTexto(16, 404, 320)
        txtModelo.MaxLength = 20

        panelForm.Controls.Add(EtiquetaCampo("ESTADO", 16, 442))
        cboEstado = New ComboBox With {.Location = New Point(16, 460), .Width = 320,
            .Font = New Font("Segoe UI", 11.0F), .DropDownStyle = ComboBoxStyle.DropDown}
        cboEstado.Items.AddRange({"excelente", "bueno", "rayado", "golpeado"})

        btnNuevo = New Button With {.Text = "➕ Nuevo", .Size = New Size(100, 38), .Location = New Point(16, 514)}
        EstilizarBoton(btnNuevo, Gris)
        AddHandler btnNuevo.Click, Sub() LimpiarFormulario()
        btnGuardar = New Button With {.Text = "💾 Guardar", .Size = New Size(102, 38), .Location = New Point(124, 514)}
        EstilizarBoton(btnGuardar, VerdeBoton)
        AddHandler btnGuardar.Click, AddressOf btnGuardar_Click
        btnEliminar = New Button With {.Text = "🗑 Eliminar", .Size = New Size(102, 38), .Location = New Point(234, 514)}
        EstilizarBoton(btnEliminar, Peligro)
        AddHandler btnEliminar.Click, AddressOf btnEliminar_Click

        panelForm.Controls.AddRange({lblTitulo, txtCodigo, txtPlaca, cboParqueadero, cboCliente,
                                     dtpFecha, txtMarca, txtModelo, cboEstado,
                                     btnNuevo, btnGuardar, btnEliminar})
        Me.Controls.AddRange({panelLista, panelForm})
    End Sub

    Private Sub FormVehiculos_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not Sesion.EsAdministrador Then
            btnEliminar.Enabled = False
            Dim ayuda As New ToolTip()
            ayuda.SetToolTip(btnEliminar, "Solo un Administrador puede eliminar registros.")
        End If
        Try
            cboParqueadero.DataSource = ParqueaderoService.ParaCombo()
            cboParqueadero.DisplayMember = "etiqueta"
            cboParqueadero.ValueMember = "codigo_parqueadero"
            cboCliente.DataSource = ClienteService.ParaCombo()
            cboCliente.DisplayMember = "etiqueta"
            cboCliente.ValueMember = "codigo_cliente"
        Catch ex As Exception
            DialogoParko.Show("Error al cargar los combos: " & ex.Message, "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        CargarLista()
        LimpiarFormulario()
    End Sub

    Private Sub CargarLista(Optional filtro As String = "")
        Try
            cargandoLista = True
            dgv.DataSource = VehiculoService.Listar(filtro)
            If dgv.Columns.Count > 0 Then
                dgv.Columns("codigo_vehiculo").HeaderText = "Código"
                dgv.Columns("codigo_vehiculo").FillWeight = 50
                dgv.Columns("placa").HeaderText = "Placa"
                dgv.Columns("marca").HeaderText = "Marca"
                dgv.Columns("modelo").HeaderText = "Modelo"
                dgv.Columns("estado").HeaderText = "Estado"
                dgv.Columns("cliente").HeaderText = "Cliente"
                dgv.Columns("cliente").FillWeight = 120
                dgv.Columns("fecha_ingreso").HeaderText = "Ingreso"
                dgv.Columns("fecha_ingreso").DefaultCellStyle.Format = "dd/MM/yyyy"
                dgv.Columns("codigo_cliente").Visible = False
                dgv.Columns("codigo_parqueadero").Visible = False
            End If
            dgv.ClearSelection()
        Catch ex As Exception
            DialogoParko.Show("Error al cargar vehículos: " & ex.Message, "Error",
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
        txtCodigo.Text = fila("codigo_vehiculo").ToString()
        txtCodigo.ReadOnly = True
        txtCodigo.BackColor = Fondo
        txtPlaca.Text = fila("placa").ToString()
        cboParqueadero.SelectedValue = fila("codigo_parqueadero").ToString()
        cboCliente.SelectedValue = fila("codigo_cliente").ToString()
        If IsDBNull(fila("fecha_ingreso")) Then
            dtpFecha.Checked = False
        Else
            dtpFecha.Checked = True
            dtpFecha.Value = CDate(fila("fecha_ingreso"))
        End If
        txtMarca.Text = fila("marca").ToString()
        txtModelo.Text = fila("modelo").ToString()
        cboEstado.Text = fila("estado").ToString()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtCodigo.Text) OrElse String.IsNullOrWhiteSpace(txtPlaca.Text) OrElse
           String.IsNullOrWhiteSpace(txtModelo.Text) OrElse String.IsNullOrWhiteSpace(cboEstado.Text) OrElse
           cboParqueadero.SelectedValue Is Nothing OrElse cboCliente.SelectedValue Is Nothing Then
            DialogoParko.Show("Completa: código, placa, modelo, estado, parqueadero y cliente.",
                              "Campos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim fecha As Date? = Nothing
        If dtpFecha.Checked Then fecha = dtpFecha.Value.Date

        Try
            If editando Then
                VehiculoService.Actualizar(txtCodigo.Text, cboParqueadero.SelectedValue.ToString(),
                                           cboCliente.SelectedValue.ToString(), fecha,
                                           txtMarca.Text, txtModelo.Text, cboEstado.Text, txtPlaca.Text)
                DialogoParko.Show("Vehículo actualizado correctamente.", "Éxito")
            Else
                If VehiculoService.Existe(txtCodigo.Text) Then
                    DialogoParko.Show("Ya existe un vehículo con ese código.", "Código duplicado",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                VehiculoService.Insertar(txtCodigo.Text, cboParqueadero.SelectedValue.ToString(),
                                         cboCliente.SelectedValue.ToString(), fecha,
                                         txtMarca.Text, txtModelo.Text, cboEstado.Text, txtPlaca.Text)
                DialogoParko.Show("Vehículo registrado correctamente.", "Éxito")
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
            DialogoParko.Show("Selecciona primero un vehículo de la lista.", "Sin selección",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If DialogoParko.Show($"¿Eliminar el vehículo con placa '{txtPlaca.Text}'?", "Confirmar",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        Try
            VehiculoService.Eliminar(txtCodigo.Text)
            DialogoParko.Show("Vehículo eliminado.", "Éxito")
            CargarLista(txtBuscar.Text)
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show("Error al eliminar: " & ex.Message, "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editando = False
        txtCodigo.Clear()
        txtCodigo.ReadOnly = False
        txtCodigo.BackColor = Color.White
        txtPlaca.Clear()
        cboParqueadero.SelectedIndex = -1
        cboCliente.SelectedIndex = -1
        dtpFecha.Checked = False
        txtMarca.Clear()
        txtModelo.Clear()
        cboEstado.SelectedIndex = -1
        cboEstado.Text = ""
        dgv.ClearSelection()
        txtCodigo.Focus()
    End Sub
End Class
