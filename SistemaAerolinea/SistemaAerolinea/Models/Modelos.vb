''' <summary>Cuenta que entra al sistema. No es lo mismo que `pasajero`: un agente
''' entra pero no viaja, y un pasajero puede viajar sin tener cuenta (lo registra
''' el agente en el mostrador). Cuando el rol es Pasajero, IdPasajero apunta a su
''' ficha de viajero y es lo que limita todo lo que puede ver.</summary>
Public Class Usuario
    Public Property UsuarioID As Integer
    Public Property NombreCompleto As String
    Public Property Email As String
    Public Property NombreUsuario As String
    Public Property Rol As String
    Public Property DebeCambiarContrasena As Boolean
    Public Property IdPasajero As String

    Public ReadOnly Property EsAdministrador As Boolean
        Get
            Return Rol = "Administrador"
        End Get
    End Property

    Public ReadOnly Property EsAgente As Boolean
        Get
            Return Rol = "Agente"
        End Get
    End Property

    Public ReadOnly Property EsPasajero As Boolean
        Get
            Return Rol = "Pasajero"
        End Get
    End Property

    ''' <summary>Administradores y agentes: los que trabajan en la aerolínea.</summary>
    Public ReadOnly Property EsPersonal As Boolean
        Get
            Return Not EsPasajero
        End Get
    End Property
End Class

''' <summary>Un asiento dentro del mapa de un vuelo, con el precio que le
''' corresponde en ESE vuelo según su clase.</summary>
Public Class AsientoMapa
    Public Property IdAsiento As Integer
    Public Property Fila As Integer
    Public Property Letra As String
    Public Property Etiqueta As String          ' 12C
    Public Property Clase As String
    Public Property IdTarifa As Integer
    Public Property Precio As Decimal
    Public Property Impuesto As Decimal
    Public Property Total As Decimal
    Public Property EquipajeKg As Integer
    Public Property Ocupado As Boolean
End Class

''' <summary>Un asiento ya elegido en el asistente, con el pasajero que lo ocupará.</summary>
Public Class AsientoElegido
    Public Property Asiento As AsientoMapa
    Public Property IdPasajero As String
    Public Property NombrePasajero As String

    Public ReadOnly Property Etiqueta As String
        Get
            Return Asiento.Etiqueta
        End Get
    End Property

    Public ReadOnly Property Clase As String
        Get
            Return Asiento.Clase
        End Get
    End Property

    Public ReadOnly Property Precio As Decimal
        Get
            Return Asiento.Precio
        End Get
    End Property

    Public ReadOnly Property Impuesto As Decimal
        Get
            Return Asiento.Impuesto
        End Get
    End Property

    Public ReadOnly Property Total As Decimal
        Get
            Return Asiento.Total
        End Get
    End Property

    Public ReadOnly Property Asignado As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(IdPasajero)
        End Get
    End Property
End Class

''' <summary>Datos del vuelo que el asistente lleva de un paso al siguiente.</summary>
Public Class VueloElegido
    Public Property IdVuelo As Integer
    Public Property CodigoVuelo As String
    Public Property Aerolinea As String
    Public Property TipoAvion As String
    Public Property IataOrigen As String
    Public Property CiudadOrigen As String
    Public Property IataDestino As String
    Public Property CiudadDestino As String
    Public Property FechaSalida As DateTime
    Public Property FechaLlegada As DateTime
    Public Property DuracionMinutos As Integer
    Public Property AsientosPorFila As Integer
    Public Property AsientosDisponibles As Integer

    Public ReadOnly Property Ruta As String
        Get
            Return $"{IataOrigen} → {IataDestino}"
        End Get
    End Property

    Public ReadOnly Property Descripcion As String
        Get
            Return $"{CodigoVuelo} · {CiudadOrigen} → {CiudadDestino}"
        End Get
    End Property
End Class

''' <summary>Lo que devuelve emitir una reserva: el localizador que se le entrega
''' al pasajero y el total cobrado.</summary>
Public Class ResultadoReserva
    Public Property IdReserva As Integer
    Public Property CodigoReserva As String
    Public Property Subtotal As Decimal
    Public Property Impuesto As Decimal
    Public Property Total As Decimal
    Public Property Boletos As Integer
End Class
