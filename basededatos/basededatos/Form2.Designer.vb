<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form2
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento
    'Se puede modificar usando el Diseñador de Windows Forms.  
    'No lo modifique con el editor de código.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim Codigo_parqueaderoLabel As System.Windows.Forms.Label
        Dim DireccionLabel As System.Windows.Forms.Label
        Dim TelefonoLabel As System.Windows.Forms.Label
        Dim NitLabel As System.Windows.Forms.Label
        Dim AdministradorLabel As System.Windows.Forms.Label
        Dim OperadorLabel As System.Windows.Forms.Label
        Dim HorarioLabel As System.Windows.Forms.Label
        Me.ParqueaderoDataSet = New basededatos.parqueaderoDataSet()
        Me.ParqueaderoBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.ParqueaderoTableAdapter = New basededatos.parqueaderoDataSetTableAdapters.parqueaderoTableAdapter()
        Me.TableAdapterManager = New basededatos.parqueaderoDataSetTableAdapters.TableAdapterManager()
        Me.Codigo_parqueaderoTextBox = New System.Windows.Forms.TextBox()
        Me.DireccionTextBox = New System.Windows.Forms.TextBox()
        Me.TelefonoTextBox = New System.Windows.Forms.TextBox()
        Me.NitTextBox = New System.Windows.Forms.TextBox()
        Me.AdministradorTextBox = New System.Windows.Forms.TextBox()
        Me.OperadorTextBox = New System.Windows.Forms.TextBox()
        Me.HorarioTextBox = New System.Windows.Forms.TextBox()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.btnPrimero = New System.Windows.Forms.Button()
        Me.btnAnterior = New System.Windows.Forms.Button()
        Me.btnSiguiente = New System.Windows.Forms.Button()
        Me.btnUltimo = New System.Windows.Forms.Button()
        Me.btnNuevo = New System.Windows.Forms.Button()
        Me.btnGuardar = New System.Windows.Forms.Button()
        Me.btnEliminar = New System.Windows.Forms.Button()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.lblRegistro = New System.Windows.Forms.Label()
        Me.btnIrClientes = New System.Windows.Forms.Button()
        Codigo_parqueaderoLabel = New System.Windows.Forms.Label()
        DireccionLabel = New System.Windows.Forms.Label()
        TelefonoLabel = New System.Windows.Forms.Label()
        NitLabel = New System.Windows.Forms.Label()
        AdministradorLabel = New System.Windows.Forms.Label()
        OperadorLabel = New System.Windows.Forms.Label()
        HorarioLabel = New System.Windows.Forms.Label()
        CType(Me.ParqueaderoDataSet, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.ParqueaderoBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Codigo_parqueaderoLabel
        '
        Codigo_parqueaderoLabel.AutoSize = True
        Codigo_parqueaderoLabel.Location = New System.Drawing.Point(62, 91)
        Codigo_parqueaderoLabel.Name = "Codigo_parqueaderoLabel"
        Codigo_parqueaderoLabel.Size = New System.Drawing.Size(171, 23)
        Codigo_parqueaderoLabel.TabIndex = 1
        Codigo_parqueaderoLabel.Text = "Codigo Parqueadero:"
        '
        'DireccionLabel
        '
        DireccionLabel.AutoSize = True
        DireccionLabel.Location = New System.Drawing.Point(62, 131)
        DireccionLabel.Name = "DireccionLabel"
        DireccionLabel.Size = New System.Drawing.Size(85, 23)
        DireccionLabel.TabIndex = 3
        DireccionLabel.Text = "Direccion:"
        '
        'TelefonoLabel
        '
        TelefonoLabel.AutoSize = True
        TelefonoLabel.Location = New System.Drawing.Point(62, 172)
        TelefonoLabel.Name = "TelefonoLabel"
        TelefonoLabel.Size = New System.Drawing.Size(78, 23)
        TelefonoLabel.TabIndex = 5
        TelefonoLabel.Text = "Telefono:"
        '
        'NitLabel
        '
        NitLabel.AutoSize = True
        NitLabel.Location = New System.Drawing.Point(62, 212)
        NitLabel.Name = "NitLabel"
        NitLabel.Size = New System.Drawing.Size(37, 23)
        NitLabel.TabIndex = 7
        NitLabel.Text = "Nit:"
        '
        'AdministradorLabel
        '
        AdministradorLabel.AutoSize = True
        AdministradorLabel.Location = New System.Drawing.Point(62, 252)
        AdministradorLabel.Name = "AdministradorLabel"
        AdministradorLabel.Size = New System.Drawing.Size(122, 23)
        AdministradorLabel.TabIndex = 9
        AdministradorLabel.Text = "Administrador:"
        '
        'OperadorLabel
        '
        OperadorLabel.AutoSize = True
        OperadorLabel.Location = New System.Drawing.Point(62, 292)
        OperadorLabel.Name = "OperadorLabel"
        OperadorLabel.Size = New System.Drawing.Size(87, 23)
        OperadorLabel.TabIndex = 11
        OperadorLabel.Text = "Operador:"
        '
        'HorarioLabel
        '
        HorarioLabel.AutoSize = True
        HorarioLabel.Location = New System.Drawing.Point(62, 333)
        HorarioLabel.Name = "HorarioLabel"
        HorarioLabel.Size = New System.Drawing.Size(71, 23)
        HorarioLabel.TabIndex = 13
        HorarioLabel.Text = "Horario:"
        '
        'ParqueaderoDataSet
        '
        Me.ParqueaderoDataSet.DataSetName = "parqueaderoDataSet"
        Me.ParqueaderoDataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'ParqueaderoBindingSource
        '
        Me.ParqueaderoBindingSource.DataMember = "parqueadero"
        Me.ParqueaderoBindingSource.DataSource = Me.ParqueaderoDataSet
        '
        'ParqueaderoTableAdapter
        '
        Me.ParqueaderoTableAdapter.ClearBeforeFill = True
        '
        'TableAdapterManager
        '
        Me.TableAdapterManager.BackupDataSetBeforeUpdate = False
        Me.TableAdapterManager.clienteTableAdapter = Nothing
        Me.TableAdapterManager.parqueaderoTableAdapter = Me.ParqueaderoTableAdapter
        Me.TableAdapterManager.UpdateOrder = basededatos.parqueaderoDataSetTableAdapters.TableAdapterManager.UpdateOrderOption.InsertUpdateDelete
        Me.TableAdapterManager.vehiculoTableAdapter = Nothing
        '
        'Codigo_parqueaderoTextBox
        '
        Me.Codigo_parqueaderoTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Codigo_parqueaderoTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ParqueaderoBindingSource, "codigo_parqueadero", True))
        Me.Codigo_parqueaderoTextBox.Location = New System.Drawing.Point(237, 91)
        Me.Codigo_parqueaderoTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.Codigo_parqueaderoTextBox.Name = "Codigo_parqueaderoTextBox"
        Me.Codigo_parqueaderoTextBox.Size = New System.Drawing.Size(199, 30)
        Me.Codigo_parqueaderoTextBox.TabIndex = 2
        '
        'DireccionTextBox
        '
        Me.DireccionTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.DireccionTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ParqueaderoBindingSource, "direccion", True))
        Me.DireccionTextBox.Location = New System.Drawing.Point(237, 131)
        Me.DireccionTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.DireccionTextBox.Name = "DireccionTextBox"
        Me.DireccionTextBox.Size = New System.Drawing.Size(199, 30)
        Me.DireccionTextBox.TabIndex = 4
        '
        'TelefonoTextBox
        '
        Me.TelefonoTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.TelefonoTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ParqueaderoBindingSource, "telefono", True))
        Me.TelefonoTextBox.Location = New System.Drawing.Point(237, 171)
        Me.TelefonoTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.TelefonoTextBox.Name = "TelefonoTextBox"
        Me.TelefonoTextBox.Size = New System.Drawing.Size(199, 30)
        Me.TelefonoTextBox.TabIndex = 6
        '
        'NitTextBox
        '
        Me.NitTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.NitTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ParqueaderoBindingSource, "nit", True))
        Me.NitTextBox.Location = New System.Drawing.Point(237, 211)
        Me.NitTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.NitTextBox.Name = "NitTextBox"
        Me.NitTextBox.Size = New System.Drawing.Size(199, 30)
        Me.NitTextBox.TabIndex = 8
        '
        'AdministradorTextBox
        '
        Me.AdministradorTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.AdministradorTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ParqueaderoBindingSource, "administrador", True))
        Me.AdministradorTextBox.Location = New System.Drawing.Point(237, 252)
        Me.AdministradorTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.AdministradorTextBox.Name = "AdministradorTextBox"
        Me.AdministradorTextBox.Size = New System.Drawing.Size(199, 30)
        Me.AdministradorTextBox.TabIndex = 10
        '
        'OperadorTextBox
        '
        Me.OperadorTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.OperadorTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ParqueaderoBindingSource, "operador", True))
        Me.OperadorTextBox.Location = New System.Drawing.Point(237, 292)
        Me.OperadorTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.OperadorTextBox.Name = "OperadorTextBox"
        Me.OperadorTextBox.Size = New System.Drawing.Size(199, 30)
        Me.OperadorTextBox.TabIndex = 12
        '
        'HorarioTextBox
        '
        Me.HorarioTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.HorarioTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ParqueaderoBindingSource, "horario", True))
        Me.HorarioTextBox.Location = New System.Drawing.Point(237, 332)
        Me.HorarioTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.HorarioTextBox.Name = "HorarioTextBox"
        Me.HorarioTextBox.Size = New System.Drawing.Size(199, 30)
        Me.HorarioTextBox.TabIndex = 14
        '
        'PictureBox1
        '
        Me.PictureBox1.Location = New System.Drawing.Point(468, 91)
        Me.PictureBox1.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(254, 273)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.PictureBox1.TabIndex = 15
        Me.PictureBox1.TabStop = False
        '
        'btnPrimero
        '
        Me.btnPrimero.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnPrimero.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnPrimero.FlatAppearance.BorderSize = 0
        Me.btnPrimero.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnPrimero.ForeColor = System.Drawing.Color.White
        Me.btnPrimero.Location = New System.Drawing.Point(121, 434)
        Me.btnPrimero.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnPrimero.Name = "btnPrimero"
        Me.btnPrimero.Size = New System.Drawing.Size(110, 35)
        Me.btnPrimero.TabIndex = 16
        Me.btnPrimero.Text = "⏮ Primero"
        Me.btnPrimero.UseVisualStyleBackColor = False
        '
        'btnAnterior
        '
        Me.btnAnterior.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnAnterior.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnAnterior.FlatAppearance.BorderSize = 0
        Me.btnAnterior.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAnterior.ForeColor = System.Drawing.Color.White
        Me.btnAnterior.Location = New System.Drawing.Point(254, 434)
        Me.btnAnterior.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnAnterior.Name = "btnAnterior"
        Me.btnAnterior.Size = New System.Drawing.Size(110, 35)
        Me.btnAnterior.TabIndex = 17
        Me.btnAnterior.Text = "◀ Anterior"
        Me.btnAnterior.UseVisualStyleBackColor = False
        '
        'btnSiguiente
        '
        Me.btnSiguiente.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnSiguiente.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnSiguiente.FlatAppearance.BorderSize = 0
        Me.btnSiguiente.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSiguiente.ForeColor = System.Drawing.Color.White
        Me.btnSiguiente.Location = New System.Drawing.Point(391, 434)
        Me.btnSiguiente.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnSiguiente.Name = "btnSiguiente"
        Me.btnSiguiente.Size = New System.Drawing.Size(110, 35)
        Me.btnSiguiente.TabIndex = 18
        Me.btnSiguiente.Text = "Siguiente ▶"
        Me.btnSiguiente.UseVisualStyleBackColor = False
        '
        'btnUltimo
        '
        Me.btnUltimo.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnUltimo.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnUltimo.FlatAppearance.BorderSize = 0
        Me.btnUltimo.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnUltimo.ForeColor = System.Drawing.Color.White
        Me.btnUltimo.Location = New System.Drawing.Point(521, 434)
        Me.btnUltimo.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnUltimo.Name = "btnUltimo"
        Me.btnUltimo.Size = New System.Drawing.Size(110, 35)
        Me.btnUltimo.TabIndex = 19
        Me.btnUltimo.Text = "" & Global.Microsoft.VisualBasic.ChrW(9) & "Último ⏭"
        Me.btnUltimo.UseVisualStyleBackColor = False
        '
        'btnNuevo
        '
        Me.btnNuevo.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnNuevo.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnNuevo.FlatAppearance.BorderSize = 0
        Me.btnNuevo.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnNuevo.ForeColor = System.Drawing.Color.White
        Me.btnNuevo.Location = New System.Drawing.Point(195, 503)
        Me.btnNuevo.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnNuevo.Name = "btnNuevo"
        Me.btnNuevo.Size = New System.Drawing.Size(110, 35)
        Me.btnNuevo.TabIndex = 20
        Me.btnNuevo.Text = "➕ Nuevo"
        Me.btnNuevo.UseVisualStyleBackColor = False
        '
        'btnGuardar
        '
        Me.btnGuardar.BackColor = System.Drawing.Color.FromArgb(CType(CType(39, Byte), Integer), CType(CType(174, Byte), Integer), CType(CType(96, Byte), Integer))
        Me.btnGuardar.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnGuardar.FlatAppearance.BorderSize = 0
        Me.btnGuardar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnGuardar.ForeColor = System.Drawing.Color.White
        Me.btnGuardar.Location = New System.Drawing.Point(326, 503)
        Me.btnGuardar.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnGuardar.Name = "btnGuardar"
        Me.btnGuardar.Size = New System.Drawing.Size(110, 35)
        Me.btnGuardar.TabIndex = 21
        Me.btnGuardar.Text = "💾 Guardar"
        Me.btnGuardar.UseVisualStyleBackColor = False
        '
        'btnEliminar
        '
        Me.btnEliminar.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(57, Byte), Integer), CType(CType(43, Byte), Integer))
        Me.btnEliminar.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnEliminar.FlatAppearance.BorderSize = 0
        Me.btnEliminar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnEliminar.ForeColor = System.Drawing.Color.White
        Me.btnEliminar.Location = New System.Drawing.Point(459, 503)
        Me.btnEliminar.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnEliminar.Name = "btnEliminar"
        Me.btnEliminar.Size = New System.Drawing.Size(110, 35)
        Me.btnEliminar.TabIndex = 22
        Me.btnEliminar.Text = "🗑 Eliminar"
        Me.btnEliminar.UseVisualStyleBackColor = False
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.FromArgb(CType(CType(25, Byte), Integer), CType(CType(42, Byte), Integer), CType(CType(86, Byte), Integer))
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Top
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(764, 60)
        Me.Panel1.TabIndex = 23
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Segoe UI", 16.2!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.White
        Me.Label1.Location = New System.Drawing.Point(188, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(422, 38)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "REGISTRO DE PARQUEADEROS"
        '
        'lblRegistro
        '
        Me.lblRegistro.AutoSize = True
        Me.lblRegistro.Location = New System.Drawing.Point(337, 387)
        Me.lblRegistro.Name = "lblRegistro"
        Me.lblRegistro.Size = New System.Drawing.Size(59, 23)
        Me.lblRegistro.TabIndex = 24
        Me.lblRegistro.Text = "Label2"
        '
        'btnIrClientes
        '
        Me.btnIrClientes.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.btnIrClientes.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnIrClientes.FlatAppearance.BorderSize = 0
        Me.btnIrClientes.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnIrClientes.Location = New System.Drawing.Point(591, 588)
        Me.btnIrClientes.Name = "btnIrClientes"
        Me.btnIrClientes.Size = New System.Drawing.Size(152, 31)
        Me.btnIrClientes.TabIndex = 34
        Me.btnIrClientes.Text = "◀ Menú Principal"
        Me.btnIrClientes.UseVisualStyleBackColor = False
        '
        'Form2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 23.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.WhiteSmoke
        Me.ClientSize = New System.Drawing.Size(764, 631)
        Me.Controls.Add(Me.btnIrClientes)
        Me.Controls.Add(Me.lblRegistro)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.btnEliminar)
        Me.Controls.Add(Me.btnGuardar)
        Me.Controls.Add(Me.btnNuevo)
        Me.Controls.Add(Me.btnUltimo)
        Me.Controls.Add(Me.btnSiguiente)
        Me.Controls.Add(Me.btnAnterior)
        Me.Controls.Add(Me.btnPrimero)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Codigo_parqueaderoLabel)
        Me.Controls.Add(Me.Codigo_parqueaderoTextBox)
        Me.Controls.Add(DireccionLabel)
        Me.Controls.Add(Me.DireccionTextBox)
        Me.Controls.Add(TelefonoLabel)
        Me.Controls.Add(Me.TelefonoTextBox)
        Me.Controls.Add(NitLabel)
        Me.Controls.Add(Me.NitTextBox)
        Me.Controls.Add(AdministradorLabel)
        Me.Controls.Add(Me.AdministradorTextBox)
        Me.Controls.Add(OperadorLabel)
        Me.Controls.Add(Me.OperadorTextBox)
        Me.Controls.Add(HorarioLabel)
        Me.Controls.Add(Me.HorarioTextBox)
        Me.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.MaximizeBox = False
        Me.Name = "Form2"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Gestión de Parqueadero"
        CType(Me.ParqueaderoDataSet, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.ParqueaderoBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents ParqueaderoDataSet As parqueaderoDataSet
    Friend WithEvents ParqueaderoBindingSource As BindingSource
    Friend WithEvents ParqueaderoTableAdapter As parqueaderoDataSetTableAdapters.parqueaderoTableAdapter
    Friend WithEvents TableAdapterManager As parqueaderoDataSetTableAdapters.TableAdapterManager
    Friend WithEvents Codigo_parqueaderoTextBox As TextBox
    Friend WithEvents DireccionTextBox As TextBox
    Friend WithEvents TelefonoTextBox As TextBox
    Friend WithEvents NitTextBox As TextBox
    Friend WithEvents AdministradorTextBox As TextBox
    Friend WithEvents OperadorTextBox As TextBox
    Friend WithEvents HorarioTextBox As TextBox
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents btnPrimero As Button
    Friend WithEvents btnAnterior As Button
    Friend WithEvents btnSiguiente As Button
    Friend WithEvents btnUltimo As Button
    Friend WithEvents btnNuevo As Button
    Friend WithEvents btnGuardar As Button
    Friend WithEvents btnEliminar As Button
    Friend WithEvents Panel1 As Panel
    Friend WithEvents Label1 As Label
    Friend WithEvents lblRegistro As Label
    Friend WithEvents btnIrClientes As Button
End Class
