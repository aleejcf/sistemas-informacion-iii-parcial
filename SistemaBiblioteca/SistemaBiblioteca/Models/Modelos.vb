''' <summary>Cuenta del personal que opera el sistema. Distinta de `socio`:
''' una es de quien presta el libro y otra de quien se lo lleva.</summary>
Public Class Usuario
    Public Property UsuarioID As Integer
    Public Property NombreCompleto As String
    Public Property Email As String
    Public Property NombreUsuario As String
    Public Property Rol As String
    Public Property DebeCambiarContrasena As Boolean

    Public ReadOnly Property EsAdministrador As Boolean
        Get
            Return Rol = "Administrador"
        End Get
    End Property
End Class

''' <summary>El socio tal como lo necesita el mostrador de préstamo: quién es y,
''' sobre todo, si puede llevarse libros hoy y cuántos.</summary>
Public Class SocioResumen
    Public Property IdSocio As String
    Public Property NombreCompleto As String
    Public Property Email As String
    Public Property Telefono As String
    Public Property TipoSocio As String
    Public Property MaxPrestamos As Integer
    Public Property DiasPrestamo As Integer
    Public Property MultaDiaria As Decimal
    Public Property EjemplaresAfuera As Integer
    Public Property PrestamosVencidos As Integer
    Public Property MultasPendientes As Integer
    Public Property MontoAdeudado As Decimal
    Public Property CupoDisponible As Integer
    Public Property PuedePrestar As Boolean
    Public Property EstaActivo As Boolean

    ''' <summary>La razón por la que NO puede prestar, para decírsela al usuario en
    ''' vez de un "no se puede" a secas. Nothing si sí puede.</summary>
    Public ReadOnly Property MotivoBloqueo As String
        Get
            If PuedePrestar Then Return Nothing
            If Not EstaActivo Then Return "El socio está inactivo."
            If PrestamosVencidos > 0 Then
                Return If(PrestamosVencidos = 1,
                          "Tiene un préstamo vencido sin devolver.",
                          $"Tiene {PrestamosVencidos} préstamos vencidos sin devolver.")
            End If
            If MontoAdeudado > 0 Then Return $"Tiene multas pendientes por {Formato.Dinero(MontoAdeudado)}."
            If CupoDisponible <= 0 Then
                Return $"Ya alcanzó su límite de {MaxPrestamos} ejemplares como {TipoSocio}."
            End If
            Return "No cumple los requisitos para llevarse libros."
        End Get
    End Property
End Class

''' <summary>Un ejemplar ya puesto en el carrito de préstamo.</summary>
Public Class EjemplarElegido
    Public Property IdEjemplar As Integer
    Public Property CodigoBarras As String
    Public Property IdLibro As String
    Public Property Titulo As String
    Public Property Autor As String
    Public Property Ubicacion As String
    Public Property Condicion As String
End Class

''' <summary>Lo que devuelve registrar un préstamo: el folio que se le entrega al
''' socio y la fecha en que tiene que traer los libros de vuelta.</summary>
Public Class ResultadoPrestamo
    Public Property IdPrestamo As Integer
    Public Property Codigo As String
    Public Property FechaVencimiento As Date
    Public Property Ejemplares As Integer
    Public Property Socio As String
End Class

''' <summary>Un ejemplar que vuelve al mostrador, con el estado en que llegó.</summary>
Public Class LineaDevolucion
    Public Property IdDetalle As Integer
    Public Property IdEjemplar As Integer
    Public Property CodigoBarras As String
    Public Property Titulo As String
    Public Property Condicion As String = "Bueno"

    ''' <summary>Si el bibliotecario marcó este ejemplar como recibido. Vive aquí y
    ''' no en una clase aparte porque la pantalla de devolución lista exactamente
    ''' estos objetos y marca unos sí y otros no: el socio puede traer dos de los
    ''' tres libros hoy. La página filtra por esta bandera antes de llamar al
    ''' servicio, que solo recibe las líneas que de verdad volvieron.</summary>
    Public Property Marcada As Boolean = True
End Class

''' <summary>Lo que devuelve registrar una devolución: cuántos ejemplares
''' volvieron y qué multa se generó, si es que se generó alguna.</summary>
Public Class ResultadoDevolucion
    Public Property Ejemplares As Integer
    Public Property DiasRetraso As Integer
    Public Property MontoMulta As Decimal
    Public Property PrestamoCerrado As Boolean

    Public ReadOnly Property HuboMulta As Boolean
        Get
            Return MontoMulta > 0
        End Get
    End Property
End Class
