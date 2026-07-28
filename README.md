# PARKO Honduras — Sistema de Parqueadero

> *Espacio inteligente, flujo constante.*

Sistema de gestión de parqueaderos desarrollado en **VB.NET** sobre **SQL Server**.
Proyecto del III Parcial — Alejandro Calderón.

---

## Qué hace el sistema

- **Inicio de sesión y registro** de usuarios con contraseñas cifradas (BCrypt)
- **Roles**: Administrador (control total) y Operador (no puede eliminar registros)
- **Recuperación de contraseña** mediante pregunta de seguridad
- **Gestión de clientes**, parqueaderos (con fotografía) y vehículos
- **Control de entradas y salidas** con cálculo automático de tarifa y ticket de cobro
- **Panel de indicadores** con vehículos dentro, ingresos del día y últimos movimientos

---

## Los dos proyectos

| Proyecto | Tecnología | Módulos |
|----------|-----------|---------|
| `ParqueaderoWPF/` | WPF (.NET 9) | Completo: login, dashboard, clientes, parqueaderos, vehículos, entradas y salidas |
| `ParkoForms/` | Windows Forms (.NET 9) | Versión simplificada: login, clientes, parqueaderos, vehículos |

Ambos comparten la **misma base de datos** y la **misma capa de servicios**: los usuarios
registrados en uno funcionan en el otro.

---

## Requisitos

- Visual Studio 2022 o superior
- .NET 9 SDK
- SQL Server Express (instancia `ALECALDE\SQLEXPRESS`)

---

## Instalación

**1. Crear la base de datos.** Ejecuta los scripts en orden desde SQL Server Management Studio,
o desde la terminal:

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -i "ParqueaderoWPF/Scripts/01_tablas_sistema.sql"
```

```bash
sqlcmd -S "ALECALDE\SQLEXPRESS" -E -i "ParqueaderoWPF/Scripts/02_indices_y_procedimientos.sql"
```

**2. Abrir y ejecutar.** Abre `ParqueaderoWPF/ParqueaderoWPF.slnx` en Visual Studio y presiona F5.

**3. Crear tu cuenta.** En la pantalla de inicio haz clic en *Regístrate aquí*.
El **primer usuario registrado queda como Administrador**; los siguientes serán Operadores.

Si la cadena de conexión de tu servidor es distinta, edítala en `ParqueaderoWPF/Services/Db.vb`.

---

## Ejecutar las pruebas

```bash
dotnet test ParqueaderoWPF/ParqueaderoWPF.Tests/ParqueaderoWPF.Tests.vbproj
```

33 pruebas unitarias cubren las validaciones de registro y el cálculo de cobro por hora o fracción.

---

## Estructura del código

El proyecto sigue una **arquitectura en capas**, donde la lógica del negocio no depende de
la interfaz. Gracias a eso, la versión de Windows Forms reutiliza los mismos servicios sin cambios.

```
ParqueaderoWPF/
├── Models/          Entidades del negocio (Usuario)
├── Services/        Lógica y acceso a datos (AuthService, ClienteService, MovimientoService…)
├── Utilities/       Apoyo transversal (Validador, Sesión, Registro, MensajeError)
├── Views/           Ventanas, páginas y controles (XAML + code-behind)
├── Assets/          Logotipo e iconos
└── Scripts/         Scripts SQL de la base de datos
```

**Reglas que se respetan:**

- Las contraseñas se guardan con **hash BCrypt**, nunca en texto plano
- Todas las consultas usan **parámetros** (evita inyección SQL)
- La cuenta se **bloquea 30 segundos** tras 3 intentos fallidos
- Los errores técnicos se guardan en `logs/`; al usuario se le muestra un mensaje entendible
- El estilo visual está centralizado en `Application.xaml` (colores, botones, tablas)

---

## Base de datos

| Tabla | Contenido |
|-------|-----------|
| `usuario` | Cuentas del sistema, rol y pregunta de seguridad |
| `cliente` | Clientes del parqueadero |
| `parqueadero` | Sucursales |
| `vehiculo` | Vehículos asociados a cada cliente |
| `tarifa` | Precio por hora según tipo de vehículo |
| `movimiento` | Entradas y salidas con el cobro calculado |

Incluye índices sobre las columnas de búsqueda frecuente y procedimientos almacenados
para las consultas del panel de indicadores y los movimientos.

---

## Identidad visual

| Color | Código | Uso |
|-------|--------|-----|
| Azul Tecno | `#0A2540` | Menús, encabezados |
| Verde Eléctrico | `#00E676` | Acentos, indicadores |

Tipografía: Segoe UI.
