# ALAS Honduras — Sistema de Reserva de Vuelos

> *Conectamos Honduras con el mundo.*

Sistema de reserva y operación de vuelos desarrollado en **VB.NET** sobre **WPF** y **SQL Server**.
Proyecto del III Parcial — Alejandro Calderón.

La base de datos parte del script **`DB RESERVA.sql` del II Parcial** y lo lleva a un modelo que un
sistema real puede operar. Los cambios y el porqué de cada uno están explicados en
[`Scripts/01_esquema.sql`](Scripts/01_esquema.sql).

---

## Qué hace el sistema

| Módulo | Qué resuelve |
|--------|--------------|
| **Panel de control** | Indicadores del día, gráfico de ingresos de la última semana, próximas salidas y rutas más vendidas |
| **Llegadas y salidas** | Tablero del aeropuerto: marcar retrasos, abrir el embarque, cancelar vuelos y ver el manifiesto de pasajeros |
| **Nueva reserva** | Asistente de 4 pasos: elegir la ruta (los vuelos se listan solos) → **mapa de asientos interactivo** → asignar pasajeros → cobrar y emitir |
| **Reservas** | Consultar por localizador, hacer el check-in, reimprimir pases de abordar y cancelar |
| **Pagos** | Cobros totales o parciales, comprobantes numerados e historial |
| **Vuelos** | Programación de la malla: horarios, aeronave, ruta, precio base y estado |
| **Pasajeros** | Alta de quienes viajan, con su historial de vuelos |
| **Catálogos** | Países, aeropuertos, aerolíneas, aviones (con generación automática del mapa de asientos) y tarifas |
| **Usuarios** | Cuentas del personal, roles, contraseñas temporales *(solo Administrador)* |
| **Bitácora** | Auditoría de quién hizo qué y cuándo *(solo Administrador)* |

### Y el portal del pasajero

El pasajero entra al **mismo sistema** con su propia cuenta y ve un menú distinto:

| Módulo | Qué resuelve |
|--------|--------------|
| **Mis vuelos** | Su próximo viaje con cuenta regresiva, sus reservas, **su pago en línea**, su check-in y su pase |
| **Reservar un vuelo** | El mismo asistente, pero solo puede asignarse a sí mismo y a los acompañantes que registre |
| **Mi perfil** | Sus datos de viajero, su historial de vuelos y su contraseña |

Entra con usuario y contraseña o **con su cuenta de Google**.

**Transversal:** inicio de sesión con BCrypt, recuperación de contraseña por dos caminos, tres
roles (Administrador, Agente y Pasajero), pase de abordar en PDF y diálogos propios del sistema.

---

## Lo que hace distinto a este sistema

**El mapa de asientos.** Se dibuja con la configuración real de cada aeronave —4, 6 u 8 asientos
por fila, con el pasillo en su sitio— y muestra en color qué está libre, qué está vendido y qué se
lleva seleccionado. El precio de cada asiento sale de su clase y del precio base de ese vuelo.

**No se puede vender dos veces el mismo asiento.** No por una comprobación en la pantalla, que
siempre se puede saltar, sino por un **índice único filtrado** en la base de datos:

```sql
CREATE UNIQUE NONCLUSTERED INDEX UQ_boleto_vuelo_asiento
    ON dbo.boleto (idvuelo, idasiento)
    WHERE estado <> 'Cancelado';
```

Al ser filtrado, cancelar un boleto libera el asiento sin borrar el histórico.

**La emisión es atómica.** Crear una reserva toca tres tablas (`reserva`, `boleto` y `pago`) dentro
de una transacción `Serializable`. Si otro agente vende el mismo asiento un instante antes, se
deshace todo, no queda ninguna reserva a medias y la pantalla vuelve al mapa ya actualizado.

**Los precios son históricos.** El boleto guarda el precio y el impuesto con que se vendió. Cambiar
la tarifa mañana no altera lo que ya se facturó.

