<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim Codigo_clienteLabel As System.Windows.Forms.Label
        Dim Codigo_parqueaderoLabel As System.Windows.Forms.Label
        Dim NombreLabel As System.Windows.Forms.Label
        Dim CelularLabel As System.Windows.Forms.Label
        Dim CedulaLabel As System.Windows.Forms.Label
        Dim Tipo_vehiculoLabel As System.Windows.Forms.Label
        Me.ParqueaderoDataSet = New basededatos.parqueaderoDataSet()
        Me.ClienteBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.ClienteTableAdapter = New basededatos.parqueaderoDataSetTableAdapters.clienteTableAdapter()
        Me.TableAdapterManager = New basededatos.parqueaderoDataSetTableAdapters.TableAdapterManager()
        Me.Codigo_clienteTextBox = New System.Windows.Forms.TextBox()
        Me.Codigo_parqueaderoTextBox = New System.Windows.Forms.TextBox()
        Me.NombreTextBox = New System.Windows.Forms.TextBox()
        Me.CelularTextBox = New System.Windows.Forms.TextBox()
        Me.CedulaTextBox = New System.Windows.Forms.TextBox()
        Me.Tipo_vehiculoTextBox = New System.Windows.Forms.TextBox()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.lblRegistro = New System.Windows.Forms.Label()
        Me.btnEliminar = New System.Windows.Forms.Button()
        Me.btnGuardar = New System.Windows.Forms.Button()
        Me.btnNuevo = New System.Windows.Forms.Button()
        Me.btnUltimo = New System.Windows.Forms.Button()
        Me.btnSiguiente = New System.Windows.Forms.Button()
        Me.btnAnterior = New System.Windows.Forms.Button()
        Me.btnPrimero = New System.Windows.Forms.Button()
        Me.btnIrParqueadero = New System.Windows.Forms.Button()
        Codigo_clienteLabel = New System.Windows.Forms.Label()
        Codigo_parqueaderoLabel = New System.Windows.Forms.Label()
        NombreLabel = New System.Windows.Forms.Label()
        CelularLabel = New System.Windows.Forms.Label()
        CedulaLabel = New System.Windows.Forms.Label()
        Tipo_vehiculoLabel = New System.Windows.Forms.Label()
        CType(Me.ParqueaderoDataSet, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.ClienteBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Codigo_clienteLabel
        '
        Codigo_clienteLabel.AutoSize = True
        Codigo_clienteLabel.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Codigo_clienteLabel.Location = New System.Drawing.Point(53, 109)
        Codigo_clienteLabel.Name = "Codigo_clienteLabel"
        Codigo_clienteLabel.Size = New System.Drawing.Size(124, 23)
        Codigo_clienteLabel.TabIndex = 1
        Codigo_clienteLabel.Text = "Codigo cliente:"
        '
        'Codigo_parqueaderoLabel
        '
        Codigo_parqueaderoLabel.AutoSize = True
        Codigo_parqueaderoLabel.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Codigo_parqueaderoLabel.Location = New System.Drawing.Point(53, 147)
        Codigo_parqueaderoLabel.Name = "Codigo_parqueaderoLabel"
        Codigo_parqueaderoLabel.Size = New System.Drawing.Size(172, 23)
        Codigo_parqueaderoLabel.TabIndex = 3
        Codigo_parqueaderoLabel.Text = "Codigo parqueadero:"
        '
        'NombreLabel
        '
        NombreLabel.AutoSize = True
        NombreLabel.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        NombreLabel.Location = New System.Drawing.Point(53, 185)
        NombreLabel.Name = "NombreLabel"
        NombreLabel.Size = New System.Drawing.Size(77, 23)
        NombreLabel.TabIndex = 5
        NombreLabel.Text = "Nombre:"
        '
        'CelularLabel
        '
        CelularLabel.AutoSize = True
        CelularLabel.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        CelularLabel.Location = New System.Drawing.Point(53, 223)
        CelularLabel.Name = "CelularLabel"
        CelularLabel.Size = New System.Drawing.Size(67, 23)
        CelularLabel.TabIndex = 7
        CelularLabel.Text = "Celular:"
        '
        'CedulaLabel
        '
        CedulaLabel.AutoSize = True
        CedulaLabel.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        CedulaLabel.Location = New System.Drawing.Point(53, 261)
        CedulaLabel.Name = "CedulaLabel"
        CedulaLabel.Size = New System.Drawing.Size(67, 23)
        CedulaLabel.TabIndex = 9
        CedulaLabel.Text = "Cedula:"
        '
        'Tipo_vehiculoLabel
        '
        Tipo_vehiculoLabel.AutoSize = True
        Tipo_vehiculoLabel.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Tipo_vehiculoLabel.Location = New System.Drawing.Point(53, 299)
        Tipo_vehiculoLabel.Name = "Tipo_vehiculoLabel"
        Tipo_vehiculoLabel.Size = New System.Drawing.Size(115, 23)
        Tipo_vehiculoLabel.TabIndex = 11
        Tipo_vehiculoLabel.Text = "Tipo vehiculo:"
        '
        'ParqueaderoDataSet
        '
        Me.ParqueaderoDataSet.DataSetName = "parqueaderoDataSet"
        Me.ParqueaderoDataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'ClienteBindingSource
        '
        Me.ClienteBindingSource.DataMember = "cliente"
        Me.ClienteBindingSource.DataSource = Me.ParqueaderoDataSet
        '
        'ClienteTableAdapter
        '
        Me.ClienteTableAdapter.ClearBeforeFill = True
        '
        'TableAdapterManager
        '
        Me.TableAdapterManager.BackupDataSetBeforeUpdate = False
        Me.TableAdapterManager.clienteTableAdapter = Me.ClienteTableAdapter
        Me.TableAdapterManager.parqueaderoTableAdapter = Nothing
        Me.TableAdapterManager.UpdateOrder = basededatos.parqueaderoDataSetTableAdapters.TableAdapterManager.UpdateOrderOption.InsertUpdateDelete
        Me.TableAdapterManager.vehiculoTableAdapter = Nothing
        '
        'Codigo_clienteTextBox
        '
        Me.Codigo_clienteTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Codigo_clienteTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ClienteBindingSource, "codigo_cliente", True))
        Me.Codigo_clienteTextBox.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Codigo_clienteTextBox.Location = New System.Drawing.Point(277, 109)
        Me.Codigo_clienteTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.Codigo_clienteTextBox.Name = "Codigo_clienteTextBox"
        Me.Codigo_clienteTextBox.Size = New System.Drawing.Size(335, 30)
        Me.Codigo_clienteTextBox.TabIndex = 2
        '
        'Codigo_parqueaderoTextBox
        '
        Me.Codigo_parqueaderoTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Codigo_parqueaderoTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ClienteBindingSource, "codigo_parqueadero", True))
        Me.Codigo_parqueaderoTextBox.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Codigo_parqueaderoTextBox.Location = New System.Drawing.Point(277, 147)
        Me.Codigo_parqueaderoTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.Codigo_parqueaderoTextBox.Name = "Codigo_parqueaderoTextBox"
        Me.Codigo_parqueaderoTextBox.Size = New System.Drawing.Size(335, 30)
        Me.Codigo_parqueaderoTextBox.TabIndex = 4
        '
        'NombreTextBox
        '
        Me.NombreTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.NombreTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ClienteBindingSource, "nombre", True))
        Me.NombreTextBox.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.NombreTextBox.Location = New System.Drawing.Point(277, 185)
        Me.NombreTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.NombreTextBox.Name = "NombreTextBox"
        Me.NombreTextBox.Size = New System.Drawing.Size(335, 30)
        Me.NombreTextBox.TabIndex = 6
        '
        'CelularTextBox
        '
        Me.CelularTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.CelularTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ClienteBindingSource, "celular", True))
        Me.CelularTextBox.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CelularTextBox.Location = New System.Drawing.Point(277, 223)
        Me.CelularTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.CelularTextBox.Name = "CelularTextBox"
        Me.CelularTextBox.Size = New System.Drawing.Size(335, 30)
        Me.CelularTextBox.TabIndex = 8
        '
        'CedulaTextBox
        '
        Me.CedulaTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.CedulaTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ClienteBindingSource, "cedula", True))
        Me.CedulaTextBox.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CedulaTextBox.Location = New System.Drawing.Point(277, 261)
        Me.CedulaTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.CedulaTextBox.Name = "CedulaTextBox"
        Me.CedulaTextBox.Size = New System.Drawing.Size(335, 30)
        Me.CedulaTextBox.TabIndex = 10
        '
        'Tipo_vehiculoTextBox
        '
        Me.Tipo_vehiculoTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Tipo_vehiculoTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Me.ClienteBindingSource, "tipo_vehiculo", True))
        Me.Tipo_vehiculoTextBox.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Tipo_vehiculoTextBox.Location = New System.Drawing.Point(277, 299)
        Me.Tipo_vehiculoTextBox.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.Tipo_vehiculoTextBox.Name = "Tipo_vehiculoTextBox"
        Me.Tipo_vehiculoTextBox.Size = New System.Drawing.Size(335, 30)
        Me.Tipo_vehiculoTextBox.TabIndex = 12
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.FromArgb(CType(CType(25, Byte), Integer), CType(CType(42, Byte), Integer), CType(CType(86, Byte), Integer))
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Top
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(681, 68)
        Me.Panel1.TabIndex = 24
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Segoe UI", 16.2!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.White
        Me.Label1.Location = New System.Drawing.Point(174, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(326, 38)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "REGISTRO DE CLIENTES"
        '
        'lblRegistro
        '
        Me.lblRegistro.AutoSize = True
        Me.lblRegistro.Location = New System.Drawing.Point(297, 370)
        Me.lblRegistro.Name = "lblRegistro"
        Me.lblRegistro.Size = New System.Drawing.Size(59, 23)
        Me.lblRegistro.TabIndex = 32
        Me.lblRegistro.Text = "Label2"
        '
        'btnEliminar
        '
        Me.btnEliminar.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(57, Byte), Integer), CType(CType(43, Byte), Integer))
        Me.btnEliminar.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnEliminar.FlatAppearance.BorderSize = 0
        Me.btnEliminar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnEliminar.ForeColor = System.Drawing.Color.White
        Me.btnEliminar.Location = New System.Drawing.Point(419, 486)
        Me.btnEliminar.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnEliminar.Name = "btnEliminar"
        Me.btnEliminar.Size = New System.Drawing.Size(110, 35)
        Me.btnEliminar.TabIndex = 31
        Me.btnEliminar.Text = "🗑 Eliminar"
        Me.btnEliminar.UseVisualStyleBackColor = False
        '
        'btnGuardar
        '
        Me.btnGuardar.BackColor = System.Drawing.Color.FromArgb(CType(CType(39, Byte), Integer), CType(CType(174, Byte), Integer), CType(CType(96, Byte), Integer))
        Me.btnGuardar.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnGuardar.FlatAppearance.BorderSize = 0
        Me.btnGuardar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnGuardar.ForeColor = System.Drawing.Color.White
        Me.btnGuardar.Location = New System.Drawing.Point(286, 486)
        Me.btnGuardar.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnGuardar.Name = "btnGuardar"
        Me.btnGuardar.Size = New System.Drawing.Size(110, 35)
        Me.btnGuardar.TabIndex = 30
        Me.btnGuardar.Text = "💾 Guardar"
        Me.btnGuardar.UseVisualStyleBackColor = False
        '
        'btnNuevo
        '
        Me.btnNuevo.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnNuevo.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnNuevo.FlatAppearance.BorderSize = 0
        Me.btnNuevo.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnNuevo.ForeColor = System.Drawing.Color.White
        Me.btnNuevo.Location = New System.Drawing.Point(155, 486)
        Me.btnNuevo.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnNuevo.Name = "btnNuevo"
        Me.btnNuevo.Size = New System.Drawing.Size(110, 35)
        Me.btnNuevo.TabIndex = 29
        Me.btnNuevo.Text = "➕ Nuevo"
        Me.btnNuevo.UseVisualStyleBackColor = False
        '
        'btnUltimo
        '
        Me.btnUltimo.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnUltimo.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnUltimo.FlatAppearance.BorderSize = 0
        Me.btnUltimo.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnUltimo.ForeColor = System.Drawing.Color.White
        Me.btnUltimo.Location = New System.Drawing.Point(481, 417)
        Me.btnUltimo.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnUltimo.Name = "btnUltimo"
        Me.btnUltimo.Size = New System.Drawing.Size(110, 35)
        Me.btnUltimo.TabIndex = 28
        Me.btnUltimo.Text = "" & Global.Microsoft.VisualBasic.ChrW(9) & "Último ⏭"
        Me.btnUltimo.UseVisualStyleBackColor = False
        '
        'btnSiguiente
        '
        Me.btnSiguiente.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnSiguiente.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnSiguiente.FlatAppearance.BorderSize = 0
        Me.btnSiguiente.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSiguiente.ForeColor = System.Drawing.Color.White
        Me.btnSiguiente.Location = New System.Drawing.Point(351, 417)
        Me.btnSiguiente.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnSiguiente.Name = "btnSiguiente"
        Me.btnSiguiente.Size = New System.Drawing.Size(110, 35)
        Me.btnSiguiente.TabIndex = 27
        Me.btnSiguiente.Text = "Siguiente ▶"
        Me.btnSiguiente.UseVisualStyleBackColor = False
        '
        'btnAnterior
        '
        Me.btnAnterior.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnAnterior.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnAnterior.FlatAppearance.BorderSize = 0
        Me.btnAnterior.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAnterior.ForeColor = System.Drawing.Color.White
        Me.btnAnterior.Location = New System.Drawing.Point(214, 417)
        Me.btnAnterior.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnAnterior.Name = "btnAnterior"
        Me.btnAnterior.Size = New System.Drawing.Size(110, 35)
        Me.btnAnterior.TabIndex = 26
        Me.btnAnterior.Text = "◀ Anterior"
        Me.btnAnterior.UseVisualStyleBackColor = False
        '
        'btnPrimero
        '
        Me.btnPrimero.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(73, Byte), Integer), CType(CType(94, Byte), Integer))
        Me.btnPrimero.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnPrimero.FlatAppearance.BorderSize = 0
        Me.btnPrimero.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnPrimero.ForeColor = System.Drawing.Color.White
        Me.btnPrimero.Location = New System.Drawing.Point(81, 417)
        Me.btnPrimero.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.btnPrimero.Name = "btnPrimero"
        Me.btnPrimero.Size = New System.Drawing.Size(110, 35)
        Me.btnPrimero.TabIndex = 25
        Me.btnPrimero.Text = "⏮ Primero"
        Me.btnPrimero.UseVisualStyleBackColor = False
        '
        'btnIrParqueadero
        '
        Me.btnIrParqueadero.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.btnIrParqueadero.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnIrParqueadero.FlatAppearance.BorderSize = 0
        Me.btnIrParqueadero.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnIrParqueadero.Location = New System.Drawing.Point(481, 573)
        Me.btnIrParqueadero.Name = "btnIrParqueadero"
        Me.btnIrParqueadero.Size = New System.Drawing.Size(182, 31)
        Me.btnIrParqueadero.TabIndex = 33
        Me.btnIrParqueadero.Text = "◀ Menú Principal"
        Me.btnIrParqueadero.UseVisualStyleBackColor = False
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 23.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(681, 616)
        Me.Controls.Add(Me.btnIrParqueadero)
        Me.Controls.Add(Me.lblRegistro)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.btnEliminar)
        Me.Controls.Add(Codigo_clienteLabel)
        Me.Controls.Add(Me.btnGuardar)
        Me.Controls.Add(Me.Codigo_clienteTextBox)
        Me.Controls.Add(Me.btnNuevo)
        Me.Controls.Add(Codigo_parqueaderoLabel)
        Me.Controls.Add(Me.btnUltimo)
        Me.Controls.Add(Me.btnSiguiente)
        Me.Controls.Add(Me.Codigo_parqueaderoTextBox)
        Me.Controls.Add(Me.btnAnterior)
        Me.Controls.Add(NombreLabel)
        Me.Controls.Add(Me.btnPrimero)
        Me.Controls.Add(Me.NombreTextBox)
        Me.Controls.Add(CelularLabel)
        Me.Controls.Add(Me.CelularTextBox)
        Me.Controls.Add(CedulaLabel)
        Me.Controls.Add(Me.CedulaTextBox)
        Me.Controls.Add(Tipo_vehiculoLabel)
        Me.Controls.Add(Me.Tipo_vehiculoTextBox)
        Me.Font = New System.Drawing.Font("Segoe UI", 10.2!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.Name = "Form1"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Gestión de Clientes — Parqueadero"
        CType(Me.ParqueaderoDataSet, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.ClienteBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents ParqueaderoDataSet As parqueaderoDataSet
    Friend WithEvents ClienteBindingSource As BindingSource
    Friend WithEvents ClienteTableAdapter As parqueaderoDataSetTableAdapters.clienteTableAdapter
    Friend WithEvents TableAdapterManager As parqueaderoDataSetTableAdapters.TableAdapterManager
    Friend WithEvents Codigo_clienteTextBox As TextBox
    Friend WithEvents Codigo_parqueaderoTextBox As TextBox
    Friend WithEvents NombreTextBox As TextBox
    Friend WithEvents CelularTextBox As TextBox
    Friend WithEvents CedulaTextBox As TextBox
    Friend WithEvents Tipo_vehiculoTextBox As TextBox
    Friend WithEvents Panel1 As Panel
    Friend WithEvents Label1 As Label
    Friend WithEvents lblRegistro As Label
    Friend WithEvents btnEliminar As Button
    Friend WithEvents btnGuardar As Button
    Friend WithEvents btnNuevo As Button
    Friend WithEvents btnUltimo As Button
    Friend WithEvents btnSiguiente As Button
    Friend WithEvents btnAnterior As Button
    Friend WithEvents btnPrimero As Button
    Friend WithEvents btnIrParqueadero As Button
End Class
