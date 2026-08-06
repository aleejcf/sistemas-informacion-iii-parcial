Imports System.Data
Imports System.IO
Imports Microsoft.Win32

Public Class ParqueaderosPage

    Private editando As Boolean = False

    Private ReadOnly Property CarpetaFotos As String
        Get
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fotos")
        End Get
    End Property

    Private Sub ParqueaderosPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Not Permisos.PuedeEliminar Then
            btnEliminar.IsEnabled = False
            lblSoloAdmin.Visibility = Visibility.Visible
        End If
        CargarLista()
        LimpiarFormulario()
    End Sub

    Private Sub CargarLista()
        Try
            dgParqueaderos.ItemsSource = ParqueaderoService.Listar().DefaultView
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al cargar parqueaderos", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub dgParqueaderos_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles dgParqueaderos.SelectionChanged

        Dim fila = TryCast(dgParqueaderos.SelectedItem, DataRowView)
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
            ' Se carga en memoria para no bloquear el archivo y poder reemplazarlo
            Dim bmp As New BitmapImage()
            bmp.BeginInit()
            bmp.CacheOption = BitmapCacheOption.OnLoad
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache
            bmp.UriSource = New Uri(ruta)
            bmp.EndInit()
            imgFoto.Source = bmp
            lblSinFoto.Visibility = Visibility.Collapsed
        Else
            imgFoto.Source = Nothing
            lblSinFoto.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub btnFoto_Click(sender As Object, e As RoutedEventArgs) Handles btnFoto.Click
        If String.IsNullOrWhiteSpace(txtCodigo.Text) Then
            DialogoParko.Show("Primero escribe o selecciona el código del parqueadero.", "Falta el código",
                            MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim dialogo As New OpenFileDialog With {
            .Filter = "Imágenes|*.jpg;*.jpeg;*.png",
            .Title = "Seleccionar foto del parqueadero"
        }
        If dialogo.ShowDialog() <> True Then Return

        Try
            Directory.CreateDirectory(CarpetaFotos)
            Dim destino = Path.Combine(CarpetaFotos, txtCodigo.Text.Trim() & ".jpg")
            File.Copy(dialogo.FileName, destino, overwrite:=True)
            MostrarFoto(txtCodigo.Text.Trim())
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("No se pudo copiar la foto", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ---------- Botones ----------
    Private Sub btnNuevo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuevo.Click
        LimpiarFormulario()
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        If String.IsNullOrWhiteSpace(txtCodigo.Text) OrElse String.IsNullOrWhiteSpace(txtDireccion.Text) Then
            DialogoParko.Show("Completa al menos el código y la dirección.", "Campos incompletos",
                            MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            If editando Then
                ParqueaderoService.Actualizar(txtCodigo.Text, txtDireccion.Text, txtTelefono.Text,
                                              txtNit.Text, txtAdministrador.Text, txtOperador.Text, txtHorario.Text)
                DialogoParko.Show("Parqueadero actualizado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information)
            Else
                If ParqueaderoService.Existe(txtCodigo.Text) Then
                    DialogoParko.Show("Ya existe un parqueadero con ese código.", "Código duplicado",
                                    MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If
                ParqueaderoService.Insertar(txtCodigo.Text, txtDireccion.Text, txtTelefono.Text,
                                            txtNit.Text, txtAdministrador.Text, txtOperador.Text, txtHorario.Text)
                DialogoParko.Show("Parqueadero registrado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information)
            End If
            CargarLista()
            LimpiarFormulario()
        Catch ex As Exception
            DialogoParko.Show(MensajeError.Traducir("Error al guardar", ex), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As RoutedEventArgs) Handles btnEliminar.Click
        If Not editando Then
            DialogoParko.Show("Selecciona primero un parqueadero de la lista.", "Sin selección",
                            MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Dim respuesta = DialogoParko.Show($"¿Eliminar el parqueadero '{txtCodigo.Text}'?", "Confirmar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question)
        If respuesta <> MessageBoxResult.Yes Then Return

        Try
            ParqueaderoService.Eliminar(txtCodigo.Text)
            DialogoParko.Show("Parqueadero eliminado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information)
            CargarLista()
            LimpiarFormulario()
        Catch ex As Exception
            Registro.Error_("Eliminar parqueadero", ex)
            DialogoParko.Show("No se pudo eliminar. Es posible que tenga clientes o vehículos asociados.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub LimpiarFormulario()
        editando = False
        ' El sistema asigna el código automáticamente
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
        imgFoto.Source = Nothing
        lblSinFoto.Visibility = Visibility.Visible
        dgParqueaderos.SelectedItem = Nothing
        txtDireccion.Focus()
    End Sub
End Class