**El pase de abordar es un PDF de verdad.** Lleva un código de barras **PDF417 con la cadena IATA
BCBP** (Resolución 792, formato M1): 60 caracteres de ancho fijo, sin separadores, cada dato en su
posición exacta. Es el mismo formato que llevan los pases de cualquier aerolínea del mundo, y por
eso un lector de cualquier aeropuerto lo entiende. Si el viaje tiene escala, el pase la anuncia.
Solo existe **después del check-in**, que es cuando la aerolínea confirma que esa persona viaja.

**Un pasajero no puede ver los datos de otro.** El filtro no está en la pantalla —donde un descuido
lo saltaría— sino en los servicios: `ReservaService` lo aplica solo, a partir de la sesión, y
comprueba la propiedad de la reserva antes de abrir el detalle, hacer check-in o emitir un pase.
A un pasajero, la lista de personas solo se devuelve con él mismo dentro.

---

## Requisitos

- Visual Studio 2022 o superior
- .NET 9 SDK
- SQL Server Express (instancia `ALECALDE\SQLEXPRESS`)

---

## Instalación

**1. Crear la base de datos.** Ejecuta los scripts **en orden** desde SQL Server Management
Studio, o desde la terminal. La opción `-I` es necesaria: el índice único filtrado exige
`QUOTED_IDENTIFIER ON`.

