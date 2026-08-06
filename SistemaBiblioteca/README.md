# ALEJANDRÍA — Sistema de Biblioteca

> *El saber al alcance de todos.*

Sistema de gestión bibliotecaria desarrollado en **VB.NET (WPF)** sobre **SQL Server**.
Proyecto del III Parcial — Alejandro Calderón.

Parte de la consulta `DB BIBLIOTECA.sql` del **II Parcial** y la lleva a un modelo que
una biblioteca real puede operar.

---

## Qué hace el sistema

**Circulación**

- **Mostrador de préstamo** con asistente de tres pasos (socio → libros → confirmar) y
  carrito siempre a la vista
- **Devoluciones parciales**: el socio puede traer dos de los tres libros hoy y el
  tercero la otra semana
- **Multas automáticas** por retraso, daño y extravío, calculadas al registrar la devolución
- **Renovación** de préstamos, con límite y bloqueada si alguien reservó el título
- **Solvencia del socio**: no se presta a quien tiene mora o deuda, y el sistema
  explica exactamente por qué

**Acervo**

- **Catálogo** con búsqueda por título, autor, editorial, ISBN o código
- **Ejemplares físicos**: cada copia con su código de barras, ubicación y estado
- **Reservas** (fila de espera) para los títulos sin copias disponibles
- Catálogos de autoridad: autores, editoriales, categorías Dewey y tipos de socio

**Administración**

- **Inicio de sesión** con contraseñas cifradas (BCrypt) y recuperación por pregunta
  de seguridad o código de verificación
- **Roles**: Administrador (control total) y Bibliotecario (atiende el mostrador)
- **Panel de indicadores** con movimiento diario, mora y títulos más pedidos
- **Bitácora** de auditoría: quién hizo qué y cuándo

---

## Requisitos

- Visual Studio 2022 o superior
- .NET 9 SDK
- SQL Server Express (instancia `ALECALDE\SQLEXPRESS`)

---

## Instalación

**1. Crear la base de datos.** Ejecuta los cuatro scripts **en orden** desde SQL Server
Management Studio, o desde la terminal:

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -b -f 65001 -i "Scripts/01_esquema.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -b -f 65001 -i "Scripts/02_datos_semilla.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -b -f 65001 -i "Scripts/03_vistas_indices_procedimientos.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -b -f 65001 -i "Scripts/04_sistema_login.sql"
```

> El `-f 65001` le dice a sqlcmd que los archivos están en UTF-8; sin él, los acentos
> y las eñes entran mal a la base de datos.

Los scripts son **idempotentes**: se pueden volver a ejecutar sin error.

**2. Abrir y ejecutar.** Abre `SistemaBiblioteca.slnx` en Visual Studio y presiona F5.

**3. Crear tu cuenta.** En la pantalla de inicio haz clic en *Regístrate aquí*.
El **primer usuario registrado queda como Administrador**; los siguientes serán
Bibliotecarios.

Si la cadena de conexión de tu servidor es distinta, edítala en
`SistemaBiblioteca/Services/Db.vb`.

**4. Configurar el correo saliente (opcional).** Sirve para enviar el código de
verificación cuando alguien recupera su contraseña. La primera vez que se abre el
sistema se crea el archivo:

```
%APPDATA%\BibliotecaAlejandria\correo.config
```

Ábrelo y llena dos líneas — `remitente` con la dirección y `clave` con una
**contraseña de aplicación** (no la del correo). Para Gmail se genera en
[myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords) con la
verificación en dos pasos activada.

> El archivo vive en la carpeta del usuario, **fuera del proyecto**: la credencial no
> se entrega junto al código ni queda en el repositorio.

Mientras esté sin configurar, la recuperación sigue funcionando: el sistema muestra el
código en pantalla y explica por qué no lo envió.

---

## Ejecutar las pruebas

```bash
dotnet test SistemaBiblioteca.Tests/SistemaBiblioteca.Tests.vbproj
```

119 pruebas unitarias cubren las validaciones, el dígito de control del ISBN, el
cálculo de la mora, la regla de solvencia y la construcción de todas las vistas.
No necesitan base de datos.

---

## Qué cambió respecto al II Parcial

El script original funcionaba como ejercicio de consultas, pero tenía tres cosas que
un sistema en operación no puede sostener. Se conservan las llaves originales
(`L00001`, `U00001`) y los 20 libros y 20 socios de entonces.

### 1. `stock` era un número; ahora son ejemplares

Un número no se puede prestar: lo que se presta es **un libro físico** de la estantería.

| Antes | Ahora |
|-------|-------|
| `Libros.stock = 10` | 10 filas en `ejemplar`, cada una con su código de barras, su ubicación y su estado |

Esa separación es la que permite saber cuál copia tiene cada socio, marcar una como
dañada sin tocar las otras y **evitar prestar dos veces la misma**. Esto último lo
impone la base de datos, no la aplicación:

```sql
CREATE UNIQUE NONCLUSTERED INDEX UQ_ejemplar_en_prestamo
    ON dbo.detalle_prestamo (idejemplar)
    WHERE fecha_devolucion IS NULL;
