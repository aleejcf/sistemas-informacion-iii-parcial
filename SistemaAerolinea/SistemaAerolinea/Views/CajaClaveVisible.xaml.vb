''' <summary>Caja de contraseña con botón de ojo para mostrarla u ocultarla.
''' Las dos cajas (oculta y visible) se mantienen sincronizadas mientras se escribe.
''' Es DRY aplicado: la usan el inicio de sesión, el registro, la recuperación,
''' el cambio obligatorio y "Mi cuenta".</summary>
Public Class CajaClaveVisible

    Private sincronizando As Boolean = False

    ''' <summary>La contraseña escrita, sin importar en qué modo esté la caja.</summary>
    Public Property Password As String
        Get
            Return caja.Password
        End Get
        Set(value As String)
            caja.Password = value
        End Set
    End Property

    ''' <summary>Se dispara con cada tecla: la usa el medidor de fuerza del registro.</summary>
    Public Event ClaveCambiada As EventHandler

    Private Sub caja_PasswordChanged(sender As Object, e As RoutedEventArgs) Handles caja.PasswordChanged
        If Not sincronizando Then
            sincronizando = True
            cajaVisible.Text = caja.Password
            sincronizando = False
        End If
        RaiseEvent ClaveCambiada(Me, EventArgs.Empty)
    End Sub

    Private Sub cajaVisible_TextChanged(sender As Object, e As TextChangedEventArgs) Handles cajaVisible.TextChanged
        If sincronizando Then Return
        sincronizando = True
        caja.Password = cajaVisible.Text
        sincronizando = False
    End Sub

    Private Sub btnOjo_Click(sender As Object, e As RoutedEventArgs) Handles btnOjo.Click
        If cajaVisible.Visibility = Visibility.Visible Then
            ' Ocultar la contraseña → icono de ojo abierto
            cajaVisible.Visibility = Visibility.Collapsed
            caja.Visibility = Visibility.Visible
            iconoOcultar.Visibility = Visibility.Collapsed
            iconoVer.Visibility = Visibility.Visible
            btnOjo.ToolTip = "Mostrar contraseña"
            caja.Focus()
        Else
            ' Mostrarla en texto plano → icono de ojo tachado
            caja.Visibility = Visibility.Collapsed
            cajaVisible.Visibility = Visibility.Visible
            iconoVer.Visibility = Visibility.Collapsed
            iconoOcultar.Visibility = Visibility.Visible
            btnOjo.ToolTip = "Ocultar contraseña"
            cajaVisible.Focus()
            cajaVisible.CaretIndex = cajaVisible.Text.Length
        End If
    End Sub

    Public Overloads Sub Clear()
        caja.Clear()
        cajaVisible.Clear()
    End Sub

    Public Overloads Sub Focus()
        If cajaVisible.Visibility = Visibility.Visible Then cajaVisible.Focus() Else caja.Focus()
    End Sub
End Class