> **Sobre los acentos.** Los `.sql` están en UTF-8 **con BOM**, para que tanto SSMS como
> `sqlcmd` los lean bien sin banderas extra. Si los abres y los vuelves a guardar en otra
> codificación, "Bogotá" entrará a la base como "BogotÃ¡". Si eso ya te pasó, el script
> `05_corregir_acentos.sql` lo repara sin tener que rehacer la base.

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -C -I -i "Scripts/01_esquema.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -C -I -i "Scripts/02_datos_semilla.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -C -I -i "Scripts/03_vistas_indices_procedimientos.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -C -I -i "Scripts/04_sistema_login.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -C -I -i "Scripts/05_corregir_acentos.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -C -I -i "Scripts/06_portal_pasajero.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -C -I -i "Scripts/07_pagos_portal_y_google.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -C -I -i "Scripts/08_bloqueo_de_intentos.sql"
```

Los scripts son **idempotentes**: se pueden volver a ejecutar sin error.

**2. Abrir y ejecutar.** Abre `SistemaAerolinea.slnx` en Visual Studio y presiona F5.

**3. Crear tu cuenta.** En la pantalla de inicio haz clic en *Regístrate aquí*.

- El **primer registro queda como Administrador**, porque alguien tiene que poder crear a los demás.
- A partir de ahí, el registro público es el de **pasajeros**: pide también los datos de viajero y
  crea la cuenta y la ficha juntas.
- Las cuentas del **personal** (Agente o Administrador) las crea un Administrador desde la pantalla
  de *Usuarios*, con contraseña temporal. En una aerolínea nadie se da de alta como agente solo.

Si tu servidor tiene otro nombre, cambia la cadena de conexión en
[`SistemaAerolinea/Services/Db.vb`](SistemaAerolinea/Services/Db.vb).

---

## Ejecutar las pruebas

```bash
dotnet test SistemaAerolinea.Tests/SistemaAerolinea.Tests.vbproj
```

**140 pruebas** cubren cinco frentes:

- **Lógica pura** — validaciones, totales de una reserva, localizadores, comprobantes, formatos y
  la cadena IATA BCBP campo por campo.
- **Interfaz** — construye las 24 vistas y muestra las 11 páginas para que sus manejadores de
  `Loaded` se ejecuten. Caza los recursos de estilo inexistentes y los controles mal inicializados,
  que el compilador no detecta y solo revientan al abrir la pantalla.
- **Acceso a datos** — ejecuta las ~39 consultas de lectura contra la base real y comprueba que el
  mapa de asientos cuadre con la disponibilidad del vuelo.
- **Aislamiento del portal** — que un pasajero no pueda listar, abrir, hacer check-in, pagar ni
  emitir el pase de una reserva ajena.
- **Criptografía de Google** — que el verificador PKCE cumpla el RFC, que su huella sea realmente
  SHA-256 en base64url y que no deje adivinar el original. El flujo completo no se automatiza
  (abre el navegador y necesita una cuenta real), pero sí las piezas donde un error se convierte en
  un agujero de seguridad en vez de en un botón que no anda.

Las que necesitan SQL Server se dan por buenas si no hay servidor, para que la integración continua
no dependa de una base de datos.

---

## Estructura del código

Arquitectura **en capas**: la lógica del negocio no sabe nada de la interfaz, y por eso se puede
probar sin abrir una sola ventana.

```
SistemaAerolinea/
├── Models/        Entidades del negocio (Usuario, AsientoMapa, VueloElegido…)
├── Services/      Lógica y acceso a datos (ReservaService, VueloService, AuthService…)
│   └── Db.vb      Conexión única, consultas parametrizadas y transacciones
├── Utilities/     Apoyo transversal (Validador, Sesión, Registro, MensajeError, GeneradorPnr)
├── Converters/    Conversores de la interfaz (estado → color, minutos → duración)
├── Views/         Ventanas, páginas y controles (XAML + code-behind)
│   └── Pages/     Una página por módulo del menú lateral
├── Application.xaml   Sistema de diseño completo: paleta, botones, campos, tablas
└── Scripts/       Scripts SQL de la base de datos
```

### Reglas que se respetan

- Contraseñas y respuestas de seguridad con **hash BCrypt** (factor 11), nunca en texto plano
- **Todas** las consultas usan parámetros — nunca se concatena un valor dentro del SQL
- Bloqueo de **30 segundos** tras 3 intentos fallidos, contado sobre la **cuenta** y no sobre
  la ventana: cerrarla y volver a abrirla no lo reinicia, y vale para todas las terminales
- Los errores técnicos van a `logs/`; al usuario se le muestra un mensaje entendible (OWASP)
- Permisos concentrados en una sola clase (`Permisos`), las vistas nunca comparan el rol
- El aislamiento entre pasajeros y los permisos que mueven dinero los aplican los
  **servicios**, no las pantallas
- El estilo visual está centralizado en `Application.xaml`: ninguna vista define colores propios

---

## Inicio de sesión con Google

Implementado con **OAuth 2.0 + PKCE** (RFC 8252 y RFC 7636), el flujo que corresponde a una
aplicación de escritorio. En orden:

1. Se genera un secreto de un solo uso (*verificador*) y a Google solo se le manda su huella
   SHA-256. Eso es **PKCE**: sin el verificador original, un código de autorización robado no
   sirve para nada.
2. Se abre el **navegador del usuario**, no una ventana dentro del programa. Es lo que exige el
   RFC 8252: la contraseña de Google se escribe en el sitio de Google, y esta aplicación nunca la
   ve ni podría verla.
3. Google redirige a un servidor diminuto que vive en `127.0.0.1`, en un puerto libre, durante los
   segundos que dura el intercambio. Se usa `TcpListener` y no `HttpListener` a propósito: el
   segundo exige en Windows una reserva de URL o permisos de administrador.
4. Con el código y el verificador se pide el token y del `id_token` se leen el identificador, el
   nombre y el correo.

La cuenta se liga por el **`sub` de Google**, no por el correo: una persona puede cambiar de correo
y seguir siendo la misma. Si ya existía una cuenta con ese correo, las dos identidades se ligan
solas la primera vez. Si no existe ninguna, se le piden sus datos de viajero una única vez —Google
confirma quién es, pero no sabe su número de documento ni su fecha de nacimiento, y sin eso no se
puede emitir un boleto.

### Cómo configurarlo

1. En [Google Cloud Console](https://console.cloud.google.com/) → *Google Auth Platform* → *Clientes*,
   crea un cliente de tipo **Aplicación de escritorio**.
2. Descarga su JSON, renómbralo a **`google-oauth.json`** y déjalo en la carpeta
   `SistemaAerolinea/` (junto al `.vbproj`). Se copia solo a la carpeta de salida al compilar.
3. Si la pantalla de consentimiento está en modo *Testing*, agrega tu correo en **Público → Usuarios
   de prueba**; si no, Google rechazará el acceso.

No hay que configurar ninguna URL de redirección: para los clientes de escritorio Google acepta
`127.0.0.1` en cualquier puerto.

> El archivo `google-oauth.json` está en el `.gitignore` y **nunca se sube al repositorio**. En el
> proyecto queda `google-oauth.example.json` como plantilla. Si el archivo no está, el sistema
> funciona con normalidad y el botón de Google simplemente no aparece.

El acceso por usuario y contraseña (BCrypt) sigue siendo el camino principal: funciona sin internet
y sin depender de ningún servicio externo.

---

## Pago en línea desde el portal

El pasajero paga su reserva desde la misma aplicación, como en cualquier aerolínea de hoy: elige el
método, se cobra el saldo completo y la reserva queda confirmada al instante, sin pasar por el
mostrador. Cada cobro guarda su **referencia de autorización** y el **canal** por el que entró
(mostrador o portal), que es lo que permite después cuadrar la caja.

**No se pide ni se guarda el número de ninguna tarjeta**, y es a propósito. En un sistema real ese
dato lo captura la pasarela (Stripe, PayPal, un banco), que solo devuelve la autorización: así el
comercio nunca toca el número y no queda obligado a cumplir la norma **PCI-DSS**. Este proyecto
simula esa respuesta manteniendo el mismo flujo y exactamente los mismos datos que se guardarían de
verdad. El efectivo no se ofrece en el portal, por razones evidentes.

---

## Base de datos `dbreserva_vuelos`

| Tabla | Contenido |
|-------|-----------|
| `pais`, `aeropuerto`, `aerolinea` | Catálogos geográficos y de compañías |
| `avion`, `asiento` | Aeronaves y el mapa de asientos real de cada una |
| `tarifa` | Clase, multiplicador de precio, impuesto y equipaje incluido |
| `vuelo` | Trayecto programado: aeronave, ruta, horarios, precio base y estado |
| `pasajero` | Quienes viajan |
| `reserva` | Localizador (PNR), titular, estado y desglose de importes |
| `boleto` | Un asiento de un vuelo vendido a un pasajero |
| `pago`, `metodo_pago` | Cobros y comprobantes |
| `usuario`, `bitacora` | Cuentas del sistema (con el vínculo a la ficha del pasajero) y auditoría |

### Qué cambió respecto al script del II Parcial

| Antes | Ahora | Por qué |
|-------|-------|---------|
| `vuelo` guardaba un asiento, una reserva y una tarifa | `vuelo` es el trayecto; `boleto` es el asiento vendido | Cada fila de `vuelo` era en realidad un boleto: así no se podía vender más de un asiento por vuelo |
| `asiento` era una lista de 20 compartida | Cada asiento pertenece a un avión y tiene su clase | Un asiento 12C solo existe dentro de una aeronave concreta |
| `tarifa` eran 20 precios sueltos | Una tarifa por clase, con multiplicador | Cambiar el precio de una ruta es cambiar un solo número |
| `reserva` no se ligaba al pasajero | `reserva` tiene titular y localizador de 6 caracteres | Es lo que se le entrega al pasajero para consultar su vuelo |
| `pasajero.clave` en texto plano | Tabla `usuario` aparte, con hash BCrypt | Quien opera el sistema no es quien viaja, y una contraseña nunca se guarda legible |

Incluye **3 vistas**, **12 índices** y **8 procedimientos almacenados** para las consultas del panel
de control, el buscador de vuelos y el mapa de asientos.

---

## Identidad visual

| Color | Código | Uso |
|-------|--------|-----|
| Azul noche | `#08182F` | Menú lateral, encabezados, pases de abordar |
| Azul cielo | `#0C7CD5` | Acciones principales, indicadores |
| Ámbar atardecer | `#F2B01E` | Acentos y el avión del logotipo |

Tipografía: Segoe UI, con Consolas para códigos de vuelo, asientos y localizadores —igual que en un
tablero de aeropuerto. El logotipo es **vectorial**: no depende de ningún archivo de imagen y se ve
nítido en cualquier resolución.
