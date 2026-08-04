Imports System.Data

''' <summary>Los datos de viajero del pasajero que tiene la sesión abierta.
'''
''' Solo puede cambiar su teléfono, su correo y su país. El nombre, el documento y
''' la fecha de nacimiento quedan bloqueados: son los que la aerolínea ya imprimió
''' en boletos emitidos, y cambiarlos por cuenta propia rompería la coincidencia
''' con el documento de identidad que se presenta en el aeropuerto.</summary>
Public Class MiPerfilPage

    Public Sub Cargar()
        Dim idPasajero = Sesion.IdPasajero
        If idPasajero Is Nothing Then Return

        Try
            If cboPais.ItemsSource Is Nothing Then
                cboPais.ItemsSource = CatalogoService.PaisesParaCombo().DefaultView
            End If

            Dim ficha = PasajeroService.Obtener(idPasajero)
            If ficha Is Nothing Then
                DialogoAlas.Show("No se encontró tu ficha de viajero. Avisa a la aerolínea.",
                                 "Ficha no encontrada", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            txtCodigo.Text = ficha("idpasajero").ToString()
            txtNombre.Text = $"{ficha("nombre_p")} {ficha("apaterno")}" &
                             If(IsDBNull(ficha("amaterno")), "", " " & ficha("amaterno").ToString())
            txtDocumento.Text = $"{ficha("tipo_documento")} · {ficha("num_documento")}"
            txtNacimiento.Text = CDate(ficha("fecha_nacimiento")).ToString("dd/MM/yyyy")
            cboPais.SelectedValue = ficha("idpais").ToString()
            txtTelefono.Text = If(IsDBNull(ficha("telefono")), "", ficha("telefono").ToString())
            txtCorreo.Text = ficha("email").ToString()

            CargarCuenta()
            CargarHistorial(idPasajero)

        Catch ex As Exception
            Avisar("Cargar tu perfil", ex)
        End Try
    End Sub

    Private Sub CargarCuenta()
        Dim u = Sesion.UsuarioActual
        If u Is Nothing Then Return

        lblNombreCuenta.Text = u.NombreCompleto
        lblUsuarioCuenta.Text = $"@{u.NombreUsuario} · {u.Email}"

        Dim partes = If(u.NombreCompleto, "").Split(" "c).Where(Function(p) p.Length > 0).ToArray()
        lblIniciales.Text = If(partes.Length = 0, "?",
                               If(partes.Length = 1, partes(0).Substring(0, 1),
                                  partes(0).Substring(0, 1) & partes(1).Substring(0, 1))).ToUpper()
    End Sub

    Private Sub CargarHistorial(idPasajero As String)
        Dim historial = PasajeroService.Historial(idPasajero)
        dgHistorial.ItemsSource = historial.DefaultView

        lblTotalVuelos.Text = historial.Rows.Count.ToString()
        lblDestinos.Text = historial.DefaultView.ToTable(True, "iata_destino").Rows.Count.ToString()
        lblSinHistorial.Visibility = If(historial.Rows.Count = 0, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As RoutedEventArgs) Handles btnGuardar.Click
        Dim idPasajero = Sesion.IdPasajero
        If idPasajero Is Nothing Then Return

        If cboPais.SelectedValue Is Nothing Then
            DialogoAlas.Show("Selecciona tu país.", "Campos incompletos",
                             MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        Try
            ' Los campos bloqueados se releen de la base: así ni un error de la
            ' interfaz podría enviarlos alterados
            Dim ficha = PasajeroService.Obtener(idPasajero)
            If ficha Is Nothing Then Return

            Dim problema = PasajeroService.Guardar(
                idPasajero,
                ficha("nombre_p").ToString(),
                ficha("apaterno").ToString(),
                If(IsDBNull(ficha("amaterno")), "", ficha("amaterno").ToString()),
                ficha("tipo_documento").ToString(),
                ficha("num_documento").ToString(),
                CDate(ficha("fecha_nacimiento")),
                cboPais.SelectedValue.ToString(),
                txtTelefono.Text, txtCorreo.Text, editando:=True)

            If problema IsNot Nothing Then
                DialogoAlas.Show(problema, "Revisa los datos", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            DialogoAlas.Show("Tus datos quedaron guardados.", "Guardado con éxito",
                             MessageBoxButton.OK, MessageBoxImage.Information)
            Cargar()

        Catch ex As Exception
            Avisar("Guardar tus datos", ex)
        End Try
    End Sub

    Private Sub btnMiCuenta_Click(sender As Object, e As RoutedEventArgs) Handles btnMiCuenta.Click
        Dim ventana As New MiCuentaWindow With {.Owner = Window.GetWindow(Me)}
        ventana.ShowDialog()
        CargarCuenta()
    End Sub

    Private Sub Avisar(contexto As String, ex As Exception)
        DialogoAlas.Show(MensajeError.Traducir(contexto, ex), "Error",
                         MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub
End Class
