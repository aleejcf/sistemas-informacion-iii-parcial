Imports System.Data

''' <summary>Los socios de la biblioteca. Además del alta y la edición, muestra el
''' estado de cuenta del socio seleccionado —qué tiene afuera y qué debe—, que es
''' lo que hay que mirar antes de prestarle otro libro.</summary>
Public Class SociosPage

    Private preparado As Boolean = False
    Private ocupado As Boolean = False
    ''' <summary>Código del socio en edición. Vacío = se está creando uno.</summary>
    Private editandoId As String = ""

    ' ======================= CICLO DE VIDA =======================

    ''' <summary>El buscador global de la barra superior entra aquí con el código
    ''' del socio ya escrito, así que la búsqueda se aplica antes de consultar.</summary>
    Public Sub Cargar(Optional filtro As String = Nothing)
        Preparar()
        CargarCombos()
        LimpiarFormulario()

        If filtro IsNot Nothing Then
            ocupado = True
            txtBuscar.Text = filtro
            cboTipo.SelectedIndex = -1
            chkConDeuda.IsChecked = False
            ocupado = False
        End If

        CargarLista()
    End Sub

    Private Sub SociosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Preparar()
    End Sub

    Private Sub Preparar()
        If preparado Then Return
        preparado = True

        btnEliminar.IsEnabled = Permisos.PuedeEliminar
        lblSinPermiso.Visibility = If(Permisos.PuedeEliminar, Visibility.Collapsed, Visibility.Visible)
    End Sub

    Private Sub CargarCombos()
        Try
            ocupado = True
            Dim tipo = cboTipoSocio.SelectedValue
            Dim filtro = cboTipo.SelectedValue

            cboTipoSocio.ItemsSource = CatalogoService.TiposSocioParaCombo().DefaultView
            cboTipo.ItemsSource = CatalogoService.TiposSocioParaCombo().DefaultView

            cboTipoSocio.SelectedValue = tipo
            cboTipo.SelectedValue = filtro

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cargar los tipos de socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        Finally
            ocupado = False
        End Try
    End Sub

    ' ======================= LISTA =======================

    Private Sub CargarLista()
        Try
            Dim idTipo As Integer? = Nothing
            If cboTipo.SelectedValue IsNot Nothing Then idTipo = CInt(cboTipo.SelectedValue)

            Dim dt = SocioService.Listar(txtBuscar.Text, idTipo,
                                         soloActivos:=False,
                                         soloConDeuda:=chkConDeuda.IsChecked = True)
            dgSocios.ItemsSource = dt.DefaultView
            pnlVacio.Visibility = If(dt.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            ActualizarIndicadores(dt)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Consultar los socios", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ActualizarIndicadores(dt As DataTable)
        Dim alDia = 0, bloqueados = 0
        Dim deuda As Decimal = 0

        For Each fila As DataRow In dt.Rows
            If Db.Texto(fila, "situacion") = "Al día" Then alDia += 1 Else bloqueados += 1
            deuda += Db.Monto(fila, "monto_adeudado")
        Next

        lblIndTotal.Text = dt.Rows.Count.ToString()
        lblIndAlDia.Text = alDia.ToString()
        lblIndBloqueados.Text = bloqueados.ToString()
        lblIndDeuda.Text = Formato.Dinero(deuda)
    End Sub

    Private Sub txtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtBuscar.TextChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub cboTipo_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboTipo.SelectionChanged
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub chkConDeuda_Changed(sender As Object, e As RoutedEventArgs) _
        Handles chkConDeuda.Checked, chkConDeuda.Unchecked
        If ocupado Then Return
        CargarLista()
    End Sub

    Private Sub btnLimpiar_Click(sender As Object, e As RoutedEventArgs) Handles btnLimpiar.Click
        ocupado = True
        txtBuscar.Clear()
        cboTipo.SelectedIndex = -1
        chkConDeuda.IsChecked = False
        ocupado = False
        CargarLista()
    End Sub

    ' ======================= FORMULARIO =======================

    Private Sub dgSocios_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgSocios.SelectionChanged
        Dim vista = TryCast(dgSocios.SelectedItem, DataRowView)
        If vista Is Nothing Then Return

        Dim fila = vista.Row
        editandoId = Db.Texto(fila, "idsocio")

        txtCodigo.Text = editandoId
        txtCodigo.IsEnabled = False        ' el código es la llave: no se cambia
        txtNombre.Text = Db.Texto(fila, "nombre")
        txtApellido.Text = Db.Texto(fila, "apellido")
        txtIdentidad.Text = Db.Texto(fila, "identidad")
        txtEmail.Text = Db.Texto(fila, "email")
        txtTelefono.Text = Db.Texto(fila, "telefono")
        txtDireccion.Text = Db.Texto(fila, "direccion")
        cboTipoSocio.SelectedValue = Db.Numero(fila, "idtipo")
        chkActivo.IsChecked = Not IsDBNull(fila("esta_activo")) AndAlso CBool(fila("esta_activo"))

        lblTituloFormulario.Text = "Editar socio"
        lblSubtituloFormulario.Text = $"{editandoId} · {Db.Texto(fila, "nombre_completo")}"

        MostrarEstadoDeCuenta(editandoId)
    End Sub

    Private Sub cboTipoSocio_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles cboTipoSocio.SelectionChanged
        MostrarReglasDelTipo()
    End Sub

    ''' <summary>Explica en palabras qué implica el tipo elegido. Un combo que dice
    ''' "Estudiante" no dice nada; "3 libros, 7 días, L 5.00 de multa diaria" sí.</summary>
    Private Sub MostrarReglasDelTipo()
        If cboTipoSocio.SelectedValue Is Nothing Then
            lblReglasTipo.Text = ""
            Return
        End If

        Try
            Dim fila = Db.ConsultarFila(
                "SELECT nombre, max_prestamos, dias_prestamo, multa_diaria FROM tipo_socio WHERE idtipo = @t",
                New Microsoft.Data.SqlClient.SqlParameter("@t", CInt(cboTipoSocio.SelectedValue)))
            If fila Is Nothing Then Return

            lblReglasTipo.Text = $"Un {Db.Texto(fila, "nombre").ToLower()} puede tener " &
                                 $"{Db.Numero(fila, "max_prestamos")} ejemplares a la vez, por " &
                                 $"{Db.Numero(fila, "dias_prestamo")} días, con multa de " &
                                 $"{Formato.Dinero(Db.Monto(fila, "multa_diaria"))} por día de retraso."

        Catch ex As Exception
            lblReglasTipo.Text = ""
            Registro.Advertencia($"No se pudieron leer las reglas del tipo de socio: {ex.Message}")
        End Try
    End Sub

    Private Sub btnSugerir_Click(sender As Object, e As RoutedEventArgs) Handles btnSugerir.Click
        Try
            txtCodigo.Text = SocioService.SugerirCodigo()
        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Sugerir el código del socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevo.Click
        dgSocios.SelectedItem = Nothing
        LimpiarFormulario()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        If String.IsNullOrWhiteSpace(txtNombre.Text) OrElse String.IsNullOrWhiteSpace(txtApellido.Text) Then
            Avisar("Escribe el nombre y el apellido del socio.")
            Return
        End If
        If cboTipoSocio.SelectedValue Is Nothing Then
            Avisar("Elige el tipo de socio: de él dependen el cupo, el plazo y la multa.")
            Return
        End If

        Try
            Dim problema As String
            Dim codigo = txtCodigo.Text.Trim().ToUpper()

            If editandoId = "" Then
                problema = SocioService.Crear(codigo, txtNombre.Text, txtApellido.Text,
                                              txtIdentidad.Text, txtTelefono.Text, txtEmail.Text,
                                              txtDireccion.Text, CInt(cboTipoSocio.SelectedValue))
            Else
                problema = SocioService.Actualizar(editandoId, txtNombre.Text, txtApellido.Text,
                                                   txtIdentidad.Text, txtTelefono.Text, txtEmail.Text,
                                                   txtDireccion.Text, CInt(cboTipoSocio.SelectedValue),
                                                   chkActivo.IsChecked = True)
            End If

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo guardar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim nombre = $"{txtNombre.Text.Trim()} {txtApellido.Text.Trim()}"
            CargarLista()
            dgSocios.SelectedItem = Nothing
            LimpiarFormulario()
            DialogoBiblioteca.Show($"Los datos de {nombre} quedaron guardados.",
                                   "Guardado con éxito", MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Guardar el socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminar.Click
        If editandoId = "" Then
            Avisar("Selecciona primero un socio de la lista.")
            Return
        End If

        Dim nombre = $"{txtNombre.Text.Trim()} {txtApellido.Text.Trim()}"
        If DialogoBiblioteca.Show($"¿Eliminar a {nombre} del registro de socios? " &
                                  "Esta acción no se puede deshacer.",
                                  "Confirmar", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim problema = SocioService.Eliminar(editandoId)
            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo eliminar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            CargarLista()
            dgSocios.SelectedItem = Nothing
            LimpiarFormulario()
            DialogoBiblioteca.Show($"{nombre} se eliminó del registro.", "Eliminado con éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information)

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Eliminar el socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editandoId = ""
        ocupado = True
        txtCodigo.Clear()
        txtCodigo.IsEnabled = True
        txtNombre.Clear()
        txtApellido.Clear()
        txtIdentidad.Clear()
        txtEmail.Clear()
        txtTelefono.Clear()
        txtDireccion.Clear()
        cboTipoSocio.SelectedIndex = -1
        chkActivo.IsChecked = True
        ocupado = False

        lblReglasTipo.Text = ""
        lblTituloFormulario.Text = "Nuevo socio"
        lblSubtituloFormulario.Text = "Registra a quien se llevará libros"
        pnlEstadoCuenta.Visibility = Visibility.Collapsed
    End Sub

    ' ======================= ESTADO DE CUENTA =======================

    ''' <summary>Un solo procedimiento devuelve la ficha, lo que tiene afuera y lo
    ''' que debe. Aquí se reparten las tres tablas del resultado.</summary>
    Private Sub MostrarEstadoDeCuenta(idSocio As String)
        Try
            Dim ds = SocioService.EstadoDeCuenta(idSocio)
            pnlEstadoCuenta.Visibility = Visibility.Visible

            If ds.Tables.Count > 1 Then
                Dim afuera = ds.Tables(1)
                listaAfuera.ItemsSource = afuera.DefaultView
                lblSinAfuera.Visibility = If(afuera.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
            End If

            If ds.Tables.Count > 2 Then
                Dim deudas = ds.Tables(2)
                listaDeudas.ItemsSource = deudas.DefaultView
                lblSinDeudas.Visibility = If(deudas.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
                btnCobrarTodo.Visibility = If(deudas.Rows.Count = 0, Visibility.Collapsed, Visibility.Visible)
            End If

        Catch ex As Exception
            pnlEstadoCuenta.Visibility = Visibility.Collapsed
            Registro.Advertencia($"No se pudo leer el estado de cuenta: {ex.Message}")
        End Try
    End Sub

    Private Sub btnCobrarTodo_Click(sender As Object, e As RoutedEventArgs) Handles btnCobrarTodo.Click
        If editandoId = "" Then Return

        Dim nombre = $"{txtNombre.Text.Trim()} {txtApellido.Text.Trim()}"
        If DialogoBiblioteca.Show($"¿Cobrar todas las multas pendientes de {nombre}?",
                                  "Cobrar multas", MessageBoxButton.YesNo,
                                  MessageBoxImage.Question) <> MessageBoxResult.Yes Then Return

        Try
            Dim total As Decimal = 0
            Dim cantidad As Integer = 0
            Dim problema = MultaService.PagarTodasDelSocio(editandoId, total, cantidad)

            If problema IsNot Nothing Then
                DialogoBiblioteca.Show(problema, "No se pudo cobrar",
                                       MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            MostrarEstadoDeCuenta(editandoId)
            CargarLista()
            DialogoBiblioteca.MostrarConDato(
                $"Se cobraron {cantidad} multas de {nombre}. El socio queda solvente.",
                "Cobro registrado con éxito", Formato.Dinero(total))

        Catch ex As Exception
            DialogoBiblioteca.Show(MensajeError.Traducir("Cobrar las multas del socio", ex), "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Shared Sub Avisar(mensaje As String)
        DialogoBiblioteca.Show(mensaje, "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning)
    End Sub
End Class