```

El índice es único pero **filtrado**: solo mira los renglones todavía afuera. Cuando el
libro vuelve, el renglón deja de contar y el ejemplar se puede volver a prestar.

### 2. `fecha_devolucion` era ambigua

En los datos del II Parcial guardaba la fecha en que el libro *debía* volver, aun en
préstamos con estado `Activo`. Se separó en dos columnas:

- `fecha_vencimiento` — cuándo debe volver
- `fecha_devolucion` — cuándo volvió de verdad (`NULL` mientras sigue afuera)

Sin esa separación no se puede calcular un solo día de retraso, y por tanto la multa
no existe.

### 3. `Usuarios` mezclaba dos cosas distintas

La tabla original guardaba a los **socios** de la biblioteca. Ahora el sistema tiene
además cuentas de acceso para el personal:

| Tabla | Quién es |
|-------|----------|
| `socio` | Quien pide prestado (era `Usuarios`) |
| `usuario` | Quien **opera** el sistema |

### Además

- El autor, la editorial y la categoría eran texto libre dentro de `Libros`. Escribir
  "McGraw Hill" y "Mc Graw Hill" creaba dos editoriales y ningún filtro servía; ahora
  cada uno es una tabla de catálogo.
- Se agregó `tipo_socio`, que guarda **la política de préstamo en datos y no en código**:
  cuántos ejemplares se lleva cada tipo de socio, por cuántos días y cuánto paga por
  día de retraso. Cambiar el plazo de los estudiantes es cambiar un número.

---

## Estructura del código

El proyecto sigue una **arquitectura en capas**, donde la lógica del negocio no depende
de la interfaz.

```
SistemaBiblioteca/
├── Models/          Entidades del negocio (Usuario, SocioResumen, EjemplarElegido…)
├── Services/        Lógica y acceso a datos (PrestamoService, LibroService, MultaService…)
├── Utilities/       Apoyo transversal (Validador, Sesión, Registro, Comprobante)
├── Converters/      Conversores de enlace de datos (estado → color, días → texto)
├── Views/           Ventanas, páginas y controles (XAML + code-behind)
├── Assets/          Icono del ejecutable y el script que lo genera
└── Scripts/         Scripts SQL de la base de datos
```

### El icono

`Assets/alejandria.ico` se incrusta en el .exe mediante `<ApplicationIcon>`, que es lo
que hace que el Explorador y los accesos directos muestren el logotipo en vez del icono
genérico de Windows. (El `Icon="{StaticResource IconoApp}"` de las ventanas solo pinta
el icono en tiempo de ejecución.)

Trae siete resoluciones —16, 24, 32, 48, 64, 128 y 256 px— dibujadas desde la misma
geometría vectorial del logotipo. Si alguna vez cambia la marca, se regenera con:

```bash
powershell -ExecutionPolicy Bypass -File SistemaBiblioteca/Assets/GenerarIcono.ps1 SistemaBiblioteca/Assets/alejandria.ico
```

**Reglas que se respetan:**

- Las contraseñas se guardan con **hash BCrypt**, nunca en texto plano
- Todas las consultas usan **parámetros** (evita inyección SQL)
- La cuenta se **bloquea 30 segundos** tras 3 intentos fallidos
- Prestar y devolver corren dentro de una **transacción `Serializable`**: o se guarda
  todo, o no se guarda nada
- Los errores técnicos se guardan en `logs/`; al usuario se le muestra un mensaje
  entendible
- El estilo visual está centralizado en `Application.xaml` (colores, botones, tablas)

---

## Base de datos

| Tabla | Contenido |
|-------|-----------|
| `libro` | La obra catalogada: título, ISBN, autor, editorial, categoría |
| `ejemplar` | Cada copia física, con código de barras, ubicación y estado |
| `autor`, `editorial`, `categoria` | Catálogos de autoridad (categoría lleva su Dewey) |
| `socio` | Quienes se llevan libros (era `Usuarios` en el II Parcial) |
| `tipo_socio` | La política de préstamo: cupo, plazo y multa diaria |
| `prestamo` | Cabecera del préstamo, con su folio y sus fechas |
| `detalle_prestamo` | Cada ejemplar prestado, con la fecha en que volvió |
| `multa` | Cobros por retraso, daño y extravío |
| `reserva` | Fila de espera de los títulos sin copias libres |
| `usuario`, `bitacora` | Cuentas del personal y auditoría |

Incluye cuatro vistas (`v_libro_detalle`, `v_ejemplar_detalle`, `v_prestamo_detalle`,
`v_socio_detalle`), índices sobre las columnas de búsqueda frecuente y procedimientos
almacenados para el panel, la mora, los títulos más pedidos y el estado de cuenta de
cada socio.

---

## Identidad visual

| Color | Código | Uso |
|-------|--------|-----|
| Verde Tinta | `#14281E` | Menús, encabezados |
| Verde Esmeralda | `#1B7A52` | Acciones principales |
| Dorado Encuadernación | `#C9A227` | Acentos, indicadores |
| Papel | `#F5F2EA` | Fondo |

Tipografía: Segoe UI, con Georgia para los títulos de los libros y Consolas para
códigos de barras e ISBN. El logotipo (un libro abierto) es vectorial: no depende de
ningún archivo de imagen y se ve nítido en cualquier tamaño. La estantería del fondo
también es vectorial: un `DrawingBrush` teselado de 200×150 que se repite para llenar
la ventana sin dibujar cientos de rectángulos.

### Estructura de las pantallas

| Pantalla | Cómo está armada |
|----------|------------------|
| **Bienvenida** | Composición asimétrica: la marca a la izquierda y una estantería a la derecha cuyos lomos se van encendiendo — el progreso **es** la estantería, no hay barra aparte. Sin espera artificial: en cuanto responde la base de datos, cede el paso |
| **Inicio de sesión** | Ventana sin marco con una pared de estantería a pantalla completa y **una sola tarjeta flotante** al centro, con la marca en su propia cabecera oscura |
| **Crear cuenta** | Tres bloques repartidos a lo ancho (*tus datos*, *tu acceso*, *recuperación*) en vez de una columna con barra de desplazamiento: el formulario se ve completo de una mirada |
| **Ventana principal** | Dos capas que no compiten: un **riel de iconos plegable** a la izquierda para las secciones, y una **barra global** arriba con el buscador, el aviso de mora, el reloj y la cuenta conectada |

El buscador de la barra global es uno solo para todo: según lo que se escriba
—un folio `PR-000012`, un código de socio `U00007` o un título— el sistema decide a
qué pantalla llevar y entra con el filtro ya puesto.
